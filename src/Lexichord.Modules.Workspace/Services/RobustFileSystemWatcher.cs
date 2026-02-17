using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Lexichord.Modules.Workspace.Services;

/// <summary>
/// Robust file system watcher with debouncing, batching, and error recovery.
/// </summary>
/// <remarks>
/// LOGIC: This implementation wraps System.IO.FileSystemWatcher to address common issues:
/// - Debouncing: Accumulates changes for a configurable delay before emitting
/// - Batching: Multiple changes emit as a single event
/// - Buffer Overflow: Detects overflow and signals need for full refresh
/// - Ignore Patterns: Filters common directories (.git, node_modules)
/// - Error Recovery: Attempts to restart watcher on failures
///
/// Design decisions:
/// - ConcurrentDictionary for thread-safe change buffer
/// - System.Timers.Timer for debouncing (thread-pool execution)
/// - Path normalization for consistent cross-platform behavior
/// </remarks>
public sealed class RobustFileSystemWatcher : IFileSystemWatcher
{
    private const int DefaultDebounceMs = 100;
    private const int DefaultBufferSize = 64 * 1024; // 64KB
    private const int RecoveryDelayMs = 1000;
    private const int MaxRecoveryAttempts = 3;

    private readonly ILogger<RobustFileSystemWatcher> _logger;
    private readonly ConcurrentDictionary<string, FileSystemChangeInfo> _changeBuffer = new();
    private readonly object _lock = new();

    private FileSystemWatcher? _innerWatcher;
    private Timer? _debounceTimer;
    private bool _disposed;
    private int _recoveryAttempts;

    /// <summary>
    /// Default patterns for files and directories to ignore.
    /// </summary>
    private static readonly string[] DefaultIgnorePatterns = new[]
    {
        ".git",
        ".svn",
        ".hg",
        "node_modules",
        "__pycache__",
        ".DS_Store",
        "Thumbs.db",
        "*.tmp",
        "*.temp",
        "~$*"
    };

    /// <summary>
    /// Initializes a new instance of RobustFileSystemWatcher.
    /// </summary>
    public RobustFileSystemWatcher(ILogger<RobustFileSystemWatcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        DebounceDelayMs = DefaultDebounceMs;
        IgnorePatterns = new List<string>(DefaultIgnorePatterns);

        _logger.LogDebug("RobustFileSystemWatcher created with {DebounceMs}ms debounce delay",
            DebounceDelayMs);
    }

    /// <inheritdoc/>
    public string? WatchPath { get; private set; }

    /// <inheritdoc/>
    public bool IsWatching { get; private set; }

    /// <inheritdoc/>
    public int DebounceDelayMs { get; set; }

    /// <inheritdoc/>
    public IList<string> IgnorePatterns { get; }

    /// <inheritdoc/>
    public event EventHandler<FileSystemChangeBatchEventArgs>? ChangesDetected;

    /// <inheritdoc/>
    public event EventHandler<EventArgs>? BufferOverflow;

    /// <inheritdoc/>
    public event EventHandler<FileSystemWatcherErrorEventArgs>? Error;

    /// <inheritdoc/>
    public void StartWatching(string path, string filter = "*.*", bool includeSubdirectories = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        lock (_lock)
        {
            // LOGIC: Stop current watcher if already watching
            if (IsWatching)
            {
                _logger.LogDebug("Already watching, stopping current watcher before starting new one");
                StopWatchingInternal();
            }

            var normalizedPath = Path.GetFullPath(path);

            if (!Directory.Exists(normalizedPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {normalizedPath}");
            }

            _logger.LogInformation(
                "Starting file system watcher on {Path} with filter '{Filter}' (includeSubdirs: {IncludeSubdirs})",
                normalizedPath, filter, includeSubdirectories);

            try
            {
                // LOGIC: Create and configure inner watcher
                _innerWatcher = new FileSystemWatcher(normalizedPath, filter)
                {
                    IncludeSubdirectories = includeSubdirectories,
                    InternalBufferSize = DefaultBufferSize,
                    NotifyFilter = NotifyFilters.FileName
                                   | NotifyFilters.DirectoryName
                                   | NotifyFilters.LastWrite
                                   | NotifyFilters.CreationTime
                };

                // LOGIC: Wire up event handlers
                _innerWatcher.Created += OnInnerCreated;
                _innerWatcher.Changed += OnInnerChanged;
                _innerWatcher.Deleted += OnInnerDeleted;
                _innerWatcher.Renamed += OnInnerRenamed;
                _innerWatcher.Error += OnInnerError;

                // LOGIC: Create debounce timer
                _debounceTimer = new Timer(DebounceDelayMs);
                _debounceTimer.Elapsed += OnDebounceTimerElapsed;
                _debounceTimer.AutoReset = false;

                // LOGIC: Enable watching
                _innerWatcher.EnableRaisingEvents = true;

                WatchPath = normalizedPath;
                IsWatching = true;
                _recoveryAttempts = 0;

                _logger.LogInformation("File system watcher started successfully on {Path}", normalizedPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start file system watcher on {Path}", normalizedPath);
                CleanupWatcher();
                throw;
            }
        }
    }

    /// <inheritdoc/>
    public void StopWatching()
    {
        lock (_lock)
        {
            if (!IsWatching)
            {
                _logger.LogDebug("StopWatching called but not currently watching");
                return;
            }

            StopWatchingInternal();
        }
    }

    /// <summary>
    /// Stops watching without acquiring lock (must be called while holding lock).
    /// </summary>
    private void StopWatchingInternal()
    {
        var watchPath = WatchPath;
        _logger.LogInformation("Stopping file system watcher on {Path}", watchPath);

        // LOGIC: Flush any pending changes before stopping
        FlushChanges();

        CleanupWatcher();

        _logger.LogInformation("File system watcher stopped for {Path}", watchPath);
    }

    /// <summary>
    /// Cleans up watcher resources.
    /// </summary>
    private void CleanupWatcher()
    {
        if (_innerWatcher is not null)
        {
            _innerWatcher.EnableRaisingEvents = false;
            _innerWatcher.Created -= OnInnerCreated;
            _innerWatcher.Changed -= OnInnerChanged;
            _innerWatcher.Deleted -= OnInnerDeleted;
            _innerWatcher.Renamed -= OnInnerRenamed;
            _innerWatcher.Error -= OnInnerError;
            _innerWatcher.Dispose();
            _innerWatcher = null;
        }

        if (_debounceTimer is not null)
        {
            _debounceTimer.Stop();
            _debounceTimer.Elapsed -= OnDebounceTimerElapsed;
            _debounceTimer.Dispose();
            _debounceTimer = null;
        }

        _changeBuffer.Clear();
        WatchPath = null;
        IsWatching = false;
    }

    #region Inner Watcher Event Handlers

    private void OnInnerCreated(object sender, FileSystemEventArgs e)
    {
        ProcessChange(FileSystemChangeType.Created, e.FullPath, null);
    }

    private void OnInnerChanged(object sender, FileSystemEventArgs e)
    {
        ProcessChange(FileSystemChangeType.Changed, e.FullPath, null);
    }

    private void OnInnerDeleted(object sender, FileSystemEventArgs e)
    {
        ProcessChange(FileSystemChangeType.Deleted, e.FullPath, null);
    }

    private void OnInnerRenamed(object sender, RenamedEventArgs e)
    {
        ProcessChange(FileSystemChangeType.Renamed, e.FullPath, e.OldFullPath);
    }

    private async void OnInnerError(object sender, ErrorEventArgs e)
    {
        var exception = e.GetException();
        _logger.LogError(exception, "File system watcher error occurred");

        // LOGIC: Handle buffer overflow specifically
        if (exception is InternalBufferOverflowException)
        {
            _logger.LogWarning("File system watcher buffer overflow - some events may have been lost");
            BufferOverflow?.Invoke(this, EventArgs.Empty);
            return;
        }

        // LOGIC: Attempt recovery for other errors
        var recovered = await TryRecoverWatcherAsync();
        Error?.Invoke(this, new FileSystemWatcherErrorEventArgs
        {
            Exception = exception,
            IsRecoverable = recovered
        });
    }

    #endregion

    #region Change Processing

    /// <summary>
    /// Processes a file system change event.
    /// </summary>
    private void ProcessChange(FileSystemChangeType changeType, string fullPath, string? oldPath)
    {
        // LOGIC: Check ignore patterns
        if (ShouldIgnore(fullPath))
        {
            _logger.LogTrace("Ignoring change to {Path} (matches ignore pattern)", fullPath);
            return;
        }

        // LOGIC: Determine if target is a directory
        // Note: For deletes, the item might not exist anymore
        var isDirectory = changeType != FileSystemChangeType.Deleted && Directory.Exists(fullPath);

        var changeInfo = new FileSystemChangeInfo(
            changeType,
            fullPath,
            oldPath,
            isDirectory
        );

        // LOGIC: Add to buffer (overwrites existing entry for same path)
        // This naturally deduplicates rapid changes to same file
        _changeBuffer[fullPath] = changeInfo;

        _logger.LogTrace(
            "Buffered {ChangeType} for {Path} (isDir: {IsDirectory})",
            changeType, fullPath, isDirectory);

        // LOGIC: Reset debounce timer
        ResetDebounceTimer();
    }

    /// <summary>
    /// Checks if a path should be ignored based on ignore patterns.
    /// </summary>
    internal bool ShouldIgnore(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        foreach (var pattern in IgnorePatterns)
        {
            if (MatchPattern(path, pattern))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Matches a path against a pattern (glob-style).
    /// </summary>
    /// <remarks>
    /// LOGIC: Pattern matching rules:
    /// - "*.ext" matches files ending with .ext
    /// - "~$*" matches files starting with ~$
    /// - "dirname" matches any path segment equal to dirname
    /// </remarks>
    private static bool MatchPattern(string input, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return false;

        var fileName = Path.GetFileName(input);

        // LOGIC: Handle wildcard patterns
        if (pattern.Contains('*'))
        {
            // *.ext pattern (file extension)
            if (pattern.StartsWith("*."))
            {
                var extension = pattern[1..]; // ".ext"
                return fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
            }

            // ~$* pattern (prefix match)
            if (pattern.EndsWith("*"))
            {
                var prefix = pattern[..^1]; // Remove trailing *
                return fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        // LOGIC: Check if any path segment matches the pattern exactly
        // This handles .git, node_modules, etc.
        var normalizedPath = input.Replace('\\', '/');
        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return segments.Any(segment =>
            segment.Equals(pattern, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Debounce Timer

    /// <summary>
    /// Resets the debounce timer.
    /// </summary>
    private void ResetDebounceTimer()
    {
        lock (_lock)
        {
            if (_debounceTimer is null || _disposed)
                return;

            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
    }

    /// <summary>
    /// Handles debounce timer elapsed.
    /// </summary>
    private void OnDebounceTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        FlushChanges();
    }

    /// <summary>
    /// Flushes buffered changes and raises ChangesDetected event.
    /// </summary>
    private void FlushChanges()
    {
        if (_changeBuffer.IsEmpty)
            return;

        // LOGIC: Extract all changes atomically
        var changes = new List<FileSystemChangeInfo>();
        var keys = _changeBuffer.Keys.ToArray();

        foreach (var key in keys)
        {
            if (_changeBuffer.TryRemove(key, out var changeInfo))
            {
                changes.Add(changeInfo);
            }
        }

        if (changes.Count == 0)
            return;

        _logger.LogDebug("Flushing {Count} buffered changes", changes.Count);

        // LOGIC: Raise batched event
        ChangesDetected?.Invoke(this, new FileSystemChangeBatchEventArgs
        {
            Changes = changes.AsReadOnly(),
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    #endregion

    #region Error Recovery

    /// <summary>
    /// Attempts to recover the file system watcher after an error.
    /// </summary>
    private async Task<bool> TryRecoverWatcherAsync()
    {
        if (_recoveryAttempts >= MaxRecoveryAttempts)
        {
            _logger.LogError(
                "Max recovery attempts ({Max}) reached, giving up on watcher recovery",
                MaxRecoveryAttempts);
            return false;
        }

        _recoveryAttempts++;
        _logger.LogWarning(
            "Attempting watcher recovery (attempt {Attempt}/{Max})",
            _recoveryAttempts, MaxRecoveryAttempts);

        var currentPath = WatchPath;
        if (string.IsNullOrEmpty(currentPath))
        {
            _logger.LogError("Cannot recover watcher - no watch path recorded");
            return false;
        }

        try
        {
            lock (_lock)
            {
                CleanupWatcher();
            }

            // LOGIC: Brief delay before restart
            await Task.Delay(RecoveryDelayMs);

            // LOGIC: Restart watching
            StartWatching(currentPath);

            _logger.LogInformation("Watcher recovery successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Watcher recovery failed");
            return false;
        }
    }

    #endregion

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;

            _logger.LogDebug("Disposing RobustFileSystemWatcher");
            StopWatchingInternal();
        }
    }
}
