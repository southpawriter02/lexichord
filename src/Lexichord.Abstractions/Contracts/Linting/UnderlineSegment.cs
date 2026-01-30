namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Represents an RGBA color for underlines.
/// </summary>
/// <remarks>
/// LOGIC: Platform-agnostic color representation. The Style module
/// is UI-agnostic, so we use this simple struct instead of Avalonia.Media.Color.
/// The rendering layer converts this to the appropriate platform color type.
///
/// Version: v0.2.4b - Added theme-aware color palettes
/// </remarks>
/// <param name="R">Red component (0-255).</param>
/// <param name="G">Green component (0-255).</param>
/// <param name="B">Blue component (0-255).</param>
/// <param name="A">Alpha component (0-255, 255 = fully opaque).</param>
public readonly record struct UnderlineColor(byte R, byte G, byte B, byte A = 255)
{
    // ========================================
    // Light Theme Colors (high contrast on white)
    // ========================================

    /// <summary>Light theme error red (#E51400).</summary>
    public static UnderlineColor LightError => new(0xE5, 0x14, 0x00);

    /// <summary>Light theme warning orange (#F0A30A).</summary>
    public static UnderlineColor LightWarning => new(0xF0, 0xA3, 0x0A);

    /// <summary>Light theme info blue (#0078D4).</summary>
    public static UnderlineColor LightInfo => new(0x00, 0x78, 0xD4);

    /// <summary>Light theme hint gray (#808080).</summary>
    public static UnderlineColor LightHint => new(0x80, 0x80, 0x80);

    // ========================================
    // Dark Theme Colors (softer on dark backgrounds)
    // ========================================

    /// <summary>Dark theme error salmon red (#FF6B6B).</summary>
    public static UnderlineColor DarkError => new(0xFF, 0x6B, 0x6B);

    /// <summary>Dark theme warning light orange (#FFB347).</summary>
    public static UnderlineColor DarkWarning => new(0xFF, 0xB3, 0x47);

    /// <summary>Dark theme info light blue (#4FC3F7).</summary>
    public static UnderlineColor DarkInfo => new(0x4F, 0xC3, 0xF7);

    /// <summary>Dark theme hint light gray (#B0B0B0).</summary>
    public static UnderlineColor DarkHint => new(0xB0, 0xB0, 0xB0);

    // ========================================
    // Semi-transparent Background Colors
    // ========================================

    /// <summary>Light theme error background (12% opacity).</summary>
    public static UnderlineColor LightErrorBackground => new(0xE5, 0x14, 0x00, 0x20);

    /// <summary>Light theme warning background (12% opacity).</summary>
    public static UnderlineColor LightWarningBackground => new(0xF0, 0xA3, 0x0A, 0x20);

    /// <summary>Light theme info background (12% opacity).</summary>
    public static UnderlineColor LightInfoBackground => new(0x00, 0x78, 0xD4, 0x20);

    /// <summary>Dark theme error background (19% opacity).</summary>
    public static UnderlineColor DarkErrorBackground => new(0xFF, 0x6B, 0x6B, 0x30);

    /// <summary>Dark theme warning background (19% opacity).</summary>
    public static UnderlineColor DarkWarningBackground => new(0xFF, 0xB3, 0x47, 0x30);

    /// <summary>Dark theme info background (19% opacity).</summary>
    public static UnderlineColor DarkInfoBackground => new(0x4F, 0xC3, 0xF7, 0x30);

    // ========================================
    // Legacy Aliases (for backward compatibility)
    // ========================================

    /// <summary>Standard red for errors (alias for LightError).</summary>
    public static UnderlineColor ErrorRed => LightError;

    /// <summary>Standard yellow/orange for warnings (alias for LightWarning).</summary>
    public static UnderlineColor WarningYellow => LightWarning;

    /// <summary>Standard blue for info (alias for LightInfo).</summary>
    public static UnderlineColor InfoBlue => LightInfo;

    /// <summary>Standard gray for hints (alias for LightHint).</summary>
    public static UnderlineColor HintGray => LightHint;
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
