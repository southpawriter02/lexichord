using Avalonia.Controls;
using Avalonia.Interactivity;
using Lexichord.Abstractions.Layout;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Host.Views.Sections;

/// <summary>
/// Style Guide section view - main content area for style and terminology management.
/// </summary>
/// <remarks>
/// LOGIC: Provides overview and navigation to style-related tools from the
/// StyleModule. The actual functionality is provided by LexiconView and
/// ProblemsPanelView which are registered in the Right dock region.
/// </remarks>
public partial class StyleGuideSectionView : UserControl
{
    public StyleGuideSectionView()
    {
        InitializeComponent();
    }

    private async void OnOpenLexiconClick(object? sender, RoutedEventArgs e)
    {
        // LOGIC: Navigate to the Lexicon tool in the Right dock region
        var regionManager = App.Services.GetService<IRegionManager>();
        if (regionManager is not null)
        {
            await regionManager.NavigateToAsync("lexichord.lexicon");
        }
    }

    private async void OnOpenProblemsClick(object? sender, RoutedEventArgs e)
    {
        // LOGIC: Navigate to the Problems panel in the Right dock region
        var regionManager = App.Services.GetService<IRegionManager>();
        if (regionManager is not null)
        {
            await regionManager.NavigateToAsync("lexichord.problems");
        }
    }
}
