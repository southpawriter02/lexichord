namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for detecting and handling first run scenarios.
/// </summary>
/// <remarks>
/// LOGIC: This service tracks application version across runs to detect:
/// - Fresh installation (no previous version stored)
/// - Update from previous version (stored version differs)
/// - Normal run (stored version matches current)
///
/// Detection happens during startup, before the main window is shown.
/// The service coordinates with IEditorService to display release notes.
///
/// Usage Pattern:
/// 1. Access IsFirstRunAfterUpdate or IsFirstRunEver during startup
/// 2. If update, call GetReleaseNotesAsync() and display
/// 3. Call MarkRunCompletedAsync() to update stored version
///
/// Version: v0.1.7c
/// </remarks>
public interface IFirstRunService
{
    /// <summary>
    /// Gets whether this is the first run after an update.
    /// </summary>
    /// <remarks>
    /// LOGIC: True when:
    /// - Previous version is stored AND
    /// - Previous version differs from current version
    ///
    /// Use this to trigger release notes display.
    /// </remarks>
    bool IsFirstRunAfterUpdate { get; }

    /// <summary>
    /// Gets whether this is the first run ever (fresh install).
    /// </summary>
    /// <remarks>
    /// LOGIC: True when no previous version is stored.
    /// May also check Velopack LEXICHORD_FIRST_RUN environment variable.
    ///
    /// Use this to trigger welcome/onboarding flow.
    /// </remarks>
    bool IsFirstRunEver { get; }

    /// <summary>
    /// Gets the previously stored version, if any.
    /// </summary>
    /// <remarks>
    /// LOGIC: Null for fresh installations.
    /// Contains the version string from the last run.
    /// </remarks>
    string? PreviousVersion { get; }

    /// <summary>
    /// Gets the current application version.
    /// </summary>
    /// <remarks>
    /// LOGIC: Extracted from the executing assembly.
    /// Format: Major.Minor.Build.Revision (e.g., "0.1.7.0")
    /// </remarks>
    string CurrentVersion { get; }

    /// <summary>
    /// Gets the path to the bundled CHANGELOG.md file.
    /// </summary>
    /// <remarks>
    /// LOGIC: CHANGELOG.md is copied to output directory during build.
    /// Path is relative to AppContext.BaseDirectory.
    /// </remarks>
    string ChangelogPath { get; }

    /// <summary>
    /// Marks the current run as completed, storing the current version.
    /// </summary>
    /// <remarks>
    /// LOGIC: Call this after displaying release notes or welcome screen.
    /// Updates the stored version to prevent re-triggering on next launch.
    /// Resets IsFirstRunAfterUpdate and IsFirstRunEver to false.
    /// </remarks>
    Task MarkRunCompletedAsync();

    /// <summary>
    /// Gets the release notes content for display.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Markdown content of CHANGELOG.md.</returns>
    /// <remarks>
    /// LOGIC: Reads the bundled CHANGELOG.md file.
    /// Returns fallback content if file not found.
    /// </remarks>
    Task<string> GetReleaseNotesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets release notes for a specific version range.
    /// </summary>
    /// <param name="fromVersion">Start version (exclusive).</param>
    /// <param name="toVersion">End version (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Markdown content for the version range.</returns>
    /// <remarks>
    /// LOGIC: Parses CHANGELOG.md to extract entries between versions.
    /// Useful for showing only changes since last run.
    /// </remarks>
    Task<string> GetReleaseNotesForRangeAsync(
        string fromVersion,
        string toVersion,
        CancellationToken cancellationToken = default);
}
