// =============================================================================
// File: SimilarityDetectorOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for the similarity detection service.
// =============================================================================
// VERSION: v0.5.9a (Similarity Detection Infrastructure)
// LOGIC: Provides configurable thresholds and batch sizes for similarity queries.
//   - SimilarityThreshold defaults to 0.95 (conservative, avoiding false positives).
//   - BatchSize of 10 is optimized for pgvector query performance.
//   - MaxResultsPerChunk limits result set size for performance.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Configuration options for the similarity detection service.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9a as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// The default threshold of 0.95 is intentionally conservative to minimize false
/// positives in duplicate detection. This can be lowered for more aggressive
/// deduplication, but may require human review of flagged pairs.
/// </para>
/// <para>
/// <b>Performance Considerations:</b>
/// </para>
/// <list type="bullet">
///   <item><description>
///     Batch processing is essential for large document sets. The default batch
///     size of 10 balances memory usage with query efficiency.
///   </description></item>
///   <item><description>
///     Higher thresholds reduce result sets and improve query performance by
///     allowing pgvector to prune more candidates early.
///   </description></item>
///   <item><description>
///     MaxResultsPerChunk prevents runaway result sets when a chunk has many
///     near-duplicates (e.g., boilerplate headers).
///   </description></item>
/// </list>
/// </remarks>
public sealed class SimilarityDetectorOptions
{
    /// <summary>
    /// The minimum cosine similarity score for a chunk to be considered a duplicate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Range: [0.0, 1.0]. Default: 0.95 (conservative).
    /// </para>
    /// <para>
    /// This threshold is intentionally high to avoid false positives. Chunks below
    /// this threshold are not returned as potential duplicates. Adjust lower for
    /// more aggressive detection, higher for precision-focused detection.
    /// </para>
    /// </remarks>
    public double SimilarityThreshold { get; init; } = 0.95;

    /// <summary>
    /// The maximum number of similar chunks to return per source chunk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: 5. Range: [1, 50].
    /// </para>
    /// <para>
    /// Limits result set size for chunks that may have many near-duplicates,
    /// such as boilerplate headers, license text, or repeated code comments.
    /// The most similar matches are returned first.
    /// </para>
    /// </remarks>
    public int MaxResultsPerChunk { get; init; } = 5;

    /// <summary>
    /// The number of chunks to process in a single batch query.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: 10. Range: [1, 100].
    /// </para>
    /// <para>
    /// Controls the tradeoff between memory usage and query efficiency. Larger
    /// batches reduce database round-trips but increase memory pressure. The
    /// default of 10 is optimized for typical pgvector deployments.
    /// </para>
    /// </remarks>
    public int BatchSize { get; init; } = 10;

    /// <summary>
    /// Whether to exclude matches from the same document as the source chunk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default: false (include same-document matches).
    /// </para>
    /// <para>
    /// When enabled, only cross-document duplicates are returned. Same-document
    /// repeats are often intentional (e.g., document summaries, repeated warnings)
    /// and may not require deduplication action.
    /// </para>
    /// </remarks>
    public bool ExcludeSameDocument { get; init; } = false;

    /// <summary>
    /// Gets the default configuration options.
    /// </summary>
    /// <remarks>
    /// Uses conservative defaults optimized for precision over recall:
    /// <list type="bullet">
    ///   <item><description>SimilarityThreshold: 0.95</description></item>
    ///   <item><description>MaxResultsPerChunk: 5</description></item>
    ///   <item><description>BatchSize: 10</description></item>
    ///   <item><description>ExcludeSameDocument: false</description></item>
    /// </list>
    /// </remarks>
    public static SimilarityDetectorOptions Default { get; } = new();
}
