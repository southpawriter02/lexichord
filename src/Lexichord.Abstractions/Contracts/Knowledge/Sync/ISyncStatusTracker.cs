// =============================================================================
// File: ISyncStatusTracker.cs
// Project: Lexichord.Abstractions
// Description: Interface for tracking document sync status.
// =============================================================================
// LOGIC: ISyncStatusTracker maintains the sync state for each document.
//   It provides methods to query and update status, tracking last sync
//   time, pending changes, and unresolved conflicts.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// Dependencies: SyncStatus (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Service for tracking synchronization status of documents.
/// </summary>
/// <remarks>
/// <para>
/// Maintains per-document sync state information:
/// </para>
/// <list type="bullet">
///   <item><b>Query:</b> Get current status for a document.</item>
///   <item><b>Update:</b> Record status changes after sync operations.</item>
/// </list>
/// <para>
/// This is an internal service used by <see cref="ISyncService"/>. Status
/// is persisted to enable recovery across sessions.
/// </para>
/// <para>
/// <b>Implementation:</b> See <c>SyncStatusTracker</c> in Lexichord.Modules.Knowledge.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public interface ISyncStatusTracker
{
    /// <summary>
    /// Gets the current sync status for a document.
    /// </summary>
    /// <param name="documentId">The document ID to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The current <see cref="SyncStatus"/> for the document.
    /// Returns a default status with <see cref="SyncState.NeverSynced"/>
    /// if no status record exists.
    /// </returns>
    /// <remarks>
    /// LOGIC: Queries the status store for the document. Creates a default
    /// status on first access. Status includes timestamps, pending counts,
    /// and conflict counts.
    /// </remarks>
    Task<SyncStatus> GetStatusAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the sync status for a document.
    /// </summary>
    /// <param name="documentId">The document ID to update.</param>
    /// <param name="status">The new status to record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Persists the new status to the store. Called by
    /// <see cref="ISyncService"/> after sync operations to record:
    /// </para>
    /// <list type="bullet">
    ///   <item>State transitions (PendingSync → InSync, etc.).</item>
    ///   <item>Timestamp updates (last sync, last attempt).</item>
    ///   <item>Conflict and pending change counts.</item>
    ///   <item>Error messages from failed syncs.</item>
    /// </list>
    /// </remarks>
    Task UpdateStatusAsync(
        Guid documentId,
        SyncStatus status,
        CancellationToken ct = default);
}
