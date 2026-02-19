// =============================================================================
// File: IDocumentToGraphSyncProvider.cs
// Project: Lexichord.Abstractions
// Description: Interface for the document-to-graph sync provider.
// =============================================================================
// LOGIC: Primary interface for syncing documents to the knowledge graph.
//   Provides methods for sync execution, validation, lineage retrieval,
//   and rollback capabilities.
//
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// Dependencies: DocToGraphSyncResult, DocToGraphSyncOptions, ExtractionResult,
//               ValidationResult, ExtractionRecord, Document
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;

/// <summary>
/// Provider for document-to-graph synchronization operations.
/// </summary>
/// <remarks>
/// <para>
/// The main entry point for syncing documents to the knowledge graph. Provides:
/// </para>
/// <list type="bullet">
///   <item><b>Sync:</b> Extract entities/claims from documents and upsert to graph.</item>
///   <item><b>Validation:</b> Validate extraction results before upsert.</item>
///   <item><b>Lineage:</b> Retrieve extraction history for a document.</item>
///   <item><b>Rollback:</b> Restore graph state to a previous extraction version.</item>
/// </list>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item>Core: No access to sync features.</item>
///   <item>WriterPro: Manual sync only (basic validation, lineage).</item>
///   <item>Teams: Full sync with enrichment and advanced features.</item>
///   <item>Enterprise: Full access with custom policies.</item>
/// </list>
/// </para>
/// <para>
/// <b>Implementation:</b> See <c>DocumentToGraphSyncProvider</c> in
/// Lexichord.Modules.Knowledge.Sync.DocToGraph.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6f as part of the Doc-to-Graph Sync module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Sync a document to the knowledge graph
/// var options = new DocToGraphSyncOptions
/// {
///     ValidateBeforeUpsert = true,
///     ExtractClaims = true,
///     EnrichWithGraphContext = true
/// };
///
/// var result = await provider.SyncAsync(document, options, ct);
///
/// if (result.Status == SyncOperationStatus.Success)
/// {
///     Console.WriteLine($"Synced {result.TotalEntitiesAffected} entities");
/// }
/// </code>
/// </example>
public interface IDocumentToGraphSyncProvider
{
    /// <summary>
    /// Synchronizes a document to the knowledge graph.
    /// </summary>
    /// <param name="document">The document to synchronize.</param>
    /// <param name="options">Configuration for the sync operation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="DocToGraphSyncResult"/> containing the operation status,
    /// upserted entities, relationships, claims, and any validation errors.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: The sync pipeline:
    /// </para>
    /// <list type="number">
    ///   <item>Validate user's license tier.</item>
    ///   <item>Extract entities using <c>IEntityExtractionPipeline</c>.</item>
    ///   <item>Validate extraction if <see cref="DocToGraphSyncOptions.ValidateBeforeUpsert"/> enabled.</item>
    ///   <item>Transform entities to <see cref="KnowledgeEntity"/> format.</item>
    ///   <item>Enrich entities with graph context if enabled.</item>
    ///   <item>Upsert entities to graph via <c>IGraphRepository</c>.</item>
    ///   <item>Create relationships if enabled.</item>
    ///   <item>Extract claims if enabled.</item>
    ///   <item>Record extraction lineage if enabled.</item>
    ///   <item>Publish <see cref="Events.DocToGraphSyncCompletedEvent"/>.</item>
    /// </list>
    /// </remarks>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the user's license tier does not support sync operations.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown when the operation exceeds <see cref="DocToGraphSyncOptions.Timeout"/>.
    /// </exception>
    Task<DocToGraphSyncResult> SyncAsync(
        Document document,
        DocToGraphSyncOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Validates an extraction result against the graph schema.
    /// </summary>
    /// <param name="extraction">The extraction result to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> containing the validation outcome.
    /// </returns>
    /// <remarks>
    /// LOGIC: Validates entities and relationships without performing the
    /// actual upsert. Useful for preview/dry-run scenarios.
    /// </remarks>
    Task<ValidationResult> ValidateExtractionAsync(
        ExtractionResult extraction,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the extraction lineage for a document.
    /// </summary>
    /// <param name="documentId">The document ID to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A chronologically ordered list of <see cref="ExtractionRecord"/>
    /// entries for the document, most recent first.
    /// </returns>
    /// <remarks>
    /// LOGIC: Retrieves all extraction records for the document, enabling
    /// version comparison and rollback target selection.
    /// </remarks>
    Task<IReadOnlyList<ExtractionRecord>> GetExtractionLineageAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Rolls back a previous sync operation to restore graph state.
    /// </summary>
    /// <param name="documentId">The document whose sync to rollback.</param>
    /// <param name="targetVersion">The timestamp to rollback to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// True if rollback succeeded, false if the target version was not found
    /// or rollback was not possible.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Rollback process:
    /// </para>
    /// <list type="number">
    ///   <item>Find the extraction record at or before targetVersion.</item>
    ///   <item>Identify entities/claims/relationships from later extractions.</item>
    ///   <item>Remove entities not in the target version.</item>
    ///   <item>Optionally restore deleted entities from target version.</item>
    /// </list>
    /// <para>
    /// Note: Rollback may affect entities shared with other documents.
    /// Consider the impact before executing.
    /// </para>
    /// </remarks>
    Task<bool> RollbackSyncAsync(
        Guid documentId,
        DateTimeOffset targetVersion,
        CancellationToken ct = default);
}
