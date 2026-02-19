// =============================================================================
// File: SyncStatusTracker.cs
// Project: Lexichord.Modules.Knowledge
// Description: Tracks synchronization status for documents.
// =============================================================================
// LOGIC: SyncStatusTracker maintains per-document sync state with caching,
//   history tracking, metrics computation, and event publishing. Uses a
//   repository for persistence and in-memory cache for performance.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// v0.7.6i: Enhanced with batch operations, history, metrics, and operation records.
// Dependencies: SyncStatus, SyncState (v0.7.6e), ISyncStatusRepository,
//               SyncStatusHistory, SyncOperationRecord, SyncMetrics (v0.7.6i),
//               ILicenseContext (v0.0.4c), IMediator (MediatR)
// =============================================================================

using System.Collections.Concurrent;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Status;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Status.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync;

/// <summary>
/// Service for tracking document synchronization status.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="ISyncStatusTracker"/> to maintain sync state for documents.
/// Uses a repository for persistence and an in-memory cache for fast access.
/// </para>
/// <para>
/// <b>Features (v0.7.6i):</b>
/// </para>
/// <list type="bullet">
///   <item><b>Caching:</b> In-memory cache for fast status lookups.</item>
///   <item><b>Persistence:</b> Repository-backed storage for durability.</item>
///   <item><b>History:</b> Automatic history recording on state changes.</item>
///   <item><b>Events:</b> MediatR event publishing on state transitions.</item>
///   <item><b>Metrics:</b> Aggregated metrics from operation records.</item>
///   <item><b>License Gating:</b> Feature access controlled by license tier.</item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This service is thread-safe and can be used from
/// multiple concurrent sync operations.
/// </para>
/// <para>
/// <b>License Requirements:</b>
/// </para>
/// <list type="bullet">
///   <item>Core: No access (throws UnauthorizedAccessException).</item>
///   <item>WriterPro: Basic status operations, 30-day history.</item>
///   <item>Teams: Full operations including batch and metrics, 90-day history.</item>
///   <item>Enterprise: All features with unlimited history.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// <b>Enhanced in:</b> v0.7.6i with batch operations, history, and metrics.
/// </para>
/// </remarks>
public sealed class SyncStatusTracker : ISyncStatusTracker
{
    // LOGIC: ConcurrentDictionary for thread-safe in-memory cache.
    // Provides fast access while repository provides persistence.
    private readonly ConcurrentDictionary<Guid, SyncStatus> _statusCache = new();

    private readonly ISyncStatusRepository _repository;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<SyncStatusTracker> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SyncStatusTracker"/>.
    /// </summary>
    /// <param name="repository">The sync status repository for persistence.</param>
    /// <param name="licenseContext">The license context for feature gating.</param>
    /// <param name="mediator">The MediatR mediator for event publishing.</param>
    /// <param name="logger">The logger instance.</param>
    public SyncStatusTracker(
        ISyncStatusRepository repository,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<SyncStatusTracker> logger)
    {
        _repository = repository;
        _licenseContext = licenseContext;
        _mediator = mediator;
        _logger = logger;

        _logger.LogDebug("SyncStatusTracker initialized");
    }

    #region Basic Operations (v0.7.6e)

    /// <inheritdoc/>
    public async Task<SyncStatus> GetStatusAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogTrace(
            "Getting sync status for document {DocumentId}",
            documentId);

        // LOGIC: Check cache first for fast access.
        if (_statusCache.TryGetValue(documentId, out var cachedStatus))
        {
            _logger.LogTrace(
                "Cache hit for document {DocumentId}",
                documentId);
            return cachedStatus;
        }

        // LOGIC: Cache miss - fetch from repository.
        var status = await _repository.GetAsync(documentId, ct);

        if (status == null)
        {
            // LOGIC: Create default status for new documents.
            _logger.LogDebug(
                "Creating default sync status for document {DocumentId}",
                documentId);

            status = new SyncStatus
            {
                DocumentId = documentId,
                State = SyncState.NeverSynced,
                LastSyncAt = null,
                PendingChanges = 0,
                LastAttemptAt = null,
                LastError = null,
                UnresolvedConflicts = 0,
                IsSyncInProgress = false
            };

            // LOGIC: Persist the new default status.
            status = await _repository.CreateAsync(status, ct);
        }

        // LOGIC: Update cache with fetched/created status.
        _statusCache.AddOrUpdate(documentId, status, (_, _) => status);

        return status;
    }

    /// <inheritdoc/>
    public async Task<SyncStatus> UpdateStatusAsync(
        Guid documentId,
        SyncStatus status,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // LOGIC: Validate that the status matches the document ID.
        if (status.DocumentId != documentId)
        {
            throw new ArgumentException(
                $"Status DocumentId ({status.DocumentId}) does not match provided documentId ({documentId})",
                nameof(status));
        }

        _logger.LogDebug(
            "Updating sync status for document {DocumentId}: State={State}, " +
            "IsSyncInProgress={InProgress}, UnresolvedConflicts={Conflicts}",
            documentId, status.State, status.IsSyncInProgress, status.UnresolvedConflicts);

        // LOGIC: Get current status to detect state changes.
        var currentStatus = await GetStatusAsync(documentId, ct);
        var stateChanged = currentStatus.State != status.State;

        // LOGIC: Record history if state changed.
        if (stateChanged)
        {
            _logger.LogInformation(
                "Sync state changed for document {DocumentId}: {OldState} -> {NewState}",
                documentId, currentStatus.State, status.State);

            var history = new SyncStatusHistory
            {
                HistoryId = Guid.NewGuid(),
                DocumentId = documentId,
                PreviousState = currentStatus.State,
                NewState = status.State,
                ChangedAt = DateTimeOffset.UtcNow,
                Reason = $"State transition: {currentStatus.State} -> {status.State}"
            };

            await _repository.AddHistoryAsync(history, ct);
        }

        // LOGIC: Persist to repository.
        status = await _repository.UpdateAsync(status, ct);

        // LOGIC: Update cache atomically.
        _statusCache.AddOrUpdate(documentId, status, (_, _) => status);

        // LOGIC: Publish event if state changed.
        if (stateChanged)
        {
            try
            {
                var eventToPublish = SyncStatusUpdatedEvent.Create(
                    documentId,
                    currentStatus.State,
                    status.State);

                await _mediator.Publish(eventToPublish, ct);

                _logger.LogDebug(
                    "Published SyncStatusUpdatedEvent for document {DocumentId}",
                    documentId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // LOGIC: Log but don't fail the update if event publishing fails.
                _logger.LogWarning(ex,
                    "Failed to publish SyncStatusUpdatedEvent for document {DocumentId}",
                    documentId);
            }
        }

        return status;
    }

    #endregion

    #region Batch Operations (v0.7.6i)

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncStatus>> UpdateStatusBatchAsync(
        IReadOnlyList<(Guid DocumentId, SyncStatus Status)> updates,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // LOGIC: License check for batch operations (Teams+).
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.SyncStatusTracker))
        {
            _logger.LogWarning(
                "Batch update requires SyncStatusTracker feature (Teams+ license)");
            throw new UnauthorizedAccessException(
                "Batch status updates require Teams or higher license tier.");
        }

        _logger.LogDebug(
            "Batch updating sync status for {Count} documents",
            updates.Count);

        var results = new List<SyncStatus>(updates.Count);

        // LOGIC: Process updates sequentially to maintain consistency.
        // Each update records history and publishes events as needed.
        foreach (var (docId, status) in updates)
        {
            ct.ThrowIfCancellationRequested();

            var updated = await UpdateStatusAsync(docId, status, ct);
            results.Add(updated);
        }

        _logger.LogInformation(
            "Batch updated {Count} document statuses",
            results.Count);

        return results;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncStatus>> GetStatusesAsync(
        IReadOnlyList<Guid> documentIds,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Getting sync statuses for {Count} documents",
            documentIds.Count);

        var statuses = new List<SyncStatus>(documentIds.Count);

        // LOGIC: Fetch each status, leveraging cache when available.
        foreach (var docId in documentIds)
        {
            ct.ThrowIfCancellationRequested();

            var status = await GetStatusAsync(docId, ct);
            statuses.Add(status);
        }

        return statuses;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Guid>> GetDocumentsByStateAsync(
        SyncState state,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Getting documents in sync state {State}",
            state);

        var query = new SyncStatusQuery
        {
            State = state,
            PageSize = 10000 // Large limit for state queries
        };

        var statuses = await _repository.QueryAsync(query, ct);
        var documentIds = statuses.Select(s => s.DocumentId).ToList();

        _logger.LogDebug(
            "Found {Count} documents in state {State}",
            documentIds.Count, state);

        return documentIds;
    }

    #endregion

    #region History Operations (v0.7.6i)

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncStatusHistory>> GetStatusHistoryAsync(
        Guid documentId,
        int limit = 100,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Getting sync status history for document {DocumentId} with limit {Limit}",
            documentId, limit);

        // LOGIC: Apply license-based retention limits.
        var effectiveLimit = ApplyHistoryRetentionLimit(limit);

        var history = await _repository.GetHistoryAsync(documentId, effectiveLimit, ct);

        _logger.LogDebug(
            "Retrieved {Count} history entries for document {DocumentId}",
            history.Count, documentId);

        return history;
    }

    /// <summary>
    /// Applies license-tier-based history retention limits.
    /// </summary>
    /// <param name="requestedLimit">The requested limit.</param>
    /// <returns>The effective limit based on license tier.</returns>
    private int ApplyHistoryRetentionLimit(int requestedLimit)
    {
        // LOGIC: License tiers have different history retention:
        // - WriterPro: 30 days worth of history (estimated ~100 entries)
        // - Teams: 90 days worth of history (estimated ~300 entries)
        // - Enterprise: Unlimited

        var tier = _licenseContext.Tier;

        return tier switch
        {
            LicenseTier.Enterprise => requestedLimit,
            LicenseTier.Teams => Math.Min(requestedLimit, 300),
            _ => Math.Min(requestedLimit, 100)
        };
    }

    #endregion

    #region Metrics Operations (v0.7.6i)

    /// <inheritdoc/>
    public async Task<SyncMetrics> GetMetricsAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Computing sync metrics for document {DocumentId}",
            documentId);

        // LOGIC: Get current status for state information.
        var status = await GetStatusAsync(documentId, ct);

        // LOGIC: Get operation records for metrics computation.
        var operations = await _repository.GetOperationRecordsAsync(documentId, 1000, ct);

        // LOGIC: Compute metrics from operation records.
        var successfulOps = operations.Count(o =>
            o.Status == SyncOperationStatus.Success ||
            o.Status == SyncOperationStatus.SuccessWithConflicts ||
            o.Status == SyncOperationStatus.PartialSuccess);

        var failedOps = operations.Count(o => o.Status == SyncOperationStatus.Failed);

        var durations = operations
            .Where(o => o.Duration.HasValue)
            .Select(o => o.Duration!.Value)
            .ToList();

        // LOGIC: Calculate time in current state from last history entry.
        var history = await _repository.GetHistoryAsync(documentId, 1, ct);
        var lastStateChange = history.FirstOrDefault()?.ChangedAt ?? DateTimeOffset.UtcNow;
        var timeInState = DateTimeOffset.UtcNow - lastStateChange;

        var metrics = new SyncMetrics
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

        _logger.LogDebug(
            "Computed metrics for document {DocumentId}: " +
            "TotalOps={Total}, SuccessRate={Rate:F1}%",
            documentId, metrics.TotalOperations, metrics.SuccessRate);

        return metrics;
    }

    /// <inheritdoc/>
    public async Task RecordSyncOperationAsync(
        Guid documentId,
        SyncOperationRecord operation,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Recording sync operation {OperationId} for document {DocumentId}: " +
            "Direction={Direction}, Status={Status}",
            operation.OperationId, documentId, operation.Direction, operation.Status);

        // LOGIC: Validate operation document ID matches.
        if (operation.DocumentId != documentId)
        {
            throw new ArgumentException(
                $"Operation DocumentId ({operation.DocumentId}) does not match provided documentId ({documentId})",
                nameof(operation));
        }

        await _repository.AddOperationRecordAsync(operation, ct);

        _logger.LogInformation(
            "Recorded sync operation {OperationId} for document {DocumentId}: " +
            "Status={Status}, Duration={Duration}",
            operation.OperationId, documentId, operation.Status, operation.Duration);
    }

    #endregion
}
