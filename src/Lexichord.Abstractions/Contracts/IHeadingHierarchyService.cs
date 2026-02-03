// =============================================================================
// File: IHeadingHierarchyService.cs
// Project: Lexichord.Abstractions
// Description: Interface for resolving heading breadcrumbs from document chunks.
// =============================================================================
// LOGIC: Provides heading hierarchy navigation for chunks, enabling users to
//   understand where a chunk appears within the document's organizational
//   structure (e.g., "Chapter 1 > Section 2 > Subsection A").
// =============================================================================
// VERSION: v0.5.3c (Heading Hierarchy)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for resolving heading hierarchy breadcrumbs from document chunks.
/// </summary>
/// <remarks>
/// <para>
/// The heading hierarchy service builds a structural understanding of documents
/// based on their heading levels (H1-H6 in Markdown/HTML). This enables:
/// <list type="bullet">
///   <item><description>Breadcrumb navigation in search results.</description></item>
///   <item><description>Section-aware context expansion.</description></item>
///   <item><description>Document outline generation.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Caching:</b> Heading trees are cached per document to avoid repeated database
/// queries. Cache is automatically invalidated when documents are re-indexed or removed.
/// </para>
/// <para>
/// <b>License Gate:</b> Writer Pro (via <c>FeatureFlags.RAG.ContextWindow</c>)
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3c as part of The Context Window feature.
/// </para>
/// </remarks>
public interface IHeadingHierarchyService
{
    /// <summary>
    /// Gets the heading breadcrumb trail for a chunk at the specified index.
    /// </summary>
    /// <param name="documentId">The document containing the chunk.</param>
    /// <param name="chunkIndex">The zero-based index of the chunk within the document.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// A list of heading strings representing the path from root to the chunk's
    /// immediate parent heading. Empty if no heading context exists.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if chunkIndex is negative.</exception>
    /// <remarks>
    /// <para>
    /// The breadcrumb is ordered from outermost (root) to innermost (immediate parent).
    /// For example, a chunk under "# Auth > ## OAuth > ### Tokens" returns:
    /// <c>["Auth", "OAuth", "Tokens"]</c>
    /// </para>
    /// <para>
    /// <b>Edge cases:</b>
    /// <list type="bullet">
    ///   <item><description>Chunk before first heading: returns empty list</description></item>
    ///   <item><description>Chunk after last heading: belongs to last heading</description></item>
    ///   <item><description>Document without headings: returns empty list</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var breadcrumb = await headingService.GetBreadcrumbAsync(docId, chunkIndex);
    /// var displayPath = string.Join(" > ", breadcrumb);
    /// // Output: "Authentication > OAuth > Token Refresh"
    /// </code>
    /// </example>
    Task<IReadOnlyList<string>> GetBreadcrumbAsync(
        Guid documentId,
        int chunkIndex,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the complete heading tree for a document.
    /// </summary>
    /// <param name="documentId">The document to analyze.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// The root of the heading tree, or null if the document has no headings.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The heading tree represents the document's structural hierarchy.
    /// Each node contains the heading text, level, and child headings.
    /// </para>
    /// <para>
    /// <b>Tree structure:</b>
    /// <list type="bullet">
    ///   <item><description>H1 headings are at root level</description></item>
    ///   <item><description>H2 headings are children of preceding H1</description></item>
    ///   <item><description>H3 headings are children of preceding H2 (or H1 if H2 is skipped)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<HeadingNode?> BuildHeadingTreeAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the heading tree cache for a specific document.
    /// </summary>
    /// <param name="documentId">Document ID to clear from cache.</param>
    /// <remarks>
    /// <para>
    /// Call this method when a document is re-indexed or its content changes.
    /// This is typically handled automatically via MediatR event handlers for
    /// <c>DocumentIndexedEvent</c> and <c>DocumentRemovedFromIndexEvent</c>.
    /// </para>
    /// </remarks>
    void InvalidateCache(Guid documentId);

    /// <summary>
    /// Clears the entire heading tree cache.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use sparingly - this clears all cached heading trees and forces
    /// database queries for subsequent breadcrumb requests.
    /// </para>
    /// </remarks>
    void ClearCache();
}

/// <summary>
/// Represents a node in the document heading hierarchy.
/// </summary>
/// <param name="Id">Unique identifier for this heading (typically the chunk ID).</param>
/// <param name="Text">The heading text (without the level markers).</param>
/// <param name="Level">The heading level (1-6, corresponding to H1-H6).</param>
/// <param name="ChunkIndex">The chunk index where this heading appears.</param>
/// <param name="Children">Child headings nested under this heading.</param>
/// <remarks>
/// <para>
/// <b>Hierarchy Rules:</b>
/// <list type="bullet">
///   <item><description>Level 1 = H1 (# in Markdown)</description></item>
///   <item><description>Level 2 = H2 (## in Markdown)</description></item>
///   <item><description>And so on up to Level 6</description></item>
/// </list>
/// </para>
/// <para>
/// Children are headings with higher level numbers that appear after this
/// heading and before the next heading of equal or lower level.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3c as part of The Context Window feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Document structure:
/// // # Authentication (level 1, index 0)
/// //   ## OAuth (level 2, index 5)
/// //     ### Token Refresh (level 3, index 10)
/// //   ## Basic Auth (level 2, index 15)
/// // # Authorization (level 1, index 20)
///
/// var tree = await headingService.BuildHeadingTreeAsync(docId);
/// // tree.Text = "Authentication"
/// // tree.Children[0].Text = "OAuth"
/// // tree.Children[0].Children[0].Text = "Token Refresh"
/// </code>
/// </example>
public record HeadingNode(
    Guid Id,
    string Text,
    int Level,
    int ChunkIndex,
    IReadOnlyList<HeadingNode> Children)
{
    /// <summary>
    /// Creates a new leaf node with no children.
    /// </summary>
    /// <param name="id">Unique identifier for this heading.</param>
    /// <param name="text">The heading text.</param>
    /// <param name="level">The heading level (1-6).</param>
    /// <param name="chunkIndex">The chunk index where this heading appears.</param>
    /// <returns>A new <see cref="HeadingNode"/> with no children.</returns>
    public static HeadingNode Leaf(Guid id, string text, int level, int chunkIndex) =>
        new(id, text, level, chunkIndex, Array.Empty<HeadingNode>());

    /// <summary>
    /// Gets whether this node has child headings.
    /// </summary>
    public bool HasChildren => Children.Count > 0;
}
