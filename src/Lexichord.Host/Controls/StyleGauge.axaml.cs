// ═══════════════════════════════════════════════════════════════════════════
// LEXICHORD - StyleGauge Control
// Circular progress indicator showing style score with dynamic coloring
// ═══════════════════════════════════════════════════════════════════════════

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Lexichord.Host.Controls;

/// <summary>
/// Circular gauge control displaying a style score from 0-100.
/// Color automatically transitions based on score thresholds:
/// - 0-49: Red (error)
/// - 50-79: Yellow (warning)
/// - 80-100: Green (success)
/// </summary>
public partial class StyleGauge : UserControl
{
    // ─────────────────────────────────────────────────────────────────────────
    // Color Constants (matching Lexichord design system)
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly Color ErrorColor = Color.Parse("#FFEF4444");
    private static readonly Color WarningColor = Color.Parse("#FFF59E0B");
    private static readonly Color SuccessColor = Color.Parse("#FF22C55E");

    // ─────────────────────────────────────────────────────────────────────────
    // Styled Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The score value (0-100).
    /// </summary>
    public static readonly StyledProperty<int> ScoreProperty =
        AvaloniaProperty.Register<StyleGauge, int>(nameof(Score), 0,
            coerce: (_, value) => Math.Clamp(value, 0, 100));

    public int Score
    {
        get => GetValue(ScoreProperty);
        set => SetValue(ScoreProperty, value);
    }

    /// <summary>
    /// Label text displayed below the gauge.
    /// </summary>
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<StyleGauge, string>(nameof(Label), "Style Score");

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Computed Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates the sweep angle for the arc based on score.
    /// Full arc is 270 degrees (from -225 to +45).
    /// </summary>
    public double SweepAngle => (Score / 100.0) * 270.0;

    /// <summary>
    /// Returns the appropriate color brush based on score thresholds.
    /// </summary>
    public IBrush GaugeColor
    {
        get
        {
            var color = Score switch
            {
                < 50 => ErrorColor,
                < 80 => WarningColor,
                _ => SuccessColor
            };
            return new SolidColorBrush(color);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────────

    public StyleGauge()
    {
        InitializeComponent();
        DataContext = this;

        // Update computed properties when Score changes
        this.GetObservable(ScoreProperty).Subscribe(_ =>
        {
            RaisePropertyChanged(SweepAngleProperty, default, SweepAngle);
            RaisePropertyChanged(GaugeColorProperty, default, GaugeColor);
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Direct Properties for Binding
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Direct property for SweepAngle binding (workaround for computed properties).
    /// </summary>
    public static readonly DirectProperty<StyleGauge, double> SweepAngleProperty =
        AvaloniaProperty.RegisterDirect<StyleGauge, double>(
            nameof(SweepAngle),
            o => o.SweepAngle);

    /// <summary>
    /// Direct property for GaugeColor binding.
    /// </summary>
    public static readonly DirectProperty<StyleGauge, IBrush?> GaugeColorProperty =
        AvaloniaProperty.RegisterDirect<StyleGauge, IBrush?>(
            nameof(GaugeColor),
            o => o.GaugeColor);
}
