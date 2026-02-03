// =============================================================================
// File: InlineCitationFormatter.cs
// Project: Lexichord.Modules.RAG
// Description: Formats citations in inline style: [document.md, §Heading]
// =============================================================================
// LOGIC: Produces compact inline citation references suitable for embedding
//   within paragraph text (v0.5.2b).
//   - Format: [filename.md, §Heading] or [filename.md] when no heading present.
//   - Uses Citation.FileName (short name) for conciseness, not the full path.
//   - The section symbol (§) precedes the heading when present.
//   - FormatForClipboard returns the same output as Format for this style.
//   - Thread-safe: no mutable state, registered as singleton.
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.RAG.Formatters;

/// <summary>
/// Formats citations in inline style: <c>[document.md, §Heading]</c>.
/// </summary>
/// <remarks>
/// <para>
/// The inline format is designed for embedding within paragraph text, providing
/// a compact reference to the source document and its heading context. This is
/// the default citation style and is most commonly used in academic papers and
/// inline references in technical documentation.
/// </para>
/// <para>
/// <b>Format Pattern:</b>
/// <list type="bullet">
///   <item><description>With heading: <c>[auth-guide.md, §Authentication]</c></description></item>
///   <item><description>Without heading: <c>[auth-guide.md]</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This formatter is stateless and thread-safe. It is
/// registered as a singleton in the DI container.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2b as part of the Citation Engine.
/// </para>
/// </remarks>
public sealed class InlineCitationFormatter : ICitationFormatter
{
    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Returns <see cref="CitationStyle.Inline"/> to identify this formatter
    /// in the <c>CitationFormatterRegistry</c> style-to-formatter dictionary.
    /// </remarks>
    public CitationStyle Style => CitationStyle.Inline;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Display label shown in the settings panel radio buttons and
    /// the context menu "Copy as..." submenu.
    /// </remarks>
    public string DisplayName => "Inline";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Secondary description text shown beneath the display name
    /// in the settings panel, providing context about the format.
    /// </remarks>
    public string Description => "Compact reference in brackets";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Sample output shown in the settings panel preview area,
    /// illustrating the format pattern with placeholder content.
    /// </remarks>
    public string Example => "[document.md, §Heading]";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Inline formatting algorithm:
    /// <list type="number">
    ///   <item><description>Validate that citation is not null.</description></item>
    ///   <item><description>Check if heading context is available via <see cref="Citation.HasHeading"/>.</description></item>
    ///   <item><description>Build the heading suffix: <c>, §{Heading}</c> or empty string.</description></item>
    ///   <item><description>Combine filename and heading: <c>[{FileName}{heading}]</c>.</description></item>
    /// </list>
    /// </remarks>
    public string Format(Citation citation)
    {
        ArgumentNullException.ThrowIfNull(citation);

        // LOGIC: Include heading suffix only when heading context is available.
        // The section symbol (§) is a standard typographic marker for section references.
        var heading = citation.HasHeading
            ? $", §{citation.Heading}"
            : string.Empty;

        return $"[{citation.FileName}{heading}]";
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: For inline style, the clipboard format is identical to the display format.
    /// No additional context or whitespace is needed for clipboard usage.
    /// </remarks>
    public string FormatForClipboard(Citation citation) => Format(citation);
}
