// =============================================================================
// File: FixConflictDetector.cs
// Project: Lexichord.Modules.Agents
// Description: Detects conflicts between fixes before application.
// =============================================================================
// LOGIC: Analyzes a set of issues with fixes for overlapping positions,
//   contradictory suggestions, dependent fixes, and invalid locations.
//   Called before fix application to prevent document corruption.
//
// v0.7.5h: Combined Fix Workflow (Unified Validation Feature)
// =============================================================================

using Lexichord.Abstractions.Contracts.Validation;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Tuning.FixOrchestration;

/// <summary>
/// Detects conflicts between fixes before application.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Performs multiple conflict detection heuristics:
/// <list type="number">
///   <item><description>Overlapping positions — fixes that target overlapping text regions</description></item>
///   <item><description>Contradictory suggestions — same location, different replacement text</description></item>
///   <item><description>Dependent fixes — Style/Grammar fixes in close proximity</description></item>
///   <item><description>Invalid locations — fix positions outside document bounds</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Performance:</b> O(n²) worst case for n issues (pairwise comparison for
/// overlap detection). Acceptable for typical validation results (≤100 issues).
/// </para>
/// <para>
/// <b>Spec Adaptation:</b> The spec proposes async <c>DetectIssueCausingFixesAsync</c>
/// that re-validates the document for each fix. This is too expensive for synchronous
/// conflict detection. Instead, cascading issues are caught by post-application
/// re-validation in the orchestrator.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is stateless and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
internal class FixConflictDetector
{
    /// <summary>
    /// Proximity threshold (in characters) for detecting dependent fixes.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Style and Grammar fixes within this distance are considered
    /// potentially dependent, because a Style fix may change the text that a
    /// Grammar fix targets.
    /// </remarks>
    private const int DependencyProximityThreshold = 200;

    private readonly ILogger<FixConflictDetector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixConflictDetector"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    public FixConflictDetector(ILogger<FixConflictDetector> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Runs all conflict detection heuristics on the provided issues.
    /// </summary>
    /// <param name="issues">Issues with fixes to analyze.</param>
    /// <returns>List of detected conflicts, empty if no conflicts found.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issues"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Runs all detection methods and aggregates results.
    /// Only analyzes issues that have a non-null <see cref="UnifiedIssue.BestFix"/>.
    /// </remarks>
    public IReadOnlyList<FixConflictCase> Detect(IReadOnlyList<UnifiedIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        // LOGIC: Filter to only issues with available fixes.
        var fixableIssues = issues
            .Where(i => i.BestFix is not null)
            .ToList();

        if (fixableIssues.Count < 2)
        {
            _logger.LogDebug(
                "Skipping conflict detection: only {Count} fixable issues",
                fixableIssues.Count);
            return [];
        }

        _logger.LogDebug(
            "Running conflict detection on {Count} fixable issues",
            fixableIssues.Count);

        var conflicts = new List<FixConflictCase>();

        // LOGIC: Run all detection heuristics.
        conflicts.AddRange(DetectOverlappingFixes(fixableIssues));
        conflicts.AddRange(DetectContradictorySuggestions(fixableIssues));
        conflicts.AddRange(DetectDependentFixes(fixableIssues));

        if (conflicts.Count > 0)
        {
            _logger.LogInformation(
                "Detected {ConflictCount} conflicts: {ErrorCount} errors, {WarningCount} warnings, {InfoCount} info",
                conflicts.Count,
                conflicts.Count(c => c.Severity == FixConflictSeverity.Error),
                conflicts.Count(c => c.Severity == FixConflictSeverity.Warning),
                conflicts.Count(c => c.Severity == FixConflictSeverity.Info));
        }
        else
        {
            _logger.LogDebug("No conflicts detected");
        }

        return conflicts;
    }

    /// <summary>
    /// Validates fix locations against the document length.
    /// </summary>
    /// <param name="issues">Issues with fixes to validate.</param>
    /// <param name="documentLength">Length of the document content.</param>
    /// <returns>List of invalid location conflicts.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issues"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Checks that each fix's <see cref="Editor.TextSpan"/> is within
    /// the document bounds. Called separately because it requires the document length.
    /// </remarks>
    public IReadOnlyList<FixConflictCase> ValidateLocations(
        IReadOnlyList<UnifiedIssue> issues,
        int documentLength)
    {
        ArgumentNullException.ThrowIfNull(issues);

        var conflicts = new List<FixConflictCase>();

        foreach (var issue in issues.Where(i => i.BestFix is not null))
        {
            var fix = issue.BestFix!;

            // LOGIC: Check if fix location is within document bounds.
            if (fix.Location.Start < 0 ||
                fix.Location.End > documentLength ||
                fix.Location.Length < 0)
            {
                _logger.LogWarning(
                    "Fix for issue {IssueId} has invalid location: Start={Start}, Length={Length}, DocumentLength={DocLength}",
                    issue.IssueId, fix.Location.Start, fix.Location.Length, documentLength);

                conflicts.Add(new FixConflictCase
                {
                    Type = FixConflictType.InvalidLocation,
                    ConflictingIssueIds = [issue.IssueId],
                    Description = $"Fix for '{issue.SourceId}' has location [{fix.Location.Start}, {fix.Location.End}] " +
                                  $"outside document bounds [0, {documentLength}]",
                    Severity = FixConflictSeverity.Error
                });
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Detects fixes that target overlapping text regions.
    /// </summary>
    private IReadOnlyList<FixConflictCase> DetectOverlappingFixes(
        IReadOnlyList<UnifiedIssue> issues)
    {
        var conflicts = new List<FixConflictCase>();

        // LOGIC: Sort by Start position for efficient pairwise comparison.
        var sorted = issues
            .OrderBy(i => i.Location.Start)
            .ToList();

        for (var i = 0; i < sorted.Count; i++)
        {
            for (var j = i + 1; j < sorted.Count; j++)
            {
                var issue1 = sorted[i];
                var issue2 = sorted[j];

                // LOGIC: Use TextSpan.OverlapsWith() for overlap detection.
                if (issue1.BestFix!.Location.OverlapsWith(issue2.BestFix!.Location))
                {
                    _logger.LogDebug(
                        "Overlapping fixes detected: {Issue1} [{Start1},{End1}] overlaps {Issue2} [{Start2},{End2}]",
                        issue1.SourceId, issue1.BestFix.Location.Start, issue1.BestFix.Location.End,
                        issue2.SourceId, issue2.BestFix.Location.Start, issue2.BestFix.Location.End);

                    conflicts.Add(new FixConflictCase
                    {
                        Type = FixConflictType.OverlappingPositions,
                        ConflictingIssueIds = [issue1.IssueId, issue2.IssueId],
                        Description = $"Fix for '{issue1.SourceId}' at [{issue1.BestFix.Location.Start}, {issue1.BestFix.Location.End}] " +
                                      $"overlaps with fix for '{issue2.SourceId}' at [{issue2.BestFix.Location.Start}, {issue2.BestFix.Location.End}]",
                        Severity = FixConflictSeverity.Error
                    });
                }

                // LOGIC: Early exit optimization — if issue2's start is beyond issue1's end,
                // no further issues can overlap with issue1 (since sorted by start).
                if (issue2.Location.Start >= issue1.BestFix!.Location.End)
                    break;
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Detects fixes that suggest different changes to the same location.
    /// </summary>
    private IReadOnlyList<FixConflictCase> DetectContradictorySuggestions(
        IReadOnlyList<UnifiedIssue> issues)
    {
        var conflicts = new List<FixConflictCase>();

        // LOGIC: Group by fix location (Start, Length) to find issues targeting the same text.
        var grouped = issues
            .GroupBy(i => (i.BestFix!.Location.Start, i.BestFix.Location.Length));

        foreach (var group in grouped.Where(g => g.Count() > 1))
        {
            // LOGIC: Check if the fixes suggest different replacement text.
            var distinctSuggestions = group
                .Select(i => i.BestFix!.NewText ?? "")
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (distinctSuggestions.Count > 1)
            {
                var issueIds = group.Select(i => i.IssueId).ToList();
                var sourceIds = group.Select(i => i.SourceId).ToList();

                _logger.LogDebug(
                    "Contradictory suggestions at position {Start}+{Length}: {Sources} suggest {Count} different replacements",
                    group.Key.Start, group.Key.Length, string.Join(", ", sourceIds), distinctSuggestions.Count);

                conflicts.Add(new FixConflictCase
                {
                    Type = FixConflictType.ContradictorySuggestions,
                    ConflictingIssueIds = issueIds,
                    Description = $"Fixes at position [{group.Key.Start}, {group.Key.Start + group.Key.Length}] " +
                                  $"suggest {distinctSuggestions.Count} different replacements: " +
                                  string.Join(", ", distinctSuggestions.Select(s => $"'{s}'")),
                    Severity = FixConflictSeverity.Error
                });
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Detects fixes where one depends on another's successful application.
    /// </summary>
    private IReadOnlyList<FixConflictCase> DetectDependentFixes(
        IReadOnlyList<UnifiedIssue> issues)
    {
        var conflicts = new List<FixConflictCase>();

        // LOGIC: Check for Style and Grammar fixes in close proximity.
        // A Grammar fix may become invalid if a Style fix changes nearby text.
        var styleIssues = issues
            .Where(i => i.Category == IssueCategory.Style)
            .ToList();

        var grammarIssues = issues
            .Where(i => i.Category == IssueCategory.Grammar)
            .ToList();

        foreach (var style in styleIssues)
        {
            foreach (var grammar in grammarIssues)
            {
                // LOGIC: Check if the fixes are within the proximity threshold.
                var distance = Math.Abs(style.Location.Start - grammar.Location.Start);

                if (distance < DependencyProximityThreshold)
                {
                    _logger.LogDebug(
                        "Dependent fixes: Style '{StyleSource}' at {StyleStart} and Grammar '{GrammarSource}' at {GrammarStart} " +
                        "are within {Distance} chars",
                        style.SourceId, style.Location.Start,
                        grammar.SourceId, grammar.Location.Start,
                        distance);

                    conflicts.Add(new FixConflictCase
                    {
                        Type = FixConflictType.DependentFixes,
                        ConflictingIssueIds = [style.IssueId, grammar.IssueId],
                        Description = $"Grammar fix '{grammar.SourceId}' may depend on " +
                                      $"Style fix '{style.SourceId}' being applied first " +
                                      $"(distance: {distance} chars)",
                        SuggestedResolution = "Apply Style fixes before Grammar fixes (handled by category ordering)",
                        Severity = FixConflictSeverity.Warning
                    });
                }
            }
        }

        return conflicts;
    }
}
