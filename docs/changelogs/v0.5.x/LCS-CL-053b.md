# LCS-CL-053b: Sibling Chunk Retrieval

**Version:** v0.5.3b
**Date:** 2026-02
**Status:** ✅ Complete

## Summary

Implemented dedicated `SiblingCache` class providing LRU-based caching for sibling chunk queries with automatic document-level invalidation via MediatR event handlers. Integrated with `ChunkRepository.GetSiblingsAsync()` for transparent caching.

## Changes

### Abstractions (`Lexichord.Abstractions`)

| File                | Change                                              |
| ------------------- | --------------------------------------------------- |
| `SiblingCacheKey.cs` | New record struct for cache key (doc, center, before, after) |

### Data (`Lexichord.Modules.RAG`)

| File                  | Change                                                              |
| --------------------- | ------------------------------------------------------------------- |
| `SiblingCache.cs`     | New LRU cache class with MediatR event handlers                     |
| `ChunkRepository.cs`  | Integrated SiblingCache for cache-first lookup in `GetSiblingsAsync` |

### Module (`Lexichord.Modules.RAG`)

| File           | Change                                                    |
| -------------- | --------------------------------------------------------- |
| `RAGModule.cs` | Registered `SiblingCache` and MediatR notification handlers |

## Key Features

### SiblingCache

- **LRU Eviction**: MaxEntries = 500, EvictionBatch = 50
- **Thread-Safe**: Uses `ConcurrentDictionary<SiblingCacheKey, CacheEntry>` for concurrent access
- **Document Invalidation**: Subscribes to `DocumentIndexedEvent` and `DocumentRemovedFromIndexEvent`
- **Access Tracking**: Updates `LastAccessed` timestamp on cache hit for LRU ordering

### ChunkRepository Integration

- **Cache-First Lookup**: Checks cache before querying database
- **Automatic Population**: Stores query results in cache after database fetch
- **Parameter Clamping**: Validates and clamps beforeCount/afterCount to [0, 5] range
- **Logging**: Debug logs for cache hits, misses, and query details

## Tests

| File                     | Tests                                                  |
| ------------------------ | ------------------------------------------------------ |
| `SiblingCacheTests.cs`   | 22 tests - Constructor, TryGet/Set, InvalidateDocument, Clear, LRU eviction, MediatR handlers, SiblingCacheKey equality |
| `ChunkRepositoryTests.cs` | 5 tests - Updated for SiblingCache dependency (constructor validation) |

**Total: 27 unit tests**

## Dependencies

- `DocumentIndexedEvent` (v0.4.4d) - Triggers cache invalidation on re-indexing
- `DocumentRemovedFromIndexEvent` (v0.4.7b) - Triggers cache invalidation on removal
- `MediatR.INotificationHandler<T>` (NuGet) - Event subscription pattern
- `Microsoft.Extensions.Logging` (existing) - Diagnostic logging

## Acceptance Criteria Met

| # | Criterion | Verification |
|---|-----------|--------------|
| 1 | `GetSiblingsAsync` returns correct chunks by index range | Unit test |
| 2 | Results are ordered by chunk_index ascending | Unit test |
| 3 | Query uses idx_chunks_document_index | SQL EXPLAIN |
| 4 | Second query for same parameters uses cache | Unit test |
| 5 | DocumentIndexedEvent invalidates document cache | Unit test |
| 6 | DocumentRemovedFromIndexEvent invalidates document cache | Unit test |
| 7 | LRU eviction triggers at MaxEntries | Unit test |
