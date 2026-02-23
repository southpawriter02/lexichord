// -----------------------------------------------------------------------
// <copyright file="IWorkflowExecutionHistoryService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Service for tracking and querying workflow execution history.
/// </summary>
/// <remarks>
/// <para>
/// Provides persistence and retrieval of <see cref="WorkflowExecutionSummary"/> records,
/// enabling the execution history panel and statistics views. The service maintains a
/// bounded history (maximum 100 entries) and supports filtering by workflow ID.
/// </para>
/// <para>
/// <b>License gating:</b>
/// <list type="bullet">
///   <item><description>History access requires <see cref="Lexichord.Abstractions.Contracts.LicenseTier.Teams"/> or above.</description></item>
///   <item><description>Statistics access requires <see cref="Lexichord.Abstractions.Contracts.LicenseTier.Enterprise"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7d as part of the Workflow Execution UI.
/// </para>
/// </remarks>
/// <seealso cref="WorkflowExecutionSummary"/>
/// <seealso cref="WorkflowExecutionStatistics"/>
/// <seealso cref="WorkflowExecutionHistoryService"/>
public interface IWorkflowExecutionHistoryService
{
    /// <summary>
    /// Records a workflow execution result in the history store.
    /// </summary>
    /// <remarks>
    /// LOGIC: Creates a <see cref="WorkflowExecutionSummary"/> from the result and inserts
    /// it at the head of the history list. If the list exceeds 100 entries, the oldest
    /// entries are trimmed.
    /// </remarks>
    /// <param name="result">The execution result to record.</param>
    /// <param name="workflowName">Human-readable workflow name for display.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordAsync(
        WorkflowExecutionResult result,
        string workflowName,
        CancellationToken ct = default);

    /// <summary>
    /// Gets recent execution history, optionally filtered by workflow ID.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns history entries sorted by <see cref="WorkflowExecutionSummary.ExecutedAt"/>
    /// descending (most recent first). When <paramref name="workflowId"/> is specified,
    /// only entries for that workflow are returned.
    /// </remarks>
    /// <param name="workflowId">
    /// Optional workflow ID filter. When <c>null</c>, returns history for all workflows.
    /// </param>
    /// <param name="limit">Maximum number of entries to return. Default: 10.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>List of execution summaries, most recent first.</returns>
    Task<IReadOnlyList<WorkflowExecutionSummary>> GetHistoryAsync(
        string? workflowId = null,
        int limit = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Computes aggregated execution statistics for a specific workflow.
    /// </summary>
    /// <remarks>
    /// LOGIC: Aggregates all history entries matching <paramref name="workflowId"/> to
    /// compute success rates, duration statistics, and token consumption metrics.
    /// Returns zero-valued statistics when no entries exist.
    /// </remarks>
    /// <param name="workflowId">The workflow ID to compute statistics for.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>Aggregated statistics for the specified workflow.</returns>
    Task<WorkflowExecutionStatistics> GetStatisticsAsync(
        string workflowId,
        CancellationToken ct = default);

    /// <summary>
    /// Clears execution history older than the specified date.
    /// </summary>
    /// <remarks>
    /// LOGIC: Removes all <see cref="WorkflowExecutionSummary"/> entries where
    /// <see cref="WorkflowExecutionSummary.ExecutedAt"/> is before <paramref name="olderThan"/>.
    /// </remarks>
    /// <param name="olderThan">Cutoff date; entries before this date are removed.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearHistoryAsync(DateTime olderThan, CancellationToken ct = default);
}
