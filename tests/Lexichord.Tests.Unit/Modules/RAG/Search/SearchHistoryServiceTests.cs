// =============================================================================
// File: SearchHistoryServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the SearchHistoryService.
// =============================================================================
// TESTS:
//   - v0.4.6a: Basic history operations (add, get, clear, capacity, thread safety)
//   - v0.4.6d: Enhanced features (RecentQueries, RemoveQuery, persistence, events)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Search;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Search;

/// <summary>
/// Unit tests for <see cref="SearchHistoryService"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6")]
public class SearchHistoryServiceTests
{
    private readonly Mock<ILogger<SearchHistoryService>> _loggerMock;
    private readonly Mock<ISystemSettingsRepository> _settingsRepoMock;
    private readonly SearchHistoryService _sut;

    public SearchHistoryServiceTests()
    {
        _loggerMock = new Mock<ILogger<SearchHistoryService>>();
        _settingsRepoMock = new Mock<ISystemSettingsRepository>();
        _sut = new SearchHistoryService(_loggerMock.Object, _settingsRepoMock.Object);
    }

    // =========================================================================
    // MaxHistorySize Tests
    // =========================================================================

    [Fact]
    public void MaxHistorySize_ReturnsDefaultValue()
    {
        // Assert
        _sut.MaxHistorySize.Should().Be(10);
    }

    [Fact]
    public void MaxHistorySize_ReturnsCustomValue()
    {
        // Arrange
        var sut = new SearchHistoryService(_loggerMock.Object, null, maxSize: 5);

        // Assert
        sut.MaxHistorySize.Should().Be(5);
    }

    [Fact]
    public void MaxHistorySize_EnforcesMinimumOfOne()
    {
        // Arrange
        var sut = new SearchHistoryService(_loggerMock.Object, null, maxSize: 0);

        // Assert
        sut.MaxHistorySize.Should().Be(1);
    }

    // =========================================================================
    // AddQuery Tests
    // =========================================================================

    [Fact]
    public void AddQuery_NullQuery_DoesNothing()
    {
        // Act
        _sut.AddQuery(null);

        // Assert
        _sut.GetRecentQueries().Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void AddQuery_WhitespaceQuery_DoesNothing(string query)
    {
        // Act
        _sut.AddQuery(query);

        // Assert
        _sut.GetRecentQueries().Should().BeEmpty();
    }

    [Fact]
    public void AddQuery_ValidQuery_AddsToHistory()
    {
        // Act
        _sut.AddQuery("test query");

        // Assert
        _sut.GetRecentQueries().Should().ContainSingle()
            .Which.Should().Be("test query");
    }

    [Fact]
    public void AddQuery_TrimsWhitespace()
    {
        // Act
        _sut.AddQuery("  test query  ");

        // Assert
        _sut.GetRecentQueries().Should().ContainSingle()
            .Which.Should().Be("test query");
    }

    [Fact]
    public void AddQuery_MultipleQueries_NewestFirst()
    {
        // Act
        _sut.AddQuery("first");
        _sut.AddQuery("second");
        _sut.AddQuery("third");

        // Assert
        var history = _sut.GetRecentQueries();
        history.Should().HaveCount(3);
        history[0].Should().Be("third");
        history[1].Should().Be("second");
        history[2].Should().Be("first");
    }

    [Fact]
    public void AddQuery_DuplicateQuery_MovesToFront_CaseInsensitive()
    {
        // Arrange
        _sut.AddQuery("first");
        _sut.AddQuery("second");
        _sut.AddQuery("third");

        // Act - Add "FIRST" (different case)
        _sut.AddQuery("FIRST");

        // Assert - "first" moves to front (original case preserved)
        var history = _sut.GetRecentQueries();
        history.Should().HaveCount(3);
        history[0].Should().Be("first");  // Original case preserved
        history[1].Should().Be("third");
        history[2].Should().Be("second");
    }

    [Fact]
    public void AddQuery_ExceedsMaxSize_EvictsOldest()
    {
        // Arrange - Add 10 queries (MaxHistorySize)
        for (int i = 1; i <= 10; i++)
        {
            _sut.AddQuery($"query {i}");
        }

        // Act - Add one more
        _sut.AddQuery("newest");

        // Assert - oldest is evicted
        var history = _sut.GetRecentQueries();
        history.Should().HaveCount(10);
        history[0].Should().Be("newest");
        history.Should().NotContain("query 1");  // Evicted
        history.Should().Contain("query 2");       // Still present
    }

    // =========================================================================
    // RecentQueries Property Tests (v0.4.6d)
    // =========================================================================

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public void RecentQueries_EmptyHistory_ReturnsEmptyList()
    {
        // Assert
        _sut.RecentQueries.Should().BeEmpty();
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public void RecentQueries_ReturnsAllQueries()
    {
        // Arrange
        _sut.AddQuery("first");
        _sut.AddQuery("second");
        _sut.AddQuery("third");

        // Act
        var result = _sut.RecentQueries;

        // Assert
        result.Should().HaveCount(3);
        result.Should().Equal("third", "second", "first");
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public void RecentQueries_ReturnsNewSnapshot()
    {
        // Arrange
        _sut.AddQuery("test");
        var snapshot1 = _sut.RecentQueries;

        // Act
        _sut.AddQuery("new");
        var snapshot2 = _sut.RecentQueries;

        // Assert - snapshots should be different objects
        snapshot1.Should().NotBeSameAs(snapshot2);
        snapshot1.Should().HaveCount(1);
        snapshot2.Should().HaveCount(2);
    }

    // =========================================================================
    // GetRecentQueries Tests
    // =========================================================================

    [Fact]
    public void GetRecentQueries_EmptyHistory_ReturnsEmptyList()
    {
        // Assert
        _sut.GetRecentQueries().Should().BeEmpty();
    }

    [Fact]
    public void GetRecentQueries_ZeroCount_ReturnsEmptyList()
    {
        // Arrange
        _sut.AddQuery("test");

        // Act
        var result = _sut.GetRecentQueries(0);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetRecentQueries_NegativeCount_ReturnsEmptyList()
    {
        // Arrange
        _sut.AddQuery("test");

        // Act
        var result = _sut.GetRecentQueries(-1);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetRecentQueries_LimitedCount_ReturnsRequestedNumber()
    {
        // Arrange
        _sut.AddQuery("first");
        _sut.AddQuery("second");
        _sut.AddQuery("third");

        // Act
        var result = _sut.GetRecentQueries(2);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().Be("third");
        result[1].Should().Be("second");
    }

    [Fact]
    public void GetRecentQueries_CountExceedsHistory_ReturnsAll()
    {
        // Arrange
        _sut.AddQuery("first");
        _sut.AddQuery("second");

        // Act
        var result = _sut.GetRecentQueries(100);

        // Assert
        result.Should().HaveCount(2);
    }

    // =========================================================================
    // RemoveQuery Tests (v0.4.6d)
    // =========================================================================

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public void RemoveQuery_Exists_ReturnsTrue()
    {
        // Arrange
        _sut.AddQuery("test");

        // Act
        var result = _sut.RemoveQuery("test");

        // Assert
        result.Should().BeTrue();
        _sut.RecentQueries.Should().BeEmpty();
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public void RemoveQuery_NotExists_ReturnsFalse()
    {
        // Arrange
        _sut.AddQuery("test");

        // Act
        var result = _sut.RemoveQuery("other");

        // Assert
        result.Should().BeFalse();
        _sut.RecentQueries.Should().ContainSingle();
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public void RemoveQuery_CaseInsensitive()
    {
        // Arrange
        _sut.AddQuery("Test Query");

        // Act
        var result = _sut.RemoveQuery("TEST QUERY");

        // Assert
        result.Should().BeTrue();
        _sut.RecentQueries.Should().BeEmpty();
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public void RemoveQuery_NullOrWhitespace_ReturnsFalse()
    {
        // Arrange
        _sut.AddQuery("test");

        // Act & Assert
        _sut.RemoveQuery(null!).Should().BeFalse();
        _sut.RemoveQuery("").Should().BeFalse();
        _sut.RemoveQuery("   ").Should().BeFalse();
        _sut.RecentQueries.Should().ContainSingle();
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public void RemoveQuery_PreservesOrder()
    {
        // Arrange
        _sut.AddQuery("first");
        _sut.AddQuery("second");
        _sut.AddQuery("third");

        // Act
        _sut.RemoveQuery("second");

        // Assert
        _sut.RecentQueries.Should().Equal("third", "first");
    }

    // =========================================================================
    // ClearHistory Tests
    // =========================================================================

    [Fact]
    public void ClearHistory_EmptyHistory_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.ClearHistory();
        act.Should().NotThrow();
    }

    [Fact]
    public void ClearHistory_WithItems_RemovesAll()
    {
        // Arrange
        _sut.AddQuery("first");
        _sut.AddQuery("second");

        // Act
        _sut.ClearHistory();

        // Assert
        _sut.GetRecentQueries().Should().BeEmpty();
    }

    [Fact]
    public void ClearHistory_AllowsNewAdditions()
    {
        // Arrange
        _sut.AddQuery("old");
        _sut.ClearHistory();

        // Act
        _sut.AddQuery("new");

        // Assert
        _sut.GetRecentQueries().Should().ContainSingle()
            .Which.Should().Be("new");
    }

    // =========================================================================
    // HistoryChanged Event Tests (v0.4.6d)
    // =========================================================================

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public void HistoryChanged_RaisedOnAdd()
    {
        // Arrange
        SearchHistoryChangedEventArgs? args = null;
        _sut.HistoryChanged += (_, e) => args = e;

        // Act
        _sut.AddQuery("test");

        // Assert
        args.Should().NotBeNull();
        args!.ChangeType.Should().Be(SearchHistoryChangeType.Added);
        args.Query.Should().Be("test");
        args.NewCount.Should().Be(1);
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public void HistoryChanged_RaisedOnRemove()
    {
        // Arrange
        _sut.AddQuery("test");
        SearchHistoryChangedEventArgs? args = null;
        _sut.HistoryChanged += (_, e) => args = e;

        // Act
        _sut.RemoveQuery("test");

        // Assert
        args.Should().NotBeNull();
        args!.ChangeType.Should().Be(SearchHistoryChangeType.Removed);
        args.Query.Should().Be("test");
        args.NewCount.Should().Be(0);
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public void HistoryChanged_RaisedOnClear()
    {
        // Arrange
        _sut.AddQuery("test");
        SearchHistoryChangedEventArgs? args = null;
        _sut.HistoryChanged += (_, e) => args = e;

        // Act
        _sut.ClearHistory();

        // Assert
        args.Should().NotBeNull();
        args!.ChangeType.Should().Be(SearchHistoryChangeType.Cleared);
        args.Query.Should().BeNull();
        args.NewCount.Should().Be(0);
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public void HistoryChanged_NotRaisedOnRemoveNotFound()
    {
        // Arrange
        _sut.AddQuery("test");
        bool eventFired = false;
        _sut.HistoryChanged += (_, _) => eventFired = true;

        // Reset after add
        eventFired = false;

        // Act
        _sut.RemoveQuery("other");

        // Assert
        eventFired.Should().BeFalse();
    }

    // =========================================================================
    // SaveAsync Tests (v0.4.6d)
    // =========================================================================

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public async Task SaveAsync_PersistsToSettings()
    {
        // Arrange
        _sut.AddQuery("query1");
        _sut.AddQuery("query2");

        // Act
        await _sut.SaveAsync();

        // Assert
        _settingsRepoMock.Verify(r => r.SetValueAsync(
            "rag:search_history",
            It.Is<string>(json => json.Contains("query1") && json.Contains("query2")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public async Task SaveAsync_NotDirty_SkipsSave()
    {
        // Arrange - no queries added, not dirty

        // Act
        await _sut.SaveAsync();

        // Assert
        _settingsRepoMock.Verify(r => r.SetValueAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public async Task SaveAsync_ClearsDirtyFlag()
    {
        // Arrange
        _sut.AddQuery("test");
        await _sut.SaveAsync();

        // Act - Save again
        await _sut.SaveAsync();

        // Assert - Should only save once
        _settingsRepoMock.Verify(r => r.SetValueAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public async Task SaveAsync_NullRepository_DoesNotThrow()
    {
        // Arrange
        var sut = new SearchHistoryService(_loggerMock.Object, null);
        sut.AddQuery("test");

        // Act & Assert
        var act = async () => await sut.SaveAsync();
        await act.Should().NotThrowAsync();
    }

    // =========================================================================
    // LoadAsync Tests (v0.4.6d)
    // =========================================================================

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public async Task LoadAsync_RestoresHistory()
    {
        // Arrange
        _settingsRepoMock.Setup(r => r.GetValueAsync("rag:search_history", "", It.IsAny<CancellationToken>()))
            .ReturnsAsync("[\"saved1\",\"saved2\",\"saved3\"]");

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.RecentQueries.Should().Equal("saved1", "saved2", "saved3");
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public async Task LoadAsync_EmptyConfig_NoError()
    {
        // Arrange
        _settingsRepoMock.Setup(r => r.GetValueAsync("rag:search_history", "", It.IsAny<CancellationToken>()))
            .ReturnsAsync("");

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.RecentQueries.Should().BeEmpty();
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public async Task LoadAsync_InvalidJson_HandlesGracefully()
    {
        // Arrange
        _settingsRepoMock.Setup(r => r.GetValueAsync("rag:search_history", "", It.IsAny<CancellationToken>()))
            .ReturnsAsync("not valid json");

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.RecentQueries.Should().BeEmpty();
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public async Task LoadAsync_RespectsMaxSize()
    {
        // Arrange
        var sut = new SearchHistoryService(_loggerMock.Object, _settingsRepoMock.Object, maxSize: 5);
        var manyQueries = "[\"q1\",\"q2\",\"q3\",\"q4\",\"q5\",\"q6\",\"q7\",\"q8\",\"q9\",\"q10\"]";

        _settingsRepoMock.Setup(r => r.GetValueAsync("rag:search_history", "", It.IsAny<CancellationToken>()))
            .ReturnsAsync(manyQueries);

        // Act
        await sut.LoadAsync();

        // Assert
        sut.RecentQueries.Should().HaveCount(5);
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public async Task LoadAsync_RaisesHistoryChangedEvent()
    {
        // Arrange
        _settingsRepoMock.Setup(r => r.GetValueAsync("rag:search_history", "", It.IsAny<CancellationToken>()))
            .ReturnsAsync("[\"saved1\",\"saved2\"]");

        SearchHistoryChangedEventArgs? args = null;
        _sut.HistoryChanged += (_, e) => args = e;

        // Act
        await _sut.LoadAsync();

        // Assert
        args.Should().NotBeNull();
        args!.ChangeType.Should().Be(SearchHistoryChangeType.Loaded);
        args.NewCount.Should().Be(2);
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public async Task LoadAsync_NullRepository_DoesNotThrow()
    {
        // Arrange
        var sut = new SearchHistoryService(_loggerMock.Object, null);

        // Act & Assert
        var act = async () => await sut.LoadAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public async Task LoadAsync_DeduplicatesEntries()
    {
        // Arrange
        _settingsRepoMock.Setup(r => r.GetValueAsync("rag:search_history", "", It.IsAny<CancellationToken>()))
            .ReturnsAsync("[\"same\",\"SAME\",\"Same\"]");

        // Act
        await _sut.LoadAsync();

        // Assert - Should only have one entry (duplicates ignored)
        _sut.RecentQueries.Should().HaveCount(1);
    }

    // =========================================================================
    // Thread Safety Tests
    // =========================================================================

    [Fact]
    public void AddQuery_ConcurrentCalls_DoesNotThrow()
    {
        // Act & Assert
        var act = () => Parallel.For(0, 100, i =>
        {
            _sut.AddQuery($"query {i}");
        });

        act.Should().NotThrow();

        // Should have at most MaxHistorySize items
        _sut.GetRecentQueries().Count.Should().BeLessOrEqualTo(_sut.MaxHistorySize);
    }

    [Fact]
    public void GetRecentQueries_ConcurrentWithAdd_DoesNotThrow()
    {
        // Arrange
        _sut.AddQuery("initial");

        // Act & Assert
        var act = () => Parallel.For(0, 50, i =>
        {
            if (i % 2 == 0)
                _sut.AddQuery($"query {i}");
            else
                _ = _sut.GetRecentQueries();
        });

        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public void RecentQueries_ConcurrentAccess_DoesNotThrow()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
            _sut.AddQuery($"query {i}");

        // Act & Assert
        var act = () => Parallel.For(0, 100, i =>
        {
            _ = _sut.RecentQueries;
            if (i % 10 == 0)
                _sut.AddQuery($"new {i}");
        });

        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Feature", "v0.4.6d")]
    public void MixedOperations_ConcurrentCalls_DoesNotThrow()
    {
        // Act & Assert
        var act = () => Parallel.For(0, 100, i =>
        {
            switch (i % 4)
            {
                case 0:
                    _sut.AddQuery($"query {i}");
                    break;
                case 1:
                    _ = _sut.RecentQueries;
                    break;
                case 2:
                    _sut.RemoveQuery($"query {i - 1}");
                    break;
                case 3:
                    _ = _sut.GetRecentQueries(5);
                    break;
            }
        });

        act.Should().NotThrow();
    }
}
