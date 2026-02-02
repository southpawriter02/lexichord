// =============================================================================
// File: IEmbeddingCache.cs
// Project: Lexichord.Abstractions
// Description: Interface defining the contract for embedding caching.
// Version: v0.4.8d
// =============================================================================
// LOGIC: Abstracts local embedding storage to reduce API costs and latency.
//   - Content hash (SHA-256) is used as the key for cache lookups.
//   - LRU eviction is expected when size limits are exceeded.
//   - Statistics tracking for hit/miss rates and cache efficiency.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Provides caching for embedding vectors to reduce API calls and improve latency.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IEmbeddingCache"/> enables local storage of embedding vectors, keyed by
/// content hash (SHA-256). This reduces costs for repeated embeddings of the same content
/// and improves response times for cached queries.
/// </para>
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
///   <item>Content hash-based lookup for efficient retrieval.</item>
///   <item>LRU (Least Recently Used) eviction when size limits are exceeded.</item>
///   <item>Statistics tracking for monitoring cache efficiency.</item>
///   <item>Thread-safe operations for concurrent access.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.8d as part of the RAG Hardening phase.
/// </para>
/// </remarks>
public interface IEmbeddingCache
{
    /// <summary>
    /// Attempts to retrieve an embedding from the cache.
    /// </summary>
    /// <param name="contentHash">
    /// The SHA-256 hash of the content that was embedded.
    /// </param>
    /// <param name="embedding">
    /// When this method returns <c>true</c>, contains the cached embedding vector.
    /// When this method returns <c>false</c>, contains <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the embedding was found in the cache; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Thread-safe lookup that also updates the last-accessed timestamp
    /// for LRU eviction purposes. The hit/miss counters are updated accordingly.
    /// </remarks>
    bool TryGet(string contentHash, out float[]? embedding);

    /// <summary>
    /// Stores an embedding in the cache.
    /// </summary>
    /// <param name="contentHash">
    /// The SHA-256 hash of the content that was embedded.
    /// </param>
    /// <param name="embedding">
    /// The embedding vector to cache.
    /// </param>
    /// <remarks>
    /// <para>
    /// LOGIC: If the content hash already exists, the existing entry is updated.
    /// If the cache exceeds its size limit after insertion, LRU eviction is triggered.
    /// </para>
    /// <para>
    /// This operation is thread-safe and may block briefly during eviction.
    /// </para>
    /// </remarks>
    void Set(string contentHash, float[] embedding);

    /// <summary>
    /// Gets current statistics about the cache.
    /// </summary>
    /// <returns>
    /// An <see cref="EmbeddingCacheStatistics"/> record containing cache metrics.
    /// </returns>
    /// <remarks>
    /// LOGIC: Returns a snapshot of cache statistics including entry count,
    /// size in bytes, hit/miss counts, and calculated hit rate.
    /// </remarks>
    EmbeddingCacheStatistics GetStatistics();

    /// <summary>
    /// Removes all entries from the cache.
    /// </summary>
    /// <remarks>
    /// LOGIC: Clears all cached embeddings and resets statistics counters.
    /// This operation is thread-safe and will block other operations until complete.
    /// </remarks>
    void Clear();

    /// <summary>
    /// Forces compaction by evicting least recently used entries.
    /// </summary>
    /// <remarks>
    /// LOGIC: Manually triggers LRU eviction to reduce cache size.
    /// Useful when the application needs to free memory or storage space.
    /// The amount evicted depends on the configured compaction threshold.
    /// </remarks>
    void Compact();
}

/// <summary>
/// Statistics about the embedding cache state and performance.
/// </summary>
/// <param name="EntryCount">Number of embeddings currently in the cache.</param>
/// <param name="SizeBytes">Total size of cached embeddings in bytes.</param>
/// <param name="HitCount">Number of successful cache retrievals.</param>
/// <param name="MissCount">Number of cache misses.</param>
/// <param name="HitRate">Calculated hit rate (0.0 to 1.0).</param>
/// <param name="EvictionCount">Number of entries evicted due to size limits.</param>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.4.8d as part of the Embedding Cache feature.
/// </para>
/// </remarks>
public record EmbeddingCacheStatistics(
    int EntryCount,
    long SizeBytes,
    long HitCount,
    long MissCount,
    double HitRate,
    int EvictionCount)
{
    /// <summary>
    /// Formats the size in a human-readable format (e.g., "1.5 MB").
    /// </summary>
    /// <returns>A formatted string representing the cache size.</returns>
    public string FormatSize()
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        return SizeBytes switch
        {
            >= GB => $"{(double)SizeBytes / GB:F2} GB",
            >= MB => $"{(double)SizeBytes / MB:F2} MB",
            >= KB => $"{(double)SizeBytes / KB:F2} KB",
            _ => $"{SizeBytes} B"
        };
    }

    /// <summary>
    /// Creates an empty statistics record for an uninitialized cache.
    /// </summary>
    public static EmbeddingCacheStatistics Empty => new(0, 0, 0, 0, 0.0, 0);
}
