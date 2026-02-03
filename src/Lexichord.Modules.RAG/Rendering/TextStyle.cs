// =============================================================================
// File: TextStyle.cs
// Project: Lexichord.Modules.RAG
// Description: Record defining text styling properties for highlighted text.
// =============================================================================
// LOGIC: Immutable record for text styling configuration.
//   - Supports bold and italic text weight/style.
//   - Optional foreground and background colors as hex strings.
//   - Static presets for common styles (Default, ExactMatch, FuzzyMatch).
// =============================================================================
// VERSION: v0.5.6b (Query Term Highlighting)
// =============================================================================

namespace Lexichord.Modules.RAG.Rendering;

/// <summary>
/// Defines styling properties for a segment of highlighted text.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TextStyle"/> is used by <see cref="IHighlightRenderer"/> to specify
/// how text runs should be styled when rendering <see cref="Lexichord.Abstractions.Contracts.Snippet"/>
/// content in the UI.
/// </para>
/// <para>
/// <b>Color Format:</b> Colors are specified as hex strings (e.g., "#1a56db")
/// to maintain platform independence. UI controls should parse these values
/// when creating brushes.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6b as part of The Answer Preview feature.
/// </para>
/// </remarks>
/// <param name="IsBold">Whether the text should be rendered in bold.</param>
/// <param name="IsItalic">Whether the text should be rendered in italic.</param>
/// <param name="ForegroundColor">Optional foreground color as hex string.</param>
/// <param name="BackgroundColor">Optional background color as hex string.</param>
public record TextStyle(
    bool IsBold = false,
    bool IsItalic = false,
    string? ForegroundColor = null,
    string? BackgroundColor = null)
{
    /// <summary>
    /// Default style with no modifications.
    /// </summary>
    /// <remarks>
    /// Used for non-highlighted text segments.
    /// </remarks>
    public static TextStyle Default => new();

    /// <summary>
    /// Style for exact query matches (bold text).
    /// </summary>
    /// <remarks>
    /// Applied to <see cref="Lexichord.Abstractions.Contracts.HighlightType.QueryMatch"/>
    /// spans. Colors are applied separately from <see cref="HighlightTheme"/>.
    /// </remarks>
    public static TextStyle ExactMatch => new(IsBold: true);

    /// <summary>
    /// Style for fuzzy/approximate matches (italic text).
    /// </summary>
    /// <remarks>
    /// Applied to <see cref="Lexichord.Abstractions.Contracts.HighlightType.FuzzyMatch"/>
    /// spans. Colors are applied separately from <see cref="HighlightTheme"/>.
    /// </remarks>
    public static TextStyle FuzzyMatch => new(IsItalic: true);
}
