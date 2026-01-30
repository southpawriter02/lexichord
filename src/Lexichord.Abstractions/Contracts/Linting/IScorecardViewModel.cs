using System.ComponentModel;

namespace Lexichord.Abstractions.Contracts.Linting;

#region Enums

/// <summary>
/// Represents the direction of score change over time.
/// </summary>
/// <remarks>
/// LOGIC: Trend is calculated by comparing current and previous scores.
/// A ±1% tolerance is used to avoid flickering between states.
///
/// Version: v0.2.6c
/// </remarks>
public enum ScoreTrend
{
    /// <summary>Score is within ±1% of previous score.</summary>
    Stable = 0,

    /// <summary>Score increased by more than 1% from previous.</summary>
    Improving = 1,

    /// <summary>Score decreased by more than 1% from previous.</summary>
    Declining = 2
}

#endregion

#region Scorecard ViewModel Interface

/// <summary>
/// ViewModel interface for the Scorecard Widget.
/// </summary>
/// <remarks>
/// LOGIC: The Scorecard Widget displays a compliance score calculated from
/// violation counts. It provides gamification elements to motivate users
/// to address style violations.
///
/// Score Calculation:
/// - Penalty = (Errors × 5) + (Warnings × 2) + (Info × 0.5)
/// - Score = max(0, 100 - Penalty)
///
/// Grade Scale:
/// - A: 90-100% (Green)
/// - B: 80-89% (Light Green)
/// - C: 70-79% (Yellow)
/// - D: 50-69% (Orange)
/// - F: 0-49% (Red)
///
/// Trend Calculation:
/// - Improving: Score > Previous + 1%
/// - Declining: Score &lt; Previous - 1%
/// - Stable: Within ±1% tolerance
///
/// Version: v0.2.6c
/// </remarks>
public interface IScorecardViewModel : INotifyPropertyChanged
{
    #region Violation Counts

    /// <summary>
    /// Gets the total count of error-severity violations.
    /// </summary>
    int TotalErrors { get; }

    /// <summary>
    /// Gets the total count of warning-severity violations.
    /// </summary>
    int TotalWarnings { get; }

    /// <summary>
    /// Gets the total count of info-severity violations.
    /// </summary>
    int TotalInfo { get; }

    /// <summary>
    /// Gets the total count of all violations.
    /// </summary>
    int TotalCount { get; }

    #endregion

    #region Score Properties

    /// <summary>
    /// Gets the current compliance score (0-100).
    /// </summary>
    double ComplianceScore { get; }

    /// <summary>
    /// Gets the previous compliance score for trend calculation.
    /// </summary>
    double PreviousScore { get; }

    /// <summary>
    /// Gets the letter grade (A-F) based on compliance score.
    /// </summary>
    string ScoreGrade { get; }

    /// <summary>
    /// Gets the hex color code for the score display.
    /// </summary>
    string ScoreColor { get; }

    /// <summary>
    /// Gets the formatted score display string (e.g., "85%").
    /// </summary>
    string ComplianceScoreDisplay { get; }

    #endregion

    #region Trend Properties

    /// <summary>
    /// Gets the current score trend direction.
    /// </summary>
    ScoreTrend Trend { get; }

    /// <summary>
    /// Gets the trend indicator icon (↑, ↓, or →).
    /// </summary>
    string TrendIcon { get; }

    /// <summary>
    /// Gets the hex color code for the trend indicator.
    /// </summary>
    string TrendColor { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Updates the scorecard with new violation counts.
    /// </summary>
    /// <param name="errors">Count of error-severity violations.</param>
    /// <param name="warnings">Count of warning-severity violations.</param>
    /// <param name="info">Count of info-severity violations.</param>
    /// <remarks>
    /// LOGIC: Recalculates compliance score, grade, color, and trend
    /// based on the new counts. Previous score is preserved for trend.
    /// </remarks>
    void Update(int errors, int warnings, int info);

    /// <summary>
    /// Resets the scorecard to its initial state.
    /// </summary>
    /// <remarks>
    /// LOGIC: Clears all counts, sets score to 100%, grade to A,
    /// and trend to Stable. Used when switching documents.
    /// </remarks>
    void Reset();

    #endregion
}

#endregion
