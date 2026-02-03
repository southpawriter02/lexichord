// =============================================================================
// File: QuerySuggestionTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for QuerySuggestion records (v0.5.4c).
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Tests.Unit.Abstractions.RAG;

/// <summary>
/// Unit tests for <see cref="QuerySuggestion"/> and related types.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.4c")]
public class QuerySuggestionTests
{
    #region QuerySuggestion Record Tests

    [Fact]
    public void QuerySuggestion_PropertiesAreSet()
    {
        // Arrange & Act
        var suggestion = new QuerySuggestion(
            Text: "token refresh",
            Frequency: 5,
            Source: SuggestionSource.QueryHistory,
            Score: 0.85f);

        // Assert
        suggestion.Text.Should().Be("token refresh");
        suggestion.Frequency.Should().Be(5);
        suggestion.Source.Should().Be(SuggestionSource.QueryHistory);
        suggestion.Score.Should().Be(0.85f);
    }

    [Theory]
    [InlineData(0.87f, "87%")]
    [InlineData(0.5f, "50%")]
    [InlineData(1.0f, "100%")]
    [InlineData(0.0f, "0%")]
    [InlineData(0.123f, "12%")]
    public void QuerySuggestion_ScorePercent_FormatsCorrectly(float score, string expected)
    {
        // Arrange
        var suggestion = new QuerySuggestion("test", 1, SuggestionSource.ContentNgram, score);

        // Assert
        suggestion.ScorePercent.Should().Be(expected);
    }

    [Theory]
    [InlineData(SuggestionSource.QueryHistory, "Recent Query")]
    [InlineData(SuggestionSource.DocumentHeading, "Document Heading")]
    [InlineData(SuggestionSource.ContentNgram, "Suggestion")]
    [InlineData(SuggestionSource.DomainTerm, "Domain Term")]
    public void QuerySuggestion_SourceLabel_ReturnsCorrectLabel(SuggestionSource source, string expectedLabel)
    {
        // Arrange
        var suggestion = new QuerySuggestion("test", 1, source, 0.5f);

        // Assert
        suggestion.SourceLabel.Should().Be(expectedLabel);
    }

    [Theory]
    [InlineData(SuggestionSource.QueryHistory, "🕐")]
    [InlineData(SuggestionSource.DocumentHeading, "📄")]
    [InlineData(SuggestionSource.ContentNgram, "💡")]
    [InlineData(SuggestionSource.DomainTerm, "📚")]
    public void QuerySuggestion_SourceIcon_ReturnsCorrectIcon(SuggestionSource source, string expectedIcon)
    {
        // Arrange
        var suggestion = new QuerySuggestion("test", 1, source, 0.5f);

        // Assert
        suggestion.SourceIcon.Should().Be(expectedIcon);
    }

    [Fact]
    public void QuerySuggestion_RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var s1 = new QuerySuggestion("test", 5, SuggestionSource.QueryHistory, 0.8f);
        var s2 = new QuerySuggestion("test", 5, SuggestionSource.QueryHistory, 0.8f);

        // Assert
        s1.Should().Be(s2);
        s1.GetHashCode().Should().Be(s2.GetHashCode());
    }

    [Fact]
    public void QuerySuggestion_RecordEquality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var s1 = new QuerySuggestion("test1", 5, SuggestionSource.QueryHistory, 0.8f);
        var s2 = new QuerySuggestion("test2", 5, SuggestionSource.QueryHistory, 0.8f);

        // Assert
        s1.Should().NotBe(s2);
    }

    [Fact]
    public void QuerySuggestion_WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = new QuerySuggestion("test", 5, SuggestionSource.QueryHistory, 0.8f);

        // Act
        var modified = original with { Frequency = 10 };

        // Assert
        modified.Frequency.Should().Be(10);
        modified.Text.Should().Be("test");
        original.Frequency.Should().Be(5);
    }

    #endregion

    #region SuggestionSource Enum Tests

    [Fact]
    public void SuggestionSource_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<SuggestionSource>().Should().HaveCount(4);
        Enum.IsDefined(SuggestionSource.QueryHistory).Should().BeTrue();
        Enum.IsDefined(SuggestionSource.DocumentHeading).Should().BeTrue();
        Enum.IsDefined(SuggestionSource.ContentNgram).Should().BeTrue();
        Enum.IsDefined(SuggestionSource.DomainTerm).Should().BeTrue();
    }

    #endregion
}
