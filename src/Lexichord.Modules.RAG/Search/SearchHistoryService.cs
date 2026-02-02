// =============================================================================
// File: SearchHistoryService.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of search query history with in-memory storage
//              and optional persistence to system settings.
// =============================================================================
// LOGIC: Thread-safe in-memory history service using a LinkedList for O(1) operations.
//   - AddQuery: Deduplicates, moves existing to front, evicts LRU when full.
//   - RemoveQuery: Removes specific query (case-insensitive) (v0.4.6d).
//   - GetRecentQueries: Returns snapshot of history (newest first).
//   - RecentQueries: Property access to full history (v0.4.6d).
//   - ClearHistory: Clears all stored queries.
//   - SaveAsync/LoadAsync: Persist/restore to ISystemSettingsRepository (v0.4.6d).
//   - HistoryChanged: Event fired on all mutations (v0.4.6d).
//   - Uses lock for thread safety (singleton lifetime).
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.6a: ISearchHistoryService (original)
//   - v0.4.6d: Enhanced interface with persistence and events
//   - v0.0.x: ISystemSettingsRepository (persistence layer)
// =============================================================================

using System.Text.Json;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Thread-safe in-memory implementation of search query history with persistence.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SearchHistoryService"/> maintains a list of recent search queries
/// in memory, enabling quick re-execution and autocomplete in the Reference Panel.
/// The service uses a <see cref="LinkedList{T}"/> for efficient O(1) insertions
/// at the front and removals from the back.
/// </para>
/// <para>
/// <b>Persistence:</b> History is persisted to <see cref="ISystemSettingsRepository"/>
/// as a JSON array. The service tracks dirty state to avoid unnecessary saves.
/// On dispose, unsaved changes are persisted (fire-and-forget).
/// </para>
/// <para>
/// <b>Thread Safety:</b> All operations are protected by a lock to ensure
/// thread safety when accessed from multiple view models concurrently.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6a as part of the Reference Panel View.
/// Enhanced in v0.4.6d with persistence and events.
/// </para>
/// </remarks>
public sealed class SearchHistoryService : ISearchHistoryService, IDisposable
{
    private readonly LinkedList<string> _history = new();
    private readonly Dictionary<string, LinkedListNode<string>> _index = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private readonly ILogger<SearchHistoryService> _logger;
    private readonly ISystemSettingsRepository? _settingsRepository;
    private readonly int _maxSize;
    private bool _isDirty;

    private const string SettingsKey = "rag:search_history";
    private const int DefaultMaxSize = 10;

    /// <inheritdoc />
    public event EventHandler<SearchHistoryChangedEventArgs>? HistoryChanged;

    /// <inheritdoc />
    public int MaxHistorySize => _maxSize;

    /// <inheritdoc />
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

    /// <summary>
    /// Creates a new <see cref="SearchHistoryService"/> instance.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="settingsRepository">
    /// Optional settings repository for persistence. If null, persistence is disabled.
    /// </param>
    /// <param name="maxSize">Maximum number of queries to retain. Default: 10.</param>
    public SearchHistoryService(
        ILogger<SearchHistoryService> logger,
        ISystemSettingsRepository? settingsRepository = null,
        int maxSize = DefaultMaxSize)
    {
        _logger = logger;
        _settingsRepository = settingsRepository;
        _maxSize = Math.Max(1, maxSize);

        _logger.LogDebug(
            "SearchHistoryService initialized with MaxSize={MaxSize}, Persistence={HasPersistence}",
            _maxSize,
            _settingsRepository is not null);
    }

    /// <inheritdoc />
    public void AddQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return;

        var trimmed = query.Trim();
        int newCount;

        lock (_lock)
        {
            // LOGIC: If the query exists, move it to the front.
            if (_index.TryGetValue(trimmed, out var existingNode))
            {
                _history.Remove(existingNode);
                _history.AddFirst(existingNode);
                _isDirty = true;
                newCount = _history.Count;
                _logger.LogDebug("Moved existing query to front: {Query}", trimmed);
            }
            else
            {
                // LOGIC: Add new query to front.
                var newNode = _history.AddFirst(trimmed);
                _index[trimmed] = newNode;

                // LOGIC: Evict oldest if over capacity.
                if (_history.Count > _maxSize)
                {
                    var lastNode = _history.Last!;
                    _index.Remove(lastNode.Value);
                    _history.RemoveLast();
                    _logger.LogDebug("Evicted oldest query due to capacity: {Query}", lastNode.Value);
                }

                _isDirty = true;
                newCount = _history.Count;
                _logger.LogDebug("Added new query to history: {Query}", trimmed);
            }
        }

        OnHistoryChanged(new SearchHistoryChangedEventArgs
        {
            ChangeType = SearchHistoryChangeType.Added,
            Query = trimmed,
            NewCount = newCount
        });
    }

    /// <inheritdoc />
    public bool RemoveQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return false;

        var trimmed = query.Trim();
        bool removed;
        int newCount;

        lock (_lock)
        {
            if (_index.TryGetValue(trimmed, out var node))
            {
                _history.Remove(node);
                _index.Remove(trimmed);
                _isDirty = true;
                removed = true;
                newCount = _history.Count;
            }
            else
            {
                removed = false;
                newCount = _history.Count;
            }
        }

        if (removed)
        {
            _logger.LogDebug("Removed query from history: {Query}", trimmed);

            OnHistoryChanged(new SearchHistoryChangedEventArgs
            {
                ChangeType = SearchHistoryChangeType.Removed,
                Query = trimmed,
                NewCount = newCount
            });
        }

        return removed;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetRecentQueries(int count = 10)
    {
        if (count <= 0)
            return Array.Empty<string>();

        lock (_lock)
        {
            return _history.Take(count).ToList().AsReadOnly();
        }
    }

    /// <inheritdoc />
    public void ClearHistory()
    {
        lock (_lock)
        {
            _history.Clear();
            _index.Clear();
            _isDirty = true;
        }

        _logger.LogInformation("Search history cleared");

        OnHistoryChanged(new SearchHistoryChangedEventArgs
        {
            ChangeType = SearchHistoryChangeType.Cleared,
            NewCount = 0
        });
    }

    /// <inheritdoc />
    public async Task SaveAsync(CancellationToken ct = default)
    {
        if (_settingsRepository is null)
        {
            _logger.LogDebug("Persistence disabled, skipping save");
            return;
        }

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
            await _settingsRepository.SetValueAsync(
                SettingsKey,
                json,
                "Recent search queries for Reference Panel",
                ct);

            lock (_lock)
            {
                _isDirty = false;
            }

            _logger.LogDebug("Saved {Count} queries to history", toSave.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Save operation cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save search history");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (_settingsRepository is null)
        {
            _logger.LogDebug("Persistence disabled, skipping load");
            return;
        }

        try
        {
            var json = await _settingsRepository.GetValueAsync(SettingsKey, "", ct);

            if (string.IsNullOrEmpty(json))
            {
                _logger.LogDebug("No saved history found");
                return;
            }

            var loaded = JsonSerializer.Deserialize<List<string>>(json);

            if (loaded is null || loaded.Count == 0)
            {
                _logger.LogDebug("Loaded history was empty");
                return;
            }

            int newCount;
            lock (_lock)
            {
                _history.Clear();
                _index.Clear();

                foreach (var query in loaded.Take(_maxSize))
                {
                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        var trimmed = query.Trim();
                        // Avoid duplicates in loaded data
                        if (!_index.ContainsKey(trimmed))
                        {
                            var node = _history.AddLast(trimmed);
                            _index[trimmed] = node;
                        }
                    }
                }
                _isDirty = false;
                newCount = _history.Count;
            }

            _logger.LogInformation("Loaded {Count} queries from history", newCount);

            OnHistoryChanged(new SearchHistoryChangedEventArgs
            {
                ChangeType = SearchHistoryChangeType.Loaded,
                NewCount = newCount
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse search history, starting fresh");
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Load operation cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load search history");
            throw;
        }
    }

    /// <summary>
    /// Disposes the service, saving any unsaved changes.
    /// </summary>
    public void Dispose()
    {
        // Fire-and-forget save on dispose
        if (_isDirty && _settingsRepository is not null)
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

    private void OnHistoryChanged(SearchHistoryChangedEventArgs args)
    {
        HistoryChanged?.Invoke(this, args);
    }
}
