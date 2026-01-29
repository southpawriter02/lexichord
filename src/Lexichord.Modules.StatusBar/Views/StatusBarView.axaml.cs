using Avalonia.Controls;
using Avalonia.Input;
using Lexichord.Modules.StatusBar.ViewModels;

namespace Lexichord.Modules.StatusBar.Views;

/// <summary>
/// Code-behind for the StatusBar view.
/// </summary>
/// <remarks>
/// LOGIC: The code-behind handles UI events that cannot be easily
/// bound through MVVM (like showing dialogs). All business logic
/// remains in the ViewModel.
/// </remarks>
public partial class StatusBarView : UserControl
{
    public StatusBarView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Called when the vault status panel is clicked.
    /// </summary>
    /// <remarks>
    /// LOGIC: When vault shows "No Key", clicking opens the API key
    /// entry dialog. This allows users to easily configure the vault.
    /// </remarks>
    private async void OnVaultStatusClicked(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not StatusBarViewModel vm)
            return;

        // Only show dialog if vault is empty (no key)
        if (!vm.IsVaultEmpty)
            return;

        // Get the parent window
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not Window window)
            return;

        // Create and show the API key dialog
        var dialog = new ApiKeyDialog
        {
            DataContext = new ApiKeyDialogViewModel(
                vm.VaultStatusService,
                vm.Logger)
        };

        var result = await dialog.ShowDialog<bool>(window);

        if (result)
        {
            // Refresh vault status after key was added
            await vm.RefreshVaultStatusCommand.ExecuteAsync(null);
        }
    }
}
