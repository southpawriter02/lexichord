// -----------------------------------------------------------------------
// <copyright file="ChangeSignificance.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Enumeration defining the 4 significance levels for document changes (v0.7.6d).
//   Each level represents how important/impactful a change is to the document.
//
//   Levels:
//     - Low (0.0-0.3): Trivial changes (typos, formatting)
//     - Medium (0.3-0.6): Notable changes (clarifications, minor additions)
//     - High (0.6-0.8): Important changes (new sections, major modifications)
//     - Critical (0.8-1.0): Critical changes (core message, breaking changes)
//
//   Also provides extension method FromScore for converting double to enum.
//
//   Introduced in: v0.7.6d as part of Document Comparison
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.DocumentComparison;

/// <summary>
/// Significance level for document changes, used for filtering and prioritization.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Each significance level corresponds to a score range from 0.0 to 1.0.
/// The LLM assigns a significance score to each detected change, which is then mapped
/// to one of these discrete levels for easier filtering and UI display.
/// </para>
/// <para>
/// <b>Score Ranges:</b>
/// <list type="bullet">
/// <item><description><see cref="Low"/>: 0.0 - 0.3 (exclusive)</description></item>
/// <item><description><see cref="Medium"/>: 0.3 - 0.6 (exclusive)</description></item>
/// <item><description><see cref="High"/>: 0.6 - 0.8 (exclusive)</description></item>
/// <item><description><see cref="Critical"/>: 0.8 - 1.0 (inclusive)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>UI Display:</b>
/// Each significance level has an associated color:
/// <list type="bullet">
/// <item><description><see cref="Low"/>: Muted gray</description></item>
/// <item><description><see cref="Medium"/>: Blue/info</description></item>
/// <item><description><see cref="High"/>: Orange/warning</description></item>
/// <item><description><see cref="Critical"/>: Red/error</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Filtering:</b>
/// Use <see cref="ComparisonOptions.SignificanceThreshold"/> to filter out changes
/// below a certain significance score. Default threshold is 0.2 (shows all but trivial).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6d as part of the Document Comparison feature.
/// </para>
/// </remarks>
public enum ChangeSignificance
{
    /// <summary>
    /// Trivial changes with significance score 0.0 to 0.3.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Examples:</b> Typo corrections, whitespace adjustments, minor word choice changes,
    /// formatting tweaks, comment updates.
    /// </para>
    /// <para>
    /// <b>Display:</b> These changes are often collapsed or hidden by default in the UI
    /// to focus attention on more important changes.
    /// </para>
    /// </remarks>
    Low = 0,

    /// <summary>
    /// Notable changes with significance score 0.3 to 0.6.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Examples:</b> Clarifications to existing content, minor reorganization,
    /// updated references or links, supporting detail changes, examples added or modified.
    /// </para>
    /// <para>
    /// <b>Display:</b> These changes are shown but may be collapsed in groups.
    /// </para>
    /// </remarks>
    Medium = 1,

    /// <summary>
    /// Important changes with significance score 0.6 to 0.8.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Examples:</b> New major sections added, significant content removed,
    /// important details updated, new requirements or constraints, key arguments modified.
    /// </para>
    /// <para>
    /// <b>Display:</b> These changes are prominently displayed and expanded by default.
    /// </para>
    /// </remarks>
    High = 2,

    /// <summary>
    /// Critical changes with significance score 0.8 to 1.0.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Examples:</b> Changes to document title or main thesis, modifications to key conclusions,
    /// breaking changes to APIs or interfaces, significant factual corrections,
    /// major structural reorganization.
    /// </para>
    /// <para>
    /// <b>Display:</b> These changes are shown at the top of the list with prominent
    /// red/critical styling to ensure they are not missed.
    /// </para>
    /// </remarks>
    Critical = 3
}

/// <summary>
/// Extension methods for <see cref="ChangeSignificance"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Provides utility methods for working with significance levels,
/// including conversion from numeric scores to enum values.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6d as part of the Document Comparison feature.
/// </para>
/// </remarks>
public static class ChangeSignificanceExtensions
{
    /// <summary>
    /// Converts a significance score (0.0 to 1.0) to a <see cref="ChangeSignificance"/> level.
    /// </summary>
    /// <param name="score">The significance score, clamped to range [0.0, 1.0].</param>
    /// <returns>The corresponding <see cref="ChangeSignificance"/> level.</returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Maps the continuous score to discrete levels:
    /// <list type="bullet">
    /// <item><description>score &gt;= 0.8: <see cref="ChangeSignificance.Critical"/></description></item>
    /// <item><description>score &gt;= 0.6: <see cref="ChangeSignificance.High"/></description></item>
    /// <item><description>score &gt;= 0.3: <see cref="ChangeSignificance.Medium"/></description></item>
    /// <item><description>score &lt; 0.3: <see cref="ChangeSignificance.Low"/></description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Scores outside the [0.0, 1.0] range are clamped before conversion.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var level = ChangeSignificanceExtensions.FromScore(0.75); // Returns High
    /// var critical = ChangeSignificanceExtensions.FromScore(0.95); // Returns Critical
    /// var low = ChangeSignificanceExtensions.FromScore(0.1); // Returns Low
    /// </code>
    /// </example>
    public static ChangeSignificance FromScore(double score)
    {
        // LOGIC: Clamp score to valid range [0.0, 1.0]
        var clampedScore = Math.Clamp(score, 0.0, 1.0);

        // LOGIC: Map to discrete significance level using threshold boundaries
        return clampedScore switch
        {
            >= 0.8 => ChangeSignificance.Critical,
            >= 0.6 => ChangeSignificance.High,
            >= 0.3 => ChangeSignificance.Medium,
            _ => ChangeSignificance.Low
        };
    }

    /// <summary>
    /// Gets the minimum score threshold for a significance level.
    /// </summary>
    /// <param name="significance">The significance level.</param>
    /// <returns>The minimum score for this level.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Returns the lower bound of the score range for each level.
    /// Useful for filtering and threshold calculations.
    /// </remarks>
    public static double GetMinimumScore(this ChangeSignificance significance) => significance switch
    {
        ChangeSignificance.Critical => 0.8,
        ChangeSignificance.High => 0.6,
        ChangeSignificance.Medium => 0.3,
        _ => 0.0
    };

    /// <summary>
    /// Gets the display label for a significance level.
    /// </summary>
    /// <param name="significance">The significance level.</param>
    /// <returns>A human-readable label for display in the UI.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Returns uppercase labels matching the UI design spec.
    /// </remarks>
    public static string GetDisplayLabel(this ChangeSignificance significance) => significance switch
    {
        ChangeSignificance.Critical => "CRITICAL",
        ChangeSignificance.High => "HIGH",
        ChangeSignificance.Medium => "MEDIUM",
        _ => "LOW"
    };
}
