// =============================================================================
// File: EntityCrudEvents.cs
// Project: Lexichord.Abstractions
// Description: Domain events for entity CRUD operations.
// =============================================================================
// LOGIC: Defines domain events published after entity modifications.
//   These events enable loose coupling between the CRUD service and
//   other system components that need to react to entity changes.
//
// Events:
//   - EntityCreatedEvent: Published after a new entity is created
//   - EntityUpdatedEvent: Published after an entity is modified
//   - EntitiesMergedEvent: Published after entities are merged
//   - EntityDeletedEvent: Published after an entity is deleted
//
// v0.4.7g: Entity CRUD Operations
// Dependencies: INotification (MediatR), KnowledgeEntity (v0.4.5e)
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Domain event published when a new entity is created.
/// </summary>
/// <remarks>
/// <para>
/// Subscribers can use this event to update caches, trigger indexing,
/// or perform other side effects in response to entity creation.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
/// <param name="Entity">The newly created entity.</param>
public sealed record EntityCreatedEvent(KnowledgeEntity Entity) : INotification;

/// <summary>
/// Domain event published when an entity is updated.
/// </summary>
/// <remarks>
/// <para>
/// Includes both previous and new state to support diff-based processing.
/// Subscribers can compare states to determine what changed.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
/// <param name="PreviousState">The entity state before modification.</param>
/// <param name="NewState">The entity state after modification.</param>
public sealed record EntityUpdatedEvent(
    KnowledgeEntity PreviousState,
    KnowledgeEntity NewState) : INotification;

/// <summary>
/// Domain event published when multiple entities are merged.
/// </summary>
/// <remarks>
/// <para>
/// Published after source entities have been merged into the target
/// and the source entities have been deleted from the graph.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
/// <param name="SourceIds">The IDs of entities that were merged and removed.</param>
/// <param name="MergedEntity">The resulting merged target entity.</param>
public sealed record EntitiesMergedEvent(
    IReadOnlyList<Guid> SourceIds,
    KnowledgeEntity MergedEntity) : INotification;

/// <summary>
/// Domain event published when an entity is deleted.
/// </summary>
/// <remarks>
/// <para>
/// Published after the entity and its relationships (if cascaded) have
/// been removed from the graph. Includes identifying information for
/// logging and cache invalidation purposes.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
/// <param name="EntityId">The ID of the deleted entity.</param>
/// <param name="EntityName">The name of the deleted entity.</param>
/// <param name="EntityType">The type of the deleted entity.</param>
public sealed record EntityDeletedEvent(
    Guid EntityId,
    string EntityName,
    string EntityType) : INotification;
