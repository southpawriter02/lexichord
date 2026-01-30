namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Represents an RGBA color for underlines.
/// </summary>
/// <remarks>
/// LOGIC: Platform-agnostic color representation. The Style module
/// is UI-agnostic, so we use this simple struct instead of Avalonia.Media.Color.
/// The rendering layer converts this to the appropriate platform color type.
///
/// Version: v0.2.4a
/// </remarks>
/// <param name="R">Red component (0-255).</param>
/// <param name="G">Green component (0-255).</param>
/// <param name="B">Blue component (0-255).</param>
/// <param name="A">Alpha component (0-255, 255 = fully opaque).</param>
public readonly record struct UnderlineColor(byte R, byte G, byte B, byte A = 255)
{
    /// <summary>Standard red for errors.</summary>
    public static UnderlineColor ErrorRed => new(0xE5, 0x14, 0x00);

    /// <summary>Standard yellow/orange for warnings.</summary>
    public static UnderlineColor WarningYellow => new(0xFF, 0xC0, 0x00);

    /// <summary>Standard blue for info.</summary>
    public static UnderlineColor InfoBlue => new(0x1E, 0x90, 0xFF);

    /// <summary>Standard gray for hints.</summary>
    public static UnderlineColor HintGray => new(0xA0, 0xA0, 0xA0);
}

/// <summary>
/// Represents a segment of text that should have a wavy underline.
/// </summary>
/// <remarks>
/// LOGIC: UnderlineSegment is the data transfer object between
/// StyleViolationRenderer (which identifies what to underline) and
/// WavyUnderlineBackgroundRenderer (which draws the underline).
///
/// The segment contains:
/// - Position information (start/end offsets)
/// - Visual styling (color based on severity)
/// - Reference back to violation ID for tooltip/click handling
///
/// Design Decision: Immutable record for thread-safe passing between
/// the colorizer and background renderer.
///
/// Version: v0.2.4a
/// </remarks>
/// <param name="StartOffset">Character offset where the underline starts (0-indexed).</param>
/// <param name="EndOffset">Character offset where the underline ends (exclusive).</param>
/// <param name="Color">The color of the wavy underline.</param>
/// <param name="ViolationId">The ID of the associated violation for linking.</param>
public sealed record UnderlineSegment(
    int StartOffset,
    int EndOffset,
    UnderlineColor Color,
    string ViolationId)
{
    /// <summary>
    /// Gets the length of the segment in characters.
    /// </summary>
    public int Length => EndOffset - StartOffset;

    /// <summary>
    /// Checks if this segment overlaps with a character range.
    /// </summary>
    /// <param name="start">Start of range (inclusive).</param>
    /// <param name="end">End of range (exclusive).</param>
    /// <returns>True if any part of this segment is within the range.</returns>
    /// <remarks>
    /// LOGIC: Used by the renderer to filter segments to only those
    /// visible in the current line/viewport.
    /// </remarks>
    public bool OverlapsRange(int start, int end) =>
        StartOffset < end && EndOffset > start;

    /// <summary>
    /// Gets the portion of this segment that falls within a range.
    /// </summary>
    /// <param name="start">Start of range (inclusive).</param>
    /// <param name="end">End of range (exclusive).</param>
    /// <returns>A new segment clipped to the range, or null if no overlap.</returns>
    /// <remarks>
    /// LOGIC: Used when rendering a single line - we clip the segment
    /// to only the portion visible on that line.
    /// </remarks>
    public UnderlineSegment? ClipToRange(int start, int end)
    {
        if (!OverlapsRange(start, end))
            return null;

        var clippedStart = Math.Max(StartOffset, start);
        var clippedEnd = Math.Min(EndOffset, end);

        return this with
        {
            StartOffset = clippedStart,
            EndOffset = clippedEnd
        };
    }
}
