// =============================================================================
// File: Citation.cs
// Project: Lexichord.Abstractions
// Description: Record representing attribution information for a retrieved chunk,
//              enabling source traceability for search results.
// =============================================================================
// LOGIC: Immutable record carrying complete provenance information for a chunk.
//   - ChunkId links back to the specific chunk in the vector store.
//   - DocumentPath is the absolute path to the source document.
//   - DocumentTitle provides a human-readable label (falls back to filename).
//   - StartOffset/EndOffset mark the chunk's position in the source document.
//   - Heading provides parent section context from chunk metadata.
//   - LineNumber enables precise source navigation (1-indexed, calculated from offset).
//   - IndexedAt records when the document was last indexed (for staleness checks).
//   - FileName is computed from DocumentPath for inline citation formatting.
//   - RelativePath enables workspace-relative display when available.
//   - HasHeading and HasLineNumber provide null-safe boolean checks for formatting logic.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents attribution information for a retrieved chunk.
/// Contains all data needed to trace content back to its source document.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Citation"/> captures the complete provenance of a search result,
/// enabling writers to create proper citations, navigate to source locations,
/// and verify the accuracy of retrieved information.
/// </para>
/// <para>
/// <b>Formatting:</b> Citations can be formatted in multiple styles via
/// <see cref="ICitationService.FormatCitation"/>:
/// <list type="bullet">
///   <item><description>Inline: <c>[document.md, §Heading]</c></description></item>
///   <item><description>Footnote: <c>[^1]: /path/to/doc.md:42</c></description></item>
///   <item><description>Markdown: <c>[Title](file:///path#L42)</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Staleness:</b> The <see cref="IndexedAt"/> timestamp enables downstream
/// validators (v0.5.2c) to detect when the source document has been modified
/// since the citation was created.
/// </para>
/// <para>
/// <b>Persistence:</b> Citations are transient and not stored in the database.
/// They are created on-demand from <see cref="SearchHit"/> instances and
/// cached in memory during a search session.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2a as part of the Citation Engine.
/// </para>
/// </remarks>
/// <param name="ChunkId">
/// Unique identifier of the source chunk. Links back to the chunk
/// record in the vector store for re-retrieval or validation.
/// </param>
/// <param name="DocumentPath">
/// Absolute path to the source document on disk.
/// Used for file access, staleness checks, and Markdown link generation.
/// </param>
/// <param name="DocumentTitle">
/// Display title for the source document. When the document has a
/// frontmatter title, that value is used; otherwise falls back to
/// the filename via <see cref="Path.GetFileName"/>.
/// </param>
/// <param name="StartOffset">
/// Zero-based character offset where the chunk begins in the source document.
/// Used together with <paramref name="EndOffset"/> to define the chunk boundary.
/// </param>
/// <param name="EndOffset">
/// Character offset where the chunk ends in the source document (exclusive).
/// Used together with <paramref name="StartOffset"/> to define the chunk boundary.
/// </param>
/// <param name="Heading">
/// Parent heading context from the chunk's structural metadata, if available.
/// Extracted from <see cref="ChunkMetadata.Heading"/> during citation creation.
/// Null when the chunk was not under a heading or heading metadata was not preserved.
/// </param>
/// <param name="LineNumber">
/// Starting line number in the source document (1-indexed).
/// Calculated by counting newline characters from the start of the file
/// to <paramref name="StartOffset"/>. Null when the source file is not
/// accessible or the offset exceeds the file length.
/// </param>
/// <param name="IndexedAt">
/// UTC timestamp when the source document was last successfully indexed.
/// Used by staleness validation (v0.5.2c) to compare against the file's
/// current modification time.
/// </param>
public record Citation(
    Guid ChunkId,
    string DocumentPath,
    string DocumentTitle,
    int StartOffset,
    int EndOffset,
    string? Heading,
    int? LineNumber,
    DateTime IndexedAt)
{
    /// <summary>
    /// Gets the filename portion of the document path.
    /// </summary>
    /// <remarks>
    /// LOGIC: Extracts the filename (including extension) from
    /// <see cref="DocumentPath"/> using <see cref="Path.GetFileName"/>.
    /// Used in inline citation formatting: <c>[filename.md, §Heading]</c>.
    /// </remarks>
    /// <value>
    /// The filename with extension, e.g., <c>"auth-guide.md"</c>.
    /// </value>
    public string FileName => Path.GetFileName(DocumentPath);

    /// <summary>
    /// Gets or sets the relative path from the workspace root, if available.
    /// </summary>
    /// <remarks>
    /// LOGIC: Set during citation creation when the workspace root is known.
    /// Provides a shorter, more readable path for display in UI components
    /// and citation formatting. When null, <see cref="DocumentPath"/> is used instead.
    /// </remarks>
    /// <value>
    /// A workspace-relative path, e.g., <c>"docs/api/auth.md"</c>, or null.
    /// </value>
    public string? RelativePath { get; init; }

    /// <summary>
    /// Gets whether this citation has heading context.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns true when <see cref="Heading"/> is a non-null, non-empty,
    /// non-whitespace string. Used by formatting logic to decide whether to
    /// include the <c>§Heading</c> suffix in inline citations.
    /// </remarks>
    /// <value>
    /// <c>true</c> if <see cref="Heading"/> contains meaningful content;
    /// otherwise, <c>false</c>.
    /// </value>
    public bool HasHeading => !string.IsNullOrEmpty(Heading);

    /// <summary>
    /// Gets whether this citation has a valid line number.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns true when <see cref="LineNumber"/> has a value greater
    /// than zero. Line numbers are 1-indexed, so a value of 0 would indicate
    /// an error in calculation. Used by formatting logic to decide whether to
    /// include the <c>:line</c> or <c>#Lline</c> suffix.
    /// </remarks>
    /// <value>
    /// <c>true</c> if <see cref="LineNumber"/> is a positive integer;
    /// otherwise, <c>false</c>.
    /// </value>
    public bool HasLineNumber => LineNumber.HasValue && LineNumber > 0;
}
