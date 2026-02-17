// -----------------------------------------------------------------------
// <copyright file="ScannerOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Agents.Tuning.Configuration;

/// <summary>
/// Configuration options for the <see cref="StyleDeviationScanner"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> These options control the behavior of the Style Deviation Scanner:
/// <list type="bullet">
///   <item><description>Context extraction for AI prompts</description></item>
///   <item><description>Cache time-to-live settings</description></item>
///   <item><description>Severity filtering thresholds</description></item>
///   <item><description>Real-time update behavior</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Configuration:</b> Options can be configured via DI using <c>IOptions&lt;ScannerOptions&gt;</c>
/// or programmatically via <see cref="TuningServiceCollectionExtensions.AddStyleDeviationScanner"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5a as part of the Style Deviation Scanner feature.
/// </para>
/// </remarks>
public class ScannerOptions
{
    /// <summary>
    /// Number of characters of context to include before and after each violation.
    /// </summary>
    /// <value>Default: 500 characters in each direction.</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> The surrounding context helps the AI understand:
    /// <list type="bullet">
    ///   <item><description>Document tone and style</description></item>
    ///   <item><description>Paragraph structure</description></item>
    ///   <item><description>Related terminology</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Trade-offs:</b> Larger windows provide more context but increase token usage.
    /// 500 characters typically captures 1-2 sentences before and after.
    /// </para>
    /// </remarks>
    public int ContextWindowSize { get; set; } = 500;

    /// <summary>
    /// Cache time-to-live in minutes for scan results.
    /// </summary>
    /// <value>Default: 5 minutes.</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Cached results are invalidated after this duration even if content
    /// hasn't changed. Short TTL ensures reasonably fresh results without excessive scanning.
    /// </para>
    /// <para>
    /// <b>Note:</b> Cache entries also use sliding expiration at half the TTL value.
    /// </para>
    /// </remarks>
    public int CacheTtlMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum number of deviations to return per scan.
    /// </summary>
    /// <value>Default: 100. Set to 0 for unlimited.</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Prevents overwhelming the UI with too many results. When the limit
    /// is reached, additional violations are logged but not returned. The UI should
    /// indicate when results are truncated.
    /// </para>
    /// <para>
    /// <b>Recommendation:</b> Keep at 100 for typical documents. Increase for large
    /// documents with many expected violations.
    /// </para>
    /// </remarks>
    public int MaxDeviationsPerScan { get; set; } = 100;

    /// <summary>
    /// Whether to include manual-only (non-auto-fixable) violations in results.
    /// </summary>
    /// <value>Default: true.</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> When <c>false</c>, only violations that can be auto-fixed by AI
    /// are returned. Useful for automated batch processing where manual review isn't planned.
    /// </para>
    /// </remarks>
    public bool IncludeManualOnly { get; set; } = true;

    /// <summary>
    /// Minimum severity level to include in results.
    /// </summary>
    /// <value>Default: <see cref="ViolationSeverity.Hint"/> (include all).</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Violations below this severity are filtered out. Severity order
    /// from highest to lowest: Error, Warning, Info, Hint.
    /// </para>
    /// <para>
    /// <b>Example:</b> Setting to <see cref="ViolationSeverity.Warning"/> excludes
    /// Info and Hint level violations.
    /// </para>
    /// </remarks>
    public ViolationSeverity MinimumSeverity { get; set; } = ViolationSeverity.Hint;

    /// <summary>
    /// Whether to subscribe to real-time linting events for live updates.
    /// </summary>
    /// <value>Default: true.</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> When <c>true</c>, the scanner subscribes to <see cref="LintingCompletedEvent"/>
    /// and automatically re-scans open documents when linting completes. The
    /// <see cref="IStyleDeviationScanner.DeviationsDetected"/> event is raised with new results.
    /// </para>
    /// <para>
    /// <b>Performance:</b> Set to <c>false</c> for batch processing scenarios where
    /// real-time updates aren't needed.
    /// </para>
    /// </remarks>
    public bool EnableRealTimeUpdates { get; set; } = true;

    /// <summary>
    /// Categories to exclude from scanning.
    /// </summary>
    /// <value>Default: empty (include all categories).</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Violations in these categories are filtered out. Categories are
    /// matched against <see cref="RuleCategory"/> names (Terminology, Formatting, Syntax).
    /// </para>
    /// <para>
    /// <b>Example:</b> Set to ["Formatting"] to exclude formatting-related violations
    /// that are typically handled by formatters rather than AI fixes.
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> ExcludedCategories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Whether to attempt context boundary adjustment to sentence/paragraph edges.
    /// </summary>
    /// <value>Default: true.</value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> When <c>true</c>, the scanner attempts to expand the context window
    /// to natural boundaries (sentence endings, paragraph breaks) rather than cutting
    /// mid-sentence. This provides cleaner context for AI analysis.
    /// </para>
    /// </remarks>
    public bool AdjustContextToBoundaries { get; set; } = true;
}
