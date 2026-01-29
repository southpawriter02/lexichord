using Avalonia.Controls;
using Lexichord.Host.ViewModels;

namespace Lexichord.Host.Views;

/// <summary>
/// Code-behind for the Settings window.
/// </summary>
/// <remarks>
/// LOGIC: Handles window initialization and wires up the close action
/// for the ViewModel to use when closing programmatically.
///
/// Version: v0.1.6a
/// </remarks>
public partial class SettingsWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
    /// </summary>
    public SettingsWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance with a ViewModel.
    /// </summary>
    /// <param name="viewModel">The ViewModel to use.</param>
    public SettingsWindow(SettingsViewModel viewModel) : this()
    {
        DataContext = viewModel;

        // LOGIC: Wire up the close action so the ViewModel can close the window
        viewModel.CloseWindowAction = Close;
    }
}
