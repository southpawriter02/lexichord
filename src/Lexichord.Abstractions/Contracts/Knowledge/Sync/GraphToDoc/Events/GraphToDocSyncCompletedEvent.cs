// =============================================================================
// File: GraphToDocSyncCompletedEvent.cs
// Project: Lexichord.Abstractions
// Description: Event published when a graph-to-document sync operation completes.
// =============================================================================
// LOGIC: MediatR notification for sync completion, enabling subscribers
//   to react to graph-to-doc sync results (e.g., update UI, log audit,
//   trigger downstream workflows).
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: GraphToDocSyncResult, GraphChange (v0.7.6e)
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc.Events;

/// <summary>
/// Event raised when a graph-to-document synchronization operation completes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Notifies subscribers when a graph-to-doc sync finishes,
/// regardless of success or failure. Subscribers can:
/// </para>
/// <list type="bullet">
///   <item>Update UI to show new flags and affected documents.</item>
///   <item>Log audit trails for compliance tracking.</item>
///   <item>Trigger follow-up workflows based on sync results.</item>
///   <item>Update dashboard statistics.</item>
///   <item>Aggregate change notifications for batch delivery.</item>
/// </list>
/// <para>
/// <b>Publication:</b> Published by <see cref="IGraphToDocumentSyncProvider.OnGraphChangeAsync"/>
/// on operation completion, after all flags are created.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class GraphToDocSyncCompletedHandler : INotificationHandler&lt;GraphToDocSyncCompletedEvent&gt;
/// {
///     public async Task Handle(GraphToDocSyncCompletedEvent notification, CancellationToken ct)
///     {
///         Console.WriteLine($"Graph change processed: {notification.TriggeringChange.ChangeType}");
///         Console.WriteLine($"Status: {notification.Result.Status}");
///         Console.WriteLine($"Affected documents: {notification.Result.AffectedDocuments.Count}");
///         Console.WriteLine($"Flags created: {notification.Result.FlagsCreated.Count}");
///     }
/// }
/// </code>
/// </example>
public record GraphToDocSyncCompletedEvent : INotification
{
    /// <summary>
    /// The graph change that triggered this sync.
    /// </summary>
    /// <value>The change event that initiated the operation.</value>
    /// <remarks>
    /// LOGIC: Links the event to its cause. Used for logging,
    /// audit trails, and understanding the context of the sync.
    /// </remarks>
    public required GraphChange TriggeringChange { get; init; }

    /// <summary>
    /// The result of the sync operation.
    /// </summary>
    /// <value>
    /// Contains status, affected documents, created flags, notification
    /// counts, and timing information.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides full details about the sync outcome. Subscribers
    /// can inspect this to determine appropriate reactions.
    /// </remarks>
    public required GraphToDocSyncResult Result { get; init; }

    /// <summary>
    /// Timestamp when the event was raised.
    /// </summary>
    /// <value>UTC timestamp of event creation.</value>
    /// <remarks>
    /// LOGIC: Recorded at event creation for audit trails and ordering.
    /// May differ slightly from sync completion time due to event
    /// creation overhead.
    /// </remarks>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User who initiated the sync operation.
    /// </summary>
    /// <value>The user ID if initiated by a user, null if automated.</value>
    /// <remarks>
    /// LOGIC: Enables audit trails and user-specific handling.
    /// Null indicates background/automated sync (e.g., triggered by
    /// graph change events).
    /// </remarks>
    public Guid? InitiatedBy { get; init; }

    /// <summary>
    /// Creates a new <see cref="GraphToDocSyncCompletedEvent"/> with the specified parameters.
    /// </summary>
    /// <param name="triggeringChange">The graph change that triggered the sync.</param>
    /// <param name="result">The sync operation result.</param>
    /// <param name="initiatedBy">Optional user who initiated the sync.</param>
    /// <returns>A new <see cref="GraphToDocSyncCompletedEvent"/> instance.</returns>
    /// <remarks>
    /// LOGIC: Factory method following the project convention for event creation.
    /// Ensures required properties are set and provides a clean API.
    /// </remarks>
    public static GraphToDocSyncCompletedEvent Create(
        GraphChange triggeringChange,
        GraphToDocSyncResult result,
        Guid? initiatedBy = null)
        => new()
        {
            TriggeringChange = triggeringChange,
            Result = result,
            InitiatedBy = initiatedBy
        };
}
