// =============================================================================
// File: IndexStatusService.cs
// Project: Lexichord.Modules.RAG
// Description: Service for retrieving index status and statistics.
// Version: v0.4.7a
// =============================================================================
// LOGIC: Implementation of IIndexStatusService that aggregates data from
//        IDocumentRepository and IChunkRepository to provide index status info.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Service for retrieving document indexing status and aggregate statistics.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexStatusService"/> provides real-time visibility into the state
/// of the document index by querying the repositories and computing status for
/// each document.
/// </para>
/// <para>
/// <b>Dependencies:</b>
/// <list type="bullet">
/// <item><see cref="IDocumentRepository"/> - Document data access</item>
/// <item><see cref="IChunkRepository"/> - Chunk counts</item>
/// <item><see cref="IFileHashService"/> - Stale detection via hash comparison</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7a as part of the Index Status View.
/// </para>
/// </remarks>
public sealed class IndexStatusService : IIndexStatusService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IChunkRepository _chunkRepository;
    private readonly IFileHashService _fileHashService;
    private readonly ILogger<IndexStatusService> _logger;

    // Estimated average chunk size for storage calculations
    private const int AverageChunkSizeBytes = 2048;

    /// <summary>
    /// Initializes a new instance of <see cref="IndexStatusService"/>.
    /// </summary>
    /// <param name="documentRepository">Repository for document data access.</param>
    /// <param name="chunkRepository">Repository for chunk data access.</param>
    /// <param name="fileHashService">Service for computing and comparing file hashes.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the parameters are null.
    /// </exception>
    public IndexStatusService(
        IDocumentRepository documentRepository,
        IChunkRepository chunkRepository,
        IFileHashService fileHashService,
        ILogger<IndexStatusService> logger)
    {
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
        _fileHashService = fileHashService ?? throw new ArgumentNullException(nameof(fileHashService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("[IndexStatusService] Initialized with document repository, chunk repository, and file hash service");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IndexedDocumentInfo>> GetAllDocumentsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[IndexStatusService] Retrieving all documents with status information");

        try
        {
            // Get all documents with status Pending, Indexing, Indexed, Stale, or Failed
            var statuses = new[]
            {
                DocumentStatus.Pending,
                DocumentStatus.Indexing,
                DocumentStatus.Indexed,
                DocumentStatus.Failed,
                DocumentStatus.Stale
            };

            var documents = new List<Document>();
            foreach (var status in statuses)
            {
                var docs = await _documentRepository.GetByStatusAsync(status, ct);
                documents.AddRange(docs);
            }

            _logger.LogDebug("[IndexStatusService] Found {DocumentCount} documents across all statuses", documents.Count);

            var results = new List<IndexedDocumentInfo>();
            foreach (var doc in documents)
            {
                var info = await BuildDocumentInfoAsync(doc, ct);
                results.Add(info);
            }

            _logger.LogInformation("[IndexStatusService] Retrieved status for {Count} documents", results.Count);
            return results.AsReadOnly();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[IndexStatusService] Failed to retrieve document statuses");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IndexedDocumentInfo?> GetDocumentStatusAsync(Guid documentId, CancellationToken ct = default)
    {
        _logger.LogDebug("[IndexStatusService] Retrieving status for document {DocumentId}", documentId);

        try
        {
            var document = await _documentRepository.GetByIdAsync(documentId, ct);
            if (document is null)
            {
                _logger.LogWarning("[IndexStatusService] Document {DocumentId} not found", documentId);
                return null;
            }

            var info = await BuildDocumentInfoAsync(document, ct);
            _logger.LogDebug("[IndexStatusService] Document {DocumentId} has status {Status}", documentId, info.Status);
            return info;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[IndexStatusService] Failed to retrieve status for document {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IndexStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[IndexStatusService] Calculating index statistics");

        try
        {
            var documents = await GetAllDocumentsAsync(ct);

            var totalDocuments = documents.Count;
            var totalChunks = documents.Sum(d => d.ChunkCount);
            var totalSize = documents.Sum(d => d.EstimatedSizeBytes);
            var lastIndexed = documents
                .Where(d => d.IndexedAt.HasValue)
                .Select(d => d.IndexedAt!.Value)
                .DefaultIfEmpty()
                .Max();

            var statusCounts = documents
                .GroupBy(d => d.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            var statistics = new IndexStatistics
            {
                DocumentCount = totalDocuments,
                ChunkCount = totalChunks,
                StorageSizeBytes = totalSize,
                StatusCounts = statusCounts,
                LastIndexedAt = lastIndexed == default ? null : lastIndexed
            };

            _logger.LogInformation(
                "[IndexStatusService] Statistics: {DocumentCount} documents, {ChunkCount} chunks, {Size}",
                statistics.DocumentCount,
                statistics.ChunkCount,
                statistics.StorageSizeDisplay);

            return statistics;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[IndexStatusService] Failed to calculate index statistics");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RefreshStaleStatusAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[IndexStatusService] Refreshing stale status for indexed documents");

        try
        {
            var indexedDocs = await _documentRepository.GetByStatusAsync(DocumentStatus.Indexed, ct);
            var indexedList = indexedDocs.ToList();
            var staleCount = 0;

            _logger.LogDebug(
                "[IndexStatusService] Checking {Count} indexed documents for staleness",
                indexedList.Count);

            foreach (var doc in indexedList)
            {
                ct.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(doc.Hash))
                {
                    _logger.LogWarning(
                        "[IndexStatusService] Document {DocumentId} has no stored hash, skipping stale check",
                        doc.Id);
                    continue;
                }

                // Get current file metadata
                var metadata = _fileHashService.GetMetadata(doc.FilePath);

                if (!metadata.Exists)
                {
                    _logger.LogWarning(
                        "[IndexStatusService] Document {DocumentId} file no longer exists: {FilePath}",
                        doc.Id,
                        doc.FilePath);
                    continue;
                }

                // Check if file has changed
                var hasChanged = await _fileHashService.HasChangedAsync(
                    doc.FilePath,
                    doc.Hash,
                    metadata.Size,
                    metadata.LastModified,
                    ct);

                if (hasChanged)
                {
                    _logger.LogInformation(
                        "[IndexStatusService] Document {DocumentId} is stale, marking for re-indexing",
                        doc.Id);

                    await _documentRepository.UpdateStatusAsync(doc.Id, DocumentStatus.Stale, null, ct);
                    staleCount++;
                }
            }

            _logger.LogInformation(
                "[IndexStatusService] Stale refresh complete: {StaleCount} of {TotalCount} documents marked stale",
                staleCount,
                indexedList.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[IndexStatusService] Failed to refresh stale status");
            throw;
        }
    }

    /// <summary>
    /// Builds an <see cref="IndexedDocumentInfo"/> from a <see cref="Document"/>.
    /// </summary>
    private async Task<IndexedDocumentInfo> BuildDocumentInfoAsync(Document document, CancellationToken ct)
    {
        // Get chunk count for this document
        var chunks = await _chunkRepository.GetByDocumentIdAsync(document.Id, ct);
        var chunkCount = chunks.Count();
        var estimatedSize = (long)chunkCount * AverageChunkSizeBytes;

        return new IndexedDocumentInfo
        {
            Id = document.Id,
            FilePath = document.FilePath,
            Status = MapDocumentStatus(document.Status),
            ChunkCount = chunkCount,
            IndexedAt = document.IndexedAt,
            FileHash = document.Hash,
            ErrorMessage = document.FailureReason,
            EstimatedSizeBytes = estimatedSize
        };
    }

    /// <summary>
    /// Maps <see cref="DocumentStatus"/> to <see cref="IndexingStatus"/>.
    /// </summary>
    private static IndexingStatus MapDocumentStatus(DocumentStatus status)
    {
        return status switch
        {
            DocumentStatus.Pending => IndexingStatus.Pending,
            DocumentStatus.Indexing => IndexingStatus.Indexing,
            DocumentStatus.Indexed => IndexingStatus.Indexed,
            DocumentStatus.Failed => IndexingStatus.Failed,
            DocumentStatus.Stale => IndexingStatus.Stale,
            _ => IndexingStatus.NotIndexed
        };
    }
}
