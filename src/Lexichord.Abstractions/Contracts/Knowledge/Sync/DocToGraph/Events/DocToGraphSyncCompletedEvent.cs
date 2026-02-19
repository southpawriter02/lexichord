// =============================================================================
// File: DocToGraphSyncCompletedEvent.cs
// Project: Lexichord.Abstractions
// Description: Event published when a document-to-graph sync operation completes.
// =============================================================================
// LOGIC: MediatR notification for sync completion, enabling subscribers
//   to react to sync results (e.g., update UI, refresh caches, trigger
//   downstream operations).
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: DocToGraphSyncResult
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph.Events;

/// <summary>
/// Event raised when a document-to-graph synchronization operation completes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Notifies subscribers when a doc-to-graph sync finishes,
/// regardless of success or failure. Subscribers can:
/// </para>
/// <list type="bullet">
///   <item>Update UI to reflect new sync state and entity counts.</item>
///   <item>Refresh caches affected by entity/claim changes.</item>
///   <item>Trigger downstream processing for extracted entities.</item>
///   <item>Log audit trails for compliance.</item>
///   <item>Update sync status indicators.</item>
/// </list>
/// <para>
/// <b>Publication:</b> Published by <see cref="IDocumentToGraphSyncProvider.SyncAsync"/>
/// on operation completion, after all graph updates are committed.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DocToGraphSyncCompletedHandler : INotificationHandler&lt;DocToGraphSyncCompletedEvent&gt;
/// {
///     public async Task Handle(DocToGraphSyncCompletedEvent notification, CancellationToken ct)
///     {
///         Console.WriteLine($"Sync completed for document {notification.DocumentId}");
///         Console.WriteLine($"Status: {notification.Result.Status}");
///         Console.WriteLine($"Entities upserted: {notification.Result.UpsertedEntities.Count}");
///         Console.WriteLine($"Claims extracted: {notification.Result.ExtractedClaims.Count}");
///     }
/// }
/// </code>
/// </example>
public record DocToGraphSyncCompletedEvent : INotification
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
    /// Contains status, upserted entities, created relationships,
    /// extracted claims, validation errors, and timing information.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides full details about the sync outcome. Subscribers
    /// can inspect this to determine appropriate reactions.
    /// </remarks>
    public required DocToGraphSyncResult Result { get; init; }

    /// <summary>
    /// Timestamp when the event was raised.
    /// </summary>
    /// <value>UTC timestamp of event creation.</value>
    /// <remarks>
    /// LOGIC: Recorded at event creation for audit trails and ordering.
    /// May differ slightly from <see cref="DocToGraphSyncResult.Duration"/>
    /// end time due to event creation overhead.
    /// </remarks>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User who initiated the sync operation.
    /// </summary>
    /// <value>The user ID if initiated by a user, null if automated.</value>
    /// <remarks>
    /// LOGIC: Enables audit trails and user-specific notifications.
    /// Null indicates background/automated sync.
    /// </remarks>
    public Guid? InitiatedBy { get; init; }

    /// <summary>
    /// Creates a new <see cref="DocToGraphSyncCompletedEvent"/> with the specified parameters.
    /// </summary>
    /// <param name="documentId">The document that was synchronized.</param>
    /// <param name="result">The sync operation result.</param>
    /// <param name="initiatedBy">Optional user who initiated the sync.</param>
    /// <returns>A new <see cref="DocToGraphSyncCompletedEvent"/> instance.</returns>
    /// <remarks>
    /// LOGIC: Factory method following the project convention for event creation.
    /// Ensures required properties are set and provides a clean API.
    /// </remarks>
    public static DocToGraphSyncCompletedEvent Create(
        Guid documentId,
        DocToGraphSyncResult result,
        Guid? initiatedBy = null)
        => new()
        {
            DocumentId = documentId,
            Result = result,
            InitiatedBy = initiatedBy
        };
}
