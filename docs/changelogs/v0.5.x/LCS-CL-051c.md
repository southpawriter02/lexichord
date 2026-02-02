# LCS-CL-051c: Hybrid Fusion Algorithm

## Document Control

| Field              | Value                           |
| :----------------- | :------------------------------ |
| **Document ID**    | LCS-CL-051c                     |
| **Version**        | v0.5.1c                         |
| **Feature Name**   | Hybrid Fusion Algorithm         |
| **Module**         | Lexichord.Modules.RAG           |
| **Status**         | Complete                        |
| **Last Updated**   | 2026-02-02                      |
| **Related Spec**   | [LCS-DES-v0.5.1c](../../specs/v0.5.x/v0.5.1/LCS-DES-v0.5.1c.md) |

---

## Summary

Implemented `IHybridSearchService` and `HybridSearchService` using Reciprocal Rank Fusion (RRF) to combine BM25 keyword search (v0.5.1b) and semantic vector search (v0.4.5a) results into a single ranked list. Chunks appearing in both result sets are naturally boosted, capturing both exact keyword matches and conceptual similarity.

---

## Changes

### New Files

#### Lexichord.Abstractions

| File | Description |
|:-----|:------------|
| `Contracts/RAG/IHybridSearchService.cs` | Interface for hybrid search combining BM25 and semantic search via RRF |

#### Lexichord.Modules.RAG

| File | Description |
|:-----|:------------|
| `Search/HybridSearchService.cs` | Core implementation: parallel sub-search execution, RRF fusion algorithm, telemetry |
| `Search/HybridSearchOptions.cs` | Configuration record for RRF weights (SemanticWeight, BM25Weight) and k constant |
| `Search/HybridSearchExecutedEvent.cs` | MediatR notification for telemetry with sub-search hit counts and fusion details |

#### Lexichord.Tests.Unit

| File | Description |
|:-----|:------------|
| `Modules/RAG/Search/HybridSearchServiceTests.cs` | 52 unit tests covering constructor validation, input validation, license gating, RRF algorithm, parallel execution, telemetry, preprocessing, and result assembly |

### Modified Files

| File | Change |
|:-----|:-------|
| `RAGModule.cs` | Added v0.5.1c service registration block for `IHybridSearchService` → `HybridSearchService` and `HybridSearchOptions` via Options pattern |

---

## Technical Details

### Search Pipeline

1. **Input Validation** — Query non-empty, TopK 1-100, MinScore 0.0-1.0
2. **License Check** — WriterPro+ tier required via `SearchLicenseGuard` (fail-fast before parallel execution)
3. **Query Preprocessing** — Whitespace normalization via `IQueryPreprocessor`
4. **Parallel Execution** — BM25 and semantic searches run concurrently via `Task.WhenAll` with expanded TopK (2× requested, capped at 100)
5. **RRF Fusion** — Merge ranked lists using composite key `(Document.Id, Chunk.Metadata.Index)`
6. **TopK Trim** — Take top K results from fused ranking
7. **Telemetry** — Publish `HybridSearchExecutedEvent` via MediatR

### RRF Algorithm

```
RRF_score(chunk) = Σ (weight_i / (k + rank_i))
```

Where:
- `weight_i` = SemanticWeight (default 0.7) or BM25Weight (default 0.3)
- `k` = RRF constant (default 60)
- `rank_i` = 1-based position in that ranking (0 contribution if absent)

Chunks appearing in both BM25 and semantic result sets receive combined RRF contributions, naturally boosting items relevant to both keyword and conceptual queries.

### Design Adaptations

The specification referenced `BM25Hit.ChunkId` and `hit.Chunk.Id`, but the actual codebase:
- Uses `SearchResult`/`SearchHit` for both BM25 and semantic search (no separate `BM25Hit` record)
- `TextChunk` has no `Id` property — chunks are identified by composite key `(Document.Id, Chunk.Metadata.Index)`

### Dependencies

| Dependency | Version | Purpose |
|:-----------|:--------|:--------|
| ISemanticSearchService | v0.4.5a | Vector similarity sub-search |
| IBM25SearchService | v0.5.1b | Full-text keyword sub-search |
| SearchLicenseGuard | v0.4.5b | License tier validation |
| IQueryPreprocessor | v0.4.5c | Query normalization |
| SearchOptions/SearchResult | v0.4.5a | Shared search types |

---

## Verification

### Unit Tests

All 52 tests passed:

- Constructor null-parameter validation (8 tests)
- Input validation for query, TopK, MinScore (11 tests)
- License tier gating (5 tests)
- RRF algorithm correctness (8 tests)
- Parallel execution verification (3 tests)
- Telemetry event publishing (3 tests)
- Preprocessing delegation (2 tests)
- Result assembly (3 tests)
- Interface implementation (1 test)

### Build Verification

```
dotnet build
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Regression Check

```
dotnet test --filter "Category=Unit"
Passed: 2560, Skipped: 27, Failed: 0
```

---

## Related Documents

- [LCS-DES-v0.5.1c](../../specs/v0.5.x/v0.5.1/LCS-DES-v0.5.1c.md) — Design specification
- [LCS-CL-051b](./LCS-CL-051b.md) — BM25 Search Implementation (prerequisite)
- [LCS-CL-051a](./LCS-CL-051a.md) — BM25 Index Schema (prerequisite)
- [LCS-DES-v0.5.1-INDEX](../../specs/v0.5.x/v0.5.1/LCS-DES-v0.5.1-INDEX.md) — Feature index
