// =============================================================================
// File: SyncResult.cs
// Project: Lexichord.Abstractions
// Description: Result record returned from synchronization operations.
// =============================================================================
// LOGIC: Every sync operation returns a SyncResult containing the operation
//   status, affected entities/claims/relationships, any conflicts detected,
//   timing information, and error details if applicable.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// Dependencies: SyncOperationStatus, SyncConflict, KnowledgeEntity,
//               KnowledgeRelationship, Claim (all from Lexichord.Abstractions)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Result of a synchronization operation between a document and the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// Contains comprehensive information about the sync outcome:
/// </para>
/// <list type="bullet">
///   <item><b>Status:</b> Overall operation result (see <see cref="SyncOperationStatus"/>).</item>
///   <item><b>Affected Items:</b> Entities, claims, and relationships that were created/updated.</item>
///   <item><b>Conflicts:</b> Any conflicts detected (see <see cref="SyncConflict"/>).</item>
///   <item><b>Timing:</b> Operation duration and completion timestamp.</item>
///   <item><b>Error:</b> Error message if the operation failed.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var result = await syncService.SyncDocumentToGraphAsync(document, context);
/// if (result.Status == SyncOperationStatus.Success)
/// {
///     Console.WriteLine($"Synced {result.EntitiesAffected.Count} entities in {result.Duration.TotalMilliseconds}ms");
/// }
/// else if (result.Status == SyncOperationStatus.SuccessWithConflicts)
/// {
///     foreach (var conflict in result.Conflicts)
///     {
///         Console.WriteLine($"Conflict: {conflict.ConflictTarget} - {conflict.Type}");
///     }
/// }
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public record SyncResult
{
    /// <summary>
    /// Status of the sync operation.
    /// </summary>
    /// <value>The overall outcome of the synchronization.</value>
    /// <remarks>
    /// LOGIC: Determines how the caller should handle the result. Success
    /// means all changes applied. SuccessWithConflicts means changes applied
    /// but conflicts exist. Failed means no changes were applied.
    /// </remarks>
    public required SyncOperationStatus Status { get; init; }

    /// <summary>
    /// Entities affected by the synchronization.
    /// </summary>
    /// <value>
    /// A read-only list of entities that were created, updated, or involved
    /// in the sync operation. Empty if no entities were affected.
    /// </value>
    /// <remarks>
    /// LOGIC: Includes both newly created entities and existing entities
    /// that were updated. Used for UI feedback and audit logging.
    /// </remarks>
    public IReadOnlyList<KnowledgeEntity> EntitiesAffected { get; init; } = [];

    /// <summary>
    /// Claims affected by the synchronization.
    /// </summary>
    /// <value>
    /// A read-only list of claims that were created, updated, or involved
    /// in the sync operation. Empty if no claims were affected.
    /// </value>
    /// <remarks>
    /// LOGIC: Claims extracted from the document and upserted to the
    /// claim store. May include updated existing claims if values changed.
    /// </remarks>
    public IReadOnlyList<Claim> ClaimsAffected { get; init; } = [];

    /// <summary>
    /// Relationships affected by the synchronization.
    /// </summary>
    /// <value>
    /// A read-only list of relationships that were created, updated, or
    /// involved in the sync operation. Empty if no relationships were affected.
    /// </value>
    /// <remarks>
    /// LOGIC: Relationships between entities established during sync.
    /// Includes both new relationships and updated existing ones.
    /// </remarks>
    public IReadOnlyList<KnowledgeRelationship> RelationshipsAffected { get; init; } = [];

    /// <summary>
    /// Conflicts detected during synchronization.
    /// </summary>
    /// <value>
    /// A read-only list of conflicts that need resolution. Empty if no
    /// conflicts were detected or all conflicts were auto-resolved.
    /// </value>
    /// <remarks>
    /// LOGIC: Only populated when <see cref="Status"/> is
    /// <see cref="SyncOperationStatus.SuccessWithConflicts"/> or
    /// <see cref="SyncOperationStatus.Failed"/> due to conflicts.
    /// </remarks>
    public IReadOnlyList<SyncConflict> Conflicts { get; init; } = [];

    /// <summary>
    /// Duration of the sync operation.
    /// </summary>
    /// <value>
    /// The elapsed time from start to completion of the synchronization.
    /// </value>
    /// <remarks>
    /// LOGIC: Measured using Stopwatch for accuracy. Used for performance
    /// monitoring and logging. Set by the service after operation completes.
    /// </remarks>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    /// <value>
    /// Descriptive error message when <see cref="Status"/> is
    /// <see cref="SyncOperationStatus.Failed"/> or
    /// <see cref="SyncOperationStatus.PartialSuccess"/>. Null on success.
    /// </value>
    /// <remarks>
    /// LOGIC: Contains exception message or validation error details.
    /// Used for debugging and user feedback.
    /// </remarks>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Timestamp when the sync operation completed.
    /// </summary>
    /// <value>
    /// UTC timestamp of operation completion. Defaults to current time.
    /// </value>
    /// <remarks>
    /// LOGIC: Recorded at the end of the sync operation. Used for
    /// tracking sync history and determining staleness.
    /// </remarks>
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}
