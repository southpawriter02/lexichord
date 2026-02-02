// =============================================================================
// File: IndexManagementService.cs
// Project: Lexichord.Modules.RAG
// Description: Service for manual index management operations.
// Version: v0.4.7b
// =============================================================================
// LOGIC: Implements IIndexManagementService for manual index management.
//   - ReindexDocumentAsync: Clear chunks → re-read file → re-index via pipeline.
//   - RemoveFromIndexAsync: Delete chunks → delete document.
//   - ReindexAllAsync: Iterate all documents → re-index with progress reporting.
//   - Publishes MediatR events for telemetry on each operation.
// =============================================================================

using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Service for managing document index operations manually.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexManagementService"/> provides manual control over the document index,
/// enabling users to re-index individual documents, remove documents from the index,
/// or trigger a full corpus re-index through the Settings UI.
/// </para>
/// <para>
/// <b>Dependencies:</b>
/// </para>
/// <list type="bullet">
///   <item><see cref="IDocumentRepository"/> - Document data access</item>
///   <item><see cref="IChunkRepository"/> - Chunk data access</item>
///   <item><see cref="Indexing.DocumentIndexingPipeline"/> - Re-indexing workflow</item>
///   <item><see cref="IMediator"/> - Event publishing for telemetry</item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This service is thread-safe. All operations use async/await
/// and do not maintain mutable state. However, concurrent re-index operations on the
/// same document may produce undefined results.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7b as part of Manual Indexing Controls.
/// </para>
/// </remarks>
public sealed class IndexManagementService : IIndexManagementService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IChunkRepository _chunkRepository;
    private readonly IDocumentIndexingPipeline _indexingPipeline;
    private readonly IMediator _mediator;
    private readonly ILogger<IndexManagementService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="IndexManagementService"/>.
    /// </summary>
    /// <param name="documentRepository">Repository for document data access.</param>
    /// <param name="chunkRepository">Repository for chunk data access.</param>
    /// <param name="indexingPipeline">Pipeline for document indexing workflow.</param>
    /// <param name="mediator">MediatR mediator for event publishing.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public IndexManagementService(
        IDocumentRepository documentRepository,
        IChunkRepository chunkRepository,
        IDocumentIndexingPipeline indexingPipeline,
        IMediator mediator,
        ILogger<IndexManagementService> logger)
    {
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
        _indexingPipeline = indexingPipeline ?? throw new ArgumentNullException(nameof(indexingPipeline));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("[IndexManagementService] Initialized");
    }

    /// <inheritdoc />
    public async Task<IndexManagementResult> ReindexDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        _logger.LogInformation("[IndexManagementService] Starting re-index for document {DocumentId}", documentId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // LOGIC: Step 1 - Retrieve document
            _logger.LogDebug("[IndexManagementService] Retrieving document {DocumentId}", documentId);
            var document = await _documentRepository.GetByIdAsync(documentId, ct);

            if (document == null)
            {
                _logger.LogWarning("[IndexManagementService] Document {DocumentId} not found", documentId);
                return IndexManagementResult.NotFound(documentId);
            }

            _logger.LogDebug("[IndexManagementService] Found document: {FilePath}", document.FilePath);

            // LOGIC: Step 2 - Delete existing chunks
            _logger.LogDebug("[IndexManagementService] Deleting chunks for document {DocumentId}", documentId);
            var deletedChunks = await _chunkRepository.DeleteByDocumentIdAsync(documentId, ct);
            _logger.LogDebug("[IndexManagementService] Deleted {ChunkCount} chunks", deletedChunks);

            // LOGIC: Step 3 - Read file content
            _logger.LogDebug("[IndexManagementService] Reading file content from {FilePath}", document.FilePath);
            if (!File.Exists(document.FilePath))
            {
                _logger.LogWarning("[IndexManagementService] File not found: {FilePath}", document.FilePath);
                stopwatch.Stop();
                return IndexManagementResult.FailureSingle(
                    documentId,
                    $"File not found: {document.FilePath}",
                    stopwatch.Elapsed);
            }

            var content = await File.ReadAllTextAsync(document.FilePath, ct);
            _logger.LogDebug("[IndexManagementService] Read {ContentLength} characters from file", content.Length);

            // LOGIC: Step 4 - Re-index via pipeline
            _logger.LogDebug("[IndexManagementService] Re-indexing document via pipeline");
            var result = await _indexingPipeline.IndexDocumentAsync(
                document.FilePath,
                content,
                chunkingMode: null,
                ct);

            stopwatch.Stop();

            if (result.Success)
            {
                _logger.LogInformation(
                    "[IndexManagementService] Re-index completed successfully for {FilePath}: " +
                    "{ChunkCount} chunks in {Duration}ms",
                    document.FilePath,
                    result.ChunkCount,
                    stopwatch.ElapsedMilliseconds);

                // LOGIC: Publish telemetry event
                await _mediator.Publish(
                    new Indexing.DocumentReindexedEvent(
                        documentId,
                        document.FilePath,
                        result.ChunkCount,
                        stopwatch.Elapsed),
                    ct);

                return IndexManagementResult.SuccessSingle(
                    documentId,
                    $"Successfully re-indexed {document.Title} with {result.ChunkCount} chunks.",
                    stopwatch.Elapsed);
            }
            else
            {
                _logger.LogWarning(
                    "[IndexManagementService] Re-index failed for {FilePath}: {Error}",
                    document.FilePath,
                    result.ErrorMessage);

                return IndexManagementResult.FailureSingle(
                    documentId,
                    result.ErrorMessage ?? "Re-indexing failed with unknown error.",
                    stopwatch.Elapsed);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[IndexManagementService] Re-index cancelled for document {DocumentId}", documentId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IndexManagementService] Re-index failed for document {DocumentId}", documentId);
            stopwatch.Stop();
            return IndexManagementResult.FailureSingle(documentId, ex.Message, stopwatch.Elapsed);
        }
    }

    /// <inheritdoc />
    public async Task<IndexManagementResult> RemoveFromIndexAsync(Guid documentId, CancellationToken ct = default)
    {
        _logger.LogInformation("[IndexManagementService] Starting removal for document {DocumentId}", documentId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // LOGIC: Step 1 - Retrieve document
            _logger.LogDebug("[IndexManagementService] Retrieving document {DocumentId}", documentId);
            var document = await _documentRepository.GetByIdAsync(documentId, ct);

            if (document == null)
            {
                _logger.LogWarning("[IndexManagementService] Document {DocumentId} not found", documentId);
                return IndexManagementResult.NotFound(documentId);
            }

            var filePath = document.FilePath;
            _logger.LogDebug("[IndexManagementService] Found document: {FilePath}", filePath);

            // LOGIC: Step 2 - Delete chunks first (referential integrity)
            _logger.LogDebug("[IndexManagementService] Deleting chunks for document {DocumentId}", documentId);
            var deletedChunks = await _chunkRepository.DeleteByDocumentIdAsync(documentId, ct);
            _logger.LogDebug("[IndexManagementService] Deleted {ChunkCount} chunks", deletedChunks);

            // LOGIC: Step 3 - Delete document record
            _logger.LogDebug("[IndexManagementService] Deleting document record {DocumentId}", documentId);
            await _documentRepository.DeleteAsync(documentId, ct);

            stopwatch.Stop();

            _logger.LogInformation(
                "[IndexManagementService] Document removed successfully: {FilePath} ({ChunkCount} chunks deleted)",
                filePath,
                deletedChunks);

            // LOGIC: Publish telemetry event
            await _mediator.Publish(
                new Indexing.DocumentRemovedFromIndexEvent(documentId, filePath),
                ct);

            return IndexManagementResult.SuccessSingle(
                documentId,
                $"Removed {Path.GetFileName(filePath)} from index ({deletedChunks} chunks deleted).",
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[IndexManagementService] Removal cancelled for document {DocumentId}", documentId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IndexManagementService] Removal failed for document {DocumentId}", documentId);
            stopwatch.Stop();
            return IndexManagementResult.FailureSingle(documentId, ex.Message, stopwatch.Elapsed);
        }
    }

    /// <inheritdoc />
    public async Task<IndexManagementResult> ReindexAllAsync(IProgress<int>? progress = null, CancellationToken ct = default)
    {
        _logger.LogInformation("[IndexManagementService] Starting re-index of all documents");
        var stopwatch = Stopwatch.StartNew();

        var successCount = 0;
        var failedCount = 0;

        try
        {
            // LOGIC: Step 1 - Retrieve all documents across all statuses
            _logger.LogDebug("[IndexManagementService] Retrieving all indexed documents");
            var allDocuments = new List<Document>();

            // Get documents from all relevant statuses
            foreach (var status in new[] { DocumentStatus.Indexed, DocumentStatus.Stale, DocumentStatus.Failed, DocumentStatus.Pending })
            {
                var documents = await _documentRepository.GetByStatusAsync(status, ct);
                allDocuments.AddRange(documents);
            }

            var totalCount = allDocuments.Count;
            _logger.LogInformation("[IndexManagementService] Found {TotalCount} documents to re-index", totalCount);

            if (totalCount == 0)
            {
                stopwatch.Stop();
                _logger.LogInformation("[IndexManagementService] No documents to re-index");
                return IndexManagementResult.Bulk(0, 0, stopwatch.Elapsed);
            }

            // LOGIC: Step 2 - Process each document
            for (var i = 0; i < allDocuments.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var document = allDocuments[i];
                _logger.LogDebug(
                    "[IndexManagementService] Processing document {Index}/{Total}: {FilePath}",
                    i + 1,
                    totalCount,
                    document.FilePath);

                try
                {
                    // Re-index individual document (reuse existing logic)
                    var result = await ReindexDocumentAsync(document.Id, ct);

                    if (result.Success)
                    {
                        successCount++;
                        _logger.LogDebug("[IndexManagementService] Document {DocumentId} re-indexed successfully", document.Id);
                    }
                    else
                    {
                        failedCount++;
                        _logger.LogWarning(
                            "[IndexManagementService] Document {DocumentId} re-index failed: {Error}",
                            document.Id,
                            result.Message);
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogError(ex, "[IndexManagementService] Exception re-indexing document {DocumentId}", document.Id);
                }

                // Report progress (percentage)
                var percentage = (int)((i + 1) * 100.0 / totalCount);
                progress?.Report(percentage);
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "[IndexManagementService] Bulk re-index completed: {SuccessCount} succeeded, {FailedCount} failed, " +
                "total duration {Duration}ms",
                successCount,
                failedCount,
                stopwatch.ElapsedMilliseconds);

            // LOGIC: Publish telemetry event
            await _mediator.Publish(
                new Indexing.AllDocumentsReindexedEvent(successCount, failedCount, stopwatch.Elapsed),
                ct);

            return IndexManagementResult.Bulk(successCount, failedCount, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "[IndexManagementService] Bulk re-index cancelled after {SuccessCount} successes and {FailedCount} failures",
                successCount,
                failedCount);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IndexManagementService] Bulk re-index failed unexpectedly");
            stopwatch.Stop();
            return IndexManagementResult.Bulk(successCount, failedCount + 1, stopwatch.Elapsed);
        }
    }
}
