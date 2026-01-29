namespace Lexichord.Host.Settings;

using Lexichord.Abstractions.Contracts;

/// <summary>
/// Settings record for update configuration persistence.
/// </summary>
/// <remarks>
/// LOGIC: Persisted via ISettingsService to maintain update preferences.
/// Channel is stored as string for JSON compatibility.
///
/// Version: v0.1.6d
/// </remarks>
public sealed record UpdateSettings
{
    /// <summary>
    /// The settings section name for serialization.
    /// </summary>
    public const string SectionName = "Updates";

    /// <summary>
    /// Gets the selected update channel as a string.
    /// </summary>
    /// <remarks>
    /// LOGIC: Stored as string ("Stable" or "Insider") for JSON serialization.
    /// Use GetUpdateChannel() to get the enum value.
    /// </remarks>
    public string Channel { get; init; } = "Stable";

    /// <summary>
    /// Gets whether automatic update checks are enabled.
    /// </summary>
    /// <remarks>
    /// LOGIC: Reserved for future implementation. When true,
    /// the app will check for updates on a schedule.
    /// </remarks>
    public bool AutoCheck { get; init; } = true;

    /// <summary>
    /// Gets the interval in hours between automatic update checks.
    /// </summary>
    public int AutoCheckIntervalHours { get; init; } = 24;

    /// <summary>
    /// Gets the last time an update check was performed.
    /// </summary>
    public DateTime? LastCheckTime { get; init; }

    /// <summary>
    /// Converts the string channel to the UpdateChannel enum.
    /// </summary>
    /// <returns>The UpdateChannel value.</returns>
    public UpdateChannel GetUpdateChannel()
    {
        return Channel switch
        {
            "Insider" => UpdateChannel.Insider,
            _ => UpdateChannel.Stable
        };
    }
}
