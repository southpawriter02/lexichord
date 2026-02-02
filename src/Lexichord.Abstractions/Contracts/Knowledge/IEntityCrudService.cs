// =============================================================================
// File: IEntityCrudService.cs
// Project: Lexichord.Abstractions
// Description: Service interface for entity CRUD operations.
// =============================================================================
// LOGIC: Defines the service contract for creating, updating, merging, and
//   deleting knowledge entities with validation and audit trail support.
//
// Operations:
//   - CreateAsync: Create a new entity from a command
//   - UpdateAsync: Modify an existing entity
//   - MergeAsync: Combine multiple entities into one
//   - DeleteAsync: Remove an entity from the graph
//   - ValidateAsync: Validate an entity against schema and axioms
//   - GetHistoryAsync: Retrieve change history for an entity
//
// v0.4.7g: Entity CRUD Operations
// Dependencies: IGraphRepository (v0.4.5e), ISchemaRegistry (v0.4.5f),
//               IAxiomStore (v0.4.6-KG), IMediator (v0.0.7a),
//               ILicenseContext (v0.0.4c), ILogger<T> (v0.0.3b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Service interface for entity CRUD operations in the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// Provides a high-level abstraction for managing knowledge entities with
/// built-in validation, audit trails, and event publishing. All operations
/// are license-gated to Teams tier or higher.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations should be thread-safe for concurrent
/// operations. Each method should be independently atomic.
/// </para>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item>Core: No access to CRUD operations.</item>
///   <item>WriterPro: Read-only access (validation only).</item>
///   <item>Teams: Full CRUD access.</item>
///   <item>Enterprise: Full access plus bulk operations (future).</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await crudService.CreateAsync(new CreateEntityCommand
/// {
///     Type = "Concept",
///     Name = "Machine Learning",
///     Properties = new() { ["description"] = "A subset of AI..." }
/// });
/// 
/// if (result.Success)
/// {
///     Console.WriteLine($"Created entity: {result.Entity!.Id}");
/// }
/// </code>
/// </example>
public interface IEntityCrudService
{
    /// <summary>
    /// Creates a new entity in the knowledge graph.
    /// </summary>
    /// <param name="command">The command containing entity creation parameters.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A result containing the created entity on success, or error details on failure.
    /// </returns>
    /// <remarks>
    /// LOGIC: Validates entity type against schema registry, creates the entity,
    /// records the creation in the audit trail, and publishes an EntityCreatedEvent.
    /// </remarks>
    Task<EntityOperationResult> CreateAsync(CreateEntityCommand command, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing entity in the knowledge graph.
    /// </summary>
    /// <param name="command">The command containing update parameters.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A result containing the updated entity on success, or error details on failure.
    /// </returns>
    /// <remarks>
    /// LOGIC: Retrieves the entity, applies changes, validates the result,
    /// persists the update, records the change, and publishes an EntityUpdatedEvent.
    /// </remarks>
    Task<EntityOperationResult> UpdateAsync(UpdateEntityCommand command, CancellationToken ct = default);

    /// <summary>
    /// Merges multiple entities into a single target entity.
    /// </summary>
    /// <param name="command">The command containing merge parameters.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A result containing the merged entity and IDs of removed entities.
    /// </returns>
    /// <remarks>
    /// LOGIC: Retrieves all entities, applies merge strategy to combine properties,
    /// optionally transfers relationships, deletes source entities, records the
    /// merge in audit trails, and publishes an EntitiesMergedEvent.
    /// </remarks>
    Task<MergeOperationResult> MergeAsync(MergeEntitiesCommand command, CancellationToken ct = default);

    /// <summary>
    /// Deletes an entity from the knowledge graph.
    /// </summary>
    /// <param name="command">The command containing deletion parameters.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A result indicating success or failure with error details.
    /// </returns>
    /// <remarks>
    /// LOGIC: Retrieves the entity, handles relationships according to cascade mode,
    /// deletes the entity, records the deletion, and publishes an EntityDeletedEvent.
    /// </remarks>
    Task<EntityOperationResult> DeleteAsync(DeleteEntityCommand command, CancellationToken ct = default);

    /// <summary>
    /// Validates an entity against the schema registry and axiom store.
    /// </summary>
    /// <param name="entity">The entity to validate.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A validation result with errors and warnings.
    /// </returns>
    /// <remarks>
    /// LOGIC: Checks entity type exists in schema, validates required properties,
    /// applies axiom rules. Schema errors are blocking; axiom violations become warnings.
    /// </remarks>
    Task<EntityValidationResult> ValidateAsync(KnowledgeEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the change history for an entity.
    /// </summary>
    /// <param name="entityId">The ID of the entity to get history for.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A chronologically ordered list of change records, newest first.
    /// </returns>
    /// <remarks>
    /// LOGIC: Queries the audit table for all changes related to the entity,
    /// ordered by timestamp descending.
    /// </remarks>
    Task<IReadOnlyList<EntityChangeRecord>> GetHistoryAsync(Guid entityId, CancellationToken ct = default);
}
