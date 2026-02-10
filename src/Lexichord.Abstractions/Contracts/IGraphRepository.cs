// =============================================================================
// File: IGraphRepository.cs
// Project: Lexichord.Abstractions
// Description: Repository interface for knowledge graph entity operations.
// =============================================================================
// LOGIC: Defines the repository contract for querying entities from the
//   knowledge graph database. This abstraction sits above IGraphSession,
//   providing domain-specific entity operations rather than raw Cypher queries.
//
// Key operations:
//   - GetAllEntitiesAsync: Retrieve all entities from the graph
//   - GetRelationshipCountAsync: Count relationships for an entity
//   - GetMentionCountAsync: Count source document mentions for an entity
//
// v0.4.7e: Entity List View (Knowledge Graph Browser)
// Dependencies: KnowledgeEntity (v0.4.5e), IGraphSession (v0.4.5e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Repository interface for knowledge graph entity operations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IGraphRepository"/> provides a domain-specific abstraction
/// for querying <see cref="KnowledgeEntity"/> instances from the knowledge graph.
/// It wraps the lower-level <see cref="IGraphSession"/> with entity-focused
/// operations suitable for the Entity Browser UI (v0.4.7).
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations should be thread-safe for concurrent
/// read operations. The underlying graph session handles connection pooling.
/// </para>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item>WriterPro: Read access to browse entities.</item>
///   <item>Teams: Full access including entity management.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7e as part of the Entity List View.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var entities = await graphRepository.GetAllEntitiesAsync();
/// foreach (var entity in entities)
/// {
///     var relationshipCount = await graphRepository.GetRelationshipCountAsync(entity.Id);
///     Console.WriteLine($"{entity.Name}: {relationshipCount} relationships");
/// }
/// </code>
/// </example>
public interface IGraphRepository
{
    /// <summary>
    /// Retrieves all entities from the knowledge graph.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A read-only list of all <see cref="KnowledgeEntity"/> instances in the graph.
    /// Returns an empty list if no entities exist.
    /// </returns>
    /// <remarks>
    /// LOGIC: Executes a Cypher MATCH query to retrieve all nodes with entity labels.
    /// Results are mapped to <see cref="KnowledgeEntity"/> records via the graph
    /// session's type mapper. For large graphs, consider pagination in future versions.
    /// </remarks>
    Task<IReadOnlyList<KnowledgeEntity>> GetAllEntitiesAsync(CancellationToken ct = default);

    /// <summary>
    /// Counts the number of relationships connected to an entity.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// The total count of incoming and outgoing relationships for the entity.
    /// Returns 0 if the entity does not exist or has no relationships.
    /// </returns>
    /// <remarks>
    /// LOGIC: Executes a Cypher query counting both incoming and outgoing
    /// relationships: <c>MATCH (n {id: $id})-[r]-() RETURN count(r)</c>.
    /// Each relationship is counted once regardless of direction.
    /// </remarks>
    Task<int> GetRelationshipCountAsync(Guid entityId, CancellationToken ct = default);

    /// <summary>
    /// Counts the number of source document mentions for an entity.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// The number of distinct source documents that reference this entity.
    /// Returns 0 if the entity does not exist or has no source documents.
    /// </returns>
    /// <remarks>
    /// LOGIC: Returns the length of the entity's <see cref="KnowledgeEntity.SourceDocuments"/>
    /// list. This count reflects provenance tracking — how many indexed documents
    /// contributed to this entity's extraction.
    /// </remarks>
    Task<int> GetMentionCountAsync(Guid entityId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a single entity by its unique identifier.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// The <see cref="KnowledgeEntity"/> if found; otherwise <c>null</c>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Executes a Cypher MATCH query with an id parameter filter.
    /// Returns null if no entity with the specified ID exists.
    /// </remarks>
    Task<KnowledgeEntity?> GetByIdAsync(Guid entityId, CancellationToken ct = default);

    #region v0.4.7f: Entity Detail View

    /// <summary>
    /// Retrieves all relationships connected to an entity.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A read-only list of all <see cref="KnowledgeRelationship"/> instances
    /// where the entity is either the source or target. Returns an empty list
    /// if the entity does not exist or has no relationships.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Executes a Cypher query matching both incoming and outgoing
    /// relationships: <c>MATCH (n {id: $id})-[r]-(m) RETURN r</c>.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.4.7f as part of the Entity Detail View.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<KnowledgeRelationship>> GetRelationshipsForEntityAsync(
        Guid entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Counts the number of mentions for an entity in a specific document.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="documentId">The unique identifier of the source document.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// The number of times this entity is mentioned in the specified document.
    /// Returns 0 if the entity or document does not exist, or if there are no mentions.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Queries the entity-document mention tracking to count occurrences.
    /// This provides granular mention counts per document, as opposed to
    /// <see cref="GetMentionCountAsync(Guid, CancellationToken)"/> which returns
    /// the total across all documents.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.4.7f as part of the Entity Detail View.
    /// </para>
    /// </remarks>
    Task<int> GetMentionCountAsync(Guid entityId, Guid documentId, CancellationToken ct = default);

    #endregion

    #region v0.4.7g: Entity CRUD Operations

    /// <summary>
    /// Creates a new entity in the knowledge graph.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The created entity with any server-assigned values.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Executes a Cypher CREATE query to insert the entity as a new node.
    /// The entity's properties are serialized to Neo4j-compatible types.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
    /// </para>
    /// </remarks>
    Task<KnowledgeEntity> CreateEntityAsync(KnowledgeEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing entity in the knowledge graph.
    /// </summary>
    /// <param name="entity">The entity with updated values.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <remarks>
    /// <para>
    /// LOGIC: Executes a Cypher SET query to update the entity's properties.
    /// The entity is matched by ID and all properties are replaced.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
    /// </para>
    /// </remarks>
    Task UpdateEntityAsync(KnowledgeEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Deletes an entity from the knowledge graph.
    /// </summary>
    /// <param name="entityId">The ID of the entity to delete.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <remarks>
    /// <para>
    /// LOGIC: Executes a Cypher DETACH DELETE query to remove the entity
    /// and all its relationships from the graph.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
    /// </para>
    /// </remarks>
    Task DeleteEntityAsync(Guid entityId, CancellationToken ct = default);

    /// <summary>
    /// Deletes all relationships connected to an entity without deleting the entity.
    /// </summary>
    /// <param name="entityId">The ID of the entity whose relationships to delete.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <remarks>
    /// <para>
    /// LOGIC: Executes a Cypher query to delete all incoming and outgoing
    /// relationships: <c>MATCH (n {id: $id})-[r]-() DELETE r</c>.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
    /// </para>
    /// </remarks>
    Task DeleteRelationshipsForEntityAsync(Guid entityId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the change history for an entity.
    /// </summary>
    /// <param name="entityId">The ID of the entity to get history for.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A chronologically ordered list of change records, newest first.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Queries the PostgreSQL audit table for all changes related to the
    /// entity, ordered by timestamp descending.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<Knowledge.EntityChangeRecord>> GetChangeHistoryAsync(
        Guid entityId, CancellationToken ct = default);

    /// <summary>
    /// Records an entity change in the audit trail.
    /// </summary>
    /// <param name="record">The change record to persist.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <remarks>
    /// <para>
    /// LOGIC: Inserts the change record into the PostgreSQL audit table.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
    /// </para>
    /// </remarks>
    Task RecordChangeAsync(Knowledge.EntityChangeRecord record, CancellationToken ct = default);

    #endregion

    #region v0.6.6e: Graph Context Provider

    /// <summary>
    /// Searches for entities matching a query.
    /// </summary>
    /// <param name="query">The search query specifying text, limits, and optional type filters.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A read-only list of <see cref="KnowledgeEntity"/> instances matching the query.
    /// Returns an empty list if no entities match.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Searches entity names, types, and property values against the query terms.
    /// Results are limited to <see cref="Knowledge.Copilot.EntitySearchQuery.MaxResults"/>.
    /// If <see cref="Knowledge.Copilot.EntitySearchQuery.EntityTypes"/> is specified,
    /// only entities of those types are returned.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.6.6e as part of the Graph Context Provider.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<KnowledgeEntity>> SearchEntitiesAsync(
        Knowledge.Copilot.EntitySearchQuery query,
        CancellationToken ct = default);

    #endregion
}

