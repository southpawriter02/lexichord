// =============================================================================
// File: IIndexManagementService.cs
// Project: Lexichord.Abstractions
// Description: Interface for manual index management operations.
// Version: v0.4.7b
// =============================================================================
// LOGIC: Defines the contract for manual index management operations.
//   - ReindexDocumentAsync re-indexes a single document.
//   - RemoveFromIndexAsync removes a document and its chunks from the index.
//   - ReindexAllAsync re-indexes all documents with progress reporting.
//   - All operations publish MediatR events for telemetry.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Provides methods for manually managing the document index.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IIndexManagementService"/> is the primary contract for manual index
/// management operations in the Settings UI. It enables users to:
/// </para>
/// <list type="bullet">
///   <item><description>Re-index a single stale or failed document.</description></item>
///   <item><description>Remove a document from the index entirely.</description></item>
///   <item><description>Re-index all documents in the corpus.</description></item>
/// </list>
/// <para>
/// <b>Dependencies:</b>
/// </para>
/// <list type="bullet">
///   <item><see cref="IDocumentRepository"/> (v0.4.1c) - Document data access</item>
///   <item><see cref="IChunkRepository"/> (v0.4.1c) - Chunk data access</item>
///   <item><c>DocumentIndexingPipeline</c> (v0.4.4d) - Re-indexing workflow</item>
///   <item><c>IMediator</c> - Event publishing for telemetry</item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe. Operations may be
/// invoked concurrently from the UI thread while background indexing is active.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7b as part of Manual Indexing Controls.
/// </para>
/// </remarks>
public interface IIndexManagementService
{
    /// <summary>
    /// Re-indexes a single document by clearing its chunks and re-processing through
    /// the indexing pipeline.
    /// </summary>
    /// <param name="documentId">The unique identifier of the document to re-index.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>
    /// An <see cref="IndexManagementResult"/> indicating whether the re-indexing
    /// succeeded or failed with diagnostic information.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Workflow:</b>
    /// </para>
    /// <list type="number">
    ///   <item><description>Retrieve document by ID from <see cref="IDocumentRepository"/>.</description></item>
    ///   <item><description>Delete existing chunks via <see cref="IChunkRepository.DeleteByDocumentIdAsync"/>.</description></item>
    ///   <item><description>Re-process document through <c>DocumentIndexingPipeline</c>.</description></item>
    ///   <item><description>Publish <c>DocumentReindexedEvent</c> on success.</description></item>
    /// </list>
    /// <para>
    /// <b>Error Handling:</b> If the document is not found, returns a failed result
    /// with an appropriate message. If the pipeline fails, the result contains the
    /// error details.
    /// </para>
    /// </remarks>
    Task<IndexManagementResult> ReindexDocumentAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Removes a document and all its chunks from the index.
    /// </summary>
    /// <param name="documentId">The unique identifier of the document to remove.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>
    /// An <see cref="IndexManagementResult"/> indicating whether the removal
    /// succeeded or failed with diagnostic information.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Workflow:</b>
    /// </para>
    /// <list type="number">
    ///   <item><description>Retrieve document by ID from <see cref="IDocumentRepository"/>.</description></item>
    ///   <item><description>Delete all chunks via <see cref="IChunkRepository.DeleteByDocumentIdAsync"/>.</description></item>
    ///   <item><description>Delete document record via <see cref="IDocumentRepository.DeleteAsync"/>.</description></item>
    ///   <item><description>Publish <c>DocumentRemovedFromIndexEvent</c> on success.</description></item>
    /// </list>
    /// <para>
    /// <b>Destructive Operation:</b> This operation permanently removes the document
    /// from the index. The original file on disk is NOT affected. To re-add the
    /// document, the file watcher will detect it on next scan, or it can be manually
    /// added via the ingestion queue.
    /// </para>
    /// <para>
    /// <b>Error Handling:</b> If the document is not found, returns a failed result.
    /// Chunk deletion is performed first to ensure referential integrity.
    /// </para>
    /// </remarks>
    Task<IndexManagementResult> RemoveFromIndexAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Re-indexes all documents in the corpus.
    /// </summary>
    /// <param name="progress">
    /// Optional progress reporter for tracking bulk operation progress.
    /// Reports as a percentage (0-100) after each document is processed.
    /// </param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>
    /// An <see cref="IndexManagementResult"/> with aggregate statistics including
    /// the number of documents successfully processed and the number that failed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Workflow:</b>
    /// </para>
    /// <list type="number">
    ///   <item><description>Retrieve all documents from <see cref="IDocumentRepository"/>.</description></item>
    ///   <item><description>For each document: delete chunks, re-index via pipeline.</description></item>
    ///   <item><description>Report progress after each document completes.</description></item>
    ///   <item><description>Aggregate results and publish <c>AllDocumentsReindexedEvent</c>.</description></item>
    /// </list>
    /// <para>
    /// <b>Partial Failure Handling:</b> If some documents fail to re-index, the
    /// operation continues with remaining documents. The result's <see cref="IndexManagementResult.FailedCount"/>
    /// indicates how many documents failed, and <see cref="IndexManagementResult.Success"/>
    /// is <c>false</c> if any failures occurred.
    /// </para>
    /// <para>
    /// <b>Cancellation:</b> The operation checks the cancellation token after each
    /// document. Cancellation stops further processing but does not rollback
    /// already-processed documents.
    /// </para>
    /// <para>
    /// <b>Performance Consideration:</b> This operation may take significant time
    /// for large corpora. Consider running on a background thread and providing
    /// UI feedback via the progress reporter.
    /// </para>
    /// </remarks>
    Task<IndexManagementResult> ReindexAllAsync(IProgress<int>? progress = null, CancellationToken ct = default);
}
