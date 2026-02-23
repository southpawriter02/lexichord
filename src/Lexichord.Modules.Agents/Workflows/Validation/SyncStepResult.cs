// -----------------------------------------------------------------------
// <copyright file="SyncStepResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Result record returned by ISyncWorkflowStep.ExecuteSyncAsync().
//   Contains detailed sync operation metrics: items synced, conflicts
//   detected/resolved, individual changes, execution timing, and an
//   audit trail of sync logs.
//
// v0.7.7g: Sync Step Type (CKVS Phase 4d)
// Dependencies: SyncDirection (v0.7.7g), SyncStepConflict (v0.7.7g),
//               SyncChange (v0.7.7g)
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Knowledge.Sync;

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Result of a synchronization workflow step execution.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="ISyncWorkflowStep.ExecuteSyncAsync"/> to report
/// the detailed outcome of a sync operation. The <see cref="SyncWorkflowStep"/>
/// maps this to a <see cref="WorkflowStepResult"/> for the workflow engine,
/// storing the full result under the "syncResult" data key.
/// </para>
/// <para>
/// <b>Key Metrics:</b>
/// <list type="bullet">
///   <item><description><see cref="ItemsSynced"/>: Count of entities/claims/relationships synced.</description></item>
///   <item><description><see cref="ConflictsDetected"/>/<see cref="ConflictsResolved"/>: Conflict statistics.</description></item>
///   <item><description><see cref="Changes"/>: Individual change records for audit.</description></item>
///   <item><description><see cref="SyncLogs"/>: Timestamped audit trail.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7g as part of Sync Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
public record SyncStepResult
{
    /// <summary>
    /// Gets the identifier of the step that produced this result.
    /// </summary>
    public required string StepId { get; init; }

    /// <summary>
    /// Gets whether the synchronization succeeded.
    /// </summary>
    /// <remarks>
    /// <c>true</c> when the sync operation completed without fatal errors.
    /// A result can be successful even if conflicts were detected (when
    /// using a strategy that auto-resolves or continues on conflict).
    /// </remarks>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the sync direction that was used.
    /// </summary>
    public SyncDirection Direction { get; init; }

    /// <summary>
    /// Gets the number of items (entities, claims, relationships) synced.
    /// </summary>
    public int ItemsSynced { get; init; }

    /// <summary>
    /// Gets the number of conflicts detected during the sync.
    /// </summary>
    public int ConflictsDetected { get; init; }

    /// <summary>
    /// Gets the number of conflicts that were automatically resolved.
    /// </summary>
    public int ConflictsResolved { get; init; }

    /// <summary>
    /// Gets the list of conflicts that require manual resolution.
    /// </summary>
    /// <remarks>
    /// Empty when all conflicts are resolved or no conflicts exist.
    /// The workflow engine can inspect these to route to a conflict
    /// resolution step.
    /// </remarks>
    public IReadOnlyList<SyncStepConflict> UnresolvedConflicts { get; init; } = [];

    /// <summary>
    /// Gets the list of individual changes made during sync.
    /// </summary>
    /// <remarks>
    /// Contains one entry per entity/property modification. Used for
    /// audit trails and undo support.
    /// </remarks>
    public IReadOnlyList<SyncChange> Changes { get; init; } = [];

    /// <summary>
    /// Gets the execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// Gets the timestamped audit trail of sync operations.
    /// </summary>
    /// <remarks>
    /// Each entry is a log line recording a step in the sync process:
    /// start, entity extraction, conflict detection, resolution, completion.
    /// </remarks>
    public IReadOnlyList<string> SyncLogs { get; init; } = [];

    /// <summary>
    /// Gets the human-readable status message.
    /// </summary>
    /// <remarks>
    /// Examples: "Sync completed: 5 items synced", "Sync failed: service unavailable",
    /// "Skipped due to validation failures".
    /// </remarks>
    public string? StatusMessage { get; init; }

    /// <summary>
    /// Gets optional metadata about the sync operation.
    /// </summary>
    /// <remarks>
    /// May contain diagnostic data such as direction, conflict strategy,
    /// change counts, and service-specific metrics.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
