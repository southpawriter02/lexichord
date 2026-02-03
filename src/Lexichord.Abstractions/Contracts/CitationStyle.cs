// =============================================================================
// File: CitationStyle.cs
// Project: Lexichord.Abstractions
// Description: Enum defining the available citation formatting styles.
// =============================================================================
// LOGIC: Defines three citation formatting styles for different publishing contexts.
//   - Inline: Best for academic papers and inline references.
//   - Footnote: Best for formal documents with numbered footnote references.
//   - Markdown: Best for Markdown documents and wiki-style linking.
//   - Default style is Inline (stored via ISettingsService, v0.5.2b).
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Citation formatting styles for source attribution display.
/// </summary>
/// <remarks>
/// <para>
/// Each style produces a distinct string representation of a <see cref="Citation"/>
/// optimized for a specific publishing context. The user's preferred style is stored
/// via <c>ISettingsService</c> (v0.1.6a) under the key <c>Citation.DefaultStyle</c>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2a as part of the Citation Engine.
/// </para>
/// </remarks>
public enum CitationStyle
{
    /// <summary>
    /// Inline format: <c>[document.md, §Heading]</c>
    /// </summary>
    /// <remarks>
    /// LOGIC: Produces a compact inline reference suitable for embedding within
    /// paragraph text. Includes the filename and, when available, the parent
    /// heading preceded by the section symbol (§).
    /// <para>
    /// <b>Example:</b> <c>[auth-guide.md, §Authentication]</c>
    /// </para>
    /// <para>
    /// <b>Best for:</b> Academic papers and inline references in technical documentation.
    /// </para>
    /// </remarks>
    Inline,

    /// <summary>
    /// Footnote format: <c>[^XXXXXXXX]: /path/to/doc.md:line</c>
    /// </summary>
    /// <remarks>
    /// LOGIC: Produces a numbered footnote reference using the first 8 characters
    /// of the chunk ID as the footnote identifier. Includes the full document path
    /// and, when available, the line number separated by a colon.
    /// <para>
    /// <b>Example:</b> <c>[^1a2b3c4d]: /docs/api/auth.md:42</c>
    /// </para>
    /// <para>
    /// <b>Best for:</b> Formal documents with footnote references.
    /// </para>
    /// </remarks>
    Footnote,

    /// <summary>
    /// Markdown link format: <c>[Title](file:///path#Lline)</c>
    /// </summary>
    /// <remarks>
    /// LOGIC: Produces a standard Markdown link with the document title as the
    /// link text and a <c>file://</c> URI as the target. When a line number is
    /// available, it is appended as a fragment identifier (<c>#L42</c>).
    /// Spaces in the path are percent-encoded (<c>%20</c>).
    /// <para>
    /// <b>Example:</b> <c>[OAuth Guide](file:///docs/api/auth.md#L42)</c>
    /// </para>
    /// <para>
    /// <b>Best for:</b> Markdown documents and wiki-style linking.
    /// </para>
    /// </remarks>
    Markdown
}
