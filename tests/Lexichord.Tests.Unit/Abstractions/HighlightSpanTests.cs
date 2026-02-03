// =============================================================================
// File: HighlightSpanTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for HighlightSpan record (v0.5.6a).
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions;

/// <summary>
/// Unit tests for <see cref="HighlightSpan"/> record.
/// Verifies computed properties, overlap detection, and merge logic.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6a")]
public class HighlightSpanTests
{
    #region End Property Tests

    [Fact]
    public void End_EqualsStartPlusLength()
    {
        // Arrange
        var span = new HighlightSpan(5, 10, HighlightType.QueryMatch);

        // Assert
        span.End.Should().Be(15);
    }

    [Fact]
    public void End_ZeroLengthSpan_EqualsStart()
    {
        // Arrange
        var span = new HighlightSpan(5, 0, HighlightType.QueryMatch);

        // Assert
        span.End.Should().Be(5);
    }

    #endregion

    #region Overlaps Tests

    [Fact]
    public void Overlaps_CompleteOverlap_ReturnsTrue()
    {
        // Arrange
        var span1 = new HighlightSpan(0, 10, HighlightType.QueryMatch);
        var span2 = new HighlightSpan(3, 4, HighlightType.FuzzyMatch);

        // Assert
        span1.Overlaps(span2).Should().BeTrue();
        span2.Overlaps(span1).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_PartialOverlap_ReturnsTrue()
    {
        // Arrange
        var span1 = new HighlightSpan(0, 10, HighlightType.QueryMatch);
        var span2 = new HighlightSpan(5, 10, HighlightType.FuzzyMatch);

        // Assert
        span1.Overlaps(span2).Should().BeTrue();
        span2.Overlaps(span1).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_NoOverlap_ReturnsFalse()
    {
        // Arrange
        var span1 = new HighlightSpan(0, 5, HighlightType.QueryMatch);
        var span2 = new HighlightSpan(10, 5, HighlightType.FuzzyMatch);

        // Assert
        span1.Overlaps(span2).Should().BeFalse();
        span2.Overlaps(span1).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_Adjacent_ReturnsFalse()
    {
        // Arrange - spans touch but don't overlap
        var span1 = new HighlightSpan(0, 5, HighlightType.QueryMatch);
        var span2 = new HighlightSpan(5, 5, HighlightType.FuzzyMatch);

        // Assert
        span1.Overlaps(span2).Should().BeFalse();
        span2.Overlaps(span1).Should().BeFalse();
    }

    [Fact]
    public void Overlaps_IdenticalSpans_ReturnsTrue()
    {
        // Arrange
        var span1 = new HighlightSpan(5, 10, HighlightType.QueryMatch);
        var span2 = new HighlightSpan(5, 10, HighlightType.FuzzyMatch);

        // Assert
        span1.Overlaps(span2).Should().BeTrue();
    }

    #endregion

    #region Merge Tests

    [Fact]
    public void Merge_CombinesOverlappingSpans()
    {
        // Arrange
        var span1 = new HighlightSpan(0, 10, HighlightType.FuzzyMatch);
        var span2 = new HighlightSpan(5, 10, HighlightType.QueryMatch);

        // Act
        var merged = span1.Merge(span2);

        // Assert
        merged.Start.Should().Be(0);
        merged.Length.Should().Be(15);
        merged.End.Should().Be(15);
    }

    [Fact]
    public void Merge_PrefersMoreSpecificType_QueryMatchOverFuzzy()
    {
        // Arrange
        var span1 = new HighlightSpan(0, 10, HighlightType.FuzzyMatch);
        var span2 = new HighlightSpan(5, 10, HighlightType.QueryMatch);

        // Act
        var merged = span1.Merge(span2);

        // Assert
        merged.Type.Should().Be(HighlightType.QueryMatch);
    }

    [Fact]
    public void Merge_PrefersMoreSpecificType_QueryMatchWhenFirst()
    {
        // Arrange
        var span1 = new HighlightSpan(0, 10, HighlightType.QueryMatch);
        var span2 = new HighlightSpan(5, 10, HighlightType.FuzzyMatch);

        // Act
        var merged = span1.Merge(span2);

        // Assert
        merged.Type.Should().Be(HighlightType.QueryMatch);
    }

    [Fact]
    public void Merge_CompletelyContainedSpan()
    {
        // Arrange
        var outer = new HighlightSpan(0, 20, HighlightType.FuzzyMatch);
        var inner = new HighlightSpan(5, 5, HighlightType.QueryMatch);

        // Act
        var merged = outer.Merge(inner);

        // Assert
        merged.Start.Should().Be(0);
        merged.Length.Should().Be(20);
        merged.Type.Should().Be(HighlightType.QueryMatch);
    }

    [Fact]
    public void Merge_AdjacentSpans()
    {
        // Arrange
        var span1 = new HighlightSpan(0, 5, HighlightType.QueryMatch);
        var span2 = new HighlightSpan(5, 5, HighlightType.FuzzyMatch);

        // Act
        var merged = span1.Merge(span2);

        // Assert
        merged.Start.Should().Be(0);
        merged.Length.Should().Be(10);
    }

    [Fact]
    public void Merge_IdenticalSpans()
    {
        // Arrange
        var span1 = new HighlightSpan(5, 10, HighlightType.QueryMatch);
        var span2 = new HighlightSpan(5, 10, HighlightType.QueryMatch);

        // Act
        var merged = span1.Merge(span2);

        // Assert
        merged.Start.Should().Be(5);
        merged.Length.Should().Be(10);
        merged.Type.Should().Be(HighlightType.QueryMatch);
    }

    #endregion

    #region Type Priority Tests

    [Fact]
    public void Merge_TypePriority_QueryMatchWinsOverAll()
    {
        // Arrange
        var queryMatch = new HighlightSpan(0, 10, HighlightType.QueryMatch);
        var entity = new HighlightSpan(0, 10, HighlightType.Entity);

        // Act
        var merged = entity.Merge(queryMatch);

        // Assert
        merged.Type.Should().Be(HighlightType.QueryMatch);
    }

    [Fact]
    public void Merge_TypePriority_FuzzyMatchWinsOverKeyPhrase()
    {
        // Arrange
        var fuzzy = new HighlightSpan(0, 10, HighlightType.FuzzyMatch);
        var keyPhrase = new HighlightSpan(0, 10, HighlightType.KeyPhrase);

        // Act
        var merged = keyPhrase.Merge(fuzzy);

        // Assert
        merged.Type.Should().Be(HighlightType.FuzzyMatch);
    }

    #endregion
}
