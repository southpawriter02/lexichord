using System;
using System.Collections.Generic;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Defines severity levels for telemetry messages.
/// </summary>
/// <remarks>
/// Maps to Sentry severity levels for consistent categorization.
/// </remarks>
public enum TelemetryLevel
{
    /// <summary>Debug-level messages for detailed diagnostics.</summary>
    Debug,

    /// <summary>Informational messages about normal operations.</summary>
    Info,

    /// <summary>Warning messages about potential issues.</summary>
    Warning,

    /// <summary>Error messages about failures that don't crash the app.</summary>
    Error,

    /// <summary>Fatal messages about unrecoverable errors.</summary>
    Fatal
}

/// <summary>
/// Service for optional crash reporting and telemetry.
/// </summary>
/// <remarks>
/// PRIVACY REQUIREMENTS:
/// 1. Opt-in by default — Never enabled without user consent
/// 2. No PII collection — Email, paths, content never transmitted
/// 3. Immediate effect — Toggle changes apply instantly
/// 4. Clear disclosure — Users know exactly what is collected
/// 5. Minimal data — Only crash data, not usage patterns
///
/// All methods are safe to call regardless of enabled state.
/// When disabled, methods are no-ops with minimal overhead.
///
/// Version: v0.1.7d
/// </remarks>
public interface ITelemetryService : IDisposable
{
    /// <summary>
    /// Gets whether telemetry is currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enables crash reporting with user consent.
    /// </summary>
    /// <remarks>
    /// Initializes the Sentry SDK if not already initialized.
    /// Persists the preference for future sessions.
    /// </remarks>
    void Enable();

    /// <summary>
    /// Disables crash reporting.
    /// </summary>
    /// <remarks>
    /// Flushes any pending events before disabling.
    /// Persists the preference for future sessions.
    /// </remarks>
    void Disable();

    /// <summary>
    /// Captures an exception for crash reporting.
    /// </summary>
    /// <param name="exception">The exception to capture.</param>
    /// <param name="tags">Optional key-value tags for categorization.</param>
    /// <remarks>
    /// All PII is scrubbed from the exception before transmission:
    /// - User paths replaced with [USER_PATH]
    /// - Email addresses replaced with [EMAIL]
    /// </remarks>
    void CaptureException(Exception exception, IDictionary<string, string>? tags = null);

    /// <summary>
    /// Captures a message for telemetry.
    /// </summary>
    /// <param name="message">The message to capture.</param>
    /// <param name="level">The severity level.</param>
    void CaptureMessage(string message, TelemetryLevel level = TelemetryLevel.Info);

    /// <summary>
    /// Adds a breadcrumb for contextual tracking.
    /// </summary>
    /// <param name="message">The breadcrumb message.</param>
    /// <param name="category">Optional category for grouping.</param>
    /// <remarks>
    /// Breadcrumbs are included with crash reports to provide
    /// context about user actions leading up to the crash.
    /// Limited to last 100 entries.
    /// </remarks>
    void AddBreadcrumb(string message, string? category = null);

    /// <summary>
    /// Begins a scoped operation for telemetry tracking.
    /// </summary>
    /// <param name="operation">Name of the operation.</param>
    /// <returns>A disposable scope that ends the operation when disposed.</returns>
    IDisposable BeginScope(string operation);

    /// <summary>
    /// Sets anonymous user identifier for session correlation.
    /// </summary>
    /// <param name="userId">Anonymous user/installation identifier.</param>
    /// <remarks>
    /// This should be the InstallationId from TelemetrySettings,
    /// NOT any personally identifiable information.
    /// </remarks>
    void SetUser(string userId);

    /// <summary>
    /// Flushes pending events to the telemetry service.
    /// </summary>
    /// <param name="timeout">Maximum time to wait for flush.</param>
    /// <remarks>
    /// Should be called during application shutdown to ensure
    /// all pending crash reports are transmitted.
    /// </remarks>
    void Flush(TimeSpan timeout);
}
