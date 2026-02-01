# Changelog: v0.4.5b - Vector Search Query

**Release Date:** 2026-02-01
**Status:** Implemented
**Specification:** [LCS-DES-v0.4.5b](../../specs/v0.4.x/v0.4.5/LCS-DES-v0.4.5b.md)

---

## Summary

Implements `PgVectorSearchService`, the core semantic search service that executes cosine similarity queries against indexed document chunks using pgvector. Also introduces `SearchLicenseGuard` for WriterPro tier enforcement, `IQueryPreprocessor` interface with a passthrough stub, `ChunkSearchRow` for Dapper result mapping, and MediatR notification events for search telemetry and license denial tracking.

---

## Changes

### Added

#### Lexichord.Modules.RAG/Search/

| File                              | Type          | Description                                                        |
| :-------------------------------- | :------------ | :----------------------------------------------------------------- |
| `IQueryPreprocessor.cs`           | Interface     | Contract for query normalization, abbreviation expansion, and embedding caching |
| `PassthroughQueryPreprocessor.cs` | Class         | Temporary no-op stub (trimming only); replaced by v0.4.5c          |
| `SearchEvents.cs`                 | Records       | `SemanticSearchExecutedEvent` and `SearchDeniedEvent` MediatR notifications |
| `SearchLicenseGuard.cs`           | Class         | License tier validation with throw, try/publish, and property modes |
| `ChunkSearchRow.cs`               | Internal Record | Dapper result mapping for pgvector cosine similarity queries       |
| `PgVectorSearchService.cs`       | Class         | Core `ISemanticSearchService` implementation using pgvector `<=>` operator |

#### Lexichord.Tests.Unit/Modules/RAG/Search/

| File                              | Tests | Coverage                                                          |
| :-------------------------------- | :---- | :---------------------------------------------------------------- |
| `SearchEventsTests.cs`            | 14    | Record construction, defaults, equality, INotification, tier recording |
| `SearchLicenseGuardTests.cs`      | 18    | Constructor, CurrentTier, IsSearchAvailable, EnsureSearchAuthorized, TryAuthorizeSearchAsync |
| `PgVectorSearchServiceTests.cs`   | 27    | Constructor, input validation, license gating, preprocessing, caching, interface impl |

### Modified

#### Lexichord.Modules.RAG/

| File             | Change                                                              |
| :--------------- | :------------------------------------------------------------------ |
| `RAGModule.cs`   | Added v0.4.5 DI registrations (SearchLicenseGuard, IQueryPreprocessor, ISemanticSearchService); bumped version to 0.4.5 |

---

## Technical Details

### PgVectorSearchService Pipeline

| Step | Operation                  | Component               | Description                                 |
| :--- | :------------------------- | :---------------------- | :------------------------------------------ |
| 1    | Validate inputs            | PgVectorSearchService   | Query non-empty, TopK 1-100, MinScore 0-1   |
| 2    | License check              | SearchLicenseGuard      | WriterPro+ required; throws if unauthorized |
| 3    | Preprocess query           | IQueryPreprocessor      | Normalize whitespace (stub trims only)       |
| 4    | Get embedding              | IEmbeddingService       | Cache check → generate → cache store         |
| 5    | Execute SQL                | pgvector `<=>` operator | Cosine similarity with score threshold       |
| 6    | Map results                | ChunkSearchRow → SearchHit | Document caching, metadata construction   |
| 7    | Publish telemetry          | IMediator               | SemanticSearchExecutedEvent                  |
| 8    | Return result              | SearchResult            | Hits, Duration, QueryEmbedding, WasTruncated |

### SQL Query Pattern

```sql
SELECT
    c."Id", c."DocumentId", c."Content", c."ChunkIndex",
    c."Metadata", c."Heading", c."HeadingLevel",
    COALESCE(c."StartOffset", 0) AS "StartOffset",
    COALESCE(c."EndOffset", 0) AS "EndOffset",
    1 - (c."Embedding" <=> @query_embedding::vector) AS "Score"
FROM "Chunks" c
WHERE c."Embedding" IS NOT NULL
  AND 1 - (c."Embedding" <=> @query_embedding::vector) >= @min_score
ORDER BY c."Embedding" <=> @query_embedding::vector ASC
LIMIT @top_k
```

### SearchLicenseGuard Modes

| Method                     | Behavior                                           | Use Case          |
| :------------------------- | :------------------------------------------------- | :---------------- |
| `EnsureSearchAuthorized()` | Throws `FeatureNotLicensedException` if unlicensed | Service methods   |
| `TryAuthorizeSearchAsync()`| Returns bool, publishes `SearchDeniedEvent`        | UI code           |
| `IsSearchAvailable`        | Property check, no side effects                    | Element visibility |

### DI Registrations (RAGModule.cs)

| Service                    | Lifetime | Implementation                |
| :------------------------- | :------- | :---------------------------- |
| `SearchLicenseGuard`       | Singleton | Direct (concrete class)      |
| `IQueryPreprocessor`       | Singleton | `PassthroughQueryPreprocessor` |
| `ISemanticSearchService`   | Scoped   | `PgVectorSearchService`       |

### Spec Adaptations

| Spec                        | Actual Code                           | Reason                         |
| :-------------------------- | :------------------------------------ | :----------------------------- |
| `_licenseContext.Tier`      | `_licenseContext.GetCurrentTier()`    | ILicenseContext uses method    |
| 3-arg FeatureNotLicensedException | 2-arg `(message, requiredTier)` | Actual constructor signature   |
| `DynamicParameters`         | Anonymous types                       | Project convention             |
| lowercase SQL columns       | Double-quoted PascalCase (`"Id"`)     | Schema convention (Migration_003) |

---

## Verification

```bash
# Build solution
dotnet build
# Result: Build succeeded, 0 warnings, 0 errors

# Run v0.4.5b tests
dotnet test tests/Lexichord.Tests.Unit --filter "Feature=v0.4.5b"
# Result: 81 tests passed (includes v0.4.5a tests discovered via same filter pattern)

# Run full regression
dotnet test tests/Lexichord.Tests.Unit
# Result: 4016 passed, 0 failed, 33 skipped (4049 total)
```

---

## Test Coverage

| Category                          | Tests |
| :-------------------------------- | ----: |
| SearchEvents construction         |     7 |
| SearchEvents equality/defaults    |     4 |
| SearchEvents tier recording       |     3 |
| SearchLicenseGuard constructor    |     4 |
| SearchLicenseGuard properties     |     8 |
| SearchLicenseGuard EnsureSearch   |     4 |
| SearchLicenseGuard TryAuthorize   |     6 |
| PgVectorSearchService constructor |     8 |
| PgVectorSearchService validation  |    10 |
| PgVectorSearchService license     |     4 |
| PgVectorSearchService preprocessing |   2 |
| PgVectorSearchService caching     |     3 |
| PgVectorSearchService interface   |     1 |
| **Total**                         | **59** |

---

## Dependencies

- v0.4.5a: `ISemanticSearchService`, `SearchOptions`, `SearchResult`, `SearchHit`
- v0.4.4a: `IEmbeddingService` for query embedding generation
- v0.4.4d: `FeatureNotLicensedException` for license enforcement
- v0.4.3a: `TextChunk`, `ChunkMetadata` records
- v0.4.1c: `IDocumentRepository`, `Document` entity
- v0.0.5b: `IDbConnectionFactory` for database connections
- v0.0.4c: `ILicenseContext`, `LicenseTier` for tier checks
- v0.0.7a: `IMediator` for event publishing

## Dependents

- v0.4.5c: Query Preprocessing (implements IQueryPreprocessor, replaces PassthroughQueryPreprocessor)
- v0.4.5d: License Gating (SearchLicenseGuard already implemented here)
- v0.4.6: Reference Panel (consumes ISemanticSearchService)

---

## Related Documents

- [LCS-DES-v0.4.5b](../../specs/v0.4.x/v0.4.5/LCS-DES-v0.4.5b.md) - Design specification
- [LCS-SBD-v0.4.5 §3.2](../../specs/v0.4.x/v0.4.5/LCS-SBD-v0.4.5.md#32-v045b-vector-search-query) - Scope breakdown
- [LCS-DES-v0.4.5-INDEX](../../specs/v0.4.x/v0.4.5/LCS-DES-v0.4.5-INDEX.md) - Version index
- [LCS-CL-v0.4.5a](./LCS-CL-v0.4.5a.md) - Previous version (Search Abstractions)
