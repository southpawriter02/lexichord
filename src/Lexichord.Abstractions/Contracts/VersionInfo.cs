namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Encapsulates version information for the application.
/// </summary>
/// <remarks>
/// LOGIC: Extracted from assembly metadata at runtime. Provides both
/// user-friendly display versions and detailed versions for diagnostics.
///
/// Version: v0.1.6d
/// </remarks>
/// <param name="Version">Semantic version (e.g., "0.1.6").</param>
/// <param name="FullVersion">Full version including build number (e.g., "0.1.6.0").</param>
/// <param name="BuildDate">Date when this build was created.</param>
/// <param name="GitCommit">Git commit hash (short form), if available.</param>
/// <param name="GitBranch">Git branch name, if available.</param>
/// <param name="IsDebugBuild">True if this is a debug build.</param>
/// <param name="RuntimeInfo">Runtime environment info (e.g., ".NET 8.0.0").</param>
public record VersionInfo(
    string Version,
    string FullVersion,
    DateTime BuildDate,
    string? GitCommit,
    string? GitBranch,
    bool IsDebugBuild,
    string RuntimeInfo
)
{
    /// <summary>
    /// Gets the user-friendly display version.
    /// </summary>
    /// <remarks>
    /// LOGIC: Appends "(Debug)" suffix for debug builds to clearly
    /// indicate non-production builds in the UI.
    /// </remarks>
    public string DisplayVersion => IsDebugBuild
        ? $"{Version} (Debug)"
        : Version;

    /// <summary>
    /// Gets the detailed version string for diagnostics.
    /// </summary>
    /// <remarks>
    /// LOGIC: Includes git commit hash when available for precise
    /// build identification in bug reports.
    /// </remarks>
    public string DetailedVersion => GitCommit is not null
        ? $"{FullVersion} ({GitCommit})"
        : FullVersion;
}
