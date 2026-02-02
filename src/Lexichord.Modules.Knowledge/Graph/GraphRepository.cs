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
}
