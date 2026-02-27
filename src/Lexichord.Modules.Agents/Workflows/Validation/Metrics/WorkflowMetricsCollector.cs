// -----------------------------------------------------------------------
// <copyright file="WorkflowMetricsCollector.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Workflows.Validation.Metrics;

/// <summary>
/// Collects workflow execution metrics, calculates health scores, and generates reports.
/// </summary>
/// <remarks>
/// <para>
/// This is the primary metrics entry point. It delegates persistence to
/// <see cref="IMetricsStore"/> and health scoring to <see cref="IHealthScoreCalculator"/>.
/// </para>
/// <para>
/// <b>Recording behavior:</b> All <c>Record*</c> methods are fire-and-forget — they
/// catch and log exceptions without re-throwing. This ensures that metrics collection
/// never disrupts workflow execution.
/// </para>
/// <para>
/// <b>Query behavior:</b> <c>Get*</c> methods propagate exceptions to the caller,
/// as reporting failures should be surfaced to the requesting code.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §4.1
/// </para>
/// </remarks>
internal sealed class WorkflowMetricsCollector : IWorkflowMetricsCollector
{
    // ── Fields ───────────────────────────────────────────────────────────

    /// <summary>Backing store for metrics records.</summary>
    private readonly IMetricsStore _store;

    /// <summary>Health score calculation engine.</summary>
    private readonly IHealthScoreCalculator _healthCalculator;

    /// <summary>Logger for diagnostic output.</summary>
    private readonly ILogger<WorkflowMetricsCollector> _logger;

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of <see cref="WorkflowMetricsCollector"/>.
    /// </summary>
    /// <param name="store">Metrics persistence layer.</param>
    /// <param name="healthCalculator">Health score calculator.</param>
    /// <param name="logger">Logger for recording collector operations.</param>
    public WorkflowMetricsCollector(
        IMetricsStore store,
        IHealthScoreCalculator healthCalculator,
        ILogger<WorkflowMetricsCollector> logger)
    {
        _store = store;
        _healthCalculator = healthCalculator;
        _logger = logger;

        _logger.LogDebug("WorkflowMetricsCollector initialized");
    }

    // ── Recording Methods (Fire-and-Forget) ─────────────────────────────

    /// <inheritdoc />
    public async Task RecordWorkflowExecutionAsync(
        WorkflowExecutionMetrics metrics,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug(
                "RecordWorkflowExecutionAsync: recording {WorkflowId} ({Status})",
                metrics.WorkflowId, metrics.Status);

            await _store.StoreWorkflowExecutionAsync(metrics, ct);

            _logger.LogInformation(
                "Recorded workflow execution: {WorkflowId} {Status} " +
                "(steps={StepsExecuted}, errors={TotalErrors}, warnings={TotalWarnings}, " +
                "duration={Duration}ms)",
                metrics.WorkflowId, metrics.Status,
                metrics.StepsExecuted, metrics.TotalErrors, metrics.TotalWarnings,
                metrics.TotalExecutionTimeMs);

            // LOGIC: Trigger an asynchronous health score recalculation.
            // This runs in the background to avoid blocking the caller.
            _ = Task.Run(
                async () => await RecalculateHealthAsync(
                    metrics.WorkspaceId,
                    metrics.DocumentId,
                    ct),
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to record workflow execution metrics");
        }
    }

    /// <inheritdoc />
    public async Task RecordValidationStepAsync(
        ValidationStepMetrics metrics,
        CancellationToken ct = default)
    {
        try
        {
            await _store.StoreValidationStepAsync(metrics, ct);

            _logger.LogDebug(
                "Recorded validation step: {StepId} ({StepType}) — " +
                "passed={Passed}, errors={Errors}, warnings={Warnings}, duration={Duration}ms",
                metrics.StepId, metrics.StepType,
                metrics.Passed, metrics.ErrorCount, metrics.WarningCount,
                metrics.ExecutionTimeMs);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to record validation step metrics");
        }
    }

    /// <inheritdoc />
    public async Task RecordGatingDecisionAsync(
        GatingDecisionMetrics metrics,
        CancellationToken ct = default)
    {
        try
        {
            await _store.StoreGatingDecisionAsync(metrics, ct);

            _logger.LogDebug(
                "Recorded gating decision: {GateId} ({Decision}) — " +
                "expression={Expression}, evalTime={EvalTime}ms",
                metrics.GateId, metrics.Passed ? "PASS" : "BLOCK",
                metrics.ConditionExpression, metrics.EvaluationTimeMs);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to record gating decision metrics");
        }
    }

    /// <inheritdoc />
    public async Task RecordSyncOperationAsync(
        SyncOperationMetrics metrics,
        CancellationToken ct = default)
    {
        try
        {
            await _store.StoreSyncOperationAsync(metrics, ct);

            _logger.LogDebug(
                "Recorded sync operation: {Direction} {Status} — " +
                "items={Items}, conflicts={Detected}/{Resolved}, duration={Duration}ms",
                metrics.Direction, metrics.Success ? "SUCCESS" : "FAILED",
                metrics.ItemsSynced, metrics.ConflictsDetected, metrics.ConflictsResolved,
                metrics.ExecutionTimeMs);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to record sync operation metrics");
        }
    }

    // ── Query Methods (Propagate Exceptions) ────────────────────────────

    /// <inheritdoc />
    public async Task<MetricsReport> GetMetricsAsync(
        MetricsQuery query,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug(
                "GetMetricsAsync: workspace={WorkspaceId}, range={Start}..{End}, aggregation={Agg}",
                query.WorkspaceId, query.StartTime, query.EndTime, query.Aggregation);

            // LOGIC: Fetch all four metric types from the store.
            var executions = await _store.GetWorkflowExecutionsAsync(query, ct);
            var steps = await _store.GetValidationStepsAsync(query, ct);
            var gatingDecisions = await _store.GetGatingDecisionsAsync(query, ct);
            var syncOps = await _store.GetSyncOperationsAsync(query, ct);

            // LOGIC: Calculate aggregated statistics from execution records.
            var statistics = CalculateStatistics(executions);

            var report = new MetricsReport
            {
                Query = query,
                WorkflowExecutions = executions,
                ValidationSteps = steps,
                GatingDecisions = gatingDecisions,
                SyncOperations = syncOps,
                Statistics = statistics,
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Generated metrics report: {Executions} executions, {Steps} steps, " +
                "{Gating} gating decisions, {Sync} sync operations",
                executions.Count, steps.Count, gatingDecisions.Count, syncOps.Count);

            return report;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to generate metrics report");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<WorkspaceHealthScore> GetHealthScoreAsync(
        Guid workspaceId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug(
                "GetHealthScoreAsync: calculating for workspace {WorkspaceId}",
                workspaceId);

            // LOGIC: Use the last 7 days of metrics for health calculation.
            var recentMetrics = await _store.GetWorkflowExecutionsAsync(
                new MetricsQuery
                {
                    WorkspaceId = workspaceId,
                    StartTime = DateTime.UtcNow.AddDays(-7),
                    EndTime = DateTime.UtcNow
                },
                ct);

            var health = _healthCalculator.CalculateWorkspaceHealth(recentMetrics);

            _logger.LogInformation(
                "Calculated workspace health: {WorkspaceId} = {Score}/100 " +
                "(passRate={PassRate:P1}, documentsWithErrors={DocsWithErrors})",
                workspaceId, health.HealthScore, health.PassRate, health.DocumentsWithErrors);

            return health;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to calculate workspace health for {WorkspaceId}", workspaceId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentHealthScore> GetDocumentHealthAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug(
                "GetDocumentHealthAsync: calculating for document {DocumentId}",
                documentId);

            var recentMetrics = await _store.GetDocumentMetricsAsync(documentId, ct);

            var health = _healthCalculator.CalculateDocumentHealth(documentId, recentMetrics);

            _logger.LogInformation(
                "Calculated document health: {DocumentId} = {Score}/100 " +
                "(state={State}, errors={Errors}, warnings={Warnings})",
                documentId, health.HealthScore, health.ValidationState,
                health.UnresolvedErrors, health.Warnings);

            return health;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to calculate document health for {DocumentId}", documentId);
            throw;
        }
    }

    // ── Private Methods ─────────────────────────────────────────────────

    /// <summary>
    /// Triggers background health score recalculation after a new execution is recorded.
    /// </summary>
    /// <param name="workspaceId">Workspace to recalculate.</param>
    /// <param name="documentId">Document to recalculate.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task RecalculateHealthAsync(
        Guid workspaceId,
        Guid documentId,
        CancellationToken ct)
    {
        try
        {
            _logger.LogDebug(
                "RecalculateHealthAsync: recalculating scores for workspace={WorkspaceId}, document={DocumentId}",
                workspaceId, documentId);

            var workspaceHealth = await GetHealthScoreAsync(workspaceId, ct);
            var docHealth = await GetDocumentHealthAsync(documentId, ct);

            // LOGIC: Store the recalculated scores for caching.
            await _store.StoreHealthScoresAsync(workspaceHealth, docHealth, ct);

            _logger.LogDebug(
                "RecalculateHealthAsync: completed — workspace={WScore}, document={DScore}",
                workspaceHealth.HealthScore, docHealth.HealthScore);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // LOGIC: Background recalculation failures are logged but not propagated.
            _logger.LogError(
                ex,
                "Failed to recalculate health scores for workspace={WorkspaceId}, document={DocumentId}",
                workspaceId, documentId);
        }
    }

    /// <summary>
    /// Calculates aggregated statistics from execution metrics.
    /// </summary>
    /// <param name="executions">The execution records to aggregate.</param>
    /// <returns>Computed statistics.</returns>
    private MetricsStatistics CalculateStatistics(
        IReadOnlyList<WorkflowExecutionMetrics> executions)
    {
        if (executions.Count == 0)
        {
            return new MetricsStatistics();
        }

        var successful = executions.Count(e => e.Success);
        var failed = executions.Count(e => !e.Success);
        var totalExecutions = executions.Count;

        return new MetricsStatistics
        {
            TotalExecutions = totalExecutions,
            SuccessfulExecutions = successful,
            FailedExecutions = failed,
            SuccessRate = totalExecutions > 0
                ? (decimal)successful / totalExecutions * 100
                : 0,
            AvgExecutionTimeMs = executions.Average(e => e.TotalExecutionTimeMs),
            TotalErrors = executions.Sum(e => e.TotalErrors),
            TotalWarnings = executions.Sum(e => e.TotalWarnings),
            AvgErrorsPerExecution = totalExecutions > 0
                ? (decimal)executions.Sum(e => e.TotalErrors) / totalExecutions
                : 0,
            DocumentsWithErrors = executions
                .Where(e => e.TotalErrors > 0)
                .Select(e => e.DocumentId)
                .Distinct()
                .Count(),
            DocumentsPassedValidation = executions
                .Where(e => e.Success)
                .Select(e => e.DocumentId)
                .Distinct()
                .Count()
        };
    }
}
