// -----------------------------------------------------------------------
// <copyright file="WorkflowEngineTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Agents.Events;
using Lexichord.Modules.Agents.Workflows;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="WorkflowEngine"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests the core workflow execution engine functionality including sequential
/// execution, condition evaluation (Always, PreviousSuccess, PreviousFailed,
/// Expression), output mapping, failure handling (StopOnFirstFailure), cancellation,
/// usage metric aggregation, and MediatR event publishing.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-v0.7.7b §10
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.7.7b")]
public class WorkflowEngineTests
{
    private readonly Mock<IAgentRegistry> _registryMock;
    private readonly Mock<IExpressionEvaluator> _evaluatorMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<WorkflowEngine>> _loggerMock;
    private readonly WorkflowEngine _sut;

    public WorkflowEngineTests()
    {
        _registryMock = new Mock<IAgentRegistry>();
        _evaluatorMock = new Mock<IExpressionEvaluator>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<WorkflowEngine>>();

        _sut = new WorkflowEngine(
            _registryMock.Object,
            _evaluatorMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    // ── Test 1: Single Step Execution ────────────────────────────────────

    /// <summary>
    /// Verifies that a single-step workflow invokes the agent and returns
    /// a successful result with the agent's output as FinalOutput.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #1: Workflow with steps → steps execute in order.
    /// Acceptance Criteria #12: Multi-step → FinalOutput is last step output.
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_SingleStep_InvokesAgentAndReturnsResult()
    {
        // Arrange
        var workflow = CreateWorkflow(CreateStep("editor"));
        var context = CreateContext();
        SetupAgent("editor", "Edited content");

        // Act
        var result = await _sut.ExecuteAsync(workflow, context);

        // Assert
        result.Success.Should().BeTrue();
        result.Status.Should().Be(WorkflowExecutionStatus.Completed);
        result.StepResults.Should().HaveCount(1);
        result.FinalOutput.Should().Be("Edited content");
    }

    // ── Test 2: Multi-Step Ordered Execution ─────────────────────────────

    /// <summary>
    /// Verifies that multiple steps execute in ascending Order and the
    /// FinalOutput reflects the last step's output.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #1: Both steps execute in order.
    /// Acceptance Criteria #12: FinalOutput is last step output.
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_MultipleSteps_ExecutesInOrder()
    {
        // Arrange
        var executionOrder = new List<string>();
        var workflow = CreateWorkflow(
            CreateStep("editor", order: 1),
            CreateStep("simplifier", order: 2));
        var context = CreateContext();

        _registryMock.Setup(r => r.GetAgent("editor"))
            .Returns(CreateMockAgent("editor", "Output 1", () => executionOrder.Add("editor")));
        _registryMock.Setup(r => r.GetAgent("simplifier"))
            .Returns(CreateMockAgent("simplifier", "Output 2", () => executionOrder.Add("simplifier")));

        // Act
        var result = await _sut.ExecuteAsync(workflow, context);

        // Assert
        executionOrder.Should().ContainInOrder("editor", "simplifier");
        result.StepResults.Should().HaveCount(2);
        result.FinalOutput.Should().Be("Output 2");
    }

    // ── Test 3: Condition False Skips Step ────────────────────────────────

    /// <summary>
    /// Verifies that a step with a condition evaluating to false is skipped
    /// and the agent is never invoked.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #7: Expression evaluates false → Step skipped.
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_ConditionFalse_SkipsStep()
    {
        // Arrange
        var workflow = CreateWorkflow(
            CreateStep("editor", condition: new WorkflowStepCondition("false", ConditionType.Expression)));
        var context = CreateContext();

        _evaluatorMock.Setup(e => e.Evaluate<bool>("false", It.IsAny<IReadOnlyDictionary<string, object>>()))
            .Returns(false);

        // Act
        var result = await _sut.ExecuteAsync(workflow, context);

        // Assert
        result.Success.Should().BeTrue();
        result.StepResults[0].Status.Should().Be(WorkflowStepStatus.Skipped);
        _registryMock.Verify(r => r.GetAgent(It.IsAny<string>()), Times.Never);
    }

    // ── Test 4: PreviousSuccess Condition ─────────────────────────────────

    /// <summary>
    /// Verifies that a step with PreviousSuccess condition executes when
    /// the previous step succeeded.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #3: PreviousSuccess condition + success → executes.
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_PreviousSuccessCondition_ExecutesOnSuccess()
    {
        // Arrange
        var workflow = CreateWorkflow(
            CreateStep("editor", order: 1),
            CreateStep("simplifier", order: 2,
                condition: new WorkflowStepCondition("", ConditionType.PreviousSuccess)));
        var context = CreateContext();
        SetupAgent("editor", "Output 1");
        SetupAgent("simplifier", "Output 2");

        // Act
        var result = await _sut.ExecuteAsync(workflow, context);

        // Assert
        result.StepResults.Should().HaveCount(2);
        result.StepResults[1].Status.Should().Be(WorkflowStepStatus.Completed);
    }

    // ── Test 5: StopOnFirstFailure ────────────────────────────────────────

    /// <summary>
    /// Verifies that when StopOnFirstFailure=true and a step fails, the
    /// workflow stops and remaining steps are skipped.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #9: StopOnFirstFailure=true + failure → remaining skipped.
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_StepFails_StopsOnFirstFailure()
    {
        // Arrange
        var workflow = CreateWorkflow(
            CreateStep("editor", order: 1),
            CreateStep("simplifier", order: 2));
        var context = CreateContext(options: new WorkflowExecutionOptions(StopOnFirstFailure: true));

        _registryMock.Setup(r => r.GetAgent("editor"))
            .Returns(CreateFailingAgent("editor", "Error occurred"));

        // Act
        var result = await _sut.ExecuteAsync(workflow, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Status.Should().Be(WorkflowExecutionStatus.Failed);
        result.StepResults[0].Status.Should().Be(WorkflowStepStatus.Failed);
        result.StepResults[1].Status.Should().Be(WorkflowStepStatus.Skipped);
    }

    // ── Test 6: Cancellation ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that cancellation during step execution stops the workflow
    /// gracefully with Cancelled status.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #11: Cancellation requested → workflow cancels gracefully.
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_Cancellation_StopsGracefully()
    {
        // Arrange
        var workflow = CreateWorkflow(
            CreateStep("editor", order: 1),
            CreateStep("simplifier", order: 2));
        var context = CreateContext();
        var cts = new CancellationTokenSource();

        _registryMock.Setup(r => r.GetAgent("editor"))
            .Returns(CreateMockAgent("editor", "Output 1", () => cts.Cancel()));

        // Act
        var result = await _sut.ExecuteAsync(workflow, context, cts.Token);

        // Assert
        result.Status.Should().Be(WorkflowExecutionStatus.Cancelled);
    }

    // ── Test 7: Token Usage Aggregation ───────────────────────────────────

    /// <summary>
    /// Verifies that token usage metrics are correctly aggregated across
    /// all executed steps.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria: Aggregated metrics include all step token counts.
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_AggregatesTokenUsage()
    {
        // Arrange
        var workflow = CreateWorkflow(
            CreateStep("editor", order: 1),
            CreateStep("simplifier", order: 2));
        var context = CreateContext();

        SetupAgentWithUsage("editor", 100, 50);
        SetupAgentWithUsage("simplifier", 150, 75);

        // Act
        var result = await _sut.ExecuteAsync(workflow, context);

        // Assert
        result.TotalUsage.TotalPromptTokens.Should().Be(250);
        result.TotalUsage.TotalCompletionTokens.Should().Be(125);
        result.TotalUsage.TotalTokens.Should().Be(375);
        result.TotalUsage.StepsExecuted.Should().Be(2);
    }

    // ── Test 8: MediatR Event Publishing ──────────────────────────────────

    /// <summary>
    /// Verifies that the engine publishes the correct sequence of MediatR events
    /// during single-step execution (Started, StepStarted, StepCompleted, Completed).
    /// </summary>
    /// <remarks>
    /// Tests the observability contract from spec §7.
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_PublishesEvents()
    {
        // Arrange
        var workflow = CreateWorkflow(CreateStep("editor"));
        var context = CreateContext();
        SetupAgent("editor", "Output");

        // Act
        await _sut.ExecuteAsync(workflow, context);

        // Assert
        _mediatorMock.Verify(m => m.Publish(It.IsAny<WorkflowStartedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(m => m.Publish(It.IsAny<WorkflowStepStartedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(m => m.Publish(It.IsAny<WorkflowStepCompletedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(m => m.Publish(It.IsAny<WorkflowCompletedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Helper Methods ───────────────────────────────────────────────────

    /// <summary>
    /// Creates a workflow definition with the specified steps.
    /// </summary>
    private WorkflowDefinition CreateWorkflow(params WorkflowStepDefinition[] steps) =>
        new("wf-test", "Test", "", null, steps, CreateMetadata());

    /// <summary>
    /// Creates a step definition with configurable agent ID, order, and condition.
    /// </summary>
    private WorkflowStepDefinition CreateStep(
        string agentId,
        int order = 1,
        WorkflowStepCondition? condition = null) =>
        new($"step-{order}", agentId, null, null, order, condition, null, null);

    /// <summary>
    /// Creates an execution context with optional execution options.
    /// </summary>
    private WorkflowExecutionContext CreateContext(WorkflowExecutionOptions? options = null) =>
        new(null, "Test content", new Dictionary<string, object>(),
            options ?? new WorkflowExecutionOptions());

    /// <summary>
    /// Sets up the registry mock to return an agent that produces the specified output.
    /// </summary>
    private void SetupAgent(string agentId, string output)
    {
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.InvokeAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResponse(output, null, new UsageMetrics(100, 50, 0.003m)));
        _registryMock.Setup(r => r.GetAgent(agentId)).Returns(mockAgent.Object);
    }

    /// <summary>
    /// Sets up the registry mock to return an agent with specific token usage.
    /// </summary>
    private void SetupAgentWithUsage(string agentId, int prompt, int completion)
    {
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.InvokeAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResponse("Output", null, new UsageMetrics(prompt, completion, 0m)));
        _registryMock.Setup(r => r.GetAgent(agentId)).Returns(mockAgent.Object);
    }

    /// <summary>
    /// Creates a mock agent that optionally invokes a callback before returning.
    /// </summary>
    private IAgent CreateMockAgent(string agentId, string output, Action? callback = null)
    {
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.InvokeAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callback?.Invoke();
                return new AgentResponse(output, null, new UsageMetrics(100, 50, 0.003m));
            });
        return mockAgent.Object;
    }

    /// <summary>
    /// Creates a mock agent that throws an exception when invoked.
    /// </summary>
    private IAgent CreateFailingAgent(string agentId, string error)
    {
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.InvokeAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(error));
        return mockAgent.Object;
    }

    /// <summary>
    /// Creates a default workflow metadata record for tests.
    /// </summary>
    private WorkflowMetadata CreateMetadata() =>
        new("test", DateTime.UtcNow, DateTime.UtcNow, "1.0", Array.Empty<string>(),
            WorkflowCategory.Custom, false, LicenseTier.Teams);
}
