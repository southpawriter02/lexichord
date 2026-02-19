// =============================================================================
// File: ISyncEvent.cs
// Project: Lexichord.Abstractions
// Description: Base interface for all synchronization events.
// =============================================================================
// LOGIC: ISyncEvent extends MediatR's INotification to provide a common contract
//   for all sync-related events. This enables:
//   - Type-safe event publishing via ISyncEventPublisher
//   - Consistent event metadata across all sync events
//   - Event history recording with common properties
//   - Event filtering and subscription management
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: MediatR (INotification)
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Base interface for all synchronization events.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Provides a common contract for all sync-related events,
/// enabling consistent handling by <see cref="ISyncEventPublisher"/> and
/// <see cref="IEventStore"/>.
/// </para>
/// <para>
/// <b>Implementation:</b> All sync events must implement this interface.
/// Implementations should use C# records with <c>init</c> properties and
/// provide a static <c>Create</c> factory method.
/// </para>
/// <para>
/// <b>Properties:</b>
/// </para>
/// <list type="bullet">
///   <item><see cref="EventId"/>: Unique identifier for event deduplication and tracking.</item>
///   <item><see cref="PublishedAt"/>: Timestamp for ordering and audit trails.</item>
///   <item><see cref="DocumentId"/>: Links event to the affected document.</item>
///   <item><see cref="Metadata"/>: Extensible key-value pairs for context.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// public record MySyncEvent : ISyncEvent
/// {
///     public Guid EventId { get; init; } = Guid.NewGuid();
///     public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;
///     public required Guid DocumentId { get; init; }
///     public IReadOnlyDictionary&lt;string, object&gt; Metadata { get; init; } =
///         new Dictionary&lt;string, object&gt;();
///
///     // Event-specific properties...
///
///     public static MySyncEvent Create(Guid documentId) =&gt; new()
///     {
///         DocumentId = documentId
///     };
/// }
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
public interface ISyncEvent : INotification
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    /// <value>A globally unique identifier for the event.</value>
    /// <remarks>
    /// LOGIC: Generated at event creation time. Used for:
    /// - Event deduplication in batch publishing
    /// - Event tracking across handlers
    /// - Event history queries
    /// Implementations should default to <c>Guid.NewGuid()</c>.
    /// </remarks>
    Guid EventId { get; }

    /// <summary>
    /// Timestamp when the event was published.
    /// </summary>
    /// <value>UTC timestamp of event publication.</value>
    /// <remarks>
    /// LOGIC: Recorded at event creation for:
    /// - Event ordering in history queries
    /// - Audit trail compliance
    /// - Time-based event filtering
    /// Implementations should default to <c>DateTimeOffset.UtcNow</c>.
    /// </remarks>
    DateTimeOffset PublishedAt { get; }

    /// <summary>
    /// ID of the document involved in the sync event.
    /// </summary>
    /// <value>The unique identifier of the affected document.</value>
    /// <remarks>
    /// LOGIC: Links the event to a specific document for:
    /// - Document-centric event queries
    /// - Handler filtering by document
    /// - Subscription routing
    /// </remarks>
    Guid DocumentId { get; }

    /// <summary>
    /// Extensible metadata for event context.
    /// </summary>
    /// <value>A read-only dictionary of key-value pairs.</value>
    /// <remarks>
    /// LOGIC: Provides extensible context without modifying event types:
    /// - Correlation IDs for distributed tracing
    /// - User context for audit
    /// - Custom handler-specific data
    /// Implementations should default to an empty dictionary.
    /// </remarks>
    IReadOnlyDictionary<string, object> Metadata { get; }
}
