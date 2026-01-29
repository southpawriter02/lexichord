namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Properties for a dock node, varies by type.
/// </summary>
/// <param name="Title">Display title.</param>
/// <param name="Proportion">Size proportion (for ProportionalDock children).</param>
/// <param name="Orientation">Horizontal or Vertical (for ProportionalDock).</param>
/// <param name="Alignment">Left/Right/Top/Bottom (for ToolDock).</param>
/// <param name="IsCollapsed">Whether the dock is collapsed.</param>
/// <param name="IsActive">Whether this is the active dockable in parent.</param>
/// <param name="ActiveChildId">ID of the active child dockable.</param>
/// <param name="CanClose">Whether the dockable can be closed.</param>
/// <param name="CanFloat">Whether the dockable can be floated.</param>
/// <remarks>
/// LOGIC: Not all properties apply to all node types.
/// The serializer includes only non-null values.
/// </remarks>
public record DockNodeProperties(
    string? Title = null,
    double? Proportion = null,
    DockOrientation? Orientation = null,
    DockAlignment? Alignment = null,
    bool? IsCollapsed = null,
    bool? IsActive = null,
    string? ActiveChildId = null,
    bool? CanClose = null,
    bool? CanFloat = null
);
