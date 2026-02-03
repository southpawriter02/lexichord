// =============================================================================
// File: HighlightSpan.cs
// Project: Lexichord.Abstractions
// Description: Record representing a highlighted span within a snippet.
// =============================================================================
// LOGIC: Immutable record for tracking highlighted regions in snippet text.
//   - Positions are relative to the snippet Text, not the original chunk.
//   - Overlapping spans can be detected and merged to prevent duplication.
//   - Merge logic preserves the more specific (lower enum value) type.
// =============================================================================
// VERSION: v0.5.6a (Snippet Extraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// A highlighted span within a snippet, marking a matched region.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="HighlightSpan"/> records mark regions within a <see cref="Snippet"/>
/// that should receive visual highlighting in the UI. Each span tracks its position,
/// length, and the type of match it represents.
/// </para>
/// <para>
/// <b>Position Reference:</b> The <see cref="Start"/> position is relative to the
/// <see cref="Snippet.Text"/> property, not the original chunk content. UI components
/// can directly use these positions for styling without offset calculations.
/// </para>
/// <para>
/// <b>Overlap Handling:</b> Overlapping spans should be merged before rendering
/// using the <see cref="Merge"/> method. This ensures clean visual presentation
/// and prevents duplicate highlights.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6a as part of The Answer Preview feature.
/// </para>
/// </remarks>
/// <param name="Start">Start position within the snippet text (0-based).</param>
/// <param name="Length">Length of the highlighted region in characters.</param>
/// <param name="Type">The type of match this highlight represents.</param>
public record HighlightSpan(int Start, int Length, HighlightType Type)
{
    /// <summary>
    /// Gets the end position (exclusive) of this span.
    /// </summary>
    /// <value>
    /// The sum of <see cref="Start"/> and <see cref="Length"/>.
    /// </value>
    public int End => Start + Length;

    /// <summary>
    /// Checks if this span overlaps with another.
    /// </summary>
    /// <param name="other">The span to check for overlap.</param>
    /// <returns>
    /// <c>true</c> if the spans overlap; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Two spans overlap if their ranges intersect at any point.
    /// Adjacent spans (where one ends exactly where another begins)
    /// do not overlap.
    /// </remarks>
    public bool Overlaps(HighlightSpan other) =>
        Start < other.End && other.Start < End;

    /// <summary>
    /// Merges this span with another overlapping span.
    /// </summary>
    /// <param name="other">The span to merge with.</param>
    /// <returns>
    /// A new <see cref="HighlightSpan"/> covering both regions, using the
    /// more specific (lower enum value) <see cref="HighlightType"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The merged span covers the union of both input spans. The resulting
    /// type is the more specific of the two (lower enum value wins).
    /// </para>
    /// <para>
    /// <b>Example:</b> Merging a <see cref="HighlightType.QueryMatch"/> span
    /// with a <see cref="HighlightType.FuzzyMatch"/> span produces a span
    /// with type <see cref="HighlightType.QueryMatch"/>.
    /// </para>
    /// </remarks>
    public HighlightSpan Merge(HighlightSpan other)
    {
        var newStart = Math.Min(Start, other.Start);
        var newEnd = Math.Max(End, other.End);

        // LOGIC: Prefer more specific type (lower enum value).
        var newType = Type < other.Type ? Type : other.Type;

        return new HighlightSpan(newStart, newEnd - newStart, newType);
    }
}
