// =============================================================================
// File: SyncStatusHistory.cs
// Project: Lexichord.Abstractions
// Description: Record representing a historical sync status state change.
// =============================================================================
// LOGIC: Captures audit trail of sync status transitions. Each time a document's
//   sync state changes, a history record is created to track the transition,
//   who initiated it, and any relevant context.
//
// v0.7.6i: Sync Status Tracker (CKVS Phase 4c)
// Dependencies: SyncState (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Status;

/// <summary>
/// Historical record of a sync status state change.
/// </summary>
/// <remarks>
/// <para>
/// Provides an audit trail of sync status transitions:
/// </para>
/// <list type="bullet">
///   <item><b>States:</b> Previous and new sync states.</item>
///   <item><b>Timing:</b> When the change occurred.</item>
///   <item><b>Attribution:</b> Who initiated the change.</item>
///   <item><b>Context:</b> Reason and associated operation.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var history = await tracker.GetStatusHistoryAsync(documentId, limit: 50);
/// foreach (var entry in history)
/// {
///     Console.WriteLine($"{entry.ChangedAt}: {entry.PreviousState} -> {entry.NewState}");
/// }
/// </code>
/// </para>
/// <para>
/// <b>Retention:</b> History retention depends on license tier:
/// </para>
/// <list type="bullet">
///   <item>WriterPro: 30 days</item>
///   <item>Teams: 90 days</item>
///   <item>Enterprise: Unlimited</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6i as part of the Sync Status Tracker.
/// </para>
/// </remarks>
public record SyncStatusHistory
{
    /// <summary>
    /// Unique identifier for this history entry.
    /// </summary>
    /// <value>A globally unique identifier for the history record.</value>
    /// <remarks>
    /// LOGIC: Primary key for history lookups and deduplication.
    /// Generated when the history entry is created.
    /// </remarks>
    public required Guid HistoryId { get; init; }

    /// <summary>
    /// The document whose status changed.
    /// </summary>
    /// <value>The document ID that this history entry relates to.</value>
    /// <remarks>
    /// LOGIC: Foreign key to the document. Used for filtering history
    /// by document when querying historical status changes.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// The sync state before the change.
    /// </summary>
    /// <value>The state the document was in prior to this transition.</value>
    /// <remarks>
    /// LOGIC: Captures the origin state in the state machine.
    /// Used to verify valid state transitions and audit history.
    /// </remarks>
    public required SyncState PreviousState { get; init; }

    /// <summary>
    /// The sync state after the change.
    /// </summary>
    /// <value>The state the document transitioned to.</value>
    /// <remarks>
    /// LOGIC: Captures the destination state in the state machine.
    /// Combined with PreviousState shows the complete transition.
    /// </remarks>
    public required SyncState NewState { get; init; }

    /// <summary>
    /// When the state change occurred.
    /// </summary>
    /// <value>UTC timestamp of the state transition.</value>
    /// <remarks>
    /// LOGIC: Exact time of the state change for audit and ordering.
    /// History entries are typically ordered by ChangedAt descending.
    /// </remarks>
    public required DateTimeOffset ChangedAt { get; init; }

    /// <summary>
    /// User who initiated the state change.
    /// </summary>
    /// <value>
    /// The user ID who triggered the change, or null for system-initiated changes.
    /// </value>
    /// <remarks>
    /// LOGIC: Attribution for audit purposes. Null indicates automatic
    /// system operations (background sync, scheduled jobs, etc.).
    /// </remarks>
    public Guid? ChangedBy { get; init; }

    /// <summary>
    /// Human-readable reason for the state change.
    /// </summary>
    /// <value>
    /// Explanation of why the state changed, or null if not specified.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides context for the transition. Examples:
    /// - "Sync completed successfully"
    /// - "Conflict detected during sync"
    /// - "Manual sync initiated by user"
    /// - "Auto-resolved low-severity conflict"
    /// </remarks>
    public string? Reason { get; init; }

    /// <summary>
    /// Associated sync operation that caused this change.
    /// </summary>
    /// <value>
    /// The operation ID that triggered this state change, or null if not
    /// associated with a specific operation.
    /// </value>
    /// <remarks>
    /// LOGIC: Links to <see cref="SyncOperationRecord"/> for detailed
    /// operation information. Enables drill-down from history to operation.
    /// </remarks>
    public Guid? SyncOperationId { get; init; }

    /// <summary>
    /// Additional metadata about the state change.
    /// </summary>
    /// <value>
    /// Key-value pairs with additional context. Defaults to empty dictionary.
    /// </value>
    /// <remarks>
    /// LOGIC: Extensible metadata storage for future needs. May include:
    /// - "entityCount": Number of entities affected
    /// - "conflictCount": Number of conflicts detected
    /// - "clientVersion": Client app version
    /// - "triggerSource": What triggered the sync (UI, API, scheduled)
    /// </remarks>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
