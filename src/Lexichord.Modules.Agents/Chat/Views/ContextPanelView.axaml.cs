// -----------------------------------------------------------------------
// <copyright file="ContextPanelView.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia.Controls;
using Avalonia.Input;
using Lexichord.Modules.Agents.Chat.ViewModels;

namespace Lexichord.Modules.Agents.Chat.Views;

/// <summary>
/// Code-behind for the Context Panel view.
/// </summary>
/// <remarks>
/// <para>
/// This view provides a collapsible panel displaying active context sources
/// that will be injected into AI prompts. It supports keyboard shortcuts for
/// common operations.
/// </para>
/// <para>
/// Keyboard shortcuts:
/// </para>
/// <list type="bullet">
///   <item><description>Ctrl+R: Refresh context</description></item>
///   <item><description>Space: Toggle panel expansion</description></item>
///   <item><description>Ctrl+Shift+D: Disable all sources</description></item>
///   <item><description>Ctrl+Shift+E: Enable all sources</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ContextPanelViewModel"/>
public partial class ContextPanelView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContextPanelView"/> class.
    /// </summary>
    public ContextPanelView()
    {
        InitializeComponent();

        // Wire up keyboard shortcuts
        KeyDown += OnKeyDown;
    }

    /// <summary>
    /// Handles keyboard shortcuts for the context panel.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The key event arguments.</param>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ContextPanelViewModel vm)
        {
            return;
        }

        // Ctrl+R: Refresh context
        if (e.Key == Key.R && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            vm.RefreshContextCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Space: Toggle expansion (when focused on panel, not in text input)
        if (e.Key == Key.Space && e.KeyModifiers == KeyModifiers.None)
        {
            // Only handle if not in a text input
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.FocusManager?.GetFocusedElement() is not TextBox)
            {
                vm.ToggleExpandedCommand.Execute(null);
                e.Handled = true;
                return;
            }
        }

        // Ctrl+Shift+D: Disable all sources
        if (e.Key == Key.D && e.KeyModifiers.HasFlag(KeyModifiers.Control | KeyModifiers.Shift))
        {
            vm.DisableAllSourcesCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Ctrl+Shift+E: Enable all sources
        if (e.Key == Key.E && e.KeyModifiers.HasFlag(KeyModifiers.Control | KeyModifiers.Shift))
        {
            vm.EnableAllSourcesCommand.Execute(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles the header press to toggle panel expansion.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The pointer pressed event arguments.</param>
    private void OnHeaderPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ContextPanelViewModel vm)
        {
            vm.ToggleExpandedCommand.Execute(null);
            e.Handled = true;
        }
    }
}
