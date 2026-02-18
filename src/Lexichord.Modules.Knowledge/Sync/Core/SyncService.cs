// =============================================================================
// File: SyncService.cs
// Project: Lexichord.Modules.Knowledge
// Description: Main sync service orchestrating document-graph synchronization.
// =============================================================================
// LOGIC: SyncService is the primary entry point for sync operations. It:
//   - Validates license tier before sync operations
//   - Delegates to ISyncOrchestrator for pipeline execution
//   - Tracks sync status via ISyncStatusTracker
//   - Resolves conflicts via IConflictResolver
//   - Publishes SyncCompletedEvent via MediatR
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// Dependencies: ISyncOrchestrator, ISyncStatusTracker, IConflictResolver,
//               ILicenseContext, IMediator (MediatR), ILogger<T>
// =============================================================================

using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;
using Lexichord.Abstractions.Contracts.RAG;
using MediatR;
using Microsoft.Extensions.Logging;

// LOGIC: Alias to disambiguate from Lexichord.Abstractions.Contracts.IConflictResolver
using ISyncConflictResolver = Lexichord.Abstractions.Contracts.Knowledge.Sync.IConflictResolver;

namespace Lexichord.Modules.Knowledge.Sync.Core;

/// <summary>
/// Main sync service for document-graph synchronization.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="ISyncService"/> to provide high-level sync operations.
/// This is the primary entry point for sync functionality.
/// </para>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item>Core: No sync access (throws <see cref="UnauthorizedAccessException"/>).</item>
///   <item>WriterPro: Document-to-graph sync only.</item>
///   <item>Teams+: Full bidirectional sync with all features.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public sealed class SyncService : ISyncService
{
    private readonly ISyncOrchestrator _orchestrator;
    private readonly ISyncStatusTracker _statusTracker;
    private readonly ISyncConflictResolver _conflictResolver;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<SyncService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SyncService"/>.
    /// </summary>
    /// <param name="orchestrator">The sync orchestrator for pipeline execution.</param>
    /// <param name="statusTracker">The status tracker for sync state management.</param>
    /// <param name="conflictResolver">The conflict resolver for handling conflicts.</param>
    /// <param name="licenseContext">The license context for tier validation.</param>
    /// <param name="mediator">The MediatR mediator for event publishing.</param>
    /// <param name="logger">The logger instance.</param>
    public SyncService(
        ISyncOrchestrator orchestrator,
        ISyncStatusTracker statusTracker,
        ISyncConflictResolver conflictResolver,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<SyncService> logger)
    {
        // LOGIC: Store all dependencies for sync orchestration.
        // All dependencies are required — null checks handled by DI container.
        _orchestrator = orchestrator;
        _statusTracker = statusTracker;
        _conflictResolver = conflictResolver;
        _licenseContext = licenseContext;
        _mediator = mediator;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SyncResult> SyncDocumentToGraphAsync(
        Document document,
        SyncContext context,
        CancellationToken ct = default)
    {
        // LOGIC: Measure total operation duration for logging and result.
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting document-to-graph sync for document {DocumentId}",
                document.Id);

            // LOGIC: Validate license tier supports document-to-graph sync.
            // Core tier cannot sync. WriterPro+ can perform doc-to-graph.
            if (!CanPerformSync(SyncDirection.DocumentToGraph))
            {
                _logger.LogWarning(
                    "License tier {Tier} does not support document-to-graph sync",
                    _licenseContext.Tier);
                throw new UnauthorizedAccessException(
                    $"License tier {_licenseContext.Tier} does not support document-to-graph sync. " +
                    "Upgrade to WriterPro or higher to access sync features.");
            }

            // LOGIC: Update status to indicate sync is in progress.
            // This prevents concurrent syncs and shows UI indicator.
            await _statusTracker.UpdateStatusAsync(
                document.Id,
                new SyncStatus
                {
                    DocumentId = document.Id,
                    State = SyncState.PendingSync,
                    IsSyncInProgress = true,
                    LastAttemptAt = DateTimeOffset.UtcNow
                },
                ct);

            // LOGIC: Delegate to orchestrator for actual sync pipeline execution.
            // The orchestrator handles extraction, conflict detection, and graph operations.
            var result = await _orchestrator.ExecuteDocumentToGraphAsync(document, context, ct);

            // LOGIC: Record duration in the result.
            stopwatch.Stop();
            result = result with { Duration = stopwatch.Elapsed };

            // LOGIC: Determine new sync state based on result status.
            // Success → InSync, SuccessWithConflicts → NeedsReview, else → Conflict.
            var newState = result.Status switch
            {
                SyncOperationStatus.Success => SyncState.InSync,
                SyncOperationStatus.SuccessWithConflicts => SyncState.NeedsReview,
                SyncOperationStatus.NoChanges => SyncState.InSync,
                _ => SyncState.Conflict
            };

            // LOGIC: Update status with sync outcome.
            await _statusTracker.UpdateStatusAsync(
                document.Id,
                new SyncStatus
                {
                    DocumentId = document.Id,
                    State = newState,
                    LastSyncAt = result.Status == SyncOperationStatus.Success ||
                                 result.Status == SyncOperationStatus.NoChanges
                        ? DateTimeOffset.UtcNow
                        : null,
                    IsSyncInProgress = false,
                    UnresolvedConflicts = result.Conflicts.Count,
                    LastError = result.ErrorMessage
                },
                ct);

            // LOGIC: Publish completion event if configured.
            // Events notify UI and other listeners about sync outcome.
            if (context.PublishEvents)
            {
                await _mediator.Publish(
                    SyncCompletedEvent.Create(
                        document.Id,
                        result,
                        SyncDirection.DocumentToGraph),
                    ct);
            }

            _logger.LogInformation(
                "Document-to-graph sync completed for {DocumentId} in {Duration}ms with status {Status}. " +
                "Entities: {EntityCount}, Claims: {ClaimCount}, Conflicts: {ConflictCount}",
                document.Id,
                stopwatch.ElapsedMilliseconds,
                result.Status,
                result.EntitiesAffected.Count,
                result.ClaimsAffected.Count,
                result.Conflicts.Count);

            return result;
        }
        catch (OperationCanceledException)
        {
            // LOGIC: User cancelled the operation. Log and update status.
            _logger.LogInformation(
                "Document-to-graph sync cancelled for {DocumentId}",
                document.Id);

            await _statusTracker.UpdateStatusAsync(
                document.Id,
                new SyncStatus
                {
                    DocumentId = document.Id,
                    State = SyncState.PendingSync,
                    IsSyncInProgress = false,
                    LastAttemptAt = DateTimeOffset.UtcNow,
                    LastError = "Sync cancelled by user"
                },
                ct);

            throw;
        }
        catch (TimeoutException ex)
        {
            // LOGIC: Operation timed out. Log error and update status.
            _logger.LogError(ex,
                "Document-to-graph sync timed out for {DocumentId} after {Timeout}",
                document.Id, context.Timeout);

            await UpdateErrorStatusAsync(document.Id, ex.Message, ct);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            // LOGIC: License check failed. Re-throw without status update.
            throw;
        }
        catch (Exception ex)
        {
            // LOGIC: Unexpected error. Log and update status with error details.
            _logger.LogError(ex,
                "Document-to-graph sync failed for {DocumentId}",
                document.Id);

            await UpdateErrorStatusAsync(document.Id, ex.Message, ct);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Document>> GetAffectedDocumentsAsync(
        GraphChange change,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Finding documents affected by graph change to entity {EntityId} ({ChangeType})",
            change.EntityId, change.ChangeType);

        // LOGIC: Graph-to-document requires Teams tier.
        // WriterPro users cannot receive graph change propagation.
        if (!CanPerformSync(SyncDirection.GraphToDocument))
        {
            _logger.LogDebug(
                "License tier {Tier} does not support graph-to-document propagation",
                _licenseContext.Tier);
            return [];
        }

        // LOGIC: Delegate to orchestrator to find affected documents.
        var results = await _orchestrator.ExecuteGraphToDocumentAsync(change, ct);

        // LOGIC: Filter to documents that actually have changes.
        // NoChanges results indicate documents that reference the entity but weren't affected.
        var affectedDocuments = results
            .Where(r => r.Status != SyncOperationStatus.NoChanges)
            .Select(r => r.EntitiesAffected.Count > 0
                ? new Document(
                    Guid.Empty, // Placeholder - actual document retrieved by orchestrator
                    Guid.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    DocumentStatus.Indexed,
                    null,
                    null)
                : null)
            .Where(d => d is not null)
            .Cast<Document>()
            .ToList();

        _logger.LogDebug(
            "Found {Count} documents affected by graph change to entity {EntityId}",
            affectedDocuments.Count, change.EntityId);

        return affectedDocuments;
    }

    /// <inheritdoc/>
    public async Task<SyncStatus> GetSyncStatusAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        // LOGIC: Direct delegation to status tracker.
        // No license check — status query is available to all tiers.
        return await _statusTracker.GetStatusAsync(documentId, ct);
    }

    /// <inheritdoc/>
    public async Task<SyncResult> ResolveConflictAsync(
        Guid documentId,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Resolving conflicts for document {DocumentId} using strategy {Strategy}",
            documentId, strategy);

        // LOGIC: Get current status to check if there are conflicts to resolve.
        var status = await _statusTracker.GetStatusAsync(documentId, ct);

        if (status.State != SyncState.Conflict && status.UnresolvedConflicts == 0)
        {
            _logger.LogDebug(
                "No conflicts to resolve for document {DocumentId}",
                documentId);

            return new SyncResult
            {
                Status = SyncOperationStatus.NoChanges,
                Duration = TimeSpan.Zero
            };
        }

        // LOGIC: Delegate to conflict resolver for actual resolution.
        var result = await _conflictResolver.ResolveAsync(documentId, strategy, ct);

        // LOGIC: Update status based on resolution outcome.
        var newState = result.Status == SyncOperationStatus.Success
            ? SyncState.InSync
            : result.Conflicts.Count > 0
                ? SyncState.Conflict
                : SyncState.NeedsReview;

        await _statusTracker.UpdateStatusAsync(
            documentId,
            status with
            {
                State = newState,
                UnresolvedConflicts = result.Conflicts.Count,
                LastError = result.ErrorMessage
            },
            ct);

        _logger.LogInformation(
            "Conflict resolution completed for {DocumentId}. Status: {Status}, Remaining conflicts: {Count}",
            documentId, result.Status, result.Conflicts.Count);

        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> NeedsSyncAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        // LOGIC: Quick check for sync necessity without full status retrieval.
        var status = await _statusTracker.GetStatusAsync(documentId, ct);

        // LOGIC: Document needs sync if:
        // - State is PendingSync (changes since last sync)
        // - State is Conflict (conflicts need resolution)
        // - State is NeverSynced (never been synced)
        // - Has pending changes
        return status.State == SyncState.PendingSync ||
               status.State == SyncState.Conflict ||
               status.State == SyncState.NeverSynced ||
               status.PendingChanges > 0;
    }

    /// <summary>
    /// Checks if the current license tier can perform the specified sync direction.
    /// </summary>
    /// <param name="direction">The sync direction to check.</param>
    /// <returns>True if sync is allowed, false otherwise.</returns>
    private bool CanPerformSync(SyncDirection direction)
    {
        // LOGIC: License gating based on specification:
        // - Core: No sync access
        // - WriterPro: DocumentToGraph only
        // - Teams+: All directions
        return (_licenseContext.Tier, direction) switch
        {
            (LicenseTier.Core, _) => false,
            (LicenseTier.WriterPro, SyncDirection.DocumentToGraph) => true,
            (LicenseTier.WriterPro, _) => false,
            (LicenseTier.Teams, _) => true,
            (LicenseTier.Enterprise, _) => true,
            _ => false
        };
    }

    /// <summary>
    /// Updates document status to reflect an error state.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task UpdateErrorStatusAsync(
        Guid documentId,
        string errorMessage,
        CancellationToken ct)
    {
        try
        {
            await _statusTracker.UpdateStatusAsync(
                documentId,
                new SyncStatus
                {
                    DocumentId = documentId,
                    State = SyncState.Conflict,
                    IsSyncInProgress = false,
                    LastAttemptAt = DateTimeOffset.UtcNow,
                    LastError = errorMessage
                },
                ct);
        }
        catch (Exception ex)
        {
            // LOGIC: Don't let status update failure mask the original error.
            _logger.LogWarning(ex,
                "Failed to update error status for document {DocumentId}",
                documentId);
        }
    }
}
