// -----------------------------------------------------------------------
// <copyright file="HealthScoreCalculator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Workflows.Validation.Metrics;

/// <summary>
/// Calculates workspace and document health scores from execution metrics.
/// </summary>
/// <remarks>
/// <para>
/// Implements the health score formulas from LCS-DES-077-KG-i §4.2:
/// <list type="bullet">
///   <item><description>
///     <b>Workspace:</b> PassRate × 80 + ErrorFreedom (0–20),
///     where ErrorFreedom = 20 if no errors, otherwise max(0, 20 − totalErrors/10)
///   </description></item>
///   <item><description>
///     <b>Document:</b> BaseScore (80 if pass, 30 if fail) − ErrorPenalty (5 per error, max 30)
///     − WarningPenalty (2 per warning, max 20)
///   </description></item>
/// </list>
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §4.2
/// </para>
/// </remarks>
internal sealed class HealthScoreCalculator : IHealthScoreCalculator
{
    // ── Fields ───────────────────────────────────────────────────────────

    private readonly ILogger<HealthScoreCalculator> _logger;

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of <see cref="HealthScoreCalculator"/>.
    /// </summary>
    /// <param name="logger">Logger for recording calculation diagnostics.</param>
    public HealthScoreCalculator(ILogger<HealthScoreCalculator> logger)
    {
        _logger = logger;
        _logger.LogDebug("HealthScoreCalculator initialized");
    }

    // ── IHealthScoreCalculator Implementation ───────────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// <b>Formula:</b> PassRate × 80 + ErrorFreedom (0–20).
    /// Returns a score of 0 with <see cref="HealthTrend.Stable"/> if no metrics exist.
    /// </remarks>
    public WorkspaceHealthScore CalculateWorkspaceHealth(
        IReadOnlyList<WorkflowExecutionMetrics> recentMetrics)
    {
        _logger.LogDebug(
            "CalculateWorkspaceHealth: processing {Count} metric records",
            recentMetrics.Count);

        // LOGIC: No metrics → return a zero-score placeholder.
        if (recentMetrics.Count == 0)
        {
            _logger.LogInformation("CalculateWorkspaceHealth: no metrics available, returning zero score");
            return new WorkspaceHealthScore
            {
                WorkspaceId = Guid.Empty,
                HealthScore = 0,
                PassRate = 0,
                CalculatedAt = DateTime.UtcNow,
                Trend = HealthTrend.Stable
            };
        }

        // LOGIC: Calculate the pass rate (fraction of successful executions).
        var passRate = (decimal)recentMetrics.Count(m => m.Success) / recentMetrics.Count;
        var totalErrors = recentMetrics.Sum(m => m.TotalErrors);
        var totalWarnings = recentMetrics.Sum(m => m.TotalWarnings);
        var docCount = recentMetrics.Select(m => m.DocumentId).Distinct().Count();

        // LOGIC: Health score formula from spec §4.2:
        // Base: PassRate contribution (0–80 points)
        // Bonus: ErrorFreedom (0–20 points), penalized by total errors
        var baseScore = (int)(passRate * 80);
        var errorFreedom = totalErrors == 0 ? 20 : Math.Max(0, 20 - (totalErrors / 10));
        var healthScore = Math.Max(0, Math.Min(100, baseScore + (int)errorFreedom));

        _logger.LogDebug(
            "CalculateWorkspaceHealth: passRate={PassRate:P1}, errors={Errors}, " +
            "baseScore={Base}, errorFreedom={Freedom}, healthScore={Score}",
            passRate, totalErrors, baseScore, errorFreedom, healthScore);

        return new WorkspaceHealthScore
        {
            WorkspaceId = recentMetrics[0].WorkspaceId,
            HealthScore = healthScore,
            PassRate = passRate,
            DocumentsWithErrors = recentMetrics.Count(m => m.TotalErrors > 0),
            AvgErrorsPerDocument = docCount > 0
                ? (decimal)totalErrors / docCount
                : 0,
            AvgWarningsPerDocument = docCount > 0
                ? (decimal)totalWarnings / docCount
                : 0,
            TopErrorTypes = [],  // LOGIC: Error type categorization deferred to future version
            DocumentsNeedingAttention = recentMetrics.Count(m => m.TotalErrors > 0 || m.TotalWarnings > 5),
            LastValidationRun = recentMetrics.Max(m => m.ExecutedAt),
            CalculatedAt = DateTime.UtcNow,
            Trend = HealthTrend.Stable,  // LOGIC: Trend calculation requires historical comparison (deferred)
            TrendValue = 0
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Formula:</b> BaseScore (80 if pass, 30 if fail) − ErrorPenalty (5/error, max 30)
    /// − WarningPenalty (2/warning, max 20). Returns a score of 0 with
    /// <see cref="DocumentValidationState.NotValidated"/> if no metrics exist.
    /// </remarks>
    public DocumentHealthScore CalculateDocumentHealth(
        Guid documentId,
        IReadOnlyList<WorkflowExecutionMetrics> documentMetrics)
    {
        _logger.LogDebug(
            "CalculateDocumentHealth: {DocumentId} with {Count} metric records",
            documentId, documentMetrics.Count);

        // LOGIC: No metrics → document has never been validated.
        if (documentMetrics.Count == 0)
        {
            _logger.LogInformation(
                "CalculateDocumentHealth: {DocumentId} has no metrics, returning zero score",
                documentId);
            return new DocumentHealthScore
            {
                DocumentId = documentId,
                HealthScore = 0,
                ValidationState = DocumentValidationState.NotValidated,
                CalculatedAt = DateTime.UtcNow
            };
        }

        // LOGIC: Use the most recent execution as the basis for the health score.
        var lastMetric = documentMetrics.OrderByDescending(m => m.ExecutedAt).First();

        // LOGIC: Document health formula from spec §4.2:
        // Base: 80 points if the last run passed, 30 if it failed
        // Penalties: 5 points per error (max 30), 2 points per warning (max 20)
        var baseScore = lastMetric.Success ? 80 : 30;
        var errorPenalty = Math.Min(30, lastMetric.TotalErrors * 5);
        var warningPenalty = Math.Min(20, lastMetric.TotalWarnings * 2);
        var healthScore = Math.Max(0, Math.Min(100, baseScore - errorPenalty - warningPenalty));

        // LOGIC: Determine validation state from the last run's results.
        var state = lastMetric.TotalErrors == 0
            ? (lastMetric.TotalWarnings == 0
                ? DocumentValidationState.Valid
                : DocumentValidationState.ValidWithWarnings)
            : DocumentValidationState.Invalid;

        // LOGIC: Generate actionable recommendations based on issues found.
        var recommendations = new List<HealthRecommendation>();
        if (lastMetric.TotalErrors > 0)
        {
            recommendations.Add(new HealthRecommendation
            {
                Title = "Fix Validation Errors",
                Description = $"{lastMetric.TotalErrors} validation errors must be resolved",
                Type = HealthRecommendationType.FixErrors,
                Priority = 1
            });
        }

        if (lastMetric.TotalWarnings > 0)
        {
            recommendations.Add(new HealthRecommendation
            {
                Title = "Review Warnings",
                Description = $"{lastMetric.TotalWarnings} warnings to review",
                Type = HealthRecommendationType.ResolveWarnings,
                Priority = 2
            });
        }

        _logger.LogDebug(
            "CalculateDocumentHealth: {DocumentId} baseScore={Base}, " +
            "errorPenalty={EPenalty}, warningPenalty={WPenalty}, healthScore={Score}, state={State}",
            documentId, baseScore, errorPenalty, warningPenalty, healthScore, state);

        return new DocumentHealthScore
        {
            DocumentId = documentId,
            HealthScore = healthScore,
            ValidationState = state,
            UnresolvedErrors = lastMetric.TotalErrors,
            Warnings = lastMetric.TotalWarnings,
            LastValidated = lastMetric.ExecutedAt,
            ReadyToPublish = lastMetric.Success && lastMetric.TotalErrors == 0,
            Recommendations = recommendations,
            CalculatedAt = DateTime.UtcNow
        };
    }
}
