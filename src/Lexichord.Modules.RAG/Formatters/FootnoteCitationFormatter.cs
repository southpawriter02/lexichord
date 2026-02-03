// =============================================================================
// File: FootnoteCitationFormatter.cs
// Project: Lexichord.Modules.RAG
// Description: Formats citations in footnote style: [^id]: /path/to/doc.md:line
// =============================================================================
// LOGIC: Produces Markdown footnote citation references suitable for formal
//   documents with numbered footnote references (v0.5.2b).
//   - Format: [^XXXXXXXX]: /path/to/doc.md:line or [^XXXXXXXX]: /path/to/doc.md
//   - Uses the first 8 characters of ChunkId (hex, no hyphens) as footnote identifier.
//   - Line number suffix (:line) is omitted when LineNumber is not available.
//   - FormatForClipboard returns the same output as Format for this style.
//   - Thread-safe: no mutable state, registered as singleton.
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.RAG.Formatters;

/// <summary>
/// Formats citations in footnote style: <c>[^id]: /path/to/doc.md:line</c>.
/// </summary>
/// <remarks>
/// <para>
/// The footnote format produces Markdown-compatible footnote references using
/// a short identifier derived from the chunk ID. This format is designed for
/// formal documents that collect references at the end of the document.
/// </para>
/// <para>
/// <b>Format Pattern:</b>
/// <list type="bullet">
///   <item><description>With line number: <c>[^1a2b3c4d]: /docs/api/auth.md:42</c></description></item>
///   <item><description>Without line number: <c>[^1a2b3c4d]: /docs/api/auth.md</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Footnote Identifier:</b> The identifier is the first 8 characters of the
/// <see cref="Citation.ChunkId"/> formatted as a hex string without hyphens
/// (using the "N" format specifier). This provides a unique but concise ID
/// for each footnote reference.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This formatter is stateless and thread-safe. It is
/// registered as a singleton in the DI container.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2b as part of the Citation Engine.
/// </para>
/// </remarks>
public sealed class FootnoteCitationFormatter : ICitationFormatter
{
    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Returns <see cref="CitationStyle.Footnote"/> to identify this formatter
    /// in the <c>CitationFormatterRegistry</c> style-to-formatter dictionary.
    /// </remarks>
    public CitationStyle Style => CitationStyle.Footnote;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Display label shown in the settings panel radio buttons and
    /// the context menu "Copy as..." submenu.
    /// </remarks>
    public string DisplayName => "Footnote";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Secondary description text shown beneath the display name
    /// in the settings panel, providing context about the format.
    /// </remarks>
    public string Description => "Markdown footnote format";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Sample output shown in the settings panel preview area,
    /// illustrating the format pattern with placeholder content.
    /// </remarks>
    public string Example => "[^1a2b3c4d]: /docs/file.md:42";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Footnote formatting algorithm:
    /// <list type="number">
    ///   <item><description>Validate that citation is not null.</description></item>
    ///   <item><description>Generate the short identifier from ChunkId (first 8 hex chars).</description></item>
    ///   <item><description>Check if line number is available via <see cref="Citation.HasLineNumber"/>.</description></item>
    ///   <item><description>Build the line suffix: <c>:{LineNumber}</c> or empty string.</description></item>
    ///   <item><description>Combine: <c>[^{shortId}]: {DocumentPath}{line}</c>.</description></item>
    /// </list>
    /// </remarks>
    public string Format(Citation citation)
    {
        ArgumentNullException.ThrowIfNull(citation);

        // LOGIC: Extract the first 8 characters of the ChunkId as a hex string.
        // The "N" format specifier produces a 32-character hex string without hyphens.
        // Taking the first 8 characters provides a concise but unique footnote identifier.
        var shortId = citation.ChunkId.ToString("N")[..8];

        // LOGIC: Include line number suffix only when a valid line number is available.
        var line = citation.HasLineNumber
            ? $":{citation.LineNumber}"
            : string.Empty;

        return $"[^{shortId}]: {citation.DocumentPath}{line}";
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: For footnote style, the clipboard format is identical to the display format.
    /// The footnote definition line is self-contained and ready for pasting.
    /// </remarks>
    public string FormatForClipboard(Citation citation) => Format(citation);
}
