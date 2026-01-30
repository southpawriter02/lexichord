using System.Collections.Concurrent;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// LRU cache for compiled regex patterns.
/// </summary>
/// <remarks>
/// LOGIC: Provides bounded-memory caching with LRU eviction.
/// Thread-safe via ConcurrentDictionary and locking for eviction.
///
/// Design:
/// - Fixed capacity set at construction
/// - LRU eviction when at capacity
/// - Thread-safe concurrent access
/// - Hit/miss statistics tracking
///
/// Version: v0.2.3c
/// </remarks>
/// <typeparam name="TKey">The type of cache keys.</typeparam>
/// <typeparam name="TValue">The type of cached values.</typeparam>
internal sealed class PatternCache<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new();
    private readonly int _maxSize;
    private readonly object _evictionLock = new();

    private long _hits;
    private long _misses;

    /// <summary>
    /// Initializes a new pattern cache with specified capacity.
    /// </summary>
    /// <param name="maxSize">Maximum number of entries.</param>
    public PatternCache(int maxSize)
    {
        if (maxSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size must be positive.");

        _maxSize = maxSize;
    }

    /// <summary>
    /// Gets the current number of cached entries.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Gets the maximum cache capacity.
    /// </summary>
    public int MaxSize => _maxSize;

    /// <summary>
    /// Gets the total cache hits.
    /// </summary>
    public long Hits => Interlocked.Read(ref _hits);

    /// <summary>
    /// Gets the total cache misses.
    /// </summary>
    public long Misses => Interlocked.Read(ref _misses);

    /// <summary>
    /// Tries to get a cached value.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The cached value if found.</param>
    /// <returns>True if found, false otherwise.</returns>
    /// <remarks>
    /// LOGIC: Updates LastAccess for LRU tracking on hit.
    /// </remarks>
    public bool TryGet(TKey key, out TValue? value)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            entry.TouchAccess();
            Interlocked.Increment(ref _hits);
            value = entry.Value;
            return true;
        }

        Interlocked.Increment(ref _misses);
        value = default;
        return false;
    }

    /// <summary>
    /// Adds or updates a cached value.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <remarks>
    /// LOGIC: Evicts LRU entry if at capacity before adding.
    /// </remarks>
    public void Set(TKey key, TValue value)
    {
        if (_cache.ContainsKey(key))
        {
            // Update existing entry
            _cache[key] = new CacheEntry(value);
            return;
        }

        // Evict if at capacity
        if (_cache.Count >= _maxSize)
        {
            EvictLeastRecentlyUsed();
        }

        _cache.TryAdd(key, new CacheEntry(value));
    }

    /// <summary>
    /// Gets or adds a value using a factory function.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="valueFactory">Factory to create value if not cached.</param>
    /// <returns>The cached or newly created value.</returns>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        if (TryGet(key, out var existing))
        {
            return existing!;
        }

        var value = valueFactory(key);
        Set(key, value);
        return value;
    }

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
        Interlocked.Exchange(ref _hits, 0);
        Interlocked.Exchange(ref _misses, 0);
    }

    /// <summary>
    /// Evicts the least recently used entry.
    /// </summary>
    /// <remarks>
    /// LOGIC: Only one thread can evict at a time to prevent over-eviction.
    /// </remarks>
    private void EvictLeastRecentlyUsed()
    {
        lock (_evictionLock)
        {
            // Check again after acquiring lock
            if (_cache.Count < _maxSize)
                return;

            // Find LRU entry
            TKey? lruKey = default;
            long oldestTicks = long.MaxValue;

            foreach (var kvp in _cache)
            {
                if (kvp.Value.LastAccessTicks < oldestTicks)
                {
                    oldestTicks = kvp.Value.LastAccessTicks;
                    lruKey = kvp.Key;
                }
            }

            if (lruKey is not null)
            {
                _cache.TryRemove(lruKey, out _);
            }
        }
    }

    /// <summary>
    /// Internal cache entry with LRU tracking.
    /// </summary>
    private sealed class CacheEntry
    {
        public TValue Value { get; }
        public long LastAccessTicks { get; private set; }

        public CacheEntry(TValue value)
        {
            Value = value;
            LastAccessTicks = DateTime.UtcNow.Ticks;
        }

        public void TouchAccess()
        {
            LastAccessTicks = DateTime.UtcNow.Ticks;
        }
    }
}
