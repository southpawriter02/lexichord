// =============================================================================
// File: ChunkMetadata.cs
// Project: Lexichord.Abstractions
// Description: Record containing metadata about a chunk's context within its
//              source document, including position, heading, and level info.
// =============================================================================
// LOGIC: Positional record with optional heading context for header-based
//   chunking strategies. Computed properties derive navigation helpers from
//   Index and TotalChunks.
//   - TotalChunks is an init-only property set after all chunks are created.
//   - IsFirst/IsLast enable boundary detection in processing pipelines.
//   - RelativePosition (0.0-1.0) supports progress and positioning UI.
//   - HasHeading indicates whether this chunk has section context.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Metadata about a chunk's context within its source document.
/// Provides information for navigation and context preservation.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="TextChunk"/> carries a <see cref="ChunkMetadata"/> instance
/// describing its position within the document and any structural context
/// (such as a Markdown heading) that applies to it.
/// </para>
/// <para>
/// <b>Heading Context:</b> When using <see cref="ChunkingMode.MarkdownHeader"/>,
/// the <see cref="Heading"/> and <see cref="Level"/> properties capture the
/// section header that contains this chunk, enabling context injection during
/// retrieval.
/// </para>
/// <para>
/// <b>Navigation Helpers:</b> The <see cref="IsFirst"/>, <see cref="IsLast"/>,
/// and <see cref="RelativePosition"/> properties are computed from
/// <see cref="Index"/> and <see cref="TotalChunks"/>, providing convenient
/// boundary detection and progress tracking.
/// </para>
/// </remarks>
/// <param name="Index">
/// Zero-based index of this chunk within the document.
/// </param>
/// <param name="Heading">
/// Section heading this chunk belongs to, if applicable.
/// Null for chunks without heading context (e.g., when using
/// <see cref="ChunkingMode.FixedSize"/> or <see cref="ChunkingMode.Paragraph"/>).
/// </param>
/// <param name="Level">
/// Heading level (1-6) if this chunk is under a Markdown header.
/// Zero if no heading applies.
/// </param>
public record ChunkMetadata(
    int Index,
    string? Heading = null,
    int Level = 0)
{
    /// <summary>
    /// Total number of chunks in the document.
    /// Set after all chunks are created during the chunking process.
    /// </summary>
    /// <remarks>
    /// This property is set via init-only syntax after the chunking strategy
    /// completes, enabling <see cref="IsLast"/> and <see cref="RelativePosition"/>
    /// to return meaningful values.
    /// </remarks>
    public int TotalChunks { get; init; }

    /// <summary>
    /// Gets whether this is the first chunk in the document.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Index"/> is zero; otherwise, <c>false</c>.
    /// </value>
    public bool IsFirst => Index == 0;

    /// <summary>
    /// Gets whether this is the last chunk in the document.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Index"/> equals <see cref="TotalChunks"/> - 1
    /// and <see cref="TotalChunks"/> is greater than zero; otherwise, <c>false</c>.
    /// </value>
    public bool IsLast => TotalChunks > 0 && Index == TotalChunks - 1;

    /// <summary>
    /// Gets the relative position (0.0 to 1.0) within the document.
    /// </summary>
    /// <value>
    /// A value from 0.0 (start) to approaching 1.0 (end), or 0.0 if
    /// <see cref="TotalChunks"/> is zero.
    /// </value>
    public double RelativePosition => TotalChunks > 0
        ? (double)Index / TotalChunks
        : 0.0;

    /// <summary>
    /// Gets whether this chunk has heading context.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Heading"/> is not null or empty;
    /// otherwise, <c>false</c>.
    /// </value>
    public bool HasHeading => !string.IsNullOrEmpty(Heading);
}
