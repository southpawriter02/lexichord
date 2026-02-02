// =============================================================================
// File: EntityCrudCommands.cs
// Project: Lexichord.Abstractions
// Description: Command records for entity CRUD operations.
// =============================================================================
// LOGIC: Defines immutable command records that encapsulate all parameters
//   needed for entity create, update, merge, and delete operations.
//
// Commands:
//   - CreateEntityCommand: Parameters for creating a new entity
//   - UpdateEntityCommand: Parameters for modifying an existing entity
//   - MergeEntitiesCommand: Parameters for merging multiple entities into one
//   - DeleteEntityCommand: Parameters for removing an entity from the graph
//
// v0.4.7g: Entity CRUD Operations
// Dependencies: PropertyMergeStrategy, RelationshipCascadeMode (v0.4.7g)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Command to create a new knowledge entity.
/// </summary>
/// <remarks>
/// <para>
/// Encapsulates all parameters needed to create a new entity in the knowledge graph.
/// The entity type must exist in the schema registry.
/// </para>
/// <para>
/// <b>License Requirements:</b> Teams tier or higher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
public sealed record CreateEntityCommand
{
    /// <summary>
    /// Gets the entity type (must match a registered schema type).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the display name for the entity.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the initial property values for the entity.
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();

    /// <summary>
    /// Gets the optional source document ID that prompted entity creation.
    /// </summary>
    public Guid? SourceDocumentId { get; init; }

    /// <summary>
    /// Gets whether to skip schema validation during creation.
    /// </summary>
    /// <remarks>
    /// Use with caution — bypasses type and property validation.
    /// Useful for bulk imports where validation is handled externally.
    /// </remarks>
    public bool SkipValidation { get; init; }
}

/// <summary>
/// Command to update an existing knowledge entity.
/// </summary>
/// <remarks>
/// <para>
/// Supports partial updates — only specified properties are modified.
/// Properties can be set to new values or removed entirely.
/// </para>
/// <para>
/// <b>License Requirements:</b> Teams tier or higher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
public sealed record UpdateEntityCommand
{
    /// <summary>
    /// Gets the unique identifier of the entity to update.
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// Gets the new display name, or null to leave unchanged.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets properties to set or update. Null values in the dictionary remove the property.
    /// </summary>
    public Dictionary<string, object?>? SetProperties { get; init; }

    /// <summary>
    /// Gets property names to remove from the entity.
    /// </summary>
    public IReadOnlyList<string>? RemoveProperties { get; init; }

    /// <summary>
    /// Gets the reason for the change (recorded in audit trail).
    /// </summary>
    public string? ChangeReason { get; init; }
}

/// <summary>
/// Command to merge multiple entities into a single target entity.
/// </summary>
/// <remarks>
/// <para>
/// Combines properties and relationships from source entities into the target.
/// Source entities are deleted after successful merge.
/// </para>
/// <para>
/// <b>License Requirements:</b> Teams tier or higher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
public sealed record MergeEntitiesCommand
{
    /// <summary>
    /// Gets the IDs of entities to merge into the target (will be deleted).
    /// </summary>
    public required IReadOnlyList<Guid> SourceEntityIds { get; init; }

    /// <summary>
    /// Gets the ID of the entity that will receive merged data.
    /// </summary>
    public required Guid TargetEntityId { get; init; }

    /// <summary>
    /// Gets the strategy for resolving property conflicts.
    /// </summary>
    public PropertyMergeStrategy MergeStrategy { get; init; } = PropertyMergeStrategy.KeepTarget;

    /// <summary>
    /// Gets whether to transfer all relationships from source entities to target.
    /// </summary>
    public bool PreserveAllRelationships { get; init; } = true;

    /// <summary>
    /// Gets the reason for the merge (recorded in audit trail).
    /// </summary>
    public string? ChangeReason { get; init; }
}

/// <summary>
/// Command to delete an entity from the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// Removes an entity and optionally its relationships from the graph.
/// The cascade mode determines relationship handling behavior.
/// </para>
/// <para>
/// <b>License Requirements:</b> Teams tier or higher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
public sealed record DeleteEntityCommand
{
    /// <summary>
    /// Gets the unique identifier of the entity to delete.
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// Gets the cascade mode for handling relationships.
    /// </summary>
    public RelationshipCascadeMode CascadeMode { get; init; } = RelationshipCascadeMode.DeleteRelationships;

    /// <summary>
    /// Gets the reason for deletion (recorded in audit trail).
    /// </summary>
    public string? ChangeReason { get; init; }

    /// <summary>
    /// Gets whether to force deletion even if validation warnings exist.
    /// </summary>
    public bool Force { get; init; }
}
