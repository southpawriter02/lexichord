// =============================================================================
// File: SyncStatusTracker.cs
// Project: Lexichord.Modules.Knowledge
// Description: Tracks synchronization status for documents.
// =============================================================================
// LOGIC: SyncStatusTracker maintains per-document sync state in memory with
//   optional persistence. It tracks last sync time, pending changes, conflicts,
//   and whether a sync is currently in progress.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// Dependencies: SyncStatus, SyncState (v0.7.6e)
// =============================================================================

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync;

/// <summary>
/// Service for tracking document synchronization status.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="ISyncStatusTracker"/> to maintain sync state for documents.
/// Uses a thread-safe concurrent dictionary for in-memory storage.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This service is thread-safe and can be used from
/// multiple concurrent sync operations.
/// </para>
/// <para>
/// <b>Persistence:</b> Currently in-memory only. Status is lost on application
/// restart. Future versions may add persistence via PostgreSQL.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public sealed class SyncStatusTracker : ISyncStatusTracker
{
    // LOGIC: ConcurrentDictionary for thread-safe status storage.
    // Key is DocumentId, value is the latest SyncStatus.
    private readonly ConcurrentDictionary<Guid, SyncStatus> _statusCache = new();
    private readonly ILogger<SyncStatusTracker> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SyncStatusTracker"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public SyncStatusTracker(ILogger<SyncStatusTracker> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<SyncStatus> GetStatusAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        // LOGIC: Return cached status or create default for new documents.
        // Default state is NeverSynced indicating document hasn't been synced yet.
        var status = _statusCache.GetOrAdd(
            documentId,
            id =>
            {
                _logger.LogDebug(
                    "Creating default sync status for document {DocumentId}",
                    id);

                return new SyncStatus
                {
                    DocumentId = id,
                    State = SyncState.NeverSynced,
                    LastSyncAt = null,
                    PendingChanges = 0,
                    LastAttemptAt = null,
                    LastError = null,
                    UnresolvedConflicts = 0,
                    IsSyncInProgress = false
                };
            });

        return Task.FromResult(status);
    }

    /// <inheritdoc/>
    public Task UpdateStatusAsync(
        Guid documentId,
        SyncStatus status,
        CancellationToken ct = default)
    {
        // LOGIC: Validate that the status matches the document ID.
        if (status.DocumentId != documentId)
        {
            throw new ArgumentException(
                $"Status DocumentId ({status.DocumentId}) does not match provided documentId ({documentId})",
                nameof(status));
        }

        // LOGIC: Update the cached status atomically.
        _statusCache.AddOrUpdate(
            documentId,
            status,
            (_, _) => status);

        _logger.LogDebug(
            "Updated sync status for document {DocumentId}: State={State}, " +
            "IsSyncInProgress={InProgress}, UnresolvedConflicts={Conflicts}",
            documentId, status.State, status.IsSyncInProgress, status.UnresolvedConflicts);

        return Task.CompletedTask;
    }
}
