# LCS-DES-076-KG-i: Sync Status Tracker

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-076-KG-i |
| **System Breakdown** | LCS-SBD-076-KG |
| **Version** | v0.7.6 |
| **Codename** | Sync Status Tracker (CKVS Phase 4c) |
| **Estimated Hours** | 4 |
| **Status** | Complete |
| **Last Updated** | 2026-02-19 |

---

## 1. Overview

### 1.1 Purpose

The **Sync Status Tracker** maintains the synchronization state for documents, enabling visibility into sync progress, conflict status, and pending operations. It provides a queryable store of sync history and enables monitoring and alerting on sync operations.

### 1.2 Key Responsibilities

- Track sync state for each document
- Maintain sync history and audit trail
- Store sync status metadata
- Support sync status queries and reporting
- Enable monitoring and alerting workflows
- Provide sync metrics and statistics
- Support bulk status updates

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Sync/
      Status/
        ISyncStatusTracker.cs
        SyncStatusTracker.cs
        SyncStatusRepository.cs
        SyncStatusMetrics.cs
```

---

## 2. Interface Definitions

### 2.1 Sync Status Tracker

```csharp
namespace Lexichord.KnowledgeGraph.Sync.Status;

/// <summary>
/// Tracks synchronization status for documents.
/// </summary>
public interface ISyncStatusTracker
{
    /// <summary>
    /// Gets current sync status for a document.
    /// </summary>
    Task<SyncStatus> GetStatusAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates sync status for a document.
    /// </summary>
    Task<SyncStatus> UpdateStatusAsync(
        Guid documentId,
        SyncStatus status,
        CancellationToken ct = default);

    /// <summary>
    /// Updates status for multiple documents.
    /// </summary>
    Task<IReadOnlyList<SyncStatus>> UpdateStatusBatchAsync(
        IReadOnlyList<(Guid DocumentId, SyncStatus Status)> updates,
        CancellationToken ct = default);

    /// <summary>
    /// Gets sync statuses for documents.
    /// </summary>
    Task<IReadOnlyList<SyncStatus>> GetStatusesAsync(
        IReadOnlyList<Guid> documentIds,
        CancellationToken ct = default);

    /// <summary>
    /// Gets documents by sync state.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetDocumentsByStateAsync(
        SyncState state,
        CancellationToken ct = default);

    /// <summary>
    /// Gets sync history for a document.
    /// </summary>
    Task<IReadOnlyList<SyncStatusHistory>> GetStatusHistoryAsync(
        Guid documentId,
        int limit = 100,
        CancellationToken ct = default);

    /// <summary>
    /// Gets sync metrics for a document.
    /// </summary>
    Task<SyncMetrics> GetMetricsAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Records sync operation.
    /// </summary>
    Task RecordSyncOperationAsync(
        Guid documentId,
        SyncOperationRecord operation,
        CancellationToken ct = default);
}
```

### 2.2 Sync Status Repository

```csharp
/// <summary>
/// Repository for sync status data.
/// </summary>
public interface ISyncStatusRepository
{
    /// <summary>
    /// Gets current status for a document.
    /// </summary>
    Task<SyncStatus?> GetAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new status entry.
    /// </summary>
    Task<SyncStatus> CreateAsync(SyncStatus status, CancellationToken ct = default);

    /// <summary>
    /// Updates existing status entry.
    /// </summary>
    Task<SyncStatus> UpdateAsync(SyncStatus status, CancellationToken ct = default);

    /// <summary>
    /// Deletes status entry.
    /// </summary>
    Task<bool> DeleteAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Gets statuses matching criteria.
    /// </summary>
    Task<IReadOnlyList<SyncStatus>> QueryAsync(
        SyncStatusQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Counts documents by state.
    /// </summary>
    Task<Dictionary<SyncState, int>> CountByStateAsync(CancellationToken ct = default);
}
```

---

## 3. Data Types

### 3.1 Sync Status History

```csharp
/// <summary>
/// Historical record of sync status change.
/// </summary>
public record SyncStatusHistory
{
    /// <summary>Unique history ID.</summary>
    public required Guid HistoryId { get; init; }

    /// <summary>Document ID.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Previous sync state.</summary>
    public required SyncState PreviousState { get; init; }

    /// <summary>New sync state.</summary>
    public required SyncState NewState { get; init; }

    /// <summary>Timestamp of state change.</summary>
    public required DateTimeOffset ChangedAt { get; init; }

    /// <summary>User who initiated the change.</summary>
    public Guid? ChangedBy { get; init; }

    /// <summary>Reason for the change.</summary>
    public string? Reason { get; init; }

    /// <summary>Associated sync operation ID.</summary>
    public Guid? SyncOperationId { get; init; }

    /// <summary>Metadata about the change.</summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
```

### 3.2 Sync Operation Record

```csharp
/// <summary>
/// Record of a sync operation.
/// </summary>
public record SyncOperationRecord
{
    /// <summary>Unique operation ID.</summary>
    public required Guid OperationId { get; init; }

    /// <summary>Document being synced.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Sync direction.</summary>
    public required SyncDirection Direction { get; init; }

    /// <summary>Operation status.</summary>
    public required SyncOperationStatus Status { get; init; }

    /// <summary>When operation started.</summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>When operation completed.</summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Duration of operation.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>User who initiated operation.</summary>
    public Guid? InitiatedBy { get; init; }

    /// <summary>Number of entities affected.</summary>
    public int EntitiesAffected { get; init; }

    /// <summary>Number of claims affected.</summary>
    public int ClaimsAffected { get; init; }

    /// <summary>Number of relationships affected.</summary>
    public int RelationshipsAffected { get; init; }

    /// <summary>Number of conflicts detected.</summary>
    public int ConflictsDetected { get; init; }

    /// <summary>Number of conflicts resolved.</summary>
    public int ConflictsResolved { get; init; }

    /// <summary>Error message if failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Error code if failed.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Operation metadata.</summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
```

### 3.3 Sync Metrics

```csharp
/// <summary>
/// Synchronization metrics for a document.
/// </summary>
public record SyncMetrics
{
    /// <summary>Document ID.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Total sync operations.</summary>
    public int TotalOperations { get; init; }

    /// <summary>Successful operations.</summary>
    public int SuccessfulOperations { get; init; }

    /// <summary>Failed operations.</summary>
    public int FailedOperations { get; init; }

    /// <summary>Average sync duration.</summary>
    public TimeSpan AverageDuration { get; init; }

    /// <summary>Longest sync operation.</summary>
    public TimeSpan? LongestDuration { get; init; }

    /// <summary>Shortest sync operation.</summary>
    public TimeSpan? ShortestDuration { get; init; }

    /// <summary>Last successful sync.</summary>
    public DateTimeOffset? LastSuccessfulSync { get; init; }

    /// <summary>Last failed sync.</summary>
    public DateTimeOffset? LastFailedSync { get; init; }

    /// <summary>Total conflicts detected.</summary>
    public int TotalConflicts { get; init; }

    /// <summary>Resolved conflicts.</summary>
    public int ResolvedConflicts { get; init; }

    /// <summary>Unresolved conflicts.</summary>
    public int UnresolvedConflicts { get; init; }

    /// <summary>Success rate percentage.</summary>
    public float SuccessRate { get; init; }

    /// <summary>Average entities per sync.</summary>
    public float AverageEntitiesAffected { get; init; }

    /// <summary>Average claims per sync.</summary>
    public float AverageClaimsAffected { get; init; }

    /// <summary>Current sync state.</summary>
    public required SyncState CurrentState { get; init; }

    /// <summary>Time in current state.</summary>
    public TimeSpan TimeInCurrentState { get; init; }
}
```

### 3.4 Sync Status Query

```csharp
/// <summary>
/// Query criteria for sync status.
/// </summary>
public record SyncStatusQuery
{
    /// <summary>Filter by sync state.</summary>
    public SyncState? State { get; init; }

    /// <summary>Filter by workspace.</summary>
    public Guid? WorkspaceId { get; init; }

    /// <summary>Filter by last sync before date.</summary>
    public DateTimeOffset? LastSyncBefore { get; init; }

    /// <summary>Filter by last sync after date.</summary>
    public DateTimeOffset? LastSyncAfter { get; init; }

    /// <summary>Filter by pending changes threshold.</summary>
    public int? MinPendingChanges { get; init; }

    /// <summary>Filter by unresolved conflicts.</summary>
    public int? MinUnresolvedConflicts { get; init; }

    /// <summary>Filter documents in sync progress.</summary>
    public bool? SyncInProgress { get; init; }

    /// <summary>Sort order for results.</summary>
    public SortOrder SortOrder { get; init; } = SortOrder.ByLastSyncDescending;

    /// <summary>Maximum results.</summary>
    public int PageSize { get; init; } = 100;

    /// <summary>Offset for pagination.</summary>
    public int PageOffset { get; init; } = 0;
}

public enum SortOrder
{
    ByDocumentNameAscending,
    ByDocumentNameDescending,
    ByLastSyncAscending,
    ByLastSyncDescending,
    ByStateAscending,
    ByStateDescending,
    ByPendingChangesDescending
}
```

---

## 4. Implementation

### 4.1 Sync Status Tracker

```csharp
public class SyncStatusTracker : ISyncStatusTracker
{
    private readonly ISyncStatusRepository _repository;
    private readonly ILogger<SyncStatusTracker> _logger;
    private readonly ConcurrentDictionary<Guid, SyncStatus> _cache;

    public SyncStatusTracker(
        ISyncStatusRepository repository,
        ILogger<SyncStatusTracker> logger)
    {
        _repository = repository;
        _logger = logger;
        _cache = new ConcurrentDictionary<Guid, SyncStatus>();
    }

    public async Task<SyncStatus> GetStatusAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        // Check cache first
        if (_cache.TryGetValue(documentId, out var cachedStatus))
        {
            return cachedStatus;
        }

        // Fetch from repository
        var status = await _repository.GetAsync(documentId, ct);

        if (status == null)
        {
            status = new SyncStatus
            {
                DocumentId = documentId,
                State = SyncState.NeverSynced,
                PendingChanges = 0,
                UnresolvedConflicts = 0,
                IsSyncInProgress = false
            };

            status = await _repository.CreateAsync(status, ct);
        }

        // Update cache
        _cache.AddOrUpdate(documentId, status, (_, _) => status);

        return status;
    }

    public async Task<SyncStatus> UpdateStatusAsync(
        Guid documentId,
        SyncStatus status,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Updating sync status for document {DocumentId} to state {State}",
            documentId, status.State);

        var currentStatus = await GetStatusAsync(documentId, ct);

        // Record history if state changed
        if (currentStatus.State != status.State)
        {
            _logger.LogInformation(
                "Sync state changed for document {DocumentId}: {OldState} -> {NewState}",
                documentId, currentStatus.State, status.State);

            await _repository.UpdateAsync(status, ct);
        }
        else
        {
            // Just update metadata
            await _repository.UpdateAsync(status, ct);
        }

        // Update cache
        _cache.AddOrUpdate(documentId, status, (_, _) => status);

        return status;
    }

    public async Task<IReadOnlyList<SyncStatus>> UpdateStatusBatchAsync(
        IReadOnlyList<(Guid DocumentId, SyncStatus Status)> updates,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Batch updating sync status for {Count} documents",
            updates.Count);

        var results = new List<SyncStatus>();

        foreach (var (docId, status) in updates)
        {
            var updated = await UpdateStatusAsync(docId, status, ct);
            results.Add(updated);
        }

        return results;
    }

    public async Task<IReadOnlyList<SyncStatus>> GetStatusesAsync(
        IReadOnlyList<Guid> documentIds,
        CancellationToken ct = default)
    {
        var statuses = new List<SyncStatus>();

        foreach (var docId in documentIds)
        {
            var status = await GetStatusAsync(docId, ct);
            statuses.Add(status);
        }

        return statuses;
    }

    public async Task<IReadOnlyList<Guid>> GetDocumentsByStateAsync(
        SyncState state,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Getting documents in sync state {State}", state);

        var query = new SyncStatusQuery { State = state };
        var statuses = await _repository.QueryAsync(query, ct);

        return statuses.Select(s => s.DocumentId).ToList();
    }

    public async Task<IReadOnlyList<SyncStatusHistory>> GetStatusHistoryAsync(
        Guid documentId,
        int limit = 100,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Getting sync status history for document {DocumentId}",
            documentId);

        // This would be implemented by the repository
        // returning historical status changes
        return new List<SyncStatusHistory>();
    }

    public async Task<SyncMetrics> GetMetricsAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Computing sync metrics for document {DocumentId}",
            documentId);

        var status = await GetStatusAsync(documentId, ct);

        // Query repository for operations
        var operations = new List<SyncOperationRecord>(); // Would be fetched from repository

        var successfulOps = operations.Count(o => o.Status == SyncOperationStatus.Success);
        var failedOps = operations.Count(o => o.Status == SyncOperationStatus.Failed);

        var durations = operations
            .Where(o => o.Duration.HasValue)
            .Select(o => o.Duration!.Value)
            .ToList();

        var timeInState = status.LastSyncAt.HasValue
            ? DateTimeOffset.UtcNow - status.LastSyncAt.Value
            : TimeSpan.Zero;

        return new SyncMetrics
        {
            DocumentId = documentId,
            TotalOperations = operations.Count,
            SuccessfulOperations = successfulOps,
            FailedOperations = failedOps,
            AverageDuration = durations.Count > 0
                ? TimeSpan.FromMilliseconds(durations.Average(d => d.TotalMilliseconds))
                : TimeSpan.Zero,
            LongestDuration = durations.Count > 0 ? durations.Max() : null,
            ShortestDuration = durations.Count > 0 ? durations.Min() : null,
            LastSuccessfulSync = operations
                .Where(o => o.Status == SyncOperationStatus.Success)
                .OrderByDescending(o => o.CompletedAt)
                .FirstOrDefault()?.CompletedAt,
            LastFailedSync = operations
                .Where(o => o.Status == SyncOperationStatus.Failed)
                .OrderByDescending(o => o.CompletedAt)
                .FirstOrDefault()?.CompletedAt,
            TotalConflicts = operations.Sum(o => o.ConflictsDetected),
            ResolvedConflicts = operations.Sum(o => o.ConflictsResolved),
            UnresolvedConflicts = status.UnresolvedConflicts,
            SuccessRate = operations.Count > 0
                ? (float)successfulOps / operations.Count * 100
                : 0,
            AverageEntitiesAffected = operations.Count > 0
                ? (float)operations.Sum(o => o.EntitiesAffected) / operations.Count
                : 0,
            AverageClaimsAffected = operations.Count > 0
                ? (float)operations.Sum(o => o.ClaimsAffected) / operations.Count
                : 0,
            CurrentState = status.State,
            TimeInCurrentState = timeInState
        };
    }

    public async Task RecordSyncOperationAsync(
        Guid documentId,
        SyncOperationRecord operation,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Recording sync operation {OperationId} for document {DocumentId}",
            operation.OperationId, documentId);

        // Store operation record (implementation in repository)
        // This would be persisted for historical tracking
    }
}
```

### 4.2 Sync Status Repository

```csharp
public class SyncStatusRepository : ISyncStatusRepository
{
    private readonly IDataStore _dataStore;
    private readonly ILogger<SyncStatusRepository> _logger;

    public SyncStatusRepository(
        IDataStore dataStore,
        ILogger<SyncStatusRepository> logger)
    {
        _dataStore = dataStore;
        _logger = logger;
    }

    public async Task<SyncStatus?> GetAsync(Guid documentId, CancellationToken ct = default)
    {
        try
        {
            return await _dataStore.GetAsync<SyncStatus>(
                $"sync-status:{documentId}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get sync status for document {DocumentId}",
                documentId);

            return null;
        }
    }

    public async Task<SyncStatus> CreateAsync(SyncStatus status, CancellationToken ct = default)
    {
        await _dataStore.SetAsync(
            $"sync-status:{status.DocumentId}",
            status,
            ct);

        return status;
    }

    public async Task<SyncStatus> UpdateAsync(SyncStatus status, CancellationToken ct = default)
    {
        await _dataStore.SetAsync(
            $"sync-status:{status.DocumentId}",
            status,
            ct);

        return status;
    }

    public async Task<bool> DeleteAsync(Guid documentId, CancellationToken ct = default)
    {
        return await _dataStore.DeleteAsync(
            $"sync-status:{documentId}", ct);
    }

    public async Task<IReadOnlyList<SyncStatus>> QueryAsync(
        SyncStatusQuery query,
        CancellationToken ct = default)
    {
        var allStatuses = await _dataStore.QueryAsync<SyncStatus>(
            "sync-status:*", ct);

        var filtered = allStatuses.AsEnumerable();

        // Apply filters
        if (query.State.HasValue)
        {
            filtered = filtered.Where(s => s.State == query.State.Value);
        }

        if (query.LastSyncBefore.HasValue)
        {
            filtered = filtered.Where(s =>
                s.LastSyncAt == null || s.LastSyncAt < query.LastSyncBefore);
        }

        if (query.LastSyncAfter.HasValue)
        {
            filtered = filtered.Where(s =>
                s.LastSyncAt != null && s.LastSyncAt > query.LastSyncAfter);
        }

        if (query.MinPendingChanges.HasValue)
        {
            filtered = filtered.Where(s =>
                s.PendingChanges >= query.MinPendingChanges);
        }

        if (query.MinUnresolvedConflicts.HasValue)
        {
            filtered = filtered.Where(s =>
                s.UnresolvedConflicts >= query.MinUnresolvedConflicts);
        }

        if (query.SyncInProgress.HasValue)
        {
            filtered = filtered.Where(s =>
                s.IsSyncInProgress == query.SyncInProgress);
        }

        // Apply sorting
        filtered = query.SortOrder switch
        {
            SortOrder.ByDocumentNameAscending => filtered
                .OrderBy(s => s.DocumentId.ToString()),
            SortOrder.ByDocumentNameDescending => filtered
                .OrderByDescending(s => s.DocumentId.ToString()),
            SortOrder.ByLastSyncAscending => filtered
                .OrderBy(s => s.LastSyncAt),
            SortOrder.ByLastSyncDescending => filtered
                .OrderByDescending(s => s.LastSyncAt),
            SortOrder.ByStateAscending => filtered
                .OrderBy(s => s.State),
            SortOrder.ByStateDescending => filtered
                .OrderByDescending(s => s.State),
            SortOrder.ByPendingChangesDescending => filtered
                .OrderByDescending(s => s.PendingChanges),
            _ => filtered
        };

        // Apply pagination
        filtered = filtered
            .Skip(query.PageOffset)
            .Take(query.PageSize);

        return filtered.ToList();
    }

    public async Task<Dictionary<SyncState, int>> CountByStateAsync(CancellationToken ct = default)
    {
        var allStatuses = await _dataStore.QueryAsync<SyncStatus>(
            "sync-status:*", ct);

        return allStatuses
            .GroupBy(s => s.State)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}

public interface IDataStore
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, CancellationToken ct = default);
    Task<bool> DeleteAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<T>> QueryAsync<T>(string pattern, CancellationToken ct = default);
}
```

---

## 5. Algorithm / Flow

### Status Update Flow

```
1. Receive status update request
2. Get current status (from cache or repository)
3. If state changed:
   - Create SyncStatusHistory record
   - Log state transition
4. Update status in repository
5. Update in-memory cache
6. Return updated status
```

### Metrics Computation Flow

```
1. Get current status
2. Query repository for sync operations
3. Filter operations for the document
4. Calculate statistics:
   - Success rate: successful / total
   - Average duration: sum(duration) / count
   - Longest/shortest: max/min duration
   - Conflict stats: sum across operations
5. Calculate time in current state
6. Return SyncMetrics record
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Repository unavailable | Return cached status if available |
| Cache miss | Fetch from repository |
| Concurrent updates | Last-write-wins with conflict logging |
| Query timeout | Return partial results or empty |

---

## 7. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `GetStatus_ReturnsStatus` | Status retrieved correctly |
| `UpdateStatus_ChangesState` | State updated correctly |
| `UpdateStatusBatch_Works` | Batch updates work |
| `GetDocumentsByState_ReturnsCorrect` | Correct documents returned |
| `GetMetrics_ComputesCorrectly` | Metrics calculated correctly |
| `Cache_ImmediatelyUpdated` | Cache stays in sync |
| `StatusHistory_Recorded` | History recorded on changes |

---

## 8. Performance

| Aspect | Target |
| :----- | :----- |
| Get status | < 10ms (from cache) |
| Update status | < 100ms |
| Batch update | < 1s per 1000 updates |
| Query by state | < 500ms per 10k documents |
| Metrics computation | < 300ms |

---

## 9. License Gating

| Tier | Status Tracking | History | Metrics | Alerts |
| :--- | :--- | :--- | :--- | :--- |
| Core | No | N/A | N/A | N/A |
| WriterPro | Yes | 30 days | Basic | Manual |
| Teams | Yes | 90 days | Full | Automated |
| Enterprise | Yes | Unlimited | Advanced | Customizable |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
