# LCS-DES-072-KG-f: Entity Relevance Scorer

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-072-KG-f |
| **System Breakdown** | LCS-SBD-072-KG |
| **Version** | v0.7.2 |
| **Codename** | Entity Relevance Scorer (CKVS Phase 4a) |
| **Estimated Hours** | 4 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Entity Relevance Scorer** ranks Knowledge Graph entities by their relevance to a context request. It uses multiple signals including semantic similarity, document mentions, type matching, and recency to produce relevance scores.

### 1.2 Key Responsibilities

- Score entities by relevance to query
- Combine multiple relevance signals
- Support configurable scoring weights
- Handle document-based mentions
- Optimize for performance

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Context/
      Scoring/
        IEntityRelevanceScorer.cs
        EntityRelevanceScorer.cs
        RelevanceSignals.cs
```

---

## 2. Interface Definitions

### 2.1 Entity Relevance Scorer Interface

```csharp
namespace Lexichord.KnowledgeGraph.Context.Scoring;

/// <summary>
/// Scores entities by relevance to a context request.
/// </summary>
public interface IEntityRelevanceScorer
{
    /// <summary>
    /// Scores entities for a context request.
    /// </summary>
    Task<IReadOnlyList<ScoredEntity>> ScoreEntitiesAsync(
        ContextRequest request,
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default);

    /// <summary>
    /// Scores a single entity.
    /// </summary>
    Task<ScoredEntity> ScoreEntityAsync(
        ContextRequest request,
        KnowledgeEntity entity,
        CancellationToken ct = default);
}
```

---

## 3. Data Types

### 3.1 Scored Entity

```csharp
/// <summary>
/// Entity with relevance score.
/// </summary>
public record ScoredEntity
{
    /// <summary>The entity.</summary>
    public required KnowledgeEntity Entity { get; init; }

    /// <summary>Overall relevance score (0-1).</summary>
    public float Score { get; init; }

    /// <summary>Individual signal scores.</summary>
    public required RelevanceSignalScores Signals { get; init; }

    /// <summary>Terms that matched.</summary>
    public IReadOnlyList<string> MatchedTerms { get; init; } = [];
}

/// <summary>
/// Individual signal scores.
/// </summary>
public record RelevanceSignalScores
{
    /// <summary>Semantic similarity score.</summary>
    public float SemanticScore { get; init; }

    /// <summary>Document mention score.</summary>
    public float MentionScore { get; init; }

    /// <summary>Type match score.</summary>
    public float TypeScore { get; init; }

    /// <summary>Recency score.</summary>
    public float RecencyScore { get; init; }

    /// <summary>Name match score.</summary>
    public float NameMatchScore { get; init; }
}
```

### 3.2 Scoring Configuration

```csharp
/// <summary>
/// Configuration for relevance scoring.
/// </summary>
public record ScoringConfig
{
    /// <summary>Weight for semantic similarity (0-1).</summary>
    public float SemanticWeight { get; init; } = 0.35f;

    /// <summary>Weight for document mentions (0-1).</summary>
    public float MentionWeight { get; init; } = 0.25f;

    /// <summary>Weight for type matching (0-1).</summary>
    public float TypeWeight { get; init; } = 0.20f;

    /// <summary>Weight for recency (0-1).</summary>
    public float RecencyWeight { get; init; } = 0.10f;

    /// <summary>Weight for name matching (0-1).</summary>
    public float NameMatchWeight { get; init; } = 0.10f;

    /// <summary>Preferred entity types (boost score).</summary>
    public IReadOnlySet<string>? PreferredTypes { get; init; }

    /// <summary>Recency decay in days.</summary>
    public int RecencyDecayDays { get; init; } = 365;
}
```

---

## 4. Implementation

### 4.1 Entity Relevance Scorer

```csharp
public class EntityRelevanceScorer : IEntityRelevanceScorer
{
    private readonly ISemanticSearchService _semanticSearch;
    private readonly ScoringConfig _config;
    private readonly ILogger<EntityRelevanceScorer> _logger;

    public EntityRelevanceScorer(
        ISemanticSearchService semanticSearch,
        IOptions<ScoringConfig> config,
        ILogger<EntityRelevanceScorer> logger)
    {
        _semanticSearch = semanticSearch;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ScoredEntity>> ScoreEntitiesAsync(
        ContextRequest request,
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default)
    {
        if (entities.Count == 0)
            return [];

        // Pre-compute query terms for efficiency
        var queryTerms = ExtractTerms(request.Query);

        // Get document content for mention scoring
        var documentContent = request.Document?.Content ?? "";
        var documentTerms = ExtractTerms(documentContent);

        // Batch compute semantic embeddings
        var queryEmbedding = await _semanticSearch.GetEmbeddingAsync(request.Query, ct);

        var entityTexts = entities
            .Select(e => $"{e.Name} {e.Type} {string.Join(" ", e.Properties.Values)}")
            .ToList();

        var entityEmbeddings = await _semanticSearch.GetEmbeddingsAsync(entityTexts, ct);

        // Score each entity
        var scored = new List<ScoredEntity>();
        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var signals = ComputeSignals(
                entity,
                queryTerms,
                documentTerms,
                documentContent,
                queryEmbedding,
                entityEmbeddings[i]);

            var score = ComputeFinalScore(signals);
            var matchedTerms = FindMatchedTerms(entity, queryTerms);

            scored.Add(new ScoredEntity
            {
                Entity = entity,
                Score = score,
                Signals = signals,
                MatchedTerms = matchedTerms
            });
        }

        return scored.OrderByDescending(s => s.Score).ToList();
    }

    public async Task<ScoredEntity> ScoreEntityAsync(
        ContextRequest request,
        KnowledgeEntity entity,
        CancellationToken ct = default)
    {
        var results = await ScoreEntitiesAsync(request, [entity], ct);
        return results[0];
    }

    private RelevanceSignalScores ComputeSignals(
        KnowledgeEntity entity,
        IReadOnlyList<string> queryTerms,
        IReadOnlyList<string> documentTerms,
        string documentContent,
        float[] queryEmbedding,
        float[] entityEmbedding)
    {
        return new RelevanceSignalScores
        {
            SemanticScore = ComputeSemanticScore(queryEmbedding, entityEmbedding),
            MentionScore = ComputeMentionScore(entity, documentContent, documentTerms),
            TypeScore = ComputeTypeScore(entity),
            RecencyScore = ComputeRecencyScore(entity),
            NameMatchScore = ComputeNameMatchScore(entity, queryTerms)
        };
    }

    private float ComputeSemanticScore(float[] queryEmbedding, float[] entityEmbedding)
    {
        // Cosine similarity
        if (queryEmbedding.Length != entityEmbedding.Length)
            return 0;

        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < queryEmbedding.Length; i++)
        {
            dot += queryEmbedding[i] * entityEmbedding[i];
            normA += queryEmbedding[i] * queryEmbedding[i];
            normB += entityEmbedding[i] * entityEmbedding[i];
        }

        var similarity = dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
        return Math.Max(0, similarity); // Clamp to [0, 1]
    }

    private float ComputeMentionScore(
        KnowledgeEntity entity,
        string documentContent,
        IReadOnlyList<string> documentTerms)
    {
        if (string.IsNullOrEmpty(documentContent))
            return 0;

        var entityNameLower = entity.Name.ToLowerInvariant();
        var mentionCount = 0;

        // Count exact name mentions
        var index = 0;
        while ((index = documentContent.IndexOf(entity.Name, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            mentionCount++;
            index += entity.Name.Length;
        }

        // Count term matches
        var entityTerms = ExtractTerms(entity.Name);
        var termMatches = entityTerms.Count(t => documentTerms.Contains(t));
        mentionCount += termMatches;

        // Normalize: 5+ mentions = max score
        return Math.Min(mentionCount / 5.0f, 1.0f);
    }

    private float ComputeTypeScore(KnowledgeEntity entity)
    {
        if (_config.PreferredTypes == null || _config.PreferredTypes.Count == 0)
            return 0.5f; // Neutral

        return _config.PreferredTypes.Contains(entity.Type) ? 1.0f : 0.3f;
    }

    private float ComputeRecencyScore(KnowledgeEntity entity)
    {
        var daysOld = (DateTime.UtcNow - entity.ModifiedAt).TotalDays;
        var decay = Math.Max(0, 1.0 - daysOld / _config.RecencyDecayDays);
        return (float)decay;
    }

    private float ComputeNameMatchScore(
        KnowledgeEntity entity,
        IReadOnlyList<string> queryTerms)
    {
        if (queryTerms.Count == 0) return 0;

        var entityNameLower = entity.Name.ToLowerInvariant();
        var entityTerms = ExtractTerms(entity.Name);

        var matchCount = 0;

        // Exact name contains
        foreach (var term in queryTerms)
        {
            if (entityNameLower.Contains(term))
                matchCount += 2;
            else if (entityTerms.Contains(term))
                matchCount += 1;
        }

        return Math.Min(matchCount / (float)(queryTerms.Count * 2), 1.0f);
    }

    private float ComputeFinalScore(RelevanceSignalScores signals)
    {
        var score =
            signals.SemanticScore * _config.SemanticWeight +
            signals.MentionScore * _config.MentionWeight +
            signals.TypeScore * _config.TypeWeight +
            signals.RecencyScore * _config.RecencyWeight +
            signals.NameMatchScore * _config.NameMatchWeight;

        return Math.Clamp(score, 0, 1);
    }

    private IReadOnlyList<string> FindMatchedTerms(
        KnowledgeEntity entity,
        IReadOnlyList<string> queryTerms)
    {
        var entityText = $"{entity.Name} {entity.Type}".ToLowerInvariant();
        return queryTerms.Where(t => entityText.Contains(t)).ToList();
    }

    private IReadOnlyList<string> ExtractTerms(string text)
    {
        return text.ToLowerInvariant()
            .Split([' ', ',', '.', '/', '-', '_', ':', ';', '(', ')', '[', ']'],
                StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .Distinct()
            .ToList();
    }
}
```

---

## 5. Scoring Algorithm

```
ALGORITHM: Entity Relevance Scoring

INPUT: Query q, Entity e, Document d, Config c
OUTPUT: Relevance score (0.0-1.0)

1. SEMANTIC SIMILARITY (35%)
   query_embed = embed(q)
   entity_embed = embed(e.name + e.type + e.properties)
   semantic_score = cosine_similarity(query_embed, entity_embed)

2. DOCUMENT MENTION (25%)
   mention_count = count_occurrences(e.name, d.content)
   term_matches = count_term_matches(e.name_terms, d.terms)
   mention_score = min((mention_count + term_matches) / 5.0, 1.0)

3. TYPE MATCH (20%)
   type_score = 1.0 if e.type in c.preferred_types else 0.3

4. RECENCY (10%)
   days_old = (now - e.modified_at).days
   recency_score = max(0, 1.0 - days_old / c.recency_decay_days)

5. NAME MATCH (10%)
   exact_matches = count_exact_matches(q.terms, e.name)
   name_score = min(exact_matches * 2 / q.terms.count, 1.0)

6. FINAL SCORE
   score = 0.35 * semantic + 0.25 * mention + 0.20 * type + 0.10 * recency + 0.10 * name
   RETURN clamp(score, 0, 1)
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Embedding service fails | Fall back to term matching only |
| Empty query | Return 0 for all entities |
| Missing document | Skip mention scoring |

---

## 7. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `ScoreEntities_RanksCorrectly` | Higher relevance ranked first |
| `ScoreEntity_SemanticSimilarity` | Semantic signal works |
| `ScoreEntity_DocumentMention` | Mention signal works |
| `ScoreEntity_TypeMatch` | Type signal works |
| `ScoreEntity_Recency` | Recency signal works |

---

## 8. Performance Considerations

- **Batch Embeddings:** Compute embeddings in batch
- **Term Caching:** Cache extracted terms
- **Parallel Scoring:** Score entities concurrently
- **Early Exit:** Stop if enough high-relevance found

---

## 9. License Gating

| Tier | Access |
| :--- | :----- |
| All tiers | Full scoring (part of context strategy) |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
