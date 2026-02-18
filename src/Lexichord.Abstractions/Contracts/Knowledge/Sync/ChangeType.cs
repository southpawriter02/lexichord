// =============================================================================
// File: ChangeType.cs
// Project: Lexichord.Abstractions
// Description: Defines types of changes that can occur in the knowledge graph.
// =============================================================================
// LOGIC: When the knowledge graph changes (entities created/updated/deleted),
//   the sync service needs to identify affected documents. Each change type
//   has different implications for document sync.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Type of change that occurred in the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="GraphChange"/> to describe what happened:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="EntityCreated"/>: New entity added to graph.</description></item>
///   <item><description><see cref="EntityUpdated"/>: Existing entity modified.</description></item>
///   <item><description><see cref="EntityDeleted"/>: Entity removed from graph.</description></item>
///   <item><description><see cref="RelationshipCreated"/>: New relationship added.</description></item>
///   <item><description><see cref="RelationshipDeleted"/>: Relationship removed.</description></item>
///   <item><description><see cref="PropertyChanged"/>: Entity property modified.</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public enum ChangeType
{
    /// <summary>
    /// A new entity was created in the knowledge graph.
    /// </summary>
    /// <remarks>
    /// LOGIC: A new node was added to the graph. Documents referencing
    /// related entities may need to be notified. The <see cref="GraphChange.NewValue"/>
    /// contains the created entity.
    /// </remarks>
    EntityCreated = 0,

    /// <summary>
    /// An existing entity was updated in the knowledge graph.
    /// </summary>
    /// <remarks>
    /// LOGIC: An entity's core properties were modified. Documents that
    /// were the source of this entity should be flagged for review.
    /// <see cref="GraphChange.PreviousValue"/> contains the old state.
    /// </remarks>
    EntityUpdated = 1,

    /// <summary>
    /// An entity was deleted from the knowledge graph.
    /// </summary>
    /// <remarks>
    /// LOGIC: A node was removed from the graph. Source documents should
    /// be notified as claims referencing this entity are now orphaned.
    /// </remarks>
    EntityDeleted = 2,

    /// <summary>
    /// A new relationship was created between entities.
    /// </summary>
    /// <remarks>
    /// LOGIC: A new edge was added to the graph. Documents containing
    /// either endpoint entity may need to reflect this relationship.
    /// </remarks>
    RelationshipCreated = 3,

    /// <summary>
    /// A relationship was deleted from the knowledge graph.
    /// </summary>
    /// <remarks>
    /// LOGIC: An edge was removed from the graph. Documents that were
    /// the source of this relationship should be flagged for review.
    /// </remarks>
    RelationshipDeleted = 4,

    /// <summary>
    /// A specific property of an entity was changed.
    /// </summary>
    /// <remarks>
    /// LOGIC: A single property value was modified without changing
    /// the entity structure. More granular than <see cref="EntityUpdated"/>.
    /// <see cref="GraphChange.PreviousValue"/> and <see cref="GraphChange.NewValue"/>
    /// contain the old and new property values.
    /// </remarks>
    PropertyChanged = 5
}
