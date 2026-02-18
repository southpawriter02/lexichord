// =============================================================================
// File: IConflictResolver.cs
// Project: Lexichord.Abstractions
// Description: Interface for resolving synchronization conflicts.
// =============================================================================
// LOGIC: IConflictResolver applies resolution strategies to sync conflicts.
//   It handles the actual modification of graph or document state based
//   on the chosen strategy (UseDocument, UseGraph, Merge, etc.).
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// Dependencies: SyncResult, ConflictResolutionStrategy (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Service for resolving synchronization conflicts between documents and graph.
/// </summary>
/// <remarks>
/// <para>
/// Applies conflict resolution strategies:
/// </para>
/// <list type="bullet">
///   <item><b>UseDocument:</b> Overwrite graph with document values.</item>
///   <item><b>UseGraph:</b> Discard document changes, keep graph values.</item>
///   <item><b>Merge:</b> Attempt intelligent combination of both sources.</item>
///   <item><b>Manual:</b> Flag for user intervention (no auto-resolution).</item>
///   <item><b>Discard:</b> Discard all changes from one source.</item>
/// </list>
/// <para>
/// This is an internal service used by <see cref="ISyncService"/>.
/// </para>
/// <para>
/// <b>Implementation:</b> See <c>ConflictResolver</c> in Lexichord.Modules.Knowledge.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public interface IConflictResolver
{
    /// <summary>
    /// Resolves all pending conflicts for a document using the specified strategy.
    /// </summary>
    /// <param name="documentId">The document with conflicts to resolve.</param>
    /// <param name="strategy">The resolution strategy to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="SyncResult"/> indicating success/failure and any
    /// remaining conflicts that couldn't be resolved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Retrieves pending conflicts for the document and applies
    /// the strategy to each:
    /// </para>
    /// <list type="bullet">
    ///   <item><see cref="ConflictResolutionStrategy.UseDocument"/>:
    ///     Update graph entities with document values.</item>
    ///   <item><see cref="ConflictResolutionStrategy.UseGraph"/>:
    ///     Mark conflicts as resolved, no graph changes.</item>
    ///   <item><see cref="ConflictResolutionStrategy.Merge"/>:
    ///     Attempt to merge values; fall back to Manual if not possible.</item>
    ///   <item><see cref="ConflictResolutionStrategy.Manual"/>:
    ///     Leave conflicts unresolved for user review.</item>
    ///   <item><see cref="ConflictResolutionStrategy.DiscardDocument"/>:
    ///     Reset document sync state, keep graph unchanged.</item>
    ///   <item><see cref="ConflictResolutionStrategy.DiscardGraph"/>:
    ///     Delete graph entities linked to document, re-sync fresh.</item>
    /// </list>
    /// </remarks>
    Task<SyncResult> ResolveAsync(
        Guid documentId,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default);
}
