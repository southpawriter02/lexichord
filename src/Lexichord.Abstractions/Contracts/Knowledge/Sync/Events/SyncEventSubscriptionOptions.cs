// =============================================================================
// File: SyncEventSubscriptionOptions.cs
// Project: Lexichord.Abstractions
// Description: Options for subscribing to sync events.
// =============================================================================
// LOGIC: SyncEventSubscriptionOptions configures event subscription behavior
//   including filtering, activation state, and metadata for management.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: ISyncEvent (v0.7.6j)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Options for subscribing to sync events.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Configures event subscription behavior including event
/// filtering, activation state, and subscription metadata.
/// </para>
/// <para>
/// <b>Usage:</b> Passed to <see cref="ISyncEventPublisher.SubscribeAsync{TEvent}"/>
/// to customize subscription behavior.
/// </para>
/// <para>
/// <b>Filtering:</b> The <see cref="Filter"/> predicate enables selective
/// event handling based on event properties.
/// </para>
/// <para>
/// <b>License Requirement:</b> Subscription features require Teams tier or higher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new SyncEventSubscriptionOptions
/// {
///     EventType = typeof(SyncCompletedEvent),
///     Filter = e => e.DocumentId == targetDocumentId,
///     Name = "Document-specific sync handler"
/// };
/// var subscriptionId = await publisher.SubscribeAsync&lt;SyncCompletedEvent&gt;(
///     handler, options);
/// </code>
/// </example>
public record SyncEventSubscriptionOptions
{
    /// <summary>
    /// Type of event to subscribe to.
    /// </summary>
    /// <value>The event type to handle.</value>
    /// <remarks>
    /// LOGIC: Specifies which event type triggers the subscription.
    /// Must implement <see cref="ISyncEvent"/>.
    /// </remarks>
    public required Type EventType { get; init; }

    /// <summary>
    /// Filter predicate for events.
    /// </summary>
    /// <value>
    /// A predicate that returns true for events to process,
    /// or null to process all events of the subscribed type.
    /// </value>
    /// <remarks>
    /// LOGIC: Enables selective event handling without creating multiple subscriptions.
    /// Example: filter by document ID, user ID, or event properties.
    /// Default: null (no filtering, handle all events).
    /// </remarks>
    public Func<ISyncEvent, bool>? Filter { get; init; }

    /// <summary>
    /// Whether the subscription is active.
    /// </summary>
    /// <value>True if subscription should receive events.</value>
    /// <remarks>
    /// LOGIC: Enables temporarily disabling subscriptions without removing them.
    /// Inactive subscriptions remain registered but do not receive events.
    /// Default: true.
    /// </remarks>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// When the subscription was created.
    /// </summary>
    /// <value>UTC timestamp of subscription creation.</value>
    /// <remarks>
    /// LOGIC: Recorded for audit and management purposes.
    /// Default: current time.
    /// </remarks>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Human-readable name for the subscription.
    /// </summary>
    /// <value>A descriptive name for the subscription, or null.</value>
    /// <remarks>
    /// LOGIC: Aids in subscription management and debugging.
    /// Example: "UI sync status updater", "Audit log writer".
    /// Default: null.
    /// </remarks>
    public string? Name { get; init; }
}
