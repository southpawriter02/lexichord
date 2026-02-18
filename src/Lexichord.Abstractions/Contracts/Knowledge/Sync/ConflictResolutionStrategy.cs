// =============================================================================
// File: ConflictResolutionStrategy.cs
// Project: Lexichord.Abstractions
// Description: Defines strategies for resolving synchronization conflicts.
// =============================================================================
// LOGIC: When conflicts are detected, the user (or auto-resolver) must choose
//   how to handle them. Each strategy defines a different approach to
//   reconciling document and graph state.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Strategy for resolving synchronization conflicts between document and graph.
/// </summary>
/// <remarks>
/// <para>
/// Available strategies for conflict resolution:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="UseDocument"/>: Document value wins.</description></item>
///   <item><description><see cref="UseGraph"/>: Graph value wins.</description></item>
///   <item><description><see cref="Manual"/>: Require explicit user choice.</description></item>
///   <item><description><see cref="Merge"/>: Attempt intelligent merge.</description></item>
///   <item><description><see cref="DiscardDocument"/>: Discard document changes.</description></item>
///   <item><description><see cref="DiscardGraph"/>: Discard graph changes.</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public enum ConflictResolutionStrategy
{
    /// <summary>
    /// Use the document value as authoritative.
    /// </summary>
    /// <remarks>
    /// LOGIC: The document is the source of truth. Graph values are
    /// overwritten with document values. Use when the document represents
    /// the latest intended state.
    /// </remarks>
    UseDocument = 0,

    /// <summary>
    /// Use the knowledge graph value as authoritative.
    /// </summary>
    /// <remarks>
    /// LOGIC: The graph is the source of truth. Document extractions are
    /// ignored in favor of existing graph values. Use when the graph has
    /// been curated or verified.
    /// </remarks>
    UseGraph = 1,

    /// <summary>
    /// Require explicit manual intervention.
    /// </summary>
    /// <remarks>
    /// LOGIC: No automatic resolution. User must review each conflict and
    /// choose the correct value. Safest option for critical data but
    /// requires user attention.
    /// </remarks>
    Manual = 2,

    /// <summary>
    /// Attempt intelligent merge of both values.
    /// </summary>
    /// <remarks>
    /// LOGIC: Try to combine document and graph values intelligently.
    /// For collections, union the items. For timestamps, use the latest.
    /// For text, may attempt diff-based merge. Falls back to
    /// <see cref="Manual"/> if merge is not possible.
    /// </remarks>
    Merge = 3,

    /// <summary>
    /// Discard all document changes and keep graph state.
    /// </summary>
    /// <remarks>
    /// LOGIC: Reset the document's sync state without applying any changes.
    /// The graph remains unchanged. Use when document changes were made
    /// in error and should be abandoned.
    /// </remarks>
    DiscardDocument = 4,

    /// <summary>
    /// Discard graph changes and replace with document state.
    /// </summary>
    /// <remarks>
    /// LOGIC: Replace all graph entities linked to this document with
    /// fresh extractions. Existing graph relationships may be lost.
    /// Use for a full re-sync when graph state is corrupted.
    /// </remarks>
    DiscardGraph = 5
}
