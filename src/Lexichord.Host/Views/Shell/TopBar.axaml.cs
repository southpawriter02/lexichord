using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Lexichord.Host.ViewModels;

namespace Lexichord.Host.Views.Shell;

/// <summary>
/// Top bar component for the Lexichord shell.
/// </summary>
/// <remarks>
/// LOGIC: This component displays:
/// - Application branding (logo, title, tagline)
/// - File menu with Open Recent submenu (v0.1.4d)
/// </remarks>
public partial class TopBar : UserControl
{
    public TopBar()
    {
        InitializeComponent();
        
        // LOGIC (v0.1.4d): Resolve the menu ViewModel from DI and set as DataContext
        // This enables the File > Open Recent menu to bind to recent files
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // LOGIC: Resolve from the static App.Services after the control is loaded
        // when the DI container is guaranteed to be available
        DataContext = App.Services.GetService<OpenRecentMenuViewModel>();
    }

    /// <summary>
    /// Handles the Exit menu item click.
    /// </summary>
    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        // LOGIC: Use the parent Window to close the application
        if (VisualRoot is Window window)
        {
            window.Close();
        }
    }
}
