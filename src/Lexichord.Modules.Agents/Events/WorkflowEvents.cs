// -----------------------------------------------------------------------
// <copyright file="WorkflowEvents.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Workflows;
using MediatR;

namespace Lexichord.Modules.Agents.Events;

/// <summary>
/// Published when a workflow execution starts.
/// </summary>
/// <remarks>
/// <para>
/// This event is published by <see cref="WorkflowEngine"/> immediately before
/// the step execution loop begins. Subscribers can use this to initialize
/// progress tracking UI or logging context.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <param name="WorkflowId">The workflow definition ID being executed.</param>
/// <param name="ExecutionId">Unique identifier for this execution instance.</param>
/// <param name="TotalSteps">Total number of steps in the workflow.</param>
/// <seealso cref="WorkflowCompletedEvent"/>
/// <seealso cref="WorkflowCancelledEvent"/>
public record WorkflowStartedEvent(
    string WorkflowId,
    string ExecutionId,
    int TotalSteps
) : INotification;

/// <summary>
/// Published when a workflow step starts executing.
/// </summary>
/// <remarks>
/// <para>
/// This event is published by <see cref="WorkflowEngine"/> before each step
/// begins execution (including steps that are subsequently skipped due to
/// condition evaluation). Subscribers can use this to update the progress bar
/// and display the current step status.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <param name="WorkflowId">The workflow definition ID.</param>
/// <param name="ExecutionId">The execution instance ID.</param>
/// <param name="StepId">The step's unique identifier.</param>
/// <param name="AgentId">The agent that will be invoked for this step.</param>
/// <param name="StepNumber">1-based position of this step in the execution order.</param>
/// <param name="TotalSteps">Total number of steps in the workflow.</param>
/// <seealso cref="WorkflowStepCompletedEvent"/>
public record WorkflowStepStartedEvent(
    string WorkflowId,
    string ExecutionId,
    string StepId,
    string AgentId,
    int StepNumber,
    int TotalSteps
) : INotification;

/// <summary>
/// Published when a workflow step completes execution.
/// </summary>
/// <remarks>
/// <para>
/// This event is published by <see cref="WorkflowEngine"/> after each step finishes,
/// whether it completed successfully, failed, or was skipped. Subscribers can use
/// this to update step-level status indicators in the UI.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <param name="WorkflowId">The workflow definition ID.</param>
/// <param name="ExecutionId">The execution instance ID.</param>
/// <param name="StepId">The step's unique identifier.</param>
/// <param name="Success"><c>true</c> if the step succeeded or was skipped; <c>false</c> if it failed.</param>
/// <param name="Status">Detailed status of the step (<see cref="WorkflowStepStatus"/>).</param>
/// <param name="Duration">Wall-clock time for the step execution.</param>
/// <seealso cref="WorkflowStepStartedEvent"/>
public record WorkflowStepCompletedEvent(
    string WorkflowId,
    string ExecutionId,
    string StepId,
    bool Success,
    WorkflowStepStatus Status,
    TimeSpan Duration
) : INotification;

/// <summary>
/// Published when a workflow execution completes (successfully or with errors).
/// </summary>
/// <remarks>
/// <para>
/// This event is published by <see cref="WorkflowEngine"/> after the entire workflow
/// completes execution. It provides summary data about the execution outcome.
/// Subscribers can use this to display completion notifications and summary metrics.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <param name="WorkflowId">The workflow definition ID.</param>
/// <param name="ExecutionId">The execution instance ID.</param>
/// <param name="Success"><c>true</c> if all executed steps succeeded; <c>false</c> otherwise.</param>
/// <param name="Status">Final execution status (<see cref="WorkflowExecutionStatus"/>).</param>
/// <param name="TotalDuration">Wall-clock time for the entire workflow execution.</param>
/// <param name="StepsCompleted">Number of steps that completed successfully.</param>
/// <param name="StepsSkipped">Number of steps that were skipped.</param>
/// <seealso cref="WorkflowStartedEvent"/>
public record WorkflowCompletedEvent(
    string WorkflowId,
    string ExecutionId,
    bool Success,
    WorkflowExecutionStatus Status,
    TimeSpan TotalDuration,
    int StepsCompleted,
    int StepsSkipped
) : INotification;

/// <summary>
/// Published when a workflow is cancelled via cancellation token.
/// </summary>
/// <remarks>
/// <para>
/// This event is published by <see cref="WorkflowEngine"/> when the workflow
/// catches an <see cref="OperationCanceledException"/>. It reports which step
/// was being executed (or about to execute) when cancellation occurred.
/// </para>
/// <para>
/// Note: <see cref="WorkflowCompletedEvent"/> is NOT published when a workflow
/// is cancelled — only this event is published.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <param name="WorkflowId">The workflow definition ID.</param>
/// <param name="ExecutionId">The execution instance ID.</param>
/// <param name="CurrentStepId">
/// The step ID that was executing when cancellation occurred. <c>null</c> if
/// cancellation occurred between steps.
/// </param>
/// <param name="StepsCompleted">Number of steps that completed before cancellation.</param>
/// <seealso cref="WorkflowStartedEvent"/>
public record WorkflowCancelledEvent(
    string WorkflowId,
    string ExecutionId,
    string? CurrentStepId,
    int StepsCompleted
) : INotification;
