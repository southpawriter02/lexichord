# Changelog: v0.5.9f – Retrieval Integration

**Feature ID:** RAG-DEDUP-06  
**Version:** 0.5.9f  
**Date:** 2026-02-04  
**Status:** ✅ Complete

---

## Overview

This release implements **Retrieval Integration**, the final component that connects the Semantic Memory Deduplication pipeline (v0.5.9) to the search layer. Search queries now automatically respect canonical records, filtering out variant chunks by default while providing enriched metadata about merged variants, contradictions, and provenance for display in search results.

---

## What's New

### 🆕 New Features

#### SearchSimilarWithDeduplicationAsync Method
A new deduplication-aware search method in `IChunkRepository`:

- **Canonical-Aware Filtering**: Returns only canonical chunks by default, filtering out variants
- **Variant Metadata**: Optionally loads count of merged variants per result
- **Contradiction Detection**: Flags results with active contradiction conflicts
- **Provenance Loading**: Optionally includes full provenance history via `ICanonicalManager`
- **Backward Compatible**: Original `SearchSimilarAsync` remains unchanged

#### DeduplicatedSearchResult Record
A new result type extending search responses with deduplication metadata:

- **`CanonicalRecordId`**: Links to the canonical record if the chunk is a canonical
- **`VariantCount`**: Number of variants merged into this canonical
- **`HasContradictions`**: Whether active contradictions exist for this chunk
- **`Provenance`**: Optional list of provenance records with source document history
- **Helper Properties**: `IsCanonical`, `IsStandalone`, `HasVariants`, `HasProvenance`
- **Factory Method**: `FromBasicResult` for converting legacy search results

#### Enhanced SearchOptions
New deduplication-aware properties in `SearchOptions`:

- **`RespectCanonicals`** (default: `true`): Filter out variant chunks, return canonicals only
- **`IncludeVariantMetadata`** (default: `false`): Load variant counts per result
- **`IncludeArchived`** (default: `false`): Include archived/hidden chunks
- **`IncludeProvenance`** (default: `false`): Load full provenance history

#### Database Migration
- **`Migration_011_RetrievalIntegrationIndexes`**: Creates optimized indexes for deduplication-aware search queries
  - Covering index on `CanonicalRecords.CanonicalChunkId`
  - Index on `ChunkVariants.VariantChunkId`
  - Partial indexes on `Contradictions` for status filtering

### 🔄 Modified Components

#### ChunkRepository
- Injected `ICanonicalManager` as a new constructor dependency
- Implemented `SearchSimilarWithDeduplicationAsync` with canonical-aware SQL query
- Added helper methods for query building and parameter construction
- Added `DeduplicatedChunkRow` internal record for query result mapping

#### ChunkRepositoryTests
- Updated to include `ICanonicalManager` mock in constructor calls
- Added new test for null canonical manager validation

---

## Technical Details

### Search Query Strategy

The `SearchSimilarWithDeduplicationAsync` method uses a strategic SQL approach:

1. **Base Query**: Standard pgvector cosine similarity search on `chunks` table
2. **Canonical Join**: `LEFT JOIN` to `CanonicalRecords` to identify canonical chunks
3. **Variant Filter**: `LEFT JOIN` to `ChunkVariants` with `WHERE` clause excluding variants when `RespectCanonicals = true`
4. **Subquery Aggregations**: Conditional subqueries for variant count and contradiction status when metadata is requested

```sql
SELECT c.*, 
       1 - (c.embedding <=> @QueryEmbedding::vector) AS SimilarityScore,
       cr."Id" AS CanonicalRecordId,
       (SELECT COUNT(*) FROM "ChunkVariants" cv WHERE cv."CanonicalRecordId" = cr."Id") AS VariantCount,
       EXISTS(SELECT 1 FROM "Contradictions" ct WHERE ct."ChunkAId" = c.id OR ct."ChunkBId" = c.id) AS HasContradictions
FROM chunks c
LEFT JOIN "CanonicalRecords" cr ON c.id = cr."CanonicalChunkId"
LEFT JOIN "ChunkVariants" cv ON c.id = cv."VariantChunkId"
WHERE c.embedding IS NOT NULL
  AND 1 - (c.embedding <=> @QueryEmbedding::vector) >= @Threshold
  AND cv."VariantChunkId" IS NULL  -- Exclude variants
ORDER BY c.embedding <=> @QueryEmbedding::vector
LIMIT @TopK
```

### Provenance Loading Strategy

Provenance is loaded in a second pass to avoid query complexity:
1. Collect `CanonicalRecordId` values from initial results
2. Batch load provenance via `ICanonicalManager.GetProvenanceAsync`
3. Attach to results via dictionary lookup

### Default Behavior

| Option | Default | Effect |
|--------|---------|--------|
| `RespectCanonicals` | `true` | Filter out variant chunks |
| `IncludeVariantMetadata` | `false` | Variant counts not loaded |
| `IncludeArchived` | `false` | Archived chunks excluded |
| `IncludeProvenance` | `false` | Provenance not loaded |

---

## Files Changed

### New Files

| File | Description |
|------|-------------|
| `Lexichord.Abstractions/Contracts/RAG/DeduplicatedSearchResult.cs` | Extended search result with deduplication metadata |
| `Lexichord.Infrastructure/Migrations/Migration_011_RetrievalIntegrationIndexes.cs` | Database indexes for query optimization |
| `Lexichord.Tests.Unit/Abstractions/RAG/DeduplicatedSearchResultTests.cs` | Unit tests for DeduplicatedSearchResult |

### Modified Files

| File | Change |
|------|--------|
| `Lexichord.Abstractions/Contracts/SearchOptions.cs` | Added deduplication-aware properties |
| `Lexichord.Abstractions/Contracts/RAG/IChunkRepository.cs` | Added `SearchSimilarWithDeduplicationAsync` method |
| `Lexichord.Modules.RAG/Data/ChunkRepository.cs` | Implemented deduplication-aware search with ICanonicalManager dependency |
| `Lexichord.Tests.Unit/Modules/RAG/ChunkRepositoryTests.cs` | Updated for ICanonicalManager constructor parameter |

---

## Testing

### Unit Tests Added

- `DeduplicatedSearchResultTests`: 20 tests covering:
  - Constructor property storage
  - Null handling for optional properties
  - Helper properties (`IsCanonical`, `IsStandalone`, `HasVariants`, `HasProvenance`)
  - Factory method `FromBasicResult`
  - Record equality and `with` expressions
  - Edge cases (multiple variants, contradictions, provenance ordering)

### Test Coverage

- All 20 new `DeduplicatedSearchResultTests` pass ✅
- All 8 `ChunkRepositoryTests` pass with updated constructor ✅
- No regressions in existing test suites ✅

---

## Dependencies

### Prerequisites

- v0.5.9a: ISimilarityDetector ✅
- v0.5.9b: IRelationshipClassifier ✅
- v0.5.9c: ICanonicalManager ✅
- v0.5.9d: IDeduplicationService ✅
- v0.5.9e: IContradictionService ✅

### Database

- Requires `Migration_011_RetrievalIntegrationIndexes` to be applied
- Depends on all previous v0.5.9 migrations

---

## Migration Notes

### Applying the Migration

```bash
dotnet run --project src/Lexichord.Host -- migrate up
```

### Rollback

```bash
dotnet run --project src/Lexichord.Host -- migrate down --version 11
```

---

## Feature Completion

With v0.5.9f complete, the **Semantic Memory Deduplication** feature is fully implemented:

1. **v0.5.9a**: Similarity Detection ✅
2. **v0.5.9b**: Relationship Classification ✅
3. **v0.5.9c**: Canonical Record Management ✅
4. **v0.5.9d**: Deduplication Service ✅
5. **v0.5.9e**: Contradiction Detection ✅
6. **v0.5.9f**: Retrieval Integration ✅

### End-to-End Flow

```
Document Ingestion → Chunking → Embedding
                               ↓
                    SimilarityDetector (v0.5.9a)
                               ↓
                    RelationshipClassifier (v0.5.9b)
                               ↓
          ┌────────────────────┴────────────────────┐
          │                    │                    │
    CanonicalManager      Contradictions       Standalone
       (v0.5.9c)          flagged (v0.5.9e)    (no match)
          │                    │                    
          └────────────────────┴────────────────────┘
                               ↓
                    Search Query (user initiates)
                               ↓
              SearchSimilarWithDeduplicationAsync (v0.5.9f)
                               ↓
                    ┌──────────┴──────────┐
                    │ Canonical chunks    │ Variant chunks
                    │ returned with       │ filtered out
                    │ enriched metadata   │
                    └─────────────────────┘
```

---

## API Reference

### SearchSimilarWithDeduplicationAsync

```csharp
Task<IReadOnlyList<DeduplicatedSearchResult>> SearchSimilarWithDeduplicationAsync(
    float[] queryEmbedding,
    SearchOptions options,
    Guid? projectId = null,
    CancellationToken cancellationToken = default);
```

### DeduplicatedSearchResult

```csharp
public record DeduplicatedSearchResult(
    Chunk Chunk,
    double SimilarityScore,
    Guid? CanonicalRecordId,
    int VariantCount,
    bool HasContradictions,
    IReadOnlyList<ChunkProvenance>? Provenance)
{
    /// <summary>Whether this chunk is a canonical (has an associated canonical record).</summary>
    public bool IsCanonical => CanonicalRecordId.HasValue;

    /// <summary>Whether this is a standalone chunk (not canonical, not a variant).</summary>
    public bool IsStandalone => !IsCanonical && VariantCount == 0;

    /// <summary>Whether this canonical has one or more merged variants.</summary>
    public bool HasVariants => VariantCount > 0;

    /// <summary>Whether provenance information was loaded.</summary>
    public bool HasProvenance => Provenance is { Count: > 0 };

    /// <summary>Creates from a basic ChunkSearchResult (standalone conversion).</summary>
    public static DeduplicatedSearchResult FromBasicResult(ChunkSearchResult result);
}
```

### SearchOptions (Deduplication)

```csharp
public record SearchOptions
{
    // ... existing properties ...

    /// <summary>Filter out variant chunks, return canonicals only.</summary>
    public bool RespectCanonicals { get; init; } = true;

    /// <summary>Load variant counts per result.</summary>
    public bool IncludeVariantMetadata { get; init; } = false;

    /// <summary>Include archived/hidden chunks.</summary>
    public bool IncludeArchived { get; init; } = false;

    /// <summary>Load full provenance history.</summary>
    public bool IncludeProvenance { get; init; } = false;
}
```

---

**Author:** Lexichord Team  
**Reviewed:** Automated CI  
**Approved:** 2026-02-04
