using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Controls;
using Lexichord.Abstractions.Layout;
using Lexichord.Host.ViewModels.CommandPalette;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// </summary>
/// <remarks>
/// LOGIC: Manages the dock layout for the main window.
/// Coordinates initialization of the layout using the IDockFactory
/// and exposes the root dock for binding to the DockControl.
/// </remarks>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IDockFactory _dockFactory;
    private readonly ILogger<MainWindowViewModel> _logger;

    /// <summary>
    /// Gets or sets the root dock layout.
    /// </summary>
    /// <remarks>
    /// LOGIC: Bound to the DockControl.Layout property in MainWindow.axaml.
    /// Set during InitializeLayout() call.
    /// </remarks>
    [ObservableProperty]
    private IRootDock? _layout;

    /// <summary>
    /// Gets the Command Palette ViewModel.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.1.5b): Exposed for binding in MainWindow.axaml.
    /// </remarks>
    public CommandPaletteViewModel CommandPaletteViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="dockFactory">Factory for creating dock layouts.</param>
    /// <param name="commandPaletteViewModel">ViewModel for the command palette.</param>
    /// <param name="logger">Logger for recording operations.</param>
    public MainWindowViewModel(
        IDockFactory dockFactory,
        CommandPaletteViewModel commandPaletteViewModel,
        ILogger<MainWindowViewModel> logger)
    {
        _dockFactory = dockFactory;
        CommandPaletteViewModel = commandPaletteViewModel;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the dock layout.
    /// </summary>
    /// <remarks>
    /// LOGIC: Called when the MainWindow is ready to display the dock layout.
    /// Creates the default layout using the factory and sets it for binding.
    /// </remarks>
    public void InitializeLayout()
    {
        _logger.LogInformation("Initializing dock layout");

        Layout = _dockFactory.CreateDefaultLayout();

        _logger.LogDebug("Dock layout initialized with root: {RootId}", Layout?.Id);
    }
}
