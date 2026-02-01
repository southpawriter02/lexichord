// <copyright file="ISemanticSearchService.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for semantic search against indexed documents.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ISemanticSearchService"/> abstracts the semantic query pipeline,
/// enabling natural language queries against the indexed document corpus.
/// Implementations handle query preprocessing, embedding generation, vector
/// similarity search, and result assembly.
/// </para>
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
///   <item>Natural language queries against indexed document chunks.</item>
///   <item>Cosine similarity ranking via pgvector.</item>
///   <item>Configurable result limits and score thresholds.</item>
///   <item>Optional document-scoped search filtering.</item>
///   <item>Query preprocessing with abbreviation expansion.</item>
///   <item>Short-lived query embedding cache.</item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b> Semantic search requires <b>WriterPro</b> tier or higher.
/// Implementations must check the user's license tier and throw
/// <see cref="FeatureNotLicensedException"/> for unauthorized access.
/// </para>
/// <para>
/// <b>Search Flow:</b>
/// <list type="number">
///   <item><description>Validate license tier (WriterPro+).</description></item>
///   <item><description>Preprocess query (normalize, optionally expand abbreviations).</description></item>
///   <item><description>Generate or retrieve cached query embedding.</description></item>
///   <item><description>Execute vector similarity search via pgvector.</description></item>
///   <item><description>Assemble and return ranked <see cref="SearchResult"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5a as part of the Search Abstractions layer.
/// </para>
/// </remarks>
public interface ISemanticSearchService
{
    /// <summary>
    /// Searches the indexed corpus for semantically similar content.
    /// </summary>
    /// <param name="query">
    /// Natural language query text. Cannot be null, empty, or whitespace.
    /// The query is preprocessed (normalized, optionally expanded) before embedding.
    /// </param>
    /// <param name="options">
    /// Search configuration options controlling result count, score threshold,
    /// document filtering, abbreviation expansion, and caching behavior.
    /// Use <see cref="SearchOptions.Default"/> for sensible defaults.
    /// </param>
    /// <param name="ct">Cancellation token for operation control.</param>
    /// <returns>
    /// A task that completes with a <see cref="SearchResult"/> containing
    /// ranked <see cref="SearchHit"/> matches. Returns an empty result
    /// (via <see cref="SearchResult.Empty"/>) if no matches meet the
    /// <see cref="SearchOptions.MinScore"/> threshold.
    /// </returns>
    /// <remarks>
    /// <para>LOGIC: Primary search entry point for the semantic query pipeline.</para>
    /// <para>
    /// <b>Behavior:</b>
    /// <list type="bullet">
    ///   <item>Returns results ranked by descending cosine similarity.</item>
    ///   <item>Filters results below <see cref="SearchOptions.MinScore"/>.</item>
    ///   <item>Limits results to <see cref="SearchOptions.TopK"/> entries.</item>
    ///   <item>Applies <see cref="SearchOptions.DocumentFilter"/> if set.</item>
    ///   <item>Expands abbreviations if <see cref="SearchOptions.ExpandAbbreviations"/> is true.</item>
    ///   <item>Uses cached embeddings if <see cref="SearchOptions.UseCache"/> is true.</item>
    ///   <item>Publishes <c>SemanticSearchExecutedEvent</c> on successful completion.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Performance:</b> Target latency is &lt; 200ms for a corpus of 10,000 chunks
    /// using the pgvector HNSW index.
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
