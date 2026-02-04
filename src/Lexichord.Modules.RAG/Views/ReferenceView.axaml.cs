// =============================================================================
// File: ReferenceView.axaml.cs
// Project: Lexichord.Modules.RAG
// Description: Code-behind for the Reference Panel view.
// =============================================================================
// LOGIC: Handles keyboard interactions for the Reference Panel.
//   - Enter key triggers search command (search box focused).
//   - Up/Down arrow keys navigate results (v0.5.7a).
//   - Enter key opens selected result (results focused, v0.5.7a).
//   - Escape key clears search or returns focus (v0.5.7a).
// =============================================================================
// DEPENDENCIES:
//   - v0.5.7a: MoveSelectionUpCommand, MoveSelectionDownCommand, OpenSelectedResultCommand
// =============================================================================

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Lexichord.Modules.RAG.ViewModels;

namespace Lexichord.Modules.RAG.Views;

/// <summary>
/// Code-behind for the Reference Panel view.
/// </summary>
public partial class ReferenceView : UserControl
{
    /// <summary>
    /// Creates a new <see cref="ReferenceView"/> instance.
    /// </summary>
    public ReferenceView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the KeyDown event on the search box.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Key event args.</param>
    /// <remarks>
    /// LOGIC: Dispatches key events to appropriate commands:
    ///   - Enter: Execute search.
    ///   - Down: Move focus to results and select first item.
    /// Introduced in v0.4.6a, extended in v0.5.7a.
    /// </remarks>
    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ReferenceViewModel vm)
            return;

        switch (e.Key)
        {
            case Key.Enter:
                if (vm.SearchCommand.CanExecute(null))
                {
                    vm.SearchCommand.Execute(null);
                    e.Handled = true;
                }
                break;

            case Key.Down:
                // v0.5.7a: Arrow down from search box moves to first result.
                if (vm.CanNavigate)
                {
                    vm.SelectedResultIndex = 0;
                    // Focus transfer to results list would happen here via UI binding.
                    e.Handled = true;
                }
                break;

            case Key.Escape:
                // v0.5.7a: Escape clears the search query.
                if (!string.IsNullOrEmpty(vm.SearchQuery))
                {
                    vm.ClearSearchCommand.Execute(null);
                    e.Handled = true;
                }
                break;
        }
    }

    /// <summary>
    /// Handles keyboard navigation events on the results list.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Key event args.</param>
    /// <remarks>
    /// LOGIC: Handles navigation within the results:
    ///   - Up/Down: Move selection.
    ///   - Enter: Open selected result.
    ///   - Escape: Return focus to search box.
    /// Introduced in v0.5.7a.
    /// </remarks>
    private void OnResultsKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ReferenceViewModel vm)
            return;

        switch (e.Key)
        {
            case Key.Up:
                vm.MoveSelectionUpCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Down:
                vm.MoveSelectionDownCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Enter:
                if (vm.OpenSelectedResultCommand.CanExecute(null))
                {
                    vm.OpenSelectedResultCommand.Execute(null);
                    e.Handled = true;
                }
                break;

            case Key.Escape:
                // Return focus to search box.
                SearchBox.Focus();
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Handles pointer press on a result item to trigger navigation.
    /// </summary>
    /// <remarks>
    /// <b>Deprecated (v0.4.6b):</b> Navigation is now handled by
    /// <see cref="SearchResultItemView"/> via DoubleTapped event.
    /// This method is retained for backward compatibility but is unused.
    /// </remarks>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Pointer event args.</param>
    [Obsolete("Navigation moved to SearchResultItemView in v0.4.6b")]
    private void OnResultItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // v0.4.6b: Navigation is now handled by SearchResultItemView.OnDoubleTapped
    }
}

