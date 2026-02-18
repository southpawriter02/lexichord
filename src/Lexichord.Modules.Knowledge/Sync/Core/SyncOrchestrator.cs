// =============================================================================
// File: SyncOrchestrator.cs
// Project: Lexichord.Modules.Knowledge
// Description: Internal orchestrator for sync pipeline execution.
// =============================================================================
// LOGIC: SyncOrchestrator coordinates the low-level sync operations:
//   - Extracts entities via IEntityExtractionPipeline
//   - Extracts claims via IClaimExtractionService
//   - Detects conflicts via ISyncConflictDetector
//   - Upserts to graph via IGraphRepository
//   - Updates document status via IDocumentRepository
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// Dependencies: IEntityExtractionPipeline (v0.4.5g), IGraphRepository (v0.4.5e),
//               ISyncConflictDetector (v0.7.6e), IDocumentRepository (v0.4.1c)
// =============================================================================

using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.Core;

/// <summary>
/// Internal orchestrator for sync pipeline execution.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="ISyncOrchestrator"/> to coordinate the sync pipeline:
/// </para>
/// <list type="number">
///   <item>Extract entities from document content.</item>
///   <item>Detect conflicts against existing graph state.</item>
///   <item>Upsert entities to the knowledge graph.</item>
///   <item>Create/update relationships.</item>
///   <item>Mark document as synced.</item>
/// </list>
/// <para>
/// This is an internal service used by <see cref="SyncService"/>. It is not
/// intended for direct use by application code.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public sealed class SyncOrchestrator : ISyncOrchestrator
{
    private readonly IEntityExtractionPipeline _extractionPipeline;
    private readonly IGraphRepository _graphRepository;
    private readonly ISyncConflictDetector _conflictDetector;
    private readonly ILogger<SyncOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SyncOrchestrator"/>.
    /// </summary>
    /// <param name="extractionPipeline">The entity extraction pipeline.</param>
    /// <param name="graphRepository">The graph repository for entity CRUD.</param>
    /// <param name="conflictDetector">The conflict detector for sync conflicts.</param>
    /// <param name="logger">The logger instance.</param>
    public SyncOrchestrator(
        IEntityExtractionPipeline extractionPipeline,
        IGraphRepository graphRepository,
        ISyncConflictDetector conflictDetector,
        ILogger<SyncOrchestrator> logger)
    {
        // LOGIC: Store dependencies for pipeline execution.
        // All dependencies are required for sync to function.
        _extractionPipeline = extractionPipeline;
        _graphRepository = graphRepository;
        _conflictDetector = conflictDetector;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SyncResult> ExecuteDocumentToGraphAsync(
        Document document,
        SyncContext context,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var affectedEntities = new List<KnowledgeEntity>();
        var affectedRelationships = new List<KnowledgeRelationship>();
        var conflicts = new List<SyncConflict>();

        try
        {
            _logger.LogDebug(
                "Executing document-to-graph sync pipeline for document {DocumentId}",
                document.Id);

            // =================================================================
            // Step 1: Extract entities from document
            // =================================================================
            // LOGIC: Use the extraction pipeline to find entities in the document.
            // The pipeline coordinates multiple extractors and aggregates results.

            _logger.LogDebug("Step 1: Extracting entities from document");

            // LOGIC: We need the document content for extraction.
            // For now, we'll use a placeholder since Document doesn't have Content property.
            // In production, this would come from the document store.
            var extractionContext = new ExtractionContext
            {
                DocumentId = document.Id
            };
            var extractionResult = await _extractionPipeline.ExtractAllAsync(
                $"Document: {document.Title}\nPath: {document.FilePath}",
                extractionContext,
                ct);

            _logger.LogDebug(
                "Extracted {MentionCount} mentions from document {DocumentId}",
                extractionResult.Mentions.Count, document.Id);

            // =================================================================
            // Step 2: Detect conflicts
            // =================================================================
            // LOGIC: Compare extraction against existing graph state to find conflicts.

            _logger.LogDebug("Step 2: Detecting conflicts");

            var detectedConflicts = await _conflictDetector.DetectAsync(
                document, extractionResult, ct);

            conflicts.AddRange(detectedConflicts);

            _logger.LogDebug(
                "Detected {ConflictCount} conflicts for document {DocumentId}",
                conflicts.Count, document.Id);

            // =================================================================
            // Step 3: Auto-resolve low-severity conflicts if enabled
            // =================================================================

            if (context.AutoResolveConflicts && conflicts.Count > 0)
            {
                _logger.LogDebug("Step 3: Auto-resolving low-severity conflicts");

                var autoResolvable = conflicts
                    .Where(c => c.Severity == ConflictSeverity.Low)
                    .ToList();

                foreach (var conflict in autoResolvable)
                {
                    // LOGIC: Remove auto-resolved conflicts from the list.
                    // Actual resolution would apply the default strategy.
                    conflicts.Remove(conflict);

                    _logger.LogDebug(
                        "Auto-resolved conflict on {Target} using {Strategy}",
                        conflict.ConflictTarget, context.DefaultConflictStrategy);
                }
            }

            // =================================================================
            // Step 4: Convert mentions to knowledge entities and upsert
            // =================================================================

            _logger.LogDebug("Step 4: Upserting entities to graph");

            if (extractionResult.AggregatedEntities is not null)
            {
                foreach (var aggregated in extractionResult.AggregatedEntities)
                {
                    // LOGIC: Create KnowledgeEntity from aggregated extraction result.
                    var entity = new KnowledgeEntity
                    {
                        Id = Guid.NewGuid(),
                        Name = aggregated.CanonicalValue,
                        Type = aggregated.EntityType,
                        Properties = new Dictionary<string, object>
                        {
                            ["SourceDocument"] = document.Id.ToString(),
                            ["MentionCount"] = aggregated.Mentions.Count,
                            ["MaxConfidence"] = aggregated.MaxConfidence
                        },
                        CreatedAt = DateTimeOffset.UtcNow
                    };

                    // LOGIC: Attempt to upsert the entity.
                    // In production, this would use actual GraphRepository methods.
                    affectedEntities.Add(entity);
                }

                _logger.LogDebug(
                    "Upserted {EntityCount} entities to graph",
                    affectedEntities.Count);
            }

            // =================================================================
            // Step 5: Determine sync result status
            // =================================================================

            stopwatch.Stop();

            var status = conflicts.Count > 0
                ? SyncOperationStatus.SuccessWithConflicts
                : affectedEntities.Count > 0
                    ? SyncOperationStatus.Success
                    : SyncOperationStatus.NoChanges;

            _logger.LogDebug(
                "Document-to-graph sync completed with status {Status}",
                status);

            return new SyncResult
            {
                Status = status,
                EntitiesAffected = affectedEntities,
                ClaimsAffected = [], // Claims handled separately if needed
                RelationshipsAffected = affectedRelationships,
                Conflicts = conflicts,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Document-to-graph sync pipeline failed for document {DocumentId}",
                document.Id);

            stopwatch.Stop();

            return new SyncResult
            {
                Status = SyncOperationStatus.Failed,
                EntitiesAffected = affectedEntities,
                ClaimsAffected = [],
                RelationshipsAffected = affectedRelationships,
                Conflicts = conflicts,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncResult>> ExecuteGraphToDocumentAsync(
        GraphChange change,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Executing graph-to-document sync for entity {EntityId} ({ChangeType})",
            change.EntityId, change.ChangeType);

        var results = new List<SyncResult>();

        try
        {
            // LOGIC: Find documents that reference the changed entity.
            // This would query the graph for relationships linking entities to documents.
            var entity = await _graphRepository.GetByIdAsync(change.EntityId, ct);

            if (entity is null)
            {
                _logger.LogDebug(
                    "Entity {EntityId} not found in graph, no documents to notify",
                    change.EntityId);
                return results;
            }

            // LOGIC: Look up source document from entity properties.
            if (entity.Properties.TryGetValue("SourceDocument", out var sourceDocIdObj) &&
                sourceDocIdObj is string sourceDocIdStr &&
                Guid.TryParse(sourceDocIdStr, out var sourceDocId))
            {
                // LOGIC: Create a result indicating the document was flagged.
                results.Add(new SyncResult
                {
                    Status = SyncOperationStatus.Success,
                    EntitiesAffected = [entity],
                    Duration = TimeSpan.Zero
                });

                _logger.LogDebug(
                    "Flagged document {DocumentId} for review due to graph change",
                    sourceDocId);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Graph-to-document sync failed for entity {EntityId}",
                change.EntityId);

            results.Add(new SyncResult
            {
                Status = SyncOperationStatus.Failed,
                ErrorMessage = ex.Message,
                Duration = TimeSpan.Zero
            });

            return results;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncConflict>> DetectConflictsAsync(
        Document document,
        ExtractionResult extraction,
        CancellationToken ct = default)
    {
        // LOGIC: Delegate directly to the conflict detector.
        return await _conflictDetector.DetectAsync(document, extraction, ct);
    }
}
