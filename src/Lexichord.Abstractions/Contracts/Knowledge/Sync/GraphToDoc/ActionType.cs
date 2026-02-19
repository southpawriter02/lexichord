// =============================================================================
// File: ActionType.cs
// Project: Lexichord.Abstractions
// Description: Types of suggested actions for document review.
// =============================================================================
// LOGIC: Categorizes suggested actions to help users understand what
//   changes may be needed when reviewing flagged documents.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Type of action suggested for document review.
/// </summary>
/// <remarks>
/// <para>
/// When a document is flagged, the system may suggest specific actions:
/// </para>
/// <list type="bullet">
///   <item><b>UpdateReferences:</b> Update entity references to new values.</item>
///   <item><b>AddInformation:</b> Add new information from graph.</item>
///   <item><b>RemoveInformation:</b> Remove outdated information.</item>
///   <item><b>ManualReview:</b> Complex change requiring human judgment.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public enum ActionType
{
    /// <summary>
    /// Update references to match new entity values.
    /// </summary>
    /// <remarks>
    /// LOGIC: Entity names or identifiers changed. Document references
    /// should be updated to use the new values.
    /// Example: "ProductX" renamed to "ProductY" - update all mentions.
    /// </remarks>
    UpdateReferences = 0,

    /// <summary>
    /// Add new information from the graph.
    /// </summary>
    /// <remarks>
    /// LOGIC: New relationships or properties were added to entities.
    /// Document may benefit from including this new information.
    /// Example: New integration added - document may want to mention it.
    /// </remarks>
    AddInformation = 1,

    /// <summary>
    /// Remove outdated information.
    /// </summary>
    /// <remarks>
    /// LOGIC: Entity or relationship was deleted. Document contains
    /// references to content that no longer exists.
    /// Example: Deprecated API removed - remove documentation about it.
    /// </remarks>
    RemoveInformation = 2,

    /// <summary>
    /// Requires manual review to determine appropriate action.
    /// </summary>
    /// <remarks>
    /// LOGIC: The change is complex or ambiguous. System cannot
    /// confidently suggest a specific action. User must review
    /// the changes and decide on appropriate updates.
    /// Example: Conflicting changes or semantic shifts in meaning.
    /// </remarks>
    ManualReview = 3
}
