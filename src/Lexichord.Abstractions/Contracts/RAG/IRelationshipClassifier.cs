// =============================================================================
// File: IRelationshipClassifier.cs
// Project: Lexichord.Abstractions
// Description: Interface for classifying semantic relationships between chunks.
// =============================================================================
// VERSION: v0.5.9b (Relationship Classification)
// LOGIC: Defines the contract for hybrid rule-based and LLM-based classification
//   of semantic relationships between similar chunks.
//   - ClassifyAsync handles single-pair classification.
//   - ClassifyBatchAsync optimizes throughput for bulk operations.
//   - Supports caching, configurable thresholds, and optional LLM fallback.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Service interface for classifying the semantic relationship between similar chunks.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9b as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This interface provides relationship classification for chunk pairs identified
/// by <see cref="ISimilarityDetector"/>. It uses a hybrid approach:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Rule-based fast-path:</b> High-confidence classification for similarity >= 0.95.
///     Target latency: &lt; 5ms.
///   </description></item>
///   <item><description>
///     <b>LLM-based classification:</b> Nuanced analysis for ambiguous cases
///     (0.80 &lt;= similarity &lt; 0.95) when enabled.
///   </description></item>
///   <item><description>
///     <b>Caching:</b> Results are cached by chunk pair IDs to minimize
///     redundant LLM calls.
///   </description></item>
/// </list>
/// <para>
/// <b>Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Deduplication:</b> Determine whether similar chunks should be merged,
///     linked, or flagged for review.
///   </description></item>
///   <item><description>
///     <b>Knowledge consistency:</b> Identify contradictory information across
///     the indexed knowledge base.
///   </description></item>
///   <item><description>
///     <b>Version management:</b> Detect when newer content supersedes older.
///   </description></item>
/// </list>
/// <para>
/// <b>License:</b> Requires Writer Pro tier. Unlicensed users receive
/// <see cref="RelationshipType.Unknown"/> with zero confidence.
/// </para>
/// </remarks>
public interface IRelationshipClassifier
{
    /// <summary>
    /// Classifies the semantic relationship between two chunks.
    /// </summary>
    /// <param name="chunkA">The first chunk in the pair.</param>
    /// <param name="chunkB">The second chunk in the pair.</param>
    /// <param name="similarityScore">
    /// The similarity score between the chunks (0.0 to 1.0).
    /// Typically provided by <see cref="ISimilarityDetector"/>.
    /// </param>
    /// <param name="options">
    /// Optional configuration for classification behavior.
    /// Defaults to <see cref="ClassificationOptions.Default"/> if not provided.
    /// </param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>
    /// The classification result containing relationship type, confidence,
    /// explanation, and classification method.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="chunkA"/> or <paramref name="chunkB"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Classification strategy is determined by the similarity score:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     Similarity >= <see cref="ClassificationOptions.RuleBasedThreshold"/>:
    ///     Rule-based classification.
    ///   </description></item>
    ///   <item><description>
    ///     Similarity &lt; threshold and LLM enabled: LLM classification.
    ///   </description></item>
    ///   <item><description>
    ///     LLM unavailable or disabled: Fallback to rule-based.
    ///   </description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await classifier.ClassifyAsync(chunkA, chunkB, 0.92f);
    /// switch (result.Type)
    /// {
    ///     case RelationshipType.Equivalent:
    ///         await MergeChunksAsync(chunkA, chunkB);
    ///         break;
    ///     case RelationshipType.Contradictory:
    ///         await FlagForReviewAsync(chunkA, chunkB);
    ///         break;
    /// }
    /// </code>
    /// </example>
    Task<RelationshipClassification> ClassifyAsync(
        Chunk chunkA,
        Chunk chunkB,
        float similarityScore,
        ClassificationOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Classifies the semantic relationships for multiple chunk pairs in a batch operation.
    /// </summary>
    /// <param name="pairs">
    /// The chunk pairs to classify. Each pair includes both chunks and their similarity score.
    /// </param>
    /// <param name="options">
    /// Optional configuration for classification behavior.
    /// Defaults to <see cref="ClassificationOptions.Default"/> if not provided.
    /// </param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>
    /// A list of classification results corresponding to the input pairs (same order).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pairs"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Batch processing is optimized to minimize LLM calls by:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Checking cache first for all pairs.</description></item>
    ///   <item><description>Grouping rule-based classifications together.</description></item>
    ///   <item><description>Batching LLM requests where possible.</description></item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<RelationshipClassification>> ClassifyBatchAsync(
        IReadOnlyList<ChunkPair> pairs,
        ClassificationOptions? options = null,
        CancellationToken ct = default);
}
