// =============================================================================
// File: SyncEventOptions.cs
// Project: Lexichord.Abstractions
// Description: Options for publishing sync events.
// =============================================================================
// LOGIC: SyncEventOptions configures how events are published and handled,
//   including history storage, error handling, timeout, priority, and batching.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: EventPriority (v0.7.6j)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Options for publishing sync events.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Configures event publication behavior including history
/// storage, handler execution, and error handling.
/// </para>
/// <para>
/// <b>Usage:</b> Passed to <see cref="ISyncEventPublisher.PublishAsync{TEvent}"/>
/// to customize publication behavior per event.
/// </para>
/// <para>
/// <b>Defaults:</b> The default options store events in history, await all
/// handlers, catch handler exceptions, and use normal priority.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new SyncEventOptions
/// {
///     StoreInHistory = true,
///     Priority = EventPriority.High,
///     Tags = ["critical", "user-action"]
/// };
/// await publisher.PublishAsync(myEvent, options);
/// </code>
/// </example>
public record SyncEventOptions
{
    /// <summary>
    /// Whether to store the event in history.
    /// </summary>
    /// <value>True to persist event to <see cref="IEventStore"/>.</value>
    /// <remarks>
    /// LOGIC: When true, event is recorded for audit trail and queries.
    /// Disable for high-frequency, low-value events to reduce storage.
    /// Default: true.
    /// </remarks>
    public bool StoreInHistory { get; init; } = true;

    /// <summary>
    /// Whether to await all handlers before returning.
    /// </summary>
    /// <value>True to wait for all handlers to complete.</value>
    /// <remarks>
    /// LOGIC: When true, PublishAsync blocks until all handlers finish.
    /// When false, handlers may complete after method returns.
    /// Default: true for reliable completion tracking.
    /// </remarks>
    public bool AwaitAll { get; init; } = true;

    /// <summary>
    /// Whether to catch handler exceptions.
    /// </summary>
    /// <value>True to catch and log handler exceptions.</value>
    /// <remarks>
    /// LOGIC: When true, handler exceptions are logged but not propagated.
    /// When false, first handler exception propagates to caller.
    /// Default: true for resilient event processing.
    /// </remarks>
    public bool CatchHandlerExceptions { get; init; } = true;

    /// <summary>
    /// Timeout for handler execution.
    /// </summary>
    /// <value>Maximum time to wait for handlers, or null for no timeout.</value>
    /// <remarks>
    /// LOGIC: When set, handlers exceeding timeout are cancelled.
    /// Used to prevent runaway handlers from blocking event processing.
    /// Default: null (no timeout).
    /// </remarks>
    public TimeSpan? HandlerTimeout { get; init; }

    /// <summary>
    /// Priority for event processing.
    /// </summary>
    /// <value>The event's processing priority.</value>
    /// <remarks>
    /// LOGIC: Higher priority events are processed before lower priority
    /// events when batching is enabled.
    /// Default: <see cref="EventPriority.Normal"/>.
    /// </remarks>
    public EventPriority Priority { get; init; } = EventPriority.Normal;

    /// <summary>
    /// Whether to allow batching with other events.
    /// </summary>
    /// <value>True to allow combining with other events in a batch.</value>
    /// <remarks>
    /// LOGIC: When true, event may be combined with other events for
    /// efficient processing. When false, event is processed individually.
    /// Default: false for consistent behavior.
    /// </remarks>
    public bool AllowBatching { get; init; } = false;

    /// <summary>
    /// Tags for categorizing the event.
    /// </summary>
    /// <value>A list of string tags for event classification.</value>
    /// <remarks>
    /// LOGIC: Tags enable event filtering and categorization beyond type.
    /// Example: ["user-action", "critical", "document-sync"].
    /// Default: empty list.
    /// </remarks>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Additional context data for handlers.
    /// </summary>
    /// <value>A dictionary of key-value pairs for handler context.</value>
    /// <remarks>
    /// LOGIC: Provides additional context to handlers without modifying events.
    /// Example: correlation IDs, user context, request metadata.
    /// Default: empty dictionary.
    /// </remarks>
    public Dictionary<string, object> ContextData { get; init; } = new();

    /// <summary>
    /// Default options instance with all defaults applied.
    /// </summary>
    /// <remarks>
    /// LOGIC: Convenience accessor for default options.
    /// Use when no customization is needed.
    /// </remarks>
    public static SyncEventOptions Default { get; } = new();
}
