using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Lexichord.Modules.Style.ViewModels;

namespace Lexichord.Modules.Style.Views;

/// <summary>
/// Code-behind for the LexiconView.
/// </summary>
/// <remarks>
/// LOGIC: Handles view-specific behavior:
/// - Double-click to edit term
/// - Initial data loading
/// - Clipboard access via TopLevel (Avalonia 11)
///
/// Version: v0.2.5a
/// </remarks>
public partial class LexiconView : UserControl
{
    private LexiconViewModel? _viewModel;

    /// <summary>
    /// Initializes a new instance of the LexiconView.
    /// </summary>
    public LexiconView()
    {
        InitializeComponent();

        // LOGIC: Subscribe to double-click for edit action
        var grid = this.FindControl<DataGrid>("TermsGrid");
        if (grid is not null)
        {
            grid.DoubleTapped += OnGridDoubleTapped;
        }
    }

    /// <summary>
    /// Initializes the view with the specified ViewModel.
    /// </summary>
    /// <param name="viewModel">The LexiconViewModel instance.</param>
    public LexiconView(LexiconViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    /// <inheritdoc/>
    protected override async void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // LOGIC: Subscribe to ViewModel events
        if (DataContext is LexiconViewModel vm)
        {
            _viewModel = vm;
            _viewModel.CopyRequested += OnCopyRequested;
            await vm.LoadTermsAsync();
        }
    }

    /// <inheritdoc/>
    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        // LOGIC: Unsubscribe from ViewModel events
        if (_viewModel is not null)
        {
            _viewModel.CopyRequested -= OnCopyRequested;
            _viewModel = null;
        }
    }

    private void OnGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        // LOGIC: Double-click triggers edit command if licensed
        if (DataContext is LexiconViewModel vm && vm.EditSelectedCommand.CanExecute(null))
        {
            vm.EditSelectedCommand.Execute(null);
        }
    }

    private async void OnCopyRequested(object? sender, string text)
    {
        // LOGIC: Use Avalonia 11 TopLevel API for clipboard access
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard is IClipboard clipboard)
        {
            await clipboard.SetTextAsync(text);
        }
    }
}

