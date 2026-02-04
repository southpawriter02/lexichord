using Avalonia.Controls;
using Avalonia.Interactivity;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Layout;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Host.Views.Sections;

/// <summary>
/// Memory section view - main content area for RAG and semantic search.
/// </summary>
/// <remarks>
/// LOGIC: Provides overview and navigation to RAG-related tools from the
/// RAGModule. Includes inline search functionality and links to index
/// management in settings.
/// </remarks>
public partial class MemorySectionView : UserControl
{
    public MemorySectionView()
    {
        InitializeComponent();
    }

    private void OnSearchClick(object? sender, RoutedEventArgs e)
    {
        // LOGIC: Perform semantic search with the query
        var searchBox = this.FindControl<TextBox>("SearchBox");
        var query = searchBox?.Text;

        if (string.IsNullOrWhiteSpace(query))
            return;

        // TODO: Integrate with ISemanticSearchService or open Reference panel with query
        // For now, this serves as a placeholder for the search functionality
    }

    private void OnIndexStatusClick(object? sender, RoutedEventArgs e)
    {
        // LOGIC: Navigate to settings with Index Status page selected
        // The Index Status is registered as a settings page by RAGModule
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
