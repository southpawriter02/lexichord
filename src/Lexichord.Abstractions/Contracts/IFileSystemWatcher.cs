namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Robust file system watcher with debouncing, batching, and error recovery.
/// </summary>
/// <remarks>
/// LOGIC: This interface abstracts the complexity of file system watching:
/// - Wraps System.IO.FileSystemWatcher
/// - Debounces rapid changes (git operations, cloud sync)
/// - Batches changes for efficient processing
/// - Handles buffer overflow gracefully
/// - Supports ignore patterns (.git, node_modules)
/// - Recovers from watcher failures
///
/// Design decisions:
/// - Events are batched, not individual (reduces UI thrashing)
/// - Debounce is configurable (default 100ms)
/// - Ignore patterns use glob-style matching
/// - Buffer overflow signals need for full refresh
/// </remarks>
public interface IFileSystemWatcher : IDisposable
{
    /// <summary>
    /// Gets the path currently being watched, or null if not watching.
    /// </summary>
    string? WatchPath { get; }

    /// <summary>
    /// Gets whether the watcher is currently active.
    /// </summary>
    bool IsWatching { get; }

    /// <summary>
    /// Gets or sets the debounce delay in milliseconds.
    /// </summary>
    /// <remarks>
    /// LOGIC: Changes within this window are accumulated into a single batch.
    /// Default: 100ms. Higher values = fewer events but more latency.
    /// Lower values = more responsive but more UI updates.
    /// </remarks>
    int DebounceDelayMs { get; set; }

    /// <summary>
    /// Gets the collection of ignore patterns.
    /// </summary>
    /// <remarks>
    /// LOGIC: Patterns use simple glob-style matching:
    /// - ".git" matches any path containing ".git" segment
    /// - "*.tmp" matches files ending in .tmp
    /// - "node_modules" matches the node_modules folder
    ///
    /// Default patterns: .git, .svn, .hg, node_modules, __pycache__, .DS_Store
    /// </remarks>
    IList<string> IgnorePatterns { get; }

    /// <summary>
    /// Starts watching a directory for changes.
    /// </summary>
    /// <param name="path">Directory path to watch.</param>
    /// <param name="filter">File filter pattern (default: all files).</param>
    /// <param name="includeSubdirectories">Watch subdirectories (default: true).</param>
    /// <remarks>
    /// LOGIC: If already watching, stops current watcher first.
    /// Monitors: Created, Changed, Deleted, Renamed events.
    /// Subdirectory watching is recursive by default.
    /// </remarks>
    void StartWatching(string path, string filter = "*.*", bool includeSubdirectories = true);

    /// <summary>
    /// Stops watching the current directory.
    /// </summary>
    /// <remarks>
    /// LOGIC: Stops the watcher and flushes any pending changes.
    /// Idempotent - calling when not watching is a no-op.
    /// </remarks>
    void StopWatching();

    /// <summary>
    /// Event raised when file system changes are detected.
    /// </summary>
    /// <remarks>
    /// LOGIC: Raised after debounce window with accumulated changes.
    /// May contain multiple changes from different files/operations.
    /// </remarks>
    event EventHandler<FileSystemChangeBatchEventArgs>? ChangesDetected;

    /// <summary>
    /// Event raised when a buffer overflow occurs.
    /// </summary>
    /// <remarks>
    /// LOGIC: Indicates some events may have been lost.
    /// Consumers should trigger a full refresh of their state.
    /// </remarks>
    event EventHandler<EventArgs>? BufferOverflow;

    /// <summary>
    /// Event raised when an error occurs in the watcher.
    /// </summary>
    /// <remarks>
    /// LOGIC: IsRecoverable indicates if watcher is still functional.
    /// If not recoverable, consumer should handle gracefully.
    /// </remarks>
    event EventHandler<FileSystemWatcherErrorEventArgs>? Error;
}

/// <summary>
/// Batch of file system changes detected by the watcher.
/// </summary>
public class FileSystemChangeBatchEventArgs : EventArgs
{
    /// <summary>
    /// Gets the collection of changes in this batch.
    /// </summary>
    public required IReadOnlyList<FileSystemChangeInfo> Changes { get; init; }

    /// <summary>
    /// Gets the timestamp when the batch was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Information about a single file system change.
/// </summary>
/// <param name="ChangeType">The type of change (Created, Changed, Deleted, Renamed).</param>
/// <param name="FullPath">Full path to the affected file or directory.</param>
/// <param name="OldPath">For renames, the previous path. Null for other change types.</param>
/// <param name="IsDirectory">True if the change affects a directory.</param>
/// <remarks>
/// LOGIC: FileSystemChangeInfo is immutable (record).
/// For Renamed changes, OldPath contains the previous path.
/// IsDirectory is determined at event time (may be stale for deletes).
/// </remarks>
public record FileSystemChangeInfo(
    FileSystemChangeType ChangeType,
    string FullPath,
    string? OldPath,
    bool IsDirectory
)
{
    /// <summary>
    /// Gets the file or directory name (without path).
    /// </summary>
    public string Name => Path.GetFileName(FullPath) ?? FullPath;

    /// <summary>
    /// Gets the parent directory path.
    /// </summary>
    public string? ParentPath => Path.GetDirectoryName(FullPath);
}

/// <summary>
/// Types of file system changes.
/// </summary>
public enum FileSystemChangeType
{
    /// <summary>File or directory was created.</summary>
    Created,

    /// <summary>File content was modified.</summary>
    Changed,

    /// <summary>File or directory was deleted.</summary>
    Deleted,

    /// <summary>File or directory was renamed.</summary>
    Renamed
}

/// <summary>
/// Error information from the file system watcher.
/// </summary>
public class FileSystemWatcherErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the exception that occurred.
    /// </summary>
    public required Exception Exception { get; init; }

    /// <summary>
    /// Gets whether the watcher is still functional.
    /// </summary>
    /// <remarks>
    /// LOGIC: If false, the watcher has stopped and needs manual restart.
    /// </remarks>
    public required bool IsRecoverable { get; init; }
}
