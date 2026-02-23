// -----------------------------------------------------------------------
// <copyright file="WorkflowExecutionHistoryServiceTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.Agents.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="WorkflowExecutionHistoryService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests the in-memory history service's recording, querying, statistics computation,
/// clearing, and entry trimming behavior.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-v0.7.7d §9.5 (History Acceptance Criteria).
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.7.7d")]
public class WorkflowExecutionHistoryServiceTests
{
    /// <summary>The system under test.</summary>
    private readonly WorkflowExecutionHistoryService _sut;

    /// <summary>
    /// Initializes the service with a mock logger.
    /// </summary>
    public WorkflowExecutionHistoryServiceTests()
    {
        _sut = new WorkflowExecutionHistoryService(
            Mock.Of<ILogger<WorkflowExecutionHistoryService>>());
    }

    // ── RecordAsync Tests ───────────────────────────────────────────────

    /// <summary>
    /// Verifies that RecordAsync stores an execution and it can be retrieved.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #22: Workflow completes → Execution recorded.
    /// </remarks>
    [Fact]
    public async Task RecordAsync_StoresExecution()
    {
        // Arrange
        var result = CreateTestResult("wf-1", "exec-1", WorkflowExecutionStatus.Completed);

        // Act
        await _sut.RecordAsync(result, "Test Workflow");

        // Assert
        var history = await _sut.GetHistoryAsync();
        history.Should().HaveCount(1);
        history[0].ExecutionId.Should().Be("exec-1");
        history[0].WorkflowName.Should().Be("Test Workflow");
    }

    // ── GetHistoryAsync Tests ───────────────────────────────────────────

    /// <summary>
    /// Verifies that GetHistoryAsync returns all entries when no filter is specified,
    /// ordered by most recent first.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_ReturnsAllEntries_OrderedByDate()
    {
        // Arrange
        await _sut.RecordAsync(CreateTestResult("wf-1", "exec-1", WorkflowExecutionStatus.Completed), "W1");
        await _sut.RecordAsync(CreateTestResult("wf-1", "exec-2", WorkflowExecutionStatus.Failed), "W1");
        await _sut.RecordAsync(CreateTestResult("wf-2", "exec-3", WorkflowExecutionStatus.Completed), "W2");

        // Act
        var history = await _sut.GetHistoryAsync(limit: 10);

        // Assert
        history.Should().HaveCount(3);
    }

    /// <summary>
    /// Verifies that GetHistoryAsync filters by workflow ID when specified.
    /// </summary>
    [Fact]
    public async Task GetHistoryAsync_FiltersByWorkflowId()
    {
        // Arrange
        await _sut.RecordAsync(CreateTestResult("wf-1", "exec-1", WorkflowExecutionStatus.Completed), "W1");
        await _sut.RecordAsync(CreateTestResult("wf-2", "exec-2", WorkflowExecutionStatus.Completed), "W2");
        await _sut.RecordAsync(CreateTestResult("wf-1", "exec-3", WorkflowExecutionStatus.Failed), "W1");

        // Act
        var history = await _sut.GetHistoryAsync("wf-1");

        // Assert
        history.Should().HaveCount(2);
        history.Should().AllSatisfy(h => h.WorkflowId.Should().Be("wf-1"));
    }

    /// <summary>
    /// Verifies that GetHistoryAsync respects the limit parameter.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #23: Teams user → Last 10 executions shown.
    /// </remarks>
    [Fact]
    public async Task GetHistoryAsync_RespectsLimit()
    {
        // Arrange — insert 5 entries
        for (var i = 0; i < 5; i++)
        {
            await _sut.RecordAsync(
                CreateTestResult("wf-1", $"exec-{i}", WorkflowExecutionStatus.Completed), "W1");
        }

        // Act
        var history = await _sut.GetHistoryAsync(limit: 3);

        // Assert
        history.Should().HaveCount(3);
    }

    // ── GetStatisticsAsync Tests ────────────────────────────────────────

    /// <summary>
    /// Verifies that GetStatisticsAsync returns zero statistics when no
    /// entries exist for the specified workflow.
    /// </summary>
    [Fact]
    public async Task GetStatisticsAsync_ReturnsZeroStats_WhenNoEntries()
    {
        // Act
        var stats = await _sut.GetStatisticsAsync("nonexistent");

        // Assert
        stats.TotalExecutions.Should().Be(0);
        stats.SuccessRate.Should().Be(0);
        stats.FirstExecution.Should().BeNull();
        stats.LastExecution.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetStatisticsAsync correctly computes aggregated metrics
    /// from multiple executions.
    /// </summary>
    [Fact]
    public async Task GetStatisticsAsync_ComputesCorrectMetrics()
    {
        // Arrange
        await _sut.RecordAsync(CreateTestResult("wf-1", "exec-1", WorkflowExecutionStatus.Completed), "W1");
        await _sut.RecordAsync(CreateTestResult("wf-1", "exec-2", WorkflowExecutionStatus.Failed), "W1");
        await _sut.RecordAsync(CreateTestResult("wf-1", "exec-3", WorkflowExecutionStatus.Completed), "W1");
        await _sut.RecordAsync(CreateTestResult("wf-1", "exec-4", WorkflowExecutionStatus.Cancelled), "W1");

        // Act
        var stats = await _sut.GetStatisticsAsync("wf-1");

        // Assert
        stats.TotalExecutions.Should().Be(4);
        stats.SuccessfulExecutions.Should().Be(2);
        stats.FailedExecutions.Should().Be(1);
        stats.CancelledExecutions.Should().Be(1);
        stats.SuccessRate.Should().Be(0.5);
    }

    // ── ClearHistoryAsync Tests ─────────────────────────────────────────

    /// <summary>
    /// Verifies that ClearHistoryAsync removes entries older than the cutoff date.
    /// </summary>
    [Fact]
    public async Task ClearHistoryAsync_RemovesOldEntries()
    {
        // Arrange
        await _sut.RecordAsync(CreateTestResult("wf-1", "exec-1", WorkflowExecutionStatus.Completed), "W1");
        await _sut.RecordAsync(CreateTestResult("wf-1", "exec-2", WorkflowExecutionStatus.Completed), "W1");

        // Act — clear everything older than 1 second in the future (all entries)
        await _sut.ClearHistoryAsync(DateTime.UtcNow.AddSeconds(1));

        // Assert
        var history = await _sut.GetHistoryAsync();
        history.Should().BeEmpty();
    }

    // ── Max Entries Trimming Test ────────────────────────────────────────

    /// <summary>
    /// Verifies that the history is trimmed to <see cref="WorkflowExecutionHistoryService.MaxHistoryEntries"/>
    /// when exceeding the limit.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #24: History > 100 → New entry → Oldest entries trimmed.
    /// </remarks>
    [Fact]
    public async Task RecordAsync_TrimsToMaxEntries()
    {
        // Arrange — insert 105 entries (exceeds 100 limit)
        for (var i = 0; i < 105; i++)
        {
            await _sut.RecordAsync(
                CreateTestResult("wf-1", $"exec-{i}", WorkflowExecutionStatus.Completed), "W1");
        }

        // Act
        var history = await _sut.GetHistoryAsync(limit: 200);

        // Assert — should be trimmed to 100
        history.Should().HaveCountLessOrEqualTo(WorkflowExecutionHistoryService.MaxHistoryEntries);
    }

    // ── Helper Methods ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a test <see cref="WorkflowExecutionResult"/> with minimal data.
    /// </summary>
    private static WorkflowExecutionResult CreateTestResult(
        string workflowId, string executionId, WorkflowExecutionStatus status)
    {
        return new WorkflowExecutionResult(
            workflowId,
            executionId,
            status == WorkflowExecutionStatus.Completed,
            status,
            new List<WorkflowStepExecutionResult>
            {
                new("step-1", "editor", true, WorkflowStepStatus.Completed,
                    "Output text", TimeSpan.FromSeconds(5),
                    new AgentUsageMetrics(100, 50, 150, 0.002m), null, null)
            },
            "Final output",
            TimeSpan.FromSeconds(10),
            new WorkflowUsageMetrics(100, 50, 150, 0.002m, 1, 0),
            new Dictionary<string, object>(),
            status == WorkflowExecutionStatus.Failed ? "Test error" : null);
    }
}
