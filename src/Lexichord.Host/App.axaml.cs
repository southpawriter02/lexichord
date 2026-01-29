using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Commands;
using Lexichord.Host.Extensions;
using Lexichord.Host.Services;
using Lexichord.Host.Views;

namespace Lexichord.Host;

/// <summary>
/// The main Avalonia application class with DI and logging integration.
/// </summary>
/// <remarks>
/// LOGIC: The application class owns the DI container lifetime.
/// Services are built during OnFrameworkInitializationCompleted and
/// disposed when the application shuts down.
/// </remarks>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the application-wide service provider.
    /// </summary>
    /// <remarks>
    /// LOGIC: This static accessor enables service resolution in components
    /// that cannot receive services via constructor injection (e.g., XAML-instantiated Views).
    /// Prefer constructor injection where possible.
    /// </remarks>
    public static IServiceProvider Services =>
        ((App)Current!).GetServices();

    private IServiceProvider GetServices() =>
        _serviceProvider ?? throw new InvalidOperationException(
            "Services not initialized. Ensure OnFrameworkInitializationCompleted has been called.");

    /// <inheritdoc/>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: This is called after the framework is fully initialized.
    /// We build the DI container here because:
    /// 1. Avalonia is ready to create windows
    /// 2. We can inject the Application instance if needed
    /// 3. Command-line arguments are available via ApplicationLifetime
    /// </remarks>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // LOGIC: For now, use empty configuration. v0.0.3d will add proper config.
            var configuration = new ConfigurationBuilder().Build();

            // LOGIC: Configure full Serilog pipeline (replaces bootstrap logger)
            SerilogExtensions.ConfigureSerilog(configuration);
            Log.Information("Avalonia framework initialized, building DI container");

            // LOGIC (v0.0.5c): Check for migration commands BEFORE building full DI container
            // This allows migrations to run without initializing the UI
            if (TryProcessMigrationCommand(desktop.Args ?? [], configuration))
            {
                // LOGIC: Migration command processed - exit immediately
                // We use Environment.Exit instead of desktop.Shutdown because the
                // Avalonia dispatcher is not yet fully initialized at this point
                Environment.Exit(0);
                return;
            }

            // Build DI container
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);

            // LOGIC: Register Application instance for services that need it (e.g., ThemeManager)
            services.AddSingleton<Application>(this);

            // Register all Host services (includes Serilog ILogger<T> registration)
            services.ConfigureServices(configuration);

            // LOGIC (v0.0.4b): Discover and load modules BEFORE building the provider
            // Modules register their services during DiscoverAndLoadAsync
            var moduleLoader = CreateModuleLoader();
            moduleLoader.DiscoverAndLoadAsync(services).GetAwaiter().GetResult();

            // Register the module loader itself so modules can access it
            services.AddSingleton<IModuleLoader>(moduleLoader);

            // Build the service provider
            _serviceProvider = services.BuildServiceProvider();
            Log.Debug("DI container built with {ServiceCount} registrations", services.Count);

            // LOGIC (v0.0.4b): Initialize modules AFTER DI container is built
            // This allows modules to resolve services from the container
            moduleLoader.InitializeModulesAsync(_serviceProvider).GetAwaiter().GetResult();

            // Create main window with injected services
            desktop.MainWindow = CreateMainWindow();
            Log.Debug("MainWindow created");

            // Apply persisted settings
            ApplyPersistedSettings();

            // LOGIC (v0.1.1c): Initialize layout from saved profile or use default
            InitializeLayoutAsync().GetAwaiter().GetResult();

            // LOGIC: Register global exception handlers (v0.0.3c)
            RegisterExceptionHandlers();

            // Register shutdown handler to dispose services
            desktop.ShutdownRequested += OnShutdownRequested;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Processes migration commands if present in arguments.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>True if a migration command was processed (app should exit).</returns>
    /// <remarks>
    /// LOGIC (v0.0.5c): Migration commands require:
    /// 1. Minimal DI setup (just database and migration services)
    /// 2. No UI initialization
    /// 3. Exit after completion
    /// </remarks>
    private static bool TryProcessMigrationCommand(string[] args, IConfiguration configuration)
    {
        // Quick check - if no --migrate arg, skip expensive setup
        if (!args.Any(a => a.StartsWith("--migrate", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        Log.Information("Migration command detected, initializing migration services");

        // Build minimal service collection for migrations only
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging(builder => builder.AddSerilog(Log.Logger));

        // Register database and migration services
        Lexichord.Infrastructure.InfrastructureServices.AddDatabaseServices(services, configuration);
        Lexichord.Infrastructure.Migrations.MigrationServices.AddMigrationServices(services);

        using var provider = services.BuildServiceProvider();

        var migrationRunner = provider.GetRequiredService<Lexichord.Infrastructure.Migrations.IMigrationRunner>();
        var logger = provider.GetRequiredService<ILogger<App>>();

        return Commands.MigrationCommand.ProcessMigrationArgs(args, migrationRunner, logger);
    }

    /// <summary>
    /// Creates a ModuleLoader with early-stage logging.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.0.4b): ModuleLoader needs to run before the DI container is built
    /// because modules register their services. We create a temporary logger
    /// using Serilog's static Log.Logger (already configured at this point).
    /// </remarks>
    private static ModuleLoader CreateModuleLoader()
    {
        // LOGIC: Use Serilog's ILogger adapter for early-stage logging
        var loggerFactory = new Serilog.Extensions.Logging.SerilogLoggerFactory(Log.Logger);
        var logger = loggerFactory.CreateLogger<ModuleLoader>();

        // LOGIC (v0.0.4b): Use hardcoded license context (Core tier)
        // v0.0.4c will implement proper license validation
        var licenseContext = new HardcodedLicenseContext();

        return new ModuleLoader(logger, licenseContext);
    }

    /// <summary>
    /// Registers global exception handlers for unhandled exceptions.
    /// </summary>
    /// <remarks>
    /// LOGIC: This method registers handlers for:
    /// 1. AppDomain.UnhandledException - Captures exceptions on any thread
    /// 2. TaskScheduler.UnobservedTaskException - Captures unobserved Task exceptions
    /// </remarks>
    private void RegisterExceptionHandlers()
    {
        var crashService = _serviceProvider!.GetRequiredService<ICrashReportService>();
        var logger = _serviceProvider!.GetRequiredService<ILogger<App>>();

        // LOGIC: AppDomain.UnhandledException captures exceptions on any thread
        // that are not caught by any handler. IsTerminating indicates if the
        // CLR is about to terminate the process.
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var exception = e.ExceptionObject as Exception
                ?? new Exception($"Unknown exception object: {e.ExceptionObject}");

            logger.LogCritical(exception,
                "Unhandled domain exception. IsTerminating: {IsTerminating}",
                e.IsTerminating);

            if (e.IsTerminating)
            {
                // LOGIC: If CLR is terminating, show crash dialog
                // This gives user a chance to copy the report
                crashService.ShowCrashReport(exception);
            }
        };

        // LOGIC: TaskScheduler.UnobservedTaskException captures exceptions from
        // Tasks that were never awaited or had their exceptions checked.
        // We mark them as observed to prevent process termination.
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            logger.LogError(e.Exception, "Unobserved task exception");

            // LOGIC: Mark as observed to prevent CLR from terminating
            // The exception is logged but we don't crash for unobserved tasks
            e.SetObserved();
        };

        logger.LogDebug("Global exception handlers registered");
    }

    private MainWindow CreateMainWindow()
    {
        // LOGIC: Resolve services and inject into MainWindow
        // This demonstrates the transition from direct instantiation to DI
        var themeManager = _serviceProvider!.GetRequiredService<IThemeManager>();
        var windowStateService = _serviceProvider!.GetRequiredService<IWindowStateService>();
        var shellRegionManager = _serviceProvider!.GetRequiredService<IShellRegionManager>();
        var viewModel = _serviceProvider!.GetRequiredService<ViewModels.MainWindowViewModel>();
        var shutdownService = _serviceProvider!.GetRequiredService<IShutdownService>();

        // LOGIC (v0.1.4c): Try to resolve IFileService - it may not be registered if
        // the Editor module is not loaded
        var fileService = _serviceProvider!.GetService<Lexichord.Abstractions.Contracts.Editor.IFileService>();

        // LOGIC (v0.1.5b): Resolve command palette service for keyboard shortcuts
        var commandPaletteService = _serviceProvider!.GetRequiredService<ICommandPaletteService>();

        // LOGIC (v0.1.5d): Resolve keybinding service for shortcut management
        var keyBindingService = _serviceProvider!.GetRequiredService<IKeyBindingService>();

        return new MainWindow
        {
            ThemeManager = themeManager,
            WindowStateService = windowStateService,
            ShellRegionManager = shellRegionManager,
            ViewModel = viewModel,
            ShutdownService = shutdownService,
            FileService = fileService,
            CommandPaletteService = commandPaletteService,
            KeyBindingService = keyBindingService,
            ServiceProvider = _serviceProvider
        };
    }

    private void ApplyPersistedSettings()
    {
        var windowStateService = _serviceProvider!.GetRequiredService<IWindowStateService>();
        var themeManager = _serviceProvider!.GetRequiredService<IThemeManager>();

        var savedState = windowStateService.LoadAsync().GetAwaiter().GetResult();
        if (savedState is not null)
        {
            themeManager.SetTheme(savedState.Theme);
        }
    }

    /// <summary>
    /// Initializes the layout from saved profile or creates default.
    /// </summary>
    /// <remarks>
    /// LOGIC (v0.1.1c): Layout initialization:
    /// 1. Try to load saved layout from ILayoutService
    /// 2. If no saved layout, the default layout is already created by IDockFactory
    /// </remarks>
    private async Task InitializeLayoutAsync()
    {
        var layoutService = _serviceProvider!.GetRequiredService<Lexichord.Abstractions.Layout.ILayoutService>();

        // Try to load the default profile
        var loaded = await layoutService.LoadLayoutAsync();

        if (loaded)
        {
            Log.Debug("Layout restored from profile: {Profile}", layoutService.CurrentProfileName);
        }
        else
        {
            Log.Debug("No saved layout found, using default layout");
        }
    }

    private void OnShutdownRequested(object? sender, Avalonia.Controls.ApplicationLifetimes.ShutdownRequestedEventArgs e)
    {
        Log.Information("Application shutdown requested, disposing services");
        
        // LOGIC: Dispose the service provider to release resources
        // This ensures proper cleanup of singleton services
        _serviceProvider?.Dispose();
    }
}
