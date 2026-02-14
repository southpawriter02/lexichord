# Changelog: v0.7.2f — Entity Relevance Scorer

**Feature ID:** CTX-072f
**Version:** 0.7.2f
**Date:** 2026-02-14
**Status:** ✅ Complete

---

## Overview

Implements the Entity Relevance Scorer — a multi-signal scoring system that ranks Knowledge Graph entities by their relevance to a query. It extends the existing term-based `EntityRelevanceRanker` (v0.6.6e) with five weighted signals: semantic similarity (cosine via embeddings), document mention counting, entity type matching, recency decay, and improved name matching. Produces `ScoredEntity` results with per-signal breakdowns for diagnostic transparency.

This sub-part builds upon v0.6.6e's term-based ranking and v0.7.2e's Knowledge Context Strategy, providing more accurate entity selection for all specialist agents consuming knowledge context.

---

## What's New

### ScoringConfig

Immutable configuration record for the five-signal scoring algorithm:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Copilot`
- **Pattern:** `sealed record` with `init` setters for `with`-expression composition
- **Properties:** `SemanticWeight` (0.35), `MentionWeight` (0.25), `TypeWeight` (0.20), `RecencyWeight` (0.10), `NameMatchWeight` (0.10), `PreferredTypes` (`IReadOnlySet<string>?`), `RecencyDecayDays` (365)
- **Purpose:** Configurable signal weights and parameters for relevance scoring

### RelevanceSignalScores

Per-signal breakdown record:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Copilot`
- **Properties:** `SemanticScore`, `MentionScore`, `TypeScore`, `RecencyScore`, `NameMatchScore` — all `float` in range [0.0, 1.0]
- **Purpose:** Transparency into how the composite score was computed

### ScoringRequest

Purpose-built input record for scoring operations:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Copilot`
- **Properties:** `required string Query`, `string? DocumentContent`, `IReadOnlySet<string>? PreferredEntityTypes`
- **Purpose:** Focused scoring input — not reusing `ContextGatheringRequest` (wrong layer) or LLM `ContextRequest` (wrong purpose)

### ScoredEntity

Entity with multi-signal relevance score:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Copilot`
- **Properties:** `required KnowledgeEntity Entity`, `float Score`, `required RelevanceSignalScores Signals`, `IReadOnlyList<string> MatchedTerms`
- **Purpose:** Rich scoring result with per-signal transparency and matched term diagnostics

### IEntityRelevanceScorer

Async multi-signal scoring interface:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Copilot`
- **Methods:** `ScoreEntitiesAsync(ScoringRequest, IReadOnlyList<KnowledgeEntity>, CancellationToken)`, `ScoreEntityAsync(ScoringRequest, KnowledgeEntity, CancellationToken)`
- **Purpose:** Async because semantic scoring requires `IEmbeddingService` network calls. Complements (does not replace) the sync `IEntityRelevanceRanker`

### CosineSimilarity

Static utility for embedding vector comparison:
- **Namespace:** `Lexichord.Modules.Knowledge.Copilot.Context.Scoring`
- **Method:** `static float Compute(float[] a, float[] b)` — standard dot-product / (norm_a * norm_b), clamped to [0, 1]
- **Edge Cases:** Handles null, empty, mismatched-length, and zero-magnitude vectors (all return 0.0f)
- **Purpose:** Separated from scorer for independent testability

### EntityRelevanceScorer

Core multi-signal scoring implementation:
- **Namespace:** `Lexichord.Modules.Knowledge.Copilot.Context.Scoring`
- **Visibility:** `internal sealed class`
- **Dependencies:** `IEmbeddingService?` (nullable — graceful fallback), `IOptions<ScoringConfig>`, `ILogger<EntityRelevanceScorer>`

**5-Signal Scoring Pipeline:**
1. **Semantic Similarity (35%):** Cosine similarity between query and entity embedding vectors via `IEmbeddingService.EmbedBatchAsync()`. Returns 0.0 when embeddings unavailable.
2. **Document Mentions (25%):** Counts entity name occurrences in document content (case-insensitive) plus term-level matches. Saturates at 5 mentions (score = 1.0).
3. **Type Matching (20%):** 1.0 for preferred types, 0.3 for non-preferred, 0.5 neutral when no preferences set.
4. **Recency (10%):** Linear decay from 1.0 (today) to 0.0 (RecencyDecayDays ago). Uses `DateTimeOffset.UtcNow`.
5. **Name Matching (10%):** Substring matches score 2 points, term-level matches score 1 point. Normalized by (queryTerms.Count * 2).

**Embedding Fallback:** When `IEmbeddingService` is null or throws (non-cancellation), semantic score = 0.0 and semantic weight is redistributed proportionally to the four remaining signals:
- Mention: 0.25 → ~0.385
- Type: 0.20 → ~0.308
- Recency: 0.10 → ~0.154
- Name: 0.10 → ~0.154

**Error Handling:**
- `OperationCanceledException` → re-thrown (respect cancellation)
- `EmbeddingException` / general `Exception` → log warning, fallback to non-semantic scoring
- `null` entities → `ArgumentNullException`

---

## DI Registration

### KnowledgeModule.RegisterServices() Update

Added v0.7.2f section:
- `Options.Create(new ScoringConfig())` as singleton (default weights)
- `IEntityRelevanceScorer` → `EntityRelevanceScorer` as singleton via factory lambda resolving `IEmbeddingService?` optionally

### KnowledgeContextProvider Integration

Injected optional `IEntityRelevanceScorer?` parameter:
- When scorer available: builds `ScoringRequest` from query + options, calls `ScoreEntitiesAsync`, converts `ScoredEntity` → `RankedEntity` for existing `SelectWithinBudget`
- When scorer null: falls back to existing `IEntityRelevanceRanker.RankEntities()`
- `DocumentContent` is null at provider level (4 other signals provide meaningful scoring)

---

## Files Changed

### New Files (7)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/.../Contracts/Knowledge/Copilot/ScoringConfig.cs` | Record | Scoring weights configuration |
| `src/.../Contracts/Knowledge/Copilot/RelevanceSignalScores.cs` | Record | Per-signal score breakdown |
| `src/.../Contracts/Knowledge/Copilot/ScoringRequest.cs` | Record | Scorer input parameters |
| `src/.../Contracts/Knowledge/Copilot/ScoredEntity.cs` | Record | Multi-signal scored entity |
| `src/.../Contracts/Knowledge/Copilot/IEntityRelevanceScorer.cs` | Interface | Multi-signal scoring contract |
| `src/.../Copilot/Context/Scoring/CosineSimilarity.cs` | Utility | Cosine similarity computation |
| `src/.../Copilot/Context/Scoring/EntityRelevanceScorer.cs` | Implementation | 5-signal scoring engine |

### New Test Files (2)

| File | Type | Description |
|:-----|:-----|:------------|
| `tests/.../Copilot/Context/Scoring/CosineSimilarityTests.cs` | Tests | 16 unit tests |
| `tests/.../Copilot/Context/Scoring/EntityRelevanceScorerTests.cs` | Tests | 41 unit tests |

### Modified Files (2)

| File | Changes |
|:-----|:--------|
| `src/.../KnowledgeModule.cs` | Added v0.7.2f DI registration section (ScoringConfig, EntityRelevanceScorer) |
| `src/.../Copilot/Context/KnowledgeContextProvider.cs` | Added optional `IEntityRelevanceScorer?` injection, scorer-first ranking path with RankedEntity conversion, `EstimateEntityTokens` helper |

---

## Spec-to-Codebase Adaptations

| Spec Reference | Actual Codebase | Adaptation |
|:---|:---|:---|
| `Lexichord.KnowledgeGraph.Context.Scoring` | `Lexichord.Modules.Knowledge.Copilot.Context.Scoring` | Module namespace differs |
| `ISemanticSearchService.GetEmbeddingAsync()` | `IEmbeddingService.EmbedAsync()` / `EmbedBatchAsync()` | No embedding methods on search service |
| `ContextRequest` (undefined type) | `ScoringRequest` record | Purpose-built for scorer input |
| `public class EntityRelevanceScorer` | `internal sealed class` | Codebase convention for Knowledge module |
| `DateTime.UtcNow` | `DateTimeOffset.UtcNow` | `KnowledgeEntity.ModifiedAt` is `DateTimeOffset` |
| Moq `Mock<ILogger<T>>` | `NullLogger<T>.Instance` | Internal type prevents Moq proxy generation for strong-named assemblies |
| `[Trait("Version", ...)]` | `[Trait("SubPart", "v0.7.2f")]` | Codebase trait convention |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| CosineSimilarityTests | 16 | Null/empty/zero inputs, length mismatch, identical, scaled, orthogonal, opposite (clamped), known values |
| EntityRelevanceScorerTests | 41 | Constructor guards, empty input, null guards, no-embedding fallback, weight redistribution, sorting, embedding integration, embedding failure fallback, mention scoring, mention saturation, type scoring, type override, recency scoring, custom decay, name matching, matched terms, single entity, score clamping, zero weights, cancellation, multiple entities, custom weights, ExtractTerms |
| **Total** | **57** | All v0.7.2f tests |

### Test Traits

All new tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.2f")]`

---

## Design Decisions

1. **New Interface, Not Replacing Existing** — `IEntityRelevanceScorer` is async (embedding calls) while `IEntityRelevanceRanker` is sync. The ranker still provides `SelectWithinBudget`. Both coexist; the provider preferentially uses the scorer when available.

2. **Nullable IEmbeddingService** — Not all deployments have embeddings configured. When null or when calls fail, the scorer gracefully falls back by redistributing the semantic weight proportionally to the four remaining signals. This ensures meaningful scores even without embedding infrastructure.

3. **ScoringRequest Over Existing Types** — `ContextGatheringRequest` carries agent-specific data (agentId, hints, cursor); `ContextRequest` is LLM-layer. A focused record with just Query + DocumentContent + PreferredTypes keeps the scorer decoupled from strategy concerns.

4. **Optional Scorer in Provider** — `KnowledgeContextProvider` accepts `IEntityRelevanceScorer?`. When null, it falls back to the existing `IEntityRelevanceRanker.RankEntities()` — zero disruption to existing behavior.

5. **Singleton Lifetime** — The scorer is stateless (config is immutable, no mutable state). Safe for concurrent use. Factory lambda in DI registration optionally resolves `IEmbeddingService`.

6. **NullLogger in Tests** — Moq cannot create proxies for `ILogger<InternalType>` from strong-named assemblies. Using `NullLogger<T>.Instance` follows the pattern established by `EntityCrudServiceTests` in the Knowledge module.

7. **ScoredEntity → RankedEntity Conversion** — The provider converts `ScoredEntity` results to `RankedEntity` for compatibility with `SelectWithinBudget`. This preserves the existing budget selection algorithm while benefiting from richer scoring.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Module | Used By |
|:----------|:-------|:--------|
| `IEmbeddingService` | Lexichord.Abstractions (v0.4.4a) | EntityRelevanceScorer (semantic similarity, optional) |
| `IEntityRelevanceRanker` | Lexichord.Abstractions (v0.6.6e) | KnowledgeContextProvider (fallback ranking, budget selection) |
| `KnowledgeEntity` | Lexichord.Abstractions (v0.4.5e) | EntityRelevanceScorer (scoring target) |
| `KnowledgeContextOptions` | Lexichord.Abstractions (v0.6.6e) | KnowledgeContextProvider (entity type preferences) |

### No New NuGet Packages

All dependencies are existing project references. `Microsoft.Extensions.Options` and `Microsoft.Extensions.Logging.Abstractions` were already referenced.

---

## Known Limitations

1. **No document content at provider level** — `KnowledgeContextProvider` passes `null` for `DocumentContent` in the `ScoringRequest`. The mention signal returns 0.0 at the provider level, but the other four signals provide meaningful scoring. Document content would need to be threaded from the strategy layer for full mention scoring.
2. **Static scoring configuration** — `ScoringConfig` defaults are compiled via `Options.Create()`. Custom weights require code changes or host-level `IConfiguration` binding.
3. **Entity type case sensitivity** — Type matching uses case-sensitive string comparison (`IReadOnlySet<string>.Contains()`), matching the `EntitySearchQuery.EntityTypes` contract.
4. **No semantic caching** — Embedding vectors are not cached between scoring calls. Each `ScoreEntitiesAsync` call computes fresh embeddings. For performance-critical paths, the `IEmbeddingService` implementation should provide its own caching layer.
