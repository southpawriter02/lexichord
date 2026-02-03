// =============================================================================
// File: SiblingCache.cs
// Project: Lexichord.Modules.RAG
// Description: LRU cache for sibling chunk queries with document-level invalidation.
// Version: v0.5.3b
// =============================================================================
// LOGIC: Thread-safe LRU cache using ConcurrentDictionary.
//   - MaxEntries = 500 entries before LRU eviction triggers.
//   - EvictionBatch = 50 entries removed when at capacity.
//   - TryGet updates LastAccessed for LRU ordering.
//   - Set adds or updates entries with capacity check.
//   - InvalidateDocument removes all entries for a specific document.
//   - Subscribes to DocumentIndexedEvent and DocumentRemovedFromIndexEvent.
// =============================================================================

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Indexing;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Data;

/// <summary>
/// LRU cache for sibling chunk queries with document-level invalidation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SiblingCache"/> provides an in-memory cache for sibling chunk queries
/// to avoid repeated database hits for the same context expansion requests.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Uses <see cref="ConcurrentDictionary{TKey,TValue}"/> for
/// thread-safe read and write operations. LRU eviction uses a snapshot approach
/// to minimize lock contention.
/// </para>
/// <para>
/// <b>LRU Eviction:</b> When the cache reaches <see cref="MaxEntries"/> (500),
/// the <see cref="EvictionBatch"/> (50) oldest entries based on
/// <see cref="CacheEntry.LastAccessed"/> are removed.
/// </para>
/// <para>
/// <b>Document Invalidation:</b> Subscribes to <see cref="DocumentIndexedEvent"/>
/// and <see cref="DocumentRemovedFromIndexEvent"/> to automatically invalidate
/// stale cache entries when documents are re-indexed or removed.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3b as part of Sibling Chunk Retrieval caching.
/// </para>
/// </remarks>
public sealed class SiblingCache : INotificationHandler<DocumentIndexedEvent>,
                                    INotificationHandler<DocumentRemovedFromIndexEvent>
{
    private readonly ConcurrentDictionary<SiblingCacheKey, CacheEntry> _cache = new();
    private readonly ILogger<SiblingCache> _logger;

    /// <summary>
    /// Maximum number of entries before LRU eviction triggers.
    /// </summary>
    public const int MaxEntries = 500;

    /// <summary>
    /// Number of entries to evict when cache is at capacity.
    /// </summary>
    public const int EvictionBatch = 50;

    /// <summary>
    /// Initializes a new instance of the <see cref="SiblingCache"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    public SiblingCache(ILogger<SiblingCache> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogDebug("SiblingCache initialized with MaxEntries={MaxEntries}, EvictionBatch={EvictionBatch}",
            MaxEntries, EvictionBatch);
    }

    /// <summary>
    /// Gets the current number of cached entries.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Attempts to get cached siblings for the given key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="chunks">The cached chunks if found.</param>
    /// <returns>True if cache hit, false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// On cache hit, the entry's <see cref="CacheEntry.LastAccessed"/> timestamp
    /// is updated (touched) to preserve the entry in LRU eviction.
    /// </para>
    /// </remarks>
    public bool TryGet(SiblingCacheKey key, out IReadOnlyList<Chunk> chunks)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            // LOGIC: Touch entry to update LRU timestamp.
            entry.Touch();
            chunks = entry.Chunks;
            return true;
        }

        chunks = Array.Empty<Chunk>();
        return false;
    }

    /// <summary>
    /// Adds or updates a cache entry.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="chunks">The chunks to cache.</param>
    /// <remarks>
    /// <para>
    /// If the cache is at capacity (<see cref="MaxEntries"/>), the oldest
    /// <see cref="EvictionBatch"/> entries are evicted before adding.
    /// </para>
    /// </remarks>
    public void Set(SiblingCacheKey key, IReadOnlyList<Chunk> chunks)
    {
        // LOGIC: Evict oldest entries if at capacity.
        if (_cache.Count >= MaxEntries)
        {
            EvictOldest();
        }

        _cache[key] = new CacheEntry(chunks);
    }

    /// <summary>
    /// Invalidates all cache entries for a specific document.
    /// </summary>
    /// <param name="documentId">The document ID to invalidate.</param>
    /// <remarks>
    /// <para>
    /// Removes all cache entries where <see cref="SiblingCacheKey.DocumentId"/>
    /// matches the specified document. This is called automatically when
    /// <see cref="DocumentIndexedEvent"/> or <see cref="DocumentRemovedFromIndexEvent"/>
    /// is published.
    /// </para>
    /// </remarks>
    public void InvalidateDocument(Guid documentId)
    {
        // LOGIC: Collect keys matching the document, then remove them.
        // Uses snapshot approach to minimize lock contention.
        var keysToRemove = _cache.Keys
            .Where(k => k.DocumentId == documentId)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }

        if (keysToRemove.Count > 0)
        {
            _logger.LogDebug(
                "Invalidated {Count} sibling cache entries for document {DocumentId}",
                keysToRemove.Count, documentId);
        }
    }

    /// <summary>
    /// Clears the entire cache.
    /// </summary>
    public void Clear()
    {
        var count = _cache.Count;
        _cache.Clear();
        _logger.LogDebug("Sibling cache cleared ({Count} entries)", count);
    }

    /// <summary>
    /// Handles document indexed events by invalidating stale cache entries.
    /// </summary>
    /// <param name="notification">The document indexed event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    /// <remarks>
    /// <para>
    /// When a document is re-indexed, the chunks may have changed (different
    /// content, different chunk indices). This handler invalidates all cached
    /// sibling queries for that document to ensure fresh data is returned.
    /// </para>
    /// </remarks>
    public Task Handle(DocumentIndexedEvent notification, CancellationToken cancellationToken)
    {
        InvalidateDocument(notification.DocumentId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles document removed events by invalidating cache entries.
    /// </summary>
    /// <param name="notification">The document removed event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    /// <remarks>
    /// <para>
    /// When a document is removed from the index, its chunks no longer exist.
    /// This handler invalidates all cached sibling queries for that document.
    /// </para>
    /// </remarks>
    public Task Handle(DocumentRemovedFromIndexEvent notification, CancellationToken cancellationToken)
    {
        InvalidateDocument(notification.DocumentId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Evicts the oldest entries based on last access time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses a snapshot of the cache to determine eviction candidates,
    /// then removes them one by one. This approach minimizes lock contention
    /// in high-concurrency scenarios.
    /// </para>
    /// </remarks>
    private void EvictOldest()
    {
        // LOGIC: Get snapshot, sort by oldest access time, remove batch.
        var toEvict = _cache
            .OrderBy(kvp => kvp.Value.LastAccessed)
            .Take(EvictionBatch)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in toEvict)
        {
            _cache.TryRemove(key, out _);
        }

        _logger.LogDebug("Evicted {Count} oldest sibling cache entries", toEvict.Count);
    }

    /// <summary>
    /// Internal cache entry with access tracking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="CacheEntry"/> stores the cached chunks along with
    /// a <see cref="LastAccessed"/> timestamp for LRU eviction ordering.
    /// </para>
    /// <para>
    /// <b>Note:</b> The <see cref="Touch"/> method is intentionally not
    /// thread-safe for the timestamp update. In high-concurrency scenarios,
    /// the worst case is that an entry is evicted slightly earlier than
    /// optimal, which is acceptable for cache correctness.
    /// </para>
    /// </remarks>
    private sealed class CacheEntry
    {
        /// <summary>
        /// Gets the cached chunks.
        /// </summary>
        public IReadOnlyList<Chunk> Chunks { get; }

        /// <summary>
        /// Gets the last access timestamp for LRU ordering.
        /// </summary>
        public DateTime LastAccessed { get; private set; }

        /// <summary>
        /// Initializes a new cache entry.
        /// </summary>
        /// <param name="chunks">The chunks to cache.</param>
        public CacheEntry(IReadOnlyList<Chunk> chunks)
        {
            Chunks = chunks;
            LastAccessed = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the last access timestamp.
        /// </summary>
        public void Touch() => LastAccessed = DateTime.UtcNow;
    }
}
