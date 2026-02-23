// -----------------------------------------------------------------------
// <copyright file="WorkflowExecutionStatistics.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Aggregated statistics for workflow executions, computed from execution history.
/// </summary>
/// <remarks>
/// <para>
/// Provides comprehensive metrics about workflow usage patterns, including success rates,
/// duration statistics, and token consumption. Computed on-demand from
/// <see cref="WorkflowExecutionSummary"/> records by
/// <see cref="IWorkflowExecutionHistoryService.GetStatisticsAsync"/>.
/// </para>
/// <para>
/// When no executions exist for a workflow, all numeric fields are zero and date fields
/// are <c>null</c>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7d as part of the Workflow Execution UI.
/// </para>
/// <para>
/// <b>License gating:</b> Statistics are available to Enterprise-tier users only.
/// </para>
/// </remarks>
/// <param name="TotalExecutions">Total number of recorded executions.</param>
/// <param name="SuccessfulExecutions">Number of executions with <see cref="WorkflowExecutionStatus.Completed"/>.</param>
/// <param name="FailedExecutions">Number of executions with <see cref="WorkflowExecutionStatus.Failed"/>.</param>
/// <param name="CancelledExecutions">Number of executions with <see cref="WorkflowExecutionStatus.Cancelled"/>.</param>
/// <param name="SuccessRate">
/// Ratio of successful executions to total (0.0–1.0). Zero when no executions exist.
/// </param>
/// <param name="AverageDuration">Mean execution duration across all recorded executions.</param>
/// <param name="MinDuration">Shortest execution duration on record.</param>
/// <param name="MaxDuration">Longest execution duration on record.</param>
/// <param name="AverageTokensPerExecution">Mean token consumption per execution.</param>
/// <param name="TotalTokensUsed">Sum of all tokens consumed across all executions.</param>
/// <param name="FirstExecution">UTC timestamp of the earliest recorded execution, or <c>null</c>.</param>
/// <param name="LastExecution">UTC timestamp of the most recent recorded execution, or <c>null</c>.</param>
/// <seealso cref="IWorkflowExecutionHistoryService"/>
/// <seealso cref="WorkflowExecutionSummary"/>
public record WorkflowExecutionStatistics(
    int TotalExecutions,
    int SuccessfulExecutions,
    int FailedExecutions,
    int CancelledExecutions,
    double SuccessRate,
    TimeSpan AverageDuration,
    TimeSpan MinDuration,
    TimeSpan MaxDuration,
    int AverageTokensPerExecution,
    int TotalTokensUsed,
    DateTime? FirstExecution,
    DateTime? LastExecution
);
