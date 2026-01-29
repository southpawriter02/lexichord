using Avalonia.Controls;
using Lexichord.Host.ViewModels;

namespace Lexichord.Host.Views;

/// <summary>
/// Dialog for prompting the user to save unsaved changes before closing.
/// </summary>
public partial class SaveChangesDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveChangesDialog"/> class.
    /// </summary>
    public SaveChangesDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveChangesDialog"/> class with a ViewModel.
    /// </summary>
    /// <param name="viewModel">The ViewModel for this dialog.</param>
    public SaveChangesDialog(SaveChangesDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
