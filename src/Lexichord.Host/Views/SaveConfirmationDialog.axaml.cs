using Avalonia.Controls;
using Lexichord.Host.ViewModels;

namespace Lexichord.Host.Views;

/// <summary>
/// Code-behind for the save confirmation dialog.
/// </summary>
public partial class SaveConfirmationDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveConfirmationDialog"/> class.
    /// </summary>
    public SaveConfirmationDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance with a ViewModel.
    /// </summary>
    public SaveConfirmationDialog(SaveConfirmationViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseAction = Close;
    }
}
