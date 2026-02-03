// =============================================================================
// File: SnippetOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration record for snippet extraction behavior.
// =============================================================================
// LOGIC: Immutable options for controlling snippet extraction.
//   - MaxLength limits snippet size for UI fit.
//   - ContextPadding adds buffer around match clusters.
//   - RespectSentenceBoundaries snaps to natural breaks.
//   - IncludeFuzzyMatches controls fuzzy match highlighting.
//   - MinMatchLength filters short keywords.
// =============================================================================
// VERSION: v0.5.6a (Snippet Extraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Configuration options for snippet extraction behavior.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SnippetOptions"/> controls how <see cref="ISnippetService"/> extracts
/// and formats snippets from chunk content. Options include length constraints,
/// boundary handling, and match filtering.
/// </para>
/// <para>
/// <b>Presets:</b> Use <see cref="Default"/>, <see cref="Compact"/>, or
/// <see cref="Extended"/> for common configurations.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6a as part of The Answer Preview feature.
/// </para>
/// </remarks>
/// <param name="MaxLength">Maximum snippet length in characters (default: 200).</param>
/// <param name="ContextPadding">Characters to include before/after matches (default: 50).</param>
/// <param name="RespectSentenceBoundaries">Whether to snap to sentence boundaries (default: true).</param>
/// <param name="IncludeFuzzyMatches">Whether to highlight fuzzy matches (default: true).</param>
/// <param name="MinMatchLength">Minimum term length to consider for matching (default: 3).</param>
public record SnippetOptions(
    int MaxLength = 200,
    int ContextPadding = 50,
    bool RespectSentenceBoundaries = true,
    bool IncludeFuzzyMatches = true,
    int MinMatchLength = 3)
{
    /// <summary>
    /// Default options for general use.
    /// </summary>
    /// <remarks>
    /// Uses 200-character snippets with 50-character context padding,
    /// sentence boundary snapping, and fuzzy match highlighting.
    /// </remarks>
    public static SnippetOptions Default => new();

    /// <summary>
    /// Compact options for condensed displays.
    /// </summary>
    /// <remarks>
    /// Uses 100-character snippets with 25-character context padding.
    /// Suitable for list views with limited space.
    /// </remarks>
    public static SnippetOptions Compact => new(MaxLength: 100, ContextPadding: 25);

    /// <summary>
    /// Extended options for detailed previews.
    /// </summary>
    /// <remarks>
    /// Uses 300-character snippets with 75-character context padding.
    /// Suitable for expanded preview panels or hover tooltips.
    /// </remarks>
    public static SnippetOptions Extended => new(MaxLength: 300, ContextPadding: 75);
}
