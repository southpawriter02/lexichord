using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Commands;
using Lexichord.Host.Views.Shell;

namespace Lexichord.Host.Views;

/// <summary>
/// The main application window for Lexichord.
/// </summary>
/// <remarks>
/// LOGIC: The MainWindow hosts the Podium Layout shell components
/// and manages window state persistence (position, size, maximized state).
/// </remarks>
public partial class MainWindow : Window
{
    private IWindowStateService? _windowStateService;
    private IThemeManager? _themeManager;
    private IShellRegionManager? _shellRegionManager;
    private IShutdownService? _shutdownService;
    private Abstractions.Contracts.Editor.IFileService? _fileService;
    private ICommandPaletteService? _commandPaletteService;
    private IKeyBindingService? _keyBindingService;
    private IServiceProvider? _serviceProvider;
    private bool _closeConfirmed;

    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // LOGIC: Subscribe to Closing event to save state
        Closing += OnWindowClosing;

        // LOGIC (v0.1.5b): Subscribe to KeyDown for palette shortcut
        KeyDown += OnMainWindowKeyDown;
    }

    /// <summary>
    /// Gets the StatusBar component for service initialization.
    /// </summary>
    public StatusBar StatusBar => MainStatusBar;

    /// <summary>
    /// Gets or sets the theme manager.
    /// </summary>
    public IThemeManager? ThemeManager
    {
        get => _themeManager;
        set => _themeManager = value;
    }

    /// <summary>
    /// Gets or sets the window state service.
    /// </summary>
    public IWindowStateService? WindowStateService
    {
        get => _windowStateService;
        set
        {
            _windowStateService = value;
            if (value is not null)
            {
                _ = RestoreWindowStateAsync();
            }
        }
    }

    /// <summary>
    /// Gets or sets the shell region manager.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.0.8a): When set, initializes shell regions and populates
    /// containers with module-contributed views.
    /// </remarks>
    public IShellRegionManager? ShellRegionManager
    {
        get => _shellRegionManager;
        set
        {
            _shellRegionManager = value;
            if (value is not null)
            {
                InitializeShellRegions();
            }
        }
    }

    /// <summary>
    /// Gets or sets the main window ViewModel.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.1.1a): When set, initializes the dock layout and sets as DataContext.
    /// </remarks>
    public ViewModels.MainWindowViewModel? ViewModel
    {
        get => DataContext as ViewModels.MainWindowViewModel;
        set
        {
            DataContext = value;
            if (value is not null)
            {
                value.InitializeLayout();
            }
        }
    }

    /// <summary>
    /// Gets or sets the shutdown service.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.1.4c): When set, enables dirty document checking on close.
    /// </remarks>
    public IShutdownService? ShutdownService
    {
        get => _shutdownService;
        set => _shutdownService = value;
    }

    /// <summary>
    /// Gets or sets the file service.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.1.4c): Used by SaveChangesDialog to save dirty documents.
    /// </remarks>
    public Abstractions.Contracts.Editor.IFileService? FileService
    {
        get => _fileService;
        set => _fileService = value;
    }

    /// <summary>
    /// Gets or sets the command palette service.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.1.5b): Enables Ctrl+Shift+P shortcut for command palette.
    /// </remarks>
    public ICommandPaletteService? CommandPaletteService
    {
        get => _commandPaletteService;
        set => _commandPaletteService = value;
    }

    /// <summary>
    /// Gets or sets the keybinding service.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.1.5d): Routes key events to registered command shortcuts.
    /// </remarks>
    public IKeyBindingService? KeyBindingService
    {
        get => _keyBindingService;
        set => _keyBindingService = value;
    }

    /// <summary>
    /// Gets or sets the service provider for resolving services.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.1.6a): Used to create SettingsViewModel instances for the Settings window.
    /// </remarks>
    public IServiceProvider? ServiceProvider
    {
        get => _serviceProvider;
        set => _serviceProvider = value;
    }

    /// <summary>
    /// Initializes shell regions with module-contributed views.
    /// </summary>
    /// <remarks>
    /// LOGIC: For v0.0.8a, this only handles the Bottom region.
    /// The existing shell:StatusBar in XAML provides the base status bar,
    /// and module views can supplement or replace it.
    /// </remarks>
    private void InitializeShellRegions()
    {
        if (_shellRegionManager is null)
            return;

        // Get views registered for the Bottom region
        var bottomViews = _shellRegionManager.GetViews(ShellRegion.Bottom);

        // TODO (v0.0.8a): For now, we log that views are available.
        // Future: Replace the hardcoded StatusBar with dynamic ContentControl
        // that hosts module-contributed views.
        System.Diagnostics.Debug.WriteLine(
            $"[ShellRegions] Found {bottomViews.Count} views for Bottom region");
    }

    /// <summary>
    /// Restores window state from persisted storage.
    /// </summary>
    private async Task RestoreWindowStateAsync()
    {
        if (_windowStateService is null)
            return;

        var state = await _windowStateService.LoadAsync();

        if (state is null)
        {
            // LOGIC: First launch or corrupted file—use defaults
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        // LOGIC: Validate position before applying
        if (_windowStateService.IsPositionValid(state))
        {
            Position = new PixelPoint((int)state.X, (int)state.Y);
            Width = Math.Max(state.Width, MinWidth);
            Height = Math.Max(state.Height, MinHeight);
            WindowStartupLocation = WindowStartupLocation.Manual;
        }
        else
        {
            // LOGIC: Saved position is off-screen, center instead
            Width = Math.Max(state.Width, MinWidth);
            Height = Math.Max(state.Height, MinHeight);
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        // LOGIC: Restore maximized state after size/position
        if (state.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }

        // LOGIC: Restore theme preference
        _themeManager?.SetTheme(state.Theme);
    }

    /// <summary>
    /// Saves window state when the window is closing.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.1.4c): Checks for dirty documents before allowing close.
    /// Shows SaveChangesDialog when dirty documents exist.
    /// </remarks>
    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // LOGIC (v0.1.4c): Check for dirty documents unless close was already confirmed
        if (!_closeConfirmed && _shutdownService?.HasDirtyDocuments == true && _fileService is not null)
        {
            e.Cancel = true;

            var dirtyDocs = _shutdownService.GetDirtyDocuments();
            var viewModel = new ViewModels.SaveChangesDialogViewModel(_fileService);
            var dialog = new SaveChangesDialog(viewModel);

            // Start showing the dialog
            var resultTask = viewModel.ShowAsync(dirtyDocs);

            // Show dialog modally
            await dialog.ShowDialog(this);

            // Get result (dialog should have completed the task)
            if (resultTask.IsCompleted)
            {
                var result = resultTask.Result;

                switch (result.Action)
                {
                    case ViewModels.SaveChangesAction.SaveAll:
                        if (result.AllSucceeded)
                        {
                            _closeConfirmed = true;
                            Close();
                        }
                        // If partial failure, stay open
                        break;

                    case ViewModels.SaveChangesAction.DiscardAll:
                        _closeConfirmed = true;
                        Close();
                        break;

                    case ViewModels.SaveChangesAction.Cancel:
                        // Keep window open
                        break;
                }
            }

            return;
        }

        // Save window state
        if (_windowStateService is not null)
        {
            // LOGIC: Capture current state
            // Store current position and size (before maximizing, we don't have RestoreBounds in Avalonia 11)
            // When maximized, Width/Height reflect maximized size, but Position is still original
            var isMaximized = WindowState == WindowState.Maximized;

            // LOGIC: For maximized windows, we save the screen-appropriate defaults
            // since Avalonia 11 doesn't expose RestoreBounds. This means maximized
            // windows will restore to their last known pre-maximized size on next launch.
            var state = new WindowStateRecord(
                X: Position.X,
                Y: Position.Y,
                Width: isMaximized ? 1400 : Width,  // Default size for maximized
                Height: isMaximized ? 900 : Height, // Default size for maximized
                IsMaximized: isMaximized,
                Theme: _themeManager?.CurrentTheme ?? ThemeMode.System
            );

            await _windowStateService.SaveAsync(state);
        }
    }

    /// <summary>
    /// Handles keyboard input for global shortcuts.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.1.5d): Routes key events through IKeyBindingService for command execution.
    /// Falls back to hardcoded shortcuts for palette commands if service unavailable.
    /// </remarks>
    private async void OnMainWindowKeyDown(object? sender, KeyEventArgs e)
    {
        // LOGIC (v0.1.5d): Use keybinding service for global shortcut routing
        if (_keyBindingService is Services.KeyBindingManager keyBindingManager)
        {
            // Get current context (could be enhanced to detect focused component)
            var context = GetCurrentContext();
            if (keyBindingManager.TryHandleKeyEvent(e, context))
            {
                return; // Event was handled by a registered command
            }
        }

        // LOGIC: Fallback for palette commands (registered commands may not exist yet)
        // Ctrl+Shift+P opens Command Palette
        if (e.Key == Key.P &&
            e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.KeyModifiers.HasFlag(KeyModifiers.Shift) &&
            !e.Handled)
        {
            if (_commandPaletteService is not null)
            {
                await _commandPaletteService.ToggleAsync(PaletteMode.Commands);
                e.Handled = true;
            }
        }

        // LOGIC: Ctrl+P opens File Palette
        if (e.Key == Key.P &&
            e.KeyModifiers == KeyModifiers.Control &&
            !e.Handled)
        {
            if (_commandPaletteService is not null)
            {
                await _commandPaletteService.ToggleAsync(PaletteMode.Files);
                e.Handled = true;
            }
        }

        // LOGIC (v0.1.6a): Ctrl+, opens Settings window (Cmd+, on macOS)
        if (e.Key == Key.OemComma &&
            e.KeyModifiers == KeyModifiers.Control &&
            !e.Handled)
        {
            await OpenSettingsWindowAsync();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Opens the Settings window.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.1.6a): Creates a new SettingsViewModel, initializes it, and shows the window.
    /// </remarks>
    private async Task OpenSettingsWindowAsync(Abstractions.Contracts.SettingsWindowOptions? options = null)
    {
        if (_serviceProvider is null)
        {
            return;
        }

        // Resolve ViewModel from DI
        var viewModel = _serviceProvider.GetService(typeof(ViewModels.SettingsViewModel))
            as ViewModels.SettingsViewModel;

        if (viewModel is null)
        {
            return;
        }

        // Initialize with options
        viewModel.Initialize(options);

        // Create and show the window
        var settingsWindow = new SettingsWindow(viewModel);
        await settingsWindow.ShowDialog(this);
    }

    /// <summary>
    /// Determines the current UI context for context-aware keybindings.
    /// </summary>
    /// <returns>Context string like "editorFocus" or null for global.</returns>
    /// <remarks>
    /// LOGIC (v0.1.5d): Returns a context identifier based on the currently focused element.
    /// This enables context-aware keyboard shortcuts.
    /// </remarks>
    private static string? GetCurrentContext()
    {
        // TODO (v0.1.5d): Implement focus-based context detection
        // For now, return null (global context)
        return null;
    }
}
