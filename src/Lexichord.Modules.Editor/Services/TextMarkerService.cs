using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace Lexichord.Modules.Editor.Services;

/// <summary>
/// Service for managing text markers (highlighting) in the editor.
/// </summary>
/// <remarks>
/// LOGIC: TextMarkerService is a background renderer that draws colored
/// rectangles behind matched text. It maintains a collection of markers
/// and renders them during the TextView's rendering pass.
/// </remarks>
internal sealed class TextMarkerService : IBackgroundRenderer, IDisposable
{
    private readonly TextDocument _document;
    private readonly List<TextMarker> _markers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TextMarkerService"/> class.
    /// </summary>
    /// <param name="document">The text document to attach to.</param>
    public TextMarkerService(TextDocument document)
    {
        _document = document;
        _document.Changed += OnDocumentChanged;
    }

    /// <inheritdoc/>
    public KnownLayer Layer => KnownLayer.Selection;

    /// <summary>
    /// Creates a new text marker.
    /// </summary>
    /// <param name="startOffset">Start offset in the document.</param>
    /// <param name="length">Length of the marked text.</param>
    /// <returns>The created marker.</returns>
    public TextMarker Create(int startOffset, int length)
    {
        var marker = new TextMarker(startOffset, length);
        _markers.Add(marker);
        return marker;
    }

    /// <summary>
    /// Removes all markers matching the predicate.
    /// </summary>
    /// <param name="predicate">Predicate to match markers.</param>
    public void RemoveAll(Predicate<TextMarker> predicate)
    {
        _markers.RemoveAll(predicate);
    }

    /// <inheritdoc/>
    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_markers.Count == 0 || textView.Document is null)
            return;

        foreach (var marker in _markers)
        {
            // Skip markers outside visible region
            if (marker.StartOffset < 0 || marker.StartOffset + marker.Length > textView.Document.TextLength)
                continue;

            var geometry = BackgroundGeometryBuilder.GetRectsForSegment(
                textView,
                new TextSegment { StartOffset = marker.StartOffset, Length = marker.Length }
            );

            foreach (var rect in geometry)
            {
                drawingContext.FillRectangle(marker.BackgroundBrush, rect);
            }
        }
    }

    private void OnDocumentChanged(object? sender, DocumentChangeEventArgs e)
    {
        // LOGIC: Update marker positions when document changes
        var offset = e.Offset;
        var lenDiff = e.InsertionLength - e.RemovalLength;

        for (var i = _markers.Count - 1; i >= 0; i--)
        {
            var marker = _markers[i];

            if (marker.StartOffset >= offset + e.RemovalLength)
            {
                // Marker is after the change, shift it
                marker.StartOffset += lenDiff;
            }
            else if (marker.StartOffset + marker.Length > offset && marker.StartOffset < offset + e.RemovalLength)
            {
                // Marker overlaps with the change, remove it
                _markers.RemoveAt(i);
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _document.Changed -= OnDocumentChanged;
        _markers.Clear();
    }
}

/// <summary>
/// Represents a text marker for highlighting.
/// </summary>
internal sealed class TextMarker
{
    private static readonly IBrush DefaultBackgroundBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 0));

    /// <summary>
    /// Initializes a new instance of the <see cref="TextMarker"/> class.
    /// </summary>
    /// <param name="startOffset">Start offset in the document.</param>
    /// <param name="length">Length of the marked text.</param>
    public TextMarker(int startOffset, int length)
    {
        StartOffset = startOffset;
        Length = length;
        BackgroundBrush = DefaultBackgroundBrush;
    }

    /// <summary>
    /// Gets or sets the start offset.
    /// </summary>
    public int StartOffset { get; set; }

    /// <summary>
    /// Gets or sets the length.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Gets or sets the background brush.
    /// </summary>
    public IBrush BackgroundBrush { get; set; }
}

/// <summary>
/// Text segment for geometry building.
/// </summary>
internal sealed class TextSegment : ISegment
{
    /// <inheritdoc/>
    public int Offset => StartOffset;

    /// <summary>
    /// Gets or sets the start offset.
    /// </summary>
    public int StartOffset { get; init; }

    /// <inheritdoc/>
    public int Length { get; init; }

    /// <inheritdoc/>
    public int EndOffset => StartOffset + Length;
}
