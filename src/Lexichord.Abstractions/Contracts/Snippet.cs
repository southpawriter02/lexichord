// =============================================================================
// File: Snippet.cs
// Project: Lexichord.Abstractions
// Description: Record representing a contextual text snippet with highlights.
// =============================================================================
// LOGIC: Immutable record for extracted snippet content.
//   - Text contains the extracted content, possibly with "..." for truncation.
//   - Highlight positions are relative to Text, not the original chunk.
//   - StartOffset enables navigation back to the source chunk position.
//   - Truncation flags indicate whether content was removed at boundaries.
// =============================================================================
// VERSION: v0.5.6a (Snippet Extraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// A contextual text snippet extracted from a chunk with highlighted matches.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Snippet"/> is the primary output of <see cref="ISnippetService"/>,
/// containing the extracted text, highlight information, and metadata about
/// the extraction process.
/// </para>
/// <para>
/// <b>Text Content:</b> The <see cref="Text"/> property contains the extracted
/// content, which may include "..." markers when truncated. UI components should
/// render this text directly without additional modification.
/// </para>
/// <para>
/// <b>Highlight Positions:</b> All positions in <see cref="Highlights"/> are
/// relative to the <see cref="Text"/> property, not the original chunk content.
/// This allows direct application of highlights without offset translation.
/// </para>
/// <para>
/// <b>Source Tracking:</b> The <see cref="StartOffset"/> property records the
/// position in the original chunk where this snippet begins, enabling navigation
/// back to the source for detailed viewing.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6a as part of The Answer Preview feature.
/// </para>
/// </remarks>
/// <param name="Text">The extracted snippet text, may include "..." for truncation.</param>
/// <param name="Highlights">Highlight spans within the text, positions are relative to Text.</param>
/// <param name="StartOffset">Position in the original chunk where this snippet starts.</param>
/// <param name="IsTruncatedStart">True if content was removed before the snippet.</param>
/// <param name="IsTruncatedEnd">True if content was removed after the snippet.</param>
public record Snippet(
    string Text,
    IReadOnlyList<HighlightSpan> Highlights,
    int StartOffset,
    bool IsTruncatedStart,
    bool IsTruncatedEnd)
{
    /// <summary>
    /// Gets the length of this snippet in characters.
    /// </summary>
    /// <value>
    /// The length of the <see cref="Text"/> property.
    /// </value>
    public int Length => Text.Length;

    /// <summary>
    /// Gets whether this snippet has any highlighted regions.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Highlights"/> contains one or more spans;
    /// otherwise, <c>false</c>.
    /// </value>
    public bool HasHighlights => Highlights.Count > 0;

    /// <summary>
    /// Gets whether this snippet was truncated at either end.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="IsTruncatedStart"/> or <see cref="IsTruncatedEnd"/>
    /// is <c>true</c>; otherwise, <c>false</c>.
    /// </value>
    public bool IsTruncated => IsTruncatedStart || IsTruncatedEnd;

    /// <summary>
    /// An empty snippet with no content or highlights.
    /// </summary>
    /// <remarks>
    /// Returned when the source content is empty or null.
    /// </remarks>
    public static Snippet Empty => new(
        string.Empty,
        Array.Empty<HighlightSpan>(),
        StartOffset: 0,
        IsTruncatedStart: false,
        IsTruncatedEnd: false);

    /// <summary>
    /// Creates a snippet from plain text without highlights.
    /// </summary>
    /// <param name="text">The text content to wrap.</param>
    /// <returns>
    /// A new <see cref="Snippet"/> containing the text with no highlights
    /// and no truncation.
    /// </returns>
    /// <remarks>
    /// Useful for creating snippets when no query matching is performed,
    /// such as displaying document previews without search context.
    /// </remarks>
    public static Snippet FromPlainText(string text) =>
        new(
            text,
            Array.Empty<HighlightSpan>(),
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);
}
