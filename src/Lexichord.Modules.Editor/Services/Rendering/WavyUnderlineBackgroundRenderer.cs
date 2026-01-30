using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using Lexichord.Abstractions.Contracts.Linting;

namespace Lexichord.Modules.Editor.Services.Rendering;

/// <summary>
/// Background renderer that draws wavy underlines for style violations.
/// </summary>
/// <remarks>
/// LOGIC: WavyUnderlineBackgroundRenderer implements AvaloniaEdit's IBackgroundRenderer
/// to draw wavy underlines below text. It receives segment data from StyleViolationRenderer
/// and renders them during the TextView's rendering pass.
///
/// Drawing Algorithm:
/// - For each segment, calculate the baseline position
/// - Draw a sine wave using quadratic bezier curves
/// - Wave parameters: 2px amplitude, 4px wavelength, 1.5px thickness
///
/// Performance Optimizations:
/// - Pens are cached by color to avoid repeated allocations
/// - Geometry is built per-frame (frozen for rendering efficiency)
/// - Segments are sorted by severity for proper z-ordering (errors on top)
///
/// Layer: KnownLayer.Background - renders below the actual text
///
/// Version: v0.2.4b - Enhanced with pen caching, severity sorting, and updated wave parameters
/// </remarks>
public sealed class WavyUnderlineBackgroundRenderer : IBackgroundRenderer
{
    private readonly List<UnderlineSegment> _segments = [];
    private readonly Dictionary<Color, Pen> _penCache = new();
    private readonly object _lock = new();

    /// <summary>
    /// Wave amplitude in pixels (half the peak-to-peak height).
    /// </summary>
    private const double WaveAmplitude = 2.0;

    /// <summary>
    /// Wave period in pixels (distance for one complete wave cycle).
    /// </summary>
    private const double WavePeriod = 4.0;

    /// <summary>
    /// Underline thickness in pixels.
    /// </summary>
    private const double UnderlineThickness = 1.5;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Render on the Background layer so underlines appear
    /// below text but above the editor background.
    /// </remarks>
    public KnownLayer Layer => KnownLayer.Background;

    /// <summary>
    /// Clears all underline segments.
    /// </summary>
    public void ClearSegments()
    {
        lock (_lock)
        {
            _segments.Clear();
        }
    }

    /// <summary>
    /// Adds an underline segment.
    /// </summary>
    /// <param name="segment">The segment to add.</param>
    public void AddSegment(UnderlineSegment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);

        lock (_lock)
        {
            _segments.Add(segment);
        }
    }

    /// <summary>
    /// Replaces all segments with a new collection.
    /// </summary>
    /// <param name="segments">The new segments.</param>
    public void SetSegments(IEnumerable<UnderlineSegment> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        lock (_lock)
        {
            _segments.Clear();
            _segments.AddRange(segments);
        }
    }

    /// <inheritdoc />
    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document is null)
            return;

        // LOGIC: Get snapshot of segments, sorted by severity (info first, errors last = on top)
        List<UnderlineSegment> snapshot;
        lock (_lock)
        {
            if (_segments.Count == 0)
                return;

            // LOGIC: Sort by severity to ensure proper z-ordering
            // Lower severity draws first (underneath), higher severity draws last (on top)
            snapshot = _segments
                .OrderBy(s => GetSeverityOrder(s.Color))
                .ToList();
        }

        var documentLength = textView.Document.TextLength;

        foreach (var segment in snapshot)
        {
            // LOGIC: Skip segments outside document bounds
            if (segment.StartOffset < 0 || segment.EndOffset > documentLength)
                continue;

            // LOGIC: Get visual positions for the segment
            DrawWavyUnderline(textView, drawingContext, segment);
        }
    }

    /// <summary>
    /// Gets the sort order for a color based on its assumed severity.
    /// </summary>
    /// <remarks>
    /// LOGIC: We determine severity order by comparing colors to known values.
    /// Hint (Gray) = 0, Info (Blue) = 1, Warning (Orange) = 2, Error (Red) = 3
    /// This ensures errors draw on top.
    /// </remarks>
    private static int GetSeverityOrder(UnderlineColor color)
    {
        // Check if it's an error color (red-ish)
        if (color.R > 200 && color.G < 100 && color.B < 100)
            return 3; // Error - on top

        // Check if it's a warning color (orange-ish)
        if (color.R > 200 && color.G > 100 && color.B < 100)
            return 2; // Warning

        // Check if it's an info color (blue-ish)
        if (color.B > 150)
            return 1; // Info

        // Default: Hint (gray)
        return 0;
    }

    /// <summary>
    /// Draws a wavy underline for a single segment.
    /// </summary>
    private void DrawWavyUnderline(
        TextView textView,
        DrawingContext drawingContext,
        UnderlineSegment segment)
    {
        // LOGIC: Get the visual line(s) containing this segment
        var visualLines = textView.VisualLines;
        if (visualLines.Count == 0)
            return;

        // LOGIC: Find visual lines that contain our segment
        foreach (var visualLine in visualLines)
        {
            var lineStartOffset = visualLine.FirstDocumentLine.Offset;
            var lineEndOffset = visualLine.LastDocumentLine.EndOffset;

            // LOGIC: Check if segment overlaps this line
            if (segment.EndOffset <= lineStartOffset || segment.StartOffset >= lineEndOffset)
                continue;

            // LOGIC: Clip segment to line bounds
            var clippedStart = Math.Max(segment.StartOffset, lineStartOffset);
            var clippedEnd = Math.Min(segment.EndOffset, lineEndOffset);

            // LOGIC: Get text element positions
            var rects = BackgroundGeometryBuilder.GetRectsForSegment(
                textView,
                new SimpleSegment(clippedStart, clippedEnd - clippedStart));

            // LOGIC: Convert UnderlineColor to Avalonia.Media.Color
            var color = Color.FromArgb(segment.Color.A, segment.Color.R, segment.Color.G, segment.Color.B);

            foreach (var rect in rects)
            {
                DrawWavyLine(drawingContext, rect, color);
            }
        }
    }

    /// <summary>
    /// Draws a wavy line under a rectangle.
    /// </summary>
    private void DrawWavyLine(
        DrawingContext drawingContext,
        Rect rect,
        Color color)
    {
        // LOGIC: Position the wave at the bottom of the text
        var baseline = rect.Bottom;
        var startX = rect.Left;
        var endX = rect.Right;
        var width = endX - startX;

        if (width < 2)
            return; // Too small to draw

        // LOGIC: Get or create cached pen for this color
        var pen = GetOrCreatePen(color);

        // LOGIC: Build the wavy path using quadratic bezier curves
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(startX, baseline), false);

            // LOGIC: Draw wave segments using quadratic beziers
            // Each wave period consists of two bezier curves (up-down)
            var x = startX;
            var halfWave = WavePeriod / 2;
            var waveUp = true;

            while (x < endX)
            {
                var nextX = Math.Min(x + halfWave, endX);
                var midX = (x + nextX) / 2;

                // Control point at peak or trough
                var controlY = waveUp
                    ? baseline - WaveAmplitude
                    : baseline + WaveAmplitude;

                context.QuadraticBezierTo(
                    new Point(midX, controlY),
                    new Point(nextX, baseline));

                x = nextX;
                waveUp = !waveUp;
            }

            context.EndFigure(false);
        }

        drawingContext.DrawGeometry(null, pen, geometry);
    }

    /// <summary>
    /// Gets or creates a cached pen for the specified color.
    /// </summary>
    /// <param name="color">The pen color.</param>
    /// <returns>Cached or new Pen instance.</returns>
    /// <remarks>
    /// LOGIC: Pens are cached by color to avoid creating new objects
    /// on every draw call. Cache is small (typically 3-4 colors) so no
    /// eviction is needed.
    /// </remarks>
    private Pen GetOrCreatePen(Color color)
    {
        if (_penCache.TryGetValue(color, out var cachedPen))
            return cachedPen;

        var brush = new SolidColorBrush(color);
        var pen = new Pen(brush, UnderlineThickness, lineCap: PenLineCap.Round);

        _penCache[color] = pen;

        return pen;
    }

    /// <summary>
    /// Simple segment implementation for BackgroundGeometryBuilder.
    /// </summary>
    private sealed class SimpleSegment : AvaloniaEdit.Document.ISegment
    {
        public SimpleSegment(int offset, int length)
        {
            Offset = offset;
            Length = length;
        }

        public int Offset { get; }
        public int Length { get; }
        public int EndOffset => Offset + Length;
    }
}
