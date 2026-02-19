// =============================================================================
// File: ISyncStatusRepository.cs
// Project: Lexichord.Abstractions
// Description: Interface for sync status data persistence.
// =============================================================================
// LOGIC: Defines the persistence layer contract for sync status data. Enables
//   storage and retrieval of sync statuses, history, and operation records.
//   Implementation may use in-memory storage or persistent databases.
//
// v0.7.6i: Sync Status Tracker (CKVS Phase 4c)
// Dependencies: SyncStatus, SyncState (v0.7.6e), SyncStatusQuery,
//               SyncStatusHistory, SyncOperationRecord (v0.7.6i)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Status;

/// <summary>
/// Repository interface for sync status data persistence.
/// </summary>
/// <remarks>
/// <para>
/// Provides data access operations for sync status management:
/// </para>
/// <list type="bullet">
///   <item><b>CRUD:</b> Create, read, update, delete sync status records.</item>
///   <item><b>Query:</b> Filter and paginate sync status results.</item>
///   <item><b>History:</b> Store and retrieve status change history.</item>
///   <item><b>Operations:</b> Store and retrieve operation records.</item>
///   <item><b>Aggregation:</b> Count documents by sync state.</item>
/// </list>
/// <para>
/// <b>Implementation:</b> See <c>SyncStatusRepository</c> in Lexichord.Modules.Knowledge
/// for the default in-memory implementation.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe as the repository
/// may be accessed concurrently from multiple sync operations.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6i as part of the Sync Status Tracker.
/// </para>
/// </remarks>
public interface ISyncStatusRepository
{
    #region Status CRUD Operations

    /// <summary>
    /// Gets the current sync status for a document.
    /// </summary>
    /// <param name="documentId">The document ID to retrieve status for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The current <see cref="SyncStatus"/> if found, or null if no status exists.
    /// </returns>
    /// <remarks>
    /// LOGIC: Retrieves the stored status record for the document.
    /// Returns null for documents that have never had a status recorded.
    /// </remarks>
    Task<SyncStatus?> GetAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new sync status record.
    /// </summary>
    /// <param name="status">The status to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created status record.</returns>
    /// <remarks>
    /// LOGIC: Inserts a new status record for a document.
    /// Throws if a status already exists for the document ID.
    /// </remarks>
    Task<SyncStatus> CreateAsync(SyncStatus status, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing sync status record.
    /// </summary>
    /// <param name="status">The updated status.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated status record.</returns>
    /// <remarks>
    /// LOGIC: Replaces the existing status for the document.
    /// Creates a new record if one doesn't exist (upsert behavior).
    /// </remarks>
    Task<SyncStatus> UpdateAsync(SyncStatus status, CancellationToken ct = default);

    /// <summary>
    /// Deletes a sync status record.
    /// </summary>
    /// <param name="documentId">The document ID to delete status for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    /// <remarks>
    /// LOGIC: Removes the status record for the document.
    /// Does not delete associated history or operation records.
    /// </remarks>
    Task<bool> DeleteAsync(Guid documentId, CancellationToken ct = default);

    #endregion

    #region Query Operations

    /// <summary>
    /// Queries sync statuses matching the specified criteria.
    /// </summary>
    /// <param name="query">The query criteria and pagination settings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of matching sync statuses.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Applies filters from <see cref="SyncStatusQuery"/> in order:
    /// </para>
    /// <list type="number">
    ///   <item>Filter by State if specified</item>
    ///   <item>Filter by WorkspaceId if specified</item>
    ///   <item>Filter by LastSyncBefore/After if specified</item>
    ///   <item>Filter by MinPendingChanges if specified</item>
    ///   <item>Filter by MinUnresolvedConflicts if specified</item>
    ///   <item>Filter by SyncInProgress if specified</item>
    ///   <item>Apply SortOrder</item>
    ///   <item>Apply pagination (PageOffset, PageSize)</item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<SyncStatus>> QueryAsync(
        SyncStatusQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Counts documents grouped by sync state.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary mapping each sync state to its document count.</returns>
    /// <remarks>
    /// LOGIC: Aggregates all status records by state.
    /// Used for dashboard summaries and health monitoring.
    /// States with zero documents may be omitted from results.
    /// </remarks>
    Task<Dictionary<SyncState, int>> CountByStateAsync(CancellationToken ct = default);

    #endregion

    #region History Operations

    /// <summary>
    /// Adds a history entry for a status change.
    /// </summary>
    /// <param name="history">The history entry to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// LOGIC: Inserts the history record into the history store.
    /// Called by SyncStatusTracker when status state changes.
    /// </remarks>
    Task AddHistoryAsync(SyncStatusHistory history, CancellationToken ct = default);

    /// <summary>
    /// Gets status change history for a document.
    /// </summary>
    /// <param name="documentId">The document ID to get history for.</param>
    /// <param name="limit">Maximum number of entries to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// History entries ordered by <see cref="SyncStatusHistory.ChangedAt"/> descending
    /// (most recent first), limited to the specified count.
    /// </returns>
    /// <remarks>
    /// LOGIC: Retrieves recent history for audit and debugging.
    /// Default limit is 100 entries.
    /// </remarks>
    Task<IReadOnlyList<SyncStatusHistory>> GetHistoryAsync(
        Guid documentId,
        int limit = 100,
        CancellationToken ct = default);

    #endregion

    #region Operation Record Operations

    /// <summary>
    /// Adds an operation record for a sync operation.
    /// </summary>
    /// <param name="record">The operation record to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// LOGIC: Inserts the operation record into the operations store.
    /// Called when sync operations complete (success or failure).
    /// </remarks>
    Task AddOperationRecordAsync(SyncOperationRecord record, CancellationToken ct = default);

    /// <summary>
    /// Gets operation records for a document.
    /// </summary>
    /// <param name="documentId">The document ID to get operations for.</param>
    /// <param name="limit">Maximum number of records to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Operation records ordered by <see cref="SyncOperationRecord.StartedAt"/> descending
    /// (most recent first), limited to the specified count.
    /// </returns>
    /// <remarks>
    /// LOGIC: Retrieves operation history for metrics computation.
    /// Default limit is 100 records.
    /// </remarks>
    Task<IReadOnlyList<SyncOperationRecord>> GetOperationRecordsAsync(
        Guid documentId,
        int limit = 100,
        CancellationToken ct = default);

    #endregion
}
