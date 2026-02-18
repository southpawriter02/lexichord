// =============================================================================
// File: GraphChange.cs
// Project: Lexichord.Abstractions
// Description: Record representing a change that occurred in the knowledge graph.
// =============================================================================
// LOGIC: When entities or relationships in the graph are modified, a
//   GraphChange record captures what changed. This is used by the sync
//   service to find affected documents and propagate changes.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// Dependencies: ChangeType (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// A change that occurred in the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// Captures details about a graph modification:
/// </para>
/// <list type="bullet">
///   <item><b>Entity:</b> The entity that was modified.</item>
///   <item><b>Change Type:</b> What kind of modification (create/update/delete).</item>
///   <item><b>Values:</b> Previous and new values for auditing.</item>
///   <item><b>Attribution:</b> Who made the change and when.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var change = new GraphChange
/// {
///     EntityId = entity.Id,
///     ChangeType = ChangeType.EntityUpdated,
///     PreviousValue = oldEntity,
///     NewValue = newEntity,
///     ChangedBy = currentUser.Id,
///     ChangedAt = DateTimeOffset.UtcNow
/// };
/// var affectedDocs = await syncService.GetAffectedDocumentsAsync(change);
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public record GraphChange
{
    /// <summary>
    /// The ID of the entity that changed.
    /// </summary>
    /// <value>The unique identifier of the modified entity.</value>
    /// <remarks>
    /// LOGIC: Used to find documents that reference this entity.
    /// For relationship changes, this is the source entity ID.
    /// </remarks>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// Type of change that occurred.
    /// </summary>
    /// <value>The nature of the modification.</value>
    /// <remarks>
    /// LOGIC: Determines how affected documents should be flagged.
    /// Deletes may orphan claims; updates may create conflicts.
    /// </remarks>
    public required ChangeType ChangeType { get; init; }

    /// <summary>
    /// Previous value before the change.
    /// </summary>
    /// <value>
    /// The old state for updates/deletes. Null for creates.
    /// Type depends on what was modified (entity, relationship, property).
    /// </value>
    /// <remarks>
    /// LOGIC: Boxed as object to support any type. Used for auditing
    /// and to help users understand what changed.
    /// </remarks>
    public object? PreviousValue { get; init; }

    /// <summary>
    /// New value after the change.
    /// </summary>
    /// <value>
    /// The new state for creates/updates. The deleted entity for deletes.
    /// </value>
    /// <remarks>
    /// LOGIC: Required for all change types. For deletes, contains
    /// the entity that was removed (for reference and potential undo).
    /// </remarks>
    public required object NewValue { get; init; }

    /// <summary>
    /// ID of the user who made the change.
    /// </summary>
    /// <value>
    /// The user who initiated the modification. Null for system changes.
    /// </value>
    /// <remarks>
    /// LOGIC: Used for audit trails and determining if the change
    /// came from a different user (potential conflict source).
    /// </remarks>
    public Guid? ChangedBy { get; init; }

    /// <summary>
    /// Timestamp when the change occurred.
    /// </summary>
    /// <value>UTC timestamp of the modification.</value>
    /// <remarks>
    /// LOGIC: Used for conflict detection via timestamp comparison.
    /// If document and graph both changed after last sync, it's
    /// a potential <see cref="ConflictType.ConcurrentEdit"/>.
    /// </remarks>
    public required DateTimeOffset ChangedAt { get; init; }
}
