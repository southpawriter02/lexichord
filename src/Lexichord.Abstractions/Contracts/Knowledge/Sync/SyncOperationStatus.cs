// =============================================================================
// File: SyncOperationStatus.cs
// Project: Lexichord.Abstractions
// Description: Defines the outcome status of a synchronization operation.
// =============================================================================
// LOGIC: Each sync operation returns a status indicating whether it succeeded,
//   partially succeeded, failed, or found no changes. This enables callers to
//   take appropriate action (e.g., showing conflicts, retrying, etc.).
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Status of a synchronization operation between documents and the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// Indicates the overall outcome of a sync operation:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Success"/>: All changes synced without issues.</description></item>
///   <item><description><see cref="SuccessWithConflicts"/>: Synced but conflicts were detected.</description></item>
///   <item><description><see cref="PartialSuccess"/>: Some items synced, others failed.</description></item>
///   <item><description><see cref="Failed"/>: Operation could not complete.</description></item>
///   <item><description><see cref="NoChanges"/>: Document is already in sync.</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public enum SyncOperationStatus
{
    /// <summary>
    /// Sync completed successfully with all changes applied.
    /// </summary>
    /// <remarks>
    /// LOGIC: No conflicts detected, all entities/claims/relationships were
    /// upserted to the graph without error.
    /// </remarks>
    Success = 0,

    /// <summary>
    /// Sync completed but conflicts were detected and need resolution.
    /// </summary>
    /// <remarks>
    /// LOGIC: Changes were applied, but some conflicts exist between the
    /// document and graph state. See <see cref="SyncResult.Conflicts"/>.
    /// </remarks>
    SuccessWithConflicts = 1,

    /// <summary>
    /// Sync partially completed with some items failing.
    /// </summary>
    /// <remarks>
    /// LOGIC: Some entities/claims were synced, but others encountered errors.
    /// Check <see cref="SyncResult.ErrorMessage"/> for details.
    /// </remarks>
    PartialSuccess = 2,

    /// <summary>
    /// Sync operation failed completely.
    /// </summary>
    /// <remarks>
    /// LOGIC: A critical error prevented the sync from completing. No changes
    /// were applied. See <see cref="SyncResult.ErrorMessage"/>.
    /// </remarks>
    Failed = 3,

    /// <summary>
    /// No changes detected; document is already in sync.
    /// </summary>
    /// <remarks>
    /// LOGIC: The document's current state matches the graph state. No
    /// extraction or upsert operations were performed.
    /// </remarks>
    NoChanges = 4
}
