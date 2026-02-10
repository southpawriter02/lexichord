// =============================================================================
// File: IEntityRelevanceRanker.cs
// Project: Lexichord.Abstractions
// Description: Interface for ranking entities by relevance to a query.
// =============================================================================
// LOGIC: Defines the contract for term-based entity relevance ranking and
//   budget-constrained selection. Used by IKnowledgeContextProvider to
//   prioritize the most relevant entities for prompt injection.
//
// v0.6.6e: Graph Context Provider (CKVS Phase 3b)
// Dependencies: KnowledgeEntity (v0.4.5e), RankedEntity (v0.6.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Ranks entities by relevance to a query.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IEntityRelevanceRanker"/> provides term-based relevance
/// scoring for knowledge graph entities. It splits the query into terms
/// and computes scores based on name matches, type matches, and property
/// value matches.
/// </para>
/// <para>
/// <b>Scoring Weights:</b>
/// <list type="bullet">
///   <item>Name match: 1.0 per term</item>
///   <item>Type match: 0.5 per term</item>
///   <item>Property value match: 0.3 per term</item>
/// </list>
/// Scores are normalized by the number of query terms.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6e as part of the Graph Context Provider.
/// </para>
/// </remarks>
public interface IEntityRelevanceRanker
{
    /// <summary>
    /// Ranks entities by relevance to the query.
    /// </summary>
    /// <param name="query">The query text to match against.</param>
    /// <param name="entities">The candidate entities to rank.</param>
    /// <returns>
    /// A list of <see cref="RankedEntity"/> instances sorted by descending
    /// relevance score. All input entities are included regardless of score.
    /// </returns>
    /// <remarks>
    /// LOGIC: Extracts terms from the query (split by whitespace and
    /// common delimiters, filtered to >2 chars), then scores each entity
    /// using the weighted term matching algorithm. Results are sorted
    /// by descending relevance score.
    /// </remarks>
    IReadOnlyList<RankedEntity> RankEntities(
        string query,
        IReadOnlyList<KnowledgeEntity> entities);

    /// <summary>
    /// Selects entities that fit within the token budget.
    /// </summary>
    /// <param name="rankedEntities">Ranked entities (highest first).</param>
    /// <param name="tokenBudget">Maximum total tokens to consume.</param>
    /// <returns>
    /// A list of entities whose cumulative estimated token count does
    /// not exceed <paramref name="tokenBudget"/>. Order is preserved
    /// from the ranked input.
    /// </returns>
    /// <remarks>
    /// LOGIC: Greedy selection — entities are added in rank order until
    /// the next entity would exceed the remaining budget.
    /// </remarks>
    IReadOnlyList<KnowledgeEntity> SelectWithinBudget(
        IReadOnlyList<RankedEntity> rankedEntities,
        int tokenBudget);
}
