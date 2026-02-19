// =============================================================================
// File: SyncStatusQuery.cs
// Project: Lexichord.Abstractions
// Description: Record representing query criteria for sync status lookups.
// =============================================================================
// LOGIC: Provides a structured query object for filtering and paginating
//   sync status records. Supports filtering by state, workspace, timestamps,
//   pending changes, conflicts, and sync progress.
//
// v0.7.6i: Sync Status Tracker (CKVS Phase 4c)
// Dependencies: SyncState (v0.7.6e), SortOrder (v0.7.6i)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Status;

/// <summary>
/// Query criteria for filtering and paginating sync status records.
/// </summary>
/// <remarks>
/// <para>
/// Provides comprehensive filtering options for sync status queries:
/// </para>
/// <list type="bullet">
///   <item><b>State:</b> Filter by specific sync state.</item>
///   <item><b>Workspace:</b> Filter by workspace membership.</item>
///   <item><b>Timestamps:</b> Filter by last sync date range.</item>
///   <item><b>Thresholds:</b> Filter by pending changes or conflict counts.</item>
///   <item><b>Progress:</b> Filter by active sync operations.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var query = new SyncStatusQuery
/// {
///     State = SyncState.PendingSync,
///     MinPendingChanges = 5,
///     SortOrder = SortOrder.ByPendingChangesDescending,
///     PageSize = 50
/// };
/// var statuses = await repository.QueryAsync(query);
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6i as part of the Sync Status Tracker.
/// </para>
/// </remarks>
public record SyncStatusQuery
{
    /// <summary>
    /// Filter by specific sync state.
    /// </summary>
    /// <value>
    /// The sync state to filter by, or null to include all states.
    /// </value>
    /// <remarks>
    /// LOGIC: When specified, only documents in this exact state are returned.
    /// Common use: filter for PendingSync to find documents needing attention.
    /// </remarks>
    public SyncState? State { get; init; }

    /// <summary>
    /// Filter by workspace membership.
    /// </summary>
    /// <value>
    /// The workspace ID to filter by, or null to include all workspaces.
    /// </value>
    /// <remarks>
    /// LOGIC: Restricts results to documents belonging to a specific workspace.
    /// Used for workspace-scoped sync status views.
    /// </remarks>
    public Guid? WorkspaceId { get; init; }

    /// <summary>
    /// Filter for documents last synced before this date.
    /// </summary>
    /// <value>
    /// The cutoff date, or null to not filter by upper bound.
    /// </value>
    /// <remarks>
    /// LOGIC: Finds stale documents that haven't been synced recently.
    /// Example: LastSyncBefore = 7 days ago to find documents not synced in a week.
    /// </remarks>
    public DateTimeOffset? LastSyncBefore { get; init; }

    /// <summary>
    /// Filter for documents last synced after this date.
    /// </summary>
    /// <value>
    /// The cutoff date, or null to not filter by lower bound.
    /// </value>
    /// <remarks>
    /// LOGIC: Finds recently synced documents.
    /// Example: LastSyncAfter = today to find documents synced today.
    /// </remarks>
    public DateTimeOffset? LastSyncAfter { get; init; }

    /// <summary>
    /// Filter for documents with at least this many pending changes.
    /// </summary>
    /// <value>
    /// The minimum pending change count threshold, or null to not filter.
    /// </value>
    /// <remarks>
    /// LOGIC: Surfaces documents with significant pending changes.
    /// Used to prioritize high-change documents for sync.
    /// </remarks>
    public int? MinPendingChanges { get; init; }

    /// <summary>
    /// Filter for documents with at least this many unresolved conflicts.
    /// </summary>
    /// <value>
    /// The minimum conflict count threshold, or null to not filter.
    /// </value>
    /// <remarks>
    /// LOGIC: Surfaces documents requiring conflict resolution attention.
    /// MinUnresolvedConflicts = 1 finds all documents with any conflicts.
    /// </remarks>
    public int? MinUnresolvedConflicts { get; init; }

    /// <summary>
    /// Filter for documents with sync operations in progress.
    /// </summary>
    /// <value>
    /// True to find syncing documents, false for idle documents, null for all.
    /// </value>
    /// <remarks>
    /// LOGIC: Used to monitor active sync operations or find available documents.
    /// SyncInProgress = true shows currently syncing documents.
    /// SyncInProgress = false shows documents available for new sync.
    /// </remarks>
    public bool? SyncInProgress { get; init; }

    /// <summary>
    /// Sort order for query results.
    /// </summary>
    /// <value>
    /// The sort order to apply. Defaults to <see cref="SortOrder.ByLastSyncDescending"/>.
    /// </value>
    /// <remarks>
    /// LOGIC: Determines the ordering of returned results.
    /// Default shows most recently synced documents first.
    /// </remarks>
    public SortOrder SortOrder { get; init; } = SortOrder.ByLastSyncDescending;

    /// <summary>
    /// Maximum number of results to return per page.
    /// </summary>
    /// <value>
    /// The page size limit. Defaults to 100. Maximum recommended is 1000.
    /// </value>
    /// <remarks>
    /// LOGIC: Limits result set size for performance.
    /// Use with PageOffset for pagination through large result sets.
    /// </remarks>
    public int PageSize { get; init; } = 100;

    /// <summary>
    /// Number of results to skip for pagination.
    /// </summary>
    /// <value>
    /// The offset to skip. Defaults to 0 (start from beginning).
    /// </value>
    /// <remarks>
    /// LOGIC: Enables pagination by skipping already-seen results.
    /// Page N = PageOffset = (N - 1) * PageSize.
    /// </remarks>
    public int PageOffset { get; init; } = 0;
}
