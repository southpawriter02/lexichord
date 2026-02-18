// =============================================================================
// File: SyncState.cs
// Project: Lexichord.Abstractions
// Description: Defines the synchronization state of a document.
// =============================================================================
// LOGIC: Each document has a sync state indicating its relationship with the
//   knowledge graph. This state machine drives the sync UI and determines
//   what actions are available to the user.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Current synchronization state of a document with the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// Represents the document's position in the sync lifecycle:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="InSync"/>: Document and graph match.</description></item>
///   <item><description><see cref="PendingSync"/>: Document has unsynced changes.</description></item>
///   <item><description><see cref="NeedsReview"/>: Synced but requires human review.</description></item>
///   <item><description><see cref="Conflict"/>: Conflicting changes need resolution.</description></item>
///   <item><description><see cref="NeverSynced"/>: Document has never been synchronized.</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public enum SyncState
{
    /// <summary>
    /// Document and knowledge graph are fully synchronized.
    /// </summary>
    /// <remarks>
    /// LOGIC: No pending changes. The last sync completed successfully
    /// without conflicts. No action required.
    /// </remarks>
    InSync = 0,

    /// <summary>
    /// Document has changes pending synchronization.
    /// </summary>
    /// <remarks>
    /// LOGIC: Document content has changed since the last sync. The sync
    /// button should be enabled to allow the user to push changes to the graph.
    /// </remarks>
    PendingSync = 1,

    /// <summary>
    /// Document needs manual review before further sync.
    /// </summary>
    /// <remarks>
    /// LOGIC: Sync completed with minor issues that should be reviewed.
    /// The document is synced but may have low-confidence extractions
    /// or auto-resolved conflicts that need human verification.
    /// </remarks>
    NeedsReview = 2,

    /// <summary>
    /// Conflict exists between document and graph state.
    /// </summary>
    /// <remarks>
    /// LOGIC: The document and graph have diverged in a way that cannot
    /// be automatically resolved. User must choose a resolution strategy
    /// via <see cref="ISyncService.ResolveConflictAsync"/>.
    /// </remarks>
    Conflict = 3,

    /// <summary>
    /// Document has never been synchronized to the knowledge graph.
    /// </summary>
    /// <remarks>
    /// LOGIC: Initial state for newly indexed documents. No sync history
    /// exists. The sync button prompts for initial synchronization.
    /// </remarks>
    NeverSynced = 4
}
