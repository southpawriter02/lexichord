// -----------------------------------------------------------------------
// <copyright file="IWorkflowMetricsCollector.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation.Metrics;

/// <summary>
/// Collects, stores, and retrieves workflow execution metrics and health scores.
/// </summary>
/// <remarks>
/// <para>
/// The metrics collector is the primary entry point for recording telemetry
/// from workflow executions. It delegates storage to <see cref="IMetricsStore"/>
/// and health calculation to <see cref="IHealthScoreCalculator"/>.
/// </para>
/// <para>
/// <b>Recording methods</b> are fire-and-forget — they swallow exceptions to
/// avoid disrupting workflow execution. <b>Query methods</b> propagate exceptions
/// to the caller.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §2.1
/// </para>
/// </remarks>
public interface IWorkflowMetricsCollector
{
    /// <summary>
    /// Records metrics from a completed workflow execution.
    /// </summary>
    /// <param name="metrics">The execution metrics to record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// Triggers an asynchronous health score recalculation for the
    /// associated workspace and document after storing the metrics.
    /// </remarks>
    Task RecordWorkflowExecutionAsync(
        WorkflowExecutionMetrics metrics,
        CancellationToken ct = default);

    /// <summary>
    /// Records metrics from a validation step execution.
    /// </summary>
    /// <param name="metrics">The validation step metrics to record.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordValidationStepAsync(
        ValidationStepMetrics metrics,
        CancellationToken ct = default);

    /// <summary>
    /// Records a gating step decision (pass or block).
    /// </summary>
    /// <param name="metrics">The gating decision metrics to record.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordGatingDecisionAsync(
        GatingDecisionMetrics metrics,
        CancellationToken ct = default);

    /// <summary>
    /// Records metrics from a sync operation.
    /// </summary>
    /// <param name="metrics">The sync operation metrics to record.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordSyncOperationAsync(
        SyncOperationMetrics metrics,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a metrics report for the specified query parameters.
    /// </summary>
    /// <param name="query">Query filters and aggregation settings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A report containing matching metrics and aggregated statistics.</returns>
    Task<MetricsReport> GetMetricsAsync(
        MetricsQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Calculates the current health score for a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace to score.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The computed workspace health score.</returns>
    Task<WorkspaceHealthScore> GetHealthScoreAsync(
        Guid workspaceId,
        CancellationToken ct = default);

    /// <summary>
    /// Calculates the current health score for a document.
    /// </summary>
    /// <param name="documentId">The document to score.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The computed document health score.</returns>
    Task<DocumentHealthScore> GetDocumentHealthAsync(
        Guid documentId,
        CancellationToken ct = default);
}
