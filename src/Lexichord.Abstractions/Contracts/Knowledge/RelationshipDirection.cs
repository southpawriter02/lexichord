// =============================================================================
// File: RelationshipDirection.cs
// Project: Lexichord.Abstractions
// Description: Enum for relationship direction filtering.
// =============================================================================
// LOGIC: Defines the direction filter options for viewing entity relationships
//   in the Relationship Viewer panel. Used to filter incoming, outgoing, or
//   all relationships connected to an entity.
//
// v0.4.7h: Relationship Viewer (Knowledge Graph Browser)
// Dependencies: None (pure abstraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Direction options for filtering entity relationships.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RelationshipDirection"/> enum defines the direction filter
/// options for the Relationship Viewer panel (v0.4.7h). It controls which
/// relationships are displayed based on their orientation relative to the
/// selected entity.
/// </para>
/// <para>
/// <b>Usage:</b> Applied in the <c>RelationshipViewerPanelViewModel</c> to
/// filter the tree view contents.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7h as part of the Relationship Viewer.
/// </para>
/// </remarks>
public enum RelationshipDirection
{
    /// <summary>
    /// Show all relationships (both incoming and outgoing).
    /// </summary>
    /// <remarks>
    /// LOGIC: Default filter state. Displays all relationships where the
    /// entity is either the source (<see cref="Outgoing"/>) or target
    /// (<see cref="Incoming"/>) of the relationship.
    /// </remarks>
    Both = 0,

    /// <summary>
    /// Show only relationships where the entity is the target.
    /// </summary>
    /// <remarks>
    /// LOGIC: Filters to relationships where <c>ToEntityId</c> equals the
    /// selected entity's ID. Useful for understanding what references or
    /// depends on this entity.
    /// </remarks>
    Incoming = 1,

    /// <summary>
    /// Show only relationships where the entity is the source.
    /// </summary>
    /// <remarks>
    /// LOGIC: Filters to relationships where <c>FromEntityId</c> equals the
    /// selected entity's ID. Useful for understanding what this entity
    /// references or depends on.
    /// </remarks>
    Outgoing = 2
}
