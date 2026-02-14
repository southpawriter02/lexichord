// =============================================================================
// File: ScoredEntity.cs
// Project: Lexichord.Abstractions
// Description: Entity with multi-signal relevance score breakdown.
// =============================================================================
// LOGIC: Pairs a KnowledgeEntity with its composite relevance score,
//   per-signal breakdown, and matched query terms. Produced by
//   IEntityRelevanceScorer and consumed by KnowledgeContextProvider
//   for budget-constrained entity selection.
//
// v0.7.2f: Entity Relevance Scorer (CKVS Phase 4a)
// Dependencies: KnowledgeEntity (v0.4.5e), RelevanceSignalScores (v0.7.2f)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Entity with multi-signal relevance score breakdown.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ScoredEntity"/> record associates a <see cref="KnowledgeEntity"/>
/// with its composite relevance score and per-signal breakdown. It is produced by
/// <see cref="IEntityRelevanceScorer.ScoreEntitiesAsync"/> and consumed by the
/// <c>KnowledgeContextProvider</c> for ranked entity selection.
/// </para>
/// <para>
/// <b>Score Composition:</b> The <see cref="Score"/> is a weighted sum of five
/// individual signals captured in <see cref="Signals"/>:
/// <list type="bullet">
///   <item>Semantic similarity (cosine): 0.35 weight</item>
///   <item>Document mentions: 0.25 weight</item>
///   <item>Type matching: 0.20 weight</item>
///   <item>Recency decay: 0.10 weight</item>
///   <item>Name matching: 0.10 weight</item>
/// </list>
/// </para>
/// <para>
/// <b>Conversion:</b> A <see cref="ScoredEntity"/> can be converted to a
/// <see cref="RankedEntity"/> for use with
/// <see cref="IEntityRelevanceRanker.SelectWithinBudget"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.2f as part of the Entity Relevance Scorer.
/// </para>
/// </remarks>
public sealed record ScoredEntity
{
    /// <summary>
    /// The knowledge entity that was scored.
    /// </summary>
    /// <value>A non-null entity from the knowledge graph.</value>
    public required KnowledgeEntity Entity { get; init; }

    /// <summary>
    /// Composite relevance score (0.0–1.0).
    /// </summary>
    /// <value>
    /// A float in the range [0.0, 1.0], computed as the weighted sum
    /// of the individual signal scores in <see cref="Signals"/>.
    /// Higher values indicate greater relevance.
    /// </value>
    /// <remarks>
    /// LOGIC: Computed as:
    /// <c>SemanticWeight * SemanticScore + MentionWeight * MentionScore +
    /// TypeWeight * TypeScore + RecencyWeight * RecencyScore +
    /// NameMatchWeight * NameMatchScore</c>, clamped to [0.0, 1.0].
    /// </remarks>
    public float Score { get; init; }

    /// <summary>
    /// Per-signal score breakdown.
    /// </summary>
    /// <value>A non-null record containing the five individual signal scores.</value>
    /// <remarks>
    /// LOGIC: Provides transparency into how the <see cref="Score"/> was
    /// computed. Useful for diagnostics, logging, and debugging relevance
    /// ranking decisions.
    /// </remarks>
    public required RelevanceSignalScores Signals { get; init; }

    /// <summary>
    /// Query terms that matched this entity's name or type.
    /// </summary>
    /// <value>
    /// A list of lowercase terms from the query that matched the entity's
    /// name or type text. Empty list if no terms matched.
    /// </value>
    /// <remarks>
    /// LOGIC: Populated during name-match scoring. Useful for highlighting
    /// matched terms in the UI and for diagnostics.
    /// </remarks>
    public IReadOnlyList<string> MatchedTerms { get; init; } = [];
}
