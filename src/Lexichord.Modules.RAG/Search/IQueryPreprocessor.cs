// =============================================================================
// File: IQueryPreprocessor.cs
// Project: Lexichord.Modules.RAG
// Description: Interface for query preprocessing operations in the semantic
//              search pipeline.
// =============================================================================
// LOGIC: Defines the contract for query normalization, abbreviation expansion,
//   and query embedding caching. The preprocessor sits between the raw user
//   query input and the embedding service, ensuring consistent and optimized
//   query handling.
//
//   - Process() normalizes whitespace, Unicode, and optionally expands abbreviations.
//   - GetCachedEmbedding() checks for previously computed embeddings (5-min TTL).
//   - CacheEmbedding() stores embeddings for repeated query reuse.
//   - ClearCache() signals cache clear (entries expire naturally via sliding window).
//
//   Interface defined in v0.4.5b for use by PgVectorSearchService.
//   Implementation (QueryPreprocessor) delivered in v0.4.5c.
//   ClearCache() added in v0.4.5c.
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Interface for query preprocessing operations in the semantic search pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The query preprocessor is responsible for transforming raw user input into
/// a normalized form suitable for embedding generation. This includes:
/// </para>
/// <list type="bullet">
///   <item><description>Whitespace trimming and collapsing.</description></item>
///   <item><description>Unicode normalization (NFC form).</description></item>
///   <item><description>Optional abbreviation expansion (e.g., "API" → "API (Application Programming Interface)").</description></item>
///   <item><description>Query embedding caching with a 5-minute TTL to avoid redundant API calls.</description></item>
/// </list>
/// <para>
/// <b>Pipeline Position:</b> Raw query → <see cref="IQueryPreprocessor.Process"/> → Embedding →
/// Vector search. The preprocessor ensures that semantically identical queries produce
/// consistent embeddings regardless of whitespace or formatting differences.
/// </para>
/// <para>
/// <b>Caching Strategy:</b> Query embeddings are cached using the processed (normalized)
/// query text as the key. This means "  API docs  " and "API docs" will share a cached
/// embedding after normalization.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe for concurrent search operations.
/// </para>
/// <para>
/// <b>Introduced:</b> v0.4.5b (interface). Implementation in v0.4.5c.
/// </para>
/// </remarks>
public interface IQueryPreprocessor
{
    /// <summary>
    /// Processes a raw query string for semantic search.
    /// </summary>
    /// <param name="query">The raw query text from user input.</param>
    /// <param name="options">
    /// Search options controlling preprocessing behavior, specifically
    /// <see cref="SearchOptions.ExpandAbbreviations"/> for abbreviation expansion.
    /// </param>
    /// <returns>
    /// The processed query string with normalized whitespace, Unicode normalization,
    /// and optional abbreviation expansion applied. Returns <see cref="string.Empty"/>
    /// if the input is null or whitespace.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Processing Steps:</b>
    /// </para>
    /// <list type="number">
    ///   <item><description>Trim leading and trailing whitespace.</description></item>
    ///   <item><description>Collapse multiple whitespace characters to single spaces.</description></item>
    ///   <item><description>Apply Unicode NFC normalization.</description></item>
    ///   <item><description>If <see cref="SearchOptions.ExpandAbbreviations"/> is true,
    ///     expand known abbreviations using a lookup table.</description></item>
    /// </list>
    /// </remarks>
    string Process(string query, SearchOptions options);

    /// <summary>
    /// Gets a cached query embedding if available.
    /// </summary>
    /// <param name="query">The processed (normalized) query text.</param>
    /// <returns>
    /// The cached embedding as a <c>float[]</c> if found and not expired;
    /// otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Cache entries have a 5-minute TTL. The cache key is derived from
    /// the lowercase hash of the processed query text, so the caller should
    /// pass the output of <see cref="Process"/> rather than the raw query.
    /// </para>
    /// </remarks>
    float[]? GetCachedEmbedding(string query);

    /// <summary>
    /// Caches a query embedding for future reuse.
    /// </summary>
    /// <param name="query">The processed (normalized) query text used as the cache key.</param>
    /// <param name="embedding">The embedding vector to cache.</param>
    /// <remarks>
    /// <para>
    /// The embedding is stored with a 5-minute sliding expiration. Subsequent calls
    /// to <see cref="GetCachedEmbedding"/> with the same processed query will return
    /// this embedding until it expires.
    /// </para>
    /// </remarks>
    void CacheEmbedding(string query, float[] embedding);

    /// <summary>
    /// Clears all cached query embeddings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Note:</b> The underlying <c>IMemoryCache</c> does not natively support
    /// clearing all entries. Implementations should log that entries will expire
    /// naturally via the sliding expiration window (5 minutes).
    /// </para>
    /// <para>
    /// <b>Introduced:</b> v0.4.5c.
    /// </para>
    /// </remarks>
    void ClearCache();
}
