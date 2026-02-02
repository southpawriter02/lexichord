// <copyright file="IBM25SearchService.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Service for BM25 keyword search against indexed documents.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IBM25SearchService"/> abstracts the BM25 full-text search pipeline,
/// enabling keyword-based queries against the indexed document corpus using
/// PostgreSQL's tsvector and ts_rank functions. This service complements
/// <see cref="ISemanticSearchService"/> for hybrid search scenarios.
/// </para>
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
///   <item>Keyword-based queries against indexed document chunks.</item>
///   <item>BM25-style ranking via PostgreSQL's ts_rank function.</item>
///   <item>Configurable result limits and score thresholds.</item>
///   <item>Optional document-scoped search filtering.</item>
///   <item>Query preprocessing with whitespace normalization.</item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b> BM25 search requires <b>WriterPro</b> tier or higher.
/// Implementations must check the user's license tier and throw
/// <see cref="FeatureNotLicensedException"/> for unauthorized access.
/// </para>
/// <para>
/// <b>Search Flow:</b>
/// <list type="number">
///   <item><description>Validate license tier (WriterPro+).</description></item>
///   <item><description>Preprocess query (normalize whitespace).</description></item>
///   <item><description>Convert query to tsquery via plainto_tsquery.</description></item>
///   <item><description>Execute full-text search with ts_rank scoring.</description></item>
///   <item><description>Assemble and return ranked <see cref="SearchResult"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>BM25 vs Semantic:</b>
/// <list type="bullet">
///   <item><description>BM25 excels at exact keyword matches and technical terms.</description></item>
///   <item><description>Semantic search handles conceptual similarity and synonyms.</description></item>
///   <item><description>Hybrid search (v0.5.1c+) combines both for optimal results.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.1b as part of the Hybrid Engine feature set.
/// </para>
/// </remarks>
public interface IBM25SearchService
{
    /// <summary>
    /// Searches the indexed corpus for keyword matches using BM25 ranking.
    /// </summary>
    /// <param name="query">
    /// Keyword query text. Cannot be null, empty, or whitespace.
    /// The query is preprocessed (normalized) before conversion to tsquery.
    /// </param>
    /// <param name="options">
    /// Search configuration options controlling result count, score threshold,
    /// and document filtering. Use <see cref="SearchOptions.Default"/> for
    /// sensible defaults. Note: <see cref="SearchOptions.ExpandAbbreviations"/>
    /// and <see cref="SearchOptions.UseCache"/> are not used by BM25 search.
    /// </param>
    /// <param name="ct">Cancellation token for operation control.</param>
    /// <returns>
    /// A task that completes with a <see cref="SearchResult"/> containing
    /// ranked <see cref="SearchHit"/> matches. Returns an empty result
    /// (via <see cref="SearchResult.Empty"/>) if no matches meet the
    /// <see cref="SearchOptions.MinScore"/> threshold.
    /// </returns>
    /// <remarks>
    /// <para>LOGIC: Primary search entry point for the BM25 keyword query pipeline.</para>
    /// <para>
    /// <b>Behavior:</b>
    /// <list type="bullet">
    ///   <item>Returns results ranked by descending ts_rank score.</item>
    ///   <item>Filters results below <see cref="SearchOptions.MinScore"/>.</item>
    ///   <item>Limits results to <see cref="SearchOptions.TopK"/> entries.</item>
    ///   <item>Applies <see cref="SearchOptions.DocumentFilter"/> if set.</item>
    ///   <item>Publishes <c>BM25SearchExecutedEvent</c> on successful completion.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Performance:</b> Target latency is &lt; 100ms for a corpus of 10,000 chunks
    /// using the GIN index on ContentTsVector.
    /// </para>
    /// </remarks>
    /// <exception cref="FeatureNotLicensedException">
    /// Thrown when the user's license tier is below WriterPro.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="query"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="ct"/> is cancelled.
    /// </exception>
    Task<SearchResult> SearchAsync(
        string query,
        SearchOptions options,
        CancellationToken ct = default);
}
