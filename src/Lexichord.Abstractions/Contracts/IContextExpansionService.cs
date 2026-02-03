// =============================================================================
// File: IContextExpansionService.cs
// Project: Lexichord.Abstractions
// Description: Interface for expanding retrieved chunks with surrounding context.
// =============================================================================
// LOGIC: Provides context expansion for RAG search results by retrieving
//   adjacent chunks and heading hierarchy. Implements in-memory caching with
//   per-document invalidation for performance.
// =============================================================================
// VERSION: v0.5.3a (Context Expansion Service)
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Expands a retrieved chunk with surrounding context from the same document.
/// </summary>
/// <remarks>
/// <para>
/// Context expansion retrieves adjacent chunks (before/after) based on chunk_index,
/// providing additional document context for RAG (Retrieval-Augmented Generation)
/// search results.
/// </para>
/// <para>
/// The service also resolves the parent heading hierarchy for document structure
/// navigation, enabling users to understand where a chunk appears within the
/// document's organizational hierarchy.
/// </para>
/// <para>
/// Results are cached per-session to minimize database queries. Cache is
/// invalidated when documents are re-indexed.
/// </para>
/// <para>
/// <b>License Gate:</b> Writer Pro (Soft Gate via <c>FeatureFlags.RAG.ContextWindow</c>)
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3a as part of The Context Window feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new ContextOptions(PrecedingChunks: 2, FollowingChunks: 1);
/// var expanded = await contextService.ExpandAsync(chunk, options);
/// // expanded.Before contains up to 2 chunks
/// // expanded.After contains up to 1 chunk
/// // expanded.HeadingBreadcrumb contains the heading trail
/// </code>
/// </example>
public interface IContextExpansionService
{
    /// <summary>
    /// Expands a chunk with its surrounding context.
    /// </summary>
    /// <param name="chunk">The core chunk to expand. MUST NOT be null.</param>
    /// <param name="options">Expansion configuration options. If null, uses defaults.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Expanded chunk with before/after context and heading hierarchy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="chunk"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// The expansion process:
    /// <list type="number">
    ///   <item><description>Validates input chunk and options.</description></item>
    ///   <item><description>Checks cache for existing expansion result.</description></item>
    ///   <item><description>If cache miss, queries sibling chunks from repository.</description></item>
    ///   <item><description>Resolves heading breadcrumb if <see cref="ContextOptions.IncludeHeadings"/> is true.</description></item>
    ///   <item><description>Caches result and publishes <c>ContextExpandedEvent</c>.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<ExpandedChunk> ExpandAsync(
        Chunk chunk,
        ContextOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the context cache for a specific document.
    /// </summary>
    /// <param name="documentId">Document ID to clear from cache.</param>
    /// <remarks>
    /// Call this when a document is re-indexed to prevent stale context.
    /// Typically invoked in response to <c>DocumentIndexedEvent</c>.
    /// </remarks>
    void InvalidateCache(Guid documentId);

    /// <summary>
    /// Clears the entire context cache.
    /// </summary>
    /// <remarks>
    /// Use sparingly; primarily for testing or memory pressure scenarios.
    /// Prefer <see cref="InvalidateCache"/> for targeted invalidation.
    /// </remarks>
    void ClearCache();
}
