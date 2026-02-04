// =============================================================================
// File: IQueryResultCache.cs
// Project: Lexichord.Abstractions
// Description: Interface for global query result caching with LRU+TTL eviction.
// Version: v0.5.8c
// =============================================================================
// LOGIC: Abstracts in-memory caching of search results to reduce latency for
//   repeated queries. Uses composite key (query+options+filters) with SHA256.
//   Conservative invalidation removes entries containing changed documents.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Provides global in-memory caching for search query results.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IQueryResultCache"/> caches <see cref="SearchResult"/> objects keyed by
/// a SHA256 hash of the normalized query text, search options, and filters. This reduces
/// latency for repeated searches and decreases database load.
/// </para>
/// <para>
/// <b>Eviction Strategy:</b> LRU (Least Recently Used) combined with TTL (Time To Live).
/// Entries are removed when they exceed maximum capacity or when their TTL expires.
/// </para>
/// <para>
/// <b>Thread Safety:</b> All operations are thread-safe via <c>ReaderWriterLockSlim</c>.
/// </para>
/// <para>
/// <b>Invalidation:</b> When a document is re-indexed or removed, all cache entries
/// containing results from that document are invalidated.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.8c as part of the Multi-Layer Caching System.
/// </para>
/// </remarks>
public interface IQueryResultCache
{
    /// <summary>
    /// Attempts to retrieve a cached search result.
    /// </summary>
    /// <param name="cacheKey">
    /// The SHA256 hash key generated from the query, options, and filters.
    /// </param>
    /// <param name="result">
    /// When this method returns <c>true</c>, contains the cached search result.
    /// When this method returns <c>false</c>, contains <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the result was found in the cache and has not expired;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Thread-safe read that also updates the last-accessed timestamp for
    /// LRU tracking. Expired entries are not returned and are lazily removed.
    /// </remarks>
    bool TryGet(string cacheKey, out SearchResult? result);

    /// <summary>
    /// Stores a search result in the cache.
    /// </summary>
    /// <param name="cacheKey">
    /// The SHA256 hash key generated from the query, options, and filters.
    /// </param>
    /// <param name="result">The search result to cache.</param>
    /// <param name="documentIds">
    /// Collection of document IDs contained in the result, used for invalidation tracking.
    /// </param>
    /// <remarks>
    /// <para>
    /// LOGIC: If the key already exists, the entry is replaced. If the cache exceeds
    /// its maximum entry limit after insertion, the least recently used entry is evicted.
    /// </para>
    /// <para>
    /// The <paramref name="documentIds"/> parameter enables efficient invalidation when
    /// any referenced document is re-indexed or removed.
    /// </para>
    /// </remarks>
    void Set(string cacheKey, SearchResult result, IReadOnlyCollection<Guid> documentIds);

    /// <summary>
    /// Invalidates all cache entries that contain results from the specified document.
    /// </summary>
    /// <param name="documentId">The ID of the document that was modified.</param>
    /// <remarks>
    /// LOGIC: Scans all entries and removes those referencing the document. This ensures
    /// stale results are never returned after document changes.
    /// </remarks>
    void InvalidateForDocument(Guid documentId);

    /// <summary>
    /// Removes all entries from the cache.
    /// </summary>
    /// <remarks>
    /// LOGIC: Clears all cached results and resets statistics counters (except eviction
    /// and invalidation counts, which are cumulative).
    /// </remarks>
    void Clear();

    /// <summary>
    /// Gets current statistics about the cache.
    /// </summary>
    /// <returns>A <see cref="CacheStatistics"/> record containing cache metrics.</returns>
    CacheStatistics GetStatistics();
}
