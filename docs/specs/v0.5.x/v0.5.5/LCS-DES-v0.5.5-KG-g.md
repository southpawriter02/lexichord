# LCS-DES-055-KG-g: Entity Linker

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-055-KG-g |
| **Feature ID** | KG-055g |
| **Feature Name** | Entity Linker |
| **Target Version** | v0.5.5g |
| **Module Scope** | `Lexichord.Nlu.EntityLinking` |
| **Swimlane** | NLU Pipeline |
| **License Tier** | WriterPro (basic), Teams (full) |
| **Feature Gate Key** | `knowledge.linking.linker.enabled` |
| **Status** | Implemented |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

Given candidates from the Candidate Generator, the **Entity Linker** must score and rank candidates to determine which graph entity a mention refers to. This is the core disambiguation step that resolves "the users endpoint" to `GET /users` rather than `POST /users`.

### 2.2 The Proposed Solution

Implement a multi-factor scoring system that combines:

- **Name similarity**: String distance between mention and candidate
- **Type compatibility**: Whether entity types match
- **Context scoring**: Surrounding text relevance to candidate properties
- **Co-occurrence scoring**: Other linked entities in the same document
- **Popularity scoring**: Usage frequency and importance

The linker produces a final confidence score and either accepts the best candidate, defers to LLM disambiguation (v0.5.5h), or flags for human review (v0.5.5i).

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- v0.5.5e: `IEntityRecognizer` — Source mentions
- v0.5.5f: `ICandidateGenerator` — Candidate entities
- v0.4.5e: `IGraphRepository` — Entity relationships

**Downstream Modules:**
- v0.5.5h: `ILLMFallback` — Low-confidence disambiguation
- v0.5.5i: `ILinkingReviewService` — Human review queue

**NuGet Packages:**
- `Microsoft.ML` — ML-based scoring (optional)
- `Microsoft.Extensions.Caching.Memory` — Context caching

### 3.2 Module Placement

```
Lexichord.Nlu/
├── EntityLinking/
│   ├── IEntityLinkingService.cs
│   ├── EntityLinkingService.cs
│   ├── LinkingContext.cs
│   ├── LinkingResult.cs
│   ├── LinkedEntity.cs
│   └── Scoring/
│       ├── ILinkScorer.cs
│       ├── NameSimilarityScorer.cs
│       ├── TypeCompatibilityScorer.cs
│       ├── ContextScorer.cs
│       ├── CoOccurrenceScorer.cs
│       ├── CompositeLinkScorer.cs
│       └── ScoringWeights.cs
```

### 3.3 Licensing Behavior

- **Load Behavior:** [x] On Feature Toggle — Load with NLU pipeline
- **Fallback Experience:** WriterPro: name + type scoring only; Teams+: full scoring + LLM fallback

---

## 4. Data Contract (The API)

### 4.1 Core Interfaces

```csharp
namespace Lexichord.Nlu.EntityLinking;

/// <summary>
/// Service for linking entity mentions to canonical graph entities.
/// </summary>
public interface IEntityLinkingService
{
    /// <summary>
    /// Links entity mentions in text to graph entities.
    /// </summary>
    /// <param name="mentions">Extracted entity mentions.</param>
    /// <param name="context">Linking context with document info.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Linked entities with confidence scores.</returns>
    Task<LinkingResult> LinkEntitiesAsync(
        IReadOnlyList<EntityMention> mentions,
        LinkingContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Links a single mention (for real-time use).
    /// </summary>
    Task<LinkedEntity> LinkSingleAsync(
        EntityMention mention,
        LinkingContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Gets linking statistics.
    /// </summary>
    LinkingStats GetStats();
}
```

### 4.2 LinkedEntity Record

```csharp
namespace Lexichord.Nlu.EntityLinking;

/// <summary>
/// A mention linked to a graph entity.
/// </summary>
public record LinkedEntity
{
    /// <summary>Original mention.</summary>
    public required EntityMention Mention { get; init; }

    /// <summary>Resolved graph entity ID (null if unlinked).</summary>
    public Guid? ResolvedEntityId { get; init; }

    /// <summary>Resolved entity (null if unlinked).</summary>
    public KnowledgeEntity? ResolvedEntity { get; init; }

    /// <summary>Overall linking confidence (0.0-1.0).</summary>
    public float Confidence { get; init; }

    /// <summary>Individual score components.</summary>
    public LinkingScores? Scores { get; init; }

    /// <summary>Alternative candidates considered.</summary>
    public IReadOnlyList<ScoredCandidate>? Candidates { get; init; }

    /// <summary>Linking method used.</summary>
    public LinkingMethod Method { get; init; }

    /// <summary>Whether human review is recommended.</summary>
    public bool NeedsReview => Confidence >= 0.3f && Confidence < 0.7f && ResolvedEntityId.HasValue;

    /// <summary>Whether the link is high confidence.</summary>
    public bool IsHighConfidence => Confidence >= 0.8f;

    /// <summary>Whether we failed to find any candidate.</summary>
    public bool IsUnlinked => !ResolvedEntityId.HasValue;

    /// <summary>Reason for the linking decision.</summary>
    public string? Reason { get; init; }
}

/// <summary>
/// Linking method used.
/// </summary>
public enum LinkingMethod
{
    /// <summary>Direct exact match, no ambiguity.</summary>
    ExactMatch,

    /// <summary>Scored and ranked candidates.</summary>
    ScoredRanking,

    /// <summary>LLM disambiguation was used.</summary>
    LLMDisambiguation,

    /// <summary>Human review resolved the link.</summary>
    HumanReview,

    /// <summary>Could not link (no suitable candidates).</summary>
    Unlinked
}

/// <summary>
/// Candidate with computed scores.
/// </summary>
public record ScoredCandidate
{
    public required LinkCandidate Candidate { get; init; }
    public required float FinalScore { get; init; }
    public required LinkingScores Scores { get; init; }
    public int Rank { get; init; }
}

/// <summary>
/// Individual score components.
/// </summary>
public record LinkingScores
{
    /// <summary>Name similarity score (0.0-1.0).</summary>
    public float NameSimilarity { get; init; }

    /// <summary>Type compatibility score (0.0-1.0).</summary>
    public float TypeCompatibility { get; init; }

    /// <summary>Context relevance score (0.0-1.0).</summary>
    public float ContextRelevance { get; init; }

    /// <summary>Co-occurrence score (0.0-1.0).</summary>
    public float CoOccurrence { get; init; }

    /// <summary>Popularity score (0.0-1.0).</summary>
    public float Popularity { get; init; }

    /// <summary>Weighted combination.</summary>
    public float Combined { get; init; }
}
```

### 4.3 LinkingContext and Result

```csharp
namespace Lexichord.Nlu.EntityLinking;

/// <summary>
/// Context for entity linking.
/// </summary>
public record LinkingContext
{
    /// <summary>Document being processed.</summary>
    public Guid? DocumentId { get; init; }

    /// <summary>Full document text (for context scoring).</summary>
    public string? DocumentText { get; init; }

    /// <summary>Project ID for scoped entities.</summary>
    public Guid? ProjectId { get; init; }

    /// <summary>Already-linked entities in this document (for co-occurrence).</summary>
    public IReadOnlyList<LinkedEntity>? AlreadyLinked { get; init; }

    /// <summary>Minimum confidence to accept a link.</summary>
    public float MinAcceptConfidence { get; init; } = 0.8f;

    /// <summary>Confidence threshold for LLM fallback.</summary>
    public float LLMFallbackThreshold { get; init; } = 0.5f;

    /// <summary>Confidence threshold for human review.</summary>
    public float ReviewThreshold { get; init; } = 0.7f;

    /// <summary>Whether to use LLM disambiguation.</summary>
    public bool UseLLMFallback { get; init; } = true;

    /// <summary>Scoring weights override.</summary>
    public ScoringWeights? Weights { get; init; }
}

/// <summary>
/// Result of entity linking.
/// </summary>
public record LinkingResult
{
    /// <summary>All linked entities.</summary>
    public required IReadOnlyList<LinkedEntity> LinkedEntities { get; init; }

    /// <summary>Processing duration.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Linking statistics.</summary>
    public LinkingResultStats Stats { get; init; } = new();

    /// <summary>Entities needing human review.</summary>
    public IReadOnlyList<LinkedEntity> NeedingReview =>
        LinkedEntities.Where(e => e.NeedsReview).ToList();

    /// <summary>High-confidence links.</summary>
    public IReadOnlyList<LinkedEntity> HighConfidenceLinks =>
        LinkedEntities.Where(e => e.IsHighConfidence).ToList();

    /// <summary>Unlinked mentions.</summary>
    public IReadOnlyList<LinkedEntity> Unlinked =>
        LinkedEntities.Where(e => e.IsUnlinked).ToList();
}

/// <summary>
/// Statistics about linking results.
/// </summary>
public record LinkingResultStats
{
    public int TotalMentions { get; init; }
    public int LinkedCount { get; init; }
    public int UnlinkedCount { get; init; }
    public int HighConfidenceCount { get; init; }
    public int LowConfidenceCount { get; init; }
    public int NeedingReviewCount { get; init; }
    public int LLMFallbackUsed { get; init; }
    public float AverageConfidence { get; init; }
}
```

### 4.4 Scoring Interfaces

```csharp
namespace Lexichord.Nlu.EntityLinking.Scoring;

/// <summary>
/// Scorer for computing link scores.
/// </summary>
public interface ILinkScorer
{
    /// <summary>Scorer name for debugging.</summary>
    string Name { get; }

    /// <summary>
    /// Computes a score for a candidate.
    /// </summary>
    /// <param name="mention">The mention being linked.</param>
    /// <param name="candidate">The candidate entity.</param>
    /// <param name="context">Linking context.</param>
    /// <returns>Score between 0.0 and 1.0.</returns>
    float Score(EntityMention mention, LinkCandidate candidate, LinkingContext context);
}

/// <summary>
/// Weights for combining scores.
/// </summary>
public record ScoringWeights
{
    public float NameSimilarity { get; init; } = 0.30f;
    public float TypeCompatibility { get; init; } = 0.20f;
    public float ContextRelevance { get; init; } = 0.25f;
    public float CoOccurrence { get; init; } = 0.15f;
    public float Popularity { get; init; } = 0.10f;

    /// <summary>Default weights.</summary>
    public static ScoringWeights Default => new();

    /// <summary>Weights optimized for technical documentation.</summary>
    public static ScoringWeights TechnicalDocs => new()
    {
        NameSimilarity = 0.35f,
        TypeCompatibility = 0.25f,
        ContextRelevance = 0.20f,
        CoOccurrence = 0.10f,
        Popularity = 0.10f
    };

    public void Validate()
    {
        var sum = NameSimilarity + TypeCompatibility + ContextRelevance + CoOccurrence + Popularity;
        if (Math.Abs(sum - 1.0f) > 0.01f)
        {
            throw new ArgumentException($"Weights must sum to 1.0, got {sum}");
        }
    }
}
```

---

## 5. Implementation Logic

### 5.1 EntityLinkingService

```csharp
namespace Lexichord.Nlu.EntityLinking;

/// <summary>
/// Main entity linking service.
/// </summary>
public class EntityLinkingService : IEntityLinkingService
{
    private readonly ICandidateGenerator _candidateGenerator;
    private readonly ICompositeLinkScorer _scorer;
    private readonly ILLMFallback _llmFallback;
    private readonly ILinkingReviewService _reviewService;
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<EntityLinkingService> _logger;
    private readonly LinkingStats _stats = new();

    public async Task<LinkingResult> LinkEntitiesAsync(
        IReadOnlyList<EntityMention> mentions,
        LinkingContext context,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var linkedEntities = new List<LinkedEntity>();
        var alreadyLinked = context.AlreadyLinked?.ToList() ?? new List<LinkedEntity>();

        // Generate candidates for all mentions
        var candidateSets = await _candidateGenerator.GenerateCandidatesBatchAsync(
            mentions, null, ct);

        foreach (var mention in mentions)
        {
            var candidateSet = candidateSets.GetValueOrDefault(mention.Id)
                ?? CandidateSet.Empty(mention);

            // Update context with entities linked so far
            var updatedContext = context with { AlreadyLinked = alreadyLinked };

            var linked = await LinkMentionAsync(mention, candidateSet, updatedContext, ct);
            linkedEntities.Add(linked);

            if (!linked.IsUnlinked)
            {
                alreadyLinked.Add(linked);
            }
        }

        sw.Stop();

        return new LinkingResult
        {
            LinkedEntities = linkedEntities,
            Duration = sw.Elapsed,
            Stats = ComputeStats(linkedEntities)
        };
    }

    private async Task<LinkedEntity> LinkMentionAsync(
        EntityMention mention,
        CandidateSet candidateSet,
        LinkingContext context,
        CancellationToken ct)
    {
        // No candidates found
        if (!candidateSet.Candidates.Any())
        {
            _stats.IncrementUnlinked();
            return CreateUnlinkedEntity(mention, "No candidates found");
        }

        // Score all candidates
        var scoredCandidates = ScoreCandidates(mention, candidateSet.Candidates, context);

        var bestCandidate = scoredCandidates.First();
        var secondBest = scoredCandidates.Skip(1).FirstOrDefault();

        // Check for clear winner
        if (bestCandidate.FinalScore >= context.MinAcceptConfidence)
        {
            // High confidence: accept directly
            _stats.IncrementHighConfidence();
            return await CreateLinkedEntityAsync(
                mention, bestCandidate, scoredCandidates, LinkingMethod.ScoredRanking, ct);
        }

        // Check score gap between top candidates
        var scoreGap = secondBest != null
            ? bestCandidate.FinalScore - secondBest.FinalScore
            : 1.0f;

        // If close scores and above LLM threshold, use LLM disambiguation
        if (context.UseLLMFallback &&
            bestCandidate.FinalScore >= context.LLMFallbackThreshold &&
            scoreGap < 0.15f)
        {
            var llmResult = await _llmFallback.DisambiguateAsync(
                mention, scoredCandidates.Take(5).ToList(), context, ct);

            if (llmResult.SelectedCandidate != null)
            {
                _stats.IncrementLLMFallback();
                return await CreateLinkedEntityAsync(
                    mention, llmResult.SelectedCandidate, scoredCandidates,
                    LinkingMethod.LLMDisambiguation, ct);
            }
        }

        // Below review threshold: return as needing review
        if (bestCandidate.FinalScore >= context.LLMFallbackThreshold)
        {
            _stats.IncrementNeedsReview();
            return await CreateLinkedEntityAsync(
                mention, bestCandidate, scoredCandidates, LinkingMethod.ScoredRanking, ct);
        }

        // Too low confidence: unlinked
        _stats.IncrementUnlinked();
        return CreateUnlinkedEntity(mention, $"Best score {bestCandidate.FinalScore:F2} below threshold");
    }

    private List<ScoredCandidate> ScoreCandidates(
        EntityMention mention,
        IReadOnlyList<LinkCandidate> candidates,
        LinkingContext context)
    {
        var weights = context.Weights ?? ScoringWeights.Default;
        var scored = new List<ScoredCandidate>();

        foreach (var candidate in candidates)
        {
            var scores = _scorer.ComputeScores(mention, candidate, context);
            var finalScore = CombineScores(scores, weights);

            scored.Add(new ScoredCandidate
            {
                Candidate = candidate,
                FinalScore = finalScore,
                Scores = scores
            });
        }

        // Sort by score descending and assign ranks
        scored = scored.OrderByDescending(s => s.FinalScore).ToList();
        for (int i = 0; i < scored.Count; i++)
        {
            scored[i] = scored[i] with { Rank = i + 1 };
        }

        return scored;
    }

    private float CombineScores(LinkingScores scores, ScoringWeights weights)
    {
        return scores.NameSimilarity * weights.NameSimilarity
             + scores.TypeCompatibility * weights.TypeCompatibility
             + scores.ContextRelevance * weights.ContextRelevance
             + scores.CoOccurrence * weights.CoOccurrence
             + scores.Popularity * weights.Popularity;
    }

    private async Task<LinkedEntity> CreateLinkedEntityAsync(
        EntityMention mention,
        ScoredCandidate bestCandidate,
        List<ScoredCandidate> allCandidates,
        LinkingMethod method,
        CancellationToken ct)
    {
        var entity = await _graphRepository.GetEntityAsync(
            bestCandidate.Candidate.EntityId, ct);

        return new LinkedEntity
        {
            Mention = mention,
            ResolvedEntityId = bestCandidate.Candidate.EntityId,
            ResolvedEntity = entity,
            Confidence = bestCandidate.FinalScore,
            Scores = bestCandidate.Scores,
            Candidates = allCandidates.Take(5).ToList(),
            Method = method,
            Reason = $"Best match: {bestCandidate.Candidate.EntityName} (score: {bestCandidate.FinalScore:F2})"
        };
    }

    private LinkedEntity CreateUnlinkedEntity(EntityMention mention, string reason)
    {
        return new LinkedEntity
        {
            Mention = mention,
            ResolvedEntityId = null,
            ResolvedEntity = null,
            Confidence = 0,
            Method = LinkingMethod.Unlinked,
            Reason = reason
        };
    }
}
```

### 5.2 Composite Scorer

```csharp
namespace Lexichord.Nlu.EntityLinking.Scoring;

/// <summary>
/// Combines multiple scorers into a single score set.
/// </summary>
public interface ICompositeLinkScorer
{
    LinkingScores ComputeScores(
        EntityMention mention,
        LinkCandidate candidate,
        LinkingContext context);
}

public class CompositeLinkScorer : ICompositeLinkScorer
{
    private readonly NameSimilarityScorer _nameScorer;
    private readonly TypeCompatibilityScorer _typeScorer;
    private readonly ContextScorer _contextScorer;
    private readonly CoOccurrenceScorer _coOccurrenceScorer;
    private readonly PopularityScorer _popularityScorer;

    public LinkingScores ComputeScores(
        EntityMention mention,
        LinkCandidate candidate,
        LinkingContext context)
    {
        return new LinkingScores
        {
            NameSimilarity = _nameScorer.Score(mention, candidate, context),
            TypeCompatibility = _typeScorer.Score(mention, candidate, context),
            ContextRelevance = _contextScorer.Score(mention, candidate, context),
            CoOccurrence = _coOccurrenceScorer.Score(mention, candidate, context),
            Popularity = _popularityScorer.Score(mention, candidate, context)
        };
    }
}
```

### 5.3 Context Scorer

```csharp
namespace Lexichord.Nlu.EntityLinking.Scoring;

/// <summary>
/// Scores based on surrounding context matching entity properties.
/// </summary>
public class ContextScorer : ILinkScorer
{
    public string Name => "Context";

    public float Score(EntityMention mention, LinkCandidate candidate, LinkingContext context)
    {
        if (string.IsNullOrEmpty(mention.SurroundingContext) || candidate.Properties == null)
        {
            return 0.5f; // Neutral if no context available
        }

        var contextWords = ExtractWords(mention.SurroundingContext);
        var propertyWords = ExtractPropertyWords(candidate.Properties);

        if (!propertyWords.Any())
        {
            return 0.5f;
        }

        // Calculate Jaccard similarity between context and properties
        var intersection = contextWords.Intersect(propertyWords).Count();
        var union = contextWords.Union(propertyWords).Count();

        if (union == 0) return 0.5f;

        return (float)intersection / union;
    }

    private HashSet<string> ExtractWords(string text)
    {
        return text
            .ToLowerInvariant()
            .Split(new[] { ' ', '.', ',', '/', ':', '-', '_', '(', ')', '[', ']' },
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .ToHashSet();
    }

    private HashSet<string> ExtractPropertyWords(IReadOnlyDictionary<string, object> properties)
    {
        var words = new HashSet<string>();

        foreach (var (key, value) in properties)
        {
            words.UnionWith(ExtractWords(key));
            if (value is string s)
            {
                words.UnionWith(ExtractWords(s));
            }
        }

        return words;
    }
}
```

### 5.4 Co-occurrence Scorer

```csharp
namespace Lexichord.Nlu.EntityLinking.Scoring;

/// <summary>
/// Scores based on relationships with already-linked entities.
/// </summary>
public class CoOccurrenceScorer : ILinkScorer
{
    private readonly IGraphRepository _graphRepository;

    public string Name => "CoOccurrence";

    public float Score(EntityMention mention, LinkCandidate candidate, LinkingContext context)
    {
        if (context.AlreadyLinked == null || !context.AlreadyLinked.Any())
        {
            return 0.5f; // Neutral if no co-occurrence data
        }

        var linkedIds = context.AlreadyLinked
            .Where(e => e.ResolvedEntityId.HasValue)
            .Select(e => e.ResolvedEntityId!.Value)
            .ToHashSet();

        if (!linkedIds.Any())
        {
            return 0.5f;
        }

        // Check if candidate has relationships with linked entities
        var relatedIds = candidate.RelatedEntityIds?.ToHashSet() ?? new HashSet<Guid>();
        var commonCount = relatedIds.Intersect(linkedIds).Count();

        if (commonCount == 0)
        {
            return 0.3f; // Slight penalty for no co-occurrence
        }

        // Score based on proportion of links
        var proportion = (float)commonCount / Math.Min(relatedIds.Count, linkedIds.Count);
        return 0.5f + (proportion * 0.5f); // Range: 0.5 to 1.0
    }
}
```

---

## 6. Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     Entity Linking Flow                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌────────────────┐     ┌────────────────┐                      │
│  │ EntityMention  │     │ CandidateSet   │                      │
│  │ "users endpoint"│────►│ [Candidate 1]  │                      │
│  │ type: Endpoint │     │ [Candidate 2]  │                      │
│  └────────────────┘     │ [Candidate 3]  │                      │
│                         └───────┬────────┘                      │
│                                 │                                │
│                                 ▼                                │
│           ┌─────────────────────────────────────┐               │
│           │           Score Each Candidate       │               │
│           ├─────────────────────────────────────┤               │
│           │  ┌─────────────┐  ┌─────────────┐   │               │
│           │  │    Name     │  │    Type     │   │               │
│           │  │  Similarity │  │ Compatibility│   │               │
│           │  │   (0.85)    │  │   (1.0)     │   │               │
│           │  └─────────────┘  └─────────────┘   │               │
│           │  ┌─────────────┐  ┌─────────────┐   │               │
│           │  │   Context   │  │ Co-occurrence│   │               │
│           │  │  Relevance  │  │   Score     │   │               │
│           │  │   (0.7)     │  │   (0.6)     │   │               │
│           │  └─────────────┘  └─────────────┘   │               │
│           └─────────────────────┬───────────────┘               │
│                                 │                                │
│                                 ▼                                │
│                    ┌────────────────────────┐                   │
│                    │   Combine with Weights  │                   │
│                    │   Final Score: 0.82     │                   │
│                    └───────────┬────────────┘                   │
│                                │                                 │
│                    ┌───────────┴───────────┐                    │
│                    │                       │                     │
│                    ▼                       ▼                     │
│           Score >= 0.8?              Score 0.5-0.8?             │
│                │                           │                     │
│           YES  │                      YES  │                     │
│                ▼                           ▼                     │
│    ┌─────────────────┐        ┌─────────────────┐              │
│    │   Accept Link   │        │  LLM Fallback?  │              │
│    │ High Confidence │        │ (close scores)  │              │
│    └─────────────────┘        └────────┬────────┘              │
│                                        │                        │
│                               ┌────────┴────────┐               │
│                               │                 │               │
│                          Resolved          Unresolved           │
│                               │                 │               │
│                               ▼                 ▼               │
│                    ┌───────────────┐  ┌───────────────┐        │
│                    │ Accept Link   │  │ Flag for      │        │
│                    │ (LLM method)  │  │ Human Review  │        │
│                    └───────────────┘  └───────────────┘        │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 7. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.5g")]
public class EntityLinkingServiceTests
{
    private readonly EntityLinkingService _service;

    [Fact]
    public async Task LinkEntitiesAsync_ExactMatch_ReturnsHighConfidence()
    {
        // Arrange
        var mention = CreateMention("GET /users", "Endpoint");
        var context = new LinkingContext { MinAcceptConfidence = 0.8f };

        // Act
        var result = await _service.LinkEntitiesAsync(new[] { mention }, context);

        // Assert
        result.LinkedEntities.Should().HaveCount(1);
        var linked = result.LinkedEntities[0];
        linked.IsHighConfidence.Should().BeTrue();
        linked.Method.Should().Be(LinkingMethod.ExactMatch);
    }

    [Fact]
    public async Task LinkEntitiesAsync_AmbiguousCandidates_UsesLLMFallback()
    {
        // Arrange
        var mention = CreateMention("users", "Endpoint"); // Could be GET or POST
        var context = new LinkingContext
        {
            MinAcceptConfidence = 0.8f,
            LLMFallbackThreshold = 0.5f,
            UseLLMFallback = true
        };

        // Act
        var result = await _service.LinkEntitiesAsync(new[] { mention }, context);

        // Assert
        var linked = result.LinkedEntities[0];
        if (!linked.IsUnlinked)
        {
            // Either resolved by LLM or flagged for review
            linked.Method.Should().BeOneOf(
                LinkingMethod.LLMDisambiguation,
                LinkingMethod.ScoredRanking);
        }
    }

    [Fact]
    public async Task LinkEntitiesAsync_LowConfidence_FlagsForReview()
    {
        // Arrange
        var mention = CreateMention("something vague", "Unknown");
        var context = new LinkingContext { MinAcceptConfidence = 0.8f };

        // Act
        var result = await _service.LinkEntitiesAsync(new[] { mention }, context);

        // Assert
        var linked = result.LinkedEntities[0];
        if (!linked.IsUnlinked && !linked.IsHighConfidence)
        {
            linked.NeedsReview.Should().BeTrue();
        }
    }

    [Fact]
    public async Task LinkEntitiesAsync_CoOccurrence_BoostsRelatedEntities()
    {
        // Arrange
        var mentions = new[]
        {
            CreateMention("GET /users", "Endpoint"),
            CreateMention("limit", "Parameter") // Parameter of GET /users
        };
        var context = new LinkingContext();

        // Act
        var result = await _service.LinkEntitiesAsync(mentions, context);

        // Assert
        var paramLink = result.LinkedEntities[1];
        paramLink.Scores!.CoOccurrence.Should().BeGreaterThan(0.5f,
            "related parameter should get co-occurrence boost");
    }

    [Fact]
    public async Task LinkEntitiesAsync_CustomWeights_AffectsScoring()
    {
        // Arrange
        var mention = CreateMention("users", "Endpoint");
        var context1 = new LinkingContext
        {
            Weights = new ScoringWeights { NameSimilarity = 0.9f, TypeCompatibility = 0.1f }
        };
        var context2 = new LinkingContext
        {
            Weights = new ScoringWeights { NameSimilarity = 0.1f, TypeCompatibility = 0.9f }
        };

        // Act
        var result1 = await _service.LinkEntitiesAsync(new[] { mention }, context1);
        var result2 = await _service.LinkEntitiesAsync(new[] { mention }, context2);

        // Assert
        // Different weights should produce different confidence scores
        result1.LinkedEntities[0].Confidence
            .Should().NotBe(result2.LinkedEntities[0].Confidence);
    }

    [Fact]
    public async Task LinkSingleAsync_RealTimeMode_ReturnsQuickly()
    {
        // Arrange
        var mention = CreateMention("limit", "Parameter");
        var context = new LinkingContext();

        // Act
        var sw = Stopwatch.StartNew();
        var linked = await _service.LinkSingleAsync(mention, context);
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(100);
        linked.Should().NotBeNull();
    }
}
```

---

## 8. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | Exact matches resolve with >95% confidence. |
| 2 | Scoring correctly combines all factors per weights. |
| 3 | Close-scored candidates trigger LLM disambiguation (Teams+). |
| 4 | Low confidence links flagged for review. |
| 5 | Co-occurrence scoring boosts related entities. |
| 6 | Context scoring uses surrounding text. |
| 7 | Batch linking processes 50 mentions in <5 seconds. |
| 8 | Single mention linking completes in <100ms. |
| 9 | Statistics tracking is accurate. |
| 10 | Custom weights properly affect final scores. |

---

## 9. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `IEntityLinkingService` interface | [ ] |
| 2 | `LinkedEntity` record | [ ] |
| 3 | `LinkingContext` record | [ ] |
| 4 | `LinkingResult` record | [ ] |
| 5 | `LinkingScores` record | [ ] |
| 6 | `ScoredCandidate` record | [ ] |
| 7 | `EntityLinkingService` implementation | [ ] |
| 8 | `ILinkScorer` interface | [ ] |
| 9 | `NameSimilarityScorer` implementation | [ ] |
| 10 | `TypeCompatibilityScorer` implementation | [ ] |
| 11 | `ContextScorer` implementation | [ ] |
| 12 | `CoOccurrenceScorer` implementation | [ ] |
| 13 | `CompositeLinkScorer` implementation | [ ] |
| 14 | `ScoringWeights` record | [ ] |
| 15 | Unit tests | [ ] |

---

## 10. Changelog Entry

```markdown
### Added (v0.5.5g)

- `IEntityLinkingService` interface for entity disambiguation
- `LinkedEntity` record with confidence and scoring breakdown
- Multi-factor scoring: name, type, context, co-occurrence, popularity
- `CompositeLinkScorer` for combined scoring
- Configurable scoring weights via `ScoringWeights`
- LLM fallback integration for ambiguous candidates
- Human review flagging for low-confidence links
- Batch and single-mention linking modes
- Linking statistics tracking
```

---
