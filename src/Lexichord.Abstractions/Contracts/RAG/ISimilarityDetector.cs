// =============================================================================
// File: ISimilarityDetector.cs
// Project: Lexichord.Abstractions
// Description: Interface for detecting similar chunks in the RAG index.
// =============================================================================
// VERSION: v0.5.9a (Similarity Detection Infrastructure)
// LOGIC: Defines the contract for querying pgvector to find semantically similar chunks.
//   - FindSimilarAsync handles single-chunk queries.
//   - FindSimilarBatchAsync optimizes throughput for bulk deduplication checks.
//   - Options allow configurable thresholds and batch sizes.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Service interface for detecting semantically similar chunks in the RAG index.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9a as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This interface provides the core similarity detection capability for the deduplication
/// pipeline. It queries the pgvector index to find chunks with embeddings similar to
/// the provided source chunk(s), enabling identification of near-duplicate content
/// across the indexed knowledge base.
/// </para>
/// <para>
/// <b>Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Real-time ingestion:</b> Check new chunks against existing index during
///     document indexing to flag potential duplicates before storage.
///   </description></item>
///   <item><description>
///     <b>Batch deduplication:</b> Scan existing index to identify and consolidate
///     duplicate knowledge entries.
///   </description></item>
///   <item><description>
///     <b>Content governance:</b> Detect unintentional content duplication across
///     document collections for cleanup.
///   </description></item>
/// </list>
/// <para>
/// <b>Performance:</b> The batch method processes chunks in configurable batch sizes
/// (default: 10) to balance memory usage with query efficiency. For large-scale
/// deduplication, prefer <see cref="FindSimilarBatchAsync"/> over repeated single calls.
/// </para>
/// </remarks>
public interface ISimilarityDetector
{
    /// <summary>
    /// Finds chunks similar to the provided source chunk.
    /// </summary>
    /// <param name="chunk">
    /// The source chunk to find duplicates for. Must have a non-null
    /// <see cref="Chunk.Embedding"/> for similarity comparison.
    /// </param>
    /// <param name="options">
    /// Optional configuration for threshold and result limits.
    /// Defaults to <see cref="SimilarityDetectorOptions.Default"/> if not provided.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A list of similar chunks ordered by descending similarity score.
    /// Empty if no chunks meet the threshold. Does not include the source chunk itself.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="chunk"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="chunk"/> has a <c>null</c> embedding.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method queries the pgvector index using cosine similarity. Results are
    /// filtered by the configured threshold and limited to <see cref="SimilarityDetectorOptions.MaxResultsPerChunk"/>.
    /// </para>
    /// <para>
    /// The source chunk is automatically excluded from results to prevent self-matching.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var results = await detector.FindSimilarAsync(chunk, new SimilarityDetectorOptions
    /// {
    ///     SimilarityThreshold = 0.90 // More aggressive threshold
    /// });
    /// foreach (var match in results)
    /// {
    ///     Console.WriteLine($"Match: {match.SimilarityScore:P1} - {match.MatchedDocumentPath}");
    /// }
    /// </code>
    /// </example>
    Task<IReadOnlyList<SimilarChunkResult>> FindSimilarAsync(
        Chunk chunk,
        SimilarityDetectorOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds similar chunks for multiple source chunks in an optimized batch operation.
    /// </summary>
    /// <param name="chunks">
    /// The source chunks to find duplicates for. Each chunk must have a non-null
    /// <see cref="Chunk.Embedding"/>. Chunks with null embeddings are skipped.
    /// </param>
    /// <param name="options">
    /// Optional configuration for threshold, result limits, and batch size.
    /// Defaults to <see cref="SimilarityDetectorOptions.Default"/> if not provided.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A flattened list of all similar chunks found across all source chunks,
    /// ordered by source chunk then by descending similarity score.
    /// Empty if no chunks or no matches meet the threshold.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="chunks"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method processes chunks in batches of <see cref="SimilarityDetectorOptions.BatchSize"/>
    /// to optimize database round-trips while managing memory usage. For large-scale
    /// deduplication operations, this is significantly more efficient than repeated
    /// calls to <see cref="FindSimilarAsync"/>.
    /// </para>
    /// <para>
    /// Chunks with null embeddings are logged as warnings and skipped rather than
    /// throwing exceptions, allowing partial processing of mixed collections.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var newChunks = await indexingPipeline.ChunkDocumentAsync(document);
    /// var duplicates = await detector.FindSimilarBatchAsync(newChunks);
    /// if (duplicates.Any())
    /// {
    ///     logger.LogWarning("Found {Count} potential duplicates", duplicates.Count);
    /// }
    /// </code>
    /// </example>
    Task<IReadOnlyList<SimilarChunkResult>> FindSimilarBatchAsync(
        IEnumerable<Chunk> chunks,
        SimilarityDetectorOptions? options = null,
        CancellationToken cancellationToken = default);
}
