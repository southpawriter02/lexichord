using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Contracts.Linting;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.ViewModels;

/// <summary>
/// ViewModel for the Scorecard Widget displaying compliance score.
/// </summary>
/// <remarks>
/// LOGIC: Calculates and displays a compliance score based on violation
/// counts. The score uses a penalty-based formula with weighted severities.
///
/// Penalty Weights:
/// - Error: 5 points
/// - Warning: 2 points
/// - Info: 0.5 points
///
/// Formula: Score = max(0, 100 - Penalty)
///
/// Grade Scale:
/// - A: 90-100% (#22C55E Green)
/// - B: 80-89% (#84CC16 Light Green)
/// - C: 70-79% (#EAB308 Yellow)
/// - D: 50-69% (#F97316 Orange)
/// - F: 0-49% (#EF4444 Red)
///
/// Version: v0.2.6c
/// </remarks>
public partial class ScorecardViewModel : ObservableObject, IScorecardViewModel
{
    private readonly ILogger<ScorecardViewModel> _logger;

    #region Penalty Weights (Constants)

    /// <summary>Penalty points per error violation.</summary>
    private const double ErrorPenalty = 5.0;

    /// <summary>Penalty points per warning violation.</summary>
    private const double WarningPenalty = 2.0;

    /// <summary>Penalty points per info violation.</summary>
    private const double InfoPenalty = 0.5;

    /// <summary>Trend stability tolerance in percentage points.</summary>
    private const double TrendTolerance = 1.0;

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private int _totalErrors;

    [ObservableProperty]
    private int _totalWarnings;

    [ObservableProperty]
    private int _totalInfo;

    [ObservableProperty]
    private double _complianceScore = 100.0;

    [ObservableProperty]
    private double _previousScore = 100.0;

    [ObservableProperty]
    private ScoreTrend _trend = ScoreTrend.Stable;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the total count of all violations.
    /// </summary>
    public int TotalCount => TotalErrors + TotalWarnings + TotalInfo;

    /// <summary>
    /// Gets the letter grade based on compliance score.
    /// </summary>
    /// <remarks>
    /// LOGIC: Grade boundaries per specification:
    /// - A: 90-100
    /// - B: 80-89
    /// - C: 70-79
    /// - D: 50-69
    /// - F: 0-49
    /// </remarks>
    public string ScoreGrade => ComplianceScore switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 50 => "D",
        _ => "F"
    };

    /// <summary>
    /// Gets the hex color code for the score display.
    /// </summary>
    /// <remarks>
    /// LOGIC: Colors matched to grade scale per specification.
    /// </remarks>
    public string ScoreColor => ComplianceScore switch
    {
        >= 90 => "#22C55E", // Green (A)
        >= 80 => "#84CC16", // Light Green (B)
        >= 70 => "#EAB308", // Yellow (C)
        >= 50 => "#F97316", // Orange (D)
        _ => "#EF4444"      // Red (F)
    };

    /// <summary>
    /// Gets the formatted compliance score display.
    /// </summary>
    public string ComplianceScoreDisplay => $"{ComplianceScore:F0}%";

    /// <summary>
    /// Gets the trend indicator icon.
    /// </summary>
    /// <remarks>
    /// LOGIC: Unicode arrows for visual trend indication.
    /// </remarks>
    public string TrendIcon => Trend switch
    {
        ScoreTrend.Improving => "↑",
        ScoreTrend.Declining => "↓",
        _ => "→"
    };

    /// <summary>
    /// Gets the trend indicator color.
    /// </summary>
    /// <remarks>
    /// LOGIC: Green for improving, red for declining, gray for stable.
    /// </remarks>
    public string TrendColor => Trend switch
    {
        ScoreTrend.Improving => "#22C55E", // Green
        ScoreTrend.Declining => "#EF4444", // Red
        _ => "#9CA3AF"                      // Gray
    };

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ScorecardViewModel"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ScorecardViewModel(ILogger<ScorecardViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Update sequence:
    /// 1. Store current score as previous
    /// 2. Update violation counts (clamped to 0+)
    /// 3. Calculate penalty using weighted formula
    /// 4. Calculate new score (clamped 0-100)
    /// 5. Calculate trend based on delta
    /// 6. Notify all computed property changes
    /// 7. Log update at Information level
    /// </remarks>
    public void Update(int errors, int warnings, int info)
    {
        // STEP 1: Preserve previous score for trend calculation
        PreviousScore = ComplianceScore;

        // STEP 2: Update counts (ensure non-negative)
        TotalErrors = Math.Max(0, errors);
        TotalWarnings = Math.Max(0, warnings);
        TotalInfo = Math.Max(0, info);

        // STEP 3: Calculate penalty
        var penalty = (TotalErrors * ErrorPenalty) +
                      (TotalWarnings * WarningPenalty) +
                      (TotalInfo * InfoPenalty);

        // STEP 4: Calculate score (clamped 0-100)
        var rawScore = 100 - penalty;
        ComplianceScore = Math.Max(0, Math.Min(100, rawScore));

        // STEP 5: Calculate trend with tolerance
        var delta = ComplianceScore - PreviousScore;
        Trend = delta switch
        {
            > TrendTolerance => ScoreTrend.Improving,
            < -TrendTolerance => ScoreTrend.Declining,
            _ => ScoreTrend.Stable
        };

        // STEP 6: Notify computed property changes
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ScoreGrade));
        OnPropertyChanged(nameof(ScoreColor));
        OnPropertyChanged(nameof(ComplianceScoreDisplay));
        OnPropertyChanged(nameof(TrendIcon));
        OnPropertyChanged(nameof(TrendColor));

        // STEP 7: Log update
        _logger.LogInformation(
            "Scorecard updated: {Score}% ({Grade}) - E:{Errors} W:{Warnings} I:{Info} - Trend:{Trend}",
            ComplianceScore, ScoreGrade, TotalErrors, TotalWarnings, TotalInfo, Trend);

        _logger.LogDebug(
            "Score calculation: Penalty={Penalty} (E:{ErrorPen}+W:{WarningPen}+I:{InfoPen})",
            penalty, TotalErrors * ErrorPenalty, TotalWarnings * WarningPenalty, TotalInfo * InfoPenalty);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Reset to pristine state (100% score, A grade, no violations).
    /// Used when switching documents or clearing the panel.
    /// </remarks>
    public void Reset()
    {
        TotalErrors = 0;
        TotalWarnings = 0;
        TotalInfo = 0;
        ComplianceScore = 100.0;
        PreviousScore = 100.0;
        Trend = ScoreTrend.Stable;

        // Notify computed property changes
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ScoreGrade));
        OnPropertyChanged(nameof(ScoreColor));
        OnPropertyChanged(nameof(ComplianceScoreDisplay));
        OnPropertyChanged(nameof(TrendIcon));
        OnPropertyChanged(nameof(TrendColor));

        _logger.LogInformation("Scorecard reset to initial state");
    }
}
