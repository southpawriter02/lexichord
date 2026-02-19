// =============================================================================
// File: FlagReason.cs
// Project: Lexichord.Abstractions
// Description: Reasons why a document was flagged for review.
// =============================================================================
// LOGIC: Categorizes the cause of a document flag to enable appropriate
//   handling workflows and priority assignment.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Describes why a document was flagged for review.
/// </summary>
/// <remarks>
/// <para>
/// Flags are created when graph changes may affect document content.
/// The reason determines:
/// </para>
/// <list type="bullet">
///   <item><b>Priority:</b> Entity deletion is typically higher priority than property updates.</item>
///   <item><b>Suggested action:</b> Different reasons lead to different recommendations.</item>
///   <item><b>UI presentation:</b> Icons and labels vary by reason.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public enum FlagReason
{
    /// <summary>
    /// The primary value of a referenced entity changed.
    /// </summary>
    /// <remarks>
    /// LOGIC: The entity's name, title, or main identifying value was
    /// modified. Documents referencing this entity may have stale names.
    /// Typically high priority - content may be factually incorrect.
    /// </remarks>
    EntityValueChanged = 0,

    /// <summary>
    /// Secondary properties of a referenced entity were updated.
    /// </summary>
    /// <remarks>
    /// LOGIC: Metadata, descriptions, or non-primary properties changed.
    /// Lower impact than value changes - content may still be accurate
    /// but could benefit from updated details.
    /// </remarks>
    EntityPropertiesUpdated = 1,

    /// <summary>
    /// A referenced entity was deleted from the graph.
    /// </summary>
    /// <remarks>
    /// LOGIC: The entity no longer exists. Documents referencing it
    /// contain broken references. Typically critical priority - requires
    /// content update or removal of references.
    /// </remarks>
    EntityDeleted = 2,

    /// <summary>
    /// A new relationship was created involving a referenced entity.
    /// </summary>
    /// <remarks>
    /// LOGIC: The entity has new connections to other entities.
    /// Documents may benefit from mentioning these new relationships.
    /// Medium priority - informational enhancement opportunity.
    /// </remarks>
    NewRelationship = 3,

    /// <summary>
    /// A relationship involving a referenced entity was removed.
    /// </summary>
    /// <remarks>
    /// LOGIC: A connection between entities no longer exists.
    /// Documents describing this relationship may be inaccurate.
    /// Medium to high priority depending on relationship importance.
    /// </remarks>
    RelationshipRemoved = 4,

    /// <summary>
    /// A manual sync request was made by a user.
    /// </summary>
    /// <remarks>
    /// LOGIC: User explicitly requested synchronization review.
    /// Not triggered by graph changes but by user action.
    /// Priority set by user or defaults to medium.
    /// </remarks>
    ManualSyncRequested = 5,

    /// <summary>
    /// A conflict was detected between document and graph state.
    /// </summary>
    /// <remarks>
    /// LOGIC: Document content conflicts with graph data.
    /// May occur when both document and graph were modified independently.
    /// High priority - requires manual resolution to determine correct state.
    /// </remarks>
    ConflictDetected = 6
}
