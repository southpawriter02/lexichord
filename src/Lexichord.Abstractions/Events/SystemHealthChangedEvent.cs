using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Event raised when system health status changes.
/// </summary>
/// <remarks>
/// LOGIC: This event is published by the StatusBar module when:
/// - Database connection state changes
/// - Heartbeat fails/recovers
/// - Vault status changes
///
/// Subscribers can use this for:
/// - Logging health metrics
/// - Alerting on degraded state
/// - Triggering recovery actions
/// </remarks>
/// <param name="Status">The current health status.</param>
/// <param name="Message">Human-readable status message.</param>
/// <param name="Timestamp">When the status change occurred.</param>
public record SystemHealthChangedEvent(
    HealthStatus Status,
    string Message,
    DateTime Timestamp) : INotification;

/// <summary>
/// Health status enumeration.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// All systems operating normally.
    /// </summary>
    Healthy,

    /// <summary>
    /// Some non-critical systems degraded.
    /// </summary>
    Warning,

    /// <summary>
    /// Critical systems unavailable.
    /// </summary>
    Unhealthy
}
