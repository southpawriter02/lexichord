// =============================================================================
// File: ICitationFormatter.cs
// Project: Lexichord.Abstractions
// Description: Interface defining the contract for style-specific citation formatters.
// =============================================================================
// LOGIC: Defines the abstraction for citation formatting strategies (v0.5.2b).
//   - Each implementation handles a single CitationStyle (Inline, Footnote, Markdown).
//   - Style: Identifies which CitationStyle this formatter produces.
//   - DisplayName: Human-readable label for UI (settings panel, context menu).
//   - Description: Short description of the format for tooltips/settings.
//   - Example: Sample output string for preview in settings UI.
//   - Format: Produces the display string for the citation in this style.
//   - FormatForClipboard: Produces the clipboard-ready string (may differ from display).
//   - Implementations are registered as singletons in DI and resolved via
//     IEnumerable<ICitationFormatter> by the CitationFormatterRegistry.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Formatter for a specific citation style.
/// </summary>
/// <remarks>
/// <para>
/// Each implementation of <see cref="ICitationFormatter"/> produces formatted citation
/// strings for a single <see cref="CitationStyle"/>. The three built-in formatters are:
/// <list type="bullet">
///   <item><description><b>Inline:</b> <c>[document.md, §Heading]</c></description></item>
///   <item><description><b>Footnote:</b> <c>[^1a2b3c4d]: /path/to/doc.md:42</c></description></item>
///   <item><description><b>Markdown:</b> <c>[Title](file:///path#L42)</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Registry:</b> All registered formatters are collected by
/// <c>CitationFormatterRegistry</c> (v0.5.2b) which provides style lookup,
/// user preference management, and the preferred formatter accessor.
/// </para>
/// <para>
/// <b>Extensibility:</b> Teams+ users may register custom formatters in future
/// versions. The interface supports this by keeping the contract open for new
/// <see cref="CitationStyle"/> values and custom implementations.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe as they are registered
/// as singletons and may be invoked concurrently from multiple search operations.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2b as part of the Citation Engine.
/// </para>
/// </remarks>
public interface ICitationFormatter
{
    /// <summary>
    /// Gets the style this formatter produces.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used by <c>CitationFormatterRegistry</c> to build a lookup dictionary
    /// mapping <see cref="CitationStyle"/> values to their corresponding formatters.
    /// Each formatter must return a unique style value.
    /// </remarks>
    /// <value>
    /// The <see cref="CitationStyle"/> enum value that this formatter handles.
    /// </value>
    CitationStyle Style { get; }

    /// <summary>
    /// Gets the display name for the style (used in UI).
    /// </summary>
    /// <remarks>
    /// LOGIC: Shown in the settings panel radio button labels and in the
    /// context menu "Copy as..." submenu. Should be concise (1-2 words).
    /// </remarks>
    /// <value>
    /// A human-readable name, e.g., <c>"Inline"</c>, <c>"Footnote"</c>, <c>"Markdown Link"</c>.
    /// </value>
    string DisplayName { get; }

    /// <summary>
    /// Gets a short description of the format.
    /// </summary>
    /// <remarks>
    /// LOGIC: Displayed as secondary text in the settings panel beneath the
    /// display name. Provides context about when this format is appropriate.
    /// </remarks>
    /// <value>
    /// A brief description, e.g., <c>"Compact reference in brackets"</c>.
    /// </value>
    string Description { get; }

    /// <summary>
    /// Gets an example of the format output.
    /// </summary>
    /// <remarks>
    /// LOGIC: Displayed in the settings panel as a preview of what the formatted
    /// citation looks like. Uses placeholder content to illustrate the format pattern.
    /// </remarks>
    /// <value>
    /// A sample formatted string, e.g., <c>"[document.md, §Heading]"</c>.
    /// </value>
    string Example { get; }

    /// <summary>
    /// Formats a citation for display in the UI.
    /// </summary>
    /// <param name="citation">
    /// The citation to format. Must not be null.
    /// </param>
    /// <returns>
    /// The formatted citation string appropriate for this style.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="citation"/> is null.
    /// </exception>
    /// <remarks>
    /// LOGIC: Produces the primary display representation of the citation.
    /// The output should be human-readable and suitable for inline display
    /// in search result panels and tooltips.
    /// </remarks>
    string Format(Citation citation);

    /// <summary>
    /// Formats a citation for clipboard copy (may differ from display).
    /// </summary>
    /// <param name="citation">
    /// The citation to format. Must not be null.
    /// </param>
    /// <returns>
    /// The clipboard-ready citation string. For most styles this is identical
    /// to <see cref="Format"/>, but clipboard output may include additional
    /// context (e.g., surrounding whitespace or prefix markers).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="citation"/> is null.
    /// </exception>
    /// <remarks>
    /// LOGIC: Produces the clipboard representation of the citation, used by
    /// <c>ICitationClipboardService</c> (v0.5.2d) when copying citations.
    /// For the three built-in styles, this returns the same string as
    /// <see cref="Format"/>. Custom formatters may override this behavior.
    /// </remarks>
    string FormatForClipboard(Citation citation);
}
