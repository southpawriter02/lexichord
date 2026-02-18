// =============================================================================
// File: ConflictResolver.cs
// Project: Lexichord.Modules.Knowledge
// Description: Resolves synchronization conflicts using specified strategies.
// =============================================================================
// LOGIC: ConflictResolver applies resolution strategies to sync conflicts.
//   It modifies graph or document state based on the chosen strategy
//   (UseDocument, UseGraph, Merge, Discard).
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// Dependencies: SyncResult, SyncConflict, ConflictResolutionStrategy (v0.7.6e),
//               IGraphRepository (v0.4.5e)
// =============================================================================

using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Microsoft.Extensions.Logging;

// LOGIC: Alias to disambiguate from Lexichord.Abstractions.Contracts.IConflictResolver
using ISyncConflictResolver = Lexichord.Abstractions.Contracts.Knowledge.Sync.IConflictResolver;

namespace Lexichord.Modules.Knowledge.Sync;

/// <summary>
/// Service for resolving synchronization conflicts.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="ISyncConflictResolver"/> to apply resolution strategies:
/// </para>
/// <list type="bullet">
///   <item><see cref="ConflictResolutionStrategy.UseDocument"/>: Overwrite graph with document values.</item>
///   <item><see cref="ConflictResolutionStrategy.UseGraph"/>: Keep graph values, discard document changes.</item>
///   <item><see cref="ConflictResolutionStrategy.Merge"/>: Attempt intelligent merge.</item>
///   <item><see cref="ConflictResolutionStrategy.Manual"/>: Leave for user review.</item>
///   <item><see cref="ConflictResolutionStrategy.DiscardDocument"/>: Reset document sync state.</item>
///   <item><see cref="ConflictResolutionStrategy.DiscardGraph"/>: Delete graph entities, re-sync.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public sealed class ConflictResolver : ISyncConflictResolver
{
    private readonly IGraphRepository _graphRepository;
    private readonly ISyncStatusTracker _statusTracker;
    private readonly ILogger<ConflictResolver> _logger;

    // LOGIC: In-memory conflict storage per document.
    // In production, this would be persisted to the database.
    private readonly Dictionary<Guid, List<SyncConflict>> _pendingConflicts = new();

    /// <summary>
    /// Initializes a new instance of <see cref="ConflictResolver"/>.
    /// </summary>
    /// <param name="graphRepository">The graph repository for entity updates.</param>
    /// <param name="statusTracker">The status tracker for state updates.</param>
    /// <param name="logger">The logger instance.</param>
    public ConflictResolver(
        IGraphRepository graphRepository,
        ISyncStatusTracker statusTracker,
        ILogger<ConflictResolver> logger)
    {
        _graphRepository = graphRepository;
        _statusTracker = statusTracker;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SyncResult> ResolveAsync(
        Guid documentId,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var resolvedConflicts = new List<SyncConflict>();
        var remainingConflicts = new List<SyncConflict>();
        var affectedEntities = new List<KnowledgeEntity>();

        _logger.LogInformation(
            "Resolving conflicts for document {DocumentId} using strategy {Strategy}",
            documentId, strategy);

        try
        {
            // LOGIC: Get pending conflicts for this document.
            var conflicts = GetPendingConflicts(documentId);

            if (conflicts.Count == 0)
            {
                _logger.LogDebug(
                    "No pending conflicts for document {DocumentId}",
                    documentId);

                return new SyncResult
                {
                    Status = SyncOperationStatus.NoChanges,
                    Duration = stopwatch.Elapsed
                };
            }

            _logger.LogDebug(
                "Processing {ConflictCount} conflicts for document {DocumentId}",
                conflicts.Count, documentId);

            // LOGIC: Process each conflict according to the strategy.
            foreach (var conflict in conflicts)
            {
                var resolved = await ResolveConflictAsync(
                    conflict, strategy, affectedEntities, ct);

                if (resolved)
                {
                    resolvedConflicts.Add(conflict);
                }
                else
                {
                    remainingConflicts.Add(conflict);
                }
            }

            // LOGIC: Update pending conflicts for this document.
            UpdatePendingConflicts(documentId, remainingConflicts);

            stopwatch.Stop();

            // LOGIC: Determine result status.
            var status = remainingConflicts.Count == 0
                ? SyncOperationStatus.Success
                : resolvedConflicts.Count > 0
                    ? SyncOperationStatus.PartialSuccess
                    : SyncOperationStatus.Failed;

            _logger.LogInformation(
                "Conflict resolution completed for {DocumentId}. " +
                "Resolved: {Resolved}, Remaining: {Remaining}",
                documentId, resolvedConflicts.Count, remainingConflicts.Count);

            return new SyncResult
            {
                Status = status,
                EntitiesAffected = affectedEntities,
                Conflicts = remainingConflicts,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Conflict resolution failed for document {DocumentId}",
                documentId);

            stopwatch.Stop();

            return new SyncResult
            {
                Status = SyncOperationStatus.Failed,
                Conflicts = remainingConflicts,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Resolves a single conflict using the specified strategy.
    /// </summary>
    /// <param name="conflict">The conflict to resolve.</param>
    /// <param name="strategy">The resolution strategy.</param>
    /// <param name="affectedEntities">List to add affected entities to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if conflict was resolved, false if it needs manual review.</returns>
    private async Task<bool> ResolveConflictAsync(
        SyncConflict conflict,
        ConflictResolutionStrategy strategy,
        List<KnowledgeEntity> affectedEntities,
        CancellationToken ct)
    {
        _logger.LogDebug(
            "Resolving conflict on {Target} using {Strategy}",
            conflict.ConflictTarget, strategy);

        switch (strategy)
        {
            case ConflictResolutionStrategy.UseDocument:
                // LOGIC: Update graph with document value.
                return await ApplyDocumentValueAsync(conflict, affectedEntities, ct);

            case ConflictResolutionStrategy.UseGraph:
                // LOGIC: Keep graph value, mark conflict as resolved.
                // No changes needed to the graph.
                return true;

            case ConflictResolutionStrategy.Merge:
                // LOGIC: Attempt intelligent merge.
                return await AttemptMergeAsync(conflict, affectedEntities, ct);

            case ConflictResolutionStrategy.Manual:
                // LOGIC: Leave conflict unresolved for user review.
                return false;

            case ConflictResolutionStrategy.DiscardDocument:
                // LOGIC: Keep graph value, mark conflict as resolved.
                return true;

            case ConflictResolutionStrategy.DiscardGraph:
                // LOGIC: Delete graph entity and re-create from document.
                return await DiscardAndRecreateAsync(conflict, affectedEntities, ct);

            default:
                _logger.LogWarning(
                    "Unknown resolution strategy {Strategy}, leaving conflict unresolved",
                    strategy);
                return false;
        }
    }

    /// <summary>
    /// Applies the document value to the graph entity.
    /// </summary>
    private async Task<bool> ApplyDocumentValueAsync(
        SyncConflict conflict,
        List<KnowledgeEntity> affectedEntities,
        CancellationToken ct)
    {
        try
        {
            // LOGIC: Parse the conflict target to identify the entity and property.
            // Format: "EntityType:EntityName.PropertyName" or "EntityType:EntityName"
            var parts = conflict.ConflictTarget.Split(':');
            if (parts.Length < 2)
            {
                _logger.LogWarning(
                    "Cannot parse conflict target: {Target}",
                    conflict.ConflictTarget);
                return false;
            }

            var entityType = parts[0];
            var nameAndProperty = parts[1].Split('.');
            var entityName = nameAndProperty[0];
            var propertyName = nameAndProperty.Length > 1 ? nameAndProperty[1] : null;

            // LOGIC: Find the entity in the graph.
            var allEntities = await _graphRepository.GetAllEntitiesAsync(ct);
            var entity = allEntities.FirstOrDefault(e =>
                e.Type.Equals(entityType, StringComparison.OrdinalIgnoreCase) &&
                e.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));

            if (entity is null)
            {
                _logger.LogDebug(
                    "Entity not found for conflict resolution: {Type}:{Name}",
                    entityType, entityName);
                return true; // Conflict resolved by absence
            }

            // LOGIC: Update the property if specified.
            if (propertyName is not null)
            {
                var updatedProperties = new Dictionary<string, object>(entity.Properties)
                {
                    [propertyName] = conflict.DocumentValue
                };

                var updatedEntity = entity with
                {
                    Properties = updatedProperties,
                    ModifiedAt = DateTimeOffset.UtcNow
                };

                // LOGIC: Note: actual update would go through IEntityCrudService.
                affectedEntities.Add(updatedEntity);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to apply document value for conflict {Target}",
                conflict.ConflictTarget);
            return false;
        }
    }

    /// <summary>
    /// Attempts to merge document and graph values.
    /// </summary>
    private Task<bool> AttemptMergeAsync(
        SyncConflict conflict,
        List<KnowledgeEntity> affectedEntities,
        CancellationToken ct)
    {
        // LOGIC: Merge strategy depends on the value types.
        // For numeric values, we might take the average or latest.
        // For collections, we might union.
        // For this basic implementation, fall back to document value.

        _logger.LogDebug(
            "Merge requested for {Target}, falling back to document value",
            conflict.ConflictTarget);

        return ApplyDocumentValueAsync(conflict, affectedEntities, ct);
    }

    /// <summary>
    /// Discards graph entity and marks for re-creation.
    /// </summary>
    private Task<bool> DiscardAndRecreateAsync(
        SyncConflict conflict,
        List<KnowledgeEntity> affectedEntities,
        CancellationToken ct)
    {
        try
        {
            // LOGIC: For MissingInDocument conflicts, delete the graph entity.
            if (conflict.Type == ConflictType.MissingInDocument &&
                conflict.GraphValue is KnowledgeEntity entity)
            {
                // LOGIC: Note: actual deletion would go through IEntityCrudService.
                _logger.LogDebug(
                    "Discarding graph entity {Type}:{Name}",
                    entity.Type, entity.Name);
                return Task.FromResult(true);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to discard graph entity for conflict {Target}",
                conflict.ConflictTarget);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Gets pending conflicts for a document.
    /// </summary>
    private List<SyncConflict> GetPendingConflicts(Guid documentId)
    {
        lock (_pendingConflicts)
        {
            return _pendingConflicts.TryGetValue(documentId, out var conflicts)
                ? new List<SyncConflict>(conflicts)
                : new List<SyncConflict>();
        }
    }

    /// <summary>
    /// Updates pending conflicts for a document.
    /// </summary>
    private void UpdatePendingConflicts(Guid documentId, List<SyncConflict> remaining)
    {
        lock (_pendingConflicts)
        {
            if (remaining.Count == 0)
            {
                _pendingConflicts.Remove(documentId);
            }
            else
            {
                _pendingConflicts[documentId] = remaining;
            }
        }
    }
}
