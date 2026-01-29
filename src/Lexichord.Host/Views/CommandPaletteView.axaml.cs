using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Lexichord.Host.ViewModels.CommandPalette;

namespace Lexichord.Host.Views;

/// <summary>
/// Code-behind for CommandPaletteView.
/// </summary>
/// <remarks>
/// LOGIC: Handles keyboard navigation and focus management.
/// Most logic is in ViewModel; this handles Avalonia-specific input.
/// </remarks>
public partial class CommandPaletteView : UserControl
{
    public CommandPaletteView()
    {
        InitializeComponent();

        // LOGIC: Focus search input when palette becomes visible
        PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(IsVisible) && IsVisible)
            {
                // Use dispatcher to ensure UI is ready
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    SearchInput.Focus();
                    SearchInput.SelectAll();
                }, Avalonia.Threading.DispatcherPriority.Loaded);
            }
        };
    }

    private void OnSearchInputKeyDown(object? sender, KeyEventArgs e)
    {
        var vm = DataContext as CommandPaletteViewModel;
        if (vm is null) return;

        switch (e.Key)
        {
            case Key.Down:
                vm.MoveSelectionDownCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Up:
                vm.MoveSelectionUpCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.PageDown:
                vm.MoveSelectionPageDownCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.PageUp:
                vm.MoveSelectionPageUpCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Home when e.KeyModifiers == KeyModifiers.Control:
                vm.SelectFirstCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.End when e.KeyModifiers == KeyModifiers.Control:
                vm.SelectLastCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Enter:
                vm.ExecuteSelectedCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Escape:
                vm.HideCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Tab:
                // Future: Cycle through modes
                e.Handled = true;
                break;
        }
    }

    private void OnResultDoubleTapped(object? sender, TappedEventArgs e)
    {
        var vm = DataContext as CommandPaletteViewModel;
        vm?.ExecuteSelectedCommand.Execute(null);
    }

    private void OnOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // LOGIC: Click outside modal closes palette
        var vm = DataContext as CommandPaletteViewModel;
        vm?.HideCommand.Execute(null);
        e.Handled = true;
    }

    private void OnModalPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // LOGIC: Prevent clicks on modal from closing palette
        e.Handled = true;
    }
}
