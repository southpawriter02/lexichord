// =============================================================================
// File: QueryHistoryTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for QueryHistory records (v0.5.4d).
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Tests.Unit.Abstractions.RAG;

/// <summary>
/// Unit tests for <see cref="QueryHistoryEntry"/>, <see cref="ZeroResultQuery"/>,
/// and related types.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.4d")]
public class QueryHistoryTests
{
    #region QueryHistoryEntry Record Tests

    [Fact]
    public void QueryHistoryEntry_PropertiesAreSet()
    {
        // Arrange
        var id = Guid.NewGuid();
        var executedAt = DateTime.UtcNow;

        // Act
        var entry = new QueryHistoryEntry(
            Id: id,
            Query: "test query",
            Intent: QueryIntent.Procedural,
            ResultCount: 12,
            TopResultScore: 0.92f,
            ExecutedAt: executedAt,
            DurationMs: 145);

        // Assert
        entry.Id.Should().Be(id);
        entry.Query.Should().Be("test query");
        entry.Intent.Should().Be(QueryIntent.Procedural);
        entry.ResultCount.Should().Be(12);
        entry.TopResultScore.Should().Be(0.92f);
        entry.ExecutedAt.Should().Be(executedAt);
        entry.DurationMs.Should().Be(145);
    }

    [Theory]
    [InlineData(0, false, true)]
    [InlineData(1, true, false)]
    [InlineData(10, true, false)]
    public void QueryHistoryEntry_HasResults_And_IsZeroResult(int resultCount, bool hasResults, bool isZeroResult)
    {
        // Arrange
        var entry = new QueryHistoryEntry(
            Guid.NewGuid(),
            "test",
            QueryIntent.Factual,
            resultCount,
            resultCount > 0 ? 0.8f : null,
            DateTime.UtcNow,
            100);

        // Assert
        entry.HasResults.Should().Be(hasResults);
        entry.IsZeroResult.Should().Be(isZeroResult);
    }

    [Theory]
    [InlineData(50, "50ms")]
    [InlineData(999, "999ms")]
    [InlineData(1000, "1.0s")]
    [InlineData(1500, "1.5s")]
    [InlineData(12345, "12.3s")]
    public void QueryHistoryEntry_DurationDisplay_FormatsCorrectly(long durationMs, string expected)
    {
        // Arrange
        var entry = new QueryHistoryEntry(
            Guid.NewGuid(),
            "test",
            QueryIntent.Factual,
            5,
            0.8f,
            DateTime.UtcNow,
            durationMs);

        // Assert
        entry.DurationDisplay.Should().Be(expected);
    }

    [Fact]
    public void QueryHistoryEntry_RelativeTime_JustNow()
    {
        // Arrange
        var entry = new QueryHistoryEntry(
            Guid.NewGuid(),
            "test",
            QueryIntent.Factual,
            5,
            0.8f,
            DateTime.UtcNow.AddSeconds(-30),
            100);

        // Assert
        entry.RelativeTime.Should().Be("just now");
    }

    [Fact]
    public void QueryHistoryEntry_RelativeTime_MinutesAgo()
    {
        // Arrange
        var entry = new QueryHistoryEntry(
            Guid.NewGuid(),
            "test",
            QueryIntent.Factual,
            5,
            0.8f,
            DateTime.UtcNow.AddMinutes(-5),
            100);

        // Assert
        entry.RelativeTime.Should().Be("5 min ago");
    }

    [Fact]
    public void QueryHistoryEntry_RelativeTime_HoursAgo()
    {
        // Arrange
        var entry = new QueryHistoryEntry(
            Guid.NewGuid(),
            "test",
            QueryIntent.Factual,
            5,
            0.8f,
            DateTime.UtcNow.AddHours(-3),
            100);

        // Assert
        entry.RelativeTime.Should().Be("3 hours ago");
    }

    [Fact]
    public void QueryHistoryEntry_RelativeTime_OneHourAgo()
    {
        // Arrange
        var entry = new QueryHistoryEntry(
            Guid.NewGuid(),
            "test",
            QueryIntent.Factual,
            5,
            0.8f,
            DateTime.UtcNow.AddHours(-1),
            100);

        // Assert
        entry.RelativeTime.Should().Be("1 hour ago");
    }

    [Fact]
    public void QueryHistoryEntry_RelativeTime_DaysAgo()
    {
        // Arrange
        var entry = new QueryHistoryEntry(
            Guid.NewGuid(),
            "test",
            QueryIntent.Factual,
            5,
            0.8f,
            DateTime.UtcNow.AddDays(-2),
            100);

        // Assert
        entry.RelativeTime.Should().Be("2 days ago");
    }

    [Fact]
    public void QueryHistoryEntry_RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var time = DateTime.UtcNow;

        var e1 = new QueryHistoryEntry(id, "test", QueryIntent.Factual, 5, 0.8f, time, 100);
        var e2 = new QueryHistoryEntry(id, "test", QueryIntent.Factual, 5, 0.8f, time, 100);

        // Assert
        e1.Should().Be(e2);
        e1.GetHashCode().Should().Be(e2.GetHashCode());
    }

    [Fact]
    public void QueryHistoryEntry_WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = new QueryHistoryEntry(
            Guid.NewGuid(),
            "test",
            QueryIntent.Factual,
            5,
            0.8f,
            DateTime.UtcNow,
            100);

        // Act
        var modified = original with { ResultCount = 20 };

        // Assert
        modified.ResultCount.Should().Be(20);
        modified.Query.Should().Be("test");
        original.ResultCount.Should().Be(5);
    }

    #endregion

    #region ZeroResultQuery Record Tests

    [Fact]
    public void ZeroResultQuery_PropertiesAreSet()
    {
        // Arrange
        var lastSearched = DateTime.UtcNow;

        // Act
        var zrq = new ZeroResultQuery(
            Query: "kubernetes deployment",
            OccurrenceCount: 5,
            LastSearchedAt: lastSearched,
            SuggestedContent: null);

        // Assert
        zrq.Query.Should().Be("kubernetes deployment");
        zrq.OccurrenceCount.Should().Be(5);
        zrq.LastSearchedAt.Should().Be(lastSearched);
        zrq.SuggestedContent.Should().BeNull();
    }

    [Theory]
    [InlineData(1, "Low")]
    [InlineData(4, "Low")]
    [InlineData(5, "Medium")]
    [InlineData(9, "Medium")]
    [InlineData(10, "High")]
    [InlineData(100, "High")]
    public void ZeroResultQuery_Priority_CalculatesCorrectly(int occurrenceCount, string expectedPriority)
    {
        // Arrange
        var zrq = new ZeroResultQuery("test", occurrenceCount, DateTime.UtcNow, null);

        // Assert
        zrq.Priority.Should().Be(expectedPriority);
    }

    [Fact]
    public void ZeroResultQuery_RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var time = DateTime.UtcNow;
        var z1 = new ZeroResultQuery("test", 5, time, null);
        var z2 = new ZeroResultQuery("test", 5, time, null);

        // Assert
        z1.Should().Be(z2);
    }

    [Fact]
    public void ZeroResultQuery_WithSuggestedContent_PreservesValue()
    {
        // Arrange & Act
        var zrq = new ZeroResultQuery(
            "missing topic",
            3,
            DateTime.UtcNow,
            "Consider adding documentation about this topic.");

        // Assert
        zrq.SuggestedContent.Should().NotBeNull();
        zrq.SuggestedContent.Should().Contain("documentation");
    }

    #endregion

    #region QueryIntent Enum Tests

    [Fact]
    public void QueryIntent_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<QueryIntent>().Should().HaveCount(4);
        Enum.IsDefined(QueryIntent.Factual).Should().BeTrue();
        Enum.IsDefined(QueryIntent.Procedural).Should().BeTrue();
        Enum.IsDefined(QueryIntent.Conceptual).Should().BeTrue();
        Enum.IsDefined(QueryIntent.Navigational).Should().BeTrue();
    }

    #endregion
}
