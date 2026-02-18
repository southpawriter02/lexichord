// -----------------------------------------------------------------------
// <copyright file="ChangeCard.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Code-behind for ChangeCard view (v0.7.6d).
//   Provides minimal code-behind for the change card control.
//   Most logic is handled by the ChangeCardViewModel.
//
//   Features:
//     - Click-to-expand behavior
//     - Keyboard accessibility (Enter/Space to toggle)
//
//   Introduced in: v0.7.6d as part of Document Comparison
// -----------------------------------------------------------------------

using Avalonia.Controls;
using Avalonia.Input;

namespace Lexichord.Modules.Agents.DocumentComparison.Views;

/// <summary>
/// Code-behind for the ChangeCard view.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Provides minimal code-behind for the change card control.
/// Most presentation logic is handled by the <see cref="ViewModels.ChangeCardViewModel"/>.
/// </para>
/// <para>
/// <b>Keyboard Accessibility:</b>
/// Supports Enter and Space keys to toggle the expanded state when focused.
/// </para>
/// <para><b>Introduced in:</b> v0.7.6d as part of Document Comparison</para>
/// </remarks>
public partial class ChangeCard : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeCard"/> class.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Initializes Avalonia components and sets up keyboard handlers.
    /// </remarks>
    public ChangeCard()
    {
        InitializeComponent();

        // LOGIC: Enable keyboard navigation
        KeyDown += OnKeyDown;
    }

    /// <summary>
    /// Handles keyboard input for accessibility.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The key event arguments.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Toggles the expanded state when Enter or Space is pressed.
    /// This provides keyboard accessibility for users who cannot use a mouse.
    /// </remarks>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // LOGIC: Toggle expand on Enter or Space for keyboard accessibility
        if (e.Key is Key.Enter or Key.Space)
        {
            if (DataContext is ViewModels.ChangeCardViewModel viewModel)
            {
                viewModel.ToggleExpandCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
