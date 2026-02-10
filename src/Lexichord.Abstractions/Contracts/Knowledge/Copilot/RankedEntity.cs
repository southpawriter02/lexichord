// =============================================================================
// File: RankedEntity.cs
// Project: Lexichord.Abstractions
// Description: Entity with relevance score for ranked context selection.
// =============================================================================
// LOGIC: Pairs a KnowledgeEntity with its computed relevance score, estimated
//   token count, and matched query terms. Used by IEntityRelevanceRanker
//   to sort entities by relevance and select within token budget.
//
// v0.6.6e: Graph Context Provider (CKVS Phase 3b)
// Dependencies: KnowledgeEntity (v0.4.5e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Entity with relevance score for ranked context selection.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RankedEntity"/> record associates a <see cref="KnowledgeEntity"/>
/// with its computed relevance score relative to a query. It is produced by
/// <see cref="IEntityRelevanceRanker.RankEntities"/> and consumed by
/// <see cref="IEntityRelevanceRanker.SelectWithinBudget"/> for budget-constrained
/// entity selection.
/// </para>
/// <para>
/// <b>Scoring:</b> The relevance score is computed from name matches (1.0),
/// type matches (0.5), and property value matches (0.3), normalized by the
/// number of query terms.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6e as part of the Graph Context Provider.
/// </para>
/// </remarks>
public record RankedEntity
{
    /// <summary>
    /// The knowledge entity being ranked.
    /// </summary>
    /// <value>A non-null entity from the knowledge graph.</value>
    public required KnowledgeEntity Entity { get; init; }

    /// <summary>
    /// Computed relevance score relative to the query.
    /// </summary>
    /// <value>
    /// A non-negative float. Higher values indicate greater relevance.
    /// Typically ranges from 0.0 to ~1.8 depending on match quality.
    /// </value>
    /// <remarks>
    /// LOGIC: Computed as (name_matches * 1.0 + type_matches * 0.5 +
    /// property_matches * 0.3) / query_term_count.
    /// </remarks>
    public float RelevanceScore { get; init; }

    /// <summary>
    /// Estimated token count for this entity when formatted.
    /// </summary>
    /// <value>
    /// A positive integer estimated using ~4 characters per token
    /// plus formatting overhead.
    /// </value>
    /// <remarks>
    /// LOGIC: Used by SelectWithinBudget to greedily pack entities
    /// into the token budget.
    /// </remarks>
    public int EstimatedTokens { get; init; }

    /// <summary>
    /// Query terms that matched this entity.
    /// </summary>
    /// <value>
    /// A list of lowercase terms from the query that matched the entity's
    /// name or properties. Empty list if no terms matched.
    /// </value>
    public IReadOnlyList<string> MatchedTerms { get; init; } = [];
}
