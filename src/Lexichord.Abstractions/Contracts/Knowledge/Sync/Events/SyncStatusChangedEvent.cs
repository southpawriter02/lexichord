// =============================================================================
// File: SyncStatusChangedEvent.cs
// Project: Lexichord.Abstractions
// Description: Event published when a document's sync status changes.
// =============================================================================
// LOGIC: Published when a document's sync state transitions (e.g., NeverSynced
//   to InSync, InSync to Conflict). Handlers can update UI indicators, trigger
//   notifications, or schedule background jobs.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: ISyncEvent (v0.7.6j), SyncState (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Event published when a document's sync status changes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Notifies subscribers when a document's synchronization state
/// transitions to a new value.
/// </para>
/// <para>
/// <b>State Transitions:</b>
/// </para>
/// <list type="bullet">
///   <item>NeverSynced → InSync: Initial sync completed.</item>
///   <item>InSync → PendingSync: Document modified.</item>
///   <item>PendingSync → InSync: Sync completed successfully.</item>
///   <item>PendingSync → Conflict: Conflicts detected.</item>
///   <item>Conflict → InSync: Conflicts resolved.</item>
/// </list>
/// <para>
/// <b>Common Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item>Update sync status indicators in UI.</item>
///   <item>Send notifications for conflict states.</item>
///   <item>Schedule background sync jobs.</item>
///   <item>Log state transitions for audit.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class SyncStatusHandler : INotificationHandler&lt;SyncStatusChangedEvent&gt;
/// {
///     public Task Handle(SyncStatusChangedEvent notification, CancellationToken ct)
///     {
///         if (notification.NewState == SyncState.Conflict)
///         {
///             // Alert user about conflict
///             Console.WriteLine($"Document {notification.DocumentId} has conflicts!");
///         }
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public record SyncStatusChangedEvent : ISyncEvent
{
    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Generated at event creation. Used for deduplication and tracking.
    /// </remarks>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Recorded at event creation for ordering and audit.
    /// </remarks>
    public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: The document whose status changed.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Extensible metadata for correlation and context.
    /// </remarks>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// The sync state before the transition.
    /// </summary>
    /// <value>The state the document was in prior to this change.</value>
    /// <remarks>
    /// LOGIC: Enables handlers to detect specific transitions.
    /// Example: PendingSync → InSync indicates successful sync.
    /// </remarks>
    public required SyncState PreviousState { get; init; }

    /// <summary>
    /// The sync state after the transition.
    /// </summary>
    /// <value>The state the document transitioned to.</value>
    /// <remarks>
    /// LOGIC: Enables handlers to react to specific target states.
    /// Example: NewState == Conflict triggers conflict notifications.
    /// </remarks>
    public required SyncState NewState { get; init; }

    /// <summary>
    /// Reason for the state change.
    /// </summary>
    /// <value>Human-readable explanation for the transition, or null.</value>
    /// <remarks>
    /// LOGIC: Provides context for why the state changed.
    /// Example: "Sync completed successfully", "Conflicts detected during sync".
    /// </remarks>
    public string? Reason { get; init; }

    /// <summary>
    /// When the state change occurred.
    /// </summary>
    /// <value>UTC timestamp of the state transition.</value>
    /// <remarks>
    /// LOGIC: May differ from PublishedAt if event publication is delayed.
    /// </remarks>
    public DateTimeOffset ChangedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User who initiated the state change.
    /// </summary>
    /// <value>User ID who triggered the change, or null for system-initiated.</value>
    /// <remarks>
    /// LOGIC: Null indicates automatic operations (background sync, webhooks).
    /// Non-null indicates user-initiated actions.
    /// </remarks>
    public Guid? ChangedBy { get; init; }

    /// <summary>
    /// Creates a new <see cref="SyncStatusChangedEvent"/> with the specified parameters.
    /// </summary>
    /// <param name="documentId">The document whose status changed.</param>
    /// <param name="previousState">The state before the transition.</param>
    /// <param name="newState">The state after the transition.</param>
    /// <param name="reason">Optional reason for the change.</param>
    /// <param name="changedBy">Optional user who initiated the change.</param>
    /// <returns>A new <see cref="SyncStatusChangedEvent"/> instance.</returns>
    /// <remarks>
    /// LOGIC: Factory method for consistent event creation.
    /// Sets ChangedAt to current time.
    /// </remarks>
    public static SyncStatusChangedEvent Create(
        Guid documentId,
        SyncState previousState,
        SyncState newState,
        string? reason = null,
        Guid? changedBy = null) => new()
    {
        DocumentId = documentId,
        PreviousState = previousState,
        NewState = newState,
        Reason = reason,
        ChangedAt = DateTimeOffset.UtcNow,
        ChangedBy = changedBy
    };
}
