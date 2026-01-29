using System;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Configuration settings for telemetry and crash reporting.
/// </summary>
/// <remarks>
/// PRIVACY DEFAULTS:
/// - CrashReportingEnabled defaults to FALSE (opt-in)
/// - UsageAnalyticsEnabled defaults to FALSE (opt-in)
/// - No data is ever sent without explicit user consent
///
/// This record is persisted to disk as JSON at:
/// - Windows: %APPDATA%/Lexichord/telemetry-settings.json
/// - macOS: ~/Library/Application Support/Lexichord/telemetry-settings.json
/// - Linux: ~/.config/Lexichord/telemetry-settings.json
///
/// Version: v0.1.7d
/// </remarks>
public record TelemetrySettings
{
    /// <summary>
    /// Gets whether crash reporting is enabled.
    /// </summary>
    /// <remarks>
    /// Defaults to FALSE (opt-in). When true, unhandled
    /// exceptions are sent to Sentry with PII scrubbed.
    /// </remarks>
    public bool CrashReportingEnabled { get; init; } = false;

    /// <summary>
    /// Gets whether usage analytics are enabled.
    /// </summary>
    /// <remarks>
    /// Reserved for future use. Defaults to FALSE.
    /// Currently no usage analytics are collected.
    /// </remarks>
    public bool UsageAnalyticsEnabled { get; init; } = false;

    /// <summary>
    /// Gets the date when the user gave consent.
    /// </summary>
    /// <remarks>
    /// Null if no consent has been given.
    /// Used for audit trail and compliance.
    /// </remarks>
    public DateTimeOffset? ConsentDate { get; init; }

    /// <summary>
    /// Gets the anonymous installation identifier.
    /// </summary>
    /// <remarks>
    /// Generated on first run, used to correlate crash
    /// reports from the same installation without PII.
    /// </remarks>
    public string? InstallationId { get; init; }

    /// <summary>
    /// Gets whether the consent prompt has been shown.
    /// </summary>
    /// <remarks>
    /// Used to track whether we've asked the user about
    /// telemetry preferences. Defaults to FALSE.
    /// </remarks>
    public bool ConsentPromptShown { get; init; } = false;
}
