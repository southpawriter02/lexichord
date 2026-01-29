using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// File system-based implementation of <see cref="IStyleConfigurationWatcher"/>.
/// </summary>
/// <remarks>
/// LOGIC: v0.2.1a stub - no-op implementation.
/// Full implementation in v0.2.1d will:
/// - Use FileSystemWatcher to monitor lexichord.yaml
/// - Debounce rapid changes (100ms delay)
/// - Auto-restart on watcher errors
/// - Respect license tier for this feature
/// </remarks>
public sealed class FileSystemStyleWatcher : IStyleConfigurationWatcher
{
    private readonly ILogger<FileSystemStyleWatcher> _logger;
    private bool _disposed;

    /// <inheritdoc/>
    public bool IsWatching { get; private set; }

    /// <inheritdoc/>
    public string? WatchedPath { get; private set; }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: v0.2.1a stub - event declared but never raised.
    /// Full implementation in v0.2.1d will raise this event.
    /// </remarks>
#pragma warning disable CS0067 // Event is never used (stub implementation)
    public event EventHandler<StyleFileChangedEventArgs>? FileChanged;

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: v0.2.1a stub - event declared but never raised.
    /// Full implementation in v0.2.1d will raise this event.
    /// </remarks>
    public event EventHandler<StyleWatcherErrorEventArgs>? WatcherError;
#pragma warning restore CS0067

    /// <summary>
    /// Initializes a new instance of <see cref="FileSystemStyleWatcher"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public FileSystemStyleWatcher(ILogger<FileSystemStyleWatcher> logger)
    {
        _logger = logger;
        _logger.LogDebug("FileSystemStyleWatcher initialized (stub implementation)");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: v0.2.1a stub - logs and sets state but does not watch.
    /// Full implementation in v0.2.1d.
    /// </remarks>
    public void StartWatching(string projectRoot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);

        if (IsWatching)
        {
            StopWatching();
        }

        _logger.LogDebug("StartWatching called for '{ProjectRoot}' (stub - not actually watching)", projectRoot);

        WatchedPath = projectRoot;
        IsWatching = true;

        // TODO v0.2.1d: Create FileSystemWatcher and start monitoring
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: v0.2.1a stub - clears state.
    /// Full implementation in v0.2.1d will dispose FileSystemWatcher.
    /// </remarks>
    public void StopWatching()
    {
        if (!IsWatching)
        {
            return;
        }

        _logger.LogDebug("StopWatching called for '{WatchedPath}'", WatchedPath);

        IsWatching = false;
        WatchedPath = null;

        // TODO v0.2.1d: Dispose FileSystemWatcher
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopWatching();
        _disposed = true;

        _logger.LogDebug("FileSystemStyleWatcher disposed");
    }
}
