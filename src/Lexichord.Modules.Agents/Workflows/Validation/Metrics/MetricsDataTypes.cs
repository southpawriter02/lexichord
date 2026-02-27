// -----------------------------------------------------------------------
// <copyright file="MetricsDataTypes.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Workflows.Validation.Templates;

namespace Lexichord.Modules.Agents.Workflows.Validation.Metrics;

// ═══════════════════════════════════════════════════════════════════════
// §3.1 — Workflow Execution Metrics
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Metrics collected from a complete workflow execution.
/// </summary>
/// <remarks>
/// <para>
/// Captures outcome, timing, error/warning counts, gating decisions, and sync
/// operations for a single workflow run. Stored by <see cref="IMetricsStore"/>
/// and aggregated by <see cref="IWorkflowMetricsCollector.GetMetricsAsync"/>.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.1
/// </para>
/// </remarks>
public record WorkflowExecutionMetrics
{
    /// <summary>Unique execution identifier.</summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>Workflow definition ID that was executed.</summary>
    public required string WorkflowId { get; init; }

    /// <summary>Workspace in which the workflow was executed.</summary>
    public required Guid WorkspaceId { get; init; }

    /// <summary>Document that was validated.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Trigger that initiated the workflow execution.</summary>
    public ValidationWorkflowTrigger Trigger { get; init; }

    /// <summary>Whether the overall workflow execution succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Total execution time in milliseconds.</summary>
    public long TotalExecutionTimeMs { get; init; }

    /// <summary>Number of steps that were executed.</summary>
    public int StepsExecuted { get; init; }

    /// <summary>Number of steps that failed.</summary>
    public int StepsFailed { get; init; }

    /// <summary>Total validation errors found across all steps.</summary>
    public int TotalErrors { get; init; }

    /// <summary>Total validation warnings found across all steps.</summary>
    public int TotalWarnings { get; init; }

    /// <summary>Number of gating steps that blocked further execution.</summary>
    public int GatesBlocked { get; init; }

    /// <summary>Number of gating steps that passed.</summary>
    public int GatesPassed { get; init; }

    /// <summary>Number of conflicts detected during sync operations.</summary>
    public int ConflictsDetected { get; init; }

    /// <summary>Number of conflicts that were automatically resolved.</summary>
    public int ConflictsResolved { get; init; }

    /// <summary>User who triggered the workflow (null for automated triggers).</summary>
    public Guid? UserId { get; init; }

    /// <summary>UTC timestamp of when the execution occurred.</summary>
    public DateTime ExecutedAt { get; init; }

    /// <summary>Final execution status.</summary>
    public MetricsExecutionStatus Status { get; init; }

    /// <summary>Additional metadata key-value pairs for extensibility.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Status of a workflow execution as recorded in metrics.
/// </summary>
/// <remarks>
/// <para>
/// This enum is separate from <c>WorkflowExecutionStatus</c> (v0.7.7b) which
/// tracks runtime lifecycle states (Pending/Running). This enum captures the
/// final outcome for metrics and reporting purposes only.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.1
/// </para>
/// </remarks>
public enum MetricsExecutionStatus
{
    /// <summary>All steps completed successfully.</summary>
    Success,

    /// <summary>Some steps completed, some failed (StopOnFirstFailure=false).</summary>
    PartialSuccess,

    /// <summary>One or more steps failed and execution was stopped.</summary>
    Failed,

    /// <summary>Execution was cancelled by the user.</summary>
    Cancelled,

    /// <summary>Execution exceeded its timeout limit.</summary>
    TimedOut
}

// ═══════════════════════════════════════════════════════════════════════
// §3.2 — Validation Step Metrics
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Metrics collected from a single validation step execution.
/// </summary>
/// <remarks>
/// <para>
/// Captures pass/fail status, timing, and error/warning details for one
/// validation step within a workflow. Linked to the parent execution via
/// <see cref="ExecutionId"/>.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.2
/// </para>
/// </remarks>
public record ValidationStepMetrics
{
    /// <summary>Parent execution identifier.</summary>
    public Guid ExecutionId { get; init; }

    /// <summary>Unique step identifier within the workflow.</summary>
    public required string StepId { get; init; }

    /// <summary>Human-readable step name.</summary>
    public required string StepName { get; init; }

    /// <summary>Validation step type (Schema, Consistency, etc.).</summary>
    public ValidationStepType StepType { get; init; }

    /// <summary>Whether the validation step passed.</summary>
    public bool Passed { get; init; }

    /// <summary>Execution time in milliseconds.</summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>Number of validation errors found.</summary>
    public int ErrorCount { get; init; }

    /// <summary>Number of validation warnings found.</summary>
    public int WarningCount { get; init; }

    /// <summary>Number of items (documents, sections, etc.) validated.</summary>
    public int ItemsValidated { get; init; }

    /// <summary>Number of items that had at least one issue.</summary>
    public int ItemsWithIssues { get; init; }

    /// <summary>Number of validation rules executed.</summary>
    public int RulesExecuted { get; init; }

    /// <summary>Number of rules that triggered failures.</summary>
    public int RulesTriggered { get; init; }

    /// <summary>UTC timestamp of step execution.</summary>
    public DateTime ExecutedAt { get; init; }

    /// <summary>Categorized error types found during validation.</summary>
    public IReadOnlyList<string> ErrorCategories { get; init; } = [];
}

// ═══════════════════════════════════════════════════════════════════════
// §3.3 — Gating Decision Metrics
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Metrics from a gating step decision.
/// </summary>
/// <remarks>
/// <para>
/// Records whether a gating condition passed or blocked workflow execution,
/// the condition expression evaluated, and timing information.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.6 (referenced in §2.1)
/// </para>
/// </remarks>
public record GatingDecisionMetrics
{
    /// <summary>Parent execution identifier.</summary>
    public Guid ExecutionId { get; init; }

    /// <summary>Unique gate identifier within the workflow.</summary>
    public required string GateId { get; init; }

    /// <summary>Human-readable gate name.</summary>
    public required string GateName { get; init; }

    /// <summary>Whether the gating condition passed.</summary>
    public bool Passed { get; init; }

    /// <summary>The condition expression that was evaluated.</summary>
    public string? ConditionExpression { get; init; }

    /// <summary>Time taken to evaluate the condition in milliseconds.</summary>
    public long EvaluationTimeMs { get; init; }

    /// <summary>UTC timestamp of the decision.</summary>
    public DateTime ExecutedAt { get; init; }
}

// ═══════════════════════════════════════════════════════════════════════
// §3.4 — Sync Operation Metrics
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Metrics from a sync workflow step.
/// </summary>
/// <remarks>
/// <para>
/// Tracks document-to-knowledge-graph synchronization outcomes including
/// item counts, conflicts, and resolution results.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.6 (referenced in §2.1)
/// </para>
/// </remarks>
public record SyncOperationMetrics
{
    /// <summary>Parent execution identifier.</summary>
    public Guid ExecutionId { get; init; }

    /// <summary>Sync direction (e.g., "DocumentToGraph", "GraphToDocument").</summary>
    public required string Direction { get; init; }

    /// <summary>Whether the sync operation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Number of items synchronized.</summary>
    public int ItemsSynced { get; init; }

    /// <summary>Number of conflicts detected.</summary>
    public int ConflictsDetected { get; init; }

    /// <summary>Number of conflicts resolved.</summary>
    public int ConflictsResolved { get; init; }

    /// <summary>Execution time in milliseconds.</summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>UTC timestamp of the sync operation.</summary>
    public DateTime ExecutedAt { get; init; }
}

// ═══════════════════════════════════════════════════════════════════════
// §3.3 — Workspace Health Score
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Overall health assessment of a workspace based on recent validation results.
/// </summary>
/// <remarks>
/// <para>
/// Calculated by <see cref="IHealthScoreCalculator.CalculateWorkspaceHealth"/>
/// from recent <see cref="WorkflowExecutionMetrics"/>. The score ranges from
/// 0 (critical issues) to 100 (perfect health).
/// </para>
/// <para>
/// <b>Health score formula:</b> PassRate × 80 + ErrorFreedom (0–20)
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.3
/// </para>
/// </remarks>
public record WorkspaceHealthScore
{
    /// <summary>Workspace identifier.</summary>
    public required Guid WorkspaceId { get; init; }

    /// <summary>Overall health score (0–100).</summary>
    public int HealthScore { get; init; }

    /// <summary>Number of documents that have validation errors.</summary>
    public int DocumentsWithErrors { get; init; }

    /// <summary>Percentage of documents passing validation (0.0–1.0).</summary>
    public decimal PassRate { get; init; }

    /// <summary>Average errors per document in the evaluation period.</summary>
    public decimal AvgErrorsPerDocument { get; init; }

    /// <summary>Average warnings per document in the evaluation period.</summary>
    public decimal AvgWarningsPerDocument { get; init; }

    /// <summary>Most frequently occurring error types (top 5).</summary>
    public IReadOnlyList<ErrorTypeFrequency> TopErrorTypes { get; init; } = [];

    /// <summary>Number of documents requiring manual attention.</summary>
    public int DocumentsNeedingAttention { get; init; }

    /// <summary>UTC timestamp of the last validation run.</summary>
    public DateTime? LastValidationRun { get; init; }

    /// <summary>UTC timestamp when this score was calculated.</summary>
    public DateTime CalculatedAt { get; init; }

    /// <summary>Direction of health score change since previous period.</summary>
    public HealthTrend Trend { get; init; }

    /// <summary>Numeric change from the previous period's score.</summary>
    public int TrendValue { get; init; }
}

/// <summary>
/// Frequency of a specific error type in the workspace.
/// </summary>
/// <remarks>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.3
/// </remarks>
public record ErrorTypeFrequency
{
    /// <summary>The error type/category identifier.</summary>
    public required string ErrorType { get; init; }

    /// <summary>Number of occurrences in the evaluation period.</summary>
    public int Count { get; init; }

    /// <summary>Percentage of total errors this type represents.</summary>
    public decimal Percentage { get; init; }
}

/// <summary>
/// Direction of health score trend over time.
/// </summary>
/// <remarks>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.3
/// </remarks>
public enum HealthTrend
{
    /// <summary>Score is increasing compared to the previous period.</summary>
    Improving,

    /// <summary>Score is unchanged or within noise margin.</summary>
    Stable,

    /// <summary>Score is decreasing compared to the previous period.</summary>
    Declining
}

// ═══════════════════════════════════════════════════════════════════════
// §3.4 — Document Health Score
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Health assessment of an individual document based on validation results.
/// </summary>
/// <remarks>
/// <para>
/// Calculated by <see cref="IHealthScoreCalculator.CalculateDocumentHealth"/>.
/// Based on the most recent validation run, providing actionable recommendations.
/// </para>
/// <para>
/// <b>Health score formula:</b> BaseScore (80 if pass, 30 if fail) − ErrorPenalty − WarningPenalty
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.4
/// </para>
/// </remarks>
public record DocumentHealthScore
{
    /// <summary>Document identifier.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Document title (if available).</summary>
    public string? DocumentTitle { get; init; }

    /// <summary>Overall health score (0–100).</summary>
    public int HealthScore { get; init; }

    /// <summary>Current validation state of the document.</summary>
    public DocumentValidationState ValidationState { get; init; }

    /// <summary>Number of unresolved validation errors.</summary>
    public int UnresolvedErrors { get; init; }

    /// <summary>Number of active warnings.</summary>
    public int Warnings { get; init; }

    /// <summary>Per-category validation scores.</summary>
    public IReadOnlyList<ValidationCategoryScore> CategoryScores { get; init; } = [];

    /// <summary>UTC timestamp of the most recent validation.</summary>
    public DateTime? LastValidated { get; init; }

    /// <summary>UTC timestamp of the document's last modification.</summary>
    public DateTime? LastModified { get; init; }

    /// <summary>Whether the document meets all criteria for publication.</summary>
    public bool ReadyToPublish { get; init; }

    /// <summary>Actionable recommendations for improving document health.</summary>
    public IReadOnlyList<HealthRecommendation> Recommendations { get; init; } = [];

    /// <summary>UTC timestamp when this score was calculated.</summary>
    public DateTime CalculatedAt { get; init; }
}

/// <summary>
/// Validation state of a document.
/// </summary>
/// <remarks>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.4
/// </remarks>
public enum DocumentValidationState
{
    /// <summary>All validations passed with no errors or warnings.</summary>
    Valid,

    /// <summary>All validations passed but warnings exist.</summary>
    ValidWithWarnings,

    /// <summary>One or more validations failed.</summary>
    Invalid,

    /// <summary>Document has never been validated.</summary>
    NotValidated,

    /// <summary>Validation is currently in progress.</summary>
    ValidationInProgress
}

/// <summary>
/// Health score for a specific validation category.
/// </summary>
/// <remarks>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.4
/// </remarks>
public record ValidationCategoryScore
{
    /// <summary>Category name (e.g., "Schema", "Consistency").</summary>
    public required string Category { get; init; }

    /// <summary>Category-level health score (0–100).</summary>
    public int Score { get; init; }

    /// <summary>Number of issues in this category.</summary>
    public int Issues { get; init; }
}

/// <summary>
/// Actionable recommendation for improving document or workspace health.
/// </summary>
/// <remarks>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.4
/// </remarks>
public record HealthRecommendation
{
    /// <summary>Short title describing the recommended action.</summary>
    public required string Title { get; init; }

    /// <summary>Detailed description of what to do and why.</summary>
    public string? Description { get; init; }

    /// <summary>Category of the recommendation.</summary>
    public HealthRecommendationType Type { get; init; }

    /// <summary>Priority (1 = highest, larger = lower priority).</summary>
    public int Priority { get; init; }
}

/// <summary>
/// Categories of health improvement recommendations.
/// </summary>
/// <remarks>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.4
/// </remarks>
public enum HealthRecommendationType
{
    /// <summary>Fix validation errors to improve health score.</summary>
    FixErrors,

    /// <summary>Resolve warnings to achieve clean validation.</summary>
    ResolveWarnings,

    /// <summary>Improve consistency across documents.</summary>
    ImproveConsistency,

    /// <summary>Update broken or outdated references.</summary>
    UpdateReferences,

    /// <summary>Review and update document metadata.</summary>
    ReviewMetadata,

    /// <summary>Synchronize document with the knowledge graph.</summary>
    SyncWithGraph
}

// ═══════════════════════════════════════════════════════════════════════
// §3.5 — Metrics Query
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Query parameters for metrics retrieval and filtering.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="IWorkflowMetricsCollector.GetMetricsAsync"/> to filter
/// stored metrics by workspace, time range, document, workflow, and trigger type.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.5
/// </para>
/// </remarks>
public record MetricsQuery
{
    /// <summary>Workspace ID (required — all metrics are workspace-scoped).</summary>
    public required Guid WorkspaceId { get; init; }

    /// <summary>Start of the time range (inclusive). Null = no lower bound.</summary>
    public DateTime? StartTime { get; init; }

    /// <summary>End of the time range (inclusive). Null = no upper bound.</summary>
    public DateTime? EndTime { get; init; }

    /// <summary>Filter by specific document. Null = all documents.</summary>
    public Guid? DocumentId { get; init; }

    /// <summary>Filter by specific workflow ID. Null = all workflows.</summary>
    public string? WorkflowId { get; init; }

    /// <summary>Filter by trigger type. Null = all triggers.</summary>
    public ValidationWorkflowTrigger? Trigger { get; init; }

    /// <summary>Filter by validation step type. Null = all types.</summary>
    public ValidationStepType? ValidationType { get; init; }

    /// <summary>Aggregation granularity for time-series data.</summary>
    public MetricsAggregation Aggregation { get; init; } = MetricsAggregation.Daily;

    /// <summary>Maximum number of results to return.</summary>
    public int? Limit { get; init; } = 1000;
}

/// <summary>
/// Aggregation granularity for metrics time-series data.
/// </summary>
/// <remarks>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.5
/// </remarks>
public enum MetricsAggregation
{
    /// <summary>Return raw, unaggregated data points.</summary>
    Raw,

    /// <summary>Aggregate by hour.</summary>
    Hourly,

    /// <summary>Aggregate by day (default).</summary>
    Daily,

    /// <summary>Aggregate by week.</summary>
    Weekly,

    /// <summary>Aggregate by month.</summary>
    Monthly
}

// ═══════════════════════════════════════════════════════════════════════
// §3.6 — Metrics Report
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Report of aggregated metrics for a queried period.
/// </summary>
/// <remarks>
/// <para>
/// Generated by <see cref="IWorkflowMetricsCollector.GetMetricsAsync"/>. Contains
/// raw metric records, aggregated statistics, and the original query for reference.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.6
/// </para>
/// </remarks>
public record MetricsReport
{
    /// <summary>The query that generated this report.</summary>
    public required MetricsQuery Query { get; init; }

    /// <summary>Workflow execution records matching the query.</summary>
    public IReadOnlyList<WorkflowExecutionMetrics> WorkflowExecutions { get; init; } = [];

    /// <summary>Validation step records matching the query.</summary>
    public IReadOnlyList<ValidationStepMetrics> ValidationSteps { get; init; } = [];

    /// <summary>Gating decision records matching the query.</summary>
    public IReadOnlyList<GatingDecisionMetrics> GatingDecisions { get; init; } = [];

    /// <summary>Sync operation records matching the query.</summary>
    public IReadOnlyList<SyncOperationMetrics> SyncOperations { get; init; } = [];

    /// <summary>Aggregated statistics calculated from the matched records.</summary>
    public MetricsStatistics? Statistics { get; init; }

    /// <summary>UTC timestamp when this report was generated.</summary>
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Aggregated statistics derived from metrics records.
/// </summary>
/// <remarks>
/// <para>
/// Provides summary counters and averages across all matched workflow executions
/// in a <see cref="MetricsReport"/>.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §3.6
/// </para>
/// </remarks>
public record MetricsStatistics
{
    /// <summary>Total number of workflow executions in the period.</summary>
    public int TotalExecutions { get; init; }

    /// <summary>Number of successful executions.</summary>
    public int SuccessfulExecutions { get; init; }

    /// <summary>Number of failed executions.</summary>
    public int FailedExecutions { get; init; }

    /// <summary>Success rate as a percentage (0–100).</summary>
    public decimal SuccessRate { get; init; }

    /// <summary>Average execution time in milliseconds.</summary>
    public double AvgExecutionTimeMs { get; init; }

    /// <summary>Total errors across all executions in the period.</summary>
    public int TotalErrors { get; init; }

    /// <summary>Total warnings across all executions in the period.</summary>
    public int TotalWarnings { get; init; }

    /// <summary>Average errors per execution.</summary>
    public decimal AvgErrorsPerExecution { get; init; }

    /// <summary>Number of distinct documents with errors.</summary>
    public int DocumentsWithErrors { get; init; }

    /// <summary>Number of distinct documents that passed validation.</summary>
    public int DocumentsPassedValidation { get; init; }
}
