# v0.6.6e — Graph Context Provider

**Released:** 2026-02-10
**Feature:** Co-pilot Agent (CKVS Phase 3b)
**Spec:** [LCS-DES-v0.6.6-KG-e](../../specs/v0.6.x/v0.6.6/LCS-DES-v0.6.6-KG-e.md)

## Summary

Implements the knowledge graph context retrieval pipeline for Co-pilot prompt injection. The pipeline searches for relevant entities, ranks them by query relevance, selects within a token budget, optionally enriches with relationships/axioms/claims, and formats the assembled context for LLM consumption.

## New Files

### Abstractions (`Lexichord.Abstractions`)

| File | Description |
|------|-------------|
| `EntitySearchQuery.cs` | Query parameters for entity search (query text, max results, type filter, project scope) |
| `KnowledgeContext.cs` | Aggregated retrieval result with entities, relationships, axioms, claims, formatted output, and token count |
| `KnowledgeContextOptions.cs` | Configuration record with token budget, entity limits, format selection, and enrichment flags |
| `RankedEntity.cs` | Entity paired with relevance score, estimated tokens, and matched query terms |
| `IKnowledgeContextProvider.cs` | Orchestrator interface: `GetContextAsync(query)` and `GetContextForEntitiesAsync(ids)` |
| `IEntityRelevanceRanker.cs` | Ranking interface: `RankEntities(query, entities)` and `SelectWithinBudget(ranked, budget)` |
| `IKnowledgeContextFormatter.cs` | Formatter interface: `FormatContext(entities, rels, axioms, options)` and `EstimateTokens(text)` |

### Modified Abstractions

| File | Change |
|------|--------|
| `IGraphRepository.cs` | Added `SearchEntitiesAsync(EntitySearchQuery, CancellationToken)` |

### Implementations (`Lexichord.Modules.Knowledge`)

| File | Description |
|------|-------------|
| `KnowledgeContextProvider.cs` | Full pipeline orchestrator: search → rank → select → enrich → format |
| `EntityRelevanceRanker.cs` | Term-based relevance scoring (name 3x, type 2x, properties 1x) with greedy budget selection |
| `KnowledgeContextFormatter.cs` | Multi-format output: Markdown, YAML, JSON, Plain text |
| `GraphRepository.cs` | Added `SearchEntitiesAsync` — in-memory filtering (future: Cypher full-text index) |
| `KnowledgeModule.cs` | DI registrations: Ranker (singleton), Formatter (singleton), Provider (scoped) |

## Design Decisions

- **`IContextFormatter` → `IKnowledgeContextFormatter`** — Renamed to avoid collision with existing `IContextFormatter` in `Lexichord.Modules.Agents`.
- **Character-based token estimation** (`length / 4`) — Avoids dependency on internal `ITokenizer` from the LLM module.
- **`SearchEntitiesAsync` uses in-memory filtering** — Future optimization will use Neo4j full-text indexing for large graphs.
- **Claims limited to 5 entities × 3 claims** — Performance-bounded to prevent excessive token usage.

## Verification

- **Unit Tests:** 16/16 passed (4 ranker + 7 formatter + 5 provider)
- **Regression:** 8075 passed, 33 skipped, 0 new failures
- **Build:** Both `Lexichord.Abstractions` and `Lexichord.Modules.Knowledge` compile cleanly
