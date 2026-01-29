using System;
using System.Threading.Tasks;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host;

/// <summary>
/// Centralized handler for unhandled exceptions.
/// </summary>
/// <remarks>
/// LOGIC: Subscribes to application-wide exception events and routes
/// them to the telemetry service for crash reporting. Also logs locally
/// to ensure crash data is captured even if telemetry fails.
///
/// Handlers:
/// - AppDomain.UnhandledException — Catches exceptions on any thread
/// - TaskScheduler.UnobservedTaskException — Catches unobserved async failures
///
/// Version: v0.1.7d
/// </remarks>
public static class GlobalExceptionHandler
{
    private static ITelemetryService? _telemetry;
    private static ILogger? _logger;

    /// <summary>
    /// Initializes the global exception handler.
    /// </summary>
    /// <param name="services">The service provider for resolving dependencies.</param>
    /// <remarks>
    /// Should be called after the service provider is built but before
    /// the main application loop starts.
    /// </remarks>
    public static void Initialize(IServiceProvider services)
    {
        _telemetry = services.GetService<ITelemetryService>();
        _logger = services.GetService<ILogger<App>>();

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _logger?.LogDebug("GlobalExceptionHandler initialized");
    }

    /// <summary>
    /// Shuts down the global exception handler and flushes telemetry.
    /// </summary>
    public static void Shutdown()
    {
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

        // Flush any pending telemetry
        _telemetry?.Flush(TimeSpan.FromSeconds(2));

        _logger?.LogDebug("GlobalExceptionHandler shutdown complete");
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            HandleException(exception, "AppDomain.UnhandledException", e.IsTerminating);
        }
        else
        {
            _logger?.LogError(
                "Unhandled non-Exception object: {Type}",
                e.ExceptionObject?.GetType().Name ?? "null");
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleException(e.Exception, "TaskScheduler.UnobservedTaskException", isTerminating: false);

        // Mark as observed to prevent process termination
        e.SetObserved();
    }

    private static void HandleException(Exception exception, string source, bool isTerminating)
    {
        // Always log locally first
        _logger?.LogError(
            exception,
            "Unhandled exception from {Source}. Terminating: {IsTerminating}",
            source,
            isTerminating);

        // Capture to telemetry if available
        _telemetry?.CaptureException(exception, new Dictionary<string, string>
        {
            ["source"] = source,
            ["isTerminating"] = isTerminating.ToString()
        });

        // If terminating, flush immediately
        if (isTerminating)
        {
            _telemetry?.Flush(TimeSpan.FromSeconds(2));
        }
    }
}
