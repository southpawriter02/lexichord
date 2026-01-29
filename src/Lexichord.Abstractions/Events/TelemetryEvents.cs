using System;
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when the user changes their telemetry preference.
/// </summary>
/// <remarks>
/// This event allows other components to react to telemetry
/// state changes without direct coupling to the telemetry service.
///
/// Version: v0.1.7d
/// </remarks>
/// <param name="IsEnabled">Whether telemetry is now enabled.</param>
/// <param name="ChangedAt">Timestamp when the change occurred.</param>
public record TelemetryPreferenceChangedEvent(
    bool IsEnabled,
    DateTimeOffset ChangedAt) : INotification;

/// <summary>
/// Published after a crash is captured and sent to Sentry.
/// </summary>
/// <remarks>
/// This provides a lightweight notification about crash capture
/// without exposing sensitive exception details. Useful for:
/// - Logging that a crash was reported
/// - Updating UI to show "crash reported" feedback
/// - Analytics about crash frequency
///
/// Version: v0.1.7d
/// </remarks>
/// <param name="ExceptionType">The type name of the captured exception (no PII).</param>
/// <param name="CapturedAt">Timestamp when the crash was captured.</param>
public record CrashCapturedEvent(
    string ExceptionType,
    DateTimeOffset CapturedAt) : INotification;
