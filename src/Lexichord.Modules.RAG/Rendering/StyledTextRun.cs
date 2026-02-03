// =============================================================================
// File: StyledTextRun.cs
// Project: Lexichord.Modules.RAG
// Description: Record representing a styled segment of text for UI rendering.
// =============================================================================
// LOGIC: Immutable record pairing text content with its style.
//   - Used as output from IHighlightRenderer.Render().
//   - UI controls iterate over runs to build formatted text.
// =============================================================================
// VERSION: v0.5.6b (Query Term Highlighting)
// =============================================================================

namespace Lexichord.Modules.RAG.Rendering;

/// <summary>
/// A segment of text with associated styling for UI rendering.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="StyledTextRun"/> represents a contiguous segment of text that shares
/// the same styling. The <see cref="IHighlightRenderer"/> produces a sequence of
/// these runs from a <see cref="Lexichord.Abstractions.Contracts.Snippet"/>.
/// </para>
/// <para>
/// <b>UI Consumption:</b> Controls like <see cref="Controls.HighlightedSnippetControl"/>
/// iterate over runs and create corresponding <c>Run</c> elements in an
/// <c>InlineCollection</c>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6b as part of The Answer Preview feature.
/// </para>
/// </remarks>
/// <param name="Text">The text content of this run.</param>
/// <param name="Style">The styling to apply to this text.</param>
public record StyledTextRun(string Text, TextStyle Style)
{
    /// <summary>
    /// Gets whether this run contains any text.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Text"/> has one or more characters;
    /// otherwise, <c>false</c>.
    /// </value>
    public bool HasContent => !string.IsNullOrEmpty(Text);

    /// <summary>
    /// Gets the length of this run in characters.
    /// </summary>
    /// <value>
    /// The length of the <see cref="Text"/> property.
    /// </value>
    public int Length => Text.Length;
}
