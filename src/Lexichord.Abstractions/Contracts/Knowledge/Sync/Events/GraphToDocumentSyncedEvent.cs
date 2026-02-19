// =============================================================================
// File: GraphToDocumentSyncedEvent.cs
// Project: Lexichord.Abstractions
// Description: Event published when graph changes are synced to documents.
// =============================================================================
// LOGIC: Published when a graph change triggers document updates. Handlers can
//   update UI to show affected documents, send notifications, or trigger
//   document review workflows.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: ISyncEvent (v0.7.6j), GraphChange (v0.7.6e),
//               AffectedDocument (v0.7.6g), DocumentFlag (v0.7.6g)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Event published when graph changes are propagated to documents.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Notifies subscribers when a graph change has been detected
/// and documents have been identified as affected.
/// </para>
/// <para>
/// <b>Graph-to-Document Flow:</b>
/// </para>
/// <list type="number">
///   <item>Entity or relationship changes in the knowledge graph.</item>
///   <item>Affected documents are identified.</item>
///   <item>Documents are flagged for review.</item>
///   <item>This event is published.</item>
/// </list>
/// <para>
/// <b>Common Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item>Update document list UI with affected indicators.</item>
///   <item>Send notifications about documents needing review.</item>
///   <item>Trigger document re-indexing.</item>
///   <item>Log graph-to-document sync completion.</item>
/// </list>
/// <para>
/// <b>License Requirement:</b> Graph-to-document sync requires Teams tier or higher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class GraphSyncHandler : INotificationHandler&lt;GraphToDocumentSyncedEvent&gt;
/// {
///     public Task Handle(GraphToDocumentSyncedEvent notification, CancellationToken ct)
///     {
///         Console.WriteLine($"Graph change synced: {notification.TriggeringChange.ChangeType}");
///         Console.WriteLine($"  Affected documents: {notification.TotalAffectedDocuments}");
///         Console.WriteLine($"  Flags created: {notification.FlagsCreated.Count}");
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public record GraphToDocumentSyncedEvent : ISyncEvent
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
    /// LOGIC: Primary document affected by the sync.
    /// For graph-to-document sync, this is typically the first affected document
    /// or a placeholder if multiple documents are affected equally.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Extensible metadata for correlation and context.
    /// </remarks>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// The graph change that triggered the sync.
    /// </summary>
    /// <value>Details of the graph modification.</value>
    /// <remarks>
    /// LOGIC: Contains entity ID, change type, old/new values, and timestamp.
    /// </remarks>
    public required GraphChange TriggeringChange { get; init; }

    /// <summary>
    /// Documents affected by the graph change.
    /// </summary>
    /// <value>List of affected document details.</value>
    /// <remarks>
    /// LOGIC: Each affected document includes relationship type, reference count,
    /// and suggested action.
    /// </remarks>
    public IReadOnlyList<AffectedDocument> AffectedDocuments { get; init; } = [];

    /// <summary>
    /// Flags created for document review.
    /// </summary>
    /// <value>List of document flags created.</value>
    /// <remarks>
    /// LOGIC: Flags indicate that documents need review due to graph changes.
    /// </remarks>
    public IReadOnlyList<DocumentFlag> FlagsCreated { get; init; } = [];

    /// <summary>
    /// Total number of documents affected.
    /// </summary>
    /// <value>Count of affected documents.</value>
    /// <remarks>
    /// LOGIC: May be larger than <see cref="AffectedDocuments"/> if results were limited.
    /// </remarks>
    public int TotalAffectedDocuments { get; init; }

    /// <summary>
    /// Creates a new <see cref="GraphToDocumentSyncedEvent"/> with the specified parameters.
    /// </summary>
    /// <param name="documentId">Primary affected document ID.</param>
    /// <param name="triggeringChange">The graph change that triggered sync.</param>
    /// <param name="affectedDocuments">List of affected documents.</param>
    /// <param name="flagsCreated">Flags created for review.</param>
    /// <returns>A new <see cref="GraphToDocumentSyncedEvent"/> instance.</returns>
    /// <remarks>
    /// LOGIC: Factory method for consistent event creation.
    /// Sets TotalAffectedDocuments automatically from affected documents list.
    /// </remarks>
    public static GraphToDocumentSyncedEvent Create(
        Guid documentId,
        GraphChange triggeringChange,
        IReadOnlyList<AffectedDocument>? affectedDocuments = null,
        IReadOnlyList<DocumentFlag>? flagsCreated = null) => new()
    {
        DocumentId = documentId,
        TriggeringChange = triggeringChange,
        AffectedDocuments = affectedDocuments ?? [],
        FlagsCreated = flagsCreated ?? [],
        TotalAffectedDocuments = affectedDocuments?.Count ?? 0
    };
}
