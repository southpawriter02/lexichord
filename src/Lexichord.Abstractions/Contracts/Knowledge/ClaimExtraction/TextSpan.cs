// =============================================================================
// File: TextSpan.cs
// Project: Lexichord.Abstractions
// Description: Represents a text span with character offsets for claim extraction.
// =============================================================================
// LOGIC: Simple record to represent a contiguous piece of text with its
//   location within a document or sentence.
//
// v0.5.6g: Claim Extractor (Knowledge Graph Claim Extraction Pipeline)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.ClaimExtraction;

/// <summary>
/// Represents a contiguous span of text with character offsets.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Provides a structured way to reference text regions
/// during claim extraction, maintaining both the text content and its
/// position within the source document.
/// </para>
/// <para>
/// <b>Usage:</b> Used by <see cref="ExtractedClaim"/> to track subject
/// and object positions in the source sentence.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6g as part of the Claim Extractor.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var span = new TextSpan
/// {
///     Text = "GET /users",
///     StartOffset = 4,
///     EndOffset = 14
/// };
/// </code>
/// </example>
public record TextSpan
{
    /// <summary>
    /// Gets the text content of the span.
    /// </summary>
    /// <value>The actual text contained within this span.</value>
    public required string Text { get; init; }

    /// <summary>
    /// Gets the starting character offset (0-indexed, inclusive).
    /// </summary>
    /// <value>The position of the first character in the source text.</value>
    public int StartOffset { get; init; }

    /// <summary>
    /// Gets the ending character offset (0-indexed, exclusive).
    /// </summary>
    /// <value>The position after the last character in the source text.</value>
    public int EndOffset { get; init; }

    /// <summary>
    /// Gets the length of the span in characters.
    /// </summary>
    public int Length => EndOffset - StartOffset;

    /// <summary>
    /// Checks whether this span overlaps with another span.
    /// </summary>
    /// <param name="other">The other span to check against.</param>
    /// <returns><c>true</c> if the spans overlap; otherwise, <c>false</c>.</returns>
    public bool Overlaps(TextSpan other)
    {
        return StartOffset < other.EndOffset && EndOffset > other.StartOffset;
    }

    /// <summary>
    /// Checks whether this span contains another span.
    /// </summary>
    /// <param name="other">The other span to check.</param>
    /// <returns><c>true</c> if this span fully contains the other; otherwise, <c>false</c>.</returns>
    public bool Contains(TextSpan other)
    {
        return StartOffset <= other.StartOffset && EndOffset >= other.EndOffset;
    }

    /// <inheritdoc/>
    public override string ToString() => $"\"{Text}\" [{StartOffset}..{EndOffset}]";
}
