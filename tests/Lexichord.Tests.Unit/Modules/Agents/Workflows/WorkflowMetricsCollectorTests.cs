// -----------------------------------------------------------------------
// <copyright file="WorkflowMetricsCollectorTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Workflows.Validation.Metrics;
using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="WorkflowMetricsCollector"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests recording of metrics, report generation, health score delegation,
/// and error handling behavior (fire-and-forget recording vs. propagated queries).
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §7
/// </para>
/// </remarks>
[Trait("Category", "Workflows")]
[Trait("Version", "v0.7.7i")]
public class WorkflowMetricsCollectorTests
{
    // ── Fields ───────────────────────────────────────────────────────────

    private readonly Mock<IMetricsStore> _storeMock;
    private readonly Mock<IHealthScoreCalculator> _healthCalcMock;
    private readonly WorkflowMetricsCollector _sut;

    // ── Constructor ─────────────────────────────────────────────────────

    public WorkflowMetricsCollectorTests()
    {
        _storeMock = new Mock<IMetricsStore>();
        _healthCalcMock = new Mock<IHealthScoreCalculator>();
        var loggerMock = new Mock<ILogger<WorkflowMetricsCollector>>();

        _sut = new WorkflowMetricsCollector(
            _storeMock.Object,
            _healthCalcMock.Object,
            loggerMock.Object);
    }

    // ── RecordWorkflowExecution Tests ────────────────────────────────────

    /// <summary>
    /// Verifies that recording an execution delegates to the store.
    /// </summary>
    [Fact]
    public async Task RecordWorkflowExecution_DelegatesToStore()
    {
        // Arrange
        var metrics = CreateExecution();

        // Act
        await _sut.RecordWorkflowExecutionAsync(metrics);

        // Assert
        _storeMock.Verify(
            s => s.StoreWorkflowExecutionAsync(metrics, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that store exceptions during recording are caught (fire-and-forget).
    /// </summary>
    [Fact]
    public async Task RecordWorkflowExecution_StoreThrows_DoesNotPropagate()
    {
        // Arrange
        _storeMock.Setup(s => s.StoreWorkflowExecutionAsync(
                It.IsAny<WorkflowExecutionMetrics>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Store unavailable"));

        // Act — should not throw
        var act = () => _sut.RecordWorkflowExecutionAsync(CreateExecution());
        await act.Should().NotThrowAsync();
    }

    // ── RecordValidationStep Tests ──────────────────────────────────────

    /// <summary>
    /// Verifies that recording a validation step delegates to the store.
    /// </summary>
    [Fact]
    public async Task RecordValidationStep_DelegatesToStore()
    {
        // Arrange
        var metrics = new ValidationStepMetrics
        {
            StepId = "schema-validation",
            StepName = "Schema Validation",
            Passed = true,
            ExecutedAt = DateTime.UtcNow
        };

        // Act
        await _sut.RecordValidationStepAsync(metrics);

        // Assert
        _storeMock.Verify(
            s => s.StoreValidationStepAsync(metrics, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── RecordGatingDecision Tests ──────────────────────────────────────

    /// <summary>
    /// Verifies that recording a gating decision delegates to the store.
    /// </summary>
    [Fact]
    public async Task RecordGatingDecision_DelegatesToStore()
    {
        // Arrange
        var metrics = new GatingDecisionMetrics
        {
            GateId = "publish-gate",
            GateName = "Publish Gate",
            Passed = false,
            ExecutedAt = DateTime.UtcNow
        };

        // Act
        await _sut.RecordGatingDecisionAsync(metrics);

        // Assert
        _storeMock.Verify(
            s => s.StoreGatingDecisionAsync(metrics, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── RecordSyncOperation Tests ───────────────────────────────────────

    /// <summary>
    /// Verifies that recording a sync operation delegates to the store.
    /// </summary>
    [Fact]
    public async Task RecordSyncOperation_DelegatesToStore()
    {
        // Arrange
        var metrics = new SyncOperationMetrics
        {
            Direction = "DocumentToGraph",
            Success = true,
            ItemsSynced = 10,
            ExecutedAt = DateTime.UtcNow
        };

        // Act
        await _sut.RecordSyncOperationAsync(metrics);

        // Assert
        _storeMock.Verify(
            s => s.StoreSyncOperationAsync(metrics, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── GetMetricsAsync Tests ───────────────────────────────────────────

    /// <summary>
    /// Verifies that GetMetricsAsync generates a report with statistics.
    /// </summary>
    [Fact]
    public async Task GetMetrics_GeneratesReportWithStatistics()
    {
        // Arrange
        var query = new MetricsQuery { WorkspaceId = Guid.NewGuid() };
        var executions = new List<WorkflowExecutionMetrics>
        {
            CreateExecution(success: true),
            CreateExecution(success: false, totalErrors: 5)
        };

        _storeMock.Setup(s => s.GetWorkflowExecutionsAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(executions);
        _storeMock.Setup(s => s.GetValidationStepsAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ValidationStepMetrics>());
        _storeMock.Setup(s => s.GetGatingDecisionsAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GatingDecisionMetrics>());
        _storeMock.Setup(s => s.GetSyncOperationsAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncOperationMetrics>());

        // Act
        var report = await _sut.GetMetricsAsync(query);

        // Assert
        report.Should().NotBeNull();
        report.WorkflowExecutions.Should().HaveCount(2);
        report.Statistics.Should().NotBeNull();
        report.Statistics!.TotalExecutions.Should().Be(2);
        report.Statistics.SuccessfulExecutions.Should().Be(1);
        report.Statistics.FailedExecutions.Should().Be(1);
        report.Statistics.SuccessRate.Should().Be(50m);
    }

    /// <summary>
    /// Verifies that GetMetricsAsync propagates store exceptions.
    /// </summary>
    [Fact]
    public async Task GetMetrics_StoreThrows_Propagates()
    {
        // Arrange
        var query = new MetricsQuery { WorkspaceId = Guid.NewGuid() };
        _storeMock.Setup(s => s.GetWorkflowExecutionsAsync(query, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        var act = () => _sut.GetMetricsAsync(query);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── GetHealthScoreAsync Tests ───────────────────────────────────────

    /// <summary>
    /// Verifies that GetHealthScoreAsync delegates to the calculator.
    /// </summary>
    [Fact]
    public async Task GetHealthScore_DelegatesToCalculator()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var expectedScore = new WorkspaceHealthScore
        {
            WorkspaceId = workspaceId,
            HealthScore = 85,
            CalculatedAt = DateTime.UtcNow
        };

        _storeMock.Setup(s => s.GetWorkflowExecutionsAsync(
                It.IsAny<MetricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkflowExecutionMetrics>());
        _healthCalcMock.Setup(h => h.CalculateWorkspaceHealth(It.IsAny<IReadOnlyList<WorkflowExecutionMetrics>>()))
            .Returns(expectedScore);

        // Act
        var result = await _sut.GetHealthScoreAsync(workspaceId);

        // Assert
        result.Should().BeSameAs(expectedScore);
    }

    // ── GetDocumentHealthAsync Tests ────────────────────────────────────

    /// <summary>
    /// Verifies that GetDocumentHealthAsync delegates to the calculator.
    /// </summary>
    [Fact]
    public async Task GetDocumentHealth_DelegatesToCalculator()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var expectedScore = new DocumentHealthScore
        {
            DocumentId = documentId,
            HealthScore = 72,
            CalculatedAt = DateTime.UtcNow
        };

        _storeMock.Setup(s => s.GetDocumentMetricsAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkflowExecutionMetrics>());
        _healthCalcMock.Setup(h => h.CalculateDocumentHealth(
                documentId, It.IsAny<IReadOnlyList<WorkflowExecutionMetrics>>()))
            .Returns(expectedScore);

        // Act
        var result = await _sut.GetDocumentHealthAsync(documentId);

        // Assert
        result.Should().BeSameAs(expectedScore);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static WorkflowExecutionMetrics CreateExecution(
        bool success = true,
        int totalErrors = 0) =>
        new()
        {
            ExecutionId = Guid.NewGuid(),
            WorkflowId = "test-workflow",
            WorkspaceId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            Success = success,
            TotalErrors = totalErrors,
            TotalExecutionTimeMs = 1500,
            ExecutedAt = DateTime.UtcNow,
            Status = success ? MetricsExecutionStatus.Success : MetricsExecutionStatus.Failed
        };
}
