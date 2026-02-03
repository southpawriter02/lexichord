// =============================================================================
// File: HighlightTheme.cs
// Project: Lexichord.Modules.RAG
// Description: Record defining color palettes for query term highlighting.
// =============================================================================
// LOGIC: Immutable record with color definitions for each highlight type.
//   - Provides static Light and Dark theme presets.
//   - Colors are hex strings for platform independence.
//   - Used by HighlightRenderer to apply theme-aware colors to runs.
// =============================================================================
// VERSION: v0.5.6b (Query Term Highlighting)
// =============================================================================

namespace Lexichord.Modules.RAG.Rendering;

/// <summary>
/// Color palette for query term highlighting in snippets.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="HighlightTheme"/> provides theme-aware color definitions for
/// different types of highlights. The <see cref="IHighlightRenderer"/> uses
/// these colors when generating <see cref="StyledTextRun"/> objects.
/// </para>
/// <para>
/// <b>Color Format:</b> All colors are specified as hex strings (e.g., "#1a56db")
/// for platform independence. UI controls should parse these values when
/// creating brushes.
/// </para>
/// <para>
/// <b>Accessibility:</b> Both Light and Dark themes are designed with
/// sufficient contrast ratios for readability. Exact matches use blue tones,
/// fuzzy matches use purple tones.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6b as part of The Answer Preview feature.
/// </para>
/// </remarks>
/// <param name="ExactMatchForeground">Foreground color for exact query matches.</param>
/// <param name="ExactMatchBackground">Background color for exact query matches (nullable).</param>
/// <param name="FuzzyMatchForeground">Foreground color for fuzzy/approximate matches.</param>
/// <param name="FuzzyMatchBackground">Background color for fuzzy matches (nullable).</param>
/// <param name="KeyPhraseForeground">Foreground color for key phrase highlights (nullable).</param>
/// <param name="EllipsisColor">Color for ellipsis markers in truncated snippets.</param>
public record HighlightTheme(
    string ExactMatchForeground,
    string? ExactMatchBackground,
    string FuzzyMatchForeground,
    string? FuzzyMatchBackground,
    string? KeyPhraseForeground,
    string EllipsisColor)
{
    /// <summary>
    /// Light theme color palette optimized for light backgrounds.
    /// </summary>
    /// <remarks>
    /// Uses darker, saturated colors that provide good contrast on
    /// white or light gray backgrounds.
    /// </remarks>
    public static HighlightTheme Light => new(
        ExactMatchForeground: "#1a56db",      // Blue
        ExactMatchBackground: "#dbeafe",       // Light blue
        FuzzyMatchForeground: "#7c3aed",       // Purple
        FuzzyMatchBackground: "#ede9fe",       // Light purple
        KeyPhraseForeground: "#059669",        // Green
        EllipsisColor: "#9ca3af");             // Gray

    /// <summary>
    /// Dark theme color palette optimized for dark backgrounds.
    /// </summary>
    /// <remarks>
    /// Uses lighter, desaturated colors that provide good contrast on
    /// dark gray or black backgrounds.
    /// </remarks>
    public static HighlightTheme Dark => new(
        ExactMatchForeground: "#60a5fa",       // Light blue
        ExactMatchBackground: "#1e3a5f",       // Dark blue
        FuzzyMatchForeground: "#a78bfa",       // Light purple
        FuzzyMatchBackground: "#3b2d5e",       // Dark purple
        KeyPhraseForeground: "#34d399",        // Light green
        EllipsisColor: "#6b7280");             // Gray
}
