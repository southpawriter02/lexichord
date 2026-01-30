using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Lexichord.Abstractions.Contracts.Linting;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Editor.Services.Rendering;

/// <summary>
/// Document colorizing transformer that registers wavy underlines for style violations.
/// </summary>
/// <remarks>
/// LOGIC: StyleViolationRenderer is the main integration point between the
/// linting engine and AvaloniaEdit. It:
/// 1. Subscribes to IViolationProvider.ViolationsChanged
/// 2. On each ColorizeLine call, queries violations in the line range
/// 3. Registers underline segments with WavyUnderlineBackgroundRenderer
/// 4. Invalidates the TextView when violations change
///
/// Design Pattern: Separation of Concerns
/// - StyleViolationRenderer identifies what to underline (per line)
/// - WavyUnderlineBackgroundRenderer draws the underlines (per viewport)
///
/// Lifecycle:
/// - Created by ManuscriptViewModel
/// - AttachToTextView called by ManuscriptView
/// - Disposed when document closes
///
/// Version: v0.2.4a
/// </remarks>
public sealed class StyleViolationRenderer : DocumentColorizingTransformer, IDisposable
{
    private readonly IViolationProvider _provider;
    private readonly IViolationColorProvider _colorProvider;
    private readonly ILogger<StyleViolationRenderer> _logger;

    private TextView? _textView;
    private WavyUnderlineBackgroundRenderer? _wavyRenderer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StyleViolationRenderer"/> class.
    /// </summary>
    /// <param name="provider">Source of violations.</param>
    /// <param name="colorProvider">Maps severity to colors.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public StyleViolationRenderer(
        IViolationProvider provider,
        IViolationColorProvider colorProvider,
        ILogger<StyleViolationRenderer> logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _colorProvider = colorProvider ?? throw new ArgumentNullException(nameof(colorProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Subscribe to violation changes
        _provider.ViolationsChanged += OnViolationsChanged;

        _logger.LogDebug("StyleViolationRenderer created");
    }

    /// <summary>
    /// Attaches the renderer to a TextView.
    /// </summary>
    /// <param name="textView">The text view to attach to.</param>
    /// <param name="wavyRenderer">The wavy underline background renderer.</param>
    /// <remarks>
    /// LOGIC: Called by ManuscriptView after creating the renderer.
    /// Stores references needed for invalidation and underline registration.
    /// </remarks>
    public void AttachToTextView(TextView textView, WavyUnderlineBackgroundRenderer wavyRenderer)
    {
        ArgumentNullException.ThrowIfNull(textView);
        ArgumentNullException.ThrowIfNull(wavyRenderer);

        _textView = textView;
        _wavyRenderer = wavyRenderer;

        // LOGIC: Add this transformer to the TextView
        _textView.LineTransformers.Add(this);

        _logger.LogDebug("StyleViolationRenderer attached to TextView");
    }

    /// <summary>
    /// Detaches the renderer from the current TextView.
    /// </summary>
    /// <remarks>
    /// LOGIC: Called when document is closing or renderer is being replaced.
    /// Clears references to allow garbage collection.
    /// </remarks>
    public void DetachFromTextView()
    {
        if (_textView is not null)
        {
            _textView.LineTransformers.Remove(this);
            _wavyRenderer?.ClearSegments();

            _logger.LogDebug("StyleViolationRenderer detached from TextView");
        }

        _textView = null;
        _wavyRenderer = null;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Called by AvaloniaEdit for each visible line during rendering.
    /// We query violations that overlap this line and register underline segments.
    /// </remarks>
    protected override void ColorizeLine(DocumentLine line)
    {
        if (_wavyRenderer is null || _disposed)
            return;

        var lineStart = line.Offset;
        var lineEnd = line.EndOffset;

        // LOGIC: Query violations in this line's range
        var violations = _provider.GetViolationsInRange(lineStart, lineEnd);

        if (violations.Count == 0)
            return;

        _logger.LogDebug(
            "Colorizing line {LineNumber}: {ViolationCount} violations",
            line.LineNumber,
            violations.Count);

        // LOGIC: Register underline segments for each violation
        foreach (var violation in violations)
        {
            try
            {
                RegisterUnderlineForViolation(violation, lineStart, lineEnd);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to register underline for violation {ViolationId}",
                    violation.Id);
            }
        }
    }

    /// <summary>
    /// Registers an underline segment for a violation.
    /// </summary>
    private void RegisterUnderlineForViolation(
        AggregatedStyleViolation violation,
        int lineStart,
        int lineEnd)
    {
        // LOGIC: Clip violation to line bounds
        var segmentStart = Math.Max(violation.StartOffset, lineStart);
        var segmentEnd = Math.Min(violation.EndOffset, lineEnd);

        if (segmentEnd <= segmentStart)
            return;

        // LOGIC: Get color for this severity
        var color = _colorProvider.GetUnderlineColor(violation.Severity);

        // LOGIC: Create and register the underline segment
        var segment = new UnderlineSegment(
            segmentStart,
            segmentEnd,
            color,
            violation.Id);

        _wavyRenderer!.AddSegment(segment);
    }

    /// <summary>
    /// Handles violation changes by invalidating the view.
    /// </summary>
    private void OnViolationsChanged(object? sender, ViolationsChangedEventArgs e)
    {
        if (_disposed)
            return;

        _logger.LogDebug(
            "Violations changed: {ChangeType}, Total={Total}",
            e.ChangeType,
            e.TotalCount);

        // LOGIC: Clear old segments and trigger re-render
        if (_wavyRenderer is not null)
        {
            _wavyRenderer.ClearSegments();
        }

        // LOGIC: Invalidate the TextView to trigger redraw
        // Must be on UI thread
        if (_textView is not null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    _textView?.InvalidateLayer(KnownLayer.Text);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate TextView layer");
                }
            });
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // LOGIC: Unsubscribe from events
        _provider.ViolationsChanged -= OnViolationsChanged;

        // LOGIC: Detach from view
        DetachFromTextView();

        _logger.LogDebug("StyleViolationRenderer disposed");
    }
}
