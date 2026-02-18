// -----------------------------------------------------------------------
// <copyright file="ComparisonView.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Code-behind for ComparisonView (v0.7.6d).
//   Handles keyboard shortcuts for the document comparison panel.
//
//   Keyboard Shortcuts:
//     - Escape: Close the panel
//     - Ctrl+R: Refresh comparison
//     - Ctrl+C: Copy diff to clipboard (when panel focused)
//     - Ctrl+E: Export report
//
//   Introduced in: v0.7.6d as part of Document Comparison
// -----------------------------------------------------------------------

using Avalonia.Controls;
using Avalonia.Input;

namespace Lexichord.Modules.Agents.DocumentComparison.Views;

/// <summary>
/// Code-behind for the ComparisonView.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Handles keyboard shortcuts for the document comparison panel.
/// Most presentation logic is delegated to the <see cref="ViewModels.ComparisonViewModel"/>.
/// </para>
/// <para>
/// <b>Keyboard Shortcuts:</b>
/// <list type="table">
/// <listheader>
/// <term>Shortcut</term>
/// <description>Action</description>
/// </listheader>
/// <item><term>Escape</term><description>Close the panel</description></item>
/// <item><term>Ctrl+R</term><description>Refresh comparison</description></item>
/// <item><term>Ctrl+C</term><description>Copy diff to clipboard</description></item>
/// <item><term>Ctrl+E</term><description>Export report</description></item>
/// </list>
/// </para>
/// <para><b>Introduced in:</b> v0.7.6d as part of Document Comparison</para>
/// </remarks>
public partial class ComparisonView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComparisonView"/> class.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Initializes Avalonia components and sets up keyboard shortcut handlers.
    /// </remarks>
    public ComparisonView()
    {
        InitializeComponent();

        // LOGIC: Set up keyboard shortcut handling
        KeyDown += OnKeyDown;
    }

    /// <summary>
    /// Handles keyboard shortcuts for the comparison view.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The key event arguments.</param>
    /// <remarks>
    /// <b>LOGIC:</b> Routes keyboard shortcuts to the appropriate ViewModel commands.
    /// Shortcuts are consistent with other panels in the application.
    /// </remarks>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // LOGIC: Get ViewModel for command execution
        if (DataContext is not ViewModels.ComparisonViewModel viewModel)
        {
            return;
        }

        // LOGIC: Check for modifier keys
        var isCtrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        switch (e.Key)
        {
            // LOGIC: Escape closes the panel
            case Key.Escape:
                viewModel.CloseCommand.Execute(null);
                e.Handled = true;
                break;

            // LOGIC: Ctrl+R refreshes the comparison
            case Key.R when isCtrl:
                if (viewModel.RefreshCommand.CanExecute(null))
                {
                    viewModel.RefreshCommand.Execute(null);
                }
                e.Handled = true;
                break;

            // LOGIC: Ctrl+C copies diff to clipboard (when panel focused, not text selected)
            case Key.C when isCtrl:
                // LOGIC: Only handle if we have a result and no text is selected
                // Text selection copy is handled by TextBlock controls
                if (viewModel.CopyDiffCommand.CanExecute(null))
                {
                    viewModel.CopyDiffCommand.Execute(null);
                    e.Handled = true;
                }
                break;

            // LOGIC: Ctrl+E exports the comparison report
            case Key.E when isCtrl:
                if (viewModel.ExportReportCommand.CanExecute(null))
                {
                    viewModel.ExportReportCommand.Execute(null);
                }
                e.Handled = true;
                break;
        }
    }
}
