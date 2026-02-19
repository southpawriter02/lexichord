// =============================================================================
// File: IGraphToDocumentSyncProvider.cs
// Project: Lexichord.Abstractions
// Description: Interface for the graph-to-document sync provider.
// =============================================================================
// LOGIC: Primary interface for handling graph changes and flagging affected
//   documents. Provides methods for change handling, document detection,
//   flag management, and subscription support.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: GraphToDocSyncResult, GraphToDocSyncOptions, AffectedDocument,
//               DocumentFlag, FlagResolution, GraphChangeSubscription,
//               GraphChange (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Provider for graph-to-document synchronization operations.
/// </summary>
/// <remarks>
/// <para>
/// The main entry point for handling graph changes and notifying affected documents:
/// </para>
/// <list type="bullet">
///   <item><b>Change Handling:</b> Process graph changes and flag affected documents.</item>
///   <item><b>Detection:</b> Find documents affected by specific entities.</item>
///   <item><b>Flags:</b> Manage document flags for review workflows.</item>
///   <item><b>Subscriptions:</b> Enable targeted change notifications.</item>
/// </list>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item>Core: No access to graph-to-doc sync features.</item>
///   <item>WriterPro: No access to graph-to-doc sync features.</item>
///   <item>Teams: Full graph-to-doc sync with notifications and flagging.</item>
///   <item>Enterprise: Full access with advanced subscription features.</item>
/// </list>
/// </para>
/// <para>
/// <b>Implementation:</b> See <c>GraphToDocumentSyncProvider</c> in
/// Lexichord.Modules.Knowledge.Sync.GraphToDoc.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Handle a graph change and flag affected documents
/// var options = new GraphToDocSyncOptions
/// {
///     AutoFlagDocuments = true,
///     SendNotifications = true
/// };
///
/// var result = await provider.OnGraphChangeAsync(graphChange, options, ct);
///
/// if (result.Status == SyncOperationStatus.Success)
/// {
///     Console.WriteLine($"Found {result.AffectedDocuments.Count} affected documents");
///     Console.WriteLine($"Created {result.FlagsCreated.Count} flags");
/// }
/// </code>
/// </example>
public interface IGraphToDocumentSyncProvider
{
    /// <summary>
    /// Handles a graph change and flags affected documents.
    /// </summary>
    /// <param name="change">The graph change to process.</param>
    /// <param name="options">Configuration for the sync operation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="GraphToDocSyncResult"/> containing the operation status,
    /// affected documents, created flags, and notification counts.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: The sync pipeline:
    /// </para>
    /// <list type="number">
    ///   <item>Validate user's license tier (Teams+ required).</item>
    ///   <item>Detect documents referencing the changed entity.</item>
    ///   <item>Limit to <see cref="GraphToDocSyncOptions.MaxDocumentsPerChange"/>.</item>
    ///   <item>Determine flag priority based on change type.</item>
    ///   <item>Create flags if <see cref="GraphToDocSyncOptions.AutoFlagDocuments"/> enabled.</item>
    ///   <item>Send notifications if enabled (with deduplication).</item>
    ///   <item>Publish <see cref="Events.GraphToDocSyncCompletedEvent"/>.</item>
    /// </list>
    /// </remarks>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the user's license tier does not support graph-to-doc sync.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown when the operation exceeds <see cref="GraphToDocSyncOptions.Timeout"/>.
    /// </exception>
    Task<GraphToDocSyncResult> OnGraphChangeAsync(
        GraphChange change,
        GraphToDocSyncOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets documents affected by a specific graph entity.
    /// </summary>
    /// <param name="entityId">The entity ID to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="AffectedDocument"/> records for documents
    /// that reference the specified entity.
    /// </returns>
    /// <remarks>
    /// LOGIC: Queries the document-entity relationship store to find
    /// all documents with references to the given entity. Does not
    /// create flags or send notifications.
    /// </remarks>
    Task<IReadOnlyList<AffectedDocument>> GetAffectedDocumentsAsync(
        Guid entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets pending flags for a document.
    /// </summary>
    /// <param name="documentId">The document ID to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="DocumentFlag"/> records with
    /// <see cref="FlagStatus.Pending"/> or <see cref="FlagStatus.Acknowledged"/>
    /// status for the specified document.
    /// </returns>
    /// <remarks>
    /// LOGIC: Retrieves all unresolved flags for the document,
    /// ordered by priority (highest first) then creation date.
    /// </remarks>
    Task<IReadOnlyList<DocumentFlag>> GetPendingFlagsAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves a flag with the specified resolution.
    /// </summary>
    /// <param name="flagId">The flag ID to resolve.</param>
    /// <param name="resolution">How the flag was resolved.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the flag was resolved, false if not found.</returns>
    /// <remarks>
    /// LOGIC: Updates the flag's status to <see cref="FlagStatus.Resolved"/>
    /// and records the resolution type and timestamp.
    /// </remarks>
    Task<bool> ResolveFlagAsync(
        Guid flagId,
        FlagResolution resolution,
        CancellationToken ct = default);

    /// <summary>
    /// Subscribes a document to graph change notifications.
    /// </summary>
    /// <param name="documentId">The document to subscribe.</param>
    /// <param name="subscription">The subscription configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Creates a subscription that monitors specified entities
    /// for changes. When matching changes occur, the document is
    /// automatically flagged and the specified user is notified.
    /// </para>
    /// <para>
    /// Subscriptions are additive - a document can have multiple
    /// subscriptions with different entity sets or change types.
    /// </para>
    /// </remarks>
    Task SubscribeToGraphChangesAsync(
        Guid documentId,
        GraphChangeSubscription subscription,
        CancellationToken ct = default);
}
