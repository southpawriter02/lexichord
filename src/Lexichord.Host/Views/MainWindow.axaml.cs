using Avalonia.Controls;
using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Views.Shell;

namespace Lexichord.Host.Views;

/// <summary>
/// The main application window for Lexichord.
/// </summary>
/// <remarks>
/// LOGIC: The MainWindow hosts the Podium Layout shell components.
/// It provides access to the StatusBar for theme manager initialization.
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

    /// <summary>
    /// Gets the StatusBar component for service initialization.
    /// </summary>
    public StatusBar StatusBar => MainStatusBar;
}
