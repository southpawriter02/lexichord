// =============================================================================
// File: ExtractionTransformer.cs
// Project: Lexichord.Modules.Knowledge
// Description: Transforms extraction results to knowledge graph format.
// =============================================================================
// LOGIC: ExtractionTransformer converts raw extraction output (AggregatedEntity)
//   to KnowledgeEntity format suitable for graph storage, derives relationships
//   from entity co-occurrence, and enriches entities with graph context.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: IExtractionTransformer, ExtractionResult, Document,
//               GraphIngestionData, KnowledgeEntity, KnowledgeRelationship,
//               IGraphRepository (v0.4.5e)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.DocToGraph;

/// <summary>
/// Service for transforming extraction results for knowledge graph ingestion.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IExtractionTransformer"/> to convert raw extraction
/// output to graph-ready format.
/// </para>
/// <para>
/// <b>Transform Pipeline:</b>
/// <list type="number">
///   <item>Map AggregatedEntity to KnowledgeEntity.</item>
///   <item>Normalize entity types and properties.</item>
///   <item>Derive relationships from co-occurrence.</item>
///   <item>Enrich entities with existing graph context.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
public sealed class ExtractionTransformer : IExtractionTransformer
{
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<ExtractionTransformer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ExtractionTransformer"/>.
    /// </summary>
    /// <param name="graphRepository">The graph repository for entity lookup.</param>
    /// <param name="logger">The logger instance.</param>
    public ExtractionTransformer(
        IGraphRepository graphRepository,
        ILogger<ExtractionTransformer> logger)
    {
        _graphRepository = graphRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<GraphIngestionData> TransformAsync(
        ExtractionResult extraction,
        Document document,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Transforming extraction with {EntityCount} aggregated entities for document {DocumentId}",
            extraction.AggregatedEntities?.Count ?? 0, document.Id);

        // LOGIC: Transform aggregated entities to knowledge entities.
        var entities = await TransformEntitiesAsync(extraction.AggregatedEntities ?? [], ct);

        // LOGIC: Derive relationships from entity co-occurrence and mentions.
        var relationships = await DeriveRelationshipsAsync(entities, extraction, ct);

        // LOGIC: Build ingestion data with metadata.
        var ingestionData = new GraphIngestionData
        {
            Entities = entities,
            Relationships = relationships,
            Claims = [], // Claims are extracted separately by IClaimExtractionService
            SourceDocumentId = document.Id,
            Metadata = new Dictionary<string, object>
            {
                ["documentTitle"] = document.Title,
                ["documentHash"] = document.Hash,
                ["extractedEntityCount"] = extraction.AggregatedEntities?.Count ?? 0,
                ["extractedMentionCount"] = extraction.Mentions?.Count ?? 0,
                ["transformedAt"] = DateTimeOffset.UtcNow
            }
        };

        _logger.LogDebug(
            "Transformation complete for document {DocumentId}: {EntityCount} entities, {RelationshipCount} relationships",
            document.Id, entities.Count, relationships.Count);

        return ingestionData;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<KnowledgeEntity>> TransformEntitiesAsync(
        IReadOnlyList<AggregatedEntity> entities,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Transforming {Count} aggregated entities to knowledge entities",
            entities.Count);

        var transformedEntities = new List<KnowledgeEntity>();

        foreach (var aggregated in entities)
        {
            // LOGIC: Map AggregatedEntity properties to KnowledgeEntity.
            var properties = new Dictionary<string, object>(aggregated.MergedProperties);

            // LOGIC: Add extraction metadata to properties.
            properties["confidence"] = aggregated.MaxConfidence;
            properties["mentionCount"] = aggregated.Mentions.Count;

            var knowledgeEntity = new KnowledgeEntity
            {
                Id = Guid.NewGuid(),
                Type = NormalizeEntityType(aggregated.EntityType),
                Name = aggregated.CanonicalValue,
                Properties = properties,
                SourceDocuments = [], // Populated during upsert with document ID
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow
            };

            transformedEntities.Add(knowledgeEntity);
        }

        _logger.LogDebug(
            "Transformed {Count} aggregated entities to knowledge entities",
            transformedEntities.Count);

        return Task.FromResult<IReadOnlyList<KnowledgeEntity>>(transformedEntities);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<KnowledgeRelationship>> DeriveRelationshipsAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        ExtractionResult extraction,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Deriving relationships from {EntityCount} entities",
            entities.Count);

        var relationships = new List<KnowledgeRelationship>();

        // LOGIC: Derive relationships from entity co-occurrence in mentions.
        // Entities that appear in the same context window are likely related.
        var entityNameToId = entities.ToDictionary(e => e.Name, e => e.Id);

        // LOGIC: Group mentions by their normalized value, filtering out nulls.
        var mentionsByEntity = extraction.Mentions
            .Where(m => !string.IsNullOrEmpty(m.NormalizedValue))
            .GroupBy(m => m.NormalizedValue!)
            .ToDictionary(g => g.Key, g => g.ToList());

        // LOGIC: For each pair of entities, check if their mentions co-occur.
        // This is a simplified heuristic - production would use more sophisticated NLP.
        for (int i = 0; i < entities.Count; i++)
        {
            for (int j = i + 1; j < entities.Count; j++)
            {
                var entity1 = entities[i];
                var entity2 = entities[j];

                // LOGIC: Check if entities have co-occurring mentions.
                if (mentionsByEntity.TryGetValue(entity1.Name, out var mentions1) &&
                    mentionsByEntity.TryGetValue(entity2.Name, out var mentions2))
                {
                    // LOGIC: If both entities have multiple mentions, they may be related.
                    if (mentions1.Count >= 1 && mentions2.Count >= 1)
                    {
                        // LOGIC: Determine relationship type based on entity types.
                        var relationshipType = DetermineRelationshipType(entity1.Type, entity2.Type);

                        var relationship = new KnowledgeRelationship
                        {
                            Id = Guid.NewGuid(),
                            Type = relationshipType,
                            FromEntityId = entity1.Id,
                            ToEntityId = entity2.Id,
                            Properties = new Dictionary<string, object>
                            {
                                ["derivedFrom"] = "co-occurrence",
                                ["confidence"] = 0.6 // Lower confidence for derived relationships
                            },
                            CreatedAt = DateTimeOffset.UtcNow
                        };

                        relationships.Add(relationship);
                    }
                }
            }
        }

        _logger.LogDebug(
            "Derived {Count} relationships from entity co-occurrence",
            relationships.Count);

        return Task.FromResult<IReadOnlyList<KnowledgeRelationship>>(relationships);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<KnowledgeEntity>> EnrichEntitiesAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Enriching {Count} entities with graph context",
            entities.Count);

        var enrichedEntities = new List<KnowledgeEntity>();

        foreach (var entity in entities)
        {
            // LOGIC: Search for similar entities already in the graph using available API.
            var searchQuery = new EntitySearchQuery
            {
                Query = entity.Name,
                MaxResults = 5,
                EntityTypes = new HashSet<string> { entity.Type }
            };
            var similarEntities = await _graphRepository.SearchEntitiesAsync(searchQuery, ct);

            // LOGIC: Add enrichment metadata to properties.
            var enrichedProperties = new Dictionary<string, object>(entity.Properties)
            {
                ["enrichedAt"] = DateTimeOffset.UtcNow,
                ["similarEntityIds"] = similarEntities.Select(s => s.Id.ToString()).ToList()
            };

            // LOGIC: If we found an exact match, mark this as an update.
            var exactMatch = similarEntities.FirstOrDefault(s =>
                string.Equals(s.Name, entity.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.Type, entity.Type, StringComparison.OrdinalIgnoreCase));

            if (exactMatch is not null)
            {
                enrichedProperties["existingEntityId"] = exactMatch.Id.ToString();
                enrichedProperties["isUpdate"] = true;

                _logger.LogDebug(
                    "Entity '{Name}' matched existing entity {ExistingId}",
                    entity.Name, exactMatch.Id);
            }
            else
            {
                enrichedProperties["isUpdate"] = false;
            }

            var enrichedEntity = entity with
            {
                Properties = enrichedProperties
            };

            enrichedEntities.Add(enrichedEntity);
        }

        _logger.LogDebug(
            "Enriched {Count} entities. {UpdateCount} are updates to existing entities",
            enrichedEntities.Count,
            enrichedEntities.Count(e => e.Properties.TryGetValue("isUpdate", out var v) && v is true));

        return enrichedEntities;
    }

    /// <summary>
    /// Normalizes an entity type name to a standard format.
    /// </summary>
    /// <param name="entityType">The raw entity type.</param>
    /// <returns>The normalized entity type.</returns>
    /// <remarks>
    /// LOGIC: Normalizes type names to PascalCase and handles common aliases.
    /// </remarks>
    private static string NormalizeEntityType(string entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            return "Entity";

        // LOGIC: Handle common type aliases.
        return entityType.ToLowerInvariant() switch
        {
            "api" => "Endpoint",
            "apis" => "Endpoint",
            "function" => "Method",
            "functions" => "Method",
            "class" => "Component",
            "classes" => "Component",
            "module" => "Component",
            "modules" => "Component",
            "var" => "Parameter",
            "variable" => "Parameter",
            "variables" => "Parameter",
            "const" => "Parameter",
            "constant" => "Parameter",
            "constants" => "Parameter",
            _ => char.ToUpperInvariant(entityType[0]) + entityType[1..].ToLowerInvariant()
        };
    }

    /// <summary>
    /// Determines the relationship type based on entity types.
    /// </summary>
    /// <param name="fromType">The source entity type.</param>
    /// <param name="toType">The target entity type.</param>
    /// <returns>An appropriate relationship type.</returns>
    /// <remarks>
    /// LOGIC: Uses entity type combinations to determine likely relationship types.
    /// </remarks>
    private static string DetermineRelationshipType(string fromType, string toType)
    {
        // LOGIC: Map entity type pairs to likely relationship types.
        return (fromType, toType) switch
        {
            ("Endpoint", "Parameter") => "ACCEPTS",
            ("Component", "Method") => "CONTAINS",
            ("Component", "Property") => "HAS_PROPERTY",
            ("Component", "Component") => "DEPENDS_ON",
            ("Service", "Endpoint") => "EXPOSES",
            ("Method", "Parameter") => "ACCEPTS",
            ("Document", "Entity") => "MENTIONS",
            ("Concept", "Concept") => "RELATED_TO",
            _ => "RELATED_TO"
        };
    }
}
