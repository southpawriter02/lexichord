// -----------------------------------------------------------------------
// <copyright file="WorkflowStepExecutionState.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Modules.Agents.Workflows;

namespace Lexichord.Modules.Agents.ViewModels;

/// <summary>
/// Observable state of a single workflow step for UI display during execution.
/// </summary>
/// <remarks>
/// <para>
/// Each instance represents one step in the execution pipeline and is bound to a
/// step card in the execution panel. Properties are updated in real-time as the
/// <see cref="WorkflowExecutionViewModel"/> receives MediatR events from the
/// <see cref="Lexichord.Modules.Agents.Workflows.WorkflowEngine"/>.
/// </para>
/// <para>
/// <b>UI Binding Notes:</b>
/// <list type="bullet">
///   <item><description>
///     <see cref="Status"/> drives the step card's icon and background color
///     (clock/gray for Pending, spinner/blue for Running, checkmark/green for Completed,
///     X/red for Failed, skip/yellow for Skipped, stop/orange for Cancelled).
///   </description></item>
///   <item><description>
///     <see cref="OutputPreview"/> is always ≤200 characters for compact display;
///     <see cref="IsExpanded"/> toggles full output visibility.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7d as part of the Workflow Execution UI (spec §4.1).
/// </para>
/// </remarks>
/// <seealso cref="IWorkflowExecutionViewModel"/>
/// <seealso cref="WorkflowExecutionViewModel"/>
public partial class WorkflowStepExecutionState : ObservableObject
{
    // ── Identity Properties (init-only) ─────────────────────────────────

    /// <summary>
    /// Unique step identifier matching <see cref="WorkflowStepDefinition.StepId"/>.
    /// </summary>
    public required string StepId { get; init; }

    /// <summary>
    /// 1-based step number for display (e.g., "Step 1 of 4").
    /// </summary>
    public required int StepNumber { get; init; }

    /// <summary>
    /// Human-readable agent name for the step card header (e.g., "Editor", "Simplifier").
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    /// Icon identifier for the agent (e.g., "edit-3", "zap", "sliders", "file-text").
    /// </summary>
    public required string AgentIcon { get; init; }

    /// <summary>
    /// Optional persona name if a persona is applied to this step's agent.
    /// </summary>
    public string? PersonaName { get; init; }

    // ── Mutable State Properties ────────────────────────────────────────

    /// <summary>
    /// Current step status (Pending, Running, Completed, Failed, Skipped, Cancelled).
    /// </summary>
    /// <remarks>
    /// Updated by <see cref="WorkflowExecutionViewModel"/> when step events are received.
    /// Drives the step card's visual appearance.
    /// </remarks>
    [ObservableProperty]
    private WorkflowStepStatus _status = WorkflowStepStatus.Pending;

    /// <summary>
    /// Human-readable status message displayed below the step header.
    /// </summary>
    /// <remarks>
    /// Examples: "Waiting...", "Processing...", "Completed (18.2s)", "Failed", "Skipped (condition not met)".
    /// </remarks>
    [ObservableProperty]
    private string _statusMessage = "Waiting...";

    /// <summary>
    /// Progress within the step (0–100), or <c>null</c> if not available.
    /// </summary>
    /// <remarks>
    /// Currently unused — reserved for future streaming progress updates.
    /// </remarks>
    [ObservableProperty]
    private int? _progressPercent;

    /// <summary>
    /// Step duration after completion.
    /// </summary>
    /// <remarks>
    /// Set when the step completes. Used for both display ("18.2s") and estimated
    /// remaining time calculation.
    /// </remarks>
    [ObservableProperty]
    private TimeSpan? _duration;

    /// <summary>
    /// Full output text from the agent for this step.
    /// </summary>
    [ObservableProperty]
    private string? _output;

    /// <summary>
    /// Error message if the step failed.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Token usage metrics for this step's agent invocation.
    /// </summary>
    [ObservableProperty]
    private AgentUsageMetrics? _usage;

    /// <summary>
    /// Whether the output is expanded in the UI (user toggled "Show more").
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    // ── Computed Properties ─────────────────────────────────────────────

    /// <summary>
    /// Output preview truncated to 200 characters for compact display.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #12: Output > 200 chars → "Show more" available.
    /// </remarks>
    public string? OutputPreview => Output?.Length > 200
        ? Output[..200] + "..."
        : Output;

    /// <summary>
    /// Whether this step has output text to display.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #11: Step has output → Output preview shown.
    /// </remarks>
    public bool HasOutput => !string.IsNullOrEmpty(Output);
}
