// -----------------------------------------------------------------------
// <copyright file="WorkflowMetricsDataTypeTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Workflows.Validation.Metrics;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for metrics data types: records, enums, and default values.
/// </summary>
/// <remarks>
/// <b>Specification:</b> LCS-DES-077-KG-i §3
/// </remarks>
[Trait("Category", "Workflows")]
[Trait("Version", "v0.7.7i")]
public class WorkflowMetricsDataTypeTests
{
    /// <summary>
    /// Verifies that <see cref="MetricsExecutionStatus"/> has all 5 values.
    /// </summary>
    [Fact]
    public void MetricsExecutionStatus_Has5Values()
    {
        var values = Enum.GetValues<MetricsExecutionStatus>();

        values.Should().HaveCount(5);
        values.Should().Contain(MetricsExecutionStatus.Success);
        values.Should().Contain(MetricsExecutionStatus.PartialSuccess);
        values.Should().Contain(MetricsExecutionStatus.Failed);
        values.Should().Contain(MetricsExecutionStatus.Cancelled);
        values.Should().Contain(MetricsExecutionStatus.TimedOut);
    }

    /// <summary>
    /// Verifies that <see cref="HealthTrend"/> has all 3 values.
    /// </summary>
    [Fact]
    public void HealthTrend_Has3Values()
    {
        var values = Enum.GetValues<HealthTrend>();

        values.Should().HaveCount(3);
        values.Should().Contain(HealthTrend.Improving);
        values.Should().Contain(HealthTrend.Stable);
        values.Should().Contain(HealthTrend.Declining);
    }

    /// <summary>
    /// Verifies that <see cref="DocumentValidationState"/> has all 5 values.
    /// </summary>
    [Fact]
    public void DocumentValidationState_Has5Values()
    {
        var values = Enum.GetValues<DocumentValidationState>();

        values.Should().HaveCount(5);
        values.Should().Contain(DocumentValidationState.Valid);
        values.Should().Contain(DocumentValidationState.ValidWithWarnings);
        values.Should().Contain(DocumentValidationState.Invalid);
        values.Should().Contain(DocumentValidationState.NotValidated);
        values.Should().Contain(DocumentValidationState.ValidationInProgress);
    }

    /// <summary>
    /// Verifies that <see cref="MetricsAggregation"/> has all 5 values.
    /// </summary>
    [Fact]
    public void MetricsAggregation_Has5Values()
    {
        var values = Enum.GetValues<MetricsAggregation>();

        values.Should().HaveCount(5);
        values.Should().Contain(MetricsAggregation.Raw);
        values.Should().Contain(MetricsAggregation.Hourly);
        values.Should().Contain(MetricsAggregation.Daily);
        values.Should().Contain(MetricsAggregation.Weekly);
        values.Should().Contain(MetricsAggregation.Monthly);
    }

    /// <summary>
    /// Verifies that <see cref="HealthRecommendationType"/> has all 6 values.
    /// </summary>
    [Fact]
    public void HealthRecommendationType_Has6Values()
    {
        var values = Enum.GetValues<HealthRecommendationType>();

        values.Should().HaveCount(6);
        values.Should().Contain(HealthRecommendationType.FixErrors);
        values.Should().Contain(HealthRecommendationType.ResolveWarnings);
        values.Should().Contain(HealthRecommendationType.ImproveConsistency);
        values.Should().Contain(HealthRecommendationType.UpdateReferences);
        values.Should().Contain(HealthRecommendationType.ReviewMetadata);
        values.Should().Contain(HealthRecommendationType.SyncWithGraph);
    }

    /// <summary>
    /// Verifies that <see cref="MetricsQuery"/> has correct defaults.
    /// </summary>
    [Fact]
    public void MetricsQuery_DefaultValues()
    {
        var query = new MetricsQuery { WorkspaceId = Guid.NewGuid() };

        query.Aggregation.Should().Be(MetricsAggregation.Daily);
        query.Limit.Should().Be(1000);
        query.StartTime.Should().BeNull();
        query.EndTime.Should().BeNull();
        query.DocumentId.Should().BeNull();
        query.WorkflowId.Should().BeNull();
        query.Trigger.Should().BeNull();
        query.ValidationType.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="MetricsReport"/> has correct defaults.
    /// </summary>
    [Fact]
    public void MetricsReport_DefaultCollections()
    {
        var report = new MetricsReport
        {
            Query = new MetricsQuery { WorkspaceId = Guid.NewGuid() }
        };

        report.WorkflowExecutions.Should().BeEmpty();
        report.ValidationSteps.Should().BeEmpty();
        report.GatingDecisions.Should().BeEmpty();
        report.SyncOperations.Should().BeEmpty();
        report.Statistics.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationStepMetrics"/> has correct defaults.
    /// </summary>
    [Fact]
    public void ValidationStepMetrics_DefaultCollections()
    {
        var metrics = new ValidationStepMetrics
        {
            StepId = "test",
            StepName = "Test"
        };

        metrics.ErrorCategories.Should().BeEmpty();
        metrics.Passed.Should().BeFalse();
        metrics.ErrorCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies that <see cref="DocumentHealthScore"/> has correct defaults.
    /// </summary>
    [Fact]
    public void DocumentHealthScore_DefaultCollections()
    {
        var score = new DocumentHealthScore
        {
            DocumentId = Guid.NewGuid()
        };

        score.CategoryScores.Should().BeEmpty();
        score.Recommendations.Should().BeEmpty();
        score.ReadyToPublish.Should().BeFalse();
        score.ValidationState.Should().Be(DocumentValidationState.Valid);
    }

    /// <summary>
    /// Verifies that <see cref="WorkspaceHealthScore"/> has correct defaults.
    /// </summary>
    [Fact]
    public void WorkspaceHealthScore_DefaultCollections()
    {
        var score = new WorkspaceHealthScore
        {
            WorkspaceId = Guid.NewGuid()
        };

        score.TopErrorTypes.Should().BeEmpty();
        score.Trend.Should().Be(HealthTrend.Improving);  // Default enum value = 0
        score.TrendValue.Should().Be(0);
    }
}
