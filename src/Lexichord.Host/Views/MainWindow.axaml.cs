using Avalonia.Controls;

namespace Lexichord.Host.Views;

/// <summary>
/// The main application window for Lexichord.
/// </summary>
/// <remarks>
/// LOGIC: This is a minimal stub for v0.0.2a. The window:
/// - Displays centered welcome text
/// - Uses theme resources (TextPrimaryBrush)
/// - Has minimum dimensions enforced
///
/// Future versions will add:
/// - v0.0.2b: Podium Layout (TopBar, NavRail, ContentHost, StatusBar)
/// - v0.0.2c: Theme toggle button
/// - v0.0.2d: Window state persistence
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }
}
