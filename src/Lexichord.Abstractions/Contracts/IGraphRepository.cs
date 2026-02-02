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
}
