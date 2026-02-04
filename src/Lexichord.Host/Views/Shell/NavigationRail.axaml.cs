using Avalonia.Controls;
using Avalonia.Interactivity;
using Lexichord.Host.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Host.Views.Shell;

/// <summary>
/// Navigation rail component for the Lexichord shell.
/// </summary>
/// <remarks>
/// LOGIC: Displays icon buttons for switching between application sections.
/// Currently implements Settings button; other sections are placeholders.
/// </remarks>
public partial class NavigationRail : UserControl
{
    public NavigationRail()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the Settings button click.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.1.6a): Opens the Settings window.
    /// </remarks>
    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        // Get the parent window
        if (VisualRoot is not Window parentWindow)
            return;

        // Resolve SettingsViewModel from DI
        var settingsViewModel = App.Services.GetService<SettingsViewModel>();
        if (settingsViewModel is null)
            return;

        // Create and show the Settings window
        var settingsWindow = new SettingsWindow
        {
            DataContext = settingsViewModel
        };

        settingsWindow.ShowDialog(parentWindow);
    }

    /// <summary>
    /// Placeholder handler for section buttons.
    /// </summary>
    /// <remarks>
    /// LOGIC: Future versions will navigate to different sections.
    /// For now, these buttons show a visual feedback but take no action.
    /// </remarks>
    private void OnSectionClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement section navigation
        // For now, just consume the event for visual feedback
    }
}
