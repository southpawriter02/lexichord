// =============================================================================
// File: MatchClusteringService.cs
// Project: Lexichord.Modules.RAG
// Description: Static service for clustering matches and deduplicating snippets.
// =============================================================================
// LOGIC: Provides algorithms for multi-snippet extraction.
//   - ClusterMatches groups nearby matches by proximity (threshold: 100 chars).
//   - DeduplicateSnippets removes overlapping snippets (threshold: 50% overlap).
//   - MatchCluster record calculates density metrics for ranking.
// =============================================================================
// VERSION: v0.5.6d (Multi-Snippet Results)
// DEPENDENCIES:
//   - MatchDensityCalculator (v0.5.6c)
//   - Snippet (v0.5.6a)
//   - HighlightType (v0.5.6a)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Information about a single match in content.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MatchInfo"/> records track the position, length, and type of
/// keyword matches found during snippet extraction. They are used by
/// <see cref="MatchClusteringService"/> to group nearby matches into clusters.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6d as part of Multi-Snippet Results.
/// </para>
/// </remarks>
/// <param name="Position">Zero-based character position of the match.</param>
/// <param name="Length">Length of the matched text in characters.</param>
/// <param name="Type">The type of match (exact, fuzzy, etc.).</param>
internal record MatchInfo(int Position, int Length, HighlightType Type);

/// <summary>
/// A cluster of nearby matches for multi-snippet extraction.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MatchCluster"/> groups matches that are close together in the
/// source content. Each cluster represents a candidate region for snippet
/// extraction, ranked by total match weight (density).
/// </para>
/// <para>
/// <b>Density Scoring:</b> Clusters are ranked by <see cref="TotalWeight"/>,
/// which uses weights from <see cref="MatchDensityCalculator.GetMatchWeight"/>.
/// Higher-weight clusters are extracted first.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6d as part of Multi-Snippet Results.
/// </para>
/// </remarks>
/// <param name="Matches">The matches contained in this cluster.</param>
/// <param name="StartPosition">Start position of the cluster region.</param>
/// <param name="EndPosition">End position of the cluster region (exclusive).</param>
internal record MatchCluster(
    IReadOnlyList<MatchInfo> Matches,
    int StartPosition,
    int EndPosition)
{
    /// <summary>
    /// Gets the center position of this cluster.
    /// </summary>
    /// <value>
    /// The midpoint between <see cref="StartPosition"/> and <see cref="EndPosition"/>.
    /// </value>
    public int CenterPosition => StartPosition + (EndPosition - StartPosition) / 2;

    /// <summary>
    /// Gets the total weight of matches in this cluster.
    /// </summary>
    /// <value>
    /// The sum of weights for all matches, using <see cref="MatchDensityCalculator.GetMatchWeight"/>.
    /// </value>
    /// <remarks>
    /// Used for ranking clusters by density. Higher weights indicate more
    /// relevant regions for snippet extraction.
    /// </remarks>
    public double TotalWeight => Matches.Sum(m =>
        MatchDensityCalculator.GetMatchWeight(m.Type));

    /// <summary>
    /// Gets the span of this cluster in characters.
    /// </summary>
    /// <value>
    /// The difference between <see cref="EndPosition"/> and <see cref="StartPosition"/>.
    /// </value>
    public int Span => EndPosition - StartPosition;
}

/// <summary>
/// Static service for clustering matches and deduplicating snippets.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MatchClusteringService"/> provides algorithms for multi-snippet
/// extraction. It groups nearby matches into clusters and removes overlapping
/// snippets to produce a clean set of non-redundant previews.
/// </para>
/// <para>
/// <b>Clustering Algorithm:</b> Matches are grouped by proximity using a
/// configurable threshold (default: 100 characters). If the gap between
/// consecutive matches exceeds the threshold, a new cluster is started.
/// </para>
/// <para>
/// <b>Deduplication:</b> Snippets with significant text overlap (default: 50%)
/// are considered duplicates. The first snippet in the list is always kept,
/// and subsequent overlapping snippets are removed.
/// </para>
/// <para>
/// <b>Thread Safety:</b> All methods are static and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6d as part of Multi-Snippet Results.
/// </para>
/// </remarks>
public static class MatchClusteringService
{
    /// <summary>
    /// Default threshold for clustering matches (in characters).
    /// </summary>
    public const int DefaultClusterThreshold = 100;

    /// <summary>
    /// Default threshold for snippet overlap deduplication.
    /// </summary>
    public const double DefaultOverlapThreshold = 0.5;

    /// <summary>
    /// Clusters matches by proximity.
    /// </summary>
    /// <param name="matches">Sorted list of matches (by position).</param>
    /// <param name="threshold">Maximum gap between matches in the same cluster.</param>
    /// <returns>
    /// A list of <see cref="MatchCluster"/> instances, each containing matches
    /// that are within <paramref name="threshold"/> characters of each other.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The algorithm processes matches sequentially, extending the current
    /// cluster as long as matches are within the threshold. When a gap exceeds
    /// the threshold, the current cluster is finalized and a new one starts.
    /// </para>
    /// <para>
    /// <b>Input Requirement:</b> The <paramref name="matches"/> list must be
    /// sorted by position (ascending). Unsorted input produces incorrect clusters.
    /// </para>
    /// </remarks>
    internal static IReadOnlyList<MatchCluster> ClusterMatches(
        IReadOnlyList<MatchInfo> matches,
        int threshold = DefaultClusterThreshold)
    {
        if (matches.Count == 0)
        {
            return Array.Empty<MatchCluster>();
        }

        if (matches.Count == 1)
        {
            return new[]
            {
                new MatchCluster(
                    matches,
                    matches[0].Position,
                    matches[0].Position + matches[0].Length)
            };
        }

        var clusters = new List<MatchCluster>();
        var currentMatches = new List<MatchInfo> { matches[0] };
        var clusterStart = matches[0].Position;
        var clusterEnd = matches[0].Position + matches[0].Length;

        for (var i = 1; i < matches.Count; i++)
        {
            var match = matches[i];
            var matchStart = match.Position;

            // LOGIC: Check if match is within threshold of current cluster end.
            if (matchStart - clusterEnd <= threshold)
            {
                // Extend current cluster.
                currentMatches.Add(match);
                clusterEnd = Math.Max(clusterEnd, matchStart + match.Length);
            }
            else
            {
                // Finalize current cluster and start a new one.
                clusters.Add(new MatchCluster(
                    currentMatches.ToList(),
                    clusterStart,
                    clusterEnd));

                currentMatches = new List<MatchInfo> { match };
                clusterStart = matchStart;
                clusterEnd = matchStart + match.Length;
            }
        }

        // Finalize last cluster.
        clusters.Add(new MatchCluster(
            currentMatches.ToList(),
            clusterStart,
            clusterEnd));

        return clusters;
    }

    /// <summary>
    /// Removes duplicate or overlapping snippets.
    /// </summary>
    /// <param name="snippets">Candidate snippets to deduplicate.</param>
    /// <param name="overlapThreshold">
    /// Minimum overlap ratio (0.0–1.0) to consider a snippet as duplicate.
    /// Default is 0.5 (50% overlap).
    /// </param>
    /// <returns>
    /// A deduplicated list of snippets with overlapping entries removed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The algorithm iterates through snippets, comparing each candidate against
    /// already-accepted snippets. If the overlap ratio exceeds the threshold,
    /// the candidate is considered a duplicate and skipped.
    /// </para>
    /// <para>
    /// <b>Overlap Calculation:</b> Overlap is computed as the ratio of the
    /// overlapping region length to the minimum snippet length. This ensures
    /// that a small snippet fully contained within a large snippet is detected.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<Snippet> DeduplicateSnippets(
        IReadOnlyList<Snippet> snippets,
        double overlapThreshold = DefaultOverlapThreshold)
    {
        if (snippets.Count <= 1)
        {
            return snippets;
        }

        var result = new List<Snippet> { snippets[0] };

        for (var i = 1; i < snippets.Count; i++)
        {
            var candidate = snippets[i];
            var isDuplicate = false;

            foreach (var existing in result)
            {
                var overlap = CalculateOverlap(existing, candidate);
                if (overlap >= overlapThreshold)
                {
                    isDuplicate = true;
                    break;
                }
            }

            if (!isDuplicate)
            {
                result.Add(candidate);
            }
        }

        return result;
    }

    /// <summary>
    /// Calculates the overlap ratio between two snippets.
    /// </summary>
    /// <param name="a">First snippet.</param>
    /// <param name="b">Second snippet.</param>
    /// <returns>
    /// The overlap ratio as a value between 0.0 and 1.0, where 1.0 means
    /// complete overlap and 0.0 means no overlap.
    /// </returns>
    private static double CalculateOverlap(Snippet a, Snippet b)
    {
        var aStart = a.StartOffset;
        var aEnd = a.StartOffset + a.Text.Length;
        var bStart = b.StartOffset;
        var bEnd = b.StartOffset + b.Text.Length;

        var overlapStart = Math.Max(aStart, bStart);
        var overlapEnd = Math.Min(aEnd, bEnd);

        if (overlapStart >= overlapEnd)
        {
            return 0;
        }

        var overlapLength = overlapEnd - overlapStart;
        var minLength = Math.Min(a.Text.Length, b.Text.Length);

        return (double)overlapLength / minLength;
    }

    /// <summary>
    /// Logs clustering statistics for debugging.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="matchCount">Number of matches processed.</param>
    /// <param name="clusterCount">Number of clusters created.</param>
    internal static void LogClusteringResult(
        ILogger logger,
        int matchCount,
        int clusterCount)
    {
        logger.LogDebug(
            "Clustered {MatchCount} matches into {ClusterCount} clusters",
            matchCount,
            clusterCount);
    }
}
