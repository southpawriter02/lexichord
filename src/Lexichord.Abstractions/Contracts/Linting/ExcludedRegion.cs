namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Represents a region of document content to exclude from linting.
/// </summary>
/// <remarks>
/// LOGIC: ExcludedRegion marks a character range that should be
/// skipped during style scanning. Created by content filters
/// (e.g., MarkdownCodeBlockFilter) and consumed by the scanner.
///
/// The range is defined by character offsets from document start:
/// - StartOffset: First character to exclude (inclusive, 0-indexed)
/// - EndOffset: First character after exclusion (exclusive)
///
/// Metadata provides additional context (e.g., language hint for
/// fenced code blocks like "python" or "csharp").
///
/// Version: v0.2.7b
/// </remarks>
/// <param name="StartOffset">Character offset where exclusion starts (0-indexed, inclusive).</param>
/// <param name="EndOffset">Character offset where exclusion ends (exclusive).</param>
/// <param name="Reason">Why this region is excluded.</param>
/// <param name="Metadata">Optional additional context (e.g., language hint).</param>
public sealed record ExcludedRegion(
    int StartOffset,
    int EndOffset,
    ExclusionReason Reason,
    string? Metadata = null)
{
    /// <summary>
    /// Gets the length of the excluded region.
    /// </summary>
    public int Length => EndOffset - StartOffset;

    /// <summary>
    /// Checks if an offset falls within this excluded region.
    /// </summary>
    /// <param name="offset">The offset to check.</param>
    /// <returns>True if the offset is within [StartOffset, EndOffset).</returns>
    public bool Contains(int offset) => offset >= StartOffset && offset < EndOffset;

    /// <summary>
    /// Checks if this region overlaps with another region.
    /// </summary>
    /// <param name="other">The other region to check.</param>
    /// <returns>True if the regions overlap.</returns>
    public bool Overlaps(ExcludedRegion other) =>
        StartOffset < other.EndOffset && EndOffset > other.StartOffset;
}
