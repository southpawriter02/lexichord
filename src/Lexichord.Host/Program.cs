using Avalonia;
using Serilog;
using Serilog.Events;
using System;

namespace Lexichord.Host;

/// <summary>
/// Application entry point with bootstrap logging and exception handling.
/// </summary>
/// <remarks>
/// LOGIC: The entry point establishes a bootstrap logger before Avalonia initializes.
/// This captures any startup failures that occur before the full logging pipeline is ready.
/// The bootstrap logger is minimal (console only) and is replaced once configuration loads.
/// </remarks>
internal sealed class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code (0 = success, 1 = error).</returns>
    /// <remarks>
    /// LOGIC: STAThread is required on Windows for proper COM apartment threading.
    /// This enables clipboard operations, file dialogs, and drag-drop functionality.
    /// On other platforms, this attribute is ignored.
    /// </remarks>
    [STAThread]
    public static int Main(string[] args)
    {
        // LOGIC: Create bootstrap logger immediately for startup error capture
        // This minimal logger writes to console only until full configuration loads
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting Lexichord application");
            Log.Debug("Command line arguments: {Args}", args);

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            Log.Information("Lexichord application shutdown complete");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Lexichord application terminated unexpectedly");
            return 1;
        }
        finally
        {
            // LOGIC: Ensure all log entries are written before process exits
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Builds and configures the Avalonia application.
    /// </summary>
    /// <returns>A configured <see cref="AppBuilder"/> instance.</returns>
    /// <remarks>
    /// LOGIC: This method chain configures:
    /// - Configure&lt;App&gt;: Specifies our Application class
    /// - UsePlatformDetect: Auto-detects Windows/macOS/Linux
    /// - WithInterFont: Includes Inter font family
    /// - LogToTrace: Enables trace-level logging (Debug builds)
    ///
    /// The method is public and static so the XAML previewer can invoke it.
    /// </remarks>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
