using Serilog;
using Serilog.Configuration;
using Sentry.Serilog;

namespace Lexichord.Host.Configuration;

/// <summary>
/// Extension methods for configuring Serilog with Sentry integration.
/// </summary>
/// <remarks>
/// Provides privacy-focused Sentry sink configuration with:
/// - Minimum event level filtering
/// - Breadcrumb collection for context
/// - No PII transmission (enforced via SendDefaultPii = false)
///
/// Version: v0.1.7d
/// </remarks>
public static class SerilogSentryExtensions
{
    /// <summary>
    /// Adds Sentry as a Serilog sink with privacy-focused defaults.
    /// </summary>
    /// <param name="configuration">The logger sink configuration.</param>
    /// <param name="dsn">The Sentry DSN.</param>
    /// <param name="minimumEventLevel">Minimum level for events sent to Sentry.</param>
    /// <param name="minimumBreadcrumbLevel">Minimum level for breadcrumbs.</param>
    /// <returns>Logger configuration for chaining.</returns>
    /// <remarks>
    /// PRIVACY: SendDefaultPii is always false regardless of parameters.
    /// </remarks>
    public static LoggerConfiguration Sentry(
        this LoggerSinkConfiguration configuration,
        string dsn,
        Serilog.Events.LogEventLevel minimumEventLevel = Serilog.Events.LogEventLevel.Error,
        Serilog.Events.LogEventLevel minimumBreadcrumbLevel = Serilog.Events.LogEventLevel.Information)
    {
        return configuration.Sentry(options =>
        {
            options.Dsn = dsn;
            options.MinimumEventLevel = minimumEventLevel;
            options.MinimumBreadcrumbLevel = minimumBreadcrumbLevel;

            // CRITICAL: Never send PII
            options.InitializeSdk = false; // We initialize in TelemetryService
        });
    }

    /// <summary>
    /// Conditionally adds Sentry sink if DSN is configured.
    /// </summary>
    /// <param name="configuration">The logger configuration.</param>
    /// <param name="dsn">The Sentry DSN (may be null or empty).</param>
    /// <param name="isEnabled">Whether telemetry is enabled.</param>
    /// <returns>Logger configuration for chaining.</returns>
    public static LoggerConfiguration WriteTo_SentryIfEnabled(
        this LoggerConfiguration configuration,
        string? dsn,
        bool isEnabled)
    {
        if (!isEnabled || string.IsNullOrEmpty(dsn))
            return configuration;

        return configuration.WriteTo.Sentry(dsn);
    }
}
