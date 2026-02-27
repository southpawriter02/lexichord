// -----------------------------------------------------------------------
// <copyright file="HealthScoreCalculatorTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Workflows.Validation.Metrics;
using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="HealthScoreCalculator"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests the health score formulas:
/// <list type="bullet">
///   <item><description>Workspace: PassRate × 80 + ErrorFreedom (0–20)</description></item>
///   <item><description>Document: BaseScore (80/30) − ErrorPenalty − WarningPenalty</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §7
/// </para>
/// </remarks>
[Trait("Category", "Workflows")]
[Trait("Version", "v0.7.7i")]
public class HealthScoreCalculatorTests
{
    // ── Fields ───────────────────────────────────────────────────────────

    private readonly HealthScoreCalculator _sut;

    // ── Constructor ─────────────────────────────────────────────────────

    public HealthScoreCalculatorTests()
    {
        var loggerMock = new Mock<ILogger<HealthScoreCalculator>>();
        _sut = new HealthScoreCalculator(loggerMock.Object);
    }

    // ── Workspace Health Tests ──────────────────────────────────────────

    /// <summary>
    /// Verifies that an empty metrics list produces a zero-score workspace health.
    /// </summary>
    [Fact]
    public void CalculateWorkspaceHealth_NoMetrics_ReturnsZeroScore()
    {
        // Act
        var result = _sut.CalculateWorkspaceHealth([]);

        // Assert
        result.HealthScore.Should().Be(0);
        result.PassRate.Should().Be(0);
        result.Trend.Should().Be(HealthTrend.Stable);
    }

    /// <summary>
    /// Verifies that all-passing executions produce a high workspace health score.
    /// </summary>
    [Fact]
    public void CalculateWorkspaceHealth_AllPassing_ReturnsHighScore()
    {
        // Arrange — all 3 executions pass with 0 errors
        var metrics = Enumerable.Range(0, 3)
            .Select(_ => CreateExecution(success: true, totalErrors: 0))
            .ToList();

        // Act
        var result = _sut.CalculateWorkspaceHealth(metrics);

        // Assert
        // PassRate = 1.0 → baseScore = 80, errorFreedom = 20 → healthScore = 100
        result.HealthScore.Should().Be(100);
        result.PassRate.Should().Be(1m);
    }

    /// <summary>
    /// Verifies that errors reduce the workspace health score.
    /// </summary>
    [Fact]
    public void CalculateWorkspaceHealth_WithErrors_ReducesScore()
    {
        // Arrange — 2 passing, 1 failing with 50 errors
        var metrics = new List<WorkflowExecutionMetrics>
        {
            CreateExecution(success: true, totalErrors: 0),
            CreateExecution(success: true, totalErrors: 0),
            CreateExecution(success: false, totalErrors: 50)
        };

        // Act
        var result = _sut.CalculateWorkspaceHealth(metrics);

        // Assert
        // PassRate = 2/3 = 0.667 → baseScore = 53
        // errorFreedom = max(0, 20 - 50/10) = max(0, 15) = 15
        // healthScore = 53 + 15 = 68
        result.HealthScore.Should().BeInRange(50, 80);
        result.DocumentsWithErrors.Should().Be(1);
    }

    /// <summary>
    /// Verifies that the pass rate correctly reflects the fraction of successful executions.
    /// </summary>
    [Fact]
    public void CalculateWorkspaceHealth_PassRateReflectsSuccessRatio()
    {
        // Arrange — 1 pass, 1 fail
        var metrics = new List<WorkflowExecutionMetrics>
        {
            CreateExecution(success: true, totalErrors: 0),
            CreateExecution(success: false, totalErrors: 5)
        };

        // Act
        var result = _sut.CalculateWorkspaceHealth(metrics);

        // Assert
        result.PassRate.Should().Be(0.5m);
    }

    /// <summary>
    /// Verifies that documents needing attention are counted correctly.
    /// </summary>
    [Fact]
    public void CalculateWorkspaceHealth_CountsDocumentsNeedingAttention()
    {
        // Arrange — 1 doc with errors, 1 doc with many warnings
        var metrics = new List<WorkflowExecutionMetrics>
        {
            CreateExecution(success: false, totalErrors: 3, totalWarnings: 0),
            CreateExecution(success: true, totalErrors: 0, totalWarnings: 10)
        };

        // Act
        var result = _sut.CalculateWorkspaceHealth(metrics);

        // Assert — both should need attention (errors > 0 OR warnings > 5)
        result.DocumentsNeedingAttention.Should().Be(2);
    }

    // ── Document Health Tests ───────────────────────────────────────────

    /// <summary>
    /// Verifies that a document with no metrics returns NotValidated state.
    /// </summary>
    [Fact]
    public void CalculateDocumentHealth_NoMetrics_ReturnsNotValidated()
    {
        // Act
        var result = _sut.CalculateDocumentHealth(Guid.NewGuid(), []);

        // Assert
        result.HealthScore.Should().Be(0);
        result.ValidationState.Should().Be(DocumentValidationState.NotValidated);
    }

    /// <summary>
    /// Verifies that a passing document with no errors/warnings gets a perfect score.
    /// </summary>
    [Fact]
    public void CalculateDocumentHealth_PerfectPass_Returns80()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var metrics = new List<WorkflowExecutionMetrics>
        {
            CreateExecution(success: true, totalErrors: 0, totalWarnings: 0, documentId: docId)
        };

        // Act
        var result = _sut.CalculateDocumentHealth(docId, metrics);

        // Assert
        // baseScore = 80, errorPenalty = 0, warningPenalty = 0 → 80
        result.HealthScore.Should().Be(80);
        result.ValidationState.Should().Be(DocumentValidationState.Valid);
        result.ReadyToPublish.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that errors produce FixErrors recommendations and reduce the score.
    /// </summary>
    [Fact]
    public void CalculateDocumentHealth_WithErrors_PenalizesAndRecommends()
    {
        // Arrange — failing with 3 errors, 2 warnings
        var docId = Guid.NewGuid();
        var metrics = new List<WorkflowExecutionMetrics>
        {
            CreateExecution(success: false, totalErrors: 3, totalWarnings: 2, documentId: docId)
        };

        // Act
        var result = _sut.CalculateDocumentHealth(docId, metrics);

        // Assert
        // baseScore = 30, errorPenalty = min(30, 3*5) = 15, warningPenalty = min(20, 2*2) = 4
        // healthScore = 30 - 15 - 4 = 11
        result.HealthScore.Should().Be(11);
        result.ValidationState.Should().Be(DocumentValidationState.Invalid);
        result.ReadyToPublish.Should().BeFalse();
        result.Recommendations.Should().Contain(r =>
            r.Type == HealthRecommendationType.FixErrors);
        result.Recommendations.Should().Contain(r =>
            r.Type == HealthRecommendationType.ResolveWarnings);
    }

    /// <summary>
    /// Verifies that ValidWithWarnings state is returned when passing but with warnings.
    /// </summary>
    [Fact]
    public void CalculateDocumentHealth_PassWithWarnings_ReturnsValidWithWarnings()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var metrics = new List<WorkflowExecutionMetrics>
        {
            CreateExecution(success: true, totalErrors: 0, totalWarnings: 4, documentId: docId)
        };

        // Act
        var result = _sut.CalculateDocumentHealth(docId, metrics);

        // Assert
        result.ValidationState.Should().Be(DocumentValidationState.ValidWithWarnings);
        result.ReadyToPublish.Should().BeTrue();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static WorkflowExecutionMetrics CreateExecution(
        bool success,
        int totalErrors,
        int totalWarnings = 0,
        Guid? documentId = null) =>
        new()
        {
            ExecutionId = Guid.NewGuid(),
            WorkflowId = "test-workflow",
            WorkspaceId = Guid.NewGuid(),
            DocumentId = documentId ?? Guid.NewGuid(),
            Success = success,
            TotalErrors = totalErrors,
            TotalWarnings = totalWarnings,
            ExecutedAt = DateTime.UtcNow,
            Status = success ? MetricsExecutionStatus.Success : MetricsExecutionStatus.Failed
        };
}
