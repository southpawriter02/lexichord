// =============================================================================
// File: ISyncEventPublisher.cs
// Project: Lexichord.Abstractions
// Description: Interface for publishing sync events via MediatR.
// =============================================================================
// LOGIC: ISyncEventPublisher provides a unified API for publishing sync events,
//   managing subscriptions, and querying event history. Wraps MediatR with
//   additional features: history storage, batching, deduplication, subscriptions.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: ISyncEvent, SyncEventOptions, SyncEventSubscriptionOptions,
//               SyncEventQuery, SyncEventRecord (v0.7.6j)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Interface for publishing sync events via MediatR.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Provides a unified API for publishing sync events,
/// wrapping MediatR with additional features:
/// </para>
/// <list type="bullet">
///   <item>Event history storage via <see cref="IEventStore"/>.</item>
///   <item>Batch publishing with deduplication.</item>
///   <item>Dynamic subscriptions with filtering.</item>
///   <item>License-gated access by tier.</item>
/// </list>
/// <para>
/// <b>License Requirements:</b>
/// </para>
/// <list type="bullet">
///   <item>Core: No access to event publishing.</item>
///   <item>WriterPro: Publish events, 7-day history.</item>
///   <item>Teams: Full access, 30-day history, subscriptions, batching.</item>
///   <item>Enterprise: Unlimited history, advanced features.</item>
/// </list>
/// <para>
/// <b>Performance Targets:</b>
/// </para>
/// <list type="bullet">
///   <item>Event publication: &lt; 50ms</item>
///   <item>Handler invocation: &lt; 200ms per handler</item>
///   <item>Batch publication (100 events): &lt; 500ms</item>
///   <item>Event query (1000 events): &lt; 300ms</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Publish a single event
/// var event = SyncCompletedEvent.Create(documentId, result, direction);
/// await publisher.PublishAsync(event, new SyncEventOptions { Priority = EventPriority.High });
///
/// // Query event history
/// var query = new SyncEventQuery { DocumentId = documentId, PageSize = 50 };
/// var events = await publisher.GetEventsAsync(query);
/// </code>
/// </example>
public interface ISyncEventPublisher
{
    /// <summary>
    /// Publishes a sync event to all registered handlers.
    /// </summary>
    /// <typeparam name="TEvent">The type of sync event to publish.</typeparam>
    /// <param name="eventData">The event data to publish.</param>
    /// <param name="options">Optional publication options.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// LOGIC: Publishes the event via MediatR and optionally stores it in history.
    /// Handler exceptions are caught or propagated based on <see cref="SyncEventOptions.CatchHandlerExceptions"/>.
    /// </remarks>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the current license tier does not support event publishing.
    /// </exception>
    Task PublishAsync<TEvent>(
        TEvent eventData,
        SyncEventOptions? options = null,
        CancellationToken ct = default)
        where TEvent : ISyncEvent;

    /// <summary>
    /// Publishes multiple sync events in a batch.
    /// </summary>
    /// <typeparam name="TEvent">The type of sync events to publish.</typeparam>
    /// <param name="events">The events to publish.</param>
    /// <param name="options">Optional publication options applied to all events.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// LOGIC: Publishes events sequentially, with optional deduplication based on EventId.
    /// Batch operations require Teams tier or higher.
    /// </remarks>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the current license tier does not support batch publishing.
    /// </exception>
    Task PublishBatchAsync<TEvent>(
        IReadOnlyList<TEvent> events,
        SyncEventOptions? options = null,
        CancellationToken ct = default)
        where TEvent : ISyncEvent;

    /// <summary>
    /// Subscribes to sync events of a specific type.
    /// </summary>
    /// <typeparam name="TEvent">The type of sync event to subscribe to.</typeparam>
    /// <param name="handler">The handler function to invoke for each event.</param>
    /// <param name="options">Optional subscription options including filters.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A subscription ID that can be used to unsubscribe later.
    /// </returns>
    /// <remarks>
    /// LOGIC: Creates a dynamic subscription in addition to MediatR handlers.
    /// Subscriptions require Teams tier or higher.
    /// </remarks>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the current license tier does not support subscriptions.
    /// </exception>
    Task<Guid> SubscribeAsync<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        SyncEventSubscriptionOptions? options = null,
        CancellationToken ct = default)
        where TEvent : ISyncEvent;

    /// <summary>
    /// Unsubscribes from sync events.
    /// </summary>
    /// <typeparam name="TEvent">The type of sync event to unsubscribe from.</typeparam>
    /// <param name="subscriptionId">The subscription ID returned by <see cref="SubscribeAsync{TEvent}"/>.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// True if the subscription was found and removed, false otherwise.
    /// </returns>
    /// <remarks>
    /// LOGIC: Removes a previously created subscription.
    /// Returns false if the subscription ID is not found.
    /// </remarks>
    Task<bool> UnsubscribeAsync<TEvent>(
        Guid subscriptionId,
        CancellationToken ct = default)
        where TEvent : ISyncEvent;

    /// <summary>
    /// Gets published events from the event store.
    /// </summary>
    /// <param name="query">The query criteria including filters, sorting, and pagination.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A list of matching event records.
    /// </returns>
    /// <remarks>
    /// LOGIC: Queries the event store for historical events.
    /// Results are limited by license tier retention policy.
    /// </remarks>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the current license tier does not support event history queries.
    /// </exception>
    Task<IReadOnlyList<SyncEventRecord>> GetEventsAsync(
        SyncEventQuery query,
        CancellationToken ct = default);
}
