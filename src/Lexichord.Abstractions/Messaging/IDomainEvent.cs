namespace Lexichord.Abstractions.Messaging;

/// <summary>
/// Marker interface for domain events that notify of state changes.
/// </summary>
/// <remarks>
/// LOGIC: Events represent something that HAS HAPPENED. Key characteristics:
///
/// 1. **Multiple Handlers**: Events can have ZERO or MORE handlers.
///    Unlike commands/queries, MediatR does not require a handler.
///
/// 2. **Fire and Forget**: Publishers do not receive a return value.
///    Handlers execute asynchronously.
///
/// 3. **Immutability**: Events should be immutable records of past state changes.
///
/// 4. **Dispatch**: Events are published via IMediator.Publish().
///
/// Example:
/// <code>
/// public record DocumentCreatedEvent : IDomainEvent
/// {
///     public Guid EventId { get; init; }
///     public DateTimeOffset OccurredAt { get; init; }
///     public string? CorrelationId { get; init; }
///     public DocumentId DocumentId { get; init; }
///     public string Title { get; init; }
/// }
/// </code>
/// </remarks>
public interface IDomainEvent : MediatR.INotification
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    /// <remarks>
    /// LOGIC: Useful for event deduplication in distributed scenarios
    /// and for event sourcing/replay.
    /// </remarks>
    Guid EventId { get; }

    /// <summary>
    /// UTC timestamp when this event occurred.
    /// </summary>
    /// <remarks>
    /// LOGIC: Use DateTimeOffset for timezone-aware timestamps.
    /// This represents the actual time of the state change, not
    /// the time the event was processed.
    /// </remarks>
    DateTimeOffset OccurredAt { get; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    /// <remarks>
    /// LOGIC: Links related events and operations across service boundaries.
    /// Should flow from the originating request through all downstream events.
    /// </remarks>
    string? CorrelationId { get; }
}
