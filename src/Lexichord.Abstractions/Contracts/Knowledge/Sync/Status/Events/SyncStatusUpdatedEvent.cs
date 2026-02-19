// =============================================================================
// File: SyncStatusUpdatedEvent.cs
// Project: Lexichord.Abstractions
// Description: MediatR notification published when a sync status changes state.
// =============================================================================
// LOGIC: Enables reactive workflows when document sync status transitions.
//   Handlers can respond to state changes for UI updates, notifications,
//   background job scheduling, or audit logging.
//
// v0.7.6i: Sync Status Tracker (CKVS Phase 4c)
// Dependencies: SyncState (v0.7.6e), MediatR
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Status.Events;

/// <summary>
/// MediatR notification published when a document's sync status changes state.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="ISyncStatusTracker"/> when <see cref="SyncStatus.State"/>
/// transitions to a different value. Not published for updates that only change
/// metadata without changing state.
/// </para>
/// <para>
/// <b>Common Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item>Update UI status indicators when sync completes.</item>
///   <item>Send notifications when conflicts are detected.</item>
///   <item>Schedule background jobs for documents entering PendingSync.</item>
///   <item>Log state transitions for audit trail.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// public class SyncStatusHandler : INotificationHandler&lt;SyncStatusUpdatedEvent&gt;
/// {
///     public Task Handle(SyncStatusUpdatedEvent notification, CancellationToken ct)
///     {
///         if (notification.NewState == SyncState.Conflict)
///         {
///             // Alert user about conflict
///         }
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6i as part of the Sync Status Tracker.
/// </para>
/// </remarks>
public record SyncStatusUpdatedEvent : INotification
{
    /// <summary>
    /// The document whose status changed.
    /// </summary>
    /// <value>The unique identifier of the affected document.</value>
    /// <remarks>
    /// LOGIC: Identifies which document triggered the event.
    /// Handlers can use this to load additional document data.
    /// </remarks>
    public required Guid DocumentId { get; init; }

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
    /// When the state change occurred.
    /// </summary>
    /// <value>UTC timestamp of the state transition.</value>
    /// <remarks>
    /// LOGIC: Captures exact time for ordering and audit purposes.
    /// Defaults to UtcNow when using factory method.
    /// </remarks>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User who initiated the state change.
    /// </summary>
    /// <value>
    /// The user ID who triggered the change, or null for system-initiated changes.
    /// </value>
    /// <remarks>
    /// LOGIC: Attribution for audit and notification targeting.
    /// Null indicates automatic operations (background sync, webhooks, etc.).
    /// </remarks>
    public Guid? ChangedBy { get; init; }

    /// <summary>
    /// Creates a new <see cref="SyncStatusUpdatedEvent"/>.
    /// </summary>
    /// <param name="documentId">The document whose status changed.</param>
    /// <param name="previousState">The state before the transition.</param>
    /// <param name="newState">The state after the transition.</param>
    /// <param name="changedBy">Optional user who initiated the change.</param>
    /// <returns>A new <see cref="SyncStatusUpdatedEvent"/> instance.</returns>
    /// <remarks>
    /// LOGIC: Factory method for consistent event creation.
    /// Sets Timestamp to UtcNow automatically.
    /// </remarks>
    public static SyncStatusUpdatedEvent Create(
        Guid documentId,
        SyncState previousState,
        SyncState newState,
        Guid? changedBy = null) => new()
    {
        DocumentId = documentId,
        PreviousState = previousState,
        NewState = newState,
        Timestamp = DateTimeOffset.UtcNow,
        ChangedBy = changedBy
    };
}
