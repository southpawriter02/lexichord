// =============================================================================
// File: ContextExpansionService.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of IContextExpansionService with LRU caching.
// =============================================================================
// LOGIC: Expands retrieved chunks with surrounding context and heading
//   hierarchy. Uses ConcurrentDictionary for thread-safe LRU-style caching.
//   Gracefully degrades if heading resolution fails.
// =============================================================================
// VERSION: v0.5.3a (Context Expansion Service)
// =============================================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Expands retrieved chunks with surrounding context and heading hierarchy.
/// Uses LRU-style caching to minimize database queries.
/// </summary>
/// <remarks>
/// <para>
/// Cache key is the chunk ID. Cache is invalidated when documents are re-indexed.
/// </para>
/// <para>
/// The cache uses FIFO eviction when at capacity (100 entries). While not
/// strictly LRU, this provides acceptable behavior for typical search patterns
/// with minimal implementation complexity.
/// </para>
/// <para>
/// <b>Thread Safety:</b> All cache operations use <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// for thread-safe access.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3a as part of The Context Window feature.
/// </para>
/// </remarks>
public sealed class ContextExpansionService : IContextExpansionService
{
    private readonly IChunkRepository _chunkRepository;
    private readonly IHeadingHierarchyService _headingService;
    private readonly IMediator _mediator;
    private readonly ILogger<ContextExpansionService> _logger;

    private readonly ConcurrentDictionary<Guid, CacheEntry> _cache = new();
    private const int MaxCacheSize = 100;

    /// <summary>
    /// Cache entry with timestamp for FIFO eviction.
    /// </summary>
    private sealed record CacheEntry(ExpandedChunk Expanded, DateTimeOffset CachedAt);

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextExpansionService"/> class.
    /// </summary>
    /// <param name="chunkRepository">Repository for chunk retrieval.</param>
    /// <param name="headingService">Service for heading breadcrumb resolution.</param>
    /// <param name="mediator">MediatR mediator for event publishing.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public ContextExpansionService(
        IChunkRepository chunkRepository,
        IHeadingHierarchyService headingService,
        IMediator mediator,
        ILogger<ContextExpansionService> logger)
    {
        _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
        _headingService = headingService ?? throw new ArgumentNullException(nameof(headingService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("ContextExpansionService initialized with max cache size {MaxCacheSize}", MaxCacheSize);
    }

    /// <inheritdoc />
    public async Task<ExpandedChunk> ExpandAsync(
        Chunk chunk,
        ContextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(chunk);

        var validatedOptions = (options ?? new ContextOptions()).Validated();

        _logger.LogDebug(
            "Expanding context for chunk {ChunkId} with options: Before={Before}, After={After}, Headings={Headings}",
            chunk.Id, validatedOptions.PrecedingChunks, validatedOptions.FollowingChunks, validatedOptions.IncludeHeadings);

        // Check cache first
        if (_cache.TryGetValue(chunk.Id, out var cached))
        {
            _logger.LogDebug("Cache hit for chunk {ChunkId}", chunk.Id);

            await _mediator.Publish(new ContextExpandedEvent(
                ChunkId: chunk.Id,
                DocumentId: chunk.DocumentId,
                BeforeCount: cached.Expanded.Before.Count,
                AfterCount: cached.Expanded.After.Count,
                HasBreadcrumb: cached.Expanded.HasBreadcrumb,
                ElapsedMilliseconds: 0,
                FromCache: true), cancellationToken);

            return cached.Expanded;
        }

        var stopwatch = Stopwatch.StartNew();

        // Query sibling chunks
        var siblings = await _chunkRepository.GetSiblingsAsync(
            chunk.DocumentId,
            chunk.ChunkIndex,
            validatedOptions.PrecedingChunks,
            validatedOptions.FollowingChunks,
            cancellationToken);

        // Partition into before/after based on chunk index
        var coreIndex = chunk.ChunkIndex;
        var before = siblings
            .Where(c => c.ChunkIndex < coreIndex)
            .OrderBy(c => c.ChunkIndex)
            .ToList();
        var after = siblings
            .Where(c => c.ChunkIndex > coreIndex)
            .OrderBy(c => c.ChunkIndex)
            .ToList();

        // Resolve heading breadcrumb if requested
        IReadOnlyList<string> breadcrumb = Array.Empty<string>();
        string? parentHeading = null;

        if (validatedOptions.IncludeHeadings)
        {
            try
            {
                breadcrumb = await _headingService.GetBreadcrumbAsync(
                    chunk.DocumentId,
                    coreIndex,
                    cancellationToken);

                parentHeading = breadcrumb.LastOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to resolve heading breadcrumb for chunk {ChunkId} in document {DocumentId}",
                    chunk.Id, chunk.DocumentId);
                // Continue without breadcrumb - graceful degradation
            }
        }

        var expanded = new ExpandedChunk(
            Core: chunk,
            Before: before.AsReadOnly(),
            After: after.AsReadOnly(),
            ParentHeading: parentHeading,
            HeadingBreadcrumb: breadcrumb);

        // Cache the result
        AddToCache(chunk.Id, expanded);

        stopwatch.Stop();

        _logger.LogInformation(
            "Context expanded for chunk {ChunkId}: {BeforeCount} before, {AfterCount} after, breadcrumb={HasBreadcrumb}, elapsed={ElapsedMs}ms",
            chunk.Id, before.Count, after.Count, expanded.HasBreadcrumb, stopwatch.ElapsedMilliseconds);

        await _mediator.Publish(new ContextExpandedEvent(
            ChunkId: chunk.Id,
            DocumentId: chunk.DocumentId,
            BeforeCount: before.Count,
            AfterCount: after.Count,
            HasBreadcrumb: expanded.HasBreadcrumb,
            ElapsedMilliseconds: stopwatch.ElapsedMilliseconds,
            FromCache: false), cancellationToken);

        return expanded;
    }

    /// <inheritdoc />
    public void InvalidateCache(Guid documentId)
    {
        var keysToRemove = _cache
            .Where(kvp => kvp.Value.Expanded.Core.DocumentId == documentId)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }

        _logger.LogDebug(
            "Invalidated {Count} cached expansions for document {DocumentId}",
            keysToRemove.Count, documentId);
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        var count = _cache.Count;
        _cache.Clear();
        _logger.LogDebug("Context expansion cache cleared ({Count} entries removed)", count);
    }

    /// <summary>
    /// Adds an expanded chunk to the cache, evicting oldest if at capacity.
    /// </summary>
    private void AddToCache(Guid chunkId, ExpandedChunk expanded)
    {
        // Evict if at capacity (simple FIFO eviction)
        if (_cache.Count >= MaxCacheSize)
        {
            var oldest = _cache
                .OrderBy(kvp => kvp.Value.CachedAt)
                .FirstOrDefault();

            if (oldest.Key != Guid.Empty)
            {
                _cache.TryRemove(oldest.Key, out _);
                _logger.LogDebug("Evicted cache entry for chunk {ChunkId} (FIFO)", oldest.Key);
            }
        }

        _cache.TryAdd(chunkId, new CacheEntry(expanded, DateTimeOffset.UtcNow));
    }
}
