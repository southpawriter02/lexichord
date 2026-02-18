// =============================================================================
// File: ISyncService.cs
// Project: Lexichord.Abstractions
// Description: Interface for the main sync service orchestrating document-graph sync.
// =============================================================================
// LOGIC: ISyncService is the primary public interface for synchronization.
//   It provides high-level operations for syncing documents to the graph,
//   querying sync status, and resolving conflicts.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// Dependencies: SyncResult, SyncStatus, SyncContext, GraphChange,
//               ConflictResolutionStrategy, Document (all from Lexichord.Abstractions)
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Service for orchestrating bidirectional synchronization between documents and the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// The main entry point for sync operations. Provides high-level methods for:
/// </para>
/// <list type="bullet">
///   <item><b>Sync:</b> Push document changes to the knowledge graph.</item>
///   <item><b>Impact:</b> Find documents affected by graph changes.</item>
///   <item><b>Status:</b> Query the current sync state of documents.</item>
///   <item><b>Resolution:</b> Resolve conflicts between document and graph.</item>
/// </list>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item>Core: No access to sync features.</item>
///   <item>WriterPro: Document-to-graph sync only (manual trigger).</item>
///   <item>Teams: Full bidirectional sync with all conflict resolution strategies.</item>
///   <item>Enterprise: Full access with custom policies.</item>
/// </list>
/// </para>
/// <para>
/// <b>Implementation:</b> See <c>SyncService</c> in Lexichord.Modules.Knowledge.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Sync a document to the knowledge graph
/// var context = new SyncContext
/// {
///     UserId = currentUser.Id,
///     Document = document,
///     AutoResolveConflicts = true
/// };
/// var result = await syncService.SyncDocumentToGraphAsync(document, context);
///
/// if (result.Status == SyncOperationStatus.SuccessWithConflicts)
/// {
///     // Handle conflicts
///     await syncService.ResolveConflictAsync(
///         document.Id, ConflictResolutionStrategy.Merge);
/// }
/// </code>
/// </example>
public interface ISyncService
{
    /// <summary>
    /// Synchronizes a document to the knowledge graph.
    /// </summary>
    /// <param name="document">The document to synchronize.</param>
    /// <param name="context">Configuration for the sync operation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="SyncResult"/> containing the operation status,
    /// affected entities/claims/relationships, and any conflicts.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: This is the primary sync operation. It:
    /// </para>
    /// <list type="number">
    ///   <item>Validates the user's license tier.</item>
    ///   <item>Extracts entities and claims from the document.</item>
    ///   <item>Detects conflicts against existing graph state.</item>
    ///   <item>Applies auto-resolution if enabled and severity allows.</item>
    ///   <item>Upserts entities/claims/relationships to the graph.</item>
    ///   <item>Updates the document's sync status.</item>
    ///   <item>Publishes <see cref="SyncCompletedEvent"/> if events enabled.</item>
    /// </list>
    /// </remarks>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the user's license tier does not support sync operations.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown when the operation exceeds <see cref="SyncContext.Timeout"/>.
    /// </exception>
    Task<SyncResult> SyncDocumentToGraphAsync(
        Document document,
        SyncContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Gets documents affected by a knowledge graph change.
    /// </summary>
    /// <param name="change">The graph change to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of documents that reference the changed entity and may need
    /// to be reviewed or re-synced.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Used for graph-to-document sync propagation. When an entity
    /// is modified in the graph (e.g., via direct edit or sync from another
    /// document), this method finds all documents that reference that entity.
    /// </para>
    /// <para>
    /// Requires Teams tier or above for full functionality.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<Document>> GetAffectedDocumentsAsync(
        GraphChange change,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current sync status for a document.
    /// </summary>
    /// <param name="documentId">The document ID to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="SyncStatus"/> containing the current state, timestamps,
    /// pending changes, and conflict count.
    /// </returns>
    /// <remarks>
    /// LOGIC: Queries the status tracker for the document's current
    /// relationship with the knowledge graph. Used by UI to show sync
    /// indicators and enable/disable actions.
    /// </remarks>
    Task<SyncStatus> GetSyncStatusAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves sync conflicts for a document using the specified strategy.
    /// </summary>
    /// <param name="documentId">The document with conflicts to resolve.</param>
    /// <param name="strategy">The resolution strategy to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="SyncResult"/> containing the resolution outcome.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Applies the chosen strategy to all pending conflicts for the
    /// document. After resolution:
    /// </para>
    /// <list type="bullet">
    ///   <item>Success: Document state changes to InSync or PendingSync.</item>
    ///   <item>Partial: Some conflicts remain, state stays Conflict.</item>
    /// </list>
    /// </remarks>
    Task<SyncResult> ResolveConflictAsync(
        Guid documentId,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a document needs synchronization.
    /// </summary>
    /// <param name="documentId">The document ID to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// True if the document has pending changes, conflicts, or has never
    /// been synced. False if fully synchronized.
    /// </returns>
    /// <remarks>
    /// LOGIC: Quick check without full status retrieval. Used for
    /// badge counts, indicators, and determining if sync action is needed.
    /// </remarks>
    Task<bool> NeedsSyncAsync(
        Guid documentId,
        CancellationToken ct = default);
}
