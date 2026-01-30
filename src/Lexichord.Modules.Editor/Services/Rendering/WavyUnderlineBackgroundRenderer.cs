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
/// - Wave parameters: 1.5px amplitude, 4px wavelength
///
/// Layer: KnownLayer.Text - renders below the actual text
///
/// Version: v0.2.4a
/// </remarks>
public sealed class WavyUnderlineBackgroundRenderer : IBackgroundRenderer
{
    private readonly List<UnderlineSegment> _segments = [];
    private readonly object _lock = new();

    /// <summary>
    /// Wave amplitude in pixels (half the peak-to-peak height).
    /// </summary>
    private const double WaveAmplitude = 1.5;

    /// <summary>
    /// Wave period in pixels (distance for one complete wave cycle).
    /// </summary>
    private const double WavePeriod = 4.0;

    /// <summary>
    /// Underline thickness in pixels.
    /// </summary>
    private const double UnderlineThickness = 1.0;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Render on the Text layer so underlines appear below text
    /// but above background highlights.
    /// </remarks>
    public KnownLayer Layer => KnownLayer.Text;

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

        // LOGIC: Get snapshot of segments
        List<UnderlineSegment> snapshot;
        lock (_lock)
        {
            if (_segments.Count == 0)
                return;

            snapshot = _segments.ToList();
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
            var color = Color.FromRgb(segment.Color.R, segment.Color.G, segment.Color.B);

            foreach (var rect in rects)
            {
                DrawWavyLine(drawingContext, rect, color);
            }
        }
    }

    /// <summary>
    /// Draws a wavy line under a rectangle.
    /// </summary>
    private static void DrawWavyLine(
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

        // LOGIC: Create pen for drawing
        var pen = new Pen(new SolidColorBrush(color), UnderlineThickness);

        // LOGIC: Build the wavy path using quadratic bezier curves
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(startX, baseline), false);

            // LOGIC: Draw wave segments using quadratic beziers
            // Each wave period consists of two bezier curves (up-down)
            var x = startX;
            var waveUp = true;

            while (x < endX)
            {
                var controlX = x + WavePeriod / 4;
                var endSegmentX = Math.Min(x + WavePeriod / 2, endX);

                var controlY = waveUp
                    ? baseline - WaveAmplitude
                    : baseline + WaveAmplitude;

                var endY = baseline;

                context.QuadraticBezierTo(
                    new Point(controlX, controlY),
                    new Point(endSegmentX, endY));

                x = endSegmentX;
                waveUp = !waveUp;
            }

            context.EndFigure(false);
        }

        drawingContext.DrawGeometry(null, pen, geometry);
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
