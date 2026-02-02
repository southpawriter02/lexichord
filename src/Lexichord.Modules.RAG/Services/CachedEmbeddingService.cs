// =============================================================================
// File: CachedEmbeddingService.cs
// Project: Lexichord.Modules.RAG
// Description: Decorator that adds caching to the embedding service.
// Version: v0.4.8d
// =============================================================================
// LOGIC: Wraps IEmbeddingService to provide transparent caching of embeddings.
//   - Computes content hash for cache key lookup.
//   - Returns cached embeddings on hit, calls inner service on miss.
//   - Handles batch operations with partial cache hits efficiently.
//   - Passes through when caching is disabled.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Decorator that adds embedding caching to an <see cref="IEmbeddingService"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CachedEmbeddingService"/> implements the Decorator pattern to transparently
/// add caching capabilities to any embedding service. It computes a content hash for each
/// text input and checks the cache before calling the underlying service.
/// </para>
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
///   <item>Transparent caching via the Decorator pattern.</item>
///   <item>Content hash-based cache key for efficient lookups.</item>
///   <item>Efficient batch handling with partial cache hits.</item>
///   <item>Configurable enable/disable via options.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.8d as part of the RAG Hardening phase.
/// </para>
/// </remarks>
public sealed class CachedEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingService _inner;
    private readonly IEmbeddingCache _cache;
    private readonly EmbeddingCacheOptions _options;
    private readonly ILogger<CachedEmbeddingService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="CachedEmbeddingService"/>.
    /// </summary>
    /// <param name="inner">The underlying embedding service.</param>
    /// <param name="cache">The embedding cache.</param>
    /// <param name="options">Cache configuration options.</param>
    /// <param name="logger">Logger for cache operations.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public CachedEmbeddingService(
        IEmbeddingService inner,
        IEmbeddingCache cache,
        IOptions<EmbeddingCacheOptions> options,
        ILogger<CachedEmbeddingService> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public string ModelName => _inner.ModelName;

    /// <inheritdoc/>
    public int Dimensions => _inner.Dimensions;

    /// <inheritdoc/>
    public int MaxTokens => _inner.MaxTokens;

    /// <inheritdoc/>
    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);

        // LOGIC: Pass through if caching is disabled.
        if (!_options.Enabled)
        {
            return await _inner.EmbedAsync(text, ct);
        }

        var contentHash = SqliteEmbeddingCache.ComputeContentHash(text);

        // LOGIC: Check cache first.
        if (_cache.TryGet(contentHash, out var cachedEmbedding) && cachedEmbedding is not null)
        {
            _logger.LogDebug("Embedding cache hit for text hash {Hash}", contentHash[..8]);
            return cachedEmbedding;
        }

        // LOGIC: Cache miss - call inner service.
        _logger.LogDebug("Embedding cache miss for text hash {Hash}, calling API", contentHash[..8]);
        var embedding = await _inner.EmbedAsync(text, ct);

        // LOGIC: Cache the result.
        _cache.Set(contentHash, embedding);

        return embedding;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(texts);

        // LOGIC: Pass through if caching is disabled.
        if (!_options.Enabled)
        {
            return await _inner.EmbedBatchAsync(texts, ct);
        }

        // LOGIC: Check cache for each text and identify misses.
        var results = new float[texts.Count][];
        var missIndices = new List<int>();
        var missTexts = new List<string>();

        for (var i = 0; i < texts.Count; i++)
        {
            var text = texts[i];
            var contentHash = SqliteEmbeddingCache.ComputeContentHash(text);

            if (_cache.TryGet(contentHash, out var cachedEmbedding) && cachedEmbedding is not null)
            {
                results[i] = cachedEmbedding;
            }
            else
            {
                missIndices.Add(i);
                missTexts.Add(text);
            }
        }

        _logger.LogDebug(
            "Batch embedding: {HitCount} cache hits, {MissCount} misses",
            texts.Count - missIndices.Count,
            missIndices.Count);

        // LOGIC: If all were cached, return immediately.
        if (missIndices.Count == 0)
        {
            return results;
        }

        // LOGIC: Call inner service for misses only.
        var missedEmbeddings = await _inner.EmbedBatchAsync(missTexts, ct);

        // LOGIC: Merge results and cache the new embeddings.
        for (var i = 0; i < missIndices.Count; i++)
        {
            var originalIndex = missIndices[i];
            var embedding = missedEmbeddings[i];
            var text = missTexts[i];
            var contentHash = SqliteEmbeddingCache.ComputeContentHash(text);

            results[originalIndex] = embedding;
            _cache.Set(contentHash, embedding);
        }

        return results;
    }
}
