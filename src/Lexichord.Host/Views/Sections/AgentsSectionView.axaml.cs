using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Host.Views.Sections;

/// <summary>
/// Agents section view - main content area for AI agent configuration and interaction.
/// </summary>
/// <remarks>
/// LOGIC: Provides overview and navigation to agent-related tools from the
/// AgentsModule and LLMModule. Links to LLM configuration in settings.
/// </remarks>
public partial class AgentsSectionView : UserControl
{
    public AgentsSectionView()
    {
        InitializeComponent();
    }

    private void OnLLMSettingsClick(object? sender, RoutedEventArgs e)
    {
        // LOGIC: Navigate to settings with LLM page selected
        if (VisualRoot is not Window parentWindow)
            return;

        var settingsViewModel = App.Services.GetService<ViewModels.SettingsViewModel>();
        if (settingsViewModel is null)
            return;

        var settingsWindow = new SettingsWindow
        {
            DataContext = settingsViewModel
        };

        // TODO: Add ability to navigate to specific settings page
        settingsWindow.ShowDialog(parentWindow);
    }
}
