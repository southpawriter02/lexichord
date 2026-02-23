// -----------------------------------------------------------------------
// <copyright file="WorkflowExecutionViewModelTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Events;
using Lexichord.Modules.Agents.ViewModels;
using Lexichord.Modules.Agents.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="WorkflowExecutionViewModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests the ViewModel's progress calculation (spec §5.1), estimated time calculation
/// (spec §5.2), event handling (spec §5.3), cancellation flow (spec §5.4), and
/// result actions (Apply, Copy). Each test exercises internal methods via reflection
/// or direct property access.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-v0.7.7d §10
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.7.7d")]
public class WorkflowExecutionViewModelTests
{
    /// <summary>Mock for the workflow execution engine.</summary>
    private readonly Mock<IWorkflowEngine> _engineMock;

    /// <summary>Mock for the editor insertion service (ReplaceSelectionAsync).</summary>
    private readonly Mock<IEditorInsertionService> _editorMock;

    /// <summary>Mock for the execution history service.</summary>
    private readonly Mock<IWorkflowExecutionHistoryService> _historyMock;

    /// <summary>Mock for the clipboard service.</summary>
    private readonly Mock<IClipboardService> _clipboardMock;

    /// <summary>The system under test.</summary>
    private readonly WorkflowExecutionViewModel _sut;

    /// <summary>
    /// Initializes test dependencies with mock objects and constructs the SUT.
    /// </summary>
    public WorkflowExecutionViewModelTests()
    {
        _engineMock = new Mock<IWorkflowEngine>();
        _editorMock = new Mock<IEditorInsertionService>();
        _historyMock = new Mock<IWorkflowExecutionHistoryService>();
        _clipboardMock = new Mock<IClipboardService>();

        _sut = new WorkflowExecutionViewModel(
            _engineMock.Object,
            _editorMock.Object,
            _historyMock.Object,
            _clipboardMock.Object,
            Mock.Of<ILogger<WorkflowExecutionViewModel>>());
    }

    // ── Initialize Tests ────────────────────────────────────────────────

    /// <summary>
    /// Verifies that Initialize sets the workflow, creates step states,
    /// and resets all state to initial values.
    /// </summary>
    [Fact]
    public void Initialize_SetsWorkflowAndCreatesStepStates()
    {
        // Arrange
        var workflow = CreateTestWorkflow(3);

        // Act
        _sut.Initialize(workflow, null, "Test content");

        // Assert
        _sut.Workflow.Should().Be(workflow);
        _sut.StepStates.Should().HaveCount(3);
        _sut.Status.Should().Be(WorkflowExecutionStatus.Pending);
        _sut.Progress.Should().Be(0);
        _sut.IsComplete.Should().BeFalse();
        _sut.CanCancel.Should().BeFalse();
    }

    // ── Progress Calculation Tests (spec §5.1) ──────────────────────────

    /// <summary>
    /// Verifies progress calculation: 1 of 4 running → ~13%.
    /// Formula: (0 + 0.5) / 4 * 100 = 12.5 → 13 (rounded).
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #1: Step 1 of 4 starts → Progress shows ~12%.
    /// </remarks>
    [Fact]
    public void Progress_CalculatesCorrectly_OneOfFourRunning()
    {
        // Arrange
        var workflow = CreateTestWorkflow(4);
        _sut.Initialize(workflow, null, "Test");

        // Act — simulate step 1 started
        _sut.StepStates[0].Status = WorkflowStepStatus.Running;
        _sut.UpdateProgress();

        // Assert
        _sut.Progress.Should().Be(13); // (0 + 0.5) / 4 * 100 = 12.5 → 13
    }

    /// <summary>
    /// Verifies progress: 2 completed + 1 running of 4 → ~63%.
    /// Formula: (2 + 0.5) / 4 * 100 = 62.5 → 63.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #2: Step 2 of 4 completes → Progress shows ~50%.
    /// </remarks>
    [Fact]
    public void Progress_CalculatesCorrectly_TwoOfFourCompleteOneRunning()
    {
        // Arrange
        var workflow = CreateTestWorkflow(4);
        _sut.Initialize(workflow, null, "Test");

        // Act
        _sut.StepStates[0].Status = WorkflowStepStatus.Completed;
        _sut.StepStates[1].Status = WorkflowStepStatus.Completed;
        _sut.StepStates[2].Status = WorkflowStepStatus.Running;
        _sut.UpdateProgress();

        // Assert
        _sut.Progress.Should().Be(63); // (2 + 0.5) / 4 * 100 = 62.5 → 63
    }

    /// <summary>
    /// Verifies progress: all 4 completed → 100%.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #3: All steps complete → Progress shows 100%.
    /// </remarks>
    [Fact]
    public void Progress_Is100_WhenAllComplete()
    {
        // Arrange
        var workflow = CreateTestWorkflow(4);
        _sut.Initialize(workflow, null, "Test");

        // Act
        foreach (var step in _sut.StepStates)
        {
            step.Status = WorkflowStepStatus.Completed;
        }
        _sut.UpdateProgress();

        // Assert
        _sut.Progress.Should().Be(100);
    }

    // ── Cancellation Tests (spec §5.4) ──────────────────────────────────

    /// <summary>
    /// Verifies that the Cancel command disables the cancel button and
    /// sets the status message to "Cancelling...".
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #13: Cancel clicked → Cancel button disabled.
    /// Acceptance Criteria #14: Cancel clicked → Status shows "Cancelling...".
    /// </remarks>
    [Fact]
    public void CancelCommand_SetsCancellingState()
    {
        // Arrange
        var workflow = CreateTestWorkflow(2);
        _sut.Initialize(workflow, null, "Test");
        // Enable CanCancel to simulate an active execution
        _sut.GetType().GetProperty("CanCancel")!.SetValue(_sut, true);

        // Act
        _sut.CancelCommand.Execute(null);

        // Assert
        _sut.CanCancel.Should().BeFalse();
        _sut.StatusMessage.Should().Contain("Cancelling");
    }

    // ── Result Action Tests ─────────────────────────────────────────────

    /// <summary>
    /// Verifies that ApplyResultCommand calls ReplaceSelectionAsync on the
    /// editor insertion service with the final output text.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #19: User clicks Apply → Document selection replaced.
    /// </remarks>
    [Fact]
    public async Task ApplyResultCommand_ReplacesSelection()
    {
        // Arrange
        var workflow = CreateTestWorkflow(1);
        _sut.Initialize(workflow, null, "Test");

        var result = new WorkflowExecutionResult(
            "wf-1", "exec-1", true, WorkflowExecutionStatus.Completed,
            Array.Empty<WorkflowStepExecutionResult>(),
            "Final output text",
            TimeSpan.FromSeconds(10),
            WorkflowUsageMetrics.Empty,
            new Dictionary<string, object>(),
            null);

        _sut.GetType().GetProperty("Result")!.SetValue(_sut, result);

        // Act
        await _sut.ApplyResultCommand.ExecuteAsync(null);

        // Assert
        _editorMock.Verify(e => e.ReplaceSelectionAsync("Final output text", default), Times.Once);
    }

    /// <summary>
    /// Verifies that CopyResultCommand copies the final output to the clipboard.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #20: User clicks Copy → Result on clipboard.
    /// </remarks>
    [Fact]
    public async Task CopyResultCommand_CopiesToClipboard()
    {
        // Arrange
        var workflow = CreateTestWorkflow(1);
        _sut.Initialize(workflow, null, "Test");

        var result = new WorkflowExecutionResult(
            "wf-1", "exec-1", true, WorkflowExecutionStatus.Completed,
            Array.Empty<WorkflowStepExecutionResult>(),
            "Clipboard content",
            TimeSpan.FromSeconds(5),
            WorkflowUsageMetrics.Empty,
            new Dictionary<string, object>(),
            null);

        _sut.GetType().GetProperty("Result")!.SetValue(_sut, result);

        // Act
        await _sut.CopyResultCommand.ExecuteAsync(null);

        // Assert
        _clipboardMock.Verify(c => c.SetTextAsync("Clipboard content", default), Times.Once);
    }

    // ── Event Handler Tests (spec §5.3) ─────────────────────────────────

    /// <summary>
    /// Verifies that OnStepStarted updates the step state to Running
    /// and sets CurrentStep.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #7: Step running → Shows spinner, blue highlight.
    /// </remarks>
    [Fact]
    public void OnStepStarted_UpdatesStepState()
    {
        // Arrange
        var workflow = CreateTestWorkflow(2);
        _sut.Initialize(workflow, null, "Test");

        var evt = new WorkflowStepStartedEvent(
            workflow.WorkflowId, "exec-1", "step-1", "editor", 1, 2);

        // Act
        _sut.OnStepStarted(evt);

        // Assert
        _sut.StepStates[0].Status.Should().Be(WorkflowStepStatus.Running);
        _sut.CurrentStep.Should().Be(_sut.StepStates[0]);
    }

    /// <summary>
    /// Verifies that OnStepCompleted updates the step state with duration
    /// and completion status.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #8: Step completed → Shows checkmark, duration.
    /// </remarks>
    [Fact]
    public void OnStepCompleted_UpdatesStepStateAndDuration()
    {
        // Arrange
        var workflow = CreateTestWorkflow(2);
        _sut.Initialize(workflow, null, "Test");
        _sut.StepStates[0].Status = WorkflowStepStatus.Running;

        var evt = new WorkflowStepCompletedEvent(
            workflow.WorkflowId, "exec-1", "step-1", true,
            WorkflowStepStatus.Completed, TimeSpan.FromSeconds(5.5));

        // Act
        _sut.OnStepCompleted(evt);

        // Assert
        _sut.StepStates[0].Status.Should().Be(WorkflowStepStatus.Completed);
        _sut.StepStates[0].Duration.Should().Be(TimeSpan.FromSeconds(5.5));
        _sut.StepStates[0].StatusMessage.Should().Contain("5.5");
    }

    /// <summary>
    /// Verifies that OnWorkflowCompleted sets IsComplete, Progress to 100,
    /// and updates Status.
    /// </summary>
    [Fact]
    public void OnWorkflowCompleted_SetsIsComplete()
    {
        // Arrange
        var workflow = CreateTestWorkflow(2);
        _sut.Initialize(workflow, null, "Test");

        var evt = new WorkflowCompletedEvent(
            workflow.WorkflowId, "exec-1", true,
            WorkflowExecutionStatus.Completed, TimeSpan.FromSeconds(30), 2, 0);

        // Act
        _sut.OnWorkflowCompleted(evt);

        // Assert
        _sut.IsComplete.Should().BeTrue();
        _sut.Status.Should().Be(WorkflowExecutionStatus.Completed);
        _sut.Progress.Should().Be(100);
    }

    /// <summary>
    /// Verifies that OnWorkflowCancelled marks pending steps as Cancelled.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #16: Workflow cancelled → Status = Cancelled.
    /// Acceptance Criteria #17: Partial results → Completed step outputs available.
    /// </remarks>
    [Fact]
    public void OnWorkflowCancelled_MarksPendingStepsAsCancelled()
    {
        // Arrange
        var workflow = CreateTestWorkflow(3);
        _sut.Initialize(workflow, null, "Test");

        // Simulate: step 1 completed, step 2 running, step 3 pending
        _sut.StepStates[0].Status = WorkflowStepStatus.Completed;
        _sut.StepStates[1].Status = WorkflowStepStatus.Running;

        var evt = new WorkflowCancelledEvent(
            workflow.WorkflowId, "exec-1", "step-2", 1);

        // Act
        _sut.OnWorkflowCancelled(evt);

        // Assert
        _sut.Status.Should().Be(WorkflowExecutionStatus.Cancelled);
        _sut.IsComplete.Should().BeTrue();
        _sut.StepStates[0].Status.Should().Be(WorkflowStepStatus.Completed); // Preserved
        _sut.StepStates[2].Status.Should().Be(WorkflowStepStatus.Cancelled); // Was Pending
    }

    // ── Estimated Time Tests (spec §5.2) ────────────────────────────────

    /// <summary>
    /// Verifies that estimated remaining time is null when fewer than 2
    /// steps have completed (insufficient data per spec §5.2).
    /// </summary>
    [Fact]
    public void EstimatedRemaining_IsNull_WithLessThanTwoCompletedSteps()
    {
        // Arrange
        var workflow = CreateTestWorkflow(4);
        _sut.Initialize(workflow, null, "Test");
        _sut.StepStates[0].Status = WorkflowStepStatus.Completed;
        _sut.StepStates[0].Duration = TimeSpan.FromSeconds(10);

        // Act
        _sut.CalculateEstimatedRemaining();

        // Assert — need ≥2 completed steps
        _sut.EstimatedRemaining.Should().BeNull();
    }

    /// <summary>
    /// Verifies that estimated remaining is calculated when 2+ steps are complete.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #4: 1+ steps complete → Estimated remaining shown.
    /// (Spec requires ≥2 for calculation.)
    /// </remarks>
    [Fact]
    public void EstimatedRemaining_IsCalculated_WithTwoCompletedSteps()
    {
        // Arrange
        var workflow = CreateTestWorkflow(4);
        _sut.Initialize(workflow, null, "Test");
        _sut.StepStates[0].Status = WorkflowStepStatus.Completed;
        _sut.StepStates[0].Duration = TimeSpan.FromSeconds(10);
        _sut.StepStates[1].Status = WorkflowStepStatus.Completed;
        _sut.StepStates[1].Duration = TimeSpan.FromSeconds(20);

        // 2 pending steps remain (steps 3 and 4)
        // avg = 15s, remaining = 2, estimate = 30s

        // Act
        _sut.CalculateEstimatedRemaining();

        // Assert
        _sut.EstimatedRemaining.Should().NotBeNull();
        _sut.EstimatedRemaining!.Value.TotalSeconds.Should().Be(30);
    }

    // ── Helper Methods ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a test workflow definition with the specified number of steps.
    /// </summary>
    /// <param name="stepCount">Number of steps to create.</param>
    /// <returns>A test <see cref="WorkflowDefinition"/>.</returns>
    private static WorkflowDefinition CreateTestWorkflow(int stepCount)
    {
        var steps = Enumerable.Range(1, stepCount)
            .Select(i => new WorkflowStepDefinition(
                $"step-{i}", "editor", null, null, i, null, null, null))
            .ToList();

        return new WorkflowDefinition(
            "wf-test", "Test Workflow", "", null, steps,
            new WorkflowMetadata("test", DateTime.UtcNow, DateTime.UtcNow, null,
                Array.Empty<string>(), WorkflowCategory.General, false, LicenseTier.Teams));
    }
}
