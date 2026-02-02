// =============================================================================
// File: RelationshipTreeNodeViewModel.cs
// Project: Lexichord.Modules.Knowledge
// Description: Tree node view model for hierarchical relationship display.
// =============================================================================
// LOGIC: Represents a node in the relationship tree hierarchy. Root nodes are
//   grouped by relationship type; leaf nodes represent individual relationships.
//   Supports expansion state tracking for tree view rendering.
//
// v0.4.7h: Relationship Viewer (Knowledge Graph Browser)
// Dependencies: CommunityToolkit.Mvvm
// =============================================================================

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Contracts.Knowledge;

namespace Lexichord.Modules.Knowledge.UI.ViewModels;

/// <summary>
/// View model for a node in the relationship tree hierarchy.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RelationshipTreeNodeViewModel"/> represents a single node in
/// the hierarchical relationship tree displayed in the Relationship Viewer panel.
/// Nodes can be either:
/// <list type="bullet">
///   <item><b>Group nodes:</b> Represent a relationship type with child nodes.</item>
///   <item><b>Leaf nodes:</b> Represent individual entity connections.</item>
/// </list>
/// </para>
/// <para>
/// <b>Tree Structure:</b>
/// <code>
/// Outgoing →
///   └ CONTAINS (3)
///       ├ Endpoint: GET /api/users
///       ├ Endpoint: POST /api/users
///       └ Endpoint: DELETE /api/users/{id}
///   └ DEPENDS_ON (1)
///       └ Service: AuthService
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7h as part of the Relationship Viewer.
/// </para>
/// </remarks>
public partial class RelationshipTreeNodeViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the display label for this node.
    /// </summary>
    /// <value>
    /// For group nodes: relationship type with count (e.g., "CONTAINS (3)").
    /// For leaf nodes: entity info (e.g., "Endpoint: GET /api/users").
    /// </value>
    [ObservableProperty]
    private string _label = string.Empty;

    /// <summary>
    /// Gets or sets the relationship type for this node.
    /// </summary>
    /// <value>
    /// The relationship type (e.g., "CONTAINS", "DEPENDS_ON").
    /// For direction header nodes, this may be empty.
    /// </value>
    [ObservableProperty]
    private string _type = string.Empty;

    /// <summary>
    /// Gets or sets the direction of this relationship.
    /// </summary>
    /// <value>
    /// <see cref="RelationshipDirection.Incoming"/> or <see cref="RelationshipDirection.Outgoing"/>.
    /// </value>
    [ObservableProperty]
    private RelationshipDirection _direction;

    /// <summary>
    /// Gets or sets whether this node is expanded in the tree.
    /// </summary>
    /// <value><c>true</c> if the node is expanded; otherwise <c>false</c>.</value>
    /// <remarks>
    /// LOGIC: Bound to the TreeView's expansion state. Persists during filter
    /// changes to maintain user's view preferences.
    /// </remarks>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// Gets or sets whether this is a group node (has children).
    /// </summary>
    /// <value><c>true</c> if this node is a group; otherwise <c>false</c>.</value>
    /// <remarks>
    /// LOGIC: Group nodes display with bold styling and expand/collapse controls.
    /// Leaf nodes display the entity link without expansion controls.
    /// </remarks>
    [ObservableProperty]
    private bool _isGroup;

    /// <summary>
    /// Gets or sets the entity ID for leaf nodes (navigation target).
    /// </summary>
    /// <value>
    /// The GUID of the related entity. Only populated for leaf nodes.
    /// </value>
    public Guid? EntityId { get; init; }

    /// <summary>
    /// Gets or sets the entity name for leaf nodes.
    /// </summary>
    /// <value>The name of the related entity.</value>
    public string EntityName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type for leaf nodes.
    /// </summary>
    /// <value>The type of the related entity (e.g., "Endpoint", "Concept").</value>
    public string EntityType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the child nodes of this group node.
    /// </summary>
    /// <value>
    /// A collection of child nodes. Empty for leaf nodes.
    /// </value>
    public ObservableCollection<RelationshipTreeNodeViewModel> Children { get; } = new();

    /// <summary>
    /// Gets the count of child nodes.
    /// </summary>
    public int ChildCount => Children.Count;

    /// <summary>
    /// Creates a group node for a relationship type.
    /// </summary>
    /// <param name="type">The relationship type name.</param>
    /// <param name="direction">The direction of relationships in this group.</param>
    /// <param name="count">The number of relationships in this group.</param>
    /// <returns>A new group node view model.</returns>
    public static RelationshipTreeNodeViewModel CreateGroupNode(
        string type,
        RelationshipDirection direction,
        int count)
    {
        return new RelationshipTreeNodeViewModel
        {
            Label = $"{type} ({count})",
            Type = type,
            Direction = direction,
            IsGroup = true,
            IsExpanded = false
        };
    }

    /// <summary>
    /// Creates a leaf node for an individual relationship.
    /// </summary>
    /// <param name="entityId">The related entity's ID.</param>
    /// <param name="entityName">The related entity's name.</param>
    /// <param name="entityType">The related entity's type.</param>
    /// <param name="relationshipType">The relationship type.</param>
    /// <param name="direction">The relationship direction.</param>
    /// <returns>A new leaf node view model.</returns>
    public static RelationshipTreeNodeViewModel CreateLeafNode(
        Guid entityId,
        string entityName,
        string entityType,
        string relationshipType,
        RelationshipDirection direction)
    {
        return new RelationshipTreeNodeViewModel
        {
            Label = $"{entityType}: {entityName}",
            Type = relationshipType,
            Direction = direction,
            IsGroup = false,
            EntityId = entityId,
            EntityName = entityName,
            EntityType = entityType
        };
    }

    /// <summary>
    /// Creates a direction header node.
    /// </summary>
    /// <param name="direction">The direction (Incoming or Outgoing).</param>
    /// <returns>A new direction header node.</returns>
    public static RelationshipTreeNodeViewModel CreateDirectionHeader(RelationshipDirection direction)
    {
        var label = direction == RelationshipDirection.Incoming
            ? "← Incoming"
            : "Outgoing →";

        return new RelationshipTreeNodeViewModel
        {
            Label = label,
            Type = string.Empty,
            Direction = direction,
            IsGroup = true,
            IsExpanded = true
        };
    }
}
