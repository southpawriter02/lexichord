// =============================================================================
// File: MarkdownCitationFormatter.cs
// Project: Lexichord.Modules.RAG
// Description: Formats citations as Markdown links: [Title](file:///path#L42)
// =============================================================================
// LOGIC: Produces standard Markdown link citation references suitable for
//   Markdown documents and wiki-style linking (v0.5.2b).
//   - Format: [Title](file:///path#Lline) or [Title](file:///path)
//   - Uses Citation.DocumentTitle as the link text.
//   - Uses file:// scheme URI with the document path as the target.
//   - Line number fragment (#L42) is appended when LineNumber is available.
//   - Spaces in the path are percent-encoded as %20 for valid URLs.
//   - FormatForClipboard returns the same output as Format for this style.
//   - Thread-safe: no mutable state, registered as singleton.
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.RAG.Formatters;

/// <summary>
/// Formats citations as Markdown links: <c>[Title](file:///path#L42)</c>.
/// </summary>
/// <remarks>
/// <para>
/// The Markdown format produces standard Markdown link syntax with a <c>file://</c>
/// scheme URI, making the citation clickable in Markdown renderers and editors
/// that support file links. This format is ideal for Markdown documents, wikis,
/// and documentation systems.
/// </para>
/// <para>
/// <b>Format Pattern:</b>
/// <list type="bullet">
///   <item><description>With line number: <c>[OAuth Guide](file:///docs/api/auth.md#L42)</c></description></item>
///   <item><description>Without line number: <c>[OAuth Guide](file:///docs/api/auth.md)</c></description></item>
///   <item><description>With spaces: <c>[My Guide](file:///docs/my%20guide.md#L42)</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>URL Encoding:</b> Spaces in the document path are replaced with <c>%20</c>
/// to produce valid URIs. This follows the convention used by GitHub, VS Code,
/// and other tools that support file-scheme links.
/// </para>
/// <para>
/// <b>Line Fragment:</b> The <c>#L42</c> fragment identifier follows the convention
/// used by GitHub and VS Code for line-level linking within files.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This formatter is stateless and thread-safe. It is
/// registered as a singleton in the DI container.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2b as part of the Citation Engine.
/// </para>
/// </remarks>
public sealed class MarkdownCitationFormatter : ICitationFormatter
{
    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Returns <see cref="CitationStyle.Markdown"/> to identify this formatter
    /// in the <c>CitationFormatterRegistry</c> style-to-formatter dictionary.
    /// </remarks>
    public CitationStyle Style => CitationStyle.Markdown;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Display label shown in the settings panel radio buttons and
    /// the context menu "Copy as..." submenu.
    /// </remarks>
    public string DisplayName => "Markdown Link";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Secondary description text shown beneath the display name
    /// in the settings panel, providing context about the format.
    /// </remarks>
    public string Description => "Clickable file link with line anchor";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Sample output shown in the settings panel preview area,
    /// illustrating the format pattern with placeholder content.
    /// </remarks>
    public string Example => "[My Document](file:///docs/file.md#L42)";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Markdown formatting algorithm:
    /// <list type="number">
    ///   <item><description>Validate that citation is not null.</description></item>
    ///   <item><description>Check if line number is available via <see cref="Citation.HasLineNumber"/>.</description></item>
    ///   <item><description>Build the fragment: <c>#L{LineNumber}</c> or empty string.</description></item>
    ///   <item><description>URL-encode spaces in the document path (<c>%20</c>).</description></item>
    ///   <item><description>Combine: <c>[{DocumentTitle}](file://{escapedPath}{fragment})</c>.</description></item>
    /// </list>
    /// </remarks>
    public string Format(Citation citation)
    {
        ArgumentNullException.ThrowIfNull(citation);

        // LOGIC: Append line number fragment when available.
        // The #L prefix follows GitHub/VS Code convention for line-level linking.
        var fragment = citation.HasLineNumber
            ? $"#L{citation.LineNumber}"
            : string.Empty;

        // LOGIC: Escape spaces in the path for valid URL construction.
        // Only spaces are encoded here as they are the most common problematic
        // character in file paths. Full URI encoding is not applied to preserve
        // readability for other path characters (slashes, dots, hyphens).
        var escapedPath = citation.DocumentPath.Replace(" ", "%20");

        return $"[{citation.DocumentTitle}](file://{escapedPath}{fragment})";
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: For Markdown style, the clipboard format is identical to the display format.
    /// The Markdown link is ready for pasting into any Markdown-compatible editor.
    /// </remarks>
    public string FormatForClipboard(Citation citation) => Format(citation);
}
