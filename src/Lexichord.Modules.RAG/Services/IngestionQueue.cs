// =============================================================================
// File: IngestionQueue.cs
// Project: Lexichord.Modules.RAG
// Description: Channel-based implementation of the ingestion queue.
// =============================================================================
// LOGIC: Thread-safe priority queue using System.Threading.Channels.
//   - Uses unbounded channel internally with manual capacity tracking.
//   - Priority ordering via PriorityQueue with sorted dequeue.
//   - Duplicate detection using ConcurrentDictionary with timestamp tracking.
//   - Cleanup of stale duplicate entries via periodic maintenance.
// =============================================================================

using System.Collections.Concurrent;
using System.Threading.Channels;
using Lexichord.Abstractions.Contracts.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Channel-based implementation of the ingestion queue with priority support.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses <see cref="System.Threading.Channels"/> for thread-safe
/// producer-consumer patterns combined with <see cref="PriorityQueue{TElement, TPriority}"/>
/// for priority-based ordering.
/// </para>
/// <para>
/// <b>Architecture:</b> Items are written to a channel for thread-safe transfer,
/// then stored in a priority queue. The reader drains the channel into the priority
/// queue before returning the highest-priority item.
/// </para>
/// <para>
/// <b>Duplicate Detection:</b> When enabled, recently enqueued file paths are tracked
/// in a <see cref="ConcurrentDictionary{TKey, TValue}"/>. Paths enqueued within the
/// configured window are silently skipped with a <c>false</c> return value.
/// </para>
/// <para>
/// <b>Thread Safety:</b> All public members are thread-safe. The implementation uses
/// channels for synchronization and lock-free data structures where possible.
/// </para>
/// </remarks>
public sealed class IngestionQueue : IIngestionQueue, IDisposable
{
    private readonly Channel<IngestionQueueItem> _channel;
    private readonly PriorityQueue<IngestionQueueItem, (int Priority, long Ticks)> _priorityQueue;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _recentPaths;
    private readonly IngestionQueueOptions _options;
    private readonly ILogger<IngestionQueue> _logger;
    private readonly object _priorityQueueLock = new();
    private readonly Timer? _cleanupTimer;
    private volatile bool _isCompleted;
    private volatile bool _isDisposed;

    /// <inheritdoc />
    public event EventHandler<IngestionQueueItem>? ItemEnqueued;

    /// <inheritdoc />
    public event EventHandler<IngestionQueueItem>? ItemDequeued;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionQueue"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the queue.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="logger"/> is null.
    /// </exception>
    public IngestionQueue(
        IOptions<IngestionQueueOptions> options,
        ILogger<IngestionQueue> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _options.Validate();
        _logger = logger;

        // LOGIC: Use bounded channel for backpressure.
        // BoundedChannelFullMode.Wait causes EnqueueAsync to wait when full.
        var channelOptions = new BoundedChannelOptions(_options.MaxQueueSize)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<IngestionQueueItem>(channelOptions);

        // LOGIC: Priority queue orders by (Priority, Ticks) where lower is better.
        // Ticks ensures FIFO within same priority level.
        _priorityQueue = new PriorityQueue<IngestionQueueItem, (int Priority, long Ticks)>();

        // LOGIC: Track recent paths for duplicate detection.
        _recentPaths = new ConcurrentDictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);

        // LOGIC: Set up periodic cleanup of stale duplicate entries.
        if (_options.EnableDuplicateDetection)
        {
            var cleanupInterval = TimeSpan.FromSeconds(_options.DuplicateWindowSeconds);
            _cleanupTimer = new Timer(
                CleanupStaleEntries,
                null,
                cleanupInterval,
                cleanupInterval);
        }

        _logger.LogInformation(
            "IngestionQueue initialized with MaxQueueSize={MaxQueueSize}, ThrottleDelayMs={ThrottleDelayMs}, " +
            "DuplicateDetection={DuplicateDetection}, DuplicateWindowSeconds={DuplicateWindowSeconds}",
            _options.MaxQueueSize,
            _options.ThrottleDelayMs,
            _options.EnableDuplicateDetection,
            _options.DuplicateWindowSeconds);
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_priorityQueueLock)
            {
                return _priorityQueue.Count + _channel.Reader.Count;
            }
        }
    }

    /// <inheritdoc />
    public bool IsEmpty => Count == 0;

    /// <inheritdoc />
    public bool IsCompleted => _isCompleted;

    /// <inheritdoc />
    public async Task<bool> EnqueueAsync(IngestionQueueItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ThrowIfDisposed();

        if (_isCompleted)
        {
            throw new InvalidOperationException("Cannot enqueue items after the queue has been completed.");
        }

        // LOGIC: Check for duplicates if enabled.
        if (_options.EnableDuplicateDetection)
        {
            var normalizedPath = NormalizePath(item.FilePath);
            var now = DateTimeOffset.UtcNow;

            if (_recentPaths.TryGetValue(normalizedPath, out var lastEnqueued))
            {
                var elapsed = now - lastEnqueued;
                if (elapsed.TotalSeconds < _options.DuplicateWindowSeconds)
                {
                    _logger.LogDebug(
                        "Skipping duplicate enqueue for {FilePath} (last enqueued {ElapsedSeconds:F1}s ago)",
                        item.FilePath,
                        elapsed.TotalSeconds);
                    return false;
                }
            }

            // LOGIC: Update the timestamp for this path.
            _recentPaths[normalizedPath] = now;
        }

        // LOGIC: Write to channel (may block if at capacity).
        await _channel.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Enqueued item {Id} for {FilePath} with priority {Priority}",
            item.Id,
            item.FilePath,
            item.Priority);

        // LOGIC: Raise event for monitoring.
        ItemEnqueued?.Invoke(this, item);

        return true;
    }

    /// <inheritdoc />
    public Task<bool> EnqueueAsync(
        Guid projectId,
        string filePath,
        int priority = IngestionQueueItem.PriorityNormal,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var item = IngestionQueueItem.Create(projectId, filePath, priority, correlationId);
        return EnqueueAsync(item, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IngestionQueueItem> DequeueAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        while (true)
        {
            // LOGIC: First, drain any items from the channel into the priority queue.
            DrainChannelToPriorityQueue();

            // LOGIC: Try to get the highest-priority item.
            lock (_priorityQueueLock)
            {
                if (_priorityQueue.TryDequeue(out var item, out _))
                {
                    _logger.LogDebug(
                        "Dequeued item {Id} for {FilePath} with priority {Priority}",
                        item.Id,
                        item.FilePath,
                        item.Priority);

                    ItemDequeued?.Invoke(this, item);
                    return item;
                }
            }

            // LOGIC: No items available - wait for the next one.
            if (_isCompleted)
            {
                throw new ChannelClosedException("The queue has been completed and is empty.");
            }

            // LOGIC: Wait for an item to be available.
            if (!await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new ChannelClosedException("The queue has been completed and is empty.");
            }
        }
    }

    /// <inheritdoc />
    public Task<IngestionQueueItem?> TryDequeueAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        // LOGIC: Drain any pending items from the channel.
        DrainChannelToPriorityQueue();

        // LOGIC: Try to get the highest-priority item.
        lock (_priorityQueueLock)
        {
            if (_priorityQueue.TryDequeue(out var item, out _))
            {
                _logger.LogDebug(
                    "TryDequeue succeeded for item {Id} ({FilePath})",
                    item.Id,
                    item.FilePath);

                ItemDequeued?.Invoke(this, item);
                return Task.FromResult<IngestionQueueItem?>(item);
            }
        }

        _logger.LogDebug("TryDequeue returned null (queue empty)");
        return Task.FromResult<IngestionQueueItem?>(null);
    }

    /// <inheritdoc />
    public void Complete()
    {
        if (_isCompleted)
        {
            return;
        }

        _isCompleted = true;
        _channel.Writer.Complete();

        _logger.LogInformation(
            "IngestionQueue completed with {Count} items remaining",
            Count);
    }

    /// <inheritdoc />
    public int Clear()
    {
        ThrowIfDisposed();

        var clearedCount = 0;

        // LOGIC: Clear the priority queue.
        lock (_priorityQueueLock)
        {
            clearedCount = _priorityQueue.Count;
            _priorityQueue.Clear();
        }

        // LOGIC: Drain and discard channel items.
        while (_channel.Reader.TryRead(out _))
        {
            clearedCount++;
        }

        // LOGIC: Clear duplicate tracking.
        _recentPaths.Clear();

        _logger.LogInformation("Cleared {Count} items from the queue", clearedCount);

        return clearedCount;
    }

    /// <summary>
    /// Drains all available items from the channel into the priority queue.
    /// </summary>
    private void DrainChannelToPriorityQueue()
    {
        while (_channel.Reader.TryRead(out var item))
        {
            lock (_priorityQueueLock)
            {
                // LOGIC: Use (Priority, Ticks) as the priority key.
                // Lower priority value = higher priority.
                // Earlier ticks = higher priority within same level.
                _priorityQueue.Enqueue(item, (item.Priority, item.EnqueuedAt.Ticks));
            }
        }
    }

    /// <summary>
    /// Cleans up stale entries from the duplicate detection dictionary.
    /// </summary>
    /// <param name="state">Timer state (unused).</param>
    private void CleanupStaleEntries(object? state)
    {
        if (_isDisposed)
        {
            return;
        }

        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-_options.DuplicateWindowSeconds);
        var staleCount = 0;

        foreach (var kvp in _recentPaths)
        {
            if (kvp.Value < cutoff)
            {
                if (_recentPaths.TryRemove(kvp.Key, out _))
                {
                    staleCount++;
                }
            }
        }

        if (staleCount > 0)
        {
            _logger.LogDebug("Cleaned up {Count} stale duplicate detection entries", staleCount);
        }
    }

    /// <summary>
    /// Normalizes a file path for duplicate detection.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>Normalized path string.</returns>
    private static string NormalizePath(string path)
    {
        // LOGIC: Normalize path separators and case for consistent comparison.
        return path.Replace('\\', '/').ToLowerInvariant();
    }

    /// <summary>
    /// Throws if the object has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(IngestionQueue));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        _cleanupTimer?.Dispose();

        // LOGIC: Complete the channel if not already done.
        if (!_isCompleted)
        {
            Complete();
        }

        _logger.LogDebug("IngestionQueue disposed");
    }
}
