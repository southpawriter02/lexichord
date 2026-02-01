// =============================================================================
// File: TextChunk.cs
// Project: Lexichord.Abstractions
// Description: Record representing a chunk of text extracted from a document,
//              including content, position offsets, and metadata.
// =============================================================================
// LOGIC: Immutable positional record carrying the chunk's text content and
//   its location within the source document.
//   - StartOffset/EndOffset enable navigation back to the source document.
//   - Length is computed from offsets (not content length, which may differ
//     due to trimming when PreserveWhitespace is false).
//   - HasContent filters whitespace-only chunks in processing pipelines.
//   - Preview provides a truncated view for UI display (100 chars + "...").
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents a chunk of text extracted from a document.
/// Includes the content, position information, and metadata.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="TextChunk"/> is the fundamental output of the chunking pipeline.
/// Each chunk carries its text content along with positional information that
/// enables navigation back to the source document and structural metadata
/// for context preservation during retrieval.
/// </para>
/// <para>
/// <b>Position Tracking:</b> The <see cref="StartOffset"/> and
/// <see cref="EndOffset"/> properties record the chunk's location within
/// the original document as character offsets. These offsets reference the
/// original document content, regardless of any trimming applied to
/// <see cref="Content"/>.
/// </para>
/// <para>
/// <b>Content vs. Length:</b> The <see cref="Length"/> property is computed
/// from <see cref="EndOffset"/> - <see cref="StartOffset"/>, which may differ
/// from <c>Content.Length</c> when content trimming is applied (i.e., when
/// <see cref="ChunkingOptions.PreserveWhitespace"/> is <c>false</c>).
/// </para>
/// </remarks>
/// <param name="Content">
/// The text content of this chunk. May be trimmed based on
/// <see cref="ChunkingOptions.PreserveWhitespace"/> settings.
/// </param>
/// <param name="StartOffset">
/// Zero-based character offset from the start of the original document.
/// </param>
/// <param name="EndOffset">
/// Character offset marking the end of this chunk (exclusive).
/// </param>
/// <param name="Metadata">
/// Additional context about this chunk's position and structure.
/// </param>
public record TextChunk(
    string Content,
    int StartOffset,
    int EndOffset,
    ChunkMetadata Metadata)
{
    /// <summary>
    /// Gets the length of this chunk in characters, based on document offsets.
    /// </summary>
    /// <value>
    /// The difference between <see cref="EndOffset"/> and <see cref="StartOffset"/>.
    /// </value>
    public int Length => EndOffset - StartOffset;

    /// <summary>
    /// Gets whether this chunk has meaningful (non-whitespace) content.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Content"/> contains non-whitespace characters;
    /// otherwise, <c>false</c>.
    /// </value>
    public bool HasContent => !string.IsNullOrWhiteSpace(Content);

    /// <summary>
    /// Gets a preview of the content, truncated to 100 characters.
    /// </summary>
    /// <value>
    /// The full content if 100 characters or fewer; otherwise, the first
    /// 100 characters followed by "...".
    /// </value>
    public string Preview => Content.Length <= 100
        ? Content
        : Content[..100] + "...";
}
