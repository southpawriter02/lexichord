using Lexichord.Abstractions.Messaging;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Base record for all domain events providing common metadata.
/// </summary>
/// <remarks>
/// LOGIC: This abstract record provides consistent event properties across
/// all domain events. Using a record ensures:
///
/// 1. **Immutability**: Events represent something that HAS happened and
///    cannot be changed.
///
/// 2. **Value Equality**: Two events with the same data are considered equal.
///
/// 3. **Easy Cloning**: With-expressions allow creating modified copies.
///
/// All domain events should inherit from this base to ensure consistent
/// metadata for logging, tracing, and event sourcing scenarios.
/// </remarks>
public abstract record DomainEventBase : IDomainEvent
{
    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Auto-generated on creation. Used for:
    /// - Event deduplication (idempotency)
    /// - Event sourcing/replay identification
    /// - Distributed tracing correlation
    /// </remarks>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: UTC timestamp of when the state change occurred.
    /// This is NOT the time the event was processed or published.
    /// </remarks>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Flows from the originating request to enable distributed
    /// tracing. Should be set from the incoming request's correlation ID.
    /// </remarks>
    public string? CorrelationId { get; init; }
}
