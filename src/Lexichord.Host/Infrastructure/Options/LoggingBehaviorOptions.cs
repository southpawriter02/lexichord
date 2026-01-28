namespace Lexichord.Host.Infrastructure.Options;

/// <summary>
/// Configuration options for the LoggingBehavior pipeline behavior.
/// </summary>
/// <remarks>
/// LOGIC: These options are bound from configuration (appsettings.json)
/// and can be adjusted per environment (e.g., lower threshold in production).
/// </remarks>
public sealed class LoggingBehaviorOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "MediatR:Logging";

    /// <summary>
    /// Threshold in milliseconds for slow request warnings.
    /// </summary>
    /// <remarks>
    /// LOGIC: Requests exceeding this duration will generate a warning log.
    /// Default: 500ms. Consider lowering in production for tighter monitoring.
    /// </remarks>
    public int SlowRequestThresholdMs { get; set; } = 500;

    /// <summary>
    /// Whether to log request properties at start.
    /// </summary>
    /// <remarks>
    /// LOGIC: Enable in development for debugging, disable in production
    /// for performance and to reduce log volume.
    /// </remarks>
    public bool LogRequestProperties { get; set; } = true;

    /// <summary>
    /// Whether to log response properties on completion.
    /// </summary>
    /// <remarks>
    /// LOGIC: Can generate large logs for queries returning collections.
    /// Consider disabling in production.
    /// </remarks>
    public bool LogResponseProperties { get; set; } = false;

    /// <summary>
    /// Whether to include the full exception in failure logs.
    /// </summary>
    /// <remarks>
    /// LOGIC: Enable in development; in production, may want to log
    /// only exception message to avoid leaking stack traces.
    /// </remarks>
    public bool LogFullExceptions { get; set; } = true;

    /// <summary>
    /// Request types to exclude from logging (by full type name).
    /// </summary>
    /// <remarks>
    /// LOGIC: Some high-frequency or noise-generating requests might
    /// need to be excluded from logging.
    /// Example: ["HealthCheckQuery", "MetricsQuery"]
    /// </remarks>
    public List<string> ExcludedRequestTypes { get; set; } = new();
}
