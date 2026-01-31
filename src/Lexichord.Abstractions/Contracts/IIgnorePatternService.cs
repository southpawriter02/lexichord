namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for managing file ignore patterns from .lexichordignore files.
/// </summary>
/// <remarks>
/// LOGIC: IIgnorePatternService provides glob-based file exclusion from style analysis.
///
/// Pattern Format:
/// - Standard glob patterns: *.log, build/, **/temp/*
/// - Negation patterns: !important.log (re-include excluded files)
/// - Comments: Lines starting with # are ignored
/// - Empty lines are ignored
///
/// License Tiers:
/// - Core: Maximum 5 patterns (excess patterns are silently ignored)
/// - Writer Pro: Unlimited patterns
///
/// Hot-Reload:
/// - File watcher monitors .lexichordignore for changes
/// - Pattern changes trigger automatic reload
/// - PatternsReloaded event notifies subscribers
///
/// Thread Safety:
/// - All methods are thread-safe via ReaderWriterLockSlim
/// - IsIgnored uses read lock (allows concurrent reads)
/// - Pattern loading uses write lock (exclusive access)
///
/// Version: v0.3.6d
/// </remarks>
public interface IIgnorePatternService : IDisposable
{
    /// <summary>
    /// Gets the number of currently active patterns.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns the count of patterns currently in use.
    /// For Core tier, this may be less than the file contains if truncated.
    /// </remarks>
    int PatternCount { get; }

    /// <summary>
    /// Gets the number of patterns that were truncated due to license limits.
    /// </summary>
    /// <remarks>
    /// LOGIC: For Core tier, patterns beyond the first 5 are truncated.
    /// This property tracks how many patterns were dropped.
    /// Returns 0 for Writer Pro tier.
    /// </remarks>
    int TruncatedPatternCount { get; }

    /// <summary>
    /// Gets whether the service is currently watching for file changes.
    /// </summary>
    bool IsWatching { get; }

    /// <summary>
    /// Checks if a file path matches any ignore pattern.
    /// </summary>
    /// <param name="filePath">
    /// The absolute or workspace-relative file path to check.
    /// </param>
    /// <returns>
    /// True if the file should be ignored (matches a pattern and is not
    /// negated); false otherwise.
    /// </returns>
    /// <remarks>
    /// LOGIC: Pattern matching logic:
    /// 1. Convert path to workspace-relative if absolute
    /// 2. Check all patterns in order
    /// 3. Negation patterns (!pattern) un-ignore previously matched files
    /// 4. Empty or null paths return false (never ignored)
    ///
    /// Performance: O(n) where n is pattern count. Target &lt;10ms for 100 checks.
    /// </remarks>
    bool IsIgnored(string? filePath);

    /// <summary>
    /// Manually registers patterns without loading from file.
    /// </summary>
    /// <param name="patterns">Collection of glob patterns to register.</param>
    /// <remarks>
    /// LOGIC: Useful for testing or programmatic pattern registration.
    /// - Replaces any previously registered patterns
    /// - Patterns are subject to license tier limits
    /// - Does NOT start file watching (use LoadFromWorkspaceAsync for that)
    /// - Raises PatternsReloaded event after loading
    /// </remarks>
    void RegisterPatterns(IEnumerable<string> patterns);

    /// <summary>
    /// Loads patterns from the workspace's .lexichordignore file.
    /// </summary>
    /// <param name="workspacePath">Root path of the workspace.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Task that completes when patterns are loaded.
    /// Returns true if file was found and loaded; false if no ignore file exists.
    /// </returns>
    /// <remarks>
    /// LOGIC: File location: {workspacePath}/.lexichord/.lexichordignore
    ///
    /// Behavior:
    /// - Creates file watcher for hot-reload if file exists
    /// - Clears existing patterns before loading
    /// - Skips comments (lines starting with #) and empty lines
    /// - Respects license tier pattern limits
    /// - File size limit: 100KB (larger files are rejected)
    /// - Raises PatternsReloaded event after loading
    /// </remarks>
    Task<bool> LoadFromWorkspaceAsync(string workspacePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops file watching and clears all patterns.
    /// </summary>
    /// <remarks>
    /// LOGIC: Called when workspace is closed or service is disposed.
    /// - Stops file watcher
    /// - Clears all patterns
    /// - Raises PatternsReloaded with count 0
    /// </remarks>
    void Clear();

    /// <summary>
    /// Event raised when patterns are loaded or reloaded.
    /// </summary>
    /// <remarks>
    /// LOGIC: Raised after:
    /// - Initial load via LoadFromWorkspaceAsync
    /// - Hot-reload from file watcher
    /// - Manual registration via RegisterPatterns
    /// - Clear operation
    ///
    /// Subscribers can use this to update UI or logging.
    /// </remarks>
    event EventHandler<PatternsReloadedEventArgs>? PatternsReloaded;
}
