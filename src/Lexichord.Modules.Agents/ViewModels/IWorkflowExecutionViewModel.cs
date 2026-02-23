// -----------------------------------------------------------------------
// <copyright file="IWorkflowExecutionViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Modules.Agents.Workflows;

namespace Lexichord.Modules.Agents.ViewModels;

/// <summary>
/// ViewModel interface for the workflow execution panel.
/// </summary>
/// <remarks>
/// <para>
/// Defines the contract for workflow execution state management, progress tracking,
/// user actions (cancel, apply, copy), and event handling. The implementation
/// (<see cref="WorkflowExecutionViewModel"/>) subscribes to MediatR workflow events
/// and updates observable properties to drive the execution UI.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7d as part of the Workflow Execution UI.
/// </para>
/// </remarks>
/// <seealso cref="WorkflowExecutionViewModel"/>
/// <seealso cref="WorkflowStepExecutionState"/>
public interface IWorkflowExecutionViewModel
{
    // ── Workflow State ───────────────────────────────────────────────────

    /// <summary>
    /// The workflow definition being executed.
    /// </summary>
    /// <value>
    /// The <see cref="WorkflowDefinition"/> set via <c>Initialize</c>,
    /// or <c>null</c> before initialization.
    /// </value>
    WorkflowDefinition? Workflow { get; }

    /// <summary>
    /// Current execution status (Pending, Running, Completed, Failed, Cancelled).
    /// </summary>
    WorkflowExecutionStatus Status { get; }

    /// <summary>
    /// Overall progress percentage (0–100).
    /// </summary>
    /// <remarks>
    /// Calculated per spec §5.1: ((completed + currentWeight) / total) * 100,
    /// where currentWeight is 0.5 for a running step.
    /// </remarks>
    int Progress { get; }

    /// <summary>
    /// Elapsed wall-clock time since execution started.
    /// </summary>
    /// <remarks>
    /// Updated every 100ms via a timer while execution is running.
    /// </remarks>
    TimeSpan ElapsedTime { get; }

    /// <summary>
    /// Estimated remaining time, or <c>null</c> if insufficient data.
    /// </summary>
    /// <remarks>
    /// Requires at least 2 completed steps to produce an estimate (spec §5.2).
    /// </remarks>
    TimeSpan? EstimatedRemaining { get; }

    /// <summary>
    /// The currently executing step, or <c>null</c> if no step is running.
    /// </summary>
    WorkflowStepExecutionState? CurrentStep { get; }

    /// <summary>
    /// Observable collection of all step states for UI binding.
    /// </summary>
    ObservableCollection<WorkflowStepExecutionState> StepStates { get; }

    // ── Control State ───────────────────────────────────────────────────

    /// <summary>
    /// Whether the cancel button should be enabled.
    /// </summary>
    /// <remarks>
    /// <c>true</c> while executing and before cancellation is requested.
    /// Set to <c>false</c> immediately on cancel click to prevent double-click.
    /// </remarks>
    bool CanCancel { get; }

    /// <summary>
    /// Whether the execution has finished (success, failure, or cancellation).
    /// </summary>
    bool IsComplete { get; }

    /// <summary>
    /// The final execution result, available after completion.
    /// </summary>
    WorkflowExecutionResult? Result { get; }

    /// <summary>
    /// Human-readable status message for display in the UI header.
    /// </summary>
    string StatusMessage { get; }

    /// <summary>
    /// Error message if execution failed, or <c>null</c>.
    /// </summary>
    string? ErrorMessage { get; }

    // ── Commands ─────────────────────────────────────────────────────────

    /// <summary>Starts workflow execution.</summary>
    IAsyncRelayCommand ExecuteCommand { get; }

    /// <summary>Cancels the running workflow.</summary>
    IRelayCommand CancelCommand { get; }

    /// <summary>Applies the final result to the active document selection.</summary>
    IAsyncRelayCommand ApplyResultCommand { get; }

    /// <summary>Copies the final result to the system clipboard.</summary>
    IAsyncRelayCommand CopyResultCommand { get; }

    /// <summary>Opens an export dialog for the result.</summary>
    IRelayCommand ExportResultCommand { get; }

    /// <summary>Closes the execution panel.</summary>
    IRelayCommand CloseCommand { get; }
}
