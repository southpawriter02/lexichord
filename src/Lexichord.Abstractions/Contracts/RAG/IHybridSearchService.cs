// <copyright file="IHybridSearchService.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Service for hybrid search combining BM25 keyword matching and semantic vector similarity.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IHybridSearchService"/> abstracts the hybrid search pipeline, which
/// executes both BM25 (keyword) and semantic (vector) searches in parallel and merges
/// their results using <b>Reciprocal Rank Fusion (RRF)</b>. This produces a single
/// ranked list that captures both exact keyword matches and conceptual similarity.
/// </para>
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
///   <item><description>Parallel execution of BM25 and semantic search for optimal latency.</description></item>
///   <item><description>Reciprocal Rank Fusion (RRF) for merging heterogeneous ranked lists.</description></item>
///   <item><description>Configurable weights for semantic vs. keyword relevance.</description></item>
///   <item><description>Expanded sub-search retrieval (2× TopK) for better fusion quality.</description></item>
///   <item><description>Telemetry event publishing for analytics and monitoring.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b> Hybrid search requires <b>WriterPro</b> tier or higher.
/// Implementations must check the user's license tier and throw
/// <see cref="FeatureNotLicensedException"/> for unauthorized access.
/// </para>
/// <para>
/// <b>Search Flow:</b>
/// <list type="number">
///   <item><description>Validate inputs (query text, search options).</description></item>
///   <item><description>Validate license tier (WriterPro+).</description></item>
///   <item><description>Preprocess query (normalize whitespace).</description></item>
///   <item><description>Execute BM25 and semantic searches in parallel via <c>Task.WhenAll</c>.</description></item>
///   <item><description>Apply Reciprocal Rank Fusion to merge ranked result lists.</description></item>
///   <item><description>Return top K fused results as a <see cref="SearchResult"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>RRF Formula:</b>
/// <code>
/// RRF_score(chunk) = Σ (weight_i / (k + rank_i))
/// </code>
/// Where <c>weight_i</c> is the search type weight, <c>k</c> is the RRF constant
/// (default 60), and <c>rank_i</c> is the 1-based position in that ranking.
/// Chunks appearing in both result sets receive contributions from both rankings,
/// naturally boosting items that are both keyword-relevant and semantically similar.
/// </para>
/// <para>
/// <b>Hybrid vs. Individual Search:</b>
/// <list type="bullet">
///   <item><description>BM25 excels at exact keyword matches and technical terms.</description></item>
///   <item><description>Semantic search handles conceptual similarity and synonyms.</description></item>
///   <item><description>Hybrid search combines both for comprehensive retrieval quality.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Dependencies:</b>
/// <list type="bullet">
///   <item><description><see cref="ISemanticSearchService"/> (v0.4.5a): Vector similarity search.</description></item>
///   <item><description><see cref="IBM25SearchService"/> (v0.5.1b): Full-text keyword search.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.1c as part of the Hybrid Engine feature set.
/// </para>
/// </remarks>
public interface IHybridSearchService
{
    /// <summary>
    /// Executes a hybrid search combining BM25 keyword matching and semantic vector similarity.
    /// </summary>
    /// <param name="query">
    /// Search query text. Cannot be null, empty, or whitespace.
    /// The query is preprocessed (normalized) before being passed to both
    /// the BM25 and semantic search sub-services.
    /// </param>
    /// <param name="options">
    /// Search configuration options controlling result count, score threshold,
    /// and document filtering. Use <see cref="SearchOptions.Default"/> for
    /// sensible defaults. Both sub-searches use an expanded TopK (2× the
    /// requested TopK) to improve fusion quality, then the fused results
    /// are trimmed to the original TopK.
    /// </param>
    /// <param name="ct">Cancellation token for operation control.</param>
    /// <returns>
    /// A task that completes with a <see cref="SearchResult"/> containing
    /// fused <see cref="SearchHit"/> matches ranked by RRF score (highest first).
    /// Returns an empty result (via <see cref="SearchResult.Empty"/>) if neither
    /// sub-search produces matches meeting the score threshold.
    /// </returns>
    /// <remarks>
    /// <para>LOGIC: Primary entry point for the hybrid search pipeline.</para>
    /// <para>
    /// <b>Behavior:</b>
    /// <list type="bullet">
    ///   <item><description>Returns results ranked by descending RRF fused score.</description></item>
    ///   <item><description>Chunks appearing in both BM25 and semantic results rank higher.</description></item>
    ///   <item><description>Limits results to <see cref="SearchOptions.TopK"/> entries.</description></item>
    ///   <item><description>Applies <see cref="SearchOptions.DocumentFilter"/> if set (via sub-searches).</description></item>
    ///   <item><description>Publishes <c>HybridSearchExecutedEvent</c> on successful completion.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Performance:</b> Target latency is &lt; 300ms for a corpus of 50,000 chunks.
    /// Parallel execution of sub-searches ensures the total latency is bounded by
    /// the slowest individual search, not their sum.
    /// </para>
    /// </remarks>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the user's license tier is below WriterPro.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="query"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="SearchOptions.TopK"/> or <see cref="SearchOptions.MinScore"/>
    /// is outside the valid range.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="ct"/> is cancelled.
    /// </exception>
    Task<SearchResult> SearchAsync(
        string query,
        SearchOptions options,
        CancellationToken ct = default);
}
