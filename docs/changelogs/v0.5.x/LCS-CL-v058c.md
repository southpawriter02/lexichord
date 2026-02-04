# Changelog: v0.5.8c — Caching Strategy

**Status:** ✅ Complete  
**Completed:** 2026-02-03

---

## Summary

Implements a multi-layer in-memory caching system for the RAG subsystem, reducing redundant database queries and embedding API calls for repeated searches.

---

## Changes

### Abstractions (`Lexichord.Abstractions`)

| File | Type | Description |
|------|------|-------------|
| `Contracts/RAG/IQueryResultCache.cs` | Interface | Global cache for search results with LRU+TTL eviction |
| `Contracts/RAG/IContextExpansionCache.cs` | Interface | Session-scoped cache for expanded context |
| `Contracts/RAG/CacheStatistics.cs` | Record | Cache performance metrics (hits, misses, eviction count) |

### Configuration (`Lexichord.Modules.RAG/Configuration`)

| File | Description |
|------|-------------|
| `QueryCacheOptions.cs` | MaxEntries=100, TtlSeconds=300, Enabled flag |
| `ContextCacheOptions.cs` | MaxEntriesPerSession=50, Enabled flag |

### Implementations (`Lexichord.Modules.RAG/Services`)

| File | Description |
|------|-------------|
| `QueryResultCache.cs` | LRU eviction, TTL expiration, `ReaderWriterLockSlim` thread safety, document-aware invalidation |
| `ContextExpansionCacheService.cs` | Session isolation via nested `ConcurrentDictionary`, per-session eviction |
| `CacheInvalidationHandler.cs` | MediatR handler for `DocumentIndexedEvent` and `DocumentRemovedFromIndexEvent` |
| `CacheKeyGenerator.cs` | SHA256 deterministic key generation from normalized query + options |

### Module Registration

| File | Change |
|------|--------|
| `RAGModule.cs` | Added v0.5.8c section registering all cache services with `IOptions<T>` pattern |

---

## Tests

| Test Class | Count | Coverage |
|------------|-------|----------|
| `QueryResultCacheTests.cs` | 15+ | LRU, TTL, invalidation, disabled cache, statistics |
| `ContextExpansionCacheServiceTests.cs` | 12+ | Session isolation, eviction, invalidation |
| `CacheInvalidationHandlerTests.cs` | 6 | Event handling, error resilience |
| `CacheKeyGeneratorTests.cs` | 11 | Determinism, normalization, hash format |

**Total:** 44+ unit tests

---

## Dependencies

- `Microsoft.Extensions.Options` — Configuration binding
- `MediatR` — Event-driven cache invalidation
- No new NuGet packages required

---

## Breaking Changes

None. Caching is opt-in and transparent to existing consumers.
