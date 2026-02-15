// =============================================================================
// File: IndexStatisticsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for IndexStatistics record.
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts.RAG;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Contracts.RAG;

public class IndexStatisticsTests
{
    [Fact]
    public void PendingCount_ShouldReturnZero_WhenStatusCountsIsEmpty()
    {
        // Arrange
        var stats = new IndexStatistics();

        // Act
        var pendingCount = stats.PendingCount;

        // Assert
        pendingCount.Should().Be(0);
    }

    [Fact]
    public void PendingCount_ShouldReturnCorrectCount_WhenPendingStatusExists()
    {
        // Arrange
        var statusCounts = new Dictionary<IndexingStatus, int>
        {
            { IndexingStatus.Pending, 5 },
            { IndexingStatus.Indexed, 10 }
        };

        var stats = new IndexStatistics
        {
            StatusCounts = statusCounts
        };

        // Act
        var pendingCount = stats.PendingCount;

        // Assert
        pendingCount.Should().Be(5);
    }

    [Fact]
    public void PendingCount_ShouldReturnZero_WhenPendingStatusIsMissing()
    {
        // Arrange
        var statusCounts = new Dictionary<IndexingStatus, int>
        {
            { IndexingStatus.Indexed, 10 },
            { IndexingStatus.Failed, 2 }
        };

        var stats = new IndexStatistics
        {
            StatusCounts = statusCounts
        };

        // Act
        var pendingCount = stats.PendingCount;

        // Assert
        pendingCount.Should().Be(0);
    }

    [Fact]
    public void PendingCount_ShouldReturnZero_WhenStatusCountsIsNull()
    {
        // Arrange
        var stats = new IndexStatistics
        {
            StatusCounts = null!
        };

        // Act
        var pendingCount = stats.PendingCount;

        // Assert
        pendingCount.Should().Be(0);
    }
}
