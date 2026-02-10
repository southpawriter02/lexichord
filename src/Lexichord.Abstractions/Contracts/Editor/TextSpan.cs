// -----------------------------------------------------------------------
// <copyright file="TextSpan.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Editor;

/// <summary>
/// Represents a span of text in the document.
/// </summary>
/// <param name="Start">The zero-based starting character position.</param>
/// <param name="Length">The number of characters in the span.</param>
/// <remarks>
/// <para>
/// Used by <see cref="IEditorInsertionService"/> to specify preview locations
/// and text replacement ranges. Distinct from <see cref="TextSelection"/> which
/// carries the selected text content.
/// </para>
/// <para><b>Introduced in:</b> v0.6.7b as part of the Inline Suggestions feature.</para>
/// </remarks>
public record TextSpan(int Start, int Length)
{
    /// <summary>
    /// Gets the end position of the span (exclusive).
    /// </summary>
    public int End => Start + Length;

    /// <summary>
    /// Creates a span from start and end positions.
    /// </summary>
    /// <param name="start">Start position (inclusive).</param>
    /// <param name="end">End position (exclusive).</param>
    /// <returns>A new TextSpan instance.</returns>
    public static TextSpan FromStartEnd(int start, int end) =>
        new(start, end - start);

    /// <summary>
    /// Returns an empty span at position zero.
    /// </summary>
    public static TextSpan Empty => new(0, 0);

    /// <summary>
    /// Determines if this span contains the specified position.
    /// </summary>
    public bool Contains(int position) =>
        position >= Start && position < End;

    /// <summary>
    /// Determines if this span overlaps with another span.
    /// </summary>
    public bool OverlapsWith(TextSpan other) =>
        Start < other.End && other.Start < End;
}
