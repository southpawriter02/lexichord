using Avalonia.Controls;
using Lexichord.Modules.StatusBar.ViewModels;

namespace Lexichord.Modules.StatusBar.Views;

/// <summary>
/// Code-behind for the API key entry dialog.
/// </summary>
public partial class ApiKeyDialog : Window
{
    public ApiKeyDialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ApiKeyDialogViewModel vm)
        {
            vm.CloseRequested += OnCloseRequested;
        }
    }

    private void OnCloseRequested(object? sender, bool result)
    {
        Close(result);
    }
}
