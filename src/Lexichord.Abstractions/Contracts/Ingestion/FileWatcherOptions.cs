// =============================================================================
// File: FileWatcherOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for file watcher integration.
// =============================================================================
// LOGIC: Configures how the file watcher filters and processes file changes.
//   - SupportedExtensions controls which file types trigger indexing events.
//   - ExcludedDirectories prevents indexing of build outputs and VCS folders.
//   - DebounceDelayMs prevents rapid successive events for the same file.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Ingestion;

/// <summary>
/// Configuration options for file watcher integration with the RAG pipeline.
/// </summary>
/// <remarks>
/// <para>
/// This record provides configurable settings for the file watcher integration,
/// controlling which files trigger indexing events and how rapidly successive
/// changes are debounced.
/// </para>
/// <para>
/// Use <see cref="Default"/> to obtain an instance with sensible defaults
/// suitable for technical writing projects.
/// </para>
/// </remarks>
/// <param name="Enabled">
/// Whether file watching is enabled. When false, no file change events
/// are processed regardless of other settings.
/// </param>
/// <param name="SupportedExtensions">
/// The file extensions that should trigger indexing events (including the leading dot).
/// Files with extensions not in this list will be ignored.
/// </param>
/// <param name="ExcludedDirectories">
/// Directory names to exclude from file watching. Directory matching is case-insensitive.
/// Files within these directories will not trigger indexing events.
/// </param>
/// <param name="DebounceDelayMs">
/// The debounce delay in milliseconds for file changes. Multiple rapid changes
/// to the same file within this window are coalesced into a single event.
/// </param>
public record FileWatcherOptions(
    bool Enabled,
    IReadOnlyList<string> SupportedExtensions,
    IReadOnlyList<string> ExcludedDirectories,
    int DebounceDelayMs)
{
    /// <summary>
    /// The default debounce delay in milliseconds.
    /// </summary>
    /// <remarks>
    /// 300ms provides a good balance between responsiveness and debouncing
    /// rapid changes (e.g., multiple saves, file sync operations).
    /// </remarks>
    public const int DefaultDebounceDelayMs = 300;

    /// <summary>
    /// Gets the default file watcher options suitable for technical writing projects.
    /// </summary>
    /// <remarks>
    /// <para><b>Default values:</b></para>
    /// <list type="bullet">
    ///   <item><description>Enabled: true</description></item>
    ///   <item><description>Extensions: .md, .txt, .json, .yaml</description></item>
    ///   <item><description>Excluded: .git, node_modules, bin, obj, .vs, .idea</description></item>
    ///   <item><description>Debounce: 300ms</description></item>
    /// </list>
    /// </remarks>
    public static FileWatcherOptions Default { get; } = new(
        Enabled: true,
        SupportedExtensions: [".md", ".txt", ".json", ".yaml"],
        ExcludedDirectories: [".git", "node_modules", "bin", "obj", ".vs", ".idea"],
        DebounceDelayMs: DefaultDebounceDelayMs);

    /// <summary>
    /// Determines whether the specified file extension is supported for indexing.
    /// </summary>
    /// <param name="extension">The file extension to check (with leading dot).</param>
    /// <returns>
    /// <c>true</c> if the extension is in <see cref="SupportedExtensions"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Comparison is case-insensitive (e.g., ".MD" matches ".md").
    /// </remarks>
    public bool IsExtensionSupported(string extension)
    {
        return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the specified directory should be excluded from watching.
    /// </summary>
    /// <param name="directoryName">The directory name to check.</param>
    /// <returns>
    /// <c>true</c> if the directory is in <see cref="ExcludedDirectories"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Comparison is case-insensitive (e.g., ".GIT" matches ".git").
    /// </remarks>
    public bool IsDirectoryExcluded(string directoryName)
    {
        return ExcludedDirectories.Contains(directoryName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a file path should be processed based on extension and directory rules.
    /// </summary>
    /// <param name="filePath">The absolute path to the file.</param>
    /// <returns>
    /// <c>true</c> if the file should trigger indexing;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs two checks:
    /// </para>
    /// <list type="number">
    ///   <item><description>File extension must be in <see cref="SupportedExtensions"/>.</description></item>
    ///   <item><description>No path segment may match <see cref="ExcludedDirectories"/>.</description></item>
    /// </list>
    /// </remarks>
    public bool ShouldProcessFile(string filePath)
    {
        // LOGIC: Check extension first (fast path).
        var extension = Path.GetExtension(filePath);
        if (!IsExtensionSupported(extension))
        {
            return false;
        }

        // LOGIC: Check if any path segment matches an excluded directory.
        var normalizedPath = filePath.Replace('\\', '/');
        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            if (IsDirectoryExcluded(segment))
            {
                return false;
            }
        }

        return true;
    }
}
