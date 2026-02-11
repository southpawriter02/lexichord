// -----------------------------------------------------------------------
// <copyright file="CachedContextAssembler.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.Agents.Performance;

/// <summary>
/// Caches assembled context to avoid redundant RAG searches and style rule lookups.
/// Uses an in-memory cache with configurable expiration.
/// </summary>
/// <remarks>
/// <para>
/// This implementation wraps an <see cref="IContextInjector"/> and caches its
/// <see cref="IContextInjector.AssembleContextAsync"/> results. Cache keys are
/// derived from the document path and context request configuration to ensure
/// distinct cache entries for different query combinations.
/// </para>
/// <para>
/// Thread safety is ensured through <see cref="Interlocked"/> operations on
/// hit/miss counters and the thread-safe nature of <see cref="MemoryCache"/>.
/// </para>
/// <para>
/// Cache entries use absolute expiration relative to now, configured via
/// <see cref="PerformanceOptions.ContextCacheDuration"/>. The cache has a
/// size limit of 100 entries to prevent unbounded growth.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8c as part of Performance Optimization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var assembler = serviceProvider.GetRequiredService&lt;ICachedContextAssembler&gt;();
///
/// // First call — cache miss, assembles context
/// var context1 = await assembler.GetOrCreateAsync("doc.md", request, ct);
///
/// // Second call with same params — cache hit
/// var context2 = await assembler.GetOrCreateAsync("doc.md", request, ct);
///
/// // After document edit — invalidate and reassemble
/// assembler.Invalidate("doc.md");
/// var context3 = await assembler.GetOrCreateAsync("doc.md", request, ct);
/// </code>
/// </example>
/// <seealso cref="ICachedContextAssembler"/>
/// <seealso cref="PerformanceOptions"/>
public sealed class CachedContextAssembler : ICachedContextAssembler, IDisposable
{
    // LOGIC: The underlying context injector that performs the actual assembly work.
    private readonly IContextInjector _inner;

    // LOGIC: Logger for cache hit/miss diagnostics and ratio reporting.
    private readonly ILogger<CachedContextAssembler> _logger;

    // LOGIC: In-memory cache with size limit for context entries.
    private readonly MemoryCache _cache;

    // LOGIC: Resolved performance options including cache duration.
    private readonly PerformanceOptions _options;

    // LOGIC: Thread-safe hit counter for cache ratio calculation.
    private int _hits;

    // LOGIC: Thread-safe miss counter for cache ratio calculation.
    private int _misses;

    // LOGIC: Tracks known cache keys per document path for targeted invalidation.
    private readonly Dictionary<string, HashSet<string>> _documentKeys = new();

    // LOGIC: Lock for thread-safe access to the document keys dictionary.
    private readonly object _documentKeysLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedContextAssembler"/> class.
    /// </summary>
    /// <param name="inner">The context injector to wrap with caching.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="options">Performance configuration options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner"/>, <paramref name="logger"/>,
    /// or <paramref name="options"/> is null.
    /// </exception>
    public CachedContextAssembler(
        IContextInjector inner,
        ILogger<CachedContextAssembler> logger,
        IOptions<PerformanceOptions> options)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;

        // LOGIC: Initialize memory cache with a size limit to prevent unbounded growth.
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });

        _logger.LogDebug(
            "CachedContextAssembler initialized with CacheDuration={Duration}s",
            _options.ContextCacheDuration.TotalSeconds);
    }

    /// <inheritdoc />
    public double CacheHitRatio
    {
        get
        {
            var total = _hits + _misses;
            return total == 0 ? 0.0 : (double)_hits / total;
        }
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, object>> GetOrCreateAsync(
        string documentPath,
        ContextRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(documentPath, nameof(documentPath));
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var key = GenerateCacheKey(documentPath, request);

        // LOGIC: Check cache before performing expensive context assembly.
        if (_cache.TryGetValue(key, out IDictionary<string, object>? cached) && cached != null)
        {
            Interlocked.Increment(ref _hits);
            _logger.LogDebug("Context cache hit for {Path}", documentPath);
            return cached;
        }

        // LOGIC: Cache miss — perform the actual context assembly.
        Interlocked.Increment(ref _misses);
        _logger.LogDebug("Context cache miss for {Path}, assembling...", documentPath);

        var context = await _inner.AssembleContextAsync(request, ct);

        // LOGIC: Store the assembled context in cache with configured expiration.
        var entry = _cache.CreateEntry(key);
        entry.Value = context;
        entry.Size = 1;
        entry.AbsoluteExpirationRelativeToNow = _options.ContextCacheDuration;
        entry.Dispose(); // Commits the entry to the cache

        // LOGIC: Track the cache key for this document path to support targeted invalidation.
        TrackDocumentKey(documentPath, key);

        // LOGIC: Log cache statistics periodically (every 10 requests).
        var totalRequests = _hits + _misses;
        if (totalRequests > 0 && totalRequests % 10 == 0)
        {
            _logger.LogInformation(
                "Cache hit ratio: {Ratio:P1}",
                CacheHitRatio);
        }

        return context;
    }

    /// <inheritdoc />
    public void Invalidate(string documentPath)
    {
        ArgumentNullException.ThrowIfNull(documentPath, nameof(documentPath));

        _logger.LogDebug("Invalidating cache for {Path}", documentPath);

        // LOGIC: Remove all cache entries associated with this document path.
        lock (_documentKeysLock)
        {
            if (_documentKeys.TryGetValue(documentPath, out var keys))
            {
                foreach (var key in keys)
                {
                    _cache.Remove(key);
                }

                _documentKeys.Remove(documentPath);

                _logger.LogDebug(
                    "Removed {Count} cache entries for {Path}",
                    keys.Count,
                    documentPath);
            }
        }
    }

    /// <summary>
    /// Generates a deterministic cache key from the document path and request configuration.
    /// </summary>
    /// <param name="documentPath">The document path.</param>
    /// <param name="request">The context request configuration.</param>
    /// <returns>A unique string key for the cache entry.</returns>
    private static string GenerateCacheKey(string documentPath, ContextRequest request)
        => $"{documentPath}:{request.IncludeStyleRules}:{request.IncludeRAGContext}:{request.MaxRAGChunks}";

    /// <summary>
    /// Tracks a cache key against its document path for targeted invalidation.
    /// </summary>
    /// <param name="documentPath">The document path.</param>
    /// <param name="key">The cache key to track.</param>
    private void TrackDocumentKey(string documentPath, string key)
    {
        lock (_documentKeysLock)
        {
            if (!_documentKeys.TryGetValue(documentPath, out var keys))
            {
                keys = new HashSet<string>();
                _documentKeys[documentPath] = keys;
            }

            keys.Add(key);
        }
    }

    /// <summary>
    /// Disposes the underlying <see cref="MemoryCache"/>.
    /// </summary>
    public void Dispose()
    {
        _cache.Dispose();
    }
}
