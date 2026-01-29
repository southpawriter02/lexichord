namespace Lexichord.Modules.StatusBar.Services;

/// <summary>
/// Service for periodic health checks (heartbeat).
/// </summary>
/// <remarks>
/// LOGIC: The heartbeat service periodically checks database health
/// and updates the status bar indicator.
/// Full implementation comes in v0.0.8b.
/// </remarks>
public interface IHeartbeatService
{
    /// <summary>
    /// Gets the heartbeat interval.
    /// </summary>
    TimeSpan Interval { get; }

    /// <summary>
    /// Gets whether the heartbeat service is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the heartbeat service.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the heartbeat service.
    /// </summary>
    void Stop();

    /// <summary>
    /// Event raised when health status changes.
    /// </summary>
    event EventHandler<bool>? HealthChanged;
}
