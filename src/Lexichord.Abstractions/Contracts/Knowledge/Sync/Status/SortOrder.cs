// =============================================================================
// File: SortOrder.cs
// Project: Lexichord.Abstractions
// Description: Enum defining sort order options for sync status queries.
// =============================================================================
// LOGIC: Provides standard sort order options for querying sync status records.
//   Used by SyncStatusQuery to determine result ordering.
//
// v0.7.6i: Sync Status Tracker (CKVS Phase 4c)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Status;

/// <summary>
/// Sort order options for sync status query results.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="SyncStatusQuery"/> to specify how results should be ordered.
/// Supports sorting by document name, last sync time, sync state, and pending changes.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6i as part of the Sync Status Tracker.
/// </para>
/// </remarks>
public enum SortOrder
{
    /// <summary>
    /// Sort by document name in ascending (A-Z) order.
    /// </summary>
    /// <remarks>
    /// LOGIC: Alphabetical sorting for browsing document lists.
    /// </remarks>
    ByDocumentNameAscending = 0,

    /// <summary>
    /// Sort by document name in descending (Z-A) order.
    /// </summary>
    /// <remarks>
    /// LOGIC: Reverse alphabetical sorting for browsing document lists.
    /// </remarks>
    ByDocumentNameDescending = 1,

    /// <summary>
    /// Sort by last sync time in ascending (oldest first) order.
    /// </summary>
    /// <remarks>
    /// LOGIC: Shows documents that haven't been synced recently first.
    /// Useful for identifying stale documents that may need attention.
    /// </remarks>
    ByLastSyncAscending = 2,

    /// <summary>
    /// Sort by last sync time in descending (newest first) order.
    /// </summary>
    /// <remarks>
    /// LOGIC: Shows recently synced documents first.
    /// Default sort order for most views.
    /// </remarks>
    ByLastSyncDescending = 3,

    /// <summary>
    /// Sort by sync state in ascending order.
    /// </summary>
    /// <remarks>
    /// LOGIC: Groups documents by state (InSync, PendingSync, NeedsReview, etc.).
    /// State order follows <see cref="SyncState"/> enum values.
    /// </remarks>
    ByStateAscending = 4,

    /// <summary>
    /// Sort by sync state in descending order.
    /// </summary>
    /// <remarks>
    /// LOGIC: Reverse state grouping.
    /// State order follows reverse <see cref="SyncState"/> enum values.
    /// </remarks>
    ByStateDescending = 5,

    /// <summary>
    /// Sort by pending changes count in descending (most changes first) order.
    /// </summary>
    /// <remarks>
    /// LOGIC: Surfaces documents with the most pending changes first.
    /// Useful for prioritizing which documents to sync.
    /// </remarks>
    ByPendingChangesDescending = 6
}
