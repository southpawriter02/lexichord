// =============================================================================
// File: MatchDensityCalculatorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for MatchDensityCalculator (v0.5.6c).
// =============================================================================
// VERSION: v0.5.6c (Smart Truncation)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Services;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="MatchDensityCalculator"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6c")]
public class MatchDensityCalculatorTests
{
    #region FindHighestDensityPosition Tests

    [Fact]
    public void FindHighestDensityPosition_SingleMatch_ReturnsMatchPosition()
    {
        var matches = new List<(int Position, int Length, double Weight)> { (50, 10, 2.0) };

        var (position, score) = MatchDensityCalculator.FindHighestDensityPosition(
            200, matches, windowSize: 100);

        position.Should().Be(50);
        score.Should().Be(2.0);
    }

    [Fact]
    public void FindHighestDensityPosition_ClusteredMatches_ReturnsCenterOfCluster()
    {
        var matches = new List<(int Position, int Length, double Weight)>
        {
            (50, 5, 2.0),
            (60, 5, 2.0),
            (70, 5, 2.0),
            (200, 5, 2.0)  // Isolated match
        };

        var (position, score) = MatchDensityCalculator.FindHighestDensityPosition(
            300, matches, windowSize: 50);

        // Should prefer the cluster at 50-70
        position.Should().BeInRange(40, 80);
        score.Should().BeGreaterThanOrEqualTo(4.0); // At least 2 matches with weight 2.0
    }

    [Fact]
    public void FindHighestDensityPosition_NoMatches_ReturnsZero()
    {
        var matches = new List<(int Position, int Length, double Weight)>();

        var (position, score) = MatchDensityCalculator.FindHighestDensityPosition(
            200, matches);

        position.Should().Be(0);
        score.Should().Be(0);
    }

    [Fact]
    public void FindHighestDensityPosition_TwoMatches_ReturnsCorrectDensity()
    {
        var matches = new List<(int Position, int Length, double Weight)>
        {
            (10, 5, 2.0),
            (20, 5, 1.0)
        };

        var (position, score) = MatchDensityCalculator.FindHighestDensityPosition(
            100, matches, windowSize: 50);

        // Both matches should be in the same window
        score.Should().Be(3.0); // 2.0 + 1.0
    }

    [Fact]
    public void FindHighestDensityPosition_SpreadOutMatches_FindsBestWindow()
    {
        var matches = new List<(int Position, int Length, double Weight)>
        {
            (10, 5, 1.0),   // Window 0-100
            (50, 5, 2.0),   // Window 0-100
            (60, 5, 2.0),   // Window 0-100
            (150, 5, 1.0),  // Window 100-200
            (400, 5, 3.0)   // Window 350-450
        };

        var (position, score) = MatchDensityCalculator.FindHighestDensityPosition(
            500, matches, windowSize: 100, stepSize: 10);

        // Window containing positions 50, 60 should have score 5.0 (1+2+2)
        score.Should().Be(5.0);
    }

    [Fact]
    public void FindHighestDensityPosition_MatchOverlappingWindow_CountsMatch()
    {
        var matches = new List<(int Position, int Length, double Weight)>
        {
            (95, 20, 2.0)  // Overlaps with window [50-150] and [100-200]
        };

        var (position, score) = MatchDensityCalculator.FindHighestDensityPosition(
            300, matches, windowSize: 100, stepSize: 50);

        score.Should().Be(2.0);
    }

    #endregion

    #region GetMatchWeight Tests

    [Theory]
    [InlineData(HighlightType.QueryMatch, 2.0)]
    [InlineData(HighlightType.FuzzyMatch, 1.0)]
    [InlineData(HighlightType.KeyPhrase, 0.5)]
    [InlineData(HighlightType.Entity, 0.5)]
    public void GetMatchWeight_ReturnsCorrectWeights(HighlightType type, double expected)
    {
        var weight = MatchDensityCalculator.GetMatchWeight(type);
        weight.Should().Be(expected);
    }

    [Fact]
    public void GetMatchWeight_QueryMatch_HasHighestWeight()
    {
        var queryWeight = MatchDensityCalculator.GetMatchWeight(HighlightType.QueryMatch);
        var fuzzyWeight = MatchDensityCalculator.GetMatchWeight(HighlightType.FuzzyMatch);
        var keyPhraseWeight = MatchDensityCalculator.GetMatchWeight(HighlightType.KeyPhrase);

        queryWeight.Should().BeGreaterThan(fuzzyWeight);
        fuzzyWeight.Should().BeGreaterThan(keyPhraseWeight);
    }

    #endregion

    #region DefaultWindowSize and StepSize Tests

    [Fact]
    public void DefaultWindowSize_IsReasonable()
    {
        MatchDensityCalculator.DefaultWindowSize.Should().Be(100);
    }

    [Fact]
    public void DefaultStepSize_IsReasonable()
    {
        MatchDensityCalculator.DefaultStepSize.Should().Be(10);
    }

    [Fact]
    public void FindHighestDensityPosition_UsesDefaultParameters()
    {
        var matches = new List<(int Position, int Length, double Weight)> { (50, 10, 2.0) };

        // Should not throw when using default parameters
        var act = () => MatchDensityCalculator.FindHighestDensityPosition(200, matches);
        act.Should().NotThrow();
    }

    #endregion
}
