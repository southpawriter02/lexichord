// =============================================================================
// File: EntityCrudEnums.cs
// Project: Lexichord.Abstractions
// Description: Enumerations for entity CRUD operations.
// =============================================================================
// LOGIC: Defines behavior options for entity merging and deletion operations.
//
// Enumerations:
//   - PropertyMergeStrategy: Controls how properties are combined during merge
//   - RelationshipCascadeMode: Controls how relationships are handled during delete
//
// v0.4.7g: Entity CRUD Operations
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Specifies how properties should be merged when combining entities.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="MergeEntitiesCommand"/> to control property resolution
/// when source and target entities have conflicting property values.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
public enum PropertyMergeStrategy
{
    /// <summary>
    /// Keep the target entity's property values when conflicts occur.
    /// Source entity properties are only added if not present in target.
    /// </summary>
    KeepTarget = 0,

    /// <summary>
    /// Overwrite target entity's properties with source entity values.
    /// Target-only properties are preserved.
    /// </summary>
    KeepSource = 1,

    /// <summary>
    /// Combine all properties from both entities.
    /// For conflicts, concatenate values as arrays where applicable.
    /// </summary>
    MergeAll = 2,

    /// <summary>
    /// For string properties, keep the longer value.
    /// For collections, keep the one with more items.
    /// For other types, behaves like <see cref="KeepTarget"/>.
    /// </summary>
    KeepLongest = 3
}

/// <summary>
/// Specifies how relationships should be handled when deleting an entity.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="DeleteEntityCommand"/> to control cascade behavior
/// for relationships connected to the entity being deleted.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
public enum RelationshipCascadeMode
{
    /// <summary>
    /// Delete all relationships connected to the entity.
    /// This is the default and safest option for maintaining graph integrity.
    /// </summary>
    DeleteRelationships = 0,

    /// <summary>
    /// Fail the delete operation if the entity has any relationships.
    /// Forces explicit relationship cleanup before entity deletion.
    /// </summary>
    FailIfHasRelationships = 1,

    /// <summary>
    /// Leave relationships pointing to the deleted entity (orphaned).
    /// Use with caution — may leave the graph in an inconsistent state.
    /// </summary>
    OrphanRelationships = 2
}
