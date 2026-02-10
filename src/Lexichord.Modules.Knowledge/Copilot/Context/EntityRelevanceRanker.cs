// =============================================================================
// File: EntityRelevanceRanker.cs
// Project: Lexichord.Modules.Knowledge
// Description: Ranks knowledge graph entities by relevance to a query.
// =============================================================================
// LOGIC: Implements term-based entity relevance ranking for the Graph Context
//   Provider. Extracts query terms, scores entities using weighted matching
//   (name: 1.0, type: 0.5, property: 0.3), and supports greedy selection
//   within a token budget.
//
// v0.6.6e: Graph Context Provider (CKVS Phase 3b)
// Dependencies: IEntityRelevanceRanker (v0.6.6e), KnowledgeEntity (v0.4.5e)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Copilot.Context;

/// <summary>
/// Ranks knowledge graph entities by relevance to a query.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="EntityRelevanceRanker"/> implements a term-based relevance
/// scoring algorithm. Query text is split into terms and matched against
/// entity names, types, and property values with different weights.
/// </para>
/// <para>
/// <b>Scoring Algorithm:</b>
/// <list type="bullet">
///   <item>Name match: +1.0 per matched term (highest weight)</item>
///   <item>Type match: +0.5 per matched term</item>
///   <item>Property value match: +0.3 per matched term</item>
/// </list>
/// Scores are normalized by the number of query terms.
/// </para>
/// <para>
/// <b>Token Estimation:</b> Uses character-based approximation (~4 chars/token)
/// plus a 10-token overhead for formatting. This avoids a dependency on the
/// internal <c>ITokenizer</c> in <c>Lexichord.Modules.LLM</c>.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Stateless and thread-safe for concurrent use.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6e as part of the Graph Context Provider.
/// </para>
/// </remarks>
internal sealed class EntityRelevanceRanker : IEntityRelevanceRanker
{
    private readonly ILogger<EntityRelevanceRanker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityRelevanceRanker"/> class.
    /// </summary>
    /// <param name="logger">Logger for ranking operations.</param>
    public EntityRelevanceRanker(ILogger<EntityRelevanceRanker> logger)
    {
        _logger = logger;
        _logger.LogDebug("EntityRelevanceRanker initialized");
    }

    /// <inheritdoc />
    public IReadOnlyList<RankedEntity> RankEntities(
        string query,
        IReadOnlyList<KnowledgeEntity> entities)
    {
        _logger.LogDebug(
            "Ranking {EntityCount} entities against query: {Query}",
            entities.Count, query);

        var queryTerms = ExtractTerms(query);
        var ranked = new List<RankedEntity>();

        foreach (var entity in entities)
        {
            var score = CalculateRelevance(entity, queryTerms, out var matchedTerms);
            var tokens = EstimateEntityTokens(entity);

            ranked.Add(new RankedEntity
            {
                Entity = entity,
                RelevanceScore = score,
                EstimatedTokens = tokens,
                MatchedTerms = matchedTerms
            });
        }

        var result = ranked.OrderByDescending(r => r.RelevanceScore).ToList();

        _logger.LogDebug(
            "Ranked {Count} entities. Top score: {TopScore:F2}, Bottom score: {BottomScore:F2}",
            result.Count,
            result.Count > 0 ? result[0].RelevanceScore : 0,
            result.Count > 0 ? result[^1].RelevanceScore : 0);

        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<KnowledgeEntity> SelectWithinBudget(
        IReadOnlyList<RankedEntity> rankedEntities,
        int tokenBudget)
    {
        _logger.LogDebug(
            "Selecting entities within {TokenBudget} token budget from {CandidateCount} candidates",
            tokenBudget, rankedEntities.Count);

        var selected = new List<KnowledgeEntity>();
        var usedTokens = 0;

        foreach (var ranked in rankedEntities)
        {
            if (usedTokens + ranked.EstimatedTokens > tokenBudget)
            {
                _logger.LogDebug(
                    "Token budget exceeded at entity {EntityName} ({EstimatedTokens} tokens, {UsedTokens}/{TokenBudget} used)",
                    ranked.Entity.Name, ranked.EstimatedTokens, usedTokens, tokenBudget);
                break;
            }

            selected.Add(ranked.Entity);
            usedTokens += ranked.EstimatedTokens;
        }

        _logger.LogDebug(
            "Selected {SelectedCount}/{TotalCount} entities using {UsedTokens}/{TokenBudget} tokens",
            selected.Count, rankedEntities.Count, usedTokens, tokenBudget);

        return selected;
    }

    /// <summary>
    /// Calculates the relevance score of an entity against query terms.
    /// </summary>
    /// <param name="entity">The entity to score.</param>
    /// <param name="queryTerms">Extracted query terms.</param>
    /// <param name="matchedTerms">Output list of terms that matched.</param>
    /// <returns>Normalized relevance score (0.0 to ~1.8).</returns>
    /// <remarks>
    /// LOGIC: Three-tier weighted matching:
    ///   - Name match: 1.0 per term (highest priority — names are most descriptive)
    ///   - Type match: 0.5 per term (entity type relevance)
    ///   - Property value match: 0.3 per term (content relevance)
    /// Normalized by query term count to ensure scores are comparable
    /// across queries of different lengths.
    /// </remarks>
    private static float CalculateRelevance(
        KnowledgeEntity entity,
        IReadOnlyList<string> queryTerms,
        out List<string> matchedTerms)
    {
        matchedTerms = new List<string>();
        var score = 0f;

        // Name match (highest weight)
        foreach (var term in queryTerms)
        {
            if (entity.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 1.0f;
                matchedTerms.Add(term);
            }
        }

        // Type match
        foreach (var term in queryTerms)
        {
            if (entity.Type.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 0.5f;
            }
        }

        // Property value match
        foreach (var prop in entity.Properties)
        {
            foreach (var term in queryTerms)
            {
                if (prop.Value?.ToString()?.Contains(term, StringComparison.OrdinalIgnoreCase) == true)
                {
                    score += 0.3f;
                    if (!matchedTerms.Contains(term)) matchedTerms.Add(term);
                }
            }
        }

        // Normalize by number of query terms
        return queryTerms.Count > 0 ? score / queryTerms.Count : 0;
    }

    /// <summary>
    /// Extracts search terms from a query string.
    /// </summary>
    /// <param name="query">The raw query text.</param>
    /// <returns>A list of lowercase terms longer than 2 characters.</returns>
    /// <remarks>
    /// LOGIC: Splits on whitespace and common delimiters, filters terms
    /// shorter than 3 characters (too common to be meaningful), and
    /// deduplicates for efficient matching.
    /// </remarks>
    internal static IReadOnlyList<string> ExtractTerms(string query)
    {
        return query.ToLowerInvariant()
            .Split([' ', ',', '.', '/', '-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Estimates the token count for a single entity when formatted.
    /// </summary>
    /// <param name="entity">The entity to estimate.</param>
    /// <returns>Estimated token count.</returns>
    /// <remarks>
    /// LOGIC: Uses ~4 characters per token heuristic plus 10-token overhead
    /// for formatting markup (headers, bullet points, etc.). This avoids
    /// depending on the internal ITokenizer in Lexichord.Modules.LLM.
    /// </remarks>
    private static int EstimateEntityTokens(KnowledgeEntity entity)
    {
        var totalChars = entity.Name.Length + entity.Type.Length;
        foreach (var prop in entity.Properties)
        {
            totalChars += prop.Key.Length + (prop.Value?.ToString()?.Length ?? 0);
        }
        return totalChars / 4 + 10; // Add overhead for formatting
    }
}
