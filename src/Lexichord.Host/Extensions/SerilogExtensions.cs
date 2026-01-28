using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.IO;
using System.Reflection;

namespace Lexichord.Host.Extensions;

/// <summary>
/// Extension methods for configuring Serilog logging.
/// </summary>
/// <remarks>
/// LOGIC: This class centralizes all Serilog configuration.
/// The ConfigureSerilog method replaces the bootstrap logger with the full pipeline.
/// </remarks>
public static class SerilogExtensions
{
    /// <summary>
    /// Configures the full Serilog pipeline from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <remarks>
    /// LOGIC: This method replaces the bootstrap logger with the full configuration.
    /// It sets up multiple sinks with appropriate output templates and rolling policies:
    /// - Console: Colorized output for development
    /// - File: Daily rolling logs with 30-day retention
    /// - Error File: Error-only logs with 90-day retention
    /// </remarks>
    public static void ConfigureSerilog(IConfiguration configuration)
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logPath = Path.Combine(appDataPath, "Lexichord", "Logs");
        Directory.CreateDirectory(logPath);

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        var debugModeValue = configuration.GetSection("Lexichord:DebugMode").Value;
        var debugMode = bool.TryParse(debugModeValue, out var result) && result;
        var minimumLevel = debugMode ? LogEventLevel.Debug : LogEventLevel.Information;

        Log.Logger = new LoggerConfiguration()
            // Minimum Levels
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Avalonia", LogEventLevel.Warning)

            // Enrichers - Add context to every log entry
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "Lexichord")
            .Enrich.WithProperty("Version", version)

            // Console Sink (Development)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <{SourceContext}>{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code,
                restrictedToMinimumLevel: debugMode ? LogEventLevel.Debug : LogEventLevel.Information)

            // File Sink (Rolling daily)
            .WriteTo.File(
                path: Path.Combine(logPath, "lexichord-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 10_000_000, // 10 MB
                rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))

            // Error-only File Sink (Longer retention)
            .WriteTo.File(
                path: Path.Combine(logPath, "lexichord-errors-.log"),
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Properties:j}{NewLine}{Exception}")

            .CreateLogger();

        Log.Information("Serilog configured. Log path: {LogPath}, Debug mode: {DebugMode}",
            logPath, debugMode);
    }
}
