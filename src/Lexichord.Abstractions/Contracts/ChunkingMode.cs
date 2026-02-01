// =============================================================================
// File: ChunkingMode.cs
// Project: Lexichord.Abstractions
// Description: Enum defining the available chunking strategies for document
//              processing in the RAG pipeline.
// =============================================================================
// LOGIC: Four chunking modes covering common document segmentation approaches.
//   - FixedSize: Character-count based, suitable for unstructured text.
//   - Paragraph: Double-newline boundary detection, preserves natural structure.
//   - MarkdownHeader: Hierarchical header-based, preserves document structure.
//   - Semantic: Placeholder for future NLP-based topic boundary detection.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Available chunking strategies for document processing.
/// </summary>
/// <remarks>
/// <para>
/// Each mode represents a different algorithm for splitting document content
/// into chunks suitable for embedding and semantic search. The choice of mode
/// depends on the document structure and desired retrieval precision.
/// </para>
/// <para>
/// <b>Strategy Selection Guide:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="FixedSize"/>: Best for unstructured text or when consistent chunk sizes are required.</description></item>
///   <item><description><see cref="Paragraph"/>: Best for prose with clear paragraph structure.</description></item>
///   <item><description><see cref="MarkdownHeader"/>: Best for Markdown documents with header hierarchy.</description></item>
///   <item><description><see cref="Semantic"/>: Reserved for future NLP-based chunking (not yet implemented).</description></item>
/// </list>
/// </remarks>
public enum ChunkingMode
{
    /// <summary>
    /// Split by character count with overlap.
    /// </summary>
    /// <remarks>
    /// Suitable for unstructured text or when consistent chunk sizes are needed.
    /// Chunks are created at fixed character intervals with configurable overlap
    /// to preserve context across boundaries. Word boundary respect can be enabled
    /// to avoid splitting mid-word.
    /// </remarks>
    FixedSize = 0,

    /// <summary>
    /// Split on paragraph boundaries (double newlines).
    /// </summary>
    /// <remarks>
    /// Preserves natural text structure by detecting paragraph boundaries via
    /// double-newline (<c>\n\n</c>) separators. Short paragraphs are merged to
    /// meet minimum size requirements, and long paragraphs are split using
    /// fixed-size fallback.
    /// </remarks>
    Paragraph = 1,

    /// <summary>
    /// Split on Markdown headers for hierarchical chunking.
    /// </summary>
    /// <remarks>
    /// Preserves document structure by creating chunks at Markdown header
    /// boundaries (<c>#</c>, <c>##</c>, <c>###</c>, etc.). Each chunk includes
    /// content from a heading until the next heading of the same or higher level.
    /// Header text is preserved in <see cref="ChunkMetadata.Heading"/> for
    /// context injection during retrieval.
    /// </remarks>
    MarkdownHeader = 2,

    /// <summary>
    /// Split using semantic analysis (future).
    /// </summary>
    /// <remarks>
    /// Uses NLP to find natural topic boundaries within text. This mode is
    /// reserved for future implementation and will throw
    /// <see cref="System.NotSupportedException"/> if selected.
    /// </remarks>
    Semantic = 3
}
