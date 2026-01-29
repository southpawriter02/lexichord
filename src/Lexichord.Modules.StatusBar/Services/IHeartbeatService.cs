namespace Lexichord.Modules.StatusBar.Services;

/// <summary>
/// Service for periodic heartbeat recording.
/// </summary>
/// <remarks>
/// LOGIC: The heartbeat service runs on a timer and periodically
/// records timestamps to the database. This allows detection of:
/// - Application hangs (heartbeat stops updating)
/// - Background processing issues (timer not firing)
/// - Database connectivity problems (update fails)
///
/// The service should be started during module initialization
/// and stopped during application shutdown.
/// </remarks>
public interface IHeartbeatService : IDisposable
{
    /// <summary>
    /// Starts the heartbeat timer.
    /// </summary>
    /// <remarks>
    /// LOGIC: After calling Start(), heartbeats will be recorded
    /// at the configured interval. An initial heartbeat is recorded
    /// immediately.
    /// </remarks>
    void Start();

    /// <summary>
    /// Stops the heartbeat timer.
    /// </summary>
    /// <remarks>
    /// LOGIC: Stops the timer but does not dispose resources.
    /// The service can be restarted by calling Start() again.
    /// </remarks>
    void Stop();

    /// <summary>
    /// Gets whether the heartbeat service is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the heartbeat interval.
    /// </summary>
    TimeSpan Interval { get; }

    /// <summary>
    /// Gets the time of the last successful heartbeat recording.
    /// </summary>
    DateTime? LastHeartbeat { get; }

    /// <summary>
    /// Gets the number of consecutive heartbeat failures.
    /// </summary>
    int ConsecutiveFailures { get; }

    /// <summary>
    /// Event raised when health status changes.
    /// </summary>
    event EventHandler<bool>? HealthChanged;
}
