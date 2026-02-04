// =============================================================================
// File: SimilarityDetector.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of ISimilarityDetector for chunk deduplication.
// =============================================================================
// VERSION: v0.5.9a (Similarity Detection Infrastructure)
// LOGIC: Queries pgvector via IChunkRepository to find semantically similar chunks.
//   - Uses SearchSimilarAsync for vector similarity queries.
//   - Filters self-matches and applies configurable threshold.
//   - Batch processing with configurable batch size for throughput optimization.
//   - Comprehensive structured logging for observability.
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Implementation of <see cref="ISimilarityDetector"/> for detecting similar chunks.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9a as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This service leverages the existing <see cref="IChunkRepository.SearchSimilarAsync"/>
/// method to find chunks with similar embeddings. Results are filtered to exclude
/// self-matches and optionally same-document matches based on configuration.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This service is thread-safe and stateless. Multiple concurrent
/// calls are supported and will execute independent queries.
/// </para>
/// <para>
/// <b>Performance:</b> The batch method processes chunks in configurable batch sizes
/// to balance memory usage with database connection efficiency. For large document
/// sets, use <see cref="FindSimilarBatchAsync"/> over repeated single calls.
/// </para>
/// </remarks>
public sealed class SimilarityDetector : ISimilarityDetector
{
    private readonly IChunkRepository _chunkRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly SimilarityDetectorOptions _options;
    private readonly ILogger<SimilarityDetector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimilarityDetector"/> class.
    /// </summary>
    /// <param name="chunkRepository">Repository for chunk similarity queries.</param>
    /// <param name="documentRepository">Repository for document path lookups.</param>
    /// <param name="options">Configuration options for similarity detection.</param>
    /// <param name="logger">Logger for structured diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public SimilarityDetector(
        IChunkRepository chunkRepository,
        IDocumentRepository documentRepository,
        IOptions<SimilarityDetectorOptions> options,
        ILogger<SimilarityDetector> logger)
    {
        _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug(
            "SimilarityDetector initialized with threshold={Threshold}, maxResults={MaxResults}, batchSize={BatchSize}",
            _options.SimilarityThreshold,
            _options.MaxResultsPerChunk,
            _options.BatchSize);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SimilarChunkResult>> FindSimilarAsync(
        Chunk chunk,
        SimilarityDetectorOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        if (chunk.Embedding is null)
        {
            throw new ArgumentException("Chunk must have an embedding for similarity detection.", nameof(chunk));
        }

        var effectiveOptions = options ?? _options;

        _logger.LogDebug(
            "Finding similar chunks for ChunkId={ChunkId}, Threshold={Threshold}",
            chunk.Id,
            effectiveOptions.SimilarityThreshold);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // LOGIC: Query repository for similar chunks. Request more than needed to account
            // for filtering (self-match, same-document if configured).
            var searchResults = await _chunkRepository.SearchSimilarAsync(
                chunk.Embedding,
                topK: effectiveOptions.MaxResultsPerChunk + 5, // Buffer for filtering
                threshold: effectiveOptions.SimilarityThreshold,
                projectId: null, // Search across all projects
                cancellationToken: cancellationToken);

            // LOGIC: Convert search results to SimilarChunkResult, filtering self-matches
            // and optionally same-document matches.
            var results = new List<SimilarChunkResult>();

            foreach (var result in searchResults)
            {
                // LOGIC: Skip self-match (same chunk ID).
                if (result.Chunk.Id == chunk.Id)
                {
                    _logger.LogTrace("Skipping self-match for ChunkId={ChunkId}", chunk.Id);
                    continue;
                }

                // LOGIC: Skip same-document matches if configured.
                var isSameDocument = result.Chunk.DocumentId == chunk.DocumentId;
                if (effectiveOptions.ExcludeSameDocument && isSameDocument)
                {
                    _logger.LogTrace(
                        "Skipping same-document match: SourceChunk={SourceId}, MatchedChunk={MatchedId}",
                        chunk.Id,
                        result.Chunk.Id);
                    continue;
                }

                // LOGIC: Lookup document path for the matched chunk.
                string? documentPath = null;
                var document = await _documentRepository.GetByIdAsync(result.Chunk.DocumentId, cancellationToken);
                if (document is not null)
                {
                    documentPath = document.FilePath;
                }

                results.Add(new SimilarChunkResult(
                    SourceChunkId: chunk.Id,
                    MatchedChunkId: result.Chunk.Id,
                    SimilarityScore: result.SimilarityScore,
                    MatchedChunkContent: result.Chunk.Content,
                    MatchedDocumentPath: documentPath,
                    MatchedChunkIndex: result.Chunk.ChunkIndex)
                {
                    IsCrossDocumentMatch = !isSameDocument
                });

                // LOGIC: Stop once we have enough results.
                if (results.Count >= effectiveOptions.MaxResultsPerChunk)
                {
                    break;
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Found {Count} similar chunks for ChunkId={ChunkId} in {ElapsedMs}ms",
                results.Count,
                chunk.Id,
                stopwatch.ElapsedMilliseconds);

            return results;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Error finding similar chunks for ChunkId={ChunkId} after {ElapsedMs}ms",
                chunk.Id,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SimilarChunkResult>> FindSimilarBatchAsync(
        IEnumerable<Chunk> chunks,
        SimilarityDetectorOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        var effectiveOptions = options ?? _options;
        var chunkList = chunks.ToList();

        if (chunkList.Count == 0)
        {
            _logger.LogDebug("FindSimilarBatchAsync called with empty chunk collection");
            return Array.Empty<SimilarChunkResult>();
        }

        _logger.LogInformation(
            "Starting batch similarity detection for {Count} chunks, BatchSize={BatchSize}",
            chunkList.Count,
            effectiveOptions.BatchSize);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var allResults = new List<SimilarChunkResult>();
        var processedCount = 0;
        var skippedCount = 0;

        // LOGIC: Process chunks in batches for optimal database utilization.
        var batches = chunkList
            .Select((chunk, index) => new { chunk, index })
            .GroupBy(x => x.index / effectiveOptions.BatchSize)
            .Select(g => g.Select(x => x.chunk).ToList());

        foreach (var batch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug(
                "Processing batch of {BatchCount} chunks (total processed: {ProcessedCount})",
                batch.Count,
                processedCount);

            foreach (var chunk in batch)
            {
                // LOGIC: Skip chunks without embeddings rather than throwing.
                if (chunk.Embedding is null)
                {
                    _logger.LogWarning(
                        "Skipping chunk without embedding: ChunkId={ChunkId}",
                        chunk.Id);
                    skippedCount++;
                    continue;
                }

                try
                {
                    var results = await FindSimilarAsync(chunk, effectiveOptions, cancellationToken);
                    allResults.AddRange(results);
                    processedCount++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // LOGIC: Log error but continue processing other chunks.
                    _logger.LogError(
                        ex,
                        "Error processing chunk in batch: ChunkId={ChunkId}",
                        chunk.Id);
                }
            }
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "Batch similarity detection complete: {ProcessedCount} processed, {SkippedCount} skipped, " +
            "{MatchCount} matches found in {ElapsedMs}ms",
            processedCount,
            skippedCount,
            allResults.Count,
            stopwatch.ElapsedMilliseconds);

        return allResults;
    }
}
