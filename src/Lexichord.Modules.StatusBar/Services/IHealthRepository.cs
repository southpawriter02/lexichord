namespace Lexichord.Modules.StatusBar.Services;

/// <summary>
/// Repository for system health data storage and retrieval.
/// </summary>
/// <remarks>
/// LOGIC: The health repository tracks application uptime and heartbeats.
/// It uses a singleton row pattern (id=1) to store the current session's
/// health information. This allows detection of:
/// - Application uptime (time since startup)
/// - Application responsiveness (heartbeat staleness)
/// - Database connectivity (query success/failure)
/// </remarks>
public interface IHealthRepository
{
    /// <summary>
    /// Gets the system uptime since application started.
    /// </summary>
    /// <returns>The time elapsed since startup.</returns>
    /// <remarks>
    /// LOGIC: Uptime is calculated as current time minus startup time.
    /// The startup time is stored in memory (not database) for accuracy.
    /// </remarks>
    Task<TimeSpan> GetSystemUptimeAsync();

    /// <summary>
    /// Gets the timestamp of the last successful heartbeat.
    /// </summary>
    /// <returns>The last heartbeat time, or null if never recorded.</returns>
    /// <remarks>
    /// LOGIC: If the heartbeat is more than 2 minutes old, the system
    /// should be considered potentially unresponsive.
    /// </remarks>
    Task<DateTime?> GetLastHeartbeatAsync();

    /// <summary>
    /// Records a heartbeat with the current timestamp.
    /// </summary>
    /// <remarks>
    /// LOGIC: This method is called periodically by the HeartbeatService.
    /// It updates the last_heartbeat column in system_health table.
    /// </remarks>
    Task RecordHeartbeatAsync();

    /// <summary>
    /// Records the application start time and initializes the health record.
    /// </summary>
    /// <remarks>
    /// LOGIC: This creates or updates the singleton health record.
    /// Should be called once during module initialization.
    /// </remarks>
    Task RecordStartupAsync();

    /// <summary>
    /// Gets the current database schema version.
    /// </summary>
    /// <returns>The schema version number.</returns>
    /// <remarks>
    /// LOGIC: Used to verify migrations have run and database is compatible.
    /// </remarks>
    Task<int> GetDatabaseVersionAsync();

    /// <summary>
    /// Checks if the database is accessible and responding.
    /// </summary>
    /// <returns>True if database is healthy, false otherwise.</returns>
    Task<bool> CheckHealthAsync();
}
