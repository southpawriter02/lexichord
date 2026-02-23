// -----------------------------------------------------------------------
// <copyright file="WorkflowExecutionSummary.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Summary of a past workflow execution for display in the history panel.
/// </summary>
/// <remarks>
/// <para>
/// Provides a lightweight, serializable snapshot of a workflow execution outcome.
/// These summaries are stored by <see cref="IWorkflowExecutionHistoryService"/> and
/// displayed in the execution history list. The <see cref="FinalOutputPreview"/>
/// field is truncated to 100 characters for compact display.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7d as part of the Workflow Execution UI.
/// </para>
/// </remarks>
/// <param name="ExecutionId">Unique identifier for this execution instance.</param>
/// <param name="WorkflowId">The workflow definition ID that was executed.</param>
/// <param name="WorkflowName">Human-readable workflow name for display.</param>
/// <param name="ExecutedAt">UTC timestamp when the execution started.</param>
/// <param name="Duration">Total wall-clock time for the execution.</param>
/// <param name="Status">Final execution status (Completed, Failed, Cancelled, etc.).</param>
/// <param name="StepsCompleted">Number of steps that completed successfully.</param>
/// <param name="TotalSteps">Total number of steps in the workflow.</param>
/// <param name="TotalTokens">Total tokens consumed across all executed steps.</param>
/// <param name="FinalOutputPreview">
/// First 100 characters of the final output, or <c>null</c> if no output was produced.
/// Used for compact display in the history list.
/// </param>
/// <seealso cref="IWorkflowExecutionHistoryService"/>
/// <seealso cref="WorkflowExecutionStatistics"/>
public record WorkflowExecutionSummary(
    string ExecutionId,
    string WorkflowId,
    string WorkflowName,
    DateTime ExecutedAt,
    TimeSpan Duration,
    WorkflowExecutionStatus Status,
    int StepsCompleted,
    int TotalSteps,
    int TotalTokens,
    string? FinalOutputPreview
);
