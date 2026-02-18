// =============================================================================
// File: ISyncConflictDetector.cs
// Project: Lexichord.Abstractions
// Description: Interface for detecting conflicts between document and graph state.
// =============================================================================
// LOGIC: ISyncConflictDetector compares extraction results against the
//   current graph state to identify conflicts. It produces SyncConflict
//   records that describe what differs and how severe the conflict is.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// Dependencies: SyncConflict, Document, ExtractionResult (all from Lexichord.Abstractions)
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Service for detecting conflicts between document extractions and graph state.
/// </summary>
/// <remarks>
/// <para>
/// Analyzes document extractions against existing graph state to find:
/// </para>
/// <list type="bullet">
///   <item><b>Value Mismatches:</b> Same property with different values.</item>
///   <item><b>Missing Entities:</b> Entities in one source but not the other.</item>
///   <item><b>Relationship Differences:</b> Edges that don't match.</item>
///   <item><b>Concurrent Edits:</b> Both sides changed since last sync.</item>
/// </list>
/// <para>
/// This is an internal service used by <see cref="ISyncOrchestrator"/>.
/// </para>
/// <para>
/// <b>Implementation:</b> See <c>SyncConflictDetector</c> in Lexichord.Modules.Knowledge.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public interface ISyncConflictDetector
{
    /// <summary>
    /// Detects conflicts between document extraction and graph state.
    /// </summary>
    /// <param name="document">The source document.</param>
    /// <param name="extraction">The entities extracted from the document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="SyncConflict"/> entries. Empty if no conflicts.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: For each extracted entity/claim:
    /// </para>
    /// <list type="number">
    ///   <item>Look up corresponding graph node by ID or canonical form.</item>
    ///   <item>If not found, check if it should exist (MissingInGraph).</item>
    ///   <item>If found, compare property values (ValueMismatch).</item>
    ///   <item>Compare timestamps to detect concurrent edits.</item>
    /// </list>
    /// <para>
    /// Also checks for graph entities linked to this document that are
    /// no longer present in the extraction (MissingInDocument).
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<SyncConflict>> DetectAsync(
        Document document,
        ExtractionResult extraction,
        CancellationToken ct = default);
}
