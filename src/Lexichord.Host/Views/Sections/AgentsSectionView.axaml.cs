using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Lexichord.Host.Controls;
using Lexichord.Host.Views.Dialogs;
using Lexichord.Host.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Host.Views.Sections;

/// <summary>
/// Agents section view - "The Ensemble" - main content area for AI agent interaction.
/// </summary>
/// <remarks>
/// SPEC: SCREEN_WIREFRAMES.md Section 2.3 - The Ensemble
///
/// UPDATED: Now includes proper agent cards with descriptions, license indicators,
/// and an integrated chat panel for active sessions.
///
/// LOGIC: Provides agent selection and chat interface from the AgentsModule.
/// Links to LLM configuration and template preview.
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

        var settingsViewModel = App.Services.GetService<SettingsViewModel>();
        if (settingsViewModel is null)
            return;

        var settingsWindow = new SettingsWindow
        {
            DataContext = settingsViewModel
        };

        settingsWindow.ShowDialog(parentWindow);
    }

    private async void OnPreviewTemplatesClick(object? sender, RoutedEventArgs e)
    {
        // Open Template Preview Dialog (v0.6.3d specification)
        if (VisualRoot is not Window parentWindow)
            return;

        var dialog = new TemplatePreviewDialog
        {
            DataContext = new TemplatePreviewViewModel()
        };

        await dialog.ShowDialog(parentWindow);
    }

    private void OnEditorAgentClick(object? sender, PointerPressedEventArgs e)
    {
        // Switch chat panel to The Editor agent
        if (this.FindControl<AgentChatPanel>("ChatPanel") is { } chatPanel)
        {
            chatPanel.AgentName = "The Editor";
            chatPanel.AgentIcon = "🎭";
        }
    }

    private void OnSimplifierAgentClick(object? sender, PointerPressedEventArgs e)
    {
        // Switch chat panel to The Simplifier agent
        if (this.FindControl<AgentChatPanel>("ChatPanel") is { } chatPanel)
        {
            chatPanel.AgentName = "The Simplifier";
            chatPanel.AgentIcon = "✨";
        }
    }

    private void OnChroniclerAgentClick(object? sender, PointerPressedEventArgs e)
    {
        // The Chronicler is a Pro feature - show upgrade prompt
        // TODO: Implement license check and upgrade prompt
    }

    private void OnScribeAgentClick(object? sender, PointerPressedEventArgs e)
    {
        // The Scribe is a Pro feature - show upgrade prompt
        // TODO: Implement license check and upgrade prompt
    }
}
