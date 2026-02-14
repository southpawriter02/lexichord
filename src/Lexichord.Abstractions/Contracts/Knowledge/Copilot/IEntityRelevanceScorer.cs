// =============================================================================
// File: IEntityRelevanceScorer.cs
// Project: Lexichord.Abstractions
// Description: Interface for multi-signal entity relevance scoring.
// =============================================================================
// LOGIC: Defines the contract for scoring knowledge graph entities using
//   multiple relevance signals (semantic similarity, document mentions,
//   type matching, recency, name matching). Async because semantic scoring
//   requires IEmbeddingService network calls. Complements the existing
//   IEntityRelevanceRanker (sync, term-based) with richer signal analysis.
//
// v0.7.2f: Entity Relevance Scorer (CKVS Phase 4a)
// Dependencies: KnowledgeEntity (v0.4.5e), ScoredEntity (v0.7.2f),
//               ScoringRequest (v0.7.2f)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Scores knowledge graph entities by multi-signal relevance to a query.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IEntityRelevanceScorer"/> provides async, multi-signal
/// relevance scoring for knowledge graph entities. Unlike the synchronous
/// <see cref="IEntityRelevanceRanker"/> (term-based only), this scorer
/// combines five signals:
/// </para>
/// <list type="number">
///   <item><b>Semantic similarity</b> (35%): Cosine similarity between query and entity embedding vectors.</item>
///   <item><b>Document mentions</b> (25%): Frequency of entity name in current document content.</item>
///   <item><b>Type matching</b> (20%): Preference boost for entity types matching the request.</item>
///   <item><b>Recency</b> (10%): Linear decay based on entity modification date.</item>
///   <item><b>Name matching</b> (10%): Query term presence in entity name.</item>
/// </list>
/// <para>
/// <b>Embedding Fallback:</b> When the <c>IEmbeddingService</c> is unavailable,
/// the semantic signal returns 0.0 and its weight is redistributed proportionally
/// to the remaining four signals.
/// </para>
/// <para>
/// <b>Relationship to IEntityRelevanceRanker:</b> Both interfaces coexist.
/// The ranker provides synchronous term-based scoring and budget selection.
/// The scorer provides async multi-signal scoring. The <c>KnowledgeContextProvider</c>
/// preferentially uses the scorer when available, falling back to the ranker.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe for concurrent use.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.2f as part of the Entity Relevance Scorer.
/// </para>
/// </remarks>
public interface IEntityRelevanceScorer
{
    /// <summary>
    /// Scores a batch of entities by relevance to the scoring request.
    /// </summary>
    /// <param name="request">
    /// The scoring request containing the query text, optional document content,
    /// and optional preferred entity types.
    /// </param>
    /// <param name="entities">The candidate entities to score.</param>
    /// <param name="ct">Cancellation token for operation control.</param>
    /// <returns>
    /// A task that completes with a list of <see cref="ScoredEntity"/> instances
    /// sorted by descending composite score. All input entities are included
    /// regardless of score.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Batch scoring pipeline:
    /// <list type="number">
    ///   <item>Extract query terms for name matching.</item>
    ///   <item>Compute query embedding (batch with entity embeddings for efficiency).</item>
    ///   <item>For each entity, compute five signal scores.</item>
    ///   <item>Combine signals using configured weights.</item>
    ///   <item>Sort by descending composite score.</item>
    /// </list>
    /// </para>
    /// <para>
    /// When <paramref name="entities"/> is empty, returns an empty list.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="ct"/> is cancelled.
    /// </exception>
    Task<IReadOnlyList<ScoredEntity>> ScoreEntitiesAsync(
        ScoringRequest request,
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default);

    /// <summary>
    /// Scores a single entity by relevance to the scoring request.
    /// </summary>
    /// <param name="request">
    /// The scoring request containing the query text, optional document content,
    /// and optional preferred entity types.
    /// </param>
    /// <param name="entity">The entity to score.</param>
    /// <param name="ct">Cancellation token for operation control.</param>
    /// <returns>
    /// A task that completes with a <see cref="ScoredEntity"/> containing
    /// the composite score and per-signal breakdown.
    /// </returns>
    /// <remarks>
    /// LOGIC: Convenience method that delegates to
    /// <see cref="ScoreEntitiesAsync"/> with a single-element list.
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="ct"/> is cancelled.
    /// </exception>
    Task<ScoredEntity> ScoreEntityAsync(
        ScoringRequest request,
        KnowledgeEntity entity,
        CancellationToken ct = default);
}
