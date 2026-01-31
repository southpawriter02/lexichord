# LCS-DES-077-KG-i: Workflow Metrics

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-077-KG-i |
| **System Breakdown** | LCS-SBD-077-KG |
| **Version** | v0.7.7 |
| **Codename** | Workflow Metrics (CKVS Phase 4d) |
| **Estimated Hours** | 4 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Workflow Metrics** module tracks validation outcomes, execution performance, and workspace health scores. It collects telemetry from workflow executions, aggregates results, and provides insights into document quality, validation trends, and system health over time.

### 1.2 Key Responsibilities

- Track validation outcomes (pass/fail, errors/warnings)
- Monitor workflow execution performance
- Calculate health scores per document and workspace
- Store metrics in time-series database
- Provide metrics aggregation and trending
- Support custom metric definitions
- Enable dashboard and reporting

### 1.3 Module Location

```
src/
  Lexichord.Workflows/
    Validation/
      Metrics/
        IWorkflowMetricsCollector.cs
        WorkflowMetricsCollector.cs
        IMetricsStore.cs
        HealthScoreCalculator.cs
        MetricsAggregator.cs
```

---

## 2. Interface Definitions

### 2.1 Workflow Metrics Collector Interface

```csharp
namespace Lexichord.Workflows.Validation.Metrics;

/// <summary>
/// Collects and tracks workflow execution metrics.
/// </summary>
public interface IWorkflowMetricsCollector
{
    /// <summary>
    /// Records workflow execution metrics.
    /// </summary>
    Task RecordWorkflowExecutionAsync(
        WorkflowExecutionMetrics metrics,
        CancellationToken ct = default);

    /// <summary>
    /// Records validation step metrics.
    /// </summary>
    Task RecordValidationStepAsync(
        ValidationStepMetrics metrics,
        CancellationToken ct = default);

    /// <summary>
    /// Records gating decision.
    /// </summary>
    Task RecordGatingDecisionAsync(
        GatingDecisionMetrics metrics,
        CancellationToken ct = default);

    /// <summary>
    /// Records sync operation metrics.
    /// </summary>
    Task RecordSyncOperationAsync(
        SyncOperationMetrics metrics,
        CancellationToken ct = default);

    /// <summary>
    /// Gets metrics for period.
    /// </summary>
    Task<MetricsReport> GetMetricsAsync(
        MetricsQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Gets workspace health score.
    /// </summary>
    Task<WorkspaceHealthScore> GetHealthScoreAsync(
        Guid workspaceId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets document health score.
    /// </summary>
    Task<DocumentHealthScore> GetDocumentHealthAsync(
        Guid documentId,
        CancellationToken ct = default);
}
```

---

## 3. Data Types

### 3.1 Workflow Execution Metrics

```csharp
/// <summary>
/// Metrics from workflow execution.
/// </summary>
public record WorkflowExecutionMetrics
{
    /// <summary>Execution ID.</summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>Workflow ID.</summary>
    public required string WorkflowId { get; init; }

    /// <summary>Workspace ID.</summary>
    public required Guid WorkspaceId { get; init; }

    /// <summary>Document ID.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Trigger that started workflow.</summary>
    public WorkflowTrigger Trigger { get; init; }

    /// <summary>Overall success.</summary>
    public bool Success { get; init; }

    /// <summary>Total execution time in milliseconds.</summary>
    public long TotalExecutionTimeMs { get; init; }

    /// <summary>Number of steps executed.</summary>
    public int StepsExecuted { get; init; }

    /// <summary>Number of steps failed.</summary>
    public int StepsFailed { get; init; }

    /// <summary>Total validation errors found.</summary>
    public int TotalErrors { get; init; }

    /// <summary>Total validation warnings found.</summary>
    public int TotalWarnings { get; init; }

    /// <summary>Gates that blocked workflow.</summary>
    public int GatesBlocked { get; init; }

    /// <summary>Gates that passed.</summary>
    public int GatesPassed { get; init; }

    /// <summary>Conflicts detected during sync.</summary>
    public int ConflictsDetected { get; init; }

    /// <summary>Conflicts resolved.</summary>
    public int ConflictsResolved { get; init; }

    /// <summary>User who triggered workflow (if applicable).</summary>
    public Guid? UserId { get; init; }

    /// <summary>Timestamp of execution.</summary>
    public DateTime ExecutedAt { get; init; }

    /// <summary>Execution status.</summary>
    public WorkflowExecutionStatus Status { get; init; }

    /// <summary>Additional metadata.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

public enum WorkflowExecutionStatus
{
    Success,
    PartialSuccess,
    Failed,
    Cancelled,
    TimedOut
}
```

### 3.2 Validation Step Metrics

```csharp
/// <summary>
/// Metrics from validation step.
/// </summary>
public record ValidationStepMetrics
{
    /// <summary>Execution ID.</summary>
    public Guid ExecutionId { get; init; }

    /// <summary>Step ID.</summary>
    public required string StepId { get; init; }

    /// <summary>Step name.</summary>
    public required string StepName { get; init; }

    /// <summary>Validation type.</summary>
    public ValidationStepType StepType { get; init; }

    /// <summary>Whether validation passed.</summary>
    public bool Passed { get; init; }

    /// <summary>Execution time in milliseconds.</summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>Number of errors found.</summary>
    public int ErrorCount { get; init; }

    /// <summary>Number of warnings found.</summary>
    public int WarningCount { get; init; }

    /// <summary>Number of items validated.</summary>
    public int ItemsValidated { get; init; }

    /// <summary>Number of items with issues.</summary>
    public int ItemsWithIssues { get; init; }

    /// <summary>Rules executed.</summary>
    public int RulesExecuted { get; init; }

    /// <summary>Rules that triggered failures.</summary>
    public int RulesTriggered { get; init; }

    /// <summary>Timestamp of step execution.</summary>
    public DateTime ExecutedAt { get; init; }

    /// <summary>Error categories found.</summary>
    public IReadOnlyList<string> ErrorCategories { get; init; } = [];
}
```

### 3.3 Workspace Health Score

```csharp
/// <summary>
/// Overall health of workspace.
/// </summary>
public record WorkspaceHealthScore
{
    /// <summary>Workspace ID.</summary>
    public required Guid WorkspaceId { get; init; }

    /// <summary>Overall health score (0-100).</summary>
    public int HealthScore { get; init; }

    /// <summary>Number of documents with validation errors.</summary>
    public int DocumentsWithErrors { get; init; }

    /// <summary>Percentage of documents passing validation.</summary>
    public decimal PassRate { get; init; }

    /// <summary>Average errors per document.</summary>
    public decimal AvgErrorsPerDocument { get; init; }

    /// <summary>Average warnings per document.</summary>
    public decimal AvgWarningsPerDocument { get; init; }

    /// <summary>Most common error types.</summary>
    public IReadOnlyList<ErrorTypeFrequency> TopErrorTypes { get; init; } = [];

    /// <summary>Documents needing attention.</summary>
    public int DocumentsNeedingAttention { get; init; }

    /// <summary>Last validation run.</summary>
    public DateTime? LastValidationRun { get; init; }

    /// <summary>Timestamp of score calculation.</summary>
    public DateTime CalculatedAt { get; init; }

    /// <summary>Score trend (up, stable, down).</summary>
    public HealthTrend Trend { get; init; }

    /// <summary>Score change from previous period.</summary>
    public int TrendValue { get; init; }
}

public record ErrorTypeFrequency
{
    public required string ErrorType { get; init; }
    public int Count { get; init; }
    public decimal Percentage { get; init; }
}

public enum HealthTrend
{
    Improving,
    Stable,
    Declining
}
```

### 3.4 Document Health Score

```csharp
/// <summary>
/// Health of individual document.
/// </summary>
public record DocumentHealthScore
{
    /// <summary>Document ID.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Document title.</summary>
    public string? DocumentTitle { get; init; }

    /// <summary>Overall health score (0-100).</summary>
    public int HealthScore { get; init; }

    /// <summary>Current validation state.</summary>
    public DocumentValidationState ValidationState { get; init; }

    /// <summary>Number of unresolved errors.</summary>
    public int UnresolvedErrors { get; init; }

    /// <summary>Number of warnings.</summary>
    public int Warnings { get; init; }

    /// <summary>Validation categories with issues.</summary>
    public IReadOnlyList<ValidationCategoryScore> CategoryScores { get; init; } = [];

    /// <summary>Last validation timestamp.</summary>
    public DateTime? LastValidated { get; init; }

    /// <summary>Last modified timestamp.</summary>
    public DateTime? LastModified { get; init; }

    /// <summary>Whether document is ready to publish.</summary>
    public bool ReadyToPublish { get; init; }

    /// <summary>Recommended actions.</summary>
    public IReadOnlyList<HealthRecommendation> Recommendations { get; init; } = [];

    /// <summary>Score calculated at.</summary>
    public DateTime CalculatedAt { get; init; }
}

public enum DocumentValidationState
{
    Valid,
    ValidWithWarnings,
    Invalid,
    NotValidated,
    ValidationInProgress
}

public record ValidationCategoryScore
{
    public required string Category { get; init; }
    public int Score { get; init; }
    public int Issues { get; init; }
}

public record HealthRecommendation
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public HealthRecommendationType Type { get; init; }
    public int Priority { get; init; }
}

public enum HealthRecommendationType
{
    FixErrors,
    ResolveWarnings,
    ImproveConsistency,
    UpdateReferences,
    ReviewMetadata,
    SyncWithGraph
}
```

### 3.5 Metrics Query

```csharp
/// <summary>
/// Query for metrics retrieval.
/// </summary>
public record MetricsQuery
{
    /// <summary>Workspace ID (required).</summary>
    public required Guid WorkspaceId { get; init; }

    /// <summary>Start of time range.</summary>
    public DateTime? StartTime { get; init; }

    /// <summary>End of time range.</summary>
    public DateTime? EndTime { get; init; }

    /// <summary>Filter by document ID.</summary>
    public Guid? DocumentId { get; init; }

    /// <summary>Filter by workflow ID.</summary>
    public string? WorkflowId { get; init; }

    /// <summary>Filter by trigger type.</summary>
    public WorkflowTrigger? Trigger { get; init; }

    /// <summary>Filter by validation type.</summary>
    public ValidationStepType? ValidationType { get; init; }

    /// <summary>Aggregation granularity.</summary>
    public MetricsAggregation Aggregation { get; init; } = MetricsAggregation.Daily;

    /// <summary>Maximum results to return.</summary>
    public int? Limit { get; init; } = 1000;
}

public enum MetricsAggregation
{
    Raw,
    Hourly,
    Daily,
    Weekly,
    Monthly
}
```

### 3.6 Metrics Report

```csharp
/// <summary>
/// Report of metrics for period.
/// </summary>
public record MetricsReport
{
    /// <summary>Query that generated report.</summary>
    public required MetricsQuery Query { get; init; }

    /// <summary>Execution metrics.</summary>
    public IReadOnlyList<WorkflowExecutionMetrics> WorkflowExecutions { get; init; } = [];

    /// <summary>Step-level metrics.</summary>
    public IReadOnlyList<ValidationStepMetrics> ValidationSteps { get; init; } = [];

    /// <summary>Gating decisions.</summary>
    public IReadOnlyList<GatingDecisionMetrics> GatingDecisions { get; init; } = [];

    /// <summary>Sync operations.</summary>
    public IReadOnlyList<SyncOperationMetrics> SyncOperations { get; init; } = [];

    /// <summary>Aggregated statistics.</summary>
    public MetricsStatistics Statistics { get; init; }

    /// <summary>Generated at.</summary>
    public DateTime GeneratedAt { get; init; }
}

public record MetricsStatistics
{
    /// <summary>Total workflow executions.</summary>
    public int TotalExecutions { get; init; }

    /// <summary>Successful executions.</summary>
    public int SuccessfulExecutions { get; init; }

    /// <summary>Failed executions.</summary>
    public int FailedExecutions { get; init; }

    /// <summary>Success rate percentage.</summary>
    public decimal SuccessRate { get; init; }

    /// <summary>Average execution time.</summary>
    public double AvgExecutionTimeMs { get; init; }

    /// <summary>Total errors across period.</summary>
    public int TotalErrors { get; init; }

    /// <summary>Total warnings across period.</summary>
    public int TotalWarnings { get; init; }

    /// <summary>Average errors per execution.</summary>
    public decimal AvgErrorsPerExecution { get; init; }

    /// <summary>Documents with errors.</summary>
    public int DocumentsWithErrors { get; init; }

    /// <summary>Documents passed validation.</summary>
    public int DocumentsPassedValidation { get; init; }
}
```

---

## 4. Implementation

### 4.1 Workflow Metrics Collector

```csharp
public class WorkflowMetricsCollector : IWorkflowMetricsCollector
{
    private readonly IMetricsStore _store;
    private readonly IHealthScoreCalculator _healthCalculator;
    private readonly ILogger<WorkflowMetricsCollector> _logger;

    public WorkflowMetricsCollector(
        IMetricsStore store,
        IHealthScoreCalculator healthCalculator,
        ILogger<WorkflowMetricsCollector> logger)
    {
        _store = store;
        _healthCalculator = healthCalculator;
        _logger = logger;
    }

    public async Task RecordWorkflowExecutionAsync(
        WorkflowExecutionMetrics metrics,
        CancellationToken ct = default)
    {
        try
        {
            await _store.StoreWorkflowExecutionAsync(metrics, ct);

            _logger.LogInformation(
                "Recorded workflow execution: {WorkflowId} {Status}",
                metrics.WorkflowId, metrics.Status);

            // Trigger health score recalculation
            _ = Task.Run(
                async () => await RecalculateHealthAsync(
                    metrics.WorkspaceId,
                    metrics.DocumentId,
                    ct),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record workflow execution metrics");
        }
    }

    public async Task RecordValidationStepAsync(
        ValidationStepMetrics metrics,
        CancellationToken ct = default)
    {
        try
        {
            await _store.StoreValidationStepAsync(metrics, ct);

            _logger.LogDebug(
                "Recorded validation step: {StepId} ({StepType})",
                metrics.StepId, metrics.StepType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record validation step metrics");
        }
    }

    public async Task RecordGatingDecisionAsync(
        GatingDecisionMetrics metrics,
        CancellationToken ct = default)
    {
        try
        {
            await _store.StoreGatingDecisionAsync(metrics, ct);

            _logger.LogDebug(
                "Recorded gating decision: {GateId} ({Decision})",
                metrics.GateId, metrics.Passed ? "PASS" : "BLOCK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record gating decision metrics");
        }
    }

    public async Task RecordSyncOperationAsync(
        SyncOperationMetrics metrics,
        CancellationToken ct = default)
    {
        try
        {
            await _store.StoreSyncOperationAsync(metrics, ct);

            _logger.LogDebug(
                "Recorded sync operation: {Direction} {Status}",
                metrics.Direction, metrics.Success ? "SUCCESS" : "FAILED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record sync operation metrics");
        }
    }

    public async Task<MetricsReport> GetMetricsAsync(
        MetricsQuery query,
        CancellationToken ct = default)
    {
        try
        {
            var executions = await _store.GetWorkflowExecutionsAsync(query, ct);
            var steps = await _store.GetValidationStepsAsync(query, ct);
            var gatingDecisions = await _store.GetGatingDecisionsAsync(query, ct);
            var syncOps = await _store.GetSyncOperationsAsync(query, ct);

            var statistics = CalculateStatistics(executions, steps, gatingDecisions);

            return new MetricsReport
            {
                Query = query,
                WorkflowExecutions = executions,
                ValidationSteps = steps,
                GatingDecisions = gatingDecisions,
                SyncOperations = syncOps,
                Statistics = statistics,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate metrics report");
            throw;
        }
    }

    public async Task<WorkspaceHealthScore> GetHealthScoreAsync(
        Guid workspaceId,
        CancellationToken ct = default)
    {
        try
        {
            var recentMetrics = await _store.GetWorkflowExecutionsAsync(
                new MetricsQuery
                {
                    WorkspaceId = workspaceId,
                    StartTime = DateTime.UtcNow.AddDays(-7),
                    EndTime = DateTime.UtcNow
                },
                ct);

            var health = _healthCalculator.CalculateWorkspaceHealth(recentMetrics);

            _logger.LogInformation(
                "Calculated workspace health: {WorkspaceId} = {Score}",
                workspaceId, health.HealthScore);

            return health;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate workspace health");
            throw;
        }
    }

    public async Task<DocumentHealthScore> GetDocumentHealthAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        try
        {
            var recentMetrics = await _store.GetDocumentMetricsAsync(documentId, ct);

            var health = _healthCalculator.CalculateDocumentHealth(documentId, recentMetrics);

            _logger.LogInformation(
                "Calculated document health: {DocumentId} = {Score}",
                documentId, health.HealthScore);

            return health;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate document health");
            throw;
        }
    }

    private async Task RecalculateHealthAsync(
        Guid workspaceId,
        Guid documentId,
        CancellationToken ct)
    {
        try
        {
            var workspaceHealth = await GetHealthScoreAsync(workspaceId, ct);
            var docHealth = await GetDocumentHealthAsync(documentId, ct);

            // Store calculated scores
            await _store.StoreHealthScoresAsync(workspaceHealth, docHealth, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recalculate health scores");
        }
    }

    private MetricsStatistics CalculateStatistics(
        IReadOnlyList<WorkflowExecutionMetrics> executions,
        IReadOnlyList<ValidationStepMetrics> steps,
        IReadOnlyList<GatingDecisionMetrics> gatingDecisions)
    {
        var successful = executions.Count(e => e.Success);
        var failed = executions.Count(e => !e.Success);
        var totalExecutions = executions.Count;

        return new MetricsStatistics
        {
            TotalExecutions = totalExecutions,
            SuccessfulExecutions = successful,
            FailedExecutions = failed,
            SuccessRate = totalExecutions > 0
                ? (decimal)successful / totalExecutions * 100
                : 0,
            AvgExecutionTimeMs = executions.Count > 0
                ? executions.Average(e => e.TotalExecutionTimeMs)
                : 0,
            TotalErrors = executions.Sum(e => e.TotalErrors),
            TotalWarnings = executions.Sum(e => e.TotalWarnings),
            AvgErrorsPerExecution = totalExecutions > 0
                ? (decimal)executions.Sum(e => e.TotalErrors) / totalExecutions
                : 0,
            DocumentsWithErrors = executions
                .Where(e => e.TotalErrors > 0)
                .Select(e => e.DocumentId)
                .Distinct()
                .Count(),
            DocumentsPassedValidation = executions
                .Where(e => e.Success)
                .Select(e => e.DocumentId)
                .Distinct()
                .Count()
        };
    }
}
```

### 4.2 Health Score Calculator

```csharp
public interface IHealthScoreCalculator
{
    WorkspaceHealthScore CalculateWorkspaceHealth(
        IReadOnlyList<WorkflowExecutionMetrics> recentMetrics);

    DocumentHealthScore CalculateDocumentHealth(
        Guid documentId,
        IReadOnlyList<WorkflowExecutionMetrics> documentMetrics);
}

public class HealthScoreCalculator : IHealthScoreCalculator
{
    public WorkspaceHealthScore CalculateWorkspaceHealth(
        IReadOnlyList<WorkflowExecutionMetrics> recentMetrics)
    {
        if (!recentMetrics.Any())
        {
            return new WorkspaceHealthScore
            {
                WorkspaceId = Guid.Empty,
                HealthScore = 0,
                PassRate = 0,
                CalculatedAt = DateTime.UtcNow,
                Trend = HealthTrend.Stable
            };
        }

        var passRate = (decimal)recentMetrics.Count(m => m.Success) / recentMetrics.Count;
        var totalErrors = recentMetrics.Sum(m => m.TotalErrors);
        var totalWarnings = recentMetrics.Sum(m => m.TotalWarnings);
        var docCount = recentMetrics.Select(m => m.DocumentId).Distinct().Count();

        // Health score formula:
        // Base: PassRate (0-80 points) + ErrorFreedom (0-20 points)
        var baseScore = (int)(passRate * 80);
        var errorFreedom = totalErrors == 0 ? 20 : Math.Max(0, 20 - (totalErrors / 10));
        var healthScore = Math.Max(0, Math.Min(100, baseScore + (int)errorFreedom));

        var errorTypes = recentMetrics
            .SelectMany(m => m.Metadata?["errorCategories"] as IReadOnlyList<string> ?? new List<string>())
            .GroupBy(e => e)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new ErrorTypeFrequency
            {
                ErrorType = g.Key,
                Count = g.Count(),
                Percentage = (decimal)g.Count() / recentMetrics.Sum(m => m.TotalErrors) * 100
            })
            .ToList();

        return new WorkspaceHealthScore
        {
            WorkspaceId = recentMetrics.First().WorkspaceId,
            HealthScore = healthScore,
            PassRate = passRate,
            DocumentsWithErrors = recentMetrics.Count(m => m.TotalErrors > 0),
            AvgErrorsPerDocument = docCount > 0
                ? (decimal)totalErrors / docCount
                : 0,
            AvgWarningsPerDocument = docCount > 0
                ? (decimal)totalWarnings / docCount
                : 0,
            TopErrorTypes = errorTypes,
            DocumentsNeedingAttention = recentMetrics.Count(m => m.TotalErrors > 0 || m.TotalWarnings > 5),
            LastValidationRun = recentMetrics.Max(m => m.ExecutedAt),
            CalculatedAt = DateTime.UtcNow,
            Trend = HealthTrend.Stable
        };
    }

    public DocumentHealthScore CalculateDocumentHealth(
        Guid documentId,
        IReadOnlyList<WorkflowExecutionMetrics> documentMetrics)
    {
        if (!documentMetrics.Any())
        {
            return new DocumentHealthScore
            {
                DocumentId = documentId,
                HealthScore = 0,
                ValidationState = DocumentValidationState.NotValidated,
                CalculatedAt = DateTime.UtcNow
            };
        }

        var lastMetric = documentMetrics.OrderByDescending(m => m.ExecutedAt).First();

        var baseScore = lastMetric.Success ? 80 : 30;
        var errorPenalty = Math.Min(30, lastMetric.TotalErrors * 5);
        var warningPenalty = Math.Min(20, lastMetric.TotalWarnings * 2);
        var healthScore = Math.Max(0, Math.Min(100, baseScore - errorPenalty - warningPenalty));

        var state = lastMetric.TotalErrors == 0
            ? (lastMetric.TotalWarnings == 0
                ? DocumentValidationState.Valid
                : DocumentValidationState.ValidWithWarnings)
            : DocumentValidationState.Invalid;

        var recommendations = new List<HealthRecommendation>();
        if (lastMetric.TotalErrors > 0)
        {
            recommendations.Add(new HealthRecommendation
            {
                Title = "Fix Validation Errors",
                Description = $"{lastMetric.TotalErrors} validation errors must be resolved",
                Type = HealthRecommendationType.FixErrors,
                Priority = 1
            });
        }

        if (lastMetric.TotalWarnings > 0)
        {
            recommendations.Add(new HealthRecommendation
            {
                Title = "Review Warnings",
                Description = $"{lastMetric.TotalWarnings} warnings to review",
                Type = HealthRecommendationType.ResolveWarnings,
                Priority = 2
            });
        }

        return new DocumentHealthScore
        {
            DocumentId = documentId,
            HealthScore = healthScore,
            ValidationState = state,
            UnresolvedErrors = lastMetric.TotalErrors,
            Warnings = lastMetric.TotalWarnings,
            LastValidated = lastMetric.ExecutedAt,
            ReadyToPublish = lastMetric.Success && lastMetric.TotalErrors == 0,
            Recommendations = recommendations,
            CalculatedAt = DateTime.UtcNow
        };
    }
}
```

---

## 5. Metrics Collection Flow

```
[Workflow Execution Completes]
        |
        v
[Record WorkflowExecutionMetrics]
        |
        +---> [For Each Step]
        |     +---> [Record ValidationStepMetrics]
        |     +---> [Record GatingDecisionMetrics]
        |     +---> [Record SyncOperationMetrics]
        |
        +---> [Store in MetricsStore]
        |
        +---> [Trigger HealthScore Recalculation]
        |     +---> [Calculate Workspace Score]
        |     +---> [Calculate Document Score]
        |     +---> [Store Scores]
        |
        +---> [Available for Reporting]
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Store unavailable | Log error, continue workflow |
| Metrics retrieval fails | Return partial report |
| Health calculation fails | Return zero score |
| Division by zero | Handle edge cases |

---

## 7. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `RecordWorkflowExecution` | Metrics stored |
| `GetMetricsReport` | Report generated |
| `CalculateWorkspaceHealth` | Score calculated |
| `CalculateDocumentHealth` | Document score calculated |
| `HealthScore_PassingDocs` | Score reflects pass rate |
| `HealthScore_ErrorPenalty` | Errors reduce score |
| `HealthScore_Trend` | Trend calculated |
| `GetMetrics_TimeRange` | Time filtering works |
| `GetMetrics_Aggregation` | Aggregation works |

---

## 8. Performance Considerations

| Aspect | Target |
| :------ | :------ |
| Record metrics | < 100ms |
| Health calculation | < 500ms |
| Report generation | < 2s |
| Metrics storage | < 50ms |
| Query metrics | < 1s |

---

## 9. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | Basic metrics only |
| Teams | Full metrics collection + reporting |
| Enterprise | Full + custom metrics + analytics |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
