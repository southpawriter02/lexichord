// =============================================================================
// File: HeadingHierarchyService.cs
// Project: Lexichord.Modules.RAG
// Description: Service for resolving heading hierarchy breadcrumbs from document chunks.
// =============================================================================
// LOGIC: Builds heading trees from chunk metadata and resolves breadcrumb paths.
//   - Uses ConcurrentDictionary for thread-safe caching (MaxCacheSize=50).
//   - Stack-based tree building: pop until parent level < current level.
//   - Breadcrumb resolution via recursive depth-first search.
//   - Subscribes to DocumentIndexedEvent and DocumentRemovedFromIndexEvent for cache invalidation.
// =============================================================================
// VERSION: v0.5.3c (Heading Hierarchy)
// =============================================================================

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Indexing;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Service for resolving heading hierarchy breadcrumbs from document chunks.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="HeadingHierarchyService"/> builds a structural understanding of documents
/// based on their heading levels (H1-H6 in Markdown/HTML). This enables:
/// <list type="bullet">
///   <item><description>Breadcrumb navigation in search results.</description></item>
///   <item><description>Section-aware context expansion.</description></item>
///   <item><description>Document outline generation.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Caching:</b> Heading trees are cached per document using <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// to avoid repeated database queries. Maximum cache size is <see cref="MaxCacheSize"/> (50 entries).
/// Cache is automatically invalidated when documents are re-indexed or removed via MediatR handlers.
/// </para>
/// <para>
/// <b>Tree Building Algorithm:</b> Uses a stack-based approach where headings are pushed onto
/// a stack and popped until a parent with a lower level is found. This handles:
/// <list type="bullet">
///   <item><description>Sequential level increases (H1 → H2 → H3)</description></item>
///   <item><description>Skipped levels (H1 → H3 directly)</description></item>
///   <item><description>Level resets (H1 → H2 → H1)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Breadcrumb Resolution:</b> Uses recursive depth-first search to find the path from
/// the root to the deepest heading that contains the target chunk index. Returns the
/// heading texts in order from outermost to innermost.
/// </para>
/// <para>
/// <b>License Gate:</b> Writer Pro (via <c>FeatureFlags.RAG.ContextWindow</c>)
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3c as part of The Context Window feature.
/// </para>
/// </remarks>
public sealed class HeadingHierarchyService : IHeadingHierarchyService,
                                               INotificationHandler<DocumentIndexedEvent>,
                                               INotificationHandler<DocumentRemovedFromIndexEvent>
{
    private readonly IChunkRepository _chunkRepository;
    private readonly ILogger<HeadingHierarchyService> _logger;

    /// <summary>
    /// Cache of heading trees keyed by document ID.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cache stores the root <see cref="HeadingNode"/> for each document.
    /// A null value indicates the document has no headings.
    /// A missing key indicates the document has not been queried yet.
    /// </para>
    /// </remarks>
    private readonly ConcurrentDictionary<Guid, HeadingNode?> _cache = new();

    /// <summary>
    /// Maximum number of document heading trees to cache.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the cache exceeds this size, the oldest entries are evicted
    /// using a simple FIFO approach via snapshot and removal.
    /// </para>
    /// </remarks>
    public const int MaxCacheSize = 50;

    /// <summary>
    /// Number of entries to evict when cache exceeds <see cref="MaxCacheSize"/>.
    /// </summary>
    public const int EvictionBatch = 10;

    /// <summary>
    /// Creates a new <see cref="HeadingHierarchyService"/> instance.
    /// </summary>
    /// <param name="chunkRepository">Repository for chunk data access.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="chunkRepository"/> or <paramref name="logger"/> is null.
    /// </exception>
    public HeadingHierarchyService(
        IChunkRepository chunkRepository,
        ILogger<HeadingHierarchyService> logger)
    {
        _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug(
            "HeadingHierarchyService initialized with MaxCacheSize={MaxCacheSize}",
            MaxCacheSize);
    }

    /// <summary>
    /// Gets the current number of cached document heading trees.
    /// </summary>
    /// <value>The count of documents with cached heading trees.</value>
    public int CacheCount => _cache.Count;

    #region IHeadingHierarchyService Implementation

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>Implementation:</b> First retrieves or builds the heading tree for the document,
    /// then uses recursive depth-first search to find the path to the chunk.
    /// </para>
    /// <para>
    /// <b>Edge Cases:</b>
    /// <list type="bullet">
    ///   <item><description>Negative chunk index: throws <see cref="ArgumentException"/></description></item>
    ///   <item><description>Document without headings: returns empty list</description></item>
    ///   <item><description>Chunk before first heading: returns empty list</description></item>
    ///   <item><description>Chunk after last heading: belongs to last heading in scope</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<string>> GetBreadcrumbAsync(
        Guid documentId,
        int chunkIndex,
        CancellationToken cancellationToken = default)
    {
        // LOGIC: Validate chunk index per specification.
        if (chunkIndex < 0)
        {
            _logger.LogWarning(
                "GetBreadcrumbAsync called with negative chunk index {ChunkIndex} for document {DocumentId}",
                chunkIndex, documentId);
            throw new ArgumentException("Chunk index must be non-negative.", nameof(chunkIndex));
        }

        _logger.LogDebug(
            "GetBreadcrumbAsync: document={DocumentId}, chunkIndex={ChunkIndex}",
            documentId, chunkIndex);

        // LOGIC: Get or build the heading tree for this document.
        var tree = await BuildHeadingTreeAsync(documentId, cancellationToken);

        if (tree is null)
        {
            _logger.LogDebug(
                "No heading tree for document {DocumentId}, returning empty breadcrumb",
                documentId);
            return Array.Empty<string>();
        }

        // LOGIC: Find the breadcrumb path via recursive depth-first search.
        var breadcrumb = FindBreadcrumbPath(tree, chunkIndex);

        _logger.LogDebug(
            "Breadcrumb for document {DocumentId}, chunkIndex {ChunkIndex}: [{Breadcrumb}]",
            documentId, chunkIndex, string.Join(" > ", breadcrumb));

        return breadcrumb;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>Caching:</b> The heading tree is cached per document. Subsequent calls
    /// return the cached tree unless the cache has been invalidated.
    /// </para>
    /// <para>
    /// <b>Tree Construction:</b> Uses a stack-based algorithm:
    /// <list type="number">
    ///   <item><description>Query all chunks with headings for the document.</description></item>
    ///   <item><description>For each heading, pop the stack until a parent is found.</description></item>
    ///   <item><description>Attach the current heading as a child of the parent.</description></item>
    ///   <item><description>Push the current heading onto the stack.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public async Task<HeadingNode?> BuildHeadingTreeAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        // LOGIC: Check cache first.
        if (_cache.TryGetValue(documentId, out var cached))
        {
            _logger.LogDebug(
                "Cache hit for heading tree: document={DocumentId}",
                documentId);
            return cached;
        }

        _logger.LogDebug(
            "Cache miss for heading tree: document={DocumentId}, querying repository",
            documentId);

        // LOGIC: Query chunks with heading metadata from repository.
        var headingChunks = await _chunkRepository.GetChunksWithHeadingsAsync(
            documentId, cancellationToken);

        if (headingChunks.Count == 0)
        {
            _logger.LogDebug(
                "No headings found for document {DocumentId}",
                documentId);

            // LOGIC: Cache null to indicate no headings (avoid repeated queries).
            CacheTree(documentId, null);
            return null;
        }

        _logger.LogDebug(
            "Building heading tree for document {DocumentId} from {Count} heading chunks",
            documentId, headingChunks.Count);

        // LOGIC: Build the tree using stack-based algorithm.
        var tree = BuildTreeFromHeadings(headingChunks);

        // LOGIC: Cache the result.
        CacheTree(documentId, tree);

        return tree;
    }

    /// <inheritdoc />
    public void InvalidateCache(Guid documentId)
    {
        if (_cache.TryRemove(documentId, out _))
        {
            _logger.LogDebug(
                "Invalidated heading tree cache for document {DocumentId}",
                documentId);
        }
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        var count = _cache.Count;
        _cache.Clear();

        _logger.LogDebug(
            "Cleared entire heading tree cache ({Count} entries)",
            count);
    }

    #endregion

    #region MediatR Notification Handlers

    /// <summary>
    /// Handles document indexed events by invalidating the heading tree cache.
    /// </summary>
    /// <param name="notification">The document indexed event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    /// <remarks>
    /// <para>
    /// When a document is re-indexed, its heading structure may have changed.
    /// This handler invalidates the cached heading tree to ensure fresh data.
    /// </para>
    /// </remarks>
    public Task Handle(DocumentIndexedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "DocumentIndexedEvent received for document {DocumentId}, invalidating heading cache",
            notification.DocumentId);

        InvalidateCache(notification.DocumentId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles document removed events by invalidating the heading tree cache.
    /// </summary>
    /// <param name="notification">The document removed event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    /// <remarks>
    /// <para>
    /// When a document is removed from the index, its heading tree is no longer valid.
    /// This handler removes the cached entry to free memory.
    /// </para>
    /// </remarks>
    public Task Handle(DocumentRemovedFromIndexEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "DocumentRemovedFromIndexEvent received for document {DocumentId}, invalidating heading cache",
            notification.DocumentId);

        InvalidateCache(notification.DocumentId);
        return Task.CompletedTask;
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Builds a heading tree from a list of heading chunks using stack-based algorithm.
    /// </summary>
    /// <param name="headingChunks">Ordered list of chunks with heading metadata.</param>
    /// <returns>The root of the heading tree, or null if no valid headings.</returns>
    /// <remarks>
    /// <para>
    /// <b>Algorithm:</b>
    /// <list type="number">
    ///   <item><description>Create a virtual root at level 0 to hold multiple H1 headings.</description></item>
    ///   <item><description>Maintain a stack of (node, level) pairs for parent tracking.</description></item>
    ///   <item><description>For each heading: pop until stack top has lower level than current.</description></item>
    ///   <item><description>Attach current heading as child of stack top, push current.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Skipped Levels:</b> If an H3 follows an H1 directly (skipping H2),
    /// the H3 becomes a direct child of the H1.
    /// </para>
    /// </remarks>
    private HeadingNode? BuildTreeFromHeadings(IReadOnlyList<ChunkHeadingInfo> headingChunks)
    {
        // LOGIC: Filter out chunks without valid headings.
        var validHeadings = headingChunks
            .Where(h => !string.IsNullOrWhiteSpace(h.Heading))
            .OrderBy(h => h.ChunkIndex)
            .ToList();

        if (validHeadings.Count == 0)
        {
            _logger.LogDebug("No valid headings found after filtering");
            return null;
        }

        // LOGIC: Build mutable tree nodes first, then convert to immutable HeadingNode.
        // We use a builder pattern since HeadingNode.Children is immutable.
        var builders = new List<HeadingNodeBuilder>();
        var stack = new Stack<(HeadingNodeBuilder Node, int Level)>();

        // LOGIC: Virtual root at level 0 to hold potentially multiple H1s.
        var virtualRoot = new HeadingNodeBuilder(Guid.Empty, "ROOT", 0, -1);
        stack.Push((virtualRoot, 0));

        foreach (var heading in validHeadings)
        {
            var builder = new HeadingNodeBuilder(
                heading.Id,
                heading.Heading!,
                heading.HeadingLevel,
                heading.ChunkIndex);

            // LOGIC: Pop stack until we find a parent with strictly lower level.
            // This handles both normal nesting and skipped levels.
            while (stack.Count > 1 && stack.Peek().Level >= heading.HeadingLevel)
            {
                stack.Pop();
            }

            // LOGIC: Attach current heading as child of parent (top of stack).
            var parent = stack.Peek().Node;
            parent.Children.Add(builder);

            // LOGIC: Push current heading onto stack for potential children.
            stack.Push((builder, heading.HeadingLevel));

            _logger.LogTrace(
                "Attached heading '{Heading}' (level {Level}) to parent '{Parent}'",
                heading.Heading, heading.HeadingLevel, parent.Text);
        }

        // LOGIC: Convert mutable builders to immutable HeadingNode tree.
        if (virtualRoot.Children.Count == 0)
        {
            return null;
        }

        // LOGIC: If there's only one top-level heading, return it as the root.
        // If multiple H1s exist, return the virtual root so breadcrumb search
        // can traverse all top-level sections to find the correct scope.
        if (virtualRoot.Children.Count == 1)
        {
            return BuildImmutableTree(virtualRoot.Children[0]);
        }

        // LOGIC: Multiple top-level headings - return virtual root structure.
        // The virtual root's ChunkIndex of -1 ensures any chunk at index >= 0
        // will be considered within scope for breadcrumb searching.
        return BuildImmutableTree(virtualRoot);
    }

    /// <summary>
    /// Recursively converts a mutable HeadingNodeBuilder to an immutable HeadingNode.
    /// </summary>
    /// <param name="builder">The mutable builder to convert.</param>
    /// <returns>The immutable HeadingNode with all descendants.</returns>
    private HeadingNode BuildImmutableTree(HeadingNodeBuilder builder)
    {
        var children = builder.Children
            .Select(BuildImmutableTree)
            .ToList();

        return new HeadingNode(
            builder.Id,
            builder.Text,
            builder.Level,
            builder.ChunkIndex,
            children);
    }

    /// <summary>
    /// Finds the breadcrumb path to a chunk using recursive depth-first search.
    /// </summary>
    /// <param name="root">The root of the heading tree.</param>
    /// <param name="targetChunkIndex">The chunk index to find the path to.</param>
    /// <returns>List of heading texts from root to deepest containing heading.</returns>
    /// <remarks>
    /// <para>
    /// <b>Algorithm:</b> Recursively traverse the tree, collecting headings whose
    /// scope contains the target chunk index. A heading's scope extends from its
    /// ChunkIndex to (but not including) the next sibling's ChunkIndex or end of document.
    /// </para>
    /// </remarks>
    private IReadOnlyList<string> FindBreadcrumbPath(HeadingNode root, int targetChunkIndex)
    {
        var path = new List<string>();

        // LOGIC: If target is before the first heading, return empty path.
        if (targetChunkIndex < root.ChunkIndex)
        {
            _logger.LogDebug(
                "Target chunk {TargetIndex} is before first heading at index {RootIndex}",
                targetChunkIndex, root.ChunkIndex);
            return path;
        }

        // LOGIC: Find the path recursively.
        FindPathRecursive(root, targetChunkIndex, path, int.MaxValue);

        return path;
    }

    /// <summary>
    /// Recursively finds the breadcrumb path to a target chunk index.
    /// </summary>
    /// <param name="node">Current node being examined.</param>
    /// <param name="targetChunkIndex">The chunk index to find.</param>
    /// <param name="path">Accumulator for the path of heading texts.</param>
    /// <param name="scopeEnd">The chunk index where current scope ends.</param>
    /// <returns>True if the target is within this node's scope or descendants.</returns>
    private bool FindPathRecursive(
        HeadingNode node,
        int targetChunkIndex,
        List<string> path,
        int scopeEnd)
    {
        // LOGIC: Target must be at or after this heading's chunk index.
        if (targetChunkIndex < node.ChunkIndex)
        {
            return false;
        }

        // LOGIC: Target must be before the scope end.
        if (targetChunkIndex >= scopeEnd)
        {
            return false;
        }

        // LOGIC: Target is within this heading's scope.
        // Skip virtual root (level 0) - don't include "ROOT" in breadcrumb.
        if (node.Level > 0)
        {
            path.Add(node.Text);
        }

        // LOGIC: Check if target falls within any child's scope.
        // Child scopes are: [child.ChunkIndex, nextChild.ChunkIndex) or [child.ChunkIndex, scopeEnd) for last child.
        for (int i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            var childScopeEnd = i + 1 < node.Children.Count
                ? node.Children[i + 1].ChunkIndex
                : scopeEnd;

            if (FindPathRecursive(child, targetChunkIndex, path, childScopeEnd))
            {
                return true;
            }
        }

        // LOGIC: Target is in this heading's scope but not in any child's scope.
        return true;
    }

    /// <summary>
    /// Caches a heading tree with LRU eviction when at capacity.
    /// </summary>
    /// <param name="documentId">Document ID to cache.</param>
    /// <param name="tree">The heading tree (may be null for documents without headings).</param>
    private void CacheTree(Guid documentId, HeadingNode? tree)
    {
        // LOGIC: Evict oldest entries if at capacity.
        if (_cache.Count >= MaxCacheSize)
        {
            EvictOldest();
        }

        _cache[documentId] = tree;

        _logger.LogDebug(
            "Cached heading tree for document {DocumentId} (tree={HasTree}, cacheSize={CacheSize})",
            documentId, tree is not null, _cache.Count);
    }

    /// <summary>
    /// Evicts the oldest entries from the cache using FIFO approach.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Since <see cref="ConcurrentDictionary{TKey,TValue}"/> doesn't maintain insertion order,
    /// we simply remove a batch of entries. This is a simplification over true LRU
    /// but acceptable for the heading tree cache which is typically small.
    /// </para>
    /// </remarks>
    private void EvictOldest()
    {
        // LOGIC: Take a snapshot and remove first N entries (pseudo-FIFO).
        var toEvict = _cache.Keys.Take(EvictionBatch).ToList();

        foreach (var key in toEvict)
        {
            _cache.TryRemove(key, out _);
        }

        _logger.LogDebug(
            "Evicted {Count} heading tree cache entries",
            toEvict.Count);
    }

    #endregion

    #region Private Helper Types

    /// <summary>
    /// Mutable builder for constructing heading trees before converting to immutable HeadingNode.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This internal class is needed because <see cref="HeadingNode"/> uses
    /// <see cref="IReadOnlyList{T}"/> for Children, making it immutable.
    /// We build the tree with mutable lists first, then convert.
    /// </para>
    /// </remarks>
    private sealed class HeadingNodeBuilder
    {
        /// <summary>
        /// Gets the unique identifier for this heading.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the heading text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the heading level (1-6).
        /// </summary>
        public int Level { get; }

        /// <summary>
        /// Gets the chunk index where this heading appears.
        /// </summary>
        public int ChunkIndex { get; }

        /// <summary>
        /// Gets the mutable list of child headings.
        /// </summary>
        public List<HeadingNodeBuilder> Children { get; } = new();

        /// <summary>
        /// Creates a new heading node builder.
        /// </summary>
        /// <param name="id">Unique identifier.</param>
        /// <param name="text">Heading text.</param>
        /// <param name="level">Heading level (1-6).</param>
        /// <param name="chunkIndex">Chunk index where heading appears.</param>
        public HeadingNodeBuilder(Guid id, string text, int level, int chunkIndex)
        {
            Id = id;
            Text = text;
            Level = level;
            ChunkIndex = chunkIndex;
        }
    }

    #endregion
}
