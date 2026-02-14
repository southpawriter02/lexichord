// =============================================================================
// File: EntityRelevanceScorer.cs
// Project: Lexichord.Modules.Knowledge
// Description: Multi-signal entity relevance scorer implementation.
// =============================================================================
// LOGIC: Implements the five-signal entity relevance scoring algorithm:
//   1. Semantic similarity (35%): Cosine similarity via IEmbeddingService
//   2. Document mentions (25%): Entity name frequency in document content
//   3. Type matching (20%): Preferred entity type boosting
//   4. Recency (10%): Linear decay based on ModifiedAt
//   5. Name matching (10%): Query term presence in entity name
//
// When IEmbeddingService is unavailable (null or throws), the semantic
// weight is redistributed proportionally to the remaining four signals.
//
// v0.7.2f: Entity Relevance Scorer (CKVS Phase 4a)
// Dependencies: IEmbeddingService (v0.4.4a, optional), ScoringConfig (v0.7.2f),
//               KnowledgeEntity (v0.4.5e)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.Knowledge.Copilot.Context.Scoring;

/// <summary>
/// Multi-signal entity relevance scorer implementation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="EntityRelevanceScorer"/> scores knowledge graph entities
/// using five weighted signals to produce a composite relevance score in
/// the range [0.0, 1.0]. It extends the existing term-based
/// <see cref="EntityRelevanceRanker"/> with semantic similarity,
/// document mention counting, and recency decay.
/// </para>
/// <para>
/// <b>Scoring Algorithm:</b>
/// <code>
/// FINAL_SCORE = 0.35 × SEMANTIC + 0.25 × MENTION + 0.20 × TYPE + 0.10 × RECENCY + 0.10 × NAME
/// </code>
/// </para>
/// <para>
/// <b>Embedding Fallback:</b> The <see cref="IEmbeddingService"/> dependency
/// is optional (nullable). When null or when embedding calls fail, the semantic
/// signal returns 0.0 and its weight is redistributed proportionally:
/// <list type="bullet">
///   <item>Mention: 0.25 → ~0.385</item>
///   <item>Type: 0.20 → ~0.308</item>
///   <item>Recency: 0.10 → ~0.154</item>
///   <item>Name: 0.10 → ~0.154</item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Stateless and thread-safe for concurrent use.
/// Configuration is immutable and read at construction.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.2f as part of the Entity Relevance Scorer.
/// </para>
/// </remarks>
internal sealed class EntityRelevanceScorer : IEntityRelevanceScorer
{
    // =========================================================================
    // Delimiters for term extraction, matching EntityRelevanceRanker pattern
    // =========================================================================
    private static readonly char[] TermDelimiters =
        [' ', ',', '.', '/', '-', '_', ':', ';', '(', ')', '[', ']'];

    // =========================================================================
    // Dependencies
    // =========================================================================
    private readonly IEmbeddingService? _embeddingService;
    private readonly ScoringConfig _config;
    private readonly ILogger<EntityRelevanceScorer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityRelevanceScorer"/> class.
    /// </summary>
    /// <param name="config">
    /// Scoring configuration with signal weights and parameters.
    /// </param>
    /// <param name="logger">Logger for scoring operations.</param>
    /// <param name="embeddingService">
    /// Optional embedding service for semantic similarity. When <c>null</c>,
    /// the semantic signal is disabled and its weight is redistributed.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="config"/> or <paramref name="logger"/> is null.
    /// </exception>
    public EntityRelevanceScorer(
        IOptions<ScoringConfig> config,
        ILogger<EntityRelevanceScorer> logger,
        IEmbeddingService? embeddingService = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);

        _config = config.Value;
        _logger = logger;
        _embeddingService = embeddingService;

        _logger.LogDebug(
            "EntityRelevanceScorer initialized. EmbeddingService={EmbeddingAvailable}, " +
            "Weights=[Semantic={SemanticWeight}, Mention={MentionWeight}, Type={TypeWeight}, " +
            "Recency={RecencyWeight}, NameMatch={NameMatchWeight}]",
            _embeddingService is not null,
            _config.SemanticWeight,
            _config.MentionWeight,
            _config.TypeWeight,
            _config.RecencyWeight,
            _config.NameMatchWeight);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScoredEntity>> ScoreEntitiesAsync(
        ScoringRequest request,
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(entities);

        if (entities.Count == 0)
        {
            _logger.LogDebug("ScoreEntitiesAsync called with empty entity list, returning empty");
            return [];
        }

        _logger.LogDebug(
            "ScoreEntitiesAsync called with {EntityCount} entities, Query={Query}",
            entities.Count, request.Query);

        // Pre-compute query terms for name matching
        var queryTerms = ExtractTerms(request.Query);

        _logger.LogDebug("Extracted {TermCount} query terms: [{Terms}]",
            queryTerms.Count, string.Join(", ", queryTerms));

        // Attempt batch embedding computation
        float[]? queryEmbedding = null;
        IReadOnlyList<float[]>? entityEmbeddings = null;
        bool embeddingsAvailable = false;

        if (_embeddingService is not null)
        {
            (queryEmbedding, entityEmbeddings) = await TryComputeEmbeddingsAsync(
                request.Query, entities, ct);
            embeddingsAvailable = queryEmbedding is not null && entityEmbeddings is not null;
        }

        _logger.LogDebug(
            "Embeddings available: {Available}. Scoring {Count} entities",
            embeddingsAvailable, entities.Count);

        // Resolve effective preferred types: request overrides config
        var effectivePreferredTypes = request.PreferredEntityTypes ?? _config.PreferredTypes;

        // Score each entity
        var scored = new List<ScoredEntity>(entities.Count);

        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];

            // Compute individual signal scores
            var signals = ComputeSignals(
                entity,
                queryTerms,
                request.DocumentContent,
                effectivePreferredTypes,
                embeddingsAvailable ? queryEmbedding! : null,
                embeddingsAvailable ? entityEmbeddings![i] : null);

            // Compute weighted final score
            var score = ComputeFinalScore(signals, embeddingsAvailable);

            // Find matched terms for diagnostics
            var matchedTerms = FindMatchedTerms(entity, queryTerms);

            scored.Add(new ScoredEntity
            {
                Entity = entity,
                Score = score,
                Signals = signals,
                MatchedTerms = matchedTerms
            });
        }

        // Sort by descending composite score
        var result = scored.OrderByDescending(s => s.Score).ToList();

        _logger.LogDebug(
            "Scored {Count} entities. Top={TopScore:F3}, Bottom={BottomScore:F3}, " +
            "EmbeddingsUsed={EmbeddingsUsed}",
            result.Count,
            result.Count > 0 ? result[0].Score : 0f,
            result.Count > 0 ? result[^1].Score : 0f,
            embeddingsAvailable);

        return result;
    }

    /// <inheritdoc />
    public async Task<ScoredEntity> ScoreEntityAsync(
        ScoringRequest request,
        KnowledgeEntity entity,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(entity);

        var results = await ScoreEntitiesAsync(request, [entity], ct);
        return results[0];
    }

    // =========================================================================
    // Embedding Computation
    // =========================================================================

    /// <summary>
    /// Attempts to compute query and entity embeddings in batch.
    /// </summary>
    /// <param name="query">The query text to embed.</param>
    /// <param name="entities">The entities to embed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A tuple of (queryEmbedding, entityEmbeddings). Both are null if
    /// the embedding service fails or is unavailable.
    /// </returns>
    /// <remarks>
    /// LOGIC: Batches the query and all entity texts into a single
    /// EmbedBatchAsync call for efficiency. The first result is the
    /// query embedding; remaining results are entity embeddings.
    /// On failure, logs a warning and returns nulls (graceful fallback).
    /// </remarks>
    private async Task<(float[]? QueryEmbedding, IReadOnlyList<float[]>? EntityEmbeddings)>
        TryComputeEmbeddingsAsync(
            string query,
            IReadOnlyList<KnowledgeEntity> entities,
            CancellationToken ct)
    {
        try
        {
            // Build batch: query first, then entity texts
            var texts = new List<string>(entities.Count + 1) { query };

            foreach (var entity in entities)
            {
                // Compose entity text: name + type + property values
                var propertyValues = string.Join(" ",
                    entity.Properties.Values
                        .Select(v => v?.ToString() ?? "")
                        .Where(v => v.Length > 0));

                var entityText = $"{entity.Name} {entity.Type} {propertyValues}";
                texts.Add(entityText);
            }

            _logger.LogDebug(
                "Computing batch embeddings for {Count} texts (1 query + {EntityCount} entities)",
                texts.Count, entities.Count);

            var embeddings = await _embeddingService!.EmbedBatchAsync(texts, ct);

            // First embedding is the query; rest are entities
            var queryEmbedding = embeddings[0];
            var entityEmbeddings = embeddings.Skip(1).ToList();

            _logger.LogDebug(
                "Batch embeddings computed successfully. Dimensions={Dimensions}",
                queryEmbedding.Length);

            return (queryEmbedding, entityEmbeddings);
        }
        catch (OperationCanceledException)
        {
            // Respect cancellation — rethrow
            throw;
        }
        catch (Exception ex)
        {
            // Graceful fallback: log and return nulls
            _logger.LogWarning(ex,
                "Embedding computation failed. Falling back to non-semantic scoring. Error: {Message}",
                ex.Message);

            return (null, null);
        }
    }

    // =========================================================================
    // Signal Computation
    // =========================================================================

    /// <summary>
    /// Computes all five signal scores for an entity.
    /// </summary>
    /// <param name="entity">The entity to score.</param>
    /// <param name="queryTerms">Pre-extracted query terms.</param>
    /// <param name="documentContent">Optional document content for mentions.</param>
    /// <param name="preferredTypes">Optional preferred entity types.</param>
    /// <param name="queryEmbedding">Query embedding vector (null if unavailable).</param>
    /// <param name="entityEmbedding">Entity embedding vector (null if unavailable).</param>
    /// <returns>A <see cref="RelevanceSignalScores"/> with all five signals.</returns>
    private RelevanceSignalScores ComputeSignals(
        KnowledgeEntity entity,
        IReadOnlyList<string> queryTerms,
        string? documentContent,
        IReadOnlySet<string>? preferredTypes,
        float[]? queryEmbedding,
        float[]? entityEmbedding)
    {
        return new RelevanceSignalScores
        {
            SemanticScore = ComputeSemanticScore(queryEmbedding, entityEmbedding),
            MentionScore = ComputeMentionScore(entity, documentContent),
            TypeScore = ComputeTypeScore(entity, preferredTypes),
            RecencyScore = ComputeRecencyScore(entity),
            NameMatchScore = ComputeNameMatchScore(entity, queryTerms)
        };
    }

    /// <summary>
    /// Computes the semantic similarity score between query and entity embeddings.
    /// </summary>
    /// <param name="queryEmbedding">The query embedding vector.</param>
    /// <param name="entityEmbedding">The entity embedding vector.</param>
    /// <returns>Cosine similarity in [0.0, 1.0], or 0.0 if embeddings unavailable.</returns>
    /// <remarks>
    /// LOGIC: Delegates to <see cref="CosineSimilarity.Compute"/> for the
    /// mathematical computation. Returns 0.0 when either embedding is null.
    /// </remarks>
    private static float ComputeSemanticScore(float[]? queryEmbedding, float[]? entityEmbedding)
    {
        if (queryEmbedding is null || entityEmbedding is null)
        {
            return 0.0f;
        }

        return CosineSimilarity.Compute(queryEmbedding, entityEmbedding);
    }

    /// <summary>
    /// Computes the document mention score for an entity.
    /// </summary>
    /// <param name="entity">The entity to check for mentions.</param>
    /// <param name="documentContent">The document content (null = no mentions).</param>
    /// <returns>Mention score in [0.0, 1.0], saturating at 5 mentions.</returns>
    /// <remarks>
    /// LOGIC: Counts exact name occurrences (case-insensitive) in the
    /// document content plus term-level matches between entity name terms
    /// and document terms. Normalized by dividing by 5.0 and clamping to 1.0.
    /// Returns 0.0 when document content is null or empty.
    /// </remarks>
    private static float ComputeMentionScore(KnowledgeEntity entity, string? documentContent)
    {
        if (string.IsNullOrEmpty(documentContent))
        {
            return 0.0f;
        }

        var mentionCount = 0;

        // Count exact name mentions (case-insensitive)
        var index = 0;
        while ((index = documentContent.IndexOf(
            entity.Name, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            mentionCount++;
            index += entity.Name.Length;
        }

        // Count term-level matches between entity name terms and document terms
        var entityTerms = ExtractTerms(entity.Name);
        var documentTerms = ExtractTerms(documentContent);

        var termMatches = entityTerms.Count(t => documentTerms.Contains(t));
        mentionCount += termMatches;

        // Normalize: 5+ mentions = max score
        return Math.Min(mentionCount / 5.0f, 1.0f);
    }

    /// <summary>
    /// Computes the type matching score for an entity.
    /// </summary>
    /// <param name="entity">The entity to check type for.</param>
    /// <param name="preferredTypes">The set of preferred entity types.</param>
    /// <returns>
    /// 1.0 if preferred type matches, 0.3 if not, 0.5 if no preferences set.
    /// </returns>
    /// <remarks>
    /// LOGIC: When preferred types are configured, entities with matching
    /// types receive a strong boost (1.0) while non-matching types receive
    /// a reduced score (0.3). When no preferences are set, all types
    /// receive a neutral score (0.5).
    /// </remarks>
    private static float ComputeTypeScore(
        KnowledgeEntity entity,
        IReadOnlySet<string>? preferredTypes)
    {
        if (preferredTypes is null || preferredTypes.Count == 0)
        {
            return 0.5f; // Neutral — no preference
        }

        return preferredTypes.Contains(entity.Type) ? 1.0f : 0.3f;
    }

    /// <summary>
    /// Computes the recency score based on entity modification date.
    /// </summary>
    /// <param name="entity">The entity to compute recency for.</param>
    /// <returns>Linear decay score in [0.0, 1.0].</returns>
    /// <remarks>
    /// LOGIC: Linear decay from 1.0 (modified today) to 0.0 (modified
    /// RecencyDecayDays ago or older). Uses DateTimeOffset.UtcNow to
    /// match the KnowledgeEntity.ModifiedAt type.
    /// </remarks>
    private float ComputeRecencyScore(KnowledgeEntity entity)
    {
        var daysOld = (DateTimeOffset.UtcNow - entity.ModifiedAt).TotalDays;
        var decay = Math.Max(0.0, 1.0 - daysOld / _config.RecencyDecayDays);
        return (float)decay;
    }

    /// <summary>
    /// Computes the name matching score between query terms and entity name.
    /// </summary>
    /// <param name="entity">The entity to match against.</param>
    /// <param name="queryTerms">Pre-extracted query terms.</param>
    /// <returns>Name match score in [0.0, 1.0].</returns>
    /// <remarks>
    /// LOGIC: Two-tier matching:
    /// <list type="bullet">
    ///   <item>Substring match (entity name contains term): +2 points</item>
    ///   <item>Term-level match (entity name terms contain term): +1 point</item>
    /// </list>
    /// Normalized by (queryTerms.Count * 2) and clamped to 1.0.
    /// </remarks>
    private static float ComputeNameMatchScore(
        KnowledgeEntity entity,
        IReadOnlyList<string> queryTerms)
    {
        if (queryTerms.Count == 0)
        {
            return 0.0f;
        }

        var entityNameLower = entity.Name.ToLowerInvariant();
        var entityTerms = ExtractTerms(entity.Name);
        var matchCount = 0;

        foreach (var term in queryTerms)
        {
            // Substring match: entity name directly contains the term
            if (entityNameLower.Contains(term))
            {
                matchCount += 2;
            }
            // Term-level match: entity name, when split into terms, contains the term
            else if (entityTerms.Contains(term))
            {
                matchCount += 1;
            }
        }

        return Math.Min(matchCount / (float)(queryTerms.Count * 2), 1.0f);
    }

    // =========================================================================
    // Score Composition
    // =========================================================================

    /// <summary>
    /// Computes the final weighted composite score from individual signals.
    /// </summary>
    /// <param name="signals">The per-signal scores.</param>
    /// <param name="embeddingsAvailable">
    /// Whether embeddings were available. When <c>false</c>, the semantic
    /// weight is redistributed proportionally to other signals.
    /// </param>
    /// <returns>Composite score clamped to [0.0, 1.0].</returns>
    /// <remarks>
    /// LOGIC: When embeddings are available, uses configured weights directly.
    /// When embeddings are unavailable, redistributes the semantic weight
    /// proportionally to the four non-semantic signals to maintain a
    /// normalized score:
    /// <code>
    /// redistributionFactor = 1.0 / (1.0 - semanticWeight)
    /// effectiveWeight[signal] = configWeight[signal] * redistributionFactor
    /// </code>
    /// The semantic signal is excluded from the sum in this case.
    /// </remarks>
    private float ComputeFinalScore(RelevanceSignalScores signals, bool embeddingsAvailable)
    {
        float score;

        if (embeddingsAvailable)
        {
            // Standard weighted sum with all five signals
            score =
                signals.SemanticScore * _config.SemanticWeight +
                signals.MentionScore * _config.MentionWeight +
                signals.TypeScore * _config.TypeWeight +
                signals.RecencyScore * _config.RecencyWeight +
                signals.NameMatchScore * _config.NameMatchWeight;
        }
        else
        {
            // Redistribute semantic weight proportionally to remaining signals
            var nonSemanticWeight =
                _config.MentionWeight + _config.TypeWeight +
                _config.RecencyWeight + _config.NameMatchWeight;

            if (nonSemanticWeight > 0f)
            {
                var redistributionFactor = 1.0f / nonSemanticWeight;

                score =
                    signals.MentionScore * _config.MentionWeight * redistributionFactor +
                    signals.TypeScore * _config.TypeWeight * redistributionFactor +
                    signals.RecencyScore * _config.RecencyWeight * redistributionFactor +
                    signals.NameMatchScore * _config.NameMatchWeight * redistributionFactor;
            }
            else
            {
                score = 0f;
            }
        }

        return Math.Clamp(score, 0.0f, 1.0f);
    }

    // =========================================================================
    // Term Extraction
    // =========================================================================

    /// <summary>
    /// Finds query terms that match the entity's name or type text.
    /// </summary>
    /// <param name="entity">The entity to match against.</param>
    /// <param name="queryTerms">Pre-extracted query terms.</param>
    /// <returns>List of matched terms (lowercase).</returns>
    private static IReadOnlyList<string> FindMatchedTerms(
        KnowledgeEntity entity,
        IReadOnlyList<string> queryTerms)
    {
        var entityText = $"{entity.Name} {entity.Type}".ToLowerInvariant();
        return queryTerms.Where(t => entityText.Contains(t)).ToList();
    }

    /// <summary>
    /// Extracts search terms from a text string.
    /// </summary>
    /// <param name="text">The raw text to split into terms.</param>
    /// <returns>
    /// A list of lowercase, distinct terms longer than 2 characters.
    /// </returns>
    /// <remarks>
    /// LOGIC: Reuses the same term extraction pattern as
    /// <see cref="EntityRelevanceRanker.ExtractTerms"/>: splits on
    /// whitespace and common delimiters, filters terms shorter than
    /// 3 characters (too common to be meaningful), and deduplicates.
    /// </remarks>
    internal static IReadOnlyList<string> ExtractTerms(string text)
    {
        return text.ToLowerInvariant()
            .Split(TermDelimiters, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .Distinct()
            .ToList();
    }
}
