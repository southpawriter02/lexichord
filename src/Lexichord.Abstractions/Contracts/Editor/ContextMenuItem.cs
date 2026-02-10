// -----------------------------------------------------------------------
// <copyright file="ContextMenuItem.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows.Input;

namespace Lexichord.Abstractions.Contracts.Editor;

/// <summary>
/// Represents a context menu item that can be registered with the editor.
/// </summary>
/// <remarks>
/// <para>
/// Context menu items are registered via
/// <see cref="IEditorService.RegisterContextMenuItem"/> and appear in the
/// editor's right-click context menu. Items are grouped by <see cref="Group"/>
/// and ordered by <see cref="Order"/> within each group.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7a as part of the Selection Context feature.
/// </para>
/// </remarks>
public class ContextMenuItem
{
    /// <summary>
    /// Gets or sets the display text for the menu item.
    /// </summary>
    public required string Header { get; set; }

    /// <summary>
    /// Gets or sets the icon identifier (e.g., "mdi-robot-happy").
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the command to execute when the menu item is clicked.
    /// </summary>
    public required ICommand Command { get; set; }

    /// <summary>
    /// Gets or sets a function that determines whether the menu item is visible.
    /// </summary>
    /// <remarks>
    /// When null, the menu item is always visible.
    /// </remarks>
    public Func<bool>? IsVisible { get; set; }

    /// <summary>
    /// Gets or sets a function that determines whether the menu item is enabled.
    /// </summary>
    /// <remarks>
    /// When null, the menu item is always enabled.
    /// </remarks>
    public Func<bool>? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the group name for visual separation in the menu.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Gets or sets the sort order within the group. Lower values appear first.
    /// </summary>
    public int Order { get; set; }
}
