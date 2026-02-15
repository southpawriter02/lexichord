using Avalonia.Controls;
using Lexichord.Host.ViewModels;

namespace Lexichord.Host.Views;

/// <summary>
/// Code-behind for the message box dialog.
/// </summary>
public partial class MessageBoxDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBoxDialog"/> class.
    /// </summary>
    public MessageBoxDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance with a ViewModel.
    /// </summary>
    public MessageBoxDialog(MessageBoxViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.CloseAction = Close;
    }
}
