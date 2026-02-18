// =============================================================================
// File: ISyncOrchestrator.cs
// Project: Lexichord.Abstractions
// Description: Interface for internal sync workflow orchestration.
// =============================================================================
// LOGIC: ISyncOrchestrator handles the low-level sync pipeline execution.
//   It coordinates extraction, conflict detection, and graph operations.
//   This is an internal interface used by ISyncService.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// Dependencies: SyncResult, SyncContext, SyncConflict, GraphChange,
//               Document, ExtractionResult (all from Lexichord.Abstractions)
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Internal orchestrator for sync workflow execution.
/// </summary>
/// <remarks>
/// <para>
/// Handles the low-level sync pipeline operations:
/// </para>
/// <list type="bullet">
///   <item><b>Document-to-Graph:</b> Extract, detect conflicts, upsert.</item>
///   <item><b>Graph-to-Document:</b> Find affected documents, flag for review.</item>
///   <item><b>Conflict Detection:</b> Compare extraction against graph state.</item>
/// </list>
/// <para>
/// This is an internal interface used by <see cref="ISyncService"/>. It is
/// not intended for direct use by application code.
/// </para>
/// <para>
/// <b>Implementation:</b> See <c>SyncOrchestrator</c> in Lexichord.Modules.Knowledge.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public interface ISyncOrchestrator
{
    /// <summary>
    /// Executes the document-to-graph synchronization pipeline.
    /// </summary>
    /// <param name="document">The document to sync.</param>
    /// <param name="context">Sync configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="SyncResult"/> with affected entities, claims, relationships,
    /// and any conflicts detected.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Executes the full document-to-graph pipeline:
    /// </para>
    /// <list type="number">
    ///   <item>Extract entities from document content using <see cref="IEntityExtractionPipeline"/>.</item>
    ///   <item>Extract claims using <see cref="IClaimExtractionService"/>.</item>
    ///   <item>Detect conflicts via <see cref="ISyncConflictDetector"/>.</item>
    ///   <item>Upsert entities to graph via <see cref="IGraphRepository"/>.</item>
    ///   <item>Create/update relationships.</item>
    ///   <item>Store claims.</item>
    ///   <item>Mark document as synced via <see cref="IDocumentRepository"/>.</item>
    /// </list>
    /// </remarks>
    Task<SyncResult> ExecuteDocumentToGraphAsync(
        Document document,
        SyncContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Executes the graph-to-document synchronization pipeline.
    /// </summary>
    /// <param name="change">The graph change to propagate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="SyncResult"/> entries, one per affected document.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Propagates graph changes to source documents:
    /// </para>
    /// <list type="number">
    ///   <item>Find documents referencing the changed entity.</item>
    ///   <item>For each document, flag it for review.</item>
    ///   <item>Record the change for conflict detection on next sync.</item>
    /// </list>
    /// <para>
    /// Requires Teams tier. WriterPro users cannot receive graph-to-document
    /// propagation.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<SyncResult>> ExecuteGraphToDocumentAsync(
        GraphChange change,
        CancellationToken ct = default);

    /// <summary>
    /// Detects conflicts between document extraction and graph state.
    /// </summary>
    /// <param name="document">The source document.</param>
    /// <param name="extraction">The extraction result from the document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="SyncConflict"/> entries describing each conflict.
    /// Empty if no conflicts detected.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Compares extraction results against current graph state:
    /// </para>
    /// <list type="bullet">
    ///   <item>For each extracted entity, check if a matching graph node exists.</item>
    ///   <item>If exists, compare property values for mismatches.</item>
    ///   <item>Check for entities in graph that are no longer in document.</item>
    ///   <item>Compare timestamps to detect concurrent edits.</item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<SyncConflict>> DetectConflictsAsync(
        Document document,
        ExtractionResult extraction,
        CancellationToken ct = default);
}
