// -----------------------------------------------------------------------
// <copyright file="WorkflowExecutionRecords.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Context provided to a workflow execution.
/// </summary>
/// <remarks>
/// <para>
/// Encapsulates all context needed to execute a workflow, including the document
/// being processed, any text selection, initial variables for data passing, and
/// execution behavior options.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <param name="DocumentPath">
/// Optional path to the document being processed. Passed to agents via
/// <see cref="Lexichord.Abstractions.Agents.AgentRequest.DocumentPath"/>.
/// </param>
/// <param name="Selection">
/// Optional selected text from the document. Used as fallback content when no
/// previous step output is available.
/// </param>
/// <param name="InitialVariables">
/// Initial variables available to the first step. These are merged with
/// standard variables (documentPath, selection, _previousStepSuccess) during
/// initialization.
/// </param>
/// <param name="Options">
/// Options controlling execution behavior such as failure handling and timeouts.
/// </param>
public record WorkflowExecutionContext(
    string? DocumentPath,
    string? Selection,
    IReadOnlyDictionary<string, object> InitialVariables,
    WorkflowExecutionOptions Options
);

/// <summary>
/// Options controlling workflow execution behavior.
/// </summary>
/// <remarks>
/// <para>
/// Provides fine-grained control over how the workflow engine handles failures,
/// timeouts, and intermediate output collection. All options have sensible defaults.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <param name="StopOnFirstFailure">
/// When <c>true</c>, the engine stops executing after the first step failure and
/// marks remaining steps as <see cref="WorkflowStepStatus.Skipped"/>. Default: <c>true</c>.
/// </param>
/// <param name="CollectIntermediateOutputs">
/// When <c>true</c>, intermediate step outputs are stored in the final variables
/// dictionary. Default: <c>true</c>.
/// </param>
/// <param name="StepTimeout">
/// Maximum time allowed for a single step execution. When <c>null</c>, the engine
/// uses a default timeout of 120 seconds. Steps exceeding this timeout are marked
/// as <see cref="WorkflowStepStatus.Failed"/>.
/// </param>
/// <param name="MaxRetries">
/// Maximum number of retry attempts per failed step. Default: 0 (no retries).
/// </param>
/// <param name="DryRun">
/// When <c>true</c>, validates the workflow without executing any agents.
/// Default: <c>false</c>.
/// </param>
public record WorkflowExecutionOptions(
    bool StopOnFirstFailure = true,
    bool CollectIntermediateOutputs = true,
    TimeSpan? StepTimeout = null,
    int MaxRetries = 0,
    bool DryRun = false
);

/// <summary>
/// Result of a complete workflow execution.
/// </summary>
/// <remarks>
/// <para>
/// Contains all information about a completed (or partially completed) workflow
/// execution, including per-step results, aggregated usage metrics, final variable
/// state, and the overall execution status.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <param name="WorkflowId">The workflow definition ID that was executed.</param>
/// <param name="ExecutionId">Unique identifier for this execution instance (e.g., "exec-abc123").</param>
/// <param name="Success">
/// <c>true</c> if all executed steps succeeded (skipped steps are considered successful);
/// <c>false</c> if any step failed or the workflow was cancelled.
/// </param>
/// <param name="Status">Final execution status.</param>
/// <param name="StepResults">Per-step execution results in execution order.</param>
/// <param name="FinalOutput">
/// Content from the last successfully completed step. <c>null</c> if no steps completed.
/// </param>
/// <param name="TotalDuration">Wall-clock time for the entire execution.</param>
/// <param name="TotalUsage">Aggregated token usage across all executed steps.</param>
/// <param name="FinalVariables">
/// Final state of all variables after execution, including standard variables
/// and any output-mapped values.
/// </param>
/// <param name="ErrorMessage">
/// Error message if the workflow failed or was cancelled. <c>null</c> on success.
/// </param>
public record WorkflowExecutionResult(
    string WorkflowId,
    string ExecutionId,
    bool Success,
    WorkflowExecutionStatus Status,
    IReadOnlyList<WorkflowStepExecutionResult> StepResults,
    string? FinalOutput,
    TimeSpan TotalDuration,
    WorkflowUsageMetrics TotalUsage,
    IReadOnlyDictionary<string, object> FinalVariables,
    string? ErrorMessage
);

/// <summary>
/// Status of a workflow execution.
/// </summary>
/// <remarks>
/// Tracks the overall lifecycle state of a workflow execution from start to completion.
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
public enum WorkflowExecutionStatus
{
    /// <summary>Execution has been created but not yet started.</summary>
    Pending,

    /// <summary>Execution is currently in progress.</summary>
    Running,

    /// <summary>All steps completed successfully (including skipped steps).</summary>
    Completed,

    /// <summary>One or more steps failed and execution was stopped.</summary>
    Failed,

    /// <summary>Execution was cancelled by the user via cancellation token.</summary>
    Cancelled,

    /// <summary>
    /// Some steps completed and some failed, but execution was not stopped
    /// (StopOnFirstFailure = false).
    /// </summary>
    PartialSuccess
}

/// <summary>
/// Result of a single workflow step execution.
/// </summary>
/// <remarks>
/// <para>
/// Captures the outcome of invoking a specific agent within a workflow, including
/// the agent's response content, execution duration, token usage, and any output
/// variables extracted via output mappings.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <param name="StepId">The step's unique identifier within the workflow.</param>
/// <param name="AgentId">The ID of the agent that was invoked (or would have been invoked if skipped).</param>
/// <param name="Success">
/// <c>true</c> if the step completed without error or was skipped;
/// <c>false</c> if the step failed or timed out.
/// </param>
/// <param name="Status">Detailed status of this step's execution.</param>
/// <param name="Output">
/// Content returned by the agent. <c>null</c> for skipped or failed steps.
/// </param>
/// <param name="Duration">
/// Wall-clock time for this step. <see cref="TimeSpan.Zero"/> for skipped steps.
/// </param>
/// <param name="Usage">Token usage metrics for this step's agent invocation.</param>
/// <param name="ErrorMessage">
/// Error description if the step failed. <c>null</c> for successful or skipped steps.
/// </param>
/// <param name="OutputVariables">
/// Variables extracted from the agent response via output mappings. <c>null</c> if
/// no output mappings were defined or the step was skipped/failed.
/// </param>
public record WorkflowStepExecutionResult(
    string StepId,
    string AgentId,
    bool Success,
    WorkflowStepStatus Status,
    string? Output,
    TimeSpan Duration,
    AgentUsageMetrics Usage,
    string? ErrorMessage,
    IReadOnlyDictionary<string, object>? OutputVariables
);

/// <summary>
/// Status of a workflow step.
/// </summary>
/// <remarks>
/// Tracks the lifecycle state of an individual step within a workflow execution.
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
public enum WorkflowStepStatus
{
    /// <summary>Step has not yet been executed.</summary>
    Pending,

    /// <summary>Step is currently executing.</summary>
    Running,

    /// <summary>Step completed successfully.</summary>
    Completed,

    /// <summary>Step failed due to an error or timeout.</summary>
    Failed,

    /// <summary>Step was skipped due to a condition evaluation or a prior step failure.</summary>
    Skipped,

    /// <summary>Step was cancelled by the user.</summary>
    Cancelled
}

/// <summary>
/// Aggregated usage metrics for a workflow execution.
/// </summary>
/// <remarks>
/// <para>
/// Accumulates token usage and cost estimates across all executed steps in a workflow.
/// Provides step execution/skip counts for summary reporting.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <param name="TotalPromptTokens">Sum of prompt tokens across all executed steps.</param>
/// <param name="TotalCompletionTokens">Sum of completion tokens across all executed steps.</param>
/// <param name="TotalTokens">Sum of total tokens (prompt + completion) across all steps.</param>
/// <param name="EstimatedCost">Sum of estimated costs across all executed steps.</param>
/// <param name="StepsExecuted">Number of steps that were actually executed (not skipped).</param>
/// <param name="StepsSkipped">Number of steps that were skipped due to conditions or failures.</param>
public record WorkflowUsageMetrics(
    int TotalPromptTokens,
    int TotalCompletionTokens,
    int TotalTokens,
    decimal EstimatedCost,
    int StepsExecuted,
    int StepsSkipped
)
{
    /// <summary>
    /// Zero usage metrics for initialization.
    /// </summary>
    /// <value>
    /// A shared <see cref="WorkflowUsageMetrics"/> instance with all values set to zero.
    /// </value>
    /// <remarks>
    /// LOGIC: Used as the initial accumulator value in the execution loop.
    /// </remarks>
    public static WorkflowUsageMetrics Empty { get; } = new(0, 0, 0, 0m, 0, 0);

    /// <summary>
    /// Adds a step's usage metrics to the running total.
    /// </summary>
    /// <param name="stepUsage">The step's usage metrics to add.</param>
    /// <returns>A new <see cref="WorkflowUsageMetrics"/> with updated totals.</returns>
    /// <remarks>
    /// LOGIC: Increments StepsExecuted by 1 and adds the step's token counts and cost.
    /// </remarks>
    public WorkflowUsageMetrics Add(AgentUsageMetrics stepUsage) => new(
        TotalPromptTokens + stepUsage.PromptTokens,
        TotalCompletionTokens + stepUsage.CompletionTokens,
        TotalTokens + stepUsage.TotalTokens,
        EstimatedCost + stepUsage.EstimatedCost,
        StepsExecuted + 1,
        StepsSkipped);

    /// <summary>
    /// Increments the skipped step count.
    /// </summary>
    /// <returns>A new <see cref="WorkflowUsageMetrics"/> with StepsSkipped incremented by 1.</returns>
    /// <remarks>
    /// LOGIC: Called when a step is skipped due to condition evaluation.
    /// Token counts remain unchanged since no agent was invoked.
    /// </remarks>
    public WorkflowUsageMetrics WithSkipped() => this with { StepsSkipped = StepsSkipped + 1 };
}

/// <summary>
/// Usage metrics for a single agent invocation within a workflow step.
/// </summary>
/// <remarks>
/// <para>
/// Tracks token consumption for one agent invocation. These are aggregated into
/// <see cref="WorkflowUsageMetrics"/> at the workflow level.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <param name="PromptTokens">Number of tokens in the prompt sent to the agent.</param>
/// <param name="CompletionTokens">Number of tokens in the agent's response.</param>
/// <param name="TotalTokens">Total tokens consumed (prompt + completion).</param>
/// <param name="EstimatedCost">Estimated cost in USD for this invocation.</param>
public record AgentUsageMetrics(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal EstimatedCost
)
{
    /// <summary>
    /// Zero usage metrics for skipped or failed steps.
    /// </summary>
    /// <value>
    /// A shared <see cref="AgentUsageMetrics"/> instance with all values set to zero.
    /// </value>
    public static AgentUsageMetrics Empty { get; } = new(0, 0, 0, 0m);
}

/// <summary>
/// Validation result for workflow execution readiness.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IWorkflowEngine.ValidateExecution"/> to indicate whether
/// a workflow can be executed in the current context, along with any errors
/// (blocking) or warnings (informational).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <param name="CanExecute">
/// <c>true</c> if the workflow passes all validation checks; <c>false</c> if any
/// blocking errors were found.
/// </param>
/// <param name="Errors">List of blocking validation errors (e.g., unknown agents).</param>
/// <param name="Warnings">List of non-blocking warnings (e.g., unused variables).</param>
public record WorkflowExecutionValidation(
    bool CanExecute,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings
);

/// <summary>
/// Token usage estimate for a workflow.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IWorkflowEngine.EstimateTokens"/> to provide a
/// pre-execution cost estimate. Estimates are based on configured model pricing
/// and average token consumption for each agent type.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <param name="EstimatedPromptTokens">Total estimated prompt tokens across all steps.</param>
/// <param name="EstimatedCompletionTokens">Total estimated completion tokens across all steps.</param>
/// <param name="EstimatedTotalTokens">Total estimated tokens (prompt + completion).</param>
/// <param name="EstimatedCost">Total estimated cost in USD.</param>
/// <param name="StepEstimates">Per-step token estimates.</param>
public record WorkflowTokenEstimate(
    int EstimatedPromptTokens,
    int EstimatedCompletionTokens,
    int EstimatedTotalTokens,
    decimal EstimatedCost,
    IReadOnlyList<StepTokenEstimate> StepEstimates
);

/// <summary>
/// Token estimate for a single step.
/// </summary>
/// <remarks>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </remarks>
/// <param name="StepId">The step's unique identifier.</param>
/// <param name="AgentId">The agent that would be invoked.</param>
/// <param name="EstimatedPromptTokens">Estimated prompt tokens for this step.</param>
/// <param name="EstimatedCompletionTokens">Estimated completion tokens for this step.</param>
public record StepTokenEstimate(
    string StepId,
    string AgentId,
    int EstimatedPromptTokens,
    int EstimatedCompletionTokens
);
