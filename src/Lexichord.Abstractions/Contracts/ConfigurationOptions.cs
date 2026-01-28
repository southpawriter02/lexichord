namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Application-wide configuration options for Lexichord.
/// </summary>
/// <remarks>
/// LOGIC: These options are loaded from appsettings.json, environment variables,
/// and command-line arguments. Later sources override earlier ones.
///
/// Configuration key: "Lexichord"
/// </remarks>
public record LexichordOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Lexichord";

    /// <summary>
    /// Application display name shown in title bars and dialogs.
    /// </summary>
    /// <example>Lexichord</example>
    public string ApplicationName { get; init; } = "Lexichord";

    /// <summary>
    /// Current environment (Development, Staging, Production).
    /// </summary>
    /// <remarks>
    /// LOGIC: Determines which appsettings.{Environment}.json to load.
    /// Also affects logging verbosity and feature flag defaults.
    /// </remarks>
    public string Environment { get; init; } = "Production";

    /// <summary>
    /// Base path for application data storage.
    /// </summary>
    /// <remarks>
    /// LOGIC: When null, defaults to platform-appropriate location:
    /// - Windows: %APPDATA%/Lexichord
    /// - macOS: ~/Library/Application Support/Lexichord
    /// - Linux: ~/.config/Lexichord
    /// </remarks>
    public string? DataPath { get; init; }

    /// <summary>
    /// Enable verbose debug logging regardless of environment.
    /// </summary>
    /// <remarks>
    /// LOGIC: Can be enabled via:
    /// - appsettings.json: "Lexichord:DebugMode": true
    /// - Environment: LEXICHORD_DEBUGMODE=true
    /// - CLI: --debug-mode or -d
    /// </remarks>
    public bool DebugMode { get; init; }

    /// <summary>
    /// Gets the resolved data path (uses default if not configured).
    /// </summary>
    public string GetResolvedDataPath()
    {
        if (!string.IsNullOrEmpty(DataPath))
            return DataPath;

        var appData = System.Environment.GetFolderPath(
            System.Environment.SpecialFolder.ApplicationData);
        return System.IO.Path.Combine(appData, "Lexichord");
    }
}

/// <summary>
/// Debug-specific options for development and troubleshooting.
/// </summary>
/// <remarks>
/// Configuration key: "Debug"
/// These options are typically only enabled in Development environment.
/// </remarks>
public record DebugOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Debug";

    /// <summary>
    /// Show Avalonia DevTools window on startup.
    /// </summary>
    /// <remarks>
    /// LOGIC: Only has effect in Debug builds. Allows F12 to open DevTools.
    /// </remarks>
    public bool ShowDevTools { get; init; }

    /// <summary>
    /// Enable performance timing logs for all service methods.
    /// </summary>
    /// <remarks>
    /// LOGIC: Logs entry/exit times for methods decorated with [LogPerformance].
    /// Useful for identifying slow operations.
    /// </remarks>
    public bool EnablePerformanceLogging { get; init; }

    /// <summary>
    /// Simulate slow network for testing loading states.
    /// </summary>
    /// <remarks>
    /// LOGIC: Adds artificial delay to all HTTP requests.
    /// 0 = disabled, any positive value = delay in milliseconds.
    /// </remarks>
    public int SimulatedNetworkDelayMs { get; init; }
}

/// <summary>
/// Feature flags for gradual rollout and A/B testing.
/// </summary>
/// <remarks>
/// Configuration key: "FeatureFlags"
/// These flags control access to experimental or in-development features.
/// </remarks>
public record FeatureFlagOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "FeatureFlags";

    /// <summary>
    /// Enable experimental features that are not yet production-ready.
    /// </summary>
    public bool EnableExperimentalFeatures { get; init; }
}
