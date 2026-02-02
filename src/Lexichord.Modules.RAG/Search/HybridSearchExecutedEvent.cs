// =============================================================================
// File: HybridSearchExecutedEvent.cs
// Project: Lexichord.Modules.RAG
// Description: Telemetry event published after hybrid search execution.
// =============================================================================
// LOGIC: MediatR notification for tracking hybrid search usage and performance.
//   Published by HybridSearchService after successful search completion.
//   Captures details from both sub-searches and the RRF fusion process.
//
//   Properties:
//     - Query: Original query text before preprocessing.
//     - SemanticHitCount: Number of results from semantic (vector) search.
//     - BM25HitCount: Number of results from BM25 (keyword) search.
//     - FusedResultCount: Number of results after RRF fusion and TopK trim.
//     - Duration: Total elapsed time for the hybrid search pipeline.
//     - SemanticWeight: Semantic weight used in the RRF formula.
//     - BM25Weight: BM25 weight used in the RRF formula.
//     - RRFConstant: The k constant used in the RRF formula.
//     - Timestamp: UTC timestamp when the search completed.
//
//   Dependencies:
//     - v0.5.1c: HybridSearchService (publisher)
// =============================================================================

using MediatR;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Event published when a hybrid search completes successfully.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="HybridSearchExecutedEvent"/> provides telemetry data for hybrid search
/// operations, including individual sub-search hit counts, fused result count,
/// execution duration, and the RRF configuration used. This event is published via
/// <see cref="IMediator"/> for consumption by analytics, logging, or monitoring
/// subscribers.
/// </para>
/// <para>
/// <b>Use Cases:</b>
/// <list type="bullet">
///   <item><description>Search analytics and usage tracking.</description></item>
///   <item><description>Performance monitoring and alerting (target: &lt; 300ms).</description></item>
///   <item><description>RRF weight tuning analysis (semantic vs. BM25 contribution).</description></item>
///   <item><description>Debug logging for hybrid search pipeline troubleshooting.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.1c as part of the Hybrid Fusion Algorithm.
/// </para>
/// </remarks>
public sealed record HybridSearchExecutedEvent : INotification
{
    /// <summary>
    /// Gets the original query text submitted by the user.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Captures the raw query before preprocessing.</para>
    /// <para>Useful for search analytics, debugging, and query pattern analysis.</para>
    /// </remarks>
    public required string Query { get; init; }

    /// <summary>
    /// Gets the number of results returned by the semantic (vector) search sub-query.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Count of <see cref="Lexichord.Abstractions.Contracts.SearchHit"/> items
    /// from <see cref="Lexichord.Abstractions.Contracts.ISemanticSearchService"/> before
    /// RRF fusion. Uses expanded TopK (2× requested) for better fusion quality.</para>
    /// <para>Zero indicates no semantic matches were found.</para>
    /// </remarks>
    public required int SemanticHitCount { get; init; }

    /// <summary>
    /// Gets the number of results returned by the BM25 (keyword) search sub-query.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Count of <see cref="Lexichord.Abstractions.Contracts.SearchHit"/> items
    /// from <see cref="Lexichord.Abstractions.Contracts.RAG.IBM25SearchService"/> before
    /// RRF fusion. Uses expanded TopK (2× requested) for better fusion quality.</para>
    /// <para>Zero indicates no keyword matches were found.</para>
    /// </remarks>
    public required int BM25HitCount { get; init; }

    /// <summary>
    /// Gets the number of results after RRF fusion and TopK trimming.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Final count after merging both result sets via Reciprocal Rank Fusion
    /// and trimming to the requested TopK limit. This count represents the actual number
    /// of hits returned to the caller.</para>
    /// <para>Will be ≤ TopK and ≤ (SemanticHitCount + BM25HitCount).</para>
    /// </remarks>
    public required int FusedResultCount { get; init; }

    /// <summary>
    /// Gets the total duration of the hybrid search operation.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Measured from input validation through result assembly, including
    /// parallel sub-search execution and RRF fusion. The target threshold is
    /// &lt; 300ms for 50K chunks.</para>
    /// <para>Includes: validation, license check, preprocessing, parallel search,
    /// RRF fusion, and result assembly.</para>
    /// </remarks>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the semantic weight used in the RRF formula for this search.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Records the actual weight applied during fusion for analytics.
    /// Useful for correlating retrieval quality with weight configuration.</para>
    /// </remarks>
    public required float SemanticWeight { get; init; }

    /// <summary>
    /// Gets the BM25 weight used in the RRF formula for this search.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Records the actual weight applied during fusion for analytics.
    /// Useful for correlating retrieval quality with weight configuration.</para>
    /// </remarks>
    public required float BM25Weight { get; init; }

    /// <summary>
    /// Gets the RRF constant <c>k</c> used in the fusion formula for this search.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Records the actual k constant applied during fusion. Higher values
    /// reduce rank impact; lower values amplify rank differences.</para>
    /// </remarks>
    public required int RRFConstant { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the search completed.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Captured at search completion for time-series analysis and
    /// correlation with other system events.</para>
    /// </remarks>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
