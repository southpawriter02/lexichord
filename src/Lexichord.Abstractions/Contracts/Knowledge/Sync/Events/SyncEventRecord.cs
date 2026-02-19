// =============================================================================
// File: SyncEventRecord.cs
// Project: Lexichord.Abstractions
// Description: Historical record of a published sync event for audit trails.
// =============================================================================
// LOGIC: SyncEventRecord captures the complete state of a published event
//   including execution metrics (handlers executed/failed, duration) and
//   serialized payload. Used by IEventStore for persistence and queries.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Historical record of a published sync event.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Provides a complete audit trail for published sync events,
/// including the serialized payload and handler execution metrics.
/// </para>
/// <para>
/// <b>Storage:</b> Records are persisted by <see cref="IEventStore"/> and
/// queryable via <see cref="ISyncEventPublisher.GetEventsAsync"/>.
/// </para>
/// <para>
/// <b>Retention:</b> Record retention is license-tier dependent:
/// </para>
/// <list type="bullet">
///   <item>WriterPro: 7 days</item>
///   <item>Teams: 30 days</item>
///   <item>Enterprise: Unlimited</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
public record SyncEventRecord
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    /// <value>The event's unique identifier.</value>
    /// <remarks>
    /// LOGIC: Matches <see cref="ISyncEvent.EventId"/> of the published event.
    /// Used as primary key for event lookup.
    /// </remarks>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Type name of the event.
    /// </summary>
    /// <value>The fully qualified or simple type name of the event.</value>
    /// <remarks>
    /// LOGIC: Used for event type filtering in queries.
    /// Example: "SyncCompletedEvent", "SyncFailedEvent".
    /// </remarks>
    public required string EventType { get; init; }

    /// <summary>
    /// ID of the document involved in the event.
    /// </summary>
    /// <value>The unique identifier of the affected document.</value>
    /// <remarks>
    /// LOGIC: Enables document-centric event queries.
    /// Matches <see cref="ISyncEvent.DocumentId"/>.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// When the event was published.
    /// </summary>
    /// <value>UTC timestamp of event publication.</value>
    /// <remarks>
    /// LOGIC: Enables time-based event filtering and ordering.
    /// Matches <see cref="ISyncEvent.PublishedAt"/>.
    /// </remarks>
    public required DateTimeOffset PublishedAt { get; init; }

    /// <summary>
    /// Serialized event payload.
    /// </summary>
    /// <value>JSON-serialized event data.</value>
    /// <remarks>
    /// LOGIC: Enables event replay and debugging.
    /// Serialized using System.Text.Json for consistency.
    /// </remarks>
    public required string Payload { get; init; }

    /// <summary>
    /// Number of handlers that executed for this event.
    /// </summary>
    /// <value>Count of handlers that ran.</value>
    /// <remarks>
    /// LOGIC: Indicates event processing scope.
    /// Zero handlers may indicate configuration issues.
    /// </remarks>
    public int HandlersExecuted { get; init; }

    /// <summary>
    /// Number of handlers that failed during execution.
    /// </summary>
    /// <value>Count of handlers that threw exceptions.</value>
    /// <remarks>
    /// LOGIC: Non-zero value indicates partial failure.
    /// Check <see cref="HandlerErrors"/> for details.
    /// </remarks>
    public int HandlersFailed { get; init; }

    /// <summary>
    /// Total time to process the event across all handlers.
    /// </summary>
    /// <value>Duration from publish start to completion.</value>
    /// <remarks>
    /// LOGIC: Used for performance monitoring.
    /// Target: &lt; 200ms per handler.
    /// </remarks>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Whether all handlers succeeded.
    /// </summary>
    /// <value>True if no handlers failed.</value>
    /// <remarks>
    /// LOGIC: Quick success check without inspecting counts.
    /// Equivalent to <c>HandlersFailed == 0</c>.
    /// </remarks>
    public bool AllHandlersSucceeded { get; init; }

    /// <summary>
    /// Error details from failed handlers.
    /// </summary>
    /// <value>List of error messages from failed handlers.</value>
    /// <remarks>
    /// LOGIC: Provides diagnostic information for troubleshooting.
    /// Empty when <see cref="AllHandlersSucceeded"/> is true.
    /// </remarks>
    public IReadOnlyList<string> HandlerErrors { get; init; } = [];
}
