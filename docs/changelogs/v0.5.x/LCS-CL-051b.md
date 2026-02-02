# LCS-CL-051b: BM25 Search Implementation

## Document Control

| Field              | Value                           |
| :----------------- | :------------------------------ |
| **Document ID**    | LCS-CL-051b                     |
| **Version**        | v0.5.1b                         |
| **Feature Name**   | BM25 Search Implementation      |
| **Module**         | Lexichord.Modules.RAG           |
| **Status**         | Complete                        |
| **Last Updated**   | 2026-02-02                      |

---

## Summary

Implemented the `IBM25SearchService` interface and `BM25SearchService` class for keyword-based full-text search using PostgreSQL's `ts_rank()` function against the `ContentTsVector` column created in v0.5.1a.

---

## Changes

### New Files

#### Lexichord.Abstractions

| File | Description |
|:-----|:------------|
| `Contracts/RAG/IBM25SearchService.cs` | Interface for BM25 keyword search with extensive XML documentation |

#### Lexichord.Modules.RAG

| File | Description |
|:-----|:------------|
| `Search/BM25SearchService.cs` | Core implementation using PostgreSQL full-text search |
| `Search/BM25SearchExecutedEvent.cs` | MediatR notification for telemetry |

#### Lexichord.Tests.Unit

| File | Description |
|:-----|:------------|
| `Modules/RAG/Search/BM25SearchServiceTests.cs` | 35 unit tests covering constructor validation, input validation, license gating, and preprocessing |

### Modified Files

| File | Change |
|:-----|:-------|
| `RAGModule.cs` | Added v0.5.1b service registration block for `IBM25SearchService` → `BM25SearchService` |

---

## Technical Details

### Search Pipeline

1. **Input Validation** — Query non-empty, TopK 1-100, MinScore 0.0-1.0
2. **License Check** — WriterPro+ tier required via `SearchLicenseGuard`
3. **Query Preprocessing** — Whitespace normalization via `IQueryPreprocessor`
4. **SQL Execution** — PostgreSQL full-text search with `ts_rank()`
5. **Result Mapping** — Document caching to avoid N+1 queries
6. **Telemetry** — Publish `BM25SearchExecutedEvent` via MediatR

### SQL Pattern

```sql
SELECT *, ts_rank(c."ContentTsVector", plainto_tsquery('english', @query)) AS "Score"
FROM "Chunks" c
WHERE c."ContentTsVector" @@ plainto_tsquery('english', @query)
  AND ts_rank(...) >= @min_score
ORDER BY ts_rank(...) DESC
LIMIT @top_k
```

### Dependencies

| Dependency | Version | Purpose |
|:-----------|:--------|:--------|
| ContentTsVector column | v0.5.1a | Full-text search column |
| GIN index | v0.5.1a | Query performance |
| SearchLicenseGuard | v0.4.5b | License tier validation |
| IQueryPreprocessor | v0.4.5c | Query normalization |
| SearchOptions/SearchResult | v0.4.5a | Shared search types |

---

## Verification

### Unit Tests

All 35 tests passed:

- Constructor null-parameter validation (6 tests)
- Input validation for query, TopK, MinScore (11 tests)
- License tier gating (5 tests)
- Query preprocessing delegation (2 tests)
- License check ordering (1 test)
- Interface implementation (1 test)
- Valid parameter boundary tests (9 tests)

### Build Verification

```
dotnet build src/Lexichord.Modules.RAG --configuration Debug
Build succeeded in 8.6s
```

---

## Related Documents

- [LCS-DES-v0.5.1b](../specs/v0.5.x/v0.5.1/LCS-DES-v0.5.1b.md) — Design specification
- [LCS-CL-051a](./LCS-CL-051a.md) — BM25 Index Schema (prerequisite)
- [LCS-DES-v0.5.1-INDEX](../specs/v0.5.x/v0.5.1/LCS-DES-v0.5.1-INDEX.md) — Feature index
