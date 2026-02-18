// =============================================================================
// File: SyncCompletedEvent.cs
// Project: Lexichord.Abstractions
// Description: Event published when a sync operation completes.
// =============================================================================
// LOGIC: MediatR notification for sync completion, enabling subscribers
//   to react to sync results (e.g., update UI, refresh caches, trigger
//   downstream operations).
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// Dependencies: SyncResult, SyncDirection (v0.7.6e)
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Event raised when a synchronization operation completes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Notifies subscribers when a sync operation finishes,
/// regardless of success or failure. Subscribers can:
/// </para>
/// <list type="bullet">
///   <item>Update UI to reflect new sync state.</item>
///   <item>Refresh caches affected by entity/claim changes.</item>
///   <item>Trigger downstream processing for affected documents.</item>
///   <item>Log audit trails for compliance.</item>
/// </list>
/// <para>
/// <b>Publication:</b> Published by <see cref="ISyncService.SyncDocumentToGraphAsync"/>
/// when <see cref="SyncContext.PublishEvents"/> is true (default).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class SyncCompletedHandler : INotificationHandler&lt;SyncCompletedEvent&gt;
/// {
///     public async Task Handle(SyncCompletedEvent notification, CancellationToken ct)
///     {
///         Console.WriteLine($"Sync completed for document {notification.DocumentId}");
///         Console.WriteLine($"Status: {notification.Result.Status}");
///         Console.WriteLine($"Entities affected: {notification.Result.EntitiesAffected.Count}");
///     }
/// }
/// </code>
/// </example>
public record SyncCompletedEvent : INotification
{
    /// <summary>
    /// ID of the document that was synchronized.
    /// </summary>
    /// <value>The unique identifier of the synced document.</value>
    /// <remarks>
    /// LOGIC: Used by subscribers to identify which document was synced.
    /// Enables filtering for document-specific handlers.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// The result of the sync operation.
    /// </summary>
    /// <value>
    /// Contains status, affected entities/claims/relationships,
    /// conflicts, duration, and error details.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides full details about the sync outcome. Subscribers
    /// can inspect this to determine appropriate reactions.
    /// </remarks>
    public required SyncResult Result { get; init; }

    /// <summary>
    /// Direction of the sync operation.
    /// </summary>
    /// <value>
    /// Whether this was document-to-graph, graph-to-document, or bidirectional.
    /// </value>
    /// <remarks>
    /// LOGIC: Helps subscribers understand the context of the sync.
    /// Graph-to-document syncs indicate external changes affecting the document.
    /// </remarks>
    public required SyncDirection Direction { get; init; }

    /// <summary>
    /// Timestamp when the event was raised.
    /// </summary>
    /// <value>UTC timestamp of event creation.</value>
    /// <remarks>
    /// LOGIC: Recorded at event creation for audit trails and ordering.
    /// May differ slightly from <see cref="SyncResult.CompletedAt"/>.
    /// </remarks>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a new <see cref="SyncCompletedEvent"/> with the specified parameters.
    /// </summary>
    /// <param name="documentId">The document that was synchronized.</param>
    /// <param name="result">The sync operation result.</param>
    /// <param name="direction">The sync direction.</param>
    /// <returns>A new <see cref="SyncCompletedEvent"/> instance.</returns>
    /// <remarks>
    /// LOGIC: Factory method following the project convention for event creation.
    /// Ensures required properties are set and provides a clean API.
    /// </remarks>
    public static SyncCompletedEvent Create(Guid documentId, SyncResult result, SyncDirection direction)
        => new()
        {
            DocumentId = documentId,
            Result = result,
            Direction = direction
        };
}
