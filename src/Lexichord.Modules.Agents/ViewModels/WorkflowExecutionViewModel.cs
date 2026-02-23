// -----------------------------------------------------------------------
// <copyright file="WorkflowExecutionViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Events;
using Lexichord.Modules.Agents.Workflows;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.ViewModels;

/// <summary>
/// ViewModel for the workflow execution panel, managing execution state,
/// progress tracking, event handling, and user actions.
/// </summary>
/// <remarks>
/// <para>
/// Orchestrates the full execution lifecycle from initialization through completion:
/// </para>
/// <list type="number">
///   <item><description>
///     <b>Initialize</b> — Sets the workflow definition and creates step states.
///   </description></item>
///   <item><description>
///     <b>Execute</b> — Starts the workflow engine with a cancellation token,
///     starts the elapsed timer, and records the result to history on completion.
///   </description></item>
///   <item><description>
///     <b>Event Handling</b> — Receives <see cref="WorkflowStepStartedEvent"/>,
///     <see cref="WorkflowStepCompletedEvent"/>, <see cref="WorkflowCompletedEvent"/>,
///     and <see cref="WorkflowCancelledEvent"/> to update step states and progress.
///   </description></item>
///   <item><description>
///     <b>Result Actions</b> — After completion, enables Apply (replace selection),
///     Copy (clipboard), and Export (file dialog) commands.
///   </description></item>
/// </list>
/// <para>
/// <b>Progress Calculation (spec §5.1):</b>
/// <c>progress = ((completed + currentWeight) / total) * 100</c>, where
/// <c>currentWeight</c> is 0.5 for a running step, 0.0 otherwise. Rounded to nearest integer.
/// </para>
/// <para>
/// <b>Estimated Time (spec §5.2):</b> Requires ≥2 completed steps. Computes average step
/// duration and multiplies by remaining step count.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7d as part of the Workflow Execution UI.
/// </para>
/// </remarks>
/// <seealso cref="IWorkflowExecutionViewModel"/>
/// <seealso cref="WorkflowStepExecutionState"/>
/// <seealso cref="IWorkflowExecutionHistoryService"/>
public partial class WorkflowExecutionViewModel : ObservableObject, IWorkflowExecutionViewModel
{
    // ── Dependencies ────────────────────────────────────────────────────

    /// <summary>Workflow execution engine for running workflows.</summary>
    private readonly IWorkflowEngine _engine;

    /// <summary>Editor insertion service for applying results to document selection.</summary>
    private readonly IEditorInsertionService _editorInsertionService;

    /// <summary>History service for recording execution outcomes.</summary>
    private readonly IWorkflowExecutionHistoryService _historyService;

    /// <summary>Clipboard service for copying results.</summary>
    private readonly IClipboardService _clipboardService;

    /// <summary>Logger for diagnostic output.</summary>
    private readonly ILogger<WorkflowExecutionViewModel> _logger;

    // ── Internal State ──────────────────────────────────────────────────

    /// <summary>Cancellation token source for the current execution.</summary>
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>Stopwatch for tracking elapsed time.</summary>
    private readonly Stopwatch _stopwatch = new();

    /// <summary>Timer for updating the <see cref="ElapsedTime"/> property every 100ms.</summary>
    private readonly Timer _elapsedTimer;

    /// <summary>Execution context created during <see cref="Initialize"/>.</summary>
    private WorkflowExecutionContext? _context;

    // ── Observable Properties ───────────────────────────────────────────

    /// <inheritdoc />
    [ObservableProperty]
    private WorkflowDefinition? _workflow;

    /// <inheritdoc />
    [ObservableProperty]
    private WorkflowExecutionStatus _status = WorkflowExecutionStatus.Pending;

    /// <inheritdoc />
    [ObservableProperty]
    private int _progress;

    /// <inheritdoc />
    [ObservableProperty]
    private TimeSpan _elapsedTime;

    /// <inheritdoc />
    [ObservableProperty]
    private TimeSpan? _estimatedRemaining;

    /// <inheritdoc />
    [ObservableProperty]
    private WorkflowStepExecutionState? _currentStep;

    /// <inheritdoc />
    [ObservableProperty]
    private bool _canCancel;

    /// <inheritdoc />
    [ObservableProperty]
    private bool _isComplete;

    /// <inheritdoc />
    [ObservableProperty]
    private WorkflowExecutionResult? _result;

    /// <inheritdoc />
    [ObservableProperty]
    private string _statusMessage = "Ready to execute";

    /// <inheritdoc />
    [ObservableProperty]
    private string? _errorMessage;

    /// <inheritdoc />
    public ObservableCollection<WorkflowStepExecutionState> StepStates { get; } = new();

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowExecutionViewModel"/> class.
    /// </summary>
    /// <param name="engine">Workflow execution engine (v0.7.7b).</param>
    /// <param name="editorInsertionService">Editor service for applying results to document.</param>
    /// <param name="historyService">History service for recording executions.</param>
    /// <param name="clipboardService">Clipboard service for copying results.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public WorkflowExecutionViewModel(
        IWorkflowEngine engine,
        IEditorInsertionService editorInsertionService,
        IWorkflowExecutionHistoryService historyService,
        IClipboardService clipboardService,
        ILogger<WorkflowExecutionViewModel> logger)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _editorInsertionService = editorInsertionService ?? throw new ArgumentNullException(nameof(editorInsertionService));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Timer for elapsed time display — starts disabled, enabled during execution.
        _elapsedTimer = new Timer(UpdateElapsedTime, null, Timeout.Infinite, Timeout.Infinite);

        _logger.LogDebug("WorkflowExecutionViewModel constructed (v0.7.7d)");
    }

    // ── Initialization ──────────────────────────────────────────────────

    /// <summary>
    /// Initializes the ViewModel for a new workflow execution.
    /// </summary>
    /// <remarks>
    /// LOGIC:
    /// <list type="number">
    ///   <item><description>Sets the workflow definition and resets all state properties.</description></item>
    ///   <item><description>Creates <see cref="WorkflowStepExecutionState"/> entries for each step.</description></item>
    ///   <item><description>Builds the <see cref="WorkflowExecutionContext"/> for the engine.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="workflow">The workflow definition to execute.</param>
    /// <param name="documentPath">Optional path to the document being processed.</param>
    /// <param name="selection">Optional selected text from the document.</param>
    public void Initialize(WorkflowDefinition workflow, string? documentPath, string? selection)
    {
        // STEP 1: Set workflow and reset state
        Workflow = workflow;
        Status = WorkflowExecutionStatus.Pending;
        Progress = 0;
        IsComplete = false;
        CanCancel = false;
        Result = null;
        ErrorMessage = null;
        ElapsedTime = TimeSpan.Zero;
        EstimatedRemaining = null;
        CurrentStep = null;
        StatusMessage = $"Ready to run: {workflow.Name}";

        // STEP 2: Create step states
        InitializeStepStates(workflow);

        // STEP 3: Build execution context
        _context = new WorkflowExecutionContext(
            documentPath,
            selection,
            new Dictionary<string, object>(),
            new WorkflowExecutionOptions());

        _logger.LogDebug(
            "Workflow execution initialized: {WorkflowId} ({StepCount} steps)",
            workflow.WorkflowId, workflow.Steps.Count);
    }

    /// <summary>
    /// Creates <see cref="WorkflowStepExecutionState"/> entries for each step in the workflow.
    /// </summary>
    /// <param name="workflow">The workflow definition containing the steps.</param>
    private void InitializeStepStates(WorkflowDefinition workflow)
    {
        StepStates.Clear();

        var stepNumber = 0;
        foreach (var step in workflow.Steps.OrderBy(s => s.Order))
        {
            stepNumber++;
            StepStates.Add(new WorkflowStepExecutionState
            {
                StepId = step.StepId,
                StepNumber = stepNumber,
                AgentName = GetAgentName(step.AgentId),
                AgentIcon = GetAgentIcon(step.AgentId),
                PersonaName = step.PersonaId,
                Status = WorkflowStepStatus.Pending,
                StatusMessage = "Waiting..."
            });
        }
    }

    // ── Commands ─────────────────────────────────────────────────────────

    /// <summary>
    /// Starts the workflow execution.
    /// </summary>
    /// <remarks>
    /// LOGIC (spec §5.3 — Event Handling Flow):
    /// <list type="number">
    ///   <item><description>Creates a <see cref="CancellationTokenSource"/> for cancel support.</description></item>
    ///   <item><description>Starts the stopwatch and elapsed timer (100ms intervals).</description></item>
    ///   <item><description>Invokes <see cref="IWorkflowEngine.ExecuteAsync"/>.</description></item>
    ///   <item><description>Records the result to <see cref="IWorkflowExecutionHistoryService"/>.</description></item>
    ///   <item><description>On exception: sets <see cref="ErrorMessage"/> and <see cref="Status"/> to Failed.</description></item>
    ///   <item><description>Finally: stops timer, disables cancel, sets <see cref="IsComplete"/>.</description></item>
    /// </list>
    /// </remarks>
    [RelayCommand]
    private async Task ExecuteAsync()
    {
        if (Workflow is null || _context is null) return;

        _cancellationTokenSource = new CancellationTokenSource();
        CanCancel = true;
        Status = WorkflowExecutionStatus.Running;
        StatusMessage = "Executing workflow...";

        _logger.LogDebug("Workflow execution started: {WorkflowId}", Workflow.WorkflowId);

        // Start timing
        _stopwatch.Restart();
        _elapsedTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(100));

        try
        {
            // Execute the workflow via the engine
            Result = await _engine.ExecuteAsync(Workflow, _context, _cancellationTokenSource.Token);

            // Record to history (Acceptance Criteria #22)
            await _historyService.RecordAsync(Result, Workflow.Name);

            _logger.LogInformation(
                "Workflow execution completed: {Status} in {Duration}",
                Result.Status, Result.TotalDuration);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Workflow cancelled by user");
            Status = WorkflowExecutionStatus.Cancelled;
            StatusMessage = "Workflow cancelled by user";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow execution failed: {Error}", ex.Message);
            ErrorMessage = ex.Message;
            Status = WorkflowExecutionStatus.Failed;
            StatusMessage = "Workflow failed";
        }
        finally
        {
            _stopwatch.Stop();
            _elapsedTimer.Change(Timeout.Infinite, Timeout.Infinite);
            CanCancel = false;
            IsComplete = true;
        }
    }

    /// <summary>
    /// Cancels the currently running workflow execution.
    /// </summary>
    /// <remarks>
    /// LOGIC (spec §5.4 — Cancellation Flow):
    /// <list type="number">
    ///   <item><description>Checks <see cref="CanCancel"/>; ignores if already cancelling.</description></item>
    ///   <item><description>Disables cancel button (prevents double-click).</description></item>
    ///   <item><description>Updates <see cref="StatusMessage"/> to "Cancelling...".</description></item>
    ///   <item><description>Triggers <see cref="CancellationTokenSource.Cancel"/> on the token source.</description></item>
    /// </list>
    /// </remarks>
    [RelayCommand]
    private void Cancel()
    {
        if (!CanCancel) return;

        // Acceptance Criteria #13: Cancel clicked → Cancel button disabled
        CanCancel = false;

        // Acceptance Criteria #14: Cancel clicked → Status shows "Cancelling..."
        StatusMessage = "Cancelling...";

        _logger.LogDebug("Cancel requested for workflow execution");

        _cancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// Applies the final workflow result to the active document selection.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #19: User clicks Apply → Document selection replaced.
    /// Uses <see cref="IEditorInsertionService.ReplaceSelectionAsync"/> to replace
    /// the current selection with the workflow output.
    /// </remarks>
    [RelayCommand]
    private async Task ApplyResultAsync()
    {
        if (Result?.FinalOutput is null) return;

        await _editorInsertionService.ReplaceSelectionAsync(Result.FinalOutput);
        StatusMessage = "Result applied to document";

        _logger.LogDebug("Workflow result applied to document");
    }

    /// <summary>
    /// Copies the final workflow result to the system clipboard.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #20: User clicks Copy → Result on clipboard.
    /// </remarks>
    [RelayCommand]
    private async Task CopyResultAsync()
    {
        if (Result?.FinalOutput is null) return;

        await _clipboardService.SetTextAsync(Result.FinalOutput);
        StatusMessage = "Result copied to clipboard";

        _logger.LogDebug("Workflow result copied to clipboard");
    }

    /// <summary>
    /// Opens an export dialog for the workflow result (placeholder).
    /// </summary>
    /// <remarks>
    /// LOGIC: Implementation depends on file dialog service (future enhancement).
    /// Currently a no-op placeholder.
    /// </remarks>
    [RelayCommand]
    private void ExportResult()
    {
        // Opens save dialog for markdown export
        // Implementation depends on file dialog service — placeholder per spec
        _logger.LogDebug("Export result requested (placeholder)");
    }

    /// <summary>
    /// Closes the execution panel (placeholder).
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        _logger.LogDebug("Execution panel close requested");
    }

    // ── Event Handlers (Internal) ───────────────────────────────────────

    /// <summary>
    /// Handles the <see cref="WorkflowStepStartedEvent"/> by updating the step state
    /// to Running and recalculating progress.
    /// </summary>
    /// <remarks>
    /// LOGIC (spec §5.3):
    /// <list type="number">
    ///   <item><description>Ignores events for other workflow IDs.</description></item>
    ///   <item><description>Finds the matching step state by <see cref="WorkflowStepStartedEvent.StepId"/>.</description></item>
    ///   <item><description>Sets status to Running, updates <see cref="CurrentStep"/>.</description></item>
    ///   <item><description>Recalculates progress and updates <see cref="StatusMessage"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="evt">The step started event from the workflow engine.</param>
    internal void OnStepStarted(WorkflowStepStartedEvent evt)
    {
        if (evt.WorkflowId != Workflow?.WorkflowId) return;

        var stepState = StepStates.FirstOrDefault(s => s.StepId == evt.StepId);
        if (stepState is null) return;

        stepState.Status = WorkflowStepStatus.Running;
        stepState.StatusMessage = "Processing...";
        CurrentStep = stepState;

        UpdateProgress();
        StatusMessage = $"Running step {evt.StepNumber} of {evt.TotalSteps}: {stepState.AgentName}";

        _logger.LogDebug(
            "Step {StepId} status changed to {Status}",
            evt.StepId, WorkflowStepStatus.Running);
    }

    /// <summary>
    /// Handles the <see cref="WorkflowStepCompletedEvent"/> by updating the step state
    /// with duration and status, then recalculating progress and estimated remaining time.
    /// </summary>
    /// <remarks>
    /// LOGIC (spec §5.3):
    /// <list type="number">
    ///   <item><description>Ignores events for other workflow IDs.</description></item>
    ///   <item><description>Updates step status, duration, and status message.</description></item>
    ///   <item><description>Recalculates overall progress (spec §5.1).</description></item>
    ///   <item><description>Recalculates estimated remaining time (spec §5.2).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="evt">The step completed event from the workflow engine.</param>
    internal void OnStepCompleted(WorkflowStepCompletedEvent evt)
    {
        if (evt.WorkflowId != Workflow?.WorkflowId) return;

        var stepState = StepStates.FirstOrDefault(s => s.StepId == evt.StepId);
        if (stepState is null) return;

        stepState.Status = evt.Status;
        stepState.Duration = evt.Duration;

        // Set human-readable status message based on step outcome
        stepState.StatusMessage = evt.Status switch
        {
            WorkflowStepStatus.Completed => $"Completed ({evt.Duration.TotalSeconds:F1}s)",
            WorkflowStepStatus.Skipped => "Skipped (condition not met)",
            WorkflowStepStatus.Failed => "Failed",
            _ => evt.Status.ToString()
        };

        UpdateProgress();
        CalculateEstimatedRemaining();

        _logger.LogDebug(
            "Step {StepId} status changed to {Status}",
            evt.StepId, evt.Status);
    }

    /// <summary>
    /// Handles the <see cref="WorkflowCompletedEvent"/> by marking execution as complete
    /// and updating the final status and progress.
    /// </summary>
    /// <remarks>
    /// LOGIC (spec §5.3):
    /// <list type="number">
    ///   <item><description>Ignores events for other workflow IDs.</description></item>
    ///   <item><description>Sets <see cref="Status"/>, <see cref="IsComplete"/>, <see cref="CanCancel"/>.</description></item>
    ///   <item><description>Updates <see cref="StatusMessage"/> based on final status.</description></item>
    ///   <item><description>Sets <see cref="Progress"/> to 100.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="evt">The workflow completed event from the engine.</param>
    internal void OnWorkflowCompleted(WorkflowCompletedEvent evt)
    {
        if (evt.WorkflowId != Workflow?.WorkflowId) return;

        Status = evt.Status;
        IsComplete = true;
        CanCancel = false;

        StatusMessage = evt.Status switch
        {
            WorkflowExecutionStatus.Completed => "Workflow completed successfully",
            WorkflowExecutionStatus.Failed => "Workflow failed",
            WorkflowExecutionStatus.PartialSuccess => "Workflow completed with some failures",
            _ => "Workflow finished"
        };

        Progress = 100;

        _logger.LogInformation(
            "Workflow execution completed: {Status} in {Duration}",
            evt.Status, evt.TotalDuration);
    }

    /// <summary>
    /// Handles the <see cref="WorkflowCancelledEvent"/> by marking execution as cancelled
    /// and updating all pending steps to Cancelled status.
    /// </summary>
    /// <remarks>
    /// LOGIC (spec §5.4 — Cancellation Flow, steps 9-11):
    /// <list type="number">
    ///   <item><description>Ignores events for other workflow IDs.</description></item>
    ///   <item><description>Sets status to Cancelled, marks complete.</description></item>
    ///   <item><description>Iterates remaining pending steps and marks them Cancelled.</description></item>
    /// </list>
    /// Acceptance Criteria #17: Partial results → Completed step outputs available.
    /// </remarks>
    /// <param name="evt">The workflow cancelled event from the engine.</param>
    internal void OnWorkflowCancelled(WorkflowCancelledEvent evt)
    {
        if (evt.WorkflowId != Workflow?.WorkflowId) return;

        Status = WorkflowExecutionStatus.Cancelled;
        IsComplete = true;
        CanCancel = false;
        StatusMessage = "Workflow cancelled by user";

        // Mark remaining pending steps as cancelled
        foreach (var step in StepStates.Where(s => s.Status == WorkflowStepStatus.Pending))
        {
            step.Status = WorkflowStepStatus.Cancelled;
            step.StatusMessage = "Cancelled";
        }

        _logger.LogWarning("Workflow cancelled by user");
    }

    // ── Progress Calculation (spec §5.1) ────────────────────────────────

    /// <summary>
    /// Recalculates the <see cref="Progress"/> percentage based on step states.
    /// </summary>
    /// <remarks>
    /// LOGIC (spec §5.1):
    /// <code>
    /// progress = ((completed + currentWeight) / total) * 100
    /// where currentWeight = 0.5 if any step is Running, else 0.0
    /// </code>
    /// Result is rounded to the nearest integer.
    /// </remarks>
    internal void UpdateProgress()
    {
        var total = StepStates.Count;
        if (total == 0)
        {
            Progress = 0;
            return;
        }

        // Count completed or skipped steps
        var completed = StepStates.Count(s =>
            s.Status is WorkflowStepStatus.Completed or WorkflowStepStatus.Skipped);

        // Running step gets 0.5 weight (halfway credit)
        var currentWeight = StepStates.Any(s => s.Status == WorkflowStepStatus.Running) ? 0.5 : 0;

        Progress = (int)Math.Round((completed + currentWeight) / total * 100, MidpointRounding.AwayFromZero);

        _logger.LogDebug("Progress updated: {Progress}%", Progress);
    }

    // ── Estimated Time (spec §5.2) ──────────────────────────────────────

    /// <summary>
    /// Calculates the estimated remaining time based on completed step durations.
    /// </summary>
    /// <remarks>
    /// LOGIC (spec §5.2):
    /// <list type="number">
    ///   <item><description>Requires ≥2 completed steps with duration data (else returns null).</description></item>
    ///   <item><description>Computes average step duration from completed steps.</description></item>
    ///   <item><description>Multiplies by the number of remaining (Pending) steps.</description></item>
    /// </list>
    /// </remarks>
    internal void CalculateEstimatedRemaining()
    {
        var completedSteps = StepStates
            .Where(s => s.Duration.HasValue && s.Status == WorkflowStepStatus.Completed)
            .ToList();

        // Need at least 2 completed steps for a meaningful estimate
        if (completedSteps.Count < 2)
        {
            EstimatedRemaining = null;
            return;
        }

        var avgDuration = completedSteps.Average(s => s.Duration!.Value.TotalSeconds);
        var remaining = StepStates.Count(s => s.Status == WorkflowStepStatus.Pending);

        EstimatedRemaining = TimeSpan.FromSeconds(avgDuration * remaining);

        _logger.LogDebug("Estimated remaining: {EstimatedRemaining}", EstimatedRemaining);
    }

    // ── Timer Callback ──────────────────────────────────────────────────

    /// <summary>
    /// Updates the <see cref="ElapsedTime"/> property from the stopwatch.
    /// Called every 100ms while execution is running.
    /// </summary>
    /// <param name="state">Timer callback state (unused).</param>
    private void UpdateElapsedTime(object? state)
    {
        ElapsedTime = _stopwatch.Elapsed;
    }

    // ── Agent Name / Icon Mapping ───────────────────────────────────────

    /// <summary>
    /// Maps an agent ID to a human-readable display name.
    /// </summary>
    /// <param name="agentId">The agent's unique identifier.</param>
    /// <returns>Human-readable name for the agent.</returns>
    private static string GetAgentName(string agentId) => agentId switch
    {
        "editor" => "Editor",
        "simplifier" => "Simplifier",
        "tuning" => "Tuning Agent",
        "summarizer" => "Summarizer",
        "copilot" => "CoPilot",
        _ => agentId
    };

    /// <summary>
    /// Maps an agent ID to a UI icon identifier.
    /// </summary>
    /// <param name="agentId">The agent's unique identifier.</param>
    /// <returns>Icon identifier string for the agent.</returns>
    private static string GetAgentIcon(string agentId) => agentId switch
    {
        "editor" => "edit-3",
        "simplifier" => "zap",
        "tuning" => "sliders",
        "summarizer" => "file-text",
        "copilot" => "message-circle",
        _ => "cpu"
    };
}
