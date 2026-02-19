// =============================================================================
// File: SyncStatusRepository.cs
// Project: Lexichord.Modules.Knowledge
// Description: In-memory implementation of ISyncStatusRepository.
// =============================================================================
// LOGIC: Provides thread-safe in-memory storage for sync status data including
//   current status, status history, and operation records. Uses ConcurrentDictionary
//   for thread safety. Future versions may add persistent storage via PostgreSQL.
//
// v0.7.6i: Sync Status Tracker (CKVS Phase 4c)
// Dependencies: SyncStatus, SyncState (v0.7.6e), ISyncStatusRepository,
//               SyncStatusQuery, SyncStatusHistory, SyncOperationRecord (v0.7.6i)
// =============================================================================

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Status;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.Status;

/// <summary>
/// In-memory implementation of <see cref="ISyncStatusRepository"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides thread-safe storage for sync status data using <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </para>
/// <para>
/// <b>Storage Structure:</b>
/// </para>
/// <list type="bullet">
///   <item><b>_statuses:</b> Current sync status per document (DocumentId → SyncStatus).</item>
///   <item><b>_history:</b> Status change history per document (DocumentId → List of SyncStatusHistory).</item>
///   <item><b>_operations:</b> Operation records per document (DocumentId → List of SyncOperationRecord).</item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> All operations are thread-safe via ConcurrentDictionary.
/// List modifications use lock statements for atomicity.
/// </para>
/// <para>
/// <b>Persistence:</b> Currently in-memory only. Data is lost on application restart.
/// Future versions may add PostgreSQL persistence.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6i as part of the Sync Status Tracker.
/// </para>
/// </remarks>
public sealed class SyncStatusRepository : ISyncStatusRepository
{
    // LOGIC: Separate ConcurrentDictionary for each data type.
    // This allows independent locking and optimal concurrency.
    private readonly ConcurrentDictionary<Guid, SyncStatus> _statuses = new();
    private readonly ConcurrentDictionary<Guid, List<SyncStatusHistory>> _history = new();
    private readonly ConcurrentDictionary<Guid, List<SyncOperationRecord>> _operations = new();

    private readonly ILogger<SyncStatusRepository> _logger;

    // LOGIC: Lock objects for list modifications within the dictionaries.
    private readonly object _historyLock = new();
    private readonly object _operationsLock = new();

    /// <summary>
    /// Initializes a new instance of <see cref="SyncStatusRepository"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public SyncStatusRepository(ILogger<SyncStatusRepository> logger)
    {
        _logger = logger;
        _logger.LogDebug("SyncStatusRepository initialized");
    }

    #region Status CRUD Operations

    /// <inheritdoc/>
    public Task<SyncStatus?> GetAsync(Guid documentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogTrace(
            "Getting sync status for document {DocumentId}",
            documentId);

        _statuses.TryGetValue(documentId, out var status);
        return Task.FromResult(status);
    }

    /// <inheritdoc/>
    public Task<SyncStatus> CreateAsync(SyncStatus status, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Creating sync status for document {DocumentId} with state {State}",
            status.DocumentId, status.State);

        // LOGIC: TryAdd returns false if key already exists.
        // For create semantics, we should fail if status already exists.
        if (!_statuses.TryAdd(status.DocumentId, status))
        {
            _logger.LogWarning(
                "Sync status already exists for document {DocumentId}, using update semantics",
                status.DocumentId);

            // LOGIC: Fall back to update semantics for idempotency.
            _statuses[status.DocumentId] = status;
        }

        return Task.FromResult(status);
    }

    /// <inheritdoc/>
    public Task<SyncStatus> UpdateAsync(SyncStatus status, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Updating sync status for document {DocumentId} to state {State}",
            status.DocumentId, status.State);

        // LOGIC: AddOrUpdate provides atomic upsert semantics.
        _statuses.AddOrUpdate(
            status.DocumentId,
            status,
            (_, _) => status);

        return Task.FromResult(status);
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(Guid documentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Deleting sync status for document {DocumentId}",
            documentId);

        var removed = _statuses.TryRemove(documentId, out _);

        if (removed)
        {
            _logger.LogInformation(
                "Deleted sync status for document {DocumentId}",
                documentId);
        }
        else
        {
            _logger.LogDebug(
                "No sync status found to delete for document {DocumentId}",
                documentId);
        }

        return Task.FromResult(removed);
    }

    #endregion

    #region Query Operations

    /// <inheritdoc/>
    public Task<IReadOnlyList<SyncStatus>> QueryAsync(
        SyncStatusQuery query,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Querying sync statuses with filters: State={State}, PageSize={PageSize}, PageOffset={PageOffset}",
            query.State, query.PageSize, query.PageOffset);

        // LOGIC: Start with all statuses and apply filters incrementally.
        IEnumerable<SyncStatus> filtered = _statuses.Values;

        // Filter by State
        if (query.State.HasValue)
        {
            filtered = filtered.Where(s => s.State == query.State.Value);
        }

        // Filter by LastSyncBefore
        if (query.LastSyncBefore.HasValue)
        {
            filtered = filtered.Where(s =>
                s.LastSyncAt == null || s.LastSyncAt < query.LastSyncBefore);
        }

        // Filter by LastSyncAfter
        if (query.LastSyncAfter.HasValue)
        {
            filtered = filtered.Where(s =>
                s.LastSyncAt != null && s.LastSyncAt > query.LastSyncAfter);
        }

        // Filter by MinPendingChanges
        if (query.MinPendingChanges.HasValue)
        {
            filtered = filtered.Where(s =>
                s.PendingChanges >= query.MinPendingChanges);
        }

        // Filter by MinUnresolvedConflicts
        if (query.MinUnresolvedConflicts.HasValue)
        {
            filtered = filtered.Where(s =>
                s.UnresolvedConflicts >= query.MinUnresolvedConflicts);
        }

        // Filter by SyncInProgress
        if (query.SyncInProgress.HasValue)
        {
            filtered = filtered.Where(s =>
                s.IsSyncInProgress == query.SyncInProgress);
        }

        // LOGIC: Apply sorting based on SortOrder enum.
        filtered = query.SortOrder switch
        {
            SortOrder.ByDocumentNameAscending => filtered
                .OrderBy(s => s.DocumentId.ToString()),
            SortOrder.ByDocumentNameDescending => filtered
                .OrderByDescending(s => s.DocumentId.ToString()),
            SortOrder.ByLastSyncAscending => filtered
                .OrderBy(s => s.LastSyncAt ?? DateTimeOffset.MinValue),
            SortOrder.ByLastSyncDescending => filtered
                .OrderByDescending(s => s.LastSyncAt ?? DateTimeOffset.MinValue),
            SortOrder.ByStateAscending => filtered
                .OrderBy(s => s.State),
            SortOrder.ByStateDescending => filtered
                .OrderByDescending(s => s.State),
            SortOrder.ByPendingChangesDescending => filtered
                .OrderByDescending(s => s.PendingChanges),
            _ => filtered
        };

        // Apply pagination
        var results = filtered
            .Skip(query.PageOffset)
            .Take(query.PageSize)
            .ToList();

        _logger.LogDebug(
            "Query returned {Count} sync statuses",
            results.Count);

        return Task.FromResult<IReadOnlyList<SyncStatus>>(results);
    }

    /// <inheritdoc/>
    public Task<Dictionary<SyncState, int>> CountByStateAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug("Counting documents by sync state");

        var counts = _statuses.Values
            .GroupBy(s => s.State)
            .ToDictionary(g => g.Key, g => g.Count());

        _logger.LogDebug(
            "State counts: {Counts}",
            string.Join(", ", counts.Select(kv => $"{kv.Key}={kv.Value}")));

        return Task.FromResult(counts);
    }

    #endregion

    #region History Operations

    /// <inheritdoc/>
    public Task AddHistoryAsync(SyncStatusHistory history, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Adding status history for document {DocumentId}: {PreviousState} -> {NewState}",
            history.DocumentId, history.PreviousState, history.NewState);

        // LOGIC: Use lock to ensure atomic list modification.
        lock (_historyLock)
        {
            var historyList = _history.GetOrAdd(
                history.DocumentId,
                _ => new List<SyncStatusHistory>());

            historyList.Add(history);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SyncStatusHistory>> GetHistoryAsync(
        Guid documentId,
        int limit = 100,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Getting status history for document {DocumentId} with limit {Limit}",
            documentId, limit);

        if (!_history.TryGetValue(documentId, out var historyList))
        {
            _logger.LogDebug(
                "No history found for document {DocumentId}",
                documentId);

            return Task.FromResult<IReadOnlyList<SyncStatusHistory>>(
                Array.Empty<SyncStatusHistory>());
        }

        // LOGIC: Return most recent first, limited to requested count.
        IReadOnlyList<SyncStatusHistory> results;
        lock (_historyLock)
        {
            results = historyList
                .OrderByDescending(h => h.ChangedAt)
                .Take(limit)
                .ToList();
        }

        _logger.LogDebug(
            "Retrieved {Count} history entries for document {DocumentId}",
            results.Count, documentId);

        return Task.FromResult(results);
    }

    #endregion

    #region Operation Record Operations

    /// <inheritdoc/>
    public Task AddOperationRecordAsync(SyncOperationRecord record, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Adding operation record {OperationId} for document {DocumentId}: " +
            "Direction={Direction}, Status={Status}",
            record.OperationId, record.DocumentId, record.Direction, record.Status);

        // LOGIC: Use lock to ensure atomic list modification.
        lock (_operationsLock)
        {
            var operationsList = _operations.GetOrAdd(
                record.DocumentId,
                _ => new List<SyncOperationRecord>());

            operationsList.Add(record);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SyncOperationRecord>> GetOperationRecordsAsync(
        Guid documentId,
        int limit = 100,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Getting operation records for document {DocumentId} with limit {Limit}",
            documentId, limit);

        if (!_operations.TryGetValue(documentId, out var operationsList))
        {
            _logger.LogDebug(
                "No operations found for document {DocumentId}",
                documentId);

            return Task.FromResult<IReadOnlyList<SyncOperationRecord>>(
                Array.Empty<SyncOperationRecord>());
        }

        // LOGIC: Return most recent first, limited to requested count.
        IReadOnlyList<SyncOperationRecord> results;
        lock (_operationsLock)
        {
            results = operationsList
                .OrderByDescending(o => o.StartedAt)
                .Take(limit)
                .ToList();
        }

        _logger.LogDebug(
            "Retrieved {Count} operation records for document {DocumentId}",
            results.Count, documentId);

        return Task.FromResult(results);
    }

    #endregion
}
