using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Editor.Views;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Editor.Services.Tooltip;

/// <summary>
/// Service for displaying violation tooltips on hover.
/// </summary>
/// <remarks>
/// LOGIC: ViolationTooltipService manages the tooltip lifecycle:
///
/// 1. Hook TextView's PointerMoved event to detect hover
/// 2. Calculate document offset from mouse position
/// 3. Query violations at that offset
/// 4. Display tooltip if violations exist
/// 5. Hide tooltip on mouse move away from violations
///
/// Positioning Strategy:
/// - Primary: Below the text (like VS Code)
/// - Fallback: Above the text if near bottom of window
/// - Horizontal: Aligned with violation start
///
/// Thread Safety:
/// - All operations run on the UI thread
/// - Uses Avalonia's event system
///
/// Version: v0.2.4c
/// </remarks>
public sealed class ViolationTooltipService : IDisposable
{
    private readonly IViolationProvider _violationProvider;
    private readonly IViolationColorProvider _colorProvider;
    private readonly ILogger<ViolationTooltipService> _logger;

    private TextArea? _textArea;
    private TextView? _textView;
    private Popup? _tooltipPopup;
    private ViolationTooltipView? _tooltipView;
    private IReadOnlyList<AggregatedStyleViolation>? _currentViolations;
    private int _currentViolationIndex;
    private bool _isDisposed;
    private bool _isWaitingForI;
    private DispatcherTimer? _hoverTimer;
    private Point _lastHoverPosition;
    private int? _lastHoverOffset;

    private const int DefaultHoverDelayMs = 500;

    /// <summary>
    /// Initializes a new instance of ViolationTooltipService.
    /// </summary>
    public ViolationTooltipService(
        IViolationProvider violationProvider,
        IViolationColorProvider colorProvider,
        ILogger<ViolationTooltipService> logger)
    {
        _violationProvider = violationProvider ?? throw new ArgumentNullException(nameof(violationProvider));
        _colorProvider = colorProvider ?? throw new ArgumentNullException(nameof(colorProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets whether the tooltip is currently visible.
    /// </summary>
    public bool IsTooltipVisible => _tooltipPopup?.IsOpen ?? false;

    /// <summary>
    /// Gets or sets the hover delay in milliseconds.
    /// </summary>
    public int HoverDelayMs { get; set; } = DefaultHoverDelayMs;

    /// <summary>
    /// Attaches the tooltip service to a TextArea.
    /// </summary>
    /// <param name="textArea">The TextArea to attach to.</param>
    public void AttachToTextArea(TextArea textArea)
    {
        DetachFromTextArea();

        _textArea = textArea ?? throw new ArgumentNullException(nameof(textArea));
        _textView = textArea.TextView;

        // LOGIC: Subscribe to pointer events on TextView (not TextArea)
        _textView.PointerMoved += OnPointerMoved;
        _textView.PointerExited += OnPointerExited;

        // LOGIC: Create tooltip popup
        _tooltipView = new ViolationTooltipView();
        _tooltipView.NavigateRequested += OnNavigateRequested;

        _tooltipPopup = new Popup
        {
            Child = _tooltipView,
            Placement = PlacementMode.Pointer,
            IsLightDismissEnabled = false,
        };

        // LOGIC: Create hover delay timer
        _hoverTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(HoverDelayMs)
        };
        _hoverTimer.Tick += OnHoverTimerTick;

        _logger.LogDebug("ViolationTooltipService attached to TextArea");
    }

    /// <summary>
    /// Detaches the tooltip service from its TextArea.
    /// </summary>
    public void DetachFromTextArea()
    {
        if (_textView is not null)
        {
            _textView.PointerMoved -= OnPointerMoved;
            _textView.PointerExited -= OnPointerExited;
        }

        if (_tooltipView is not null)
        {
            _tooltipView.NavigateRequested -= OnNavigateRequested;
            _tooltipView = null;
        }

        _hoverTimer?.Stop();
        _hoverTimer = null;

        HideTooltip();
        _tooltipPopup = null;
        _textArea = null;
        _textView = null;
    }

    /// <summary>
    /// Hides the tooltip.
    /// </summary>
    public void HideTooltip()
    {
        if (_tooltipPopup?.IsOpen == true)
        {
            _tooltipPopup.IsOpen = false;
            _logger.LogDebug("Tooltip hidden");
        }

        _currentViolations = null;
        _currentViolationIndex = 0;
    }

    /// <summary>
    /// Shows tooltip at the current caret position.
    /// </summary>
    /// <remarks>
    /// LOGIC: Called when user presses Ctrl+K, Ctrl+I (VS Code convention).
    /// Shows tooltip for violation at caret.
    /// </remarks>
    public void ShowTooltipAtCaret()
    {
        if (_textArea is null || _textView is null)
            return;

        var caret = _textArea.Caret;
        var offset = caret.Offset;

        var violations = _violationProvider.GetViolationsAtOffset(offset);

        if (violations.Count == 0)
        {
            _logger.LogDebug("No violations at caret position {Offset}", offset);
            return;
        }

        // LOGIC: Get visual position of caret
        var visualLine = _textView.GetVisualLine(caret.Line);
        if (visualLine is null)
            return;

        var position = visualLine.GetVisualPosition(
            caret.Column - 1,
            VisualYPosition.LineBottom);

        ShowTooltipInternal(new Point(position.X, position.Y + 4), violations);
    }

    /// <summary>
    /// Registers keyboard shortcuts for tooltip display.
    /// </summary>
    /// <param name="textArea">TextArea to register shortcuts on.</param>
    public void RegisterKeyboardShortcuts(TextArea textArea)
    {
        // LOGIC: Ctrl+K, Ctrl+I to show tooltip (VS Code convention)
        textArea.KeyDown += (s, e) =>
        {
            if (e.Key == Key.I &&
                e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
                _isWaitingForI)
            {
                _isWaitingForI = false;
                ShowTooltipAtCaret();
                e.Handled = true;
            }
            else if (e.Key == Key.K && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                _isWaitingForI = true;
                // Set timeout to reset flag
                DispatcherTimer.RunOnce(() => _isWaitingForI = false, TimeSpan.FromSeconds(2));
            }
            else if (e.Key == Key.Escape && IsTooltipVisible)
            {
                HideTooltip();
                e.Handled = true;
            }
            else
            {
                _isWaitingForI = false;
            }
        };
    }

    /// <summary>
    /// Handles pointer moved event - starts hover timer when over violation.
    /// </summary>
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_textView is null || _textArea is null)
            return;

        var position = e.GetPosition(_textView);
        var offset = GetOffsetFromPoint(position);

        // LOGIC: If we moved to a different offset, reset hover
        if (offset != _lastHoverOffset)
        {
            _hoverTimer?.Stop();
            _lastHoverOffset = null;

            // Check if over a violation
            if (offset is not null)
            {
                var violations = _violationProvider.GetViolationsAtOffset(offset.Value);

                if (violations.Count > 0)
                {
                    // LOGIC: Start hover timer
                    _lastHoverPosition = position;
                    _lastHoverOffset = offset;
                    _hoverTimer?.Start();
                }
                else if (IsTooltipVisible)
                {
                    // LOGIC: Moved off violation area
                    HideTooltip();
                }
            }
            else if (IsTooltipVisible)
            {
                HideTooltip();
            }
        }
    }

    /// <summary>
    /// Handles pointer exit from TextView.
    /// </summary>
    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        _hoverTimer?.Stop();
        _lastHoverOffset = null;

        // LOGIC: Don't hide immediately if mouse is over the tooltip
        if (_tooltipPopup?.IsPointerOver == true)
            return;

        HideTooltip();
    }

    /// <summary>
    /// Handles hover timer tick - shows tooltip after delay.
    /// </summary>
    private void OnHoverTimerTick(object? sender, EventArgs e)
    {
        _hoverTimer?.Stop();

        if (_lastHoverOffset is null || _textArea is null)
            return;

        var violations = _violationProvider.GetViolationsAtOffset(_lastHoverOffset.Value);

        if (violations.Count > 0)
        {
            ShowTooltipInternal(_lastHoverPosition, violations);
        }
    }

    /// <summary>
    /// Handles navigation between multiple violations.
    /// </summary>
    private void OnNavigateRequested(object? sender, NavigateViolationEventArgs e)
    {
        if (_currentViolations is null || _currentViolations.Count <= 1)
            return;

        _currentViolationIndex = e.Direction switch
        {
            NavigateDirection.Previous =>
                (_currentViolationIndex - 1 + _currentViolations.Count) % _currentViolations.Count,
            NavigateDirection.Next =>
                (_currentViolationIndex + 1) % _currentViolations.Count,
            _ => _currentViolationIndex
        };

        UpdateTooltipContent();
    }

    /// <summary>
    /// Gets document offset from a point in the TextView.
    /// </summary>
    /// <param name="point">Point relative to TextView.</param>
    /// <returns>Document offset, or null if not over text.</returns>
    private int? GetOffsetFromPoint(Point point)
    {
        if (_textView is null)
            return null;

        // LOGIC: Use AvalonEdit's built-in method
        var position = _textView.GetPositionFloor(point);

        if (position is null)
            return null;

        var textPosition = position.Value;

        // LOGIC: Get the document line
        var document = _textView.Document;
        if (document is null || textPosition.Line < 1 || textPosition.Line > document.LineCount)
            return null;

        var line = document.GetLineByNumber(textPosition.Line);

        // LOGIC: Calculate offset within line
        var columnOffset = Math.Min(textPosition.Column - 1, line.Length);
        var offset = line.Offset + columnOffset;

        return offset;
    }

    /// <summary>
    /// Shows tooltip for multiple violations at a position.
    /// </summary>
    private void ShowTooltipInternal(Point position, IReadOnlyList<AggregatedStyleViolation> violations)
    {
        if (violations.Count == 0 || _tooltipPopup is null || _textArea is null)
            return;

        _currentViolations = violations;
        _currentViolationIndex = 0;

        UpdateTooltipContent();

        // LOGIC: Position popup near the hovered location
        _tooltipPopup.PlacementTarget = _textArea;
        _tooltipPopup.HorizontalOffset = position.X;
        _tooltipPopup.VerticalOffset = position.Y + 16; // Below the baseline
        _tooltipPopup.IsOpen = true;

        _logger.LogDebug(
            "Showing tooltip at ({X}, {Y}) with {Count} violation(s)",
            position.X, position.Y, violations.Count);
    }

    /// <summary>
    /// Updates the tooltip content for the current violation.
    /// </summary>
    private void UpdateTooltipContent()
    {
        if (_tooltipView is null || _currentViolations is null || _currentViolations.Count == 0)
            return;

        var violation = _currentViolations[_currentViolationIndex];

        // LOGIC: Get color and icon from provider
        var severity = violation.Severity;
        var borderColor = _colorProvider.GetTooltipBorderColor(severity);
        var iconPath = _colorProvider.GetSeverityIcon(severity);

        // LOGIC: Convert UnderlineColor to Avalonia.Media.Color
        var avaloniaColor = Color.FromArgb(
            borderColor.A,
            borderColor.R,
            borderColor.G,
            borderColor.B);

        _tooltipView.SetViolation(
            violation.RuleId,
            violation.Message,
            violation.Suggestion, // Use Suggestion instead of Recommendation
            null, // Explanation not in AggregatedStyleViolation
            avaloniaColor,
            iconPath,
            _currentViolationIndex + 1,
            _currentViolations.Count);
    }

    /// <summary>
    /// Disposes of the tooltip service.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        DetachFromTextArea();
        _isDisposed = true;

        _logger.LogDebug("ViolationTooltipService disposed");
    }
}
