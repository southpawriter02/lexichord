// =============================================================================
// File: MatchClusteringServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for MatchClusteringService (v0.5.6d).
// =============================================================================
// VERSION: v0.5.6d (Multi-Snippet Results)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Services;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="MatchClusteringService"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6d")]
public class MatchClusteringServiceTests
{
    #region ClusterMatches Tests

    [Fact]
    public void ClusterMatches_SingleMatch_ReturnsSingleCluster()
    {
        var matches = new List<MatchInfo>
        {
            new(50, 10, HighlightType.QueryMatch)
        };

        var clusters = MatchClusteringService.ClusterMatches(matches);

        clusters.Should().ContainSingle();
        clusters[0].Matches.Should().ContainSingle();
        clusters[0].StartPosition.Should().Be(50);
        clusters[0].EndPosition.Should().Be(60);
    }

    [Fact]
    public void ClusterMatches_NearbyMatches_ClustersTogether()
    {
        var matches = new List<MatchInfo>
        {
            new(50, 10, HighlightType.QueryMatch),
            new(70, 10, HighlightType.QueryMatch),  // Within 100 chars
            new(90, 10, HighlightType.QueryMatch)   // Within 100 chars
        };

        var clusters = MatchClusteringService.ClusterMatches(matches, threshold: 100);

        clusters.Should().ContainSingle();
        clusters[0].Matches.Should().HaveCount(3);
    }

    [Fact]
    public void ClusterMatches_DistantMatches_CreatesSeparateClusters()
    {
        var matches = new List<MatchInfo>
        {
            new(50, 10, HighlightType.QueryMatch),
            new(300, 10, HighlightType.QueryMatch)  // Beyond threshold
        };

        var clusters = MatchClusteringService.ClusterMatches(matches, threshold: 100);

        clusters.Should().HaveCount(2);
        clusters[0].Matches.Should().ContainSingle();
        clusters[1].Matches.Should().ContainSingle();
    }

    [Fact]
    public void ClusterMatches_EmptyInput_ReturnsEmpty()
    {
        var clusters = MatchClusteringService.ClusterMatches(
            Array.Empty<MatchInfo>());

        clusters.Should().BeEmpty();
    }

    [Fact]
    public void ClusterMatches_ThreeClusters_SortedByPosition()
    {
        var matches = new List<MatchInfo>
        {
            new(50, 10, HighlightType.QueryMatch),
            new(200, 10, HighlightType.QueryMatch),
            new(400, 10, HighlightType.QueryMatch)
        };

        var clusters = MatchClusteringService.ClusterMatches(matches, threshold: 50);

        clusters.Should().HaveCount(3);
        clusters[0].StartPosition.Should().Be(50);
        clusters[1].StartPosition.Should().Be(200);
        clusters[2].StartPosition.Should().Be(400);
    }

    [Fact]
    public void ClusterMatches_OverlappingMatches_ExtendClusterEnd()
    {
        var matches = new List<MatchInfo>
        {
            new(50, 30, HighlightType.QueryMatch),
            new(70, 30, HighlightType.QueryMatch)  // Overlaps with first
        };

        var clusters = MatchClusteringService.ClusterMatches(matches);

        clusters.Should().ContainSingle();
        clusters[0].StartPosition.Should().Be(50);
        clusters[0].EndPosition.Should().Be(100); // max(80, 100)
    }

    #endregion

    #region DeduplicateSnippets Tests

    [Fact]
    public void DeduplicateSnippets_NoOverlap_ReturnsAll()
    {
        var snippets = new List<Snippet>
        {
            new("First snippet", Array.Empty<HighlightSpan>(), 0, false, false),
            new("Second snippet", Array.Empty<HighlightSpan>(), 100, false, false)
        };

        var result = MatchClusteringService.DeduplicateSnippets(snippets);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void DeduplicateSnippets_SignificantOverlap_RemovesDuplicate()
    {
        var snippets = new List<Snippet>
        {
            new("First snippet text here", Array.Empty<HighlightSpan>(), 0, false, false),
            new("snippet text here yes", Array.Empty<HighlightSpan>(), 6, false, false)  // ~75% overlap
        };

        var result = MatchClusteringService.DeduplicateSnippets(snippets, overlapThreshold: 0.5);

        result.Should().ContainSingle();
        result[0].Text.Should().Be("First snippet text here");
    }

    [Fact]
    public void DeduplicateSnippets_SingleSnippet_ReturnsSame()
    {
        var snippets = new List<Snippet>
        {
            new("Only snippet", Array.Empty<HighlightSpan>(), 0, false, false)
        };

        var result = MatchClusteringService.DeduplicateSnippets(snippets);

        result.Should().ContainSingle();
        result.Should().BeEquivalentTo(snippets);
    }

    [Fact]
    public void DeduplicateSnippets_EmptyList_ReturnsEmpty()
    {
        var snippets = Array.Empty<Snippet>();

        var result = MatchClusteringService.DeduplicateSnippets(snippets);

        result.Should().BeEmpty();
    }

    [Fact]
    public void DeduplicateSnippets_BelowThreshold_KeepsAll()
    {
        var snippets = new List<Snippet>
        {
            new("First snippet text", Array.Empty<HighlightSpan>(), 0, false, false),
            new("other snippet text", Array.Empty<HighlightSpan>(), 50, false, false)  // Minimal overlap
        };

        var result = MatchClusteringService.DeduplicateSnippets(snippets, overlapThreshold: 0.5);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void DeduplicateSnippets_ThreeSnippets_TwoOverlapping_RemovesOne()
    {
        var snippets = new List<Snippet>
        {
            new("First snippet here", Array.Empty<HighlightSpan>(), 0, false, false),
            new("t snippet here x", Array.Empty<HighlightSpan>(), 4, false, false),  // Overlaps with first
            new("Distant snippet", Array.Empty<HighlightSpan>(), 200, false, false)
        };

        var result = MatchClusteringService.DeduplicateSnippets(snippets, overlapThreshold: 0.5);

        result.Should().HaveCount(2);
        result[0].Text.Should().Be("First snippet here");
        result[1].Text.Should().Be("Distant snippet");
    }

    #endregion

    #region Constants Tests

    [Fact]
    public void DefaultClusterThreshold_Is100()
    {
        MatchClusteringService.DefaultClusterThreshold.Should().Be(100);
    }

    [Fact]
    public void DefaultOverlapThreshold_IsHalf()
    {
        MatchClusteringService.DefaultOverlapThreshold.Should().Be(0.5);
    }

    #endregion
}
