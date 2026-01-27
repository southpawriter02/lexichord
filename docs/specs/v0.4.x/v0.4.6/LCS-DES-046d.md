# LCS-DES-046d: Search History

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-046d                             |
| **Version**      | v0.4.6d                                  |
| **Title**        | Search History                           |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Modules.RAG`                  |
| **License Tier** | WriterPro                                |

---

## 1. Overview

### 1.1 Purpose

This specification defines `ISearchHistoryService` and `SearchHistoryService`, which store and recall recent search queries. The service maintains an in-memory list of recent queries with optional persistence to user settings.

### 1.2 Goals

- Define `ISearchHistoryService` interface for query history
- Implement in-memory storage with configurable size limit
- Deduplicate queries (most recent wins)
- Support optional persistence via `IConfigurationService`
- Provide clear history action
- Thread-safe operations

### 1.3 Non-Goals

- Search result caching (handled separately)
- Query analytics/statistics (future)
- Cross-device sync (future)
- Query suggestions/autocomplete beyond history (future)

---

## 2. Design

### 2.1 ISearchHistoryService Interface

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for managing search query history.
/// </summary>
public interface ISearchHistoryService
{
    /// <summary>
    /// Gets the list of recent queries, most recent first.
    /// </summary>
    IReadOnlyList<string> RecentQueries { get; }

    /// <summary>
    /// Gets the maximum number of queries to retain.
    /// </summary>
    int MaxHistorySize { get; }

    /// <summary>
    /// Adds a query to the history.
    /// If the query already exists, it moves to the front.
    /// </summary>
    /// <param name="query">The query to add. Empty/whitespace queries are ignored.</param>
    void AddQuery(string query);

    /// <summary>
    /// Removes a specific query from history.
    /// </summary>
    /// <param name="query">The query to remove.</param>
    /// <returns>True if removed, false if not found.</returns>
    bool RemoveQuery(string query);

    /// <summary>
    /// Clears all query history.
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Saves history to persistent storage.
    /// </summary>
    Task SaveAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads history from persistent storage.
    /// </summary>
    Task LoadAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when history changes.
    /// </summary>
    event EventHandler<SearchHistoryChangedEventArgs>? HistoryChanged;
}
```

### 2.2 SearchHistoryChangedEventArgs

```csharp
namespace Lexichord.Modules.RAG.Events;

/// <summary>
/// Event args for search history changes.
/// </summary>
public class SearchHistoryChangedEventArgs : EventArgs
{
    public SearchHistoryChangeType ChangeType { get; init; }
    public string? Query { get; init; }
    public int NewCount { get; init; }
}

public enum SearchHistoryChangeType
{
    Added,
    Removed,
    Cleared,
    Loaded
}
```

### 2.3 SearchHistoryService Implementation

```csharp
namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// In-memory search history with optional persistence.
/// Thread-safe implementation using locks.
/// </summary>
public sealed class SearchHistoryService : ISearchHistoryService, IDisposable
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<SearchHistoryService> _logger;
    private readonly List<string> _history = new();
    private readonly object _lock = new();
    private readonly int _maxSize;
    private bool _isDirty;

    private const string ConfigKey = "SearchHistory";
    private const int DefaultMaxSize = 10;

    public event EventHandler<SearchHistoryChangedEventArgs>? HistoryChanged;

    public int MaxHistorySize => _maxSize;

    public IReadOnlyList<string> RecentQueries
    {
        get
        {
            lock (_lock)
            {
                return _history.ToList().AsReadOnly();
            }
        }
    }

    public SearchHistoryService(
        IConfigurationService configService,
        ILogger<SearchHistoryService> logger,
        int maxSize = DefaultMaxSize)
    {
        _configService = configService;
        _logger = logger;
        _maxSize = Math.Max(1, maxSize);
    }

    public void AddQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return;

        query = query.Trim();

        lock (_lock)
        {
            // Remove if exists (will re-add at front)
            var existingIndex = _history.FindIndex(q =>
                string.Equals(q, query, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                _history.RemoveAt(existingIndex);
            }

            // Add at front
            _history.Insert(0, query);

            // Trim to max size
            while (_history.Count > _maxSize)
            {
                _history.RemoveAt(_history.Count - 1);
            }

            _isDirty = true;
        }

        _logger.LogDebug("Added query to history: {Query}", query);

        OnHistoryChanged(new SearchHistoryChangedEventArgs
        {
            ChangeType = SearchHistoryChangeType.Added,
            Query = query,
            NewCount = _history.Count
        });
    }

    public bool RemoveQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return false;

        bool removed;
        lock (_lock)
        {
            var index = _history.FindIndex(q =>
                string.Equals(q, query, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                _history.RemoveAt(index);
                _isDirty = true;
                removed = true;
            }
            else
            {
                removed = false;
            }
        }

        if (removed)
        {
            _logger.LogDebug("Removed query from history: {Query}", query);

            OnHistoryChanged(new SearchHistoryChangedEventArgs
            {
                ChangeType = SearchHistoryChangeType.Removed,
                Query = query,
                NewCount = _history.Count
            });
        }

        return removed;
    }

    public void ClearHistory()
    {
        lock (_lock)
        {
            _history.Clear();
            _isDirty = true;
        }

        _logger.LogInformation("Search history cleared");

        OnHistoryChanged(new SearchHistoryChangedEventArgs
        {
            ChangeType = SearchHistoryChangeType.Cleared,
            NewCount = 0
        });
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        List<string> toSave;
        lock (_lock)
        {
            if (!_isDirty)
            {
                _logger.LogDebug("History not dirty, skipping save");
                return;
            }

            toSave = _history.ToList();
        }

        try
        {
            var json = JsonSerializer.Serialize(toSave);
            await _configService.SetValueAsync(ConfigKey, json, ct);

            lock (_lock)
            {
                _isDirty = false;
            }

            _logger.LogDebug("Saved {Count} queries to history", toSave.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save search history");
            throw;
        }
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            var json = await _configService.GetValueAsync(ConfigKey, ct);

            if (string.IsNullOrEmpty(json))
            {
                _logger.LogDebug("No saved history found");
                return;
            }

            var loaded = JsonSerializer.Deserialize<List<string>>(json);

            if (loaded == null || loaded.Count == 0)
            {
                _logger.LogDebug("Loaded history was empty");
                return;
            }

            lock (_lock)
            {
                _history.Clear();
                foreach (var query in loaded.Take(_maxSize))
                {
                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        _history.Add(query.Trim());
                    }
                }
                _isDirty = false;
            }

            _logger.LogInformation("Loaded {Count} queries from history", _history.Count);

            OnHistoryChanged(new SearchHistoryChangedEventArgs
            {
                ChangeType = SearchHistoryChangeType.Loaded,
                NewCount = _history.Count
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse search history, starting fresh");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load search history");
            throw;
        }
    }

    private void OnHistoryChanged(SearchHistoryChangedEventArgs args)
    {
        HistoryChanged?.Invoke(this, args);
    }

    public void Dispose()
    {
        // Fire-and-forget save on dispose
        if (_isDirty)
        {
            try
            {
                SaveAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save history on dispose");
            }
        }
    }
}
```

### 2.4 DI Registration

```csharp
// In RAGModule.cs
services.AddSingleton<ISearchHistoryService>(sp =>
{
    var configService = sp.GetRequiredService<IConfigurationService>();
    var logger = sp.GetRequiredService<ILogger<SearchHistoryService>>();

    var service = new SearchHistoryService(configService, logger, maxSize: 10);

    // Load history on startup
    _ = service.LoadAsync();

    return service;
});
```

---

## 3. Storage Format

### 3.1 Persistence Schema

History is stored as a JSON array in user configuration:

```json
{
  "SearchHistory": "[\"most recent query\",\"older query\",\"oldest query\"]"
}
```

### 3.2 Configuration Key

| Key | Type | Location |
| :-- | :--- | :------- |
| `SearchHistory` | string (JSON array) | User settings |

---

## 4. History Dropdown UI Integration

### 4.1 AutoCompleteBox Binding

```xml
<!-- In ReferenceView.axaml -->
<AutoCompleteBox Text="{Binding SearchQuery, Mode=TwoWay}"
                 ItemsSource="{Binding SearchHistory}"
                 FilterMode="Contains"
                 Watermark="Search documents..."
                 MinimumPrefixLength="0"
                 IsDropDownOpen="{Binding IsHistoryDropDownOpen}">
    <AutoCompleteBox.ItemTemplate>
        <DataTemplate>
            <Grid ColumnDefinitions="*,Auto">
                <TextBlock Grid.Column="0"
                           Text="{Binding}"
                           TextTrimming="CharacterEllipsis"/>
                <Button Grid.Column="1"
                        Classes="icon-button"
                        Command="{Binding $parent[UserControl].DataContext.RemoveHistoryItemCommand}"
                        CommandParameter="{Binding}"
                        ToolTip.Tip="Remove from history">
                    <PathIcon Data="{StaticResource CloseIcon}" Width="12" Height="12"/>
                </Button>
            </Grid>
        </DataTemplate>
    </AutoCompleteBox.ItemTemplate>
</AutoCompleteBox>
```

### 4.2 Clear History Button

```xml
<!-- History management in panel header -->
<Button Command="{Binding ClearHistoryCommand}"
        IsEnabled="{Binding SearchHistory.Count}"
        ToolTip.Tip="Clear search history">
    <StackPanel Orientation="Horizontal" Spacing="4">
        <PathIcon Data="{StaticResource TrashIcon}" Width="14" Height="14"/>
        <TextBlock Text="Clear History"/>
    </StackPanel>
</Button>
```

---

## 5. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6d")]
public class SearchHistoryServiceTests
{
    private readonly Mock<IConfigurationService> _configMock;
    private readonly SearchHistoryService _sut;

    public SearchHistoryServiceTests()
    {
        _configMock = new Mock<IConfigurationService>();
        _sut = new SearchHistoryService(
            _configMock.Object,
            NullLogger<SearchHistoryService>.Instance,
            maxSize: 5);
    }

    [Fact]
    public void AddQuery_EmptyString_Ignored()
    {
        _sut.AddQuery("");
        _sut.AddQuery("   ");
        _sut.AddQuery(null!);

        _sut.RecentQueries.Should().BeEmpty();
    }

    [Fact]
    public void AddQuery_ValidQuery_AddsToFront()
    {
        _sut.AddQuery("first");
        _sut.AddQuery("second");

        _sut.RecentQueries.Should().Equal("second", "first");
    }

    [Fact]
    public void AddQuery_DuplicateQuery_MovesToFront()
    {
        _sut.AddQuery("first");
        _sut.AddQuery("second");
        _sut.AddQuery("first");

        _sut.RecentQueries.Should().Equal("first", "second");
        _sut.RecentQueries.Should().HaveCount(2);
    }

    [Fact]
    public void AddQuery_DuplicateCaseInsensitive_MovesToFront()
    {
        _sut.AddQuery("Query");
        _sut.AddQuery("other");
        _sut.AddQuery("QUERY");

        _sut.RecentQueries.Should().HaveCount(2);
        _sut.RecentQueries[0].Should().Be("QUERY");
    }

    [Fact]
    public void AddQuery_ExceedsMaxSize_RemovesOldest()
    {
        for (int i = 1; i <= 7; i++)
        {
            _sut.AddQuery($"query{i}");
        }

        _sut.RecentQueries.Should().HaveCount(5);
        _sut.RecentQueries.Should().Equal("query7", "query6", "query5", "query4", "query3");
    }

    [Fact]
    public void AddQuery_TrimsWhitespace()
    {
        _sut.AddQuery("  spaced query  ");

        _sut.RecentQueries.Should().ContainSingle()
            .Which.Should().Be("spaced query");
    }

    [Fact]
    public void RemoveQuery_Exists_ReturnsTrue()
    {
        _sut.AddQuery("test");

        var result = _sut.RemoveQuery("test");

        result.Should().BeTrue();
        _sut.RecentQueries.Should().BeEmpty();
    }

    [Fact]
    public void RemoveQuery_NotExists_ReturnsFalse()
    {
        _sut.AddQuery("test");

        var result = _sut.RemoveQuery("other");

        result.Should().BeFalse();
        _sut.RecentQueries.Should().ContainSingle();
    }

    [Fact]
    public void RemoveQuery_CaseInsensitive()
    {
        _sut.AddQuery("Test Query");

        var result = _sut.RemoveQuery("TEST QUERY");

        result.Should().BeTrue();
        _sut.RecentQueries.Should().BeEmpty();
    }

    [Fact]
    public void ClearHistory_RemovesAll()
    {
        _sut.AddQuery("one");
        _sut.AddQuery("two");
        _sut.AddQuery("three");

        _sut.ClearHistory();

        _sut.RecentQueries.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_PersistsToConfig()
    {
        _sut.AddQuery("query1");
        _sut.AddQuery("query2");

        await _sut.SaveAsync();

        _configMock.Verify(c => c.SetValueAsync(
            "SearchHistory",
            It.Is<string>(json => json.Contains("query1") && json.Contains("query2")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveAsync_NotDirty_SkipsSave()
    {
        // No queries added, not dirty

        await _sut.SaveAsync();

        _configMock.Verify(c => c.SetValueAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LoadAsync_RestoresHistory()
    {
        _configMock.Setup(c => c.GetValueAsync("SearchHistory", It.IsAny<CancellationToken>()))
            .ReturnsAsync("[\"saved1\",\"saved2\",\"saved3\"]");

        await _sut.LoadAsync();

        _sut.RecentQueries.Should().Equal("saved1", "saved2", "saved3");
    }

    [Fact]
    public async Task LoadAsync_EmptyConfig_NoError()
    {
        _configMock.Setup(c => c.GetValueAsync("SearchHistory", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        await _sut.LoadAsync();

        _sut.RecentQueries.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_InvalidJson_HandlesGracefully()
    {
        _configMock.Setup(c => c.GetValueAsync("SearchHistory", It.IsAny<CancellationToken>()))
            .ReturnsAsync("not valid json");

        await _sut.LoadAsync();

        _sut.RecentQueries.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_RespectsMaxSize()
    {
        var manyQueries = Enumerable.Range(1, 20).Select(i => $"query{i}").ToList();
        var json = JsonSerializer.Serialize(manyQueries);

        _configMock.Setup(c => c.GetValueAsync("SearchHistory", It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);

        await _sut.LoadAsync();

        _sut.RecentQueries.Should().HaveCount(5); // maxSize is 5
    }

    [Fact]
    public void HistoryChanged_RaisedOnAdd()
    {
        SearchHistoryChangedEventArgs? args = null;
        _sut.HistoryChanged += (_, e) => args = e;

        _sut.AddQuery("test");

        args.Should().NotBeNull();
        args!.ChangeType.Should().Be(SearchHistoryChangeType.Added);
        args.Query.Should().Be("test");
        args.NewCount.Should().Be(1);
    }

    [Fact]
    public void HistoryChanged_RaisedOnRemove()
    {
        _sut.AddQuery("test");
        SearchHistoryChangedEventArgs? args = null;
        _sut.HistoryChanged += (_, e) => args = e;

        _sut.RemoveQuery("test");

        args.Should().NotBeNull();
        args!.ChangeType.Should().Be(SearchHistoryChangeType.Removed);
        args.Query.Should().Be("test");
    }

    [Fact]
    public void HistoryChanged_RaisedOnClear()
    {
        _sut.AddQuery("test");
        SearchHistoryChangedEventArgs? args = null;
        _sut.HistoryChanged += (_, e) => args = e;

        _sut.ClearHistory();

        args.Should().NotBeNull();
        args!.ChangeType.Should().Be(SearchHistoryChangeType.Cleared);
        args.NewCount.Should().Be(0);
    }

    [Fact]
    public void RecentQueries_IsThreadSafe()
    {
        // Add queries from multiple threads
        var tasks = Enumerable.Range(1, 100)
            .Select(i => Task.Run(() => _sut.AddQuery($"query{i}")))
            .ToArray();

        Task.WaitAll(tasks);

        // Should not throw, count should be maxSize
        _sut.RecentQueries.Should().HaveCount(5);
    }
}
```

---

## 6. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Debug | "Added query to history: {Query}" | After add |
| Debug | "Removed query from history: {Query}" | After remove |
| Debug | "History not dirty, skipping save" | Save skipped |
| Debug | "No saved history found" | Empty config |
| Debug | "Saved {Count} queries to history" | After save |
| Information | "Search history cleared" | After clear |
| Information | "Loaded {Count} queries from history" | After load |
| Warning | "Failed to parse search history" | Invalid JSON |
| Warning | "Failed to save history on dispose" | Dispose error |
| Error | "Failed to save search history" | Save exception |
| Error | "Failed to load search history" | Load exception |

---

## 7. File Locations

| File | Path |
| :--- | :--- |
| Interface | `src/Lexichord.Abstractions/Contracts/ISearchHistoryService.cs` |
| Implementation | `src/Lexichord.Modules.RAG/Services/SearchHistoryService.cs` |
| Event args | `src/Lexichord.Modules.RAG/Events/SearchHistoryChangedEventArgs.cs` |
| Unit tests | `tests/Lexichord.Modules.RAG.Tests/Services/SearchHistoryServiceTests.cs` |

---

## 8. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | AddQuery adds queries to front of list | [ ] |
| 2 | AddQuery deduplicates (case-insensitive, moves to front) | [ ] |
| 3 | AddQuery respects MaxHistorySize | [ ] |
| 4 | AddQuery ignores empty/whitespace | [ ] |
| 5 | RemoveQuery removes specific query | [ ] |
| 6 | ClearHistory removes all queries | [ ] |
| 7 | SaveAsync persists to IConfigurationService | [ ] |
| 8 | LoadAsync restores from IConfigurationService | [ ] |
| 9 | HistoryChanged event fires appropriately | [ ] |
| 10 | Thread-safe for concurrent access | [ ] |
| 11 | All unit tests pass | [ ] |

---

## 9. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
