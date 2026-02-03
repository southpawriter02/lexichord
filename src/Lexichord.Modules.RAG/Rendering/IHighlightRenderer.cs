// =============================================================================
// File: IHighlightRenderer.cs
// Project: Lexichord.Modules.RAG
// Description: Interface for platform-agnostic snippet highlight rendering.
// =============================================================================
// LOGIC: Defines contract for converting Snippet to styled text runs.
//   - Platform-agnostic to enable unit testing without UI dependencies.
//   - Returns styled runs that UI controls can render.
//   - Includes validation for highlight span bounds checking.
// =============================================================================
// VERSION: v0.5.6b (Query Term Highlighting)
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.RAG.Rendering;

/// <summary>
/// Renders <see cref="Snippet"/> content with styled highlights for UI display.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IHighlightRenderer"/> provides platform-agnostic rendering of
/// snippet content with query term highlighting. It transforms a <see cref="Snippet"/>
/// with its <see cref="HighlightSpan"/> positions into a sequence of
/// <see cref="StyledTextRun"/> objects.
/// </para>
/// <para>
/// <b>Platform Independence:</b> This interface produces data structures
/// (styled runs) rather than UI elements, enabling unit testing without
/// Avalonia dependencies and potential reuse across different UI frameworks.
/// </para>
/// <para>
/// <b>Styling:</b> The renderer applies styles based on <see cref="HighlightType"/>:
/// <list type="bullet">
///   <item><description><see cref="HighlightType.QueryMatch"/>: Bold text with exact match colors.</description></item>
///   <item><description><see cref="HighlightType.FuzzyMatch"/>: Italic text with fuzzy match colors.</description></item>
///   <item><description><see cref="HighlightType.KeyPhrase"/>: Key phrase foreground color.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6b as part of The Answer Preview feature.
/// </para>
/// </remarks>
public interface IHighlightRenderer
{
    /// <summary>
    /// Renders a snippet into styled text runs.
    /// </summary>
    /// <param name="snippet">The snippet containing text and highlight positions.</param>
    /// <param name="theme">The color theme to apply to highlights.</param>
    /// <returns>
    /// A read-only list of <see cref="StyledTextRun"/> objects representing
    /// the styled text segments in order.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The returned runs are ordered by their position in the snippet text.
    /// Adjacent runs with different styles are kept separate; runs with
    /// identical styles are not merged.
    /// </para>
    /// <para>
    /// If the snippet has <see cref="Snippet.IsTruncatedStart"/> or
    /// <see cref="Snippet.IsTruncatedEnd"/> set, ellipsis runs ("...") are
    /// included at the appropriate positions.
    /// </para>
    /// </remarks>
    IReadOnlyList<StyledTextRun> Render(Snippet snippet, HighlightTheme theme);

    /// <summary>
    /// Validates that all highlight spans are within the snippet text bounds.
    /// </summary>
    /// <param name="snippet">The snippet to validate.</param>
    /// <returns>
    /// <c>true</c> if all <see cref="HighlightSpan"/> positions are valid;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// A span is valid if:
    /// <list type="bullet">
    ///   <item><description><c>Start</c> is greater than or equal to 0.</description></item>
    ///   <item><description><c>Length</c> is greater than 0.</description></item>
    ///   <item><description><c>Start + Length</c> does not exceed <c>Text.Length</c>.</description></item>
    /// </list>
    /// </remarks>
    bool ValidateHighlights(Snippet snippet);
}
