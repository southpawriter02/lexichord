// =============================================================================
// File: SentenceBoundary.cs
// Project: Lexichord.Abstractions
// Description: Record representing a sentence's position within text.
// =============================================================================
// LOGIC: Represents sentence boundaries for smart truncation.
//   - Start is inclusive, End is exclusive.
//   - Contains checks if a position falls within the sentence.
//   - OverlapsWith checks for range intersection.
// =============================================================================
// VERSION: v0.5.6c (Smart Truncation)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents a sentence's position within text.
/// </summary>
/// <param name="Start">Start position of the sentence (inclusive).</param>
/// <param name="End">End position of the sentence (exclusive, after terminator).</param>
/// <remarks>
/// <para>
/// <see cref="SentenceBoundary"/> is used by <see cref="ISentenceBoundaryDetector"/>
/// to return sentence boundary information for snippet extraction.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6c as part of Smart Truncation.
/// </para>
/// </remarks>
public record SentenceBoundary(int Start, int End)
{
    /// <summary>
    /// Gets the length of this sentence in characters.
    /// </summary>
    public int Length => End - Start;

    /// <summary>
    /// Checks if a position falls within this sentence.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="position"/> is within
    /// [<see cref="Start"/>, <see cref="End"/>); otherwise <see langword="false"/>.
    /// </returns>
    public bool Contains(int position) =>
        position >= Start && position < End;

    /// <summary>
    /// Checks if this sentence overlaps with a range.
    /// </summary>
    /// <param name="rangeStart">Start of the range (inclusive).</param>
    /// <param name="rangeEnd">End of the range (exclusive).</param>
    /// <returns>
    /// <see langword="true"/> if the ranges overlap; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Uses standard range overlap logic: two ranges [a, b) and [c, d) overlap
    /// if and only if a &lt; d and c &lt; b.
    /// </remarks>
    public bool OverlapsWith(int rangeStart, int rangeEnd) =>
        Start < rangeEnd && rangeStart < End;
}
