// =============================================================================
// File: CitationCopyFormat.cs
// Project: Lexichord.Abstractions
// Description: Enum defining the format types for citation copy operations.
// =============================================================================
// LOGIC: Identifies what kind of content was copied to the clipboard.
//   - FormattedCitation: A formatted citation string (Inline, Footnote, Markdown).
//   - ChunkText: Raw source text from the chunk.
//   - DocumentPath: Plain path string to the document.
//   - FileUri: Path as a file:// URI for linking.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Copy format types for citation clipboard operations.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CitationCopyFormat"/> identifies what kind of content was copied
/// to the clipboard during a citation copy operation. This is used in telemetry
/// events (<see cref="Events.CitationCopiedEvent"/>) to track user preferences
/// and usage patterns.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2d as part of the Citation Engine.
/// </para>
/// </remarks>
public enum CitationCopyFormat
{
    /// <summary>
    /// A formatted citation string.
    /// </summary>
    /// <remarks>
    /// LOGIC: The content copied is a formatted citation produced by
    /// <see cref="ICitationService.FormatCitation"/> in one of the available
    /// <see cref="CitationStyle"/> formats (Inline, Footnote, or Markdown).
    /// </remarks>
    FormattedCitation,

    /// <summary>
    /// Raw source text from the chunk.
    /// </summary>
    /// <remarks>
    /// LOGIC: The content copied is the original text from the source document,
    /// without any citation formatting. This is the chunk's raw content as
    /// retrieved during search.
    /// </remarks>
    ChunkText,

    /// <summary>
    /// Plain path string to the document.
    /// </summary>
    /// <remarks>
    /// LOGIC: The content copied is the absolute file path as a plain string,
    /// e.g., <c>/docs/api/auth-guide.md</c>. This format is suitable for
    /// pasting into file managers, terminals, or text-based references.
    /// </remarks>
    DocumentPath,

    /// <summary>
    /// Path as a file:// URI.
    /// </summary>
    /// <remarks>
    /// LOGIC: The content copied is a file URI with the <c>file://</c> scheme,
    /// e.g., <c>file:///docs/api/auth-guide.md</c>. This format is suitable
    /// for pasting into applications that recognize file URIs as clickable links.
    /// </remarks>
    FileUri
}
