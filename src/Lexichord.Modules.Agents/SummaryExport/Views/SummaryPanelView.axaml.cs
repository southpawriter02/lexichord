// -----------------------------------------------------------------------
// <copyright file="SummaryPanelView.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Code-behind for the Summary Panel view (v0.7.6c).
//   Provides keyboard shortcuts for common operations.
//
//   Introduced in: v0.7.6c
// -----------------------------------------------------------------------

using Avalonia.Controls;
using Avalonia.Input;
using Lexichord.Modules.Agents.SummaryExport.ViewModels;

namespace Lexichord.Modules.Agents.SummaryExport.Views;

/// <summary>
/// Code-behind for the Summary Panel view.
/// </summary>
/// <remarks>
/// <para>
/// This view displays generated summaries and metadata with actions for
/// copying, exporting, and adding to frontmatter.
/// </para>
/// <para>
/// <b>Keyboard shortcuts:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Ctrl+R: Refresh summary</description></item>
///   <item><description>Ctrl+C: Copy summary to clipboard</description></item>
///   <item><description>Ctrl+S: Export to file</description></item>
///   <item><description>Escape: Close panel</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6c as part of the Summarizer Agent Export Formats.
/// </para>
/// </remarks>
/// <seealso cref="SummaryPanelViewModel"/>
public partial class SummaryPanelView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SummaryPanelView"/> class.
    /// </summary>
    public SummaryPanelView()
    {
        InitializeComponent();

        // Wire up keyboard shortcuts
        KeyDown += OnKeyDown;
    }

    /// <summary>
    /// Handles keyboard shortcuts for the summary panel.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The key event arguments.</param>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not SummaryPanelViewModel vm)
        {
            return;
        }

        // Ctrl+R: Refresh summary
        if (e.Key == Key.R && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (vm.RefreshCommand.CanExecute(null))
            {
                vm.RefreshCommand.Execute(null);
            }
            e.Handled = true;
            return;
        }

        // Ctrl+C: Copy summary to clipboard (only when not in text input)
        if (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.FocusManager?.GetFocusedElement() is not TextBox)
            {
                if (vm.CopySummaryCommand.CanExecute(null))
                {
                    vm.CopySummaryCommand.Execute(null);
                }
                e.Handled = true;
                return;
            }
        }

        // Ctrl+S: Export to file
        if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (vm.ExportFileCommand.CanExecute(null))
            {
                vm.ExportFileCommand.Execute(null);
            }
            e.Handled = true;
            return;
        }

        // Escape: Close panel
        if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
        {
            vm.CloseCommand.Execute(null);
            e.Handled = true;
        }
    }
}
