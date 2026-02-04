using Avalonia.Controls;
using Avalonia.Interactivity;
using Lexichord.Abstractions.Contracts.Navigation;
using Lexichord.Host.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Views.Shell;

/// <summary>
/// Navigation rail component for the Lexichord shell.
/// </summary>
/// <remarks>
/// LOGIC: Displays icon buttons for switching between application sections.
/// v0.6.4a: Now functional - clicking section buttons navigates to the
/// corresponding section view via ISectionNavigationService.
/// </remarks>
public partial class NavigationRail : UserControl
{
    private ILogger<NavigationRail>? _logger;

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
    /// Handles section button clicks to navigate between application sections.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.6.4a): Determines which section was clicked based on the button's
    /// ToolTip and navigates to that section via ISectionNavigationService.
    /// </remarks>
    private async void OnSectionClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;

        if (sender is not Button button)
            return;

        // LOGIC: Get the section from the button's tooltip
        var tooltip = ToolTip.GetTip(button)?.ToString();
        if (string.IsNullOrEmpty(tooltip))
            return;

        // LOGIC: Map tooltip text to NavigationSection
        var section = tooltip switch
        {
            "Documents" => NavigationSection.Documents,
            "Style Guide" => NavigationSection.StyleGuide,
            "Memory" => NavigationSection.Memory,
            "Agents" => NavigationSection.Agents,
            _ => (NavigationSection?)null
        };

        if (section is null)
            return;

        // LOGIC: Navigate to the section
        var navigationService = App.Services.GetService<ISectionNavigationService>();
        if (navigationService is null)
        {
            _logger ??= App.Services.GetService<ILogger<NavigationRail>>();
            _logger?.LogWarning("ISectionNavigationService not available");
            return;
        }

        await navigationService.NavigateToSectionAsync(section.Value);
    }
}
