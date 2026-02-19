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
// v0.7.6i: Enhanced with batch operations, history, metrics, and operation records.
// Dependencies: SyncStatus, SyncState (v0.7.6e), SyncStatusHistory,
//               SyncOperationRecord, SyncMetrics (v0.7.6i)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync.Status;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Service for tracking synchronization status of documents.
/// </summary>
/// <remarks>
/// <para>
/// Maintains per-document sync state information:
/// </para>
/// <list type="bullet">
///   <item><b>Query:</b> Get current status for one or multiple documents.</item>
///   <item><b>Update:</b> Record status changes after sync operations.</item>
///   <item><b>Batch:</b> Update multiple documents in a single operation.</item>
///   <item><b>History:</b> Track and retrieve status change history.</item>
///   <item><b>Metrics:</b> Compute aggregated sync metrics per document.</item>
///   <item><b>Operations:</b> Record and retrieve sync operation details.</item>
/// </list>
/// <para>
/// This is an internal service used by <see cref="ISyncService"/>. Status
/// is persisted to enable recovery across sessions.
/// </para>
/// <para>
/// <b>Implementation:</b> See <c>SyncStatusTracker</c> in Lexichord.Modules.Knowledge.
/// </para>
/// <para>
/// <b>License Gating:</b>
/// </para>
/// <list type="bullet">
///   <item>Core: No access to status tracking.</item>
///   <item>WriterPro: Basic status (get/update), 30-day history.</item>
///   <item>Teams: Full status (batch, metrics), 90-day history.</item>
///   <item>Enterprise: Advanced status (unlimited history, custom metrics).</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// <b>Enhanced in:</b> v0.7.6i with batch operations, history, and metrics.
/// </para>
/// </remarks>
public interface ISyncStatusTracker
{
    #region Basic Operations (v0.7.6e)

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
    /// <returns>The updated status record.</returns>
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
    /// <para>
    /// When the state changes, a <see cref="SyncStatusHistory"/> entry is
    /// recorded and a <c>SyncStatusUpdatedEvent</c> is published.
    /// </para>
    /// </remarks>
    Task<SyncStatus> UpdateStatusAsync(
        Guid documentId,
        SyncStatus status,
        CancellationToken ct = default);

    #endregion

    #region Batch Operations (v0.7.6i)

    /// <summary>
    /// Updates sync status for multiple documents in a batch.
    /// </summary>
    /// <param name="updates">List of document IDs and their new statuses.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The list of updated status records.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Efficiently updates multiple documents in sequence.
    /// Each update records history and publishes events as appropriate.
    /// </para>
    /// <para>
    /// <b>License Requirement:</b> Teams+ for batch operations.
    /// </para>
    /// <para>
    /// <b>Performance:</b> Target is &lt;1 second per 1000 updates.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<SyncStatus>> UpdateStatusBatchAsync(
        IReadOnlyList<(Guid DocumentId, SyncStatus Status)> updates,
        CancellationToken ct = default);

    /// <summary>
    /// Gets sync statuses for multiple documents.
    /// </summary>
    /// <param name="documentIds">The document IDs to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="SyncStatus"/> records for the requested documents.
    /// Documents without status records will have default <see cref="SyncState.NeverSynced"/> status.
    /// </returns>
    /// <remarks>
    /// LOGIC: Batch retrieval for efficiency when loading multiple documents.
    /// Results are returned in the same order as the input document IDs.
    /// </remarks>
    Task<IReadOnlyList<SyncStatus>> GetStatusesAsync(
        IReadOnlyList<Guid> documentIds,
        CancellationToken ct = default);

    /// <summary>
    /// Gets document IDs that are in a specific sync state.
    /// </summary>
    /// <param name="state">The sync state to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of document IDs in the specified state.</returns>
    /// <remarks>
    /// LOGIC: Used to find documents needing attention:
    /// - PendingSync: Documents ready for sync
    /// - Conflict: Documents with unresolved conflicts
    /// - NeedsReview: Documents requiring manual review
    /// </remarks>
    Task<IReadOnlyList<Guid>> GetDocumentsByStateAsync(
        SyncState state,
        CancellationToken ct = default);

    #endregion

    #region History Operations (v0.7.6i)

    /// <summary>
    /// Gets sync status change history for a document.
    /// </summary>
    /// <param name="documentId">The document ID to get history for.</param>
    /// <param name="limit">Maximum number of entries to return. Default 100.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// History entries ordered by <see cref="SyncStatusHistory.ChangedAt"/> descending
    /// (most recent first).
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Provides audit trail of status changes. Each entry shows
    /// the previous state, new state, when it changed, and who initiated it.
    /// </para>
    /// <para>
    /// <b>Retention by License Tier:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item>WriterPro: 30 days</item>
    ///   <item>Teams: 90 days</item>
    ///   <item>Enterprise: Unlimited</item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<SyncStatusHistory>> GetStatusHistoryAsync(
        Guid documentId,
        int limit = 100,
        CancellationToken ct = default);

    #endregion

    #region Metrics Operations (v0.7.6i)

    /// <summary>
    /// Gets aggregated sync metrics for a document.
    /// </summary>
    /// <param name="documentId">The document ID to get metrics for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Computed <see cref="SyncMetrics"/> including operation counts,
    /// success rates, durations, and conflict statistics.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Aggregates data from operation records to compute:
    /// </para>
    /// <list type="bullet">
    ///   <item>Total, successful, and failed operation counts</item>
    ///   <item>Average, min, and max operation durations</item>
    ///   <item>Success rate percentage</item>
    ///   <item>Conflict detection and resolution rates</item>
    ///   <item>Average entities and claims per sync</item>
    /// </list>
    /// <para>
    /// <b>Performance:</b> Target is &lt;300ms for metrics computation.
    /// </para>
    /// <para>
    /// <b>License Requirement:</b> Teams+ for full metrics.
    /// WriterPro gets basic metrics (counts and success rate only).
    /// </para>
    /// </remarks>
    Task<SyncMetrics> GetMetricsAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Records a sync operation for metrics and history tracking.
    /// </summary>
    /// <param name="documentId">The document ID the operation relates to.</param>
    /// <param name="operation">The operation record to store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Stores the operation record for later metrics aggregation.
    /// Called by sync providers after each sync operation completes.
    /// </para>
    /// <para>
    /// Operation records include timing, affected counts, conflict stats,
    /// and any error information for failed operations.
    /// </para>
    /// </remarks>
    Task RecordSyncOperationAsync(
        Guid documentId,
        SyncOperationRecord operation,
        CancellationToken ct = default);

    #endregion
}
