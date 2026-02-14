// =============================================================================
// File: KnowledgeContextProvider.cs
// Project: Lexichord.Modules.Knowledge
// Description: Provides knowledge graph context for Co-pilot prompt injection.
// =============================================================================
// LOGIC: Orchestrates the full context retrieval pipeline:
//   1. Search for relevant entities via IGraphRepository.SearchEntitiesAsync
//   2. Score/rank entities by relevance using IEntityRelevanceScorer (preferred,
//      v0.7.2f multi-signal) or IEntityRelevanceRanker (fallback, v0.6.6e
//      term-based)
//   3. Select entities within token budget
//   4. Optionally retrieve relationships, axioms, and claims
//   5. Format context using IKnowledgeContextFormatter
//
// Error handling: Returns KnowledgeContext.Empty on graph unavailability
//   or empty query results, with info-level logging.
//
// v0.6.6e: Graph Context Provider (CKVS Phase 3b)
// v0.7.2f: Entity Relevance Scorer integration (CKVS Phase 4a)
// Dependencies: IGraphRepository (v0.4.5e), IAxiomStore (v0.4.6-KG),
//               IClaimRepository (v0.5.6h), IEntityRelevanceRanker (v0.6.6e),
//               IEntityRelevanceScorer (v0.7.2f, optional),
//               IKnowledgeContextFormatter (v0.6.6e)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Lexichord.Modules.Knowledge.Axioms;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Copilot.Context;

/// <summary>
/// Provides knowledge graph context for Co-pilot prompt injection.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="KnowledgeContextProvider"/> orchestrates the full knowledge
/// retrieval pipeline, from entity search through formatting. It coordinates
/// between the graph repository (for entities and relationships), axiom store
/// (for domain rules), claim repository (for evidence), relevance ranker
/// (for smart selection), and context formatter (for output).
/// </para>
/// <para>
/// <b>Pipeline:</b>
/// <list type="number">
///   <item>Search entities via <c>IGraphRepository.SearchEntitiesAsync</c> (over-fetching 2x).</item>
///   <item>Rank entities using term-based relevance scoring.</item>
///   <item>Select within token budget (greedy).</item>
///   <item>Retrieve relationships where both endpoints are selected.</item>
///   <item>Retrieve axioms targeting the selected entity types.</item>
///   <item>Retrieve claims for grounding evidence (optional).</item>
///   <item>Format context using the configured output format.</item>
/// </list>
/// </para>
/// <para>
/// <b>Error Handling:</b>
/// <list type="bullet">
///   <item>Graph unavailable → return <see cref="KnowledgeContext.Empty"/>.</item>
///   <item>No results → return <see cref="KnowledgeContext.Empty"/>, log info.</item>
///   <item>Token budget exceeded → truncate entities, set <see cref="KnowledgeContext.WasTruncated"/>.</item>
///   <item>Invalid entity ID → skip entity, continue.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6e as part of the Graph Context Provider.
/// </para>
/// </remarks>
internal sealed class KnowledgeContextProvider : IKnowledgeContextProvider
{
    private readonly IGraphRepository _graphRepository;
    private readonly IAxiomStore _axiomStore;
    private readonly IClaimRepository _claimRepository;
    private readonly IEntityRelevanceRanker _ranker;
    private readonly IEntityRelevanceScorer? _scorer;
    private readonly IKnowledgeContextFormatter _formatter;
    private readonly ILogger<KnowledgeContextProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeContextProvider"/> class.
    /// </summary>
    /// <param name="graphRepository">The graph repository for entity and relationship queries.</param>
    /// <param name="axiomStore">The axiom store for domain rules.</param>
    /// <param name="claimRepository">The claim repository for evidence lookup.</param>
    /// <param name="ranker">The entity relevance ranker (v0.6.6e fallback).</param>
    /// <param name="formatter">The context formatter.</param>
    /// <param name="logger">Logger for provider operations.</param>
    /// <param name="scorer">
    /// Optional multi-signal entity relevance scorer (v0.7.2f). When available,
    /// preferred over the term-based ranker for entity scoring. Falls back to
    /// <paramref name="ranker"/> when null.
    /// </param>
    public KnowledgeContextProvider(
        IGraphRepository graphRepository,
        IAxiomStore axiomStore,
        IClaimRepository claimRepository,
        IEntityRelevanceRanker ranker,
        IKnowledgeContextFormatter formatter,
        ILogger<KnowledgeContextProvider> logger,
        IEntityRelevanceScorer? scorer = null)
    {
        _graphRepository = graphRepository;
        _axiomStore = axiomStore;
        _claimRepository = claimRepository;
        _ranker = ranker;
        _scorer = scorer;
        _formatter = formatter;
        _logger = logger;

        _logger.LogDebug(
            "KnowledgeContextProvider initialized. MultiSignalScorer={ScorerAvailable}",
            _scorer is not null);
    }

    /// <inheritdoc />
    public async Task<KnowledgeContext> GetContextAsync(
        string query,
        KnowledgeContextOptions options,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "GetContextAsync called with query: {Query}, MaxTokens={MaxTokens}, MaxEntities={MaxEntities}",
            query, options.MaxTokens, options.MaxEntities);

        try
        {
            // 1. Search for relevant entities (over-fetch for ranking)
            var searchResults = await _graphRepository.SearchEntitiesAsync(
                new EntitySearchQuery
                {
                    Query = query,
                    MaxResults = options.MaxEntities * 2, // Over-fetch for ranking
                    EntityTypes = options.EntityTypes
                }, ct);

            if (searchResults.Count == 0)
            {
                _logger.LogDebug("No entities found for query: {Query}", query);
                return KnowledgeContext.Empty;
            }

            _logger.LogDebug("Search returned {Count} entities", searchResults.Count);

            // 2. Score/rank by relevance
            // v0.7.2f: Prefer multi-signal scorer when available; fall back to term-based ranker
            IReadOnlyList<RankedEntity> rankedEntities;
            if (_scorer is not null)
            {
                _logger.LogDebug("Using multi-signal EntityRelevanceScorer (v0.7.2f)");

                var scoringRequest = new ScoringRequest
                {
                    Query = query,
                    DocumentContent = null, // Not available at provider level
                    PreferredEntityTypes = options.EntityTypes
                };

                var scoredEntities = await _scorer.ScoreEntitiesAsync(
                    scoringRequest, searchResults, ct);

                // Convert ScoredEntity → RankedEntity for budget selection
                rankedEntities = scoredEntities.Select(se => new RankedEntity
                {
                    Entity = se.Entity,
                    RelevanceScore = se.Score,
                    EstimatedTokens = EstimateEntityTokens(se.Entity),
                    MatchedTerms = se.MatchedTerms
                }).ToList();
            }
            else
            {
                _logger.LogDebug("Using term-based EntityRelevanceRanker (v0.6.6e fallback)");
                rankedEntities = _ranker.RankEntities(query, searchResults);
            }

            // 3. Select within token budget
            var selectedEntities = _ranker.SelectWithinBudget(
                rankedEntities, options.MaxTokens);

            _logger.LogDebug(
                "Selected {SelectedCount} of {RankedCount} entities within {TokenBudget} token budget",
                selectedEntities.Count, rankedEntities.Count, options.MaxTokens);

            // 4. Get relationships if requested
            IReadOnlyList<KnowledgeRelationship>? relationships = null;
            if (options.IncludeRelationships)
            {
                relationships = await GetRelationshipsAsync(selectedEntities, ct);
                _logger.LogDebug("Retrieved {Count} relationships", relationships.Count);
            }

            // 5. Get applicable axioms if requested
            IReadOnlyList<Abstractions.Contracts.Knowledge.Axiom>? axioms = null;
            if (options.IncludeAxioms)
            {
                axioms = GetApplicableAxioms(selectedEntities);
                _logger.LogDebug("Retrieved {Count} applicable axioms", axioms.Count);
            }

            // 6. Get claims if requested
            IReadOnlyList<Claim>? claims = null;
            if (options.IncludeClaims)
            {
                claims = await GetRelatedClaimsAsync(selectedEntities, ct);
                _logger.LogDebug("Retrieved {Count} related claims", claims.Count);
            }

            // 7. Format context
            var formatted = _formatter.FormatContext(
                selectedEntities, relationships, axioms,
                new ContextFormatOptions { Format = options.Format });

            var tokenCount = _formatter.EstimateTokens(formatted);

            _logger.LogInformation(
                "Knowledge context assembled: {EntityCount} entities, {TokenCount} tokens, truncated={Truncated}",
                selectedEntities.Count, tokenCount, rankedEntities.Count > selectedEntities.Count);

            return new KnowledgeContext
            {
                Entities = selectedEntities,
                Relationships = relationships,
                Axioms = axioms,
                Claims = claims,
                FormattedContext = formatted,
                TokenCount = tokenCount,
                OriginalQuery = query,
                WasTruncated = rankedEntities.Count > selectedEntities.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve knowledge context for query: {Query}", query);
            return KnowledgeContext.Empty;
        }
    }

    /// <inheritdoc />
    public async Task<KnowledgeContext> GetContextForEntitiesAsync(
        IReadOnlyList<Guid> entityIds,
        KnowledgeContextOptions options,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "GetContextForEntitiesAsync called with {Count} entity IDs",
            entityIds.Count);

        try
        {
            var entities = new List<KnowledgeEntity>();
            foreach (var id in entityIds.Take(options.MaxEntities))
            {
                var entity = await _graphRepository.GetByIdAsync(id, ct);
                if (entity != null)
                {
                    entities.Add(entity);
                }
                else
                {
                    _logger.LogDebug("Entity not found: {EntityId}", id);
                }
            }

            if (entities.Count == 0)
            {
                _logger.LogDebug("No valid entities found for the provided IDs");
                return KnowledgeContext.Empty;
            }

            IReadOnlyList<KnowledgeRelationship>? relationships = null;
            if (options.IncludeRelationships)
            {
                relationships = await GetRelationshipsAsync(entities, ct);
            }

            IReadOnlyList<Abstractions.Contracts.Knowledge.Axiom>? axioms = null;
            if (options.IncludeAxioms)
            {
                axioms = GetApplicableAxioms(entities);
            }

            var formatted = _formatter.FormatContext(
                entities, relationships, axioms,
                new ContextFormatOptions { Format = options.Format });

            _logger.LogInformation(
                "Entity context assembled: {EntityCount} entities, {TokenCount} tokens",
                entities.Count, _formatter.EstimateTokens(formatted));

            return new KnowledgeContext
            {
                Entities = entities,
                Relationships = relationships,
                Axioms = axioms,
                FormattedContext = formatted,
                TokenCount = _formatter.EstimateTokens(formatted)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to retrieve context for {Count} entity IDs", entityIds.Count);
            return KnowledgeContext.Empty;
        }
    }

    /// <summary>
    /// Retrieves relationships between the selected entities.
    /// </summary>
    /// <param name="entities">The selected entities.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Relationships where both endpoints are in the entity set.
    /// Deduplicated by relationship ID.
    /// </returns>
    /// <remarks>
    /// LOGIC: For each entity, retrieves all relationships and filters to
    /// those where both endpoints are in the selected set. This prevents
    /// dangling references in the context output.
    /// </remarks>
    private async Task<IReadOnlyList<KnowledgeRelationship>> GetRelationshipsAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct)
    {
        var relationships = new List<KnowledgeRelationship>();
        var entityIds = entities.Select(e => e.Id).ToHashSet();

        foreach (var entity in entities)
        {
            var rels = await _graphRepository.GetRelationshipsForEntityAsync(entity.Id, ct);
            relationships.AddRange(rels.Where(r =>
                entityIds.Contains(r.FromEntityId) ||
                entityIds.Contains(r.ToEntityId)));
        }

        // Deduplicate by relationship ID
        return relationships
            .DistinctBy(r => r.Id)
            .ToList();
    }

    /// <summary>
    /// Retrieves applicable axioms for the selected entity types.
    /// </summary>
    /// <param name="entities">The selected entities.</param>
    /// <returns>Distinct axioms targeting the entity types present.</returns>
    /// <remarks>
    /// LOGIC: Collects distinct entity types and retrieves axioms for each
    /// type from the axiom store. The axiom store's GetAxiomsForType is
    /// synchronous (backed by in-memory cache), so no async overhead.
    /// </remarks>
    private IReadOnlyList<Abstractions.Contracts.Knowledge.Axiom> GetApplicableAxioms(
        IReadOnlyList<KnowledgeEntity> entities)
    {
        var entityTypes = entities.Select(e => e.Type).Distinct();
        var axioms = new List<Abstractions.Contracts.Knowledge.Axiom>();

        foreach (var type in entityTypes)
        {
            var typeAxioms = _axiomStore.GetAxiomsForType(type);
            axioms.AddRange(typeAxioms);
        }

        return axioms
            .DistinctBy(a => a.Id)
            .ToList();
    }

    /// <summary>
    /// Retrieves related claims for the selected entities.
    /// </summary>
    /// <param name="entities">The selected entities (limited to first 5).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Top 3 claims per entity, limited to 5 entities for performance.</returns>
    /// <remarks>
    /// LOGIC: Performance-bounded claim retrieval. Limits to 5 entities and
    /// 3 claims per entity to prevent excessive token usage. Claims provide
    /// grounding evidence for LLM generation but are costly in token budget.
    /// </remarks>
    private async Task<IReadOnlyList<Claim>> GetRelatedClaimsAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct)
    {
        var claims = new List<Claim>();

        foreach (var entity in entities.Take(5)) // Limit for performance
        {
            var entityClaims = await _claimRepository.GetByEntityAsync(entity.Id, ct);
            claims.AddRange(entityClaims.Take(3)); // Top 3 per entity
        }

        return claims;
    }

    /// <summary>
    /// Estimates the token count for a single entity when formatted.
    /// </summary>
    /// <param name="entity">The entity to estimate.</param>
    /// <returns>Estimated token count.</returns>
    /// <remarks>
    /// LOGIC: Uses ~4 characters per token heuristic plus 10-token overhead
    /// for formatting markup. Matches the estimation in EntityRelevanceRanker.
    /// v0.7.2f: Needed for ScoredEntity → RankedEntity conversion when using
    /// the multi-signal scorer path.
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
