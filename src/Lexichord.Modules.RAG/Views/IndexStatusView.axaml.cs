// =============================================================================
// File: IndexStatusView.axaml.cs
// Project: Lexichord.Modules.RAG
// Description: Code-behind for the Index Status View.
// Version: v0.4.7a
// =============================================================================

using Avalonia.Controls;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.ViewModels;

namespace Lexichord.Modules.RAG.Views;

/// <summary>
/// Code-behind for the Index Status View.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexStatusView"/> displays the status of indexed documents
/// and aggregate statistics in the Settings dialog.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7a as part of the Index Status View.
/// </para>
/// </remarks>
public partial class IndexStatusView : UserControl
{
    /// <summary>
    /// Initializes a new instance of <see cref="IndexStatusView"/>.
    /// </summary>
    public IndexStatusView()
    {
        InitializeComponent();
        
        // Wire up status filter combobox
        var statusFilterComboBox = this.FindControl<ComboBox>("StatusFilterComboBox");
        if (statusFilterComboBox != null)
        {
            statusFilterComboBox.SelectionChanged += OnStatusFilterSelectionChanged;
        }
    }

    /// <summary>
    /// Initializes a new instance with the specified ViewModel.
    /// </summary>
    /// <param name="viewModel">The ViewModel to bind.</param>
    public IndexStatusView(IndexStatusViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    /// <summary>
    /// Called when the view is attached to the visual tree.
    /// </summary>
    protected override async void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Load data when the view becomes visible
        if (DataContext is IndexStatusViewModel vm)
        {
            await vm.LoadAsync();
        }
    }

    /// <summary>
    /// Handles status filter combobox selection change.
    /// </summary>
    private void OnStatusFilterSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && 
            comboBox.SelectedItem is ComboBoxItem selectedItem &&
            DataContext is IndexStatusViewModel vm)
        {
            // Tag contains the IndexingStatus value or null for "All"
            vm.StatusFilter = selectedItem.Tag as IndexingStatus?;
        }
    }
}
