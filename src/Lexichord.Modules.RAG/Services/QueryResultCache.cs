// =============================================================================
// File: QueryResultCache.cs
// Project: Lexichord.Modules.RAG
// Description: In-memory cache for search query results with LRU+TTL eviction.
// Version: v0.5.8c
// =============================================================================
// LOGIC: Provides global caching of SearchResult objects keyed by SHA256 hash.
//   - LRU eviction when MaxEntries exceeded.
//   - TTL expiration checked on every TryGet.
//   - ReaderWriterLockSlim for thread-safe concurrent access.
//   - Document-aware invalidation for freshness.
// =============================================================================

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// In-memory cache for search query results with LRU and TTL eviction.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="QueryResultCache"/> stores <see cref="SearchResult"/> objects keyed by
/// a SHA256 hash of the query parameters. This reduces latency for repeated searches
/// and decreases database load.
/// </para>
/// <para>
/// <b>Eviction Strategy:</b>
/// <list type="bullet">
///   <item><b>LRU:</b> When <see cref="QueryCacheOptions.MaxEntries"/> is exceeded.</item>
///   <item><b>TTL:</b> Entries expire after <see cref="QueryCacheOptions.TtlSeconds"/>.</item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Uses <see cref="ReaderWriterLockSlim"/> for efficient
/// concurrent read access with exclusive write locking.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.8c as part of the Multi-Layer Caching System.
/// </para>
/// </remarks>
public sealed class QueryResultCache : IQueryResultCache, IDisposable
{
    private readonly QueryCacheOptions _options;
    private readonly ILogger<QueryResultCache> _logger;
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

    // LOGIC: Main cache storage - key is SHA256 hash, value is cache entry
    private readonly Dictionary<string, CacheEntry> _cache = new();

    // LOGIC: Reverse index for document-based invalidation - documentId -> set of cache keys
    private readonly Dictionary<Guid, HashSet<string>> _documentIndex = new();

    // LOGIC: Statistics counters
    private long _hitCount;
    private long _missCount;
    private int _evictionCount;
    private int _invalidationCount;
    private bool _disposed;

    /// <summary>
    /// Cache entry with result, timestamp, and document references.
    /// </summary>
    private sealed record CacheEntry(
        SearchResult Result,
        DateTimeOffset CachedAt,
        DateTimeOffset LastAccessedAt,
        IReadOnlyCollection<Guid> DocumentIds);

    /// <summary>
    /// Initializes a new instance of <see cref="QueryResultCache"/>.
    /// </summary>
    /// <param name="options">Configuration options for the cache.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="logger"/> is null.
    /// </exception>
    public QueryResultCache(
        IOptions<QueryCacheOptions> options,
        ILogger<QueryResultCache> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;

        _logger.LogInformation(
            "QueryResultCache initialized: MaxEntries={MaxEntries}, TTL={TtlSeconds}s, Enabled={Enabled}",
            _options.MaxEntries, _options.TtlSeconds, _options.Enabled);
    }

    /// <inheritdoc/>
    public bool TryGet(string cacheKey, out SearchResult? result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        if (!_options.Enabled)
        {
            result = null;
            return false;
        }

        _lock.EnterUpgradeableReadLock();
        try
        {
            if (!_cache.TryGetValue(cacheKey, out var entry))
            {
                Interlocked.Increment(ref _missCount);
                _logger.LogDebug("Cache miss for key {Key}", TruncateKey(cacheKey));
                result = null;
                return false;
            }

            // LOGIC: Check TTL expiration
            var age = DateTimeOffset.UtcNow - entry.CachedAt;
            if (age > _options.TtlTimeSpan)
            {
                // Expired - remove lazily
                _lock.EnterWriteLock();
                try
                {
                    RemoveEntryUnsafe(cacheKey, entry);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                Interlocked.Increment(ref _missCount);
                _logger.LogDebug("Cache entry expired for key {Key} (age={Age}s)", TruncateKey(cacheKey), age.TotalSeconds);
                result = null;
                return false;
            }

            // LOGIC: Update last accessed time for LRU
            _lock.EnterWriteLock();
            try
            {
                _cache[cacheKey] = entry with { LastAccessedAt = DateTimeOffset.UtcNow };
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            Interlocked.Increment(ref _hitCount);
            _logger.LogDebug("Cache hit for key {Key}", TruncateKey(cacheKey));
            result = entry.Result;
            return true;
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    /// <inheritdoc/>
    public void Set(string cacheKey, SearchResult result, IReadOnlyCollection<Guid> documentIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(documentIds);

        if (!_options.Enabled)
        {
            return;
        }

        _lock.EnterWriteLock();
        try
        {
            // LOGIC: Remove existing entry if present
            if (_cache.TryGetValue(cacheKey, out var existing))
            {
                RemoveEntryUnsafe(cacheKey, existing);
            }

            // LOGIC: Evict LRU entry if at capacity
            while (_cache.Count >= _options.MaxEntries)
            {
                EvictLruEntryUnsafe();
            }

            // LOGIC: Add new entry
            var now = DateTimeOffset.UtcNow;
            var entry = new CacheEntry(result, now, now, documentIds);
            _cache[cacheKey] = entry;

            // LOGIC: Update document index for invalidation lookup
            foreach (var docId in documentIds)
            {
                if (!_documentIndex.TryGetValue(docId, out var keys))
                {
                    keys = new HashSet<string>();
                    _documentIndex[docId] = keys;
                }
                keys.Add(cacheKey);
            }

            _logger.LogDebug(
                "Cached result for key {Key} with {DocCount} documents, {HitCount} hits",
                TruncateKey(cacheKey), documentIds.Count, result.Hits.Count);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    public void InvalidateForDocument(Guid documentId)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_documentIndex.TryGetValue(documentId, out var keysToRemove))
            {
                _logger.LogDebug("No cache entries to invalidate for document {DocumentId}", documentId);
                return;
            }

            var count = 0;
            foreach (var cacheKey in keysToRemove.ToList())
            {
                if (_cache.TryGetValue(cacheKey, out var entry))
                {
                    RemoveEntryUnsafe(cacheKey, entry);
                    count++;
                    _invalidationCount++;
                }
            }

            _documentIndex.Remove(documentId);

            _logger.LogInformation(
                "Invalidated {Count} cache entries for document {DocumentId}",
                count, documentId);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            var count = _cache.Count;
            _cache.Clear();
            _documentIndex.Clear();

            // Reset access counters, but keep eviction/invalidation counts
            Interlocked.Exchange(ref _hitCount, 0);
            Interlocked.Exchange(ref _missCount, 0);

            _logger.LogInformation("Cleared query result cache ({Count} entries removed)", count);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    public CacheStatistics GetStatistics()
    {
        _lock.EnterReadLock();
        try
        {
            var hitCount = Interlocked.Read(ref _hitCount);
            var missCount = Interlocked.Read(ref _missCount);
            var total = hitCount + missCount;
            var hitRate = total > 0 ? (double)hitCount / total : 0.0;

            // LOGIC: Estimate memory usage (rough approximation)
            var estimatedSize = _cache.Count * 1024L; // ~1KB per entry estimate

            return new CacheStatistics(
                HitCount: hitCount,
                MissCount: missCount,
                HitRate: hitRate,
                EntryCount: _cache.Count,
                MaxEntries: _options.MaxEntries,
                EvictionCount: _evictionCount,
                InvalidationCount: _invalidationCount,
                ApproximateSizeBytes: estimatedSize);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lock.Dispose();
        _logger.LogDebug("QueryResultCache disposed");
    }

    #region Private Methods

    /// <summary>
    /// Evicts the least recently used entry. Must be called within write lock.
    /// </summary>
    private void EvictLruEntryUnsafe()
    {
        if (_cache.Count == 0)
        {
            return;
        }

        var lru = _cache
            .OrderBy(kvp => kvp.Value.LastAccessedAt)
            .First();

        RemoveEntryUnsafe(lru.Key, lru.Value);
        _evictionCount++;

        _logger.LogDebug("Evicted LRU cache entry {Key}", TruncateKey(lru.Key));
    }

    /// <summary>
    /// Removes an entry and updates the document index. Must be called within write lock.
    /// </summary>
    private void RemoveEntryUnsafe(string cacheKey, CacheEntry entry)
    {
        _cache.Remove(cacheKey);

        // LOGIC: Update document index
        foreach (var docId in entry.DocumentIds)
        {
            if (_documentIndex.TryGetValue(docId, out var keys))
            {
                keys.Remove(cacheKey);
                if (keys.Count == 0)
                {
                    _documentIndex.Remove(docId);
                }
            }
        }
    }

    /// <summary>
    /// Truncates a cache key for logging.
    /// </summary>
    private static string TruncateKey(string key) =>
        key.Length <= 12 ? key : key[..12] + "...";

    #endregion
}
