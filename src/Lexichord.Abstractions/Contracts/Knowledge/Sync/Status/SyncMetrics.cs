// =============================================================================
// File: SyncMetrics.cs
// Project: Lexichord.Abstractions
// Description: Record containing computed sync metrics for a document.
// =============================================================================
// LOGIC: Aggregates sync operation data into meaningful metrics for monitoring,
//   alerting, and reporting. Computed on-demand from operation records.
//
// v0.7.6i: Sync Status Tracker (CKVS Phase 4c)
// Dependencies: SyncState (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Status;

/// <summary>
/// Aggregated synchronization metrics for a document.
/// </summary>
/// <remarks>
/// <para>
/// Provides comprehensive statistics about sync operations:
/// </para>
/// <list type="bullet">
///   <item><b>Counts:</b> Total, successful, and failed operations.</item>
///   <item><b>Timing:</b> Average, longest, and shortest durations.</item>
///   <item><b>Outcomes:</b> Last success/failure timestamps.</item>
///   <item><b>Conflicts:</b> Total, resolved, and unresolved counts.</item>
///   <item><b>Rates:</b> Success rate percentage.</item>
///   <item><b>Averages:</b> Entities and claims per sync.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var metrics = await tracker.GetMetricsAsync(documentId);
/// Console.WriteLine($"Success rate: {metrics.SuccessRate:F1}%");
/// Console.WriteLine($"Average duration: {metrics.AverageDuration.TotalSeconds:F2}s");
/// </code>
/// </para>
/// <para>
/// <b>License Gating:</b>
/// </para>
/// <list type="bullet">
///   <item>WriterPro: Basic metrics (counts, success rate)</item>
///   <item>Teams: Full metrics (all fields)</item>
///   <item>Enterprise: Advanced metrics with custom aggregations</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6i as part of the Sync Status Tracker.
/// </para>
/// </remarks>
public record SyncMetrics
{
    /// <summary>
    /// The document these metrics apply to.
    /// </summary>
    /// <value>The document ID for the metrics.</value>
    /// <remarks>
    /// LOGIC: Identifies which document the aggregated metrics are for.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// Total number of sync operations.
    /// </summary>
    /// <value>Count of all sync operations for this document.</value>
    /// <remarks>
    /// LOGIC: Sum of all operations regardless of outcome.
    /// TotalOperations = SuccessfulOperations + FailedOperations.
    /// </remarks>
    public int TotalOperations { get; init; }

    /// <summary>
    /// Number of successful sync operations.
    /// </summary>
    /// <value>Count of operations with Success or SuccessWithConflicts status.</value>
    /// <remarks>
    /// LOGIC: Operations that completed without failure.
    /// Includes SuccessWithConflicts and PartialSuccess.
    /// </remarks>
    public int SuccessfulOperations { get; init; }

    /// <summary>
    /// Number of failed sync operations.
    /// </summary>
    /// <value>Count of operations with Failed status.</value>
    /// <remarks>
    /// LOGIC: Operations that failed to complete.
    /// High failure counts may indicate systemic issues.
    /// </remarks>
    public int FailedOperations { get; init; }

    /// <summary>
    /// Average duration of sync operations.
    /// </summary>
    /// <value>
    /// Mean duration across all completed operations.
    /// Zero if no completed operations.
    /// </value>
    /// <remarks>
    /// LOGIC: Sum of durations / count of completed operations.
    /// Used for performance monitoring and SLA tracking.
    /// </remarks>
    public TimeSpan AverageDuration { get; init; }

    /// <summary>
    /// Longest sync operation duration.
    /// </summary>
    /// <value>
    /// Maximum duration among all completed operations, or null if none.
    /// </value>
    /// <remarks>
    /// LOGIC: Peak duration for identifying outliers.
    /// May indicate complex syncs or performance issues.
    /// </remarks>
    public TimeSpan? LongestDuration { get; init; }

    /// <summary>
    /// Shortest sync operation duration.
    /// </summary>
    /// <value>
    /// Minimum duration among all completed operations, or null if none.
    /// </value>
    /// <remarks>
    /// LOGIC: Baseline duration for simple syncs.
    /// Useful for establishing performance expectations.
    /// </remarks>
    public TimeSpan? ShortestDuration { get; init; }

    /// <summary>
    /// Timestamp of last successful sync.
    /// </summary>
    /// <value>
    /// When the most recent successful sync completed, or null if never.
    /// </value>
    /// <remarks>
    /// LOGIC: Indicates data freshness.
    /// Large gaps may indicate sync issues.
    /// </remarks>
    public DateTimeOffset? LastSuccessfulSync { get; init; }

    /// <summary>
    /// Timestamp of last failed sync.
    /// </summary>
    /// <value>
    /// When the most recent failed sync occurred, or null if never.
    /// </value>
    /// <remarks>
    /// LOGIC: Indicates recent problems.
    /// Recent failures may need investigation.
    /// </remarks>
    public DateTimeOffset? LastFailedSync { get; init; }

    /// <summary>
    /// Total conflicts detected across all operations.
    /// </summary>
    /// <value>Sum of conflicts detected in all sync operations.</value>
    /// <remarks>
    /// LOGIC: Historical conflict count for trend analysis.
    /// High counts may indicate data model issues.
    /// </remarks>
    public int TotalConflicts { get; init; }

    /// <summary>
    /// Total conflicts resolved across all operations.
    /// </summary>
    /// <value>Sum of conflicts resolved in all sync operations.</value>
    /// <remarks>
    /// LOGIC: Indicates conflict resolution effectiveness.
    /// Should trend toward TotalConflicts over time.
    /// </remarks>
    public int ResolvedConflicts { get; init; }

    /// <summary>
    /// Currently unresolved conflicts.
    /// </summary>
    /// <value>Count of conflicts still requiring resolution.</value>
    /// <remarks>
    /// LOGIC: From current SyncStatus.UnresolvedConflicts.
    /// Non-zero indicates pending user action needed.
    /// </remarks>
    public int UnresolvedConflicts { get; init; }

    /// <summary>
    /// Success rate as a percentage.
    /// </summary>
    /// <value>
    /// Percentage of successful operations (0-100).
    /// Zero if no operations.
    /// </value>
    /// <remarks>
    /// LOGIC: (SuccessfulOperations / TotalOperations) * 100.
    /// Key health indicator for sync reliability.
    /// </remarks>
    public float SuccessRate { get; init; }

    /// <summary>
    /// Average entities affected per sync.
    /// </summary>
    /// <value>
    /// Mean entity count across all operations.
    /// Zero if no operations.
    /// </value>
    /// <remarks>
    /// LOGIC: Sum of EntitiesAffected / TotalOperations.
    /// Indicates typical sync complexity for entities.
    /// </remarks>
    public float AverageEntitiesAffected { get; init; }

    /// <summary>
    /// Average claims affected per sync.
    /// </summary>
    /// <value>
    /// Mean claim count across all operations.
    /// Zero if no operations.
    /// </value>
    /// <remarks>
    /// LOGIC: Sum of ClaimsAffected / TotalOperations.
    /// Indicates typical sync complexity for claims.
    /// </remarks>
    public float AverageClaimsAffected { get; init; }

    /// <summary>
    /// Current sync state of the document.
    /// </summary>
    /// <value>The document's current sync state.</value>
    /// <remarks>
    /// LOGIC: Convenience field from SyncStatus.State.
    /// Provides context for interpreting metrics.
    /// </remarks>
    public required SyncState CurrentState { get; init; }

    /// <summary>
    /// Time elapsed in current sync state.
    /// </summary>
    /// <value>Duration since the last state change.</value>
    /// <remarks>
    /// LOGIC: DateTimeOffset.UtcNow - LastStateChangeTime.
    /// Long durations in PendingSync may indicate stuck syncs.
    /// </remarks>
    public TimeSpan TimeInCurrentState { get; init; }
}
