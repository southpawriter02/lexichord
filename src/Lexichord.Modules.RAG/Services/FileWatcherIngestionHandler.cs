// =============================================================================
// File: FileWatcherIngestionHandler.cs
// Project: Lexichord.Modules.RAG
// Description: Handles file system changes and publishes indexing requests.
// =============================================================================
// LOGIC: Bridges the file watcher subsystem with the RAG ingestion pipeline.
//   - Listens to ExternalFileChangesEvent from WorkspaceService.
//   - Filters changes based on FileWatcherOptions (extension, directory rules).
//   - Debounces rapid changes per file using configurable delay.
//   - Publishes FileIndexingRequestedEvent for qualifying changes.
//   - Skips deleted files (handled separately via document removal flow).
// =============================================================================

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Ingestion;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Handles file system changes and publishes indexing requests for the RAG pipeline.
/// </summary>
/// <remarks>
/// <para>
/// This handler subscribes to <see cref="ExternalFileChangesEvent"/> notifications
/// published by the WorkspaceService when file system changes are detected. It
/// filters and debounces changes before publishing <see cref="FileIndexingRequestedEvent"/>
/// notifications for processing by the ingestion pipeline.
/// </para>
/// <para>
/// <b>Filtering Logic:</b>
/// </para>
/// <list type="number">
///   <item><description>Skip if watcher is disabled via <see cref="FileWatcherOptions.Enabled"/>.</description></item>
///   <item><description>Skip directories (only process files).</description></item>
///   <item><description>Skip deleted files (handled by document removal flow).</description></item>
///   <item><description>Check file extension against <see cref="FileWatcherOptions.SupportedExtensions"/>.</description></item>
///   <item><description>Check path segments against <see cref="FileWatcherOptions.ExcludedDirectories"/>.</description></item>
/// </list>
/// <para>
/// <b>Debouncing:</b> Rapid successive changes to the same file are coalesced.
/// When multiple changes arrive within the debounce window, only the latest
/// change type is used when the timer expires.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This handler is thread-safe. The debounce dictionary
/// uses <see cref="ConcurrentDictionary{TKey, TValue}"/> and timer callbacks
/// are properly synchronized.
/// </para>
/// </remarks>
public sealed class FileWatcherIngestionHandler
    : INotificationHandler<ExternalFileChangesEvent>, IDisposable
{
    private readonly IMediator _mediator;
    private readonly FileWatcherOptions _options;
    private readonly ILogger<FileWatcherIngestionHandler> _logger;

    /// <summary>
    /// Pending debounced changes. Key is normalized file path, value is the change info
    /// and the timer handling debounce.
    /// </summary>
    private readonly ConcurrentDictionary<string, DebouncedChange> _pendingChanges = new();

    /// <summary>
    /// Lock object for timer callbacks to prevent race conditions during disposal.
    /// </summary>
    private readonly object _timerLock = new();

    /// <summary>
    /// Flag indicating if the handler has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileWatcherIngestionHandler"/> class.
    /// </summary>
    /// <param name="mediator">MediatR mediator for publishing events.</param>
    /// <param name="options">File watcher configuration options.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="mediator"/>, <paramref name="options"/>,
    /// or <paramref name="logger"/> is null.
    /// </exception>
    public FileWatcherIngestionHandler(
        IMediator mediator,
        IOptions<FileWatcherOptions> options,
        ILogger<FileWatcherIngestionHandler> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug(
            "FileWatcherIngestionHandler initialized. Enabled: {Enabled}, DebounceMs: {DebounceMs}, " +
            "Extensions: [{Extensions}], Excluded: [{Excluded}]",
            _options.Enabled,
            _options.DebounceDelayMs,
            string.Join(", ", _options.SupportedExtensions),
            string.Join(", ", _options.ExcludedDirectories));
    }

    /// <summary>
    /// Handles the <see cref="ExternalFileChangesEvent"/> notification.
    /// </summary>
    /// <param name="notification">The notification containing file system changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    /// <remarks>
    /// This method processes each change in the batch, filtering based on
    /// configuration and queuing valid changes for debounced publishing.
    /// </remarks>
    public Task Handle(ExternalFileChangesEvent notification, CancellationToken cancellationToken)
    {
        // LOGIC: Check if watching is disabled.
        if (!_options.Enabled)
        {
            _logger.LogTrace("File watcher disabled, ignoring {Count} changes", notification.Changes.Count);
            return Task.CompletedTask;
        }

        _logger.LogDebug(
            "Processing {Count} file system changes for potential indexing",
            notification.Changes.Count);

        foreach (var change in notification.Changes)
        {
            ProcessChange(change);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Processes a single file system change.
    /// </summary>
    /// <param name="change">The file system change to process.</param>
    private void ProcessChange(FileSystemChangeInfo change)
    {
        var filePath = change.FullPath;

        // LOGIC: Skip directories - we only index files.
        if (change.IsDirectory)
        {
            _logger.LogTrace("Skipping directory change: {Path}", filePath);
            return;
        }

        // LOGIC: Skip deleted files - handled separately via document removal.
        if (change.ChangeType == FileSystemChangeType.Deleted)
        {
            _logger.LogTrace("Skipping deleted file (handled by removal flow): {Path}", filePath);
            return;
        }

        // LOGIC: Check if file should be processed based on extension and path rules.
        if (!_options.ShouldProcessFile(filePath))
        {
            var extension = Path.GetExtension(filePath);
            _logger.LogTrace(
                "Skipping file: {Path} (extension: {Extension}, not in supported list or in excluded directory)",
                filePath,
                extension);
            return;
        }

        _logger.LogDebug(
            "Queueing file for indexing: {Path} ({ChangeType})",
            filePath,
            change.ChangeType);

        // LOGIC: Debounce the change.
        QueueDebouncedChange(change);
    }

    /// <summary>
    /// Queues a change for debounced publishing.
    /// </summary>
    /// <param name="change">The change to queue.</param>
    private void QueueDebouncedChange(FileSystemChangeInfo change)
    {
        var normalizedPath = NormalizePath(change.FullPath);

        // LOGIC: Try to update existing pending change or add new one.
        _pendingChanges.AddOrUpdate(
            normalizedPath,
            // Add factory: create new debounced change.
            _ => CreateDebouncedChange(change),
            // Update factory: update existing change and reset timer.
            (_, existing) =>
            {
                _logger.LogTrace(
                    "Updating pending change for: {Path} ({OldType} -> {NewType})",
                    normalizedPath,
                    existing.LatestChangeType,
                    change.ChangeType);

                // LOGIC: Update the latest change type and old path.
                existing.LatestChangeType = change.ChangeType;
                existing.OldPath = change.OldPath;

                // LOGIC: Reset the timer.
                existing.Timer.Change(_options.DebounceDelayMs, Timeout.Infinite);

                return existing;
            });
    }

    /// <summary>
    /// Creates a new debounced change with timer.
    /// </summary>
    /// <param name="change">The initial change.</param>
    /// <returns>A new <see cref="DebouncedChange"/> instance.</returns>
    private DebouncedChange CreateDebouncedChange(FileSystemChangeInfo change)
    {
        var normalizedPath = NormalizePath(change.FullPath);

        var debouncedChange = new DebouncedChange
        {
            FilePath = normalizedPath,
            LatestChangeType = change.ChangeType,
            OldPath = change.OldPath,
            Timer = null! // Set below
        };

        // LOGIC: Create timer that fires after debounce delay.
        debouncedChange.Timer = new Timer(
            OnDebounceTimerElapsed,
            normalizedPath,
            _options.DebounceDelayMs,
            Timeout.Infinite);

        _logger.LogTrace(
            "Created debounced change for: {Path}, firing in {Ms}ms",
            normalizedPath,
            _options.DebounceDelayMs);

        return debouncedChange;
    }

    /// <summary>
    /// Handles debounce timer expiration.
    /// </summary>
    /// <param name="state">The normalized file path.</param>
    private async void OnDebounceTimerElapsed(object? state)
    {
        if (state is not string normalizedPath)
        {
            return;
        }

        lock (_timerLock)
        {
            if (_disposed)
            {
                return;
            }
        }

        // LOGIC: Remove from pending and publish event.
        if (_pendingChanges.TryRemove(normalizedPath, out var debouncedChange))
        {
            try
            {
                // LOGIC: Dispose the timer.
                debouncedChange.Timer.Dispose();

                // LOGIC: Create and publish the indexing event.
                var indexingEvent = CreateIndexingEvent(debouncedChange);

                _logger.LogInformation(
                    "Publishing indexing request: {Path} ({ChangeType})",
                    debouncedChange.FilePath,
                    debouncedChange.LatestChangeType);

                await _mediator.Publish(indexingEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error publishing indexing request for: {Path}",
                    normalizedPath);
            }
        }
    }

    /// <summary>
    /// Creates a <see cref="FileIndexingRequestedEvent"/> from a debounced change.
    /// </summary>
    /// <param name="change">The debounced change.</param>
    /// <returns>The indexing request event.</returns>
    private static FileIndexingRequestedEvent CreateIndexingEvent(DebouncedChange change)
    {
        return change.LatestChangeType switch
        {
            FileSystemChangeType.Created => FileIndexingRequestedEvent.ForCreated(change.FilePath),
            FileSystemChangeType.Changed => FileIndexingRequestedEvent.ForChanged(change.FilePath),
            FileSystemChangeType.Renamed => FileIndexingRequestedEvent.ForRenamed(change.FilePath, change.OldPath!),
            // LOGIC: Deleted should never reach here (filtered earlier), but handle defensively.
            _ => throw new InvalidOperationException($"Unexpected change type: {change.LatestChangeType}")
        };
    }

    /// <summary>
    /// Normalizes a file path for consistent dictionary keys.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>Normalized path with forward slashes and lowercase.</returns>
    private static string NormalizePath(string path)
    {
        // LOGIC: Use forward slashes and lowercase for consistent cross-platform comparison.
        return path.Replace('\\', '/').ToLowerInvariant();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_timerLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _logger.LogDebug("Disposing FileWatcherIngestionHandler");

        // LOGIC: Dispose all pending timers.
        foreach (var kvp in _pendingChanges)
        {
            try
            {
                kvp.Value.Timer.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        _pendingChanges.Clear();
    }

    /// <summary>
    /// Represents a debounced file change pending publication.
    /// </summary>
    private sealed class DebouncedChange
    {
        /// <summary>
        /// Gets or sets the normalized file path.
        /// </summary>
        public required string FilePath { get; init; }

        /// <summary>
        /// Gets or sets the latest change type (updated on rapid successive changes).
        /// </summary>
        public FileSystemChangeType LatestChangeType { get; set; }

        /// <summary>
        /// Gets or sets the old path (for rename operations).
        /// </summary>
        public string? OldPath { get; set; }

        /// <summary>
        /// Gets or sets the debounce timer.
        /// </summary>
        public required Timer Timer { get; set; }
    }
}
