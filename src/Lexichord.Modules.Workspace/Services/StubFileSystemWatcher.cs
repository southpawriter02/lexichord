using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Workspace.Services;

/// <summary>
/// Stub implementation of IFileSystemWatcher for v0.1.2a.
/// </summary>
/// <remarks>
/// LOGIC: This is a no-op implementation that allows WorkspaceService to function
/// without a real file watcher. The real implementation (RobustFileSystemWatcher)
/// will be created in v0.1.2b.
///
/// All methods are no-ops except for property accessors.
/// </remarks>
public sealed class StubFileSystemWatcher : IFileSystemWatcher
{
    private readonly ILogger<StubFileSystemWatcher> _logger;

    public StubFileSystemWatcher(ILogger<StubFileSystemWatcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        IgnorePatterns = new List<string>();
    }

    /// <inheritdoc/>
    public string? WatchPath { get; private set; }

    /// <inheritdoc/>
    public bool IsWatching { get; private set; }

    /// <inheritdoc/>
    public int DebounceDelayMs { get; set; } = 100;

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
        _logger.LogDebug(
            "StubFileSystemWatcher: StartWatching called for {Path} (stub - not actually watching)",
            path);

        WatchPath = path;
        IsWatching = true;
    }

    /// <inheritdoc/>
    public void StopWatching()
    {
        _logger.LogDebug("StubFileSystemWatcher: StopWatching called (stub)");

        WatchPath = null;
        IsWatching = false;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        StopWatching();
        // Suppress unused event warnings - these are part of the interface
        _ = ChangesDetected;
        _ = BufferOverflow;
        _ = Error;
    }
}
