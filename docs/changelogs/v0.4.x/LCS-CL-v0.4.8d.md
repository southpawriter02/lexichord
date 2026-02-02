# v0.4.8d: Embedding Cache

## Overview

**Version:** v0.4.8d  
**Date:** 2026-02-02  
**Status:** ✅ Implemented

Local SQLite-based embedding cache to reduce API costs and improve latency for repeated queries.

---

## New Components

### Interface

| Component                  | Location                            | Purpose                                                                  |
| -------------------------- | ----------------------------------- | ------------------------------------------------------------------------ |
| `IEmbeddingCache`          | `Lexichord.Abstractions/Contracts/` | Cache contract with `TryGet`, `Set`, `GetStatistics`, `Clear`, `Compact` |
| `EmbeddingCacheStatistics` | `Lexichord.Abstractions/Contracts/` | Record exposing cache metrics                                            |

### Configuration

| Component               | Location                               | Purpose                                                       |
| ----------------------- | -------------------------------------- | ------------------------------------------------------------- |
| `EmbeddingCacheOptions` | `Lexichord.Modules.RAG/Configuration/` | Configuration for cache (Enabled, MaxSizeMB, CachePath, etc.) |

### Services

| Component                | Location                          | Purpose                                                    |
| ------------------------ | --------------------------------- | ---------------------------------------------------------- |
| `SqliteEmbeddingCache`   | `Lexichord.Modules.RAG/Services/` | SQLite-based cache with LRU eviction                       |
| `CachedEmbeddingService` | `Lexichord.Modules.RAG/Services/` | Decorator for `IEmbeddingService` with transparent caching |

---

## Key Features

- **Content Hash Keying**: SHA-256 hash of content for efficient lookups
- **LRU Eviction**: Least Recently Used eviction when cache size limit exceeded
- **Binary Serialization**: Efficient `float[]` ↔ `byte[]` conversion for storage
- **Batch Optimization**: Partial cache hits handled efficiently in batch operations
- **Configurable**: Enable/disable, max size, cache path, compaction interval

---

## DI Registration

The cache is registered in `RAGModule.RegisterServices()` as follows:

```csharp
// v0.4.8d: Embedding Cache
services.AddSingleton(Options.Create(EmbeddingCacheOptions.Default));
services.AddSingleton<IEmbeddingCache, SqliteEmbeddingCache>();

// Decorate IEmbeddingService
services.AddSingleton<IEmbeddingService>(sp =>
{
    var inner = sp.GetRequiredService<OpenAIEmbeddingService>();
    var cache = sp.GetRequiredService<IEmbeddingCache>();
    var options = sp.GetRequiredService<IOptions<EmbeddingCacheOptions>>();
    var logger = sp.GetRequiredService<ILogger<CachedEmbeddingService>>();
    return new CachedEmbeddingService(inner, cache, options, logger);
});
```

---

## Configuration Options

| Option                | Default              | Description                    |
| --------------------- | -------------------- | ------------------------------ |
| `Enabled`             | `true`               | Toggle caching on/off          |
| `MaxSizeMB`           | `100`                | Maximum cache size in MB       |
| `CachePath`           | `embedding_cache.db` | SQLite database file path      |
| `EmbeddingDimensions` | `1536`               | Expected embedding vector size |
| `CompactionInterval`  | `1 hour`             | Compaction check interval      |

---

## Dependencies Added

- `Microsoft.Data.Sqlite` v9.0.0 (RAG module and test project)

---

## Unit Tests

| Test File                        | Tests | Coverage                                                          |
| -------------------------------- | ----- | ----------------------------------------------------------------- |
| `SqliteEmbeddingCacheTests.cs`   | 20    | TryGet, Set, GetStatistics, Clear, Compact, ComputeContentHash    |
| `CachedEmbeddingServiceTests.cs` | 14    | Constructor, EmbedAsync, EmbedBatchAsync, pass-through properties |

**Total: 34 tests, 34 passed** ✅

---

## Files Changed

### New Files

- [IEmbeddingCache.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/IEmbeddingCache.cs)
- [EmbeddingCacheOptions.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.RAG/Configuration/EmbeddingCacheOptions.cs)
- [SqliteEmbeddingCache.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.RAG/Services/SqliteEmbeddingCache.cs)
- [CachedEmbeddingService.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.RAG/Services/CachedEmbeddingService.cs)
- [SqliteEmbeddingCacheTests.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Modules/RAG/Services/SqliteEmbeddingCacheTests.cs)
- [CachedEmbeddingServiceTests.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Modules/RAG/Services/CachedEmbeddingServiceTests.cs)

### Modified Files

- [Lexichord.Modules.RAG.csproj](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.RAG/Lexichord.Modules.RAG.csproj) - Added Microsoft.Data.Sqlite
- [RAGModule.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.RAG/RAGModule.cs) - DI registrations
- [Lexichord.Tests.Unit.csproj](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Lexichord.Tests.Unit.csproj) - Added Microsoft.Data.Sqlite
