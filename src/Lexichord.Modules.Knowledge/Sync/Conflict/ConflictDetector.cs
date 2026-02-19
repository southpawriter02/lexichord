// =============================================================================
// File: ConflictDetector.cs
// Project: Lexichord.Modules.Knowledge
// Description: Detects conflicts between document extractions and graph state.
// =============================================================================
// LOGIC: ConflictDetector provides enhanced conflict detection with
//   value conflict analysis, structural conflict detection, and
//   entity change tracking. It uses IEntityComparer for detailed
//   property comparisons.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: IConflictDetector, IEntityComparer, ConflictDetail (v0.7.6h),
//               IGraphRepository (v0.4.5e), SyncConflict (v0.7.6e),
//               ExtractionResult (v0.4.5g), ExtractionRecord (v0.7.6f)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.Conflict;

/// <summary>
/// Service for detecting conflicts between document extractions and graph state.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IConflictDetector"/> with enhanced detection:
/// </para>
/// <list type="bullet">
///   <item>Value conflict detection using <see cref="IEntityComparer"/>.</item>
///   <item>Structural conflict detection (missing entities).</item>
///   <item>Entity change tracking since extraction.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public sealed class ConflictDetector : IConflictDetector
{
    private readonly IGraphRepository _graphRepository;
    private readonly IEntityComparer _entityComparer;
    private readonly ILogger<ConflictDetector> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ConflictDetector"/>.
    /// </summary>
    /// <param name="graphRepository">The graph repository for entity queries.</param>
    /// <param name="entityComparer">The entity comparer for property comparisons.</param>
    /// <param name="logger">The logger instance.</param>
    public ConflictDetector(
        IGraphRepository graphRepository,
        IEntityComparer entityComparer,
        ILogger<ConflictDetector> logger)
    {
        _graphRepository = graphRepository;
        _entityComparer = entityComparer;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncConflict>> DetectAsync(
        Document document,
        ExtractionResult extraction,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Detecting conflicts for document {DocumentId}",
            document.Id);

        var conflicts = new List<SyncConflict>();

        try
        {
            // LOGIC: If no aggregated entities, nothing to compare.
            if (extraction.AggregatedEntities is null || extraction.AggregatedEntities.Count == 0)
            {
                _logger.LogDebug("No aggregated entities in extraction, skipping conflict detection");
                return conflicts;
            }

            // LOGIC: Convert AggregatedEntities to KnowledgeEntities for comparison.
            var extractedEntities = extraction.AggregatedEntities
                .Select(ae => new KnowledgeEntity
                {
                    Id = Guid.NewGuid(), // LOGIC: New entities get new IDs
                    Type = ae.EntityType,
                    Name = ae.CanonicalValue,
                    Properties = new Dictionary<string, object>
                    {
                        ["MaxConfidence"] = ae.MaxConfidence,
                        ["MentionCount"] = ae.Mentions.Count,
                        ["CanonicalValue"] = ae.CanonicalValue
                    },
                    CreatedAt = DateTimeOffset.UtcNow,
                    ModifiedAt = DateTimeOffset.UtcNow
                })
                .ToList();

            // LOGIC: Detect value conflicts.
            var valueConflicts = await DetectValueConflictsAsync(extractedEntities, ct);
            foreach (var vc in valueConflicts)
            {
                conflicts.Add(new SyncConflict
                {
                    ConflictTarget = $"{vc.Entity.Type}:{vc.Entity.Name}.{vc.ConflictField}",
                    DocumentValue = vc.DocumentValue,
                    GraphValue = vc.GraphValue,
                    DetectedAt = DateTimeOffset.UtcNow,
                    Type = vc.Type,
                    Severity = vc.Severity,
                    Description = $"Value conflict in {vc.ConflictField}"
                });
            }

            // LOGIC: Detect structural conflicts.
            var structuralConflicts = await DetectStructuralConflictsAsync(document, extraction, ct);
            foreach (var sc in structuralConflicts)
            {
                conflicts.Add(new SyncConflict
                {
                    ConflictTarget = $"{sc.Entity.Type}:{sc.Entity.Name}",
                    DocumentValue = sc.DocumentValue,
                    GraphValue = sc.GraphValue,
                    DetectedAt = DateTimeOffset.UtcNow,
                    Type = sc.Type,
                    Severity = sc.Severity,
                    Description = $"Structural conflict: {sc.Type}"
                });
            }

            _logger.LogInformation(
                "Detected {Count} conflicts for document {DocumentId}",
                conflicts.Count, document.Id);

            return conflicts;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Conflict detection cancelled for document {DocumentId}", document.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Conflict detection failed for document {DocumentId}",
                document.Id);
            return conflicts;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ConflictDetail>> DetectValueConflictsAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Detecting value conflicts for {Count} entities", entities.Count);

        var conflicts = new List<ConflictDetail>();

        foreach (var entity in entities)
        {
            ct.ThrowIfCancellationRequested();

            // LOGIC: Try to find matching entity in graph by name and type.
            var graphEntity = await FindMatchingEntityAsync(entity, ct);

            if (graphEntity is null)
            {
                // LOGIC: No matching entity in graph - not a value conflict.
                continue;
            }

            // LOGIC: Compare properties using IEntityComparer.
            var comparison = await _entityComparer.CompareAsync(entity, graphEntity, ct);

            foreach (var diff in comparison.PropertyDifferences)
            {
                conflicts.Add(new ConflictDetail
                {
                    ConflictId = Guid.NewGuid(),
                    Entity = graphEntity,
                    ConflictField = diff.PropertyName,
                    DocumentValue = diff.DocumentValue ?? "(empty)",
                    GraphValue = diff.GraphValue ?? "(empty)",
                    Type = ConflictType.ValueMismatch,
                    Severity = DetermineSeverity(diff.Confidence),
                    DetectedAt = DateTimeOffset.UtcNow,
                    DocumentModifiedAt = entity.ModifiedAt,
                    GraphModifiedAt = graphEntity.ModifiedAt,
                    SuggestedStrategy = SuggestResolutionStrategy(diff.Confidence),
                    ResolutionConfidence = diff.Confidence
                });
            }
        }

        _logger.LogDebug("Detected {Count} value conflicts", conflicts.Count);
        return conflicts;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ConflictDetail>> DetectStructuralConflictsAsync(
        Document document,
        ExtractionResult extraction,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Detecting structural conflicts for document {DocumentId}",
            document.Id);

        var conflicts = new List<ConflictDetail>();

        try
        {
            // LOGIC: Get entity names from extraction.
            var extractedNames = extraction.AggregatedEntities?
                .Select(ae => ae.CanonicalValue.ToLowerInvariant())
                .ToHashSet() ?? new HashSet<string>();

            // LOGIC: Get entities from graph linked to this document.
            var graphEntities = await GetEntitiesForDocumentAsync(document.Id, ct);
            var graphNames = graphEntities
                .Select(e => e.Name.ToLowerInvariant())
                .ToHashSet();

            // LOGIC: Entities in graph but not in document (deleted from document).
            foreach (var entity in graphEntities)
            {
                ct.ThrowIfCancellationRequested();

                if (!extractedNames.Contains(entity.Name.ToLowerInvariant()))
                {
                    conflicts.Add(new ConflictDetail
                    {
                        ConflictId = Guid.NewGuid(),
                        Entity = entity,
                        ConflictField = "Entity",
                        DocumentValue = "(deleted from document)",
                        GraphValue = entity,
                        Type = ConflictType.MissingInDocument,
                        Severity = ConflictSeverity.Medium,
                        DetectedAt = DateTimeOffset.UtcNow,
                        GraphModifiedAt = entity.ModifiedAt,
                        SuggestedStrategy = ConflictResolutionStrategy.Manual,
                        ResolutionConfidence = 0.5f
                    });
                }
            }

            _logger.LogDebug("Detected {Count} structural conflicts", conflicts.Count);
            return conflicts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Structural conflict detection failed for document {DocumentId}",
                document.Id);
            return conflicts;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> EntitiesChangedAsync(
        ExtractionRecord extraction,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Checking if entities changed since extraction {ExtractionId}",
            extraction.ExtractionId);

        try
        {
            foreach (var entityId in extraction.EntityIds)
            {
                ct.ThrowIfCancellationRequested();

                var entity = await _graphRepository.GetByIdAsync(entityId, ct);
                if (entity is not null && entity.ModifiedAt > extraction.ExtractedAt)
                {
                    _logger.LogDebug(
                        "Entity {EntityId} was modified after extraction",
                        entityId);
                    return true;
                }
            }

            _logger.LogDebug("No entity changes detected since extraction");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Entity change check failed for extraction {ExtractionId}",
                extraction.ExtractionId);
            return false;
        }
    }

    /// <summary>
    /// Finds a matching entity in the graph by name and type.
    /// </summary>
    private async Task<KnowledgeEntity?> FindMatchingEntityAsync(
        KnowledgeEntity entity,
        CancellationToken ct)
    {
        // LOGIC: Query all entities and find match by name and type.
        var allEntities = await _graphRepository.GetAllEntitiesAsync(ct);

        return allEntities.FirstOrDefault(e =>
            e.Name.Equals(entity.Name, StringComparison.OrdinalIgnoreCase) &&
            e.Type.Equals(entity.Type, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets entities linked to a specific document.
    /// </summary>
    private async Task<IReadOnlyList<KnowledgeEntity>> GetEntitiesForDocumentAsync(
        Guid documentId,
        CancellationToken ct)
    {
        // LOGIC: Query graph for entities with SourceDocument property.
        var allEntities = await _graphRepository.GetAllEntitiesAsync(ct);

        return allEntities
            .Where(e =>
                e.Properties.TryGetValue("SourceDocument", out var srcDoc) &&
                srcDoc is string srcDocStr &&
                Guid.TryParse(srcDocStr, out var srcDocId) &&
                srcDocId == documentId)
            .ToList();
    }

    /// <summary>
    /// Determines conflict severity based on confidence.
    /// </summary>
    private static ConflictSeverity DetermineSeverity(float confidence)
    {
        return confidence switch
        {
            >= 0.8f => ConflictSeverity.Low,    // High confidence = minor conflict
            >= 0.5f => ConflictSeverity.Medium, // Medium confidence = should review
            _ => ConflictSeverity.High          // Low confidence = needs attention
        };
    }

    /// <summary>
    /// Suggests resolution strategy based on confidence.
    /// </summary>
    private static ConflictResolutionStrategy SuggestResolutionStrategy(float confidence)
    {
        return confidence >= 0.8f
            ? ConflictResolutionStrategy.Merge
            : ConflictResolutionStrategy.Manual;
    }
}
