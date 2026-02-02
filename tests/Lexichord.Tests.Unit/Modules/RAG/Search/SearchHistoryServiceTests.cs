// =============================================================================
// File: SearchHistoryServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the SearchHistoryService.
// =============================================================================

using FluentAssertions;
using Lexichord.Modules.RAG.Search;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Search;

/// <summary>
/// Unit tests for <see cref="SearchHistoryService"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6a")]
public class SearchHistoryServiceTests
{
    private readonly Mock<ILogger<SearchHistoryService>> _loggerMock;
    private readonly SearchHistoryService _sut;

    public SearchHistoryServiceTests()
    {
        _loggerMock = new Mock<ILogger<SearchHistoryService>>();
        _sut = new SearchHistoryService(_loggerMock.Object);
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
}
