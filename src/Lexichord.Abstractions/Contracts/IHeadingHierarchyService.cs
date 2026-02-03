// =============================================================================
// File: IHeadingHierarchyService.cs
// Project: Lexichord.Abstractions
// Description: Interface for resolving heading breadcrumbs from document chunks.
// =============================================================================
// LOGIC: Provides heading hierarchy navigation for chunks, enabling users to
//   understand where a chunk appears within the document's organizational
//   structure (e.g., "Chapter 1 > Section 2 > Subsection A").
// =============================================================================
// VERSION: v0.5.3c (Heading Hierarchy) - STUB INTERFACE
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
/// <b>License Gate:</b> Writer Pro (via <c>FeatureFlags.RAG.ContextWindow</c>)
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3c as part of The Context Window feature.
/// </para>
/// <para>
/// <b>Note:</b> This is a stub interface. Full implementation comes in v0.5.3c.
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
    /// <remarks>
    /// <para>
    /// The breadcrumb is ordered from outermost (root) to innermost (immediate parent).
    /// For example, a chunk under "# Auth > ## OAuth > ### Tokens" returns:
    /// <c>["Auth", "OAuth", "Tokens"]</c>
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
    /// This method is optional and may return null in stub implementations.
    /// </para>
    /// </remarks>
    Task<HeadingNode?> BuildHeadingTreeAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a node in the document heading hierarchy.
/// </summary>
/// <param name="Text">The heading text (without the level markers).</param>
/// <param name="Level">The heading level (1-6, corresponding to H1-H6).</param>
/// <param name="ChunkIndexStart">First chunk index covered by this heading.</param>
/// <param name="ChunkIndexEnd">Last chunk index covered by this heading (inclusive).</param>
/// <param name="Children">Child headings nested under this heading.</param>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.3c as part of The Context Window feature.
/// </para>
/// </remarks>
public record HeadingNode(
    string Text,
    int Level,
    int ChunkIndexStart,
    int ChunkIndexEnd,
    IReadOnlyList<HeadingNode> Children);
