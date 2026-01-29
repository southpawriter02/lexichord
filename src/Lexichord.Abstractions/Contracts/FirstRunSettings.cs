namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Settings related to first run and update detection.
/// </summary>
/// <remarks>
/// LOGIC: Stored in a JSON file following the UpdateSettings pattern.
/// Persisted via file-based storage in the application data directory.
///
/// Version: v0.1.7c
/// </remarks>
public sealed record FirstRunSettings
{
    /// <summary>
    /// The settings section name for serialization.
    /// </summary>
    public const string SectionName = "FirstRun";

    /// <summary>
    /// The version that was running on last launch.
    /// </summary>
    /// <remarks>
    /// Null if application has never been run.
    /// </remarks>
    public string? LastRunVersion { get; init; }

    /// <summary>
    /// Whether to show release notes after updates.
    /// </summary>
    /// <remarks>
    /// Default is true. Users can disable in Settings.
    /// </remarks>
    public bool ShowReleaseNotesOnUpdate { get; init; } = true;

    /// <summary>
    /// Whether to show welcome screen on first run.
    /// </summary>
    /// <remarks>
    /// Default is true. Only shown once.
    /// </remarks>
    public bool ShowWelcomeOnFirstRun { get; init; } = true;

    /// <summary>
    /// When the first run occurred.
    /// </summary>
    public DateTimeOffset? FirstRunDate { get; init; }

    /// <summary>
    /// Installation ID for anonymous analytics.
    /// </summary>
    /// <remarks>
    /// Generated once on first run, not tied to any PII.
    /// Used for crash correlation if telemetry enabled.
    /// </remarks>
    public string? InstallationId { get; init; }
}
