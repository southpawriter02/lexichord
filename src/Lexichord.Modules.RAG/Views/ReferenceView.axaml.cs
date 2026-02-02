// =============================================================================
// File: ReferenceView.axaml.cs
// Project: Lexichord.Modules.RAG
// Description: Code-behind for the Reference Panel view.
// =============================================================================
// LOGIC: Handles keyboard interactions for the search input.
//   - Enter key triggers search command.
//   - Result item click triggers navigation.
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
    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ReferenceViewModel vm)
        {
            if (vm.SearchCommand.CanExecute(null))
            {
                vm.SearchCommand.Execute(null);
                e.Handled = true;
            }
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
