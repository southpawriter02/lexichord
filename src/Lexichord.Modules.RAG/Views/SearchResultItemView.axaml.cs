// =============================================================================
// File: SearchResultItemView.axaml.cs
// Project: Lexichord.Modules.RAG
// Description: Code-behind for the SearchResultItemView UserControl.
// =============================================================================
// LOGIC: Handles the DoubleTapped event to navigate to the source document
//   at the chunk's location via the NavigateCommand on the ViewModel.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.6b: SearchResultItemViewModel with NavigateCommand
// =============================================================================

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Lexichord.Modules.RAG.ViewModels;

namespace Lexichord.Modules.RAG.Views;

/// <summary>
/// Code-behind for the SearchResultItemView UserControl.
/// </summary>
/// <remarks>
/// <para>
/// Handles user interaction events for search result items, specifically
/// the double-tap gesture to navigate to the source document.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6b as part of the Search Result Item View.
/// </para>
/// </remarks>
public partial class SearchResultItemView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchResultItemView"/> class.
    /// </summary>
    public SearchResultItemView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the DoubleTapped event to navigate to the source document.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    /// <remarks>
    /// LOGIC: Retrieves the DataContext as SearchResultItemViewModel and
    /// executes the NavigateCommand if available and can execute.
    /// </remarks>
    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is SearchResultItemViewModel vm && vm.NavigateCommand.CanExecute(null))
        {
            vm.NavigateCommand.Execute(null);
            e.Handled = true;
        }
    }
}
