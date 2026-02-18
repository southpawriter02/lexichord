// =============================================================================
// File: SyncStatus.cs
// Project: Lexichord.Abstractions
// Description: Record representing the current sync status of a document.
// =============================================================================
// LOGIC: Each document has a sync status that tracks its relationship with
//   the knowledge graph. This includes the current state, last sync time,
//   pending changes, and any unresolved conflicts.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// Dependencies: SyncState (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Current synchronization status for a document with the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// Provides comprehensive sync status information:
/// </para>
/// <list type="bullet">
///   <item><b>State:</b> Current sync lifecycle state (see <see cref="SyncState"/>).</item>
///   <item><b>Timing:</b> Last successful sync and last attempt timestamps.</item>
///   <item><b>Pending:</b> Count of changes waiting to be synced.</item>
///   <item><b>Conflicts:</b> Count of unresolved conflicts.</item>
///   <item><b>Progress:</b> Whether a sync is currently in progress.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var status = await syncService.GetSyncStatusAsync(documentId);
/// if (status.State == SyncState.PendingSync)
/// {
///     Console.WriteLine($"{status.PendingChanges} changes awaiting sync");
/// }
/// else if (status.State == SyncState.Conflict)
/// {
///     Console.WriteLine($"{status.UnresolvedConflicts} conflicts need resolution");
/// }
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public record SyncStatus
{
    /// <summary>
    /// The document ID this status belongs to.
    /// </summary>
    /// <value>The unique identifier of the document.</value>
    /// <remarks>
    /// LOGIC: Primary key for status lookups. Links to the Document
    /// record in the RAG system.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// Current sync state of the document.
    /// </summary>
    /// <value>The document's position in the sync lifecycle.</value>
    /// <remarks>
    /// LOGIC: Determines what sync actions are available. InSync means
    /// no action needed. PendingSync enables the sync button.
    /// Conflict requires resolution before further syncing.
    /// </remarks>
    public required SyncState State { get; init; }

    /// <summary>
    /// Timestamp of the last successful synchronization.
    /// </summary>
    /// <value>
    /// UTC timestamp of the last sync that completed with
    /// <see cref="SyncOperationStatus.Success"/>. Null if never synced successfully.
    /// </value>
    /// <remarks>
    /// LOGIC: Used to determine staleness and for display in the UI.
    /// Updated only when a sync completes without errors.
    /// </remarks>
    public DateTimeOffset? LastSyncAt { get; init; }

    /// <summary>
    /// Number of pending changes awaiting synchronization.
    /// </summary>
    /// <value>
    /// Count of document changes since last sync. 0 if in sync.
    /// </value>
    /// <remarks>
    /// LOGIC: Incremented when document content changes. Reset to 0
    /// after successful sync. Used to show badge count in UI.
    /// </remarks>
    public int PendingChanges { get; init; }

    /// <summary>
    /// Timestamp of the last sync attempt (successful or failed).
    /// </summary>
    /// <value>
    /// UTC timestamp of the most recent sync attempt. Null if never attempted.
    /// </value>
    /// <remarks>
    /// LOGIC: Updated at the start of every sync attempt. Useful for
    /// debugging and determining if retries are appropriate.
    /// </remarks>
    public DateTimeOffset? LastAttemptAt { get; init; }

    /// <summary>
    /// Error message from the last failed sync attempt.
    /// </summary>
    /// <value>
    /// Error details when the last sync failed. Null if last sync succeeded
    /// or no sync has been attempted.
    /// </value>
    /// <remarks>
    /// LOGIC: Preserved for debugging and display. Cleared when a
    /// subsequent sync succeeds.
    /// </remarks>
    public string? LastError { get; init; }

    /// <summary>
    /// Number of unresolved conflicts.
    /// </summary>
    /// <value>
    /// Count of conflicts requiring resolution. 0 if no conflicts.
    /// </value>
    /// <remarks>
    /// LOGIC: Populated when sync detects conflicts that weren't
    /// auto-resolved. User must resolve via <see cref="ISyncService.ResolveConflictAsync"/>.
    /// </remarks>
    public int UnresolvedConflicts { get; init; }

    /// <summary>
    /// Whether a sync operation is currently in progress.
    /// </summary>
    /// <value>True if sync is running, false otherwise.</value>
    /// <remarks>
    /// LOGIC: Set to true at sync start, false at completion.
    /// Used to disable the sync button and show progress indicator.
    /// </remarks>
    public bool IsSyncInProgress { get; init; }
}
