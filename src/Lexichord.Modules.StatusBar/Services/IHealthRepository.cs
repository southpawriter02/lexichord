namespace Lexichord.Modules.StatusBar.Services;

/// <summary>
/// Repository for health check data.
/// </summary>
/// <remarks>
/// LOGIC: This interface is part of the StatusBar module's internal contract.
/// It provides database health information for the status indicator.
/// Full implementation comes in v0.0.8b.
/// </remarks>
public interface IHealthRepository
{
    /// <summary>
    /// Records application startup in the database.
    /// </summary>
    Task RecordStartupAsync();

    /// <summary>
    /// Gets the database schema version.
    /// </summary>
    Task<string> GetDatabaseVersionAsync();

    /// <summary>
    /// Checks if the database connection is healthy.
    /// </summary>
    Task<bool> IsHealthyAsync();
}
