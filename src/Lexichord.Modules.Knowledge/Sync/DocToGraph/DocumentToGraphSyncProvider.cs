// =============================================================================
// File: DocumentToGraphSyncProvider.cs
// Project: Lexichord.Modules.Knowledge
// Description: Main provider for document-to-graph synchronization.
// =============================================================================
// LOGIC: DocumentToGraphSyncProvider orchestrates the doc-to-graph sync pipeline:
//   - Validates license tier before sync operations
//   - Extracts entities via IEntityExtractionPipeline
//   - Validates extraction via IExtractionValidator
//   - Transforms entities via IExtractionTransformer
//   - Upserts to graph via IGraphRepository
//   - Tracks lineage via ExtractionLineageStore
//   - Publishes DocToGraphSyncCompletedEvent via MediatR
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: IDocumentToGraphSyncProvider, IEntityExtractionPipeline (v0.4.5g),
//               IExtractionTransformer, IExtractionValidator, IGraphRepository (v0.4.5e),
//               ILicenseContext (v0.0.4c), IMediator, ILogger<T>
// =============================================================================

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph.Events;
using Lexichord.Abstractions.Contracts.RAG;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.DocToGraph;

/// <summary>
/// Main provider for document-to-graph synchronization operations.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IDocumentToGraphSyncProvider"/> to orchestrate the
/// document-to-graph sync pipeline. This is the primary entry point for
/// syncing documents to the knowledge graph.
/// </para>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item>Core: No sync access (throws <see cref="UnauthorizedAccessException"/>).</item>
///   <item>WriterPro: Basic sync with validation and lineage.</item>
///   <item>Teams+: Full sync with enrichment and advanced features.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
public sealed class DocumentToGraphSyncProvider : IDocumentToGraphSyncProvider
{
    private readonly IEntityExtractionPipeline _extractionPipeline;
    private readonly IExtractionTransformer _transformer;
    private readonly IExtractionValidator _validator;
    private readonly IGraphRepository _graphRepository;
    private readonly ExtractionLineageStore _lineageStore;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentToGraphSyncProvider> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DocumentToGraphSyncProvider"/>.
    /// </summary>
    /// <param name="extractionPipeline">The entity extraction pipeline.</param>
    /// <param name="transformer">The extraction transformer.</param>
    /// <param name="validator">The extraction validator.</param>
    /// <param name="graphRepository">The graph repository for CRUD operations.</param>
    /// <param name="lineageStore">The extraction lineage store.</param>
    /// <param name="licenseContext">The license context for tier validation.</param>
    /// <param name="mediator">The MediatR mediator for event publishing.</param>
    /// <param name="logger">The logger instance.</param>
    public DocumentToGraphSyncProvider(
        IEntityExtractionPipeline extractionPipeline,
        IExtractionTransformer transformer,
        IExtractionValidator validator,
        IGraphRepository graphRepository,
        ExtractionLineageStore lineageStore,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<DocumentToGraphSyncProvider> logger)
    {
        // LOGIC: Store all dependencies for sync orchestration.
        // All dependencies are required — null checks handled by DI container.
        _extractionPipeline = extractionPipeline;
        _transformer = transformer;
        _validator = validator;
        _graphRepository = graphRepository;
        _lineageStore = lineageStore;
        _licenseContext = licenseContext;
        _mediator = mediator;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<DocToGraphSyncResult> SyncAsync(
        Document document,
        DocToGraphSyncOptions options,
        CancellationToken ct = default)
    {
        // LOGIC: Measure total operation duration for logging and result.
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting document-to-graph sync for document {DocumentId} ({Title})",
                document.Id, document.Title);

            // LOGIC: Validate license tier supports doc-to-graph sync.
            // Core tier cannot sync.
            if (!CanPerformSync())
            {
                _logger.LogWarning(
                    "License tier {Tier} does not support doc-to-graph sync",
                    _licenseContext.Tier);
                throw new UnauthorizedAccessException(
                    $"License tier {_licenseContext.Tier} does not support doc-to-graph sync. " +
                    "Upgrade to WriterPro or higher to access sync features.");
            }

            // LOGIC: Step 1 - Read document content and extract entities.
            _logger.LogDebug(
                "Step 1: Extracting entities from document {DocumentId}",
                document.Id);

            // LOGIC: Create extraction context for the pipeline.
            var extractionContext = new ExtractionContext
            {
                DocumentId = document.Id,
                MinConfidence = 0.5f
            };

            // LOGIC: Read document content (from file path since Document doesn't have Content).
            string documentContent;
            try
            {
                documentContent = await File.ReadAllTextAsync(document.FilePath, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to read document content from {FilePath}",
                    document.FilePath);

                stopwatch.Stop();
                return new DocToGraphSyncResult
                {
                    Status = Abstractions.Contracts.Knowledge.Sync.SyncOperationStatus.Failed,
                    Duration = stopwatch.Elapsed,
                    Message = $"Failed to read document: {ex.Message}"
                };
            }

            var extractionResult = await _extractionPipeline.ExtractAllAsync(
                documentContent, extractionContext, ct);

            if (extractionResult.AggregatedEntities is null || extractionResult.AggregatedEntities.Count == 0)
            {
                _logger.LogInformation(
                    "No entities extracted from document {DocumentId}",
                    document.Id);

                stopwatch.Stop();
                return new DocToGraphSyncResult
                {
                    Status = Abstractions.Contracts.Knowledge.Sync.SyncOperationStatus.NoChanges,
                    Duration = stopwatch.Elapsed,
                    Message = "No entities found in document"
                };
            }

            // LOGIC: Limit entities if extraction exceeds max.
            if (extractionResult.AggregatedEntities.Count > options.MaxEntities)
            {
                _logger.LogWarning(
                    "Extraction exceeded max entities for {DocumentId}: {Count} > {Max}",
                    document.Id, extractionResult.AggregatedEntities.Count, options.MaxEntities);

                extractionResult = extractionResult with
                {
                    AggregatedEntities = extractionResult.AggregatedEntities
                        .Take(options.MaxEntities)
                        .ToList()
                };
            }

            _logger.LogDebug(
                "Extracted {Count} aggregated entities from document {DocumentId}",
                extractionResult.AggregatedEntities.Count, document.Id);

            // LOGIC: Step 2 - Validate extraction if enabled.
            var validationErrors = new List<ValidationError>();

            if (options.ValidateBeforeUpsert)
            {
                _logger.LogDebug(
                    "Step 2: Validating extraction for document {DocumentId}",
                    document.Id);

                var validationContext = new DocToGraphValidationContext
                {
                    DocumentId = document.Id,
                    StrictMode = false // Lenient by default, strict requires Teams+
                };

                var validationResult = await _validator.ValidateAsync(
                    extractionResult, validationContext, ct);

                if (!validationResult.IsValid && !options.AutoCorrectErrors)
                {
                    _logger.LogWarning(
                        "Validation failed for {DocumentId}: {ErrorCount} errors",
                        document.Id, validationResult.Errors.Count);

                    stopwatch.Stop();
                    return new DocToGraphSyncResult
                    {
                        Status = Abstractions.Contracts.Knowledge.Sync.SyncOperationStatus.PartialSuccess,
                        ValidationErrors = validationResult.Errors,
                        Duration = stopwatch.Elapsed,
                        Message = $"Validation failed with {validationResult.Errors.Count} errors"
                    };
                }

                validationErrors = validationResult.Errors.ToList();
            }

            // LOGIC: Step 3 - Transform extraction for graph.
            _logger.LogDebug(
                "Step 3: Transforming extraction for document {DocumentId}",
                document.Id);

            var ingestionData = await _transformer.TransformAsync(
                extractionResult, document, ct);

            // LOGIC: Step 4 - Enrich entities if enabled and licensed.
            if (options.EnrichWithGraphContext && CanEnrichEntities())
            {
                _logger.LogDebug(
                    "Step 4: Enriching entities for document {DocumentId}",
                    document.Id);

                ingestionData = ingestionData with
                {
                    Entities = await _transformer.EnrichEntitiesAsync(
                        ingestionData.Entities, ct)
                };
            }
            else if (options.EnrichWithGraphContext)
            {
                _logger.LogDebug(
                    "Skipping enrichment for {DocumentId} - requires Teams tier",
                    document.Id);
            }

            // LOGIC: Step 5 - Upsert entities to graph.
            _logger.LogDebug(
                "Step 5: Upserting {Count} entities for document {DocumentId}",
                ingestionData.Entities.Count, document.Id);

            // LOGIC: Add source document reference to entities and upsert via CreateEntityAsync.
            var upsertedEntities = new List<KnowledgeEntity>();
            foreach (var entity in ingestionData.Entities)
            {
                var entityWithSource = entity with
                {
                    SourceDocuments = [document.Id]
                };
                var created = await _graphRepository.CreateEntityAsync(entityWithSource, ct);
                upsertedEntities.Add(created);
            }

            // LOGIC: Step 6 - Create relationships if enabled.
            // Note: IGraphRepository doesn't have CreateRelationshipsAsync, so we skip this.
            // Relationships would need to be created via IGraphSession directly in production.
            var createdRelationships = new List<KnowledgeRelationship>();
            if (options.CreateRelationships && ingestionData.Relationships.Count > 0)
            {
                _logger.LogDebug(
                    "Step 6: Relationship creation for document {DocumentId} - " +
                    "{Count} relationships identified but bulk creation not yet supported",
                    document.Id, ingestionData.Relationships.Count);
                // LOGIC: In production, would need to add relationship creation methods
                // to IGraphRepository or use IGraphSession directly.
            }

            // LOGIC: Step 7 - Record extraction lineage if enabled.
            ExtractionRecord? extractionRecord = null;

            if (options.PreserveLineage)
            {
                _logger.LogDebug(
                    "Step 7: Recording extraction lineage for document {DocumentId}",
                    document.Id);

                extractionRecord = new ExtractionRecord
                {
                    ExtractionId = Guid.NewGuid(),
                    DocumentId = document.Id,
                    DocumentHash = document.Hash,
                    ExtractedAt = DateTimeOffset.UtcNow,
                    ExtractedBy = null, // Could be populated from context if available
                    EntityIds = upsertedEntities.Select(e => e.Id).ToList(),
                    ClaimIds = [], // Claims not extracted in this version
                    RelationshipIds = createdRelationships.Select(r => r.Id).ToList(),
                    ExtractionHash = ComputeExtractionHash(extractionResult),
                    ValidationErrors = validationErrors.Select(e => e.Message).ToList()
                };

                await _lineageStore.RecordExtractionAsync(extractionRecord, ct);
            }

            stopwatch.Stop();

            // LOGIC: Build the successful result.
            var result = new DocToGraphSyncResult
            {
                Status = Abstractions.Contracts.Knowledge.Sync.SyncOperationStatus.Success,
                UpsertedEntities = upsertedEntities,
                CreatedRelationships = createdRelationships,
                ExtractedClaims = [], // Claims not extracted in this simplified version
                ValidationErrors = validationErrors,
                ExtractionRecord = extractionRecord,
                Duration = stopwatch.Elapsed,
                TotalEntitiesAffected = upsertedEntities.Count,
                Message = $"Synced {upsertedEntities.Count} entities, " +
                          $"{createdRelationships.Count} relationships"
            };

            // LOGIC: Publish completion event.
            await _mediator.Publish(
                DocToGraphSyncCompletedEvent.Create(document.Id, result),
                ct);

            _logger.LogInformation(
                "Document-to-graph sync completed for {DocumentId} in {Duration}ms. " +
                "Entities: {EntityCount}, Relationships: {RelationshipCount}",
                document.Id,
                stopwatch.ElapsedMilliseconds,
                upsertedEntities.Count,
                createdRelationships.Count);

            return result;
        }
        catch (OperationCanceledException)
        {
            // LOGIC: User cancelled the operation.
            _logger.LogInformation(
                "Document-to-graph sync cancelled for {DocumentId}",
                document.Id);
            throw;
        }
        catch (TimeoutException ex)
        {
            // LOGIC: Operation timed out.
            _logger.LogError(ex,
                "Document-to-graph sync timed out for {DocumentId} after {Timeout}",
                document.Id, options.Timeout);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            // LOGIC: License check failed. Re-throw without additional handling.
            throw;
        }
        catch (Exception ex)
        {
            // LOGIC: Unexpected error.
            _logger.LogError(ex,
                "Document-to-graph sync failed for {DocumentId}",
                document.Id);

            stopwatch.Stop();
            return new DocToGraphSyncResult
            {
                Status = Abstractions.Contracts.Knowledge.Sync.SyncOperationStatus.Failed,
                Duration = stopwatch.Elapsed,
                Message = $"Sync failed: {ex.Message}"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateExtractionAsync(
        ExtractionResult extraction,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Validating extraction with {Count} entities",
            extraction.AggregatedEntities?.Count ?? 0);

        return await _validator.ValidateAsync(
            extraction,
            DocToGraphValidationContext.Default,
            ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ExtractionRecord>> GetExtractionLineageAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Retrieving extraction lineage for document {DocumentId}",
            documentId);

        return await _lineageStore.GetLineageAsync(documentId, ct);
    }

    /// <inheritdoc/>
    public async Task<bool> RollbackSyncAsync(
        Guid documentId,
        DateTimeOffset targetVersion,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Rolling back document-to-graph sync for {DocumentId} to {TargetVersion}",
            documentId, targetVersion);

        // LOGIC: Find the target extraction record.
        var targetRecord = await _lineageStore.FindExtractionAtAsync(
            documentId, targetVersion, ct);

        if (targetRecord is null)
        {
            _logger.LogWarning(
                "No extraction record found for {DocumentId} at or before {TargetVersion}",
                documentId, targetVersion);
            return false;
        }

        // LOGIC: Get all extractions after the target to identify entities to remove.
        var laterExtractions = await _lineageStore.GetExtractionsAfterAsync(
            documentId, targetRecord, ct);

        if (laterExtractions.Count == 0)
        {
            _logger.LogDebug(
                "No extractions to rollback for {DocumentId}",
                documentId);
            return true; // Already at or before target version
        }

        // LOGIC: Collect entity IDs from later extractions that aren't in target.
        var targetEntityIds = targetRecord.EntityIds.ToHashSet();
        var entitiesToRemove = laterExtractions
            .SelectMany(e => e.EntityIds)
            .Where(id => !targetEntityIds.Contains(id))
            .Distinct()
            .ToList();

        if (entitiesToRemove.Count > 0)
        {
            _logger.LogDebug(
                "Removing {Count} entities from later extractions",
                entitiesToRemove.Count);

            // LOGIC: Delete entities one by one using available API.
            foreach (var entityId in entitiesToRemove)
            {
                await _graphRepository.DeleteEntityAsync(entityId, ct);
            }
        }

        _logger.LogInformation(
            "Rollback completed for {DocumentId}. Removed {Count} entities",
            documentId, entitiesToRemove.Count);

        return true;
    }

    /// <summary>
    /// Checks if the current license tier can perform doc-to-graph sync.
    /// </summary>
    /// <returns>True if sync is allowed, false otherwise.</returns>
    private bool CanPerformSync()
    {
        // LOGIC: License gating based on specification:
        // - Core: No sync access
        // - WriterPro+: Doc-to-graph sync allowed
        return _licenseContext.Tier switch
        {
            LicenseTier.Core => false,
            LicenseTier.WriterPro => true,
            LicenseTier.Teams => true,
            LicenseTier.Enterprise => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if the current license tier can enrich entities with graph context.
    /// </summary>
    /// <returns>True if enrichment is allowed, false otherwise.</returns>
    private bool CanEnrichEntities()
    {
        // LOGIC: Entity enrichment requires Teams tier or higher.
        return _licenseContext.Tier switch
        {
            LicenseTier.Core => false,
            LicenseTier.WriterPro => false,
            LicenseTier.Teams => true,
            LicenseTier.Enterprise => true,
            _ => false
        };
    }

    /// <summary>
    /// Computes a hash of the extraction result for change detection.
    /// </summary>
    /// <param name="extraction">The extraction result to hash.</param>
    /// <returns>A base64-encoded SHA256 hash.</returns>
    private static string ComputeExtractionHash(ExtractionResult extraction)
    {
        // LOGIC: Create a deterministic string from entity names and types.
        var entities = extraction.AggregatedEntities ?? [];
        var combined = string.Join("|",
            entities
                .Select(e => $"{e.EntityType}:{e.CanonicalValue}")
                .OrderBy(s => s, StringComparer.Ordinal));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash);
    }
}
