namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Configuration options for the Recent Files feature.
/// </summary>
/// <remarks>
/// LOGIC: These options are loaded from appsettings.json section "RecentFiles".
///
/// Example configuration:
/// <code>
/// {
///   "RecentFiles": {
///     "MaxEntries": 10,
///     "AutoPruneOnStartup": true,
///     "ShowFilePaths": false
///   }
/// }
/// </code>
/// </remarks>
public record RecentFilesOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "RecentFiles";

    /// <summary>
    /// Maximum number of entries to keep in history.
    /// </summary>
    /// <remarks>
    /// LOGIC: When adding a new file that would exceed this limit,
    /// the oldest entry (by LastOpenedAt) is removed.
    /// Default: 10. Range: 1-50.
    /// </remarks>
    public int MaxEntries { get; init; } = 10;

    /// <summary>
    /// Whether to automatically prune non-existent files on application startup.
    /// </summary>
    /// <remarks>
    /// LOGIC: When enabled, the service calls PruneInvalidEntriesAsync during
    /// initialization. This keeps the list clean without user intervention.
    /// Default: true.
    /// </remarks>
    public bool AutoPruneOnStartup { get; init; } = true;

    /// <summary>
    /// Whether to show full file paths in the Recent Files menu.
    /// </summary>
    /// <remarks>
    /// LOGIC: When false (default), shows only the file name.
    /// When true, shows the full path which can help distinguish
    /// files with the same name in different directories.
    /// </remarks>
    public bool ShowFilePaths { get; init; }
}
