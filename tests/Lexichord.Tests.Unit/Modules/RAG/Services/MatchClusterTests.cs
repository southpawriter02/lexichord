// =============================================================================
// File: MatchClusterTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for MatchCluster record (v0.5.6d).
// =============================================================================
// VERSION: v0.5.6d (Multi-Snippet Results)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Services;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="MatchCluster"/> record.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6d")]
public class MatchClusterTests
{
    [Fact]
    public void CenterPosition_CalculatesCorrectly()
    {
        var cluster = new MatchCluster(
            Array.Empty<MatchInfo>(),
            StartPosition: 100,
            EndPosition: 200);

        cluster.CenterPosition.Should().Be(150);
    }

    [Fact]
    public void CenterPosition_OddSpan_TruncatesToInteger()
    {
        var cluster = new MatchCluster(
            Array.Empty<MatchInfo>(),
            StartPosition: 100,
            EndPosition: 201);

        cluster.CenterPosition.Should().Be(150); // (100 + 101/2) = 150
    }

    [Fact]
    public void TotalWeight_SumsMatchWeights()
    {
        var matches = new List<MatchInfo>
        {
            new(0, 5, HighlightType.QueryMatch),  // Weight 2.0
            new(10, 5, HighlightType.FuzzyMatch)  // Weight 1.0
        };
        var cluster = new MatchCluster(matches, 0, 20);

        cluster.TotalWeight.Should().Be(3.0);
    }

    [Fact]
    public void TotalWeight_EmptyMatches_ReturnsZero()
    {
        var cluster = new MatchCluster(
            Array.Empty<MatchInfo>(),
            StartPosition: 0,
            EndPosition: 100);

        cluster.TotalWeight.Should().Be(0);
    }

    [Fact]
    public void TotalWeight_MultipleQueryMatches_SumsCorrectly()
    {
        var matches = new List<MatchInfo>
        {
            new(0, 5, HighlightType.QueryMatch),   // Weight 2.0
            new(10, 5, HighlightType.QueryMatch),  // Weight 2.0
            new(20, 5, HighlightType.QueryMatch)   // Weight 2.0
        };
        var cluster = new MatchCluster(matches, 0, 30);

        cluster.TotalWeight.Should().Be(6.0);
    }

    [Fact]
    public void TotalWeight_MixedTypes_SumsWeightsCorrectly()
    {
        var matches = new List<MatchInfo>
        {
            new(0, 5, HighlightType.QueryMatch),   // Weight 2.0
            new(10, 5, HighlightType.FuzzyMatch),  // Weight 1.0
            new(20, 5, HighlightType.KeyPhrase),   // Weight 0.5
            new(30, 5, HighlightType.Entity)       // Weight 0.5
        };
        var cluster = new MatchCluster(matches, 0, 40);

        cluster.TotalWeight.Should().Be(4.0);
    }

    [Fact]
    public void Span_CalculatesCorrectly()
    {
        var cluster = new MatchCluster(
            Array.Empty<MatchInfo>(),
            StartPosition: 50,
            EndPosition: 150);

        cluster.Span.Should().Be(100);
    }

    [Fact]
    public void Span_ZeroSpan_ReturnsZero()
    {
        var cluster = new MatchCluster(
            Array.Empty<MatchInfo>(),
            StartPosition: 100,
            EndPosition: 100);

        cluster.Span.Should().Be(0);
    }

    [Fact]
    public void Matches_CanBeAccessed()
    {
        var matches = new List<MatchInfo>
        {
            new(0, 5, HighlightType.QueryMatch),
            new(10, 5, HighlightType.FuzzyMatch)
        };
        var cluster = new MatchCluster(matches, 0, 20);

        cluster.Matches.Should().HaveCount(2);
        cluster.Matches[0].Position.Should().Be(0);
        cluster.Matches[1].Position.Should().Be(10);
    }
}
