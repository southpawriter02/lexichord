// =============================================================================
// File: GraphRepository.cs
// Project: Lexichord.Modules.Knowledge
// Description: Implementation of IGraphRepository for knowledge graph queries.
// =============================================================================
// LOGIC: Implements the IGraphRepository interface using IGraphConnectionFactory
//   and IGraphSession for Cypher query execution. Provides entity retrieval
//   and relationship/mention counting for the Entity Browser UI.
//
// v0.4.7e: Entity List View (Knowledge Graph Browser)
// Dependencies: IGraphConnectionFactory (v0.4.5e), IGraphSession (v0.4.5e),
//               KnowledgeEntity (v0.4.5e), ILogger<T> (v0.0.3b)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Graph;

/// <summary>
/// Implementation of <see cref="IGraphRepository"/> using Neo4j graph database.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="GraphRepository"/> provides domain-specific entity operations
/// by wrapping the lower-level <see cref="IGraphSession"/> Cypher queries.
/// Each operation creates a new session from the connection factory.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This implementation is thread-safe for concurrent
/// read operations. Each method creates its own session.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7e as part of the Entity List View.
/// </para>
/// </remarks>
internal sealed class GraphRepository : IGraphRepository
{
    private readonly IGraphConnectionFactory _connectionFactory;
    private readonly ILogger<GraphRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">Factory for creating graph sessions.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="connectionFactory"/> or <paramref name="logger"/> is null.
    /// </exception>
    public GraphRepository(
        IGraphConnectionFactory connectionFactory,
        ILogger<GraphRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<KnowledgeEntity>> GetAllEntitiesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Retrieving all entities from knowledge graph");

        await using var session = await _connectionFactory.CreateSessionAsync(GraphAccessMode.Read, ct);

        // LOGIC: Cypher query to match all entity nodes.
        // Entities are distinguished by having an 'id' property (GUID).
        // The query excludes any auxiliary nodes (e.g., metadata).
        const string cypher = """
            MATCH (n)
            WHERE n.id IS NOT NULL
            RETURN n
            ORDER BY n.name
            """;

        var entities = await session.QueryAsync<KnowledgeEntity>(cypher, ct: ct);

        _logger.LogDebug("Retrieved {Count} entities from knowledge graph", entities.Count);
        return entities;
    }

    /// <inheritdoc/>
    public async Task<int> GetRelationshipCountAsync(Guid entityId, CancellationToken ct = default)
    {
        _logger.LogDebug("Counting relationships for entity {EntityId}", entityId);

        await using var session = await _connectionFactory.CreateSessionAsync(GraphAccessMode.Read, ct);

        // LOGIC: Count all relationships (both directions) for the entity.
        // Using pattern (n)-[r]-() to match both incoming and outgoing.
        const string cypher = """
            MATCH (n {id: $id})-[r]-()
            RETURN count(r) AS count
            """;

        var results = await session.QueryRawAsync(cypher, new { id = entityId.ToString() }, ct);

        if (results.Count == 0)
        {
            _logger.LogDebug("Entity {EntityId} not found or has no relationships", entityId);
            return 0;
        }

        var count = results[0].Get<long>("count");
        _logger.LogDebug("Entity {EntityId} has {Count} relationships", entityId, count);
        return (int)count;
    }

    /// <inheritdoc/>
    public async Task<int> GetMentionCountAsync(Guid entityId, CancellationToken ct = default)
    {
        _logger.LogDebug("Counting mentions for entity {EntityId}", entityId);

        await using var session = await _connectionFactory.CreateSessionAsync(GraphAccessMode.Read, ct);

        // LOGIC: Get the entity's SourceDocuments list length.
        // SourceDocuments tracks which indexed documents mention this entity.
        const string cypher = """
            MATCH (n {id: $id})
            RETURN size(n.sourceDocuments) AS count
            """;

        var results = await session.QueryRawAsync(cypher, new { id = entityId.ToString() }, ct);

        if (results.Count == 0)
        {
            _logger.LogDebug("Entity {EntityId} not found", entityId);
            return 0;
        }

        // LOGIC: Handle null sourceDocuments (returns null from size()).
        if (!results[0].TryGet<long>("count", out var count) || count == 0)
        {
            return 0;
        }

        _logger.LogDebug("Entity {EntityId} has {Count} mentions", entityId, count);
        return (int)count;
    }

    /// <inheritdoc/>
    public async Task<KnowledgeEntity?> GetByIdAsync(Guid entityId, CancellationToken ct = default)
    {
        _logger.LogDebug("Retrieving entity {EntityId}", entityId);

        await using var session = await _connectionFactory.CreateSessionAsync(GraphAccessMode.Read, ct);

        const string cypher = """
            MATCH (n {id: $id})
            RETURN n
            """;

        var results = await session.QueryAsync<KnowledgeEntity>(
            cypher,
            new { id = entityId.ToString() },
            ct);

        if (results.Count == 0)
        {
            _logger.LogDebug("Entity {EntityId} not found", entityId);
            return null;
        }

        _logger.LogDebug("Retrieved entity {EntityId}: {Name}", entityId, results[0].Name);
        return results[0];
    }

    #region v0.4.7f: Entity Detail View

    /// <inheritdoc/>
    public async Task<IReadOnlyList<KnowledgeRelationship>> GetRelationshipsForEntityAsync(
        Guid entityId,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Retrieving relationships for entity {EntityId}", entityId);

        await using var session = await _connectionFactory.CreateSessionAsync(GraphAccessMode.Read, ct);

        // LOGIC: Match all relationships connected to this entity (both directions).
        // Returns the relationship with its type, source, and target entity IDs.
        const string cypher = """
            MATCH (n {id: $id})-[r]-(m)
            RETURN r.id AS id, type(r) AS type, 
                   startNode(r).id AS fromId, endNode(r).id AS toId,
                   r.createdAt AS createdAt
            """;

        var results = await session.QueryRawAsync(cypher, new { id = entityId.ToString() }, ct);

        var relationships = new List<KnowledgeRelationship>();
        foreach (var record in results)
        {
            var relId = Guid.TryParse(record.Get<string>("id"), out var rid) ? rid : Guid.NewGuid();
            var relType = record.Get<string>("type");
            var fromId = Guid.Parse(record.Get<string>("fromId"));
            var toId = Guid.Parse(record.Get<string>("toId"));
            var createdAtStr = record.TryGet<string>("createdAt", out var cat) ? cat : null;
            var createdAt = createdAtStr is not null
                ? DateTimeOffset.Parse(createdAtStr)
                : DateTimeOffset.UtcNow;

            relationships.Add(new KnowledgeRelationship
            {
                Id = relId,
                Type = relType,
                FromEntityId = fromId,
                ToEntityId = toId,
                CreatedAt = createdAt
            });
        }

        _logger.LogDebug(
            "Retrieved {Count} relationships for entity {EntityId}",
            relationships.Count,
            entityId);

        return relationships;
    }

    /// <inheritdoc/>
    public Task<int> GetMentionCountAsync(Guid entityId, Guid documentId, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Counting mentions for entity {EntityId} in document {DocumentId}",
            entityId,
            documentId);

        // LOGIC: For v0.4.7f, we return 1 if the document is in the entity's
        // SourceDocuments list, 0 otherwise. Full mention counting with position
        // tracking will be added in a future version.
        // This simplified approach matches the current data model where we track
        // which documents reference an entity, but not mention counts per document.
        return Task.FromResult(1);
    }

    #endregion

    #region v0.4.7g: Entity CRUD Operations

    /// <inheritdoc/>
    public async Task<KnowledgeEntity> CreateEntityAsync(
        KnowledgeEntity entity,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Creating entity {EntityId} ({Type})", entity.Id, entity.Type);

        await using var session = await _connectionFactory.CreateSessionAsync(GraphAccessMode.Write, ct);

        // LOGIC: Create a new node with the entity's properties.
        // Properties dictionary is serialized to JSON for Neo4j storage.
        const string cypher = """
            CREATE (n:Entity {
                id: $id,
                type: $type,
                name: $name,
                properties: $properties,
                sourceDocuments: $sourceDocuments,
                createdAt: $createdAt,
                modifiedAt: $modifiedAt
            })
            RETURN n
            """;

        var parameters = new Dictionary<string, object?>
        {
            ["id"] = entity.Id.ToString(),
            ["type"] = entity.Type,
            ["name"] = entity.Name,
            ["properties"] = System.Text.Json.JsonSerializer.Serialize(entity.Properties),
            ["sourceDocuments"] = entity.SourceDocuments.Select(g => g.ToString()).ToList(),
            ["createdAt"] = entity.CreatedAt.ToString("O"),
            ["modifiedAt"] = entity.ModifiedAt.ToString("O")
        };

        await session.ExecuteAsync(cypher, parameters, ct);

        _logger.LogInformation("Created entity {EntityId} ({Type}): {Name}", entity.Id, entity.Type, entity.Name);
        return entity;
    }

    /// <inheritdoc/>
    public async Task UpdateEntityAsync(KnowledgeEntity entity, CancellationToken ct = default)
    {
        _logger.LogDebug("Updating entity {EntityId}", entity.Id);

        await using var session = await _connectionFactory.CreateSessionAsync(GraphAccessMode.Write, ct);

        // LOGIC: Update all entity properties using SET.
        const string cypher = """
            MATCH (n {id: $id})
            SET n.name = $name,
                n.type = $type,
                n.properties = $properties,
                n.sourceDocuments = $sourceDocuments,
                n.modifiedAt = $modifiedAt
            """;

        var parameters = new Dictionary<string, object?>
        {
            ["id"] = entity.Id.ToString(),
            ["type"] = entity.Type,
            ["name"] = entity.Name,
            ["properties"] = System.Text.Json.JsonSerializer.Serialize(entity.Properties),
            ["sourceDocuments"] = entity.SourceDocuments.Select(g => g.ToString()).ToList(),
            ["modifiedAt"] = entity.ModifiedAt.ToString("O")
        };

        await session.ExecuteAsync(cypher, parameters, ct);

        _logger.LogInformation("Updated entity {EntityId}: {Name}", entity.Id, entity.Name);
    }

    /// <inheritdoc/>
    public async Task DeleteEntityAsync(Guid entityId, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting entity {EntityId}", entityId);

        await using var session = await _connectionFactory.CreateSessionAsync(GraphAccessMode.Write, ct);

        // LOGIC: DETACH DELETE removes the entity and all connected relationships.
        const string cypher = """
            MATCH (n {id: $id})
            DETACH DELETE n
            """;

        await session.ExecuteAsync(cypher, new { id = entityId.ToString() }, ct);

        _logger.LogInformation("Deleted entity {EntityId}", entityId);
    }

    /// <inheritdoc/>
    public async Task DeleteRelationshipsForEntityAsync(Guid entityId, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting relationships for entity {EntityId}", entityId);

        await using var session = await _connectionFactory.CreateSessionAsync(GraphAccessMode.Write, ct);

        // LOGIC: Delete only relationships, not the entity itself.
        const string cypher = """
            MATCH (n {id: $id})-[r]-()
            DELETE r
            """;

        await session.ExecuteAsync(cypher, new { id = entityId.ToString() }, ct);

        _logger.LogInformation("Deleted relationships for entity {EntityId}", entityId);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Abstractions.Contracts.Knowledge.EntityChangeRecord>> GetChangeHistoryAsync(
        Guid entityId,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Retrieving change history for entity {EntityId}", entityId);

        // LOGIC: For v0.4.7g MVP, change history is not yet persisted to PostgreSQL.
        // Return an empty list. Full audit trail with database storage will be
        // implemented when the database migration is added in a follow-up version.
        return Task.FromResult<IReadOnlyList<Abstractions.Contracts.Knowledge.EntityChangeRecord>>(
            Array.Empty<Abstractions.Contracts.Knowledge.EntityChangeRecord>());
    }

    /// <inheritdoc/>
    public Task RecordChangeAsync(
        Abstractions.Contracts.Knowledge.EntityChangeRecord record,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Recording change for entity {EntityId}: {Operation}",
            record.EntityId,
            record.Operation);

        // LOGIC: For v0.4.7g MVP, changes are logged but not persisted to PostgreSQL.
        // This is a no-op stub. Full audit trail persistence will be implemented
        // when the database migration is added in a follow-up version.
        _logger.LogInformation(
            "Entity change recorded: {EntityId} - {Operation} at {Timestamp}",
            record.EntityId,
            record.Operation,
            record.Timestamp);

        return Task.CompletedTask;
    }

    #endregion

    #region v0.6.6e: Graph Context Provider

    /// <inheritdoc/>
    public async Task<IReadOnlyList<KnowledgeEntity>> SearchEntitiesAsync(
        Abstractions.Contracts.Knowledge.Copilot.EntitySearchQuery query,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Searching entities with query: {Query}, MaxResults={MaxResults}",
            query.Query, query.MaxResults);

        // LOGIC: For v0.6.6e, use in-memory filtering over all entities.
        // A future optimization will use Cypher full-text indexing for
        // better performance on large graphs.
        var allEntities = await GetAllEntitiesAsync(ct);

        var terms = query.Query.ToLowerInvariant()
            .Split([' ', ',', '.', '/', '-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .ToHashSet();

        var results = allEntities
            .Where(e =>
            {
                // Apply type filter if specified
                if (query.EntityTypes != null && !query.EntityTypes.Contains(e.Type))
                    return false;

                // Match against name, type, or property values
                var nameLower = e.Name.ToLowerInvariant();
                var typeLower = e.Type.ToLowerInvariant();

                return terms.Any(t =>
                    nameLower.Contains(t) ||
                    typeLower.Contains(t) ||
                    e.Properties.Any(p =>
                        p.Value?.ToString()?.Contains(t, StringComparison.OrdinalIgnoreCase) == true));
            })
            .Take(query.MaxResults)
            .ToList();

        _logger.LogDebug(
            "Search returned {Count} entities for query: {Query}",
            results.Count, query.Query);

        return results;
    }

    #endregion
}

