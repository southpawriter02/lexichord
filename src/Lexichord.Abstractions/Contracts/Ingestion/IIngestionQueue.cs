// =============================================================================
// File: IIngestionQueue.cs
// Project: Lexichord.Abstractions
// Description: Interface defining the contract for the ingestion queue service.
// =============================================================================
// LOGIC: Central abstraction for the file ingestion queue.
//   - EnqueueAsync adds items with priority support.
//   - DequeueAsync blocks until an item is available.
//   - TryDequeueAsync provides non-blocking dequeue.
//   - Complete signals no more items will be added.
//   - ItemEnqueued event enables monitoring.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Ingestion;

/// <summary>
/// Defines the contract for the ingestion queue service.
/// </summary>
/// <remarks>
/// <para>
/// The ingestion queue provides a thread-safe, priority-based queue for
/// files awaiting ingestion into the RAG system. Items are processed
/// in priority order (lowest value first), with FIFO ordering within
/// the same priority level.
/// </para>
/// <para>
/// <b>Thread Safety:</b> All members of this interface are thread-safe.
/// Multiple producers can enqueue items concurrently, and multiple
/// consumers can dequeue items concurrently.
/// </para>
/// <para>
/// <b>Backpressure:</b> When the queue reaches its configured capacity,
/// <see cref="EnqueueAsync"/> will wait until space is available. This
/// provides natural backpressure to prevent unbounded memory growth.
/// </para>
/// <para>
/// <b>Completion:</b> Call <see cref="Complete"/> to signal that no more
/// items will be added. After completion, <see cref="DequeueAsync"/> will
/// return all remaining items and then throw <see cref="ChannelClosedException"/>
/// when the queue is empty.
/// </para>
/// </remarks>
public interface IIngestionQueue
{
    /// <summary>
    /// Occurs when an item is successfully enqueued.
    /// </summary>
    /// <remarks>
    /// This event is raised after the item is added to the queue.
    /// Subscribers can use this for monitoring queue activity.
    /// The event is raised on the thread that called <see cref="EnqueueAsync"/>.
    /// </remarks>
    event EventHandler<IngestionQueueItem>? ItemEnqueued;

    /// <summary>
    /// Occurs when an item is successfully dequeued for processing.
    /// </summary>
    /// <remarks>
    /// This event is raised when an item is removed from the queue.
    /// Subscribers can use this for monitoring processing activity.
    /// </remarks>
    event EventHandler<IngestionQueueItem>? ItemDequeued;

    /// <summary>
    /// Gets the current number of items in the queue.
    /// </summary>
    /// <remarks>
    /// This value is approximate in concurrent scenarios as items may
    /// be enqueued or dequeued between reading this property and
    /// performing subsequent operations.
    /// </remarks>
    int Count { get; }

    /// <summary>
    /// Gets a value indicating whether the queue is empty.
    /// </summary>
    /// <remarks>
    /// This value is approximate in concurrent scenarios. Use
    /// <see cref="TryDequeueAsync"/> for atomic check-and-dequeue.
    /// </remarks>
    bool IsEmpty { get; }

    /// <summary>
    /// Gets a value indicating whether the queue has been completed.
    /// </summary>
    /// <remarks>
    /// Once completed, no more items can be enqueued. The queue may
    /// still contain items that need to be processed.
    /// </remarks>
    bool IsCompleted { get; }

    /// <summary>
    /// Adds an item to the queue.
    /// </summary>
    /// <param name="item">The item to enqueue.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the item was enqueued; <c>false</c> if it was
    /// skipped due to duplicate detection.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="item"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the queue has been completed via <see cref="Complete"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If duplicate detection is enabled and a file with the same path
    /// was recently enqueued, this method returns <c>false</c> and the
    /// item is not added to the queue.
    /// </para>
    /// <para>
    /// If the queue is at capacity, this method will wait until space
    /// is available or the cancellation token is signalled.
    /// </para>
    /// </remarks>
    Task<bool> EnqueueAsync(IngestionQueueItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a file to the queue using the factory method.
    /// </summary>
    /// <param name="projectId">The project to associate the document with.</param>
    /// <param name="filePath">Absolute path to the file to ingest.</param>
    /// <param name="priority">Processing priority (default: normal).</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the item was enqueued; <c>false</c> if skipped.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="filePath"/> is null or whitespace.
    /// </exception>
    /// <remarks>
    /// Convenience overload that creates an <see cref="IngestionQueueItem"/>
    /// using <see cref="IngestionQueueItem.Create"/>.
    /// </remarks>
    Task<bool> EnqueueAsync(
        Guid projectId,
        string filePath,
        int priority = IngestionQueueItem.PriorityNormal,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes and returns the next item from the queue.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The next item in priority order.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    /// <exception cref="ChannelClosedException">
    /// Thrown when the queue has been completed and is empty.
    /// </exception>
    /// <remarks>
    /// This method blocks until an item is available. For non-blocking
    /// behavior, use <see cref="TryDequeueAsync"/>.
    /// </remarks>
    Task<IngestionQueueItem> DequeueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to remove and return the next item from the queue.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The next item if available; <c>null</c> if the queue is empty.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    /// <remarks>
    /// This method returns immediately if no item is available.
    /// </remarks>
    Task<IngestionQueueItem?> TryDequeueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Signals that no more items will be added to the queue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After calling this method, <see cref="EnqueueAsync(IngestionQueueItem, CancellationToken)"/>
    /// will throw <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// Consumers should continue to dequeue until the queue is empty.
    /// After that, <see cref="DequeueAsync"/> will throw
    /// <see cref="ChannelClosedException"/>.
    /// </para>
    /// </remarks>
    void Complete();

    /// <summary>
    /// Clears all pending items from the queue.
    /// </summary>
    /// <returns>The number of items that were removed.</returns>
    /// <remarks>
    /// This method is useful for graceful shutdown or queue reset scenarios.
    /// Items that are currently being processed are not affected.
    /// </remarks>
    int Clear();
}
