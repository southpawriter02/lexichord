// =============================================================================
// File: RelationshipItemViewModel.cs
// Project: Lexichord.Modules.Knowledge
// Description: View model representing a relationship in the Entity Detail View.
// =============================================================================
// LOGIC: Provides a display-ready representation of an entity relationship
//   including direction indicator and linked entity information for navigation.
//
// v0.4.7f: Entity Detail View (Knowledge Graph Browser)
// Dependencies: None (pure view model record)
// =============================================================================

namespace Lexichord.Modules.Knowledge.UI.ViewModels;

/// <summary>
/// View model representing a single relationship in the Entity Detail View.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RelationshipItemViewModel"/> wraps a <see cref="KnowledgeRelationship"/>
/// with display-ready values for the Relationships section of the Entity Detail View.
/// Includes navigation support via <see cref="OtherEntityId"/>.
/// </para>
/// <para>
/// <b>Direction:</b> The <see cref="Direction"/> property indicates whether this
/// relationship originates from ("→") or points to ("←") the current entity.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7f as part of the Entity Detail View.
/// </para>
/// </remarks>
public sealed record RelationshipItemViewModel
{
    /// <summary>
    /// Gets the relationship's unique identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the relationship type name.
    /// </summary>
    /// <value>
    /// The relationship type (e.g., "CONTAINS", "ACCEPTS", "DEPENDS_ON").
    /// </value>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the direction indicator.
    /// </summary>
    /// <value>
    /// "→" if this relationship originates from the current entity (outgoing),
    /// "←" if it points to the current entity (incoming).
    /// </value>
    public string Direction { get; init; } = "→";

    /// <summary>
    /// Gets the ID of the other entity in this relationship.
    /// </summary>
    /// <value>
    /// The GUID of the entity on the other end of the relationship.
    /// Used for navigation when the user clicks on the relationship.
    /// </value>
    public Guid OtherEntityId { get; init; }

    /// <summary>
    /// Gets the name of the other entity.
    /// </summary>
    /// <value>The display name of the related entity.</value>
    public string OtherEntityName { get; init; } = "";

    /// <summary>
    /// Gets the type of the other entity.
    /// </summary>
    /// <value>The entity type (e.g., "Endpoint", "Concept") of the related entity.</value>
    public string OtherEntityType { get; init; } = "";

    /// <summary>
    /// Gets the icon for this relationship type.
    /// </summary>
    /// <value>
    /// The icon from <see cref="RelationshipTypeSchema.Icon"/>, or "↔" if not specified.
    /// </value>
    public string Icon { get; init; } = "↔";
}
