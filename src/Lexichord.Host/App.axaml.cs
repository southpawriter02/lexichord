using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Extensions;
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

            // Build DI container
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);

            // LOGIC: Register Application instance for services that need it (e.g., ThemeManager)
            services.AddSingleton<Application>(this);

            // Register all Host services (includes Serilog ILogger<T> registration)
            services.ConfigureServices(configuration);

            // Build the service provider
            _serviceProvider = services.BuildServiceProvider();
            Log.Debug("DI container built with {ServiceCount} registrations", services.Count);

            // Create main window with injected services
            desktop.MainWindow = CreateMainWindow();
            Log.Debug("MainWindow created");

            // Apply persisted settings
            ApplyPersistedSettings();

            // LOGIC: Register global exception handlers (v0.0.3c)
            RegisterExceptionHandlers();

            // Register shutdown handler to dispose services
            desktop.ShutdownRequested += OnShutdownRequested;
        }

        base.OnFrameworkInitializationCompleted();
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

        return new MainWindow
        {
            ThemeManager = themeManager,
            WindowStateService = windowStateService
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

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        Log.Information("Application shutdown requested, disposing services");
        
        // LOGIC: Dispose the service provider to release resources
        // This ensures proper cleanup of singleton services
        _serviceProvider?.Dispose();
    }
}
