# LCS-DES-048d: Embedding Cache

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-048d                             |
| **Version**      | v0.4.8d                                  |
| **Title**        | Embedding Cache                          |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Modules.RAG`                  |
| **License Tier** | Teams                                    |

---

## 1. Overview

### 1.1 Purpose

This specification defines `IEmbeddingCache` and `SqliteEmbeddingCache`, a local cache for embeddings that reduces API calls, lowers costs, and improves latency for repeated queries. Embeddings are keyed by content hash and evicted using an LRU policy.

### 1.2 Goals

- Define `IEmbeddingCache` interface for embedding storage
- Implement SQLite-based cache with content hash keys
- Implement LRU eviction when size limit reached
- Add configuration options for enable/disable and max size
- Integrate with `IEmbeddingService` transparently
- Provide cache statistics for monitoring

### 1.3 Non-Goals

- Distributed caching (future)
- Cache warming strategies (future)
- Embedding versioning (model changes invalidate all)

---

## 2. Design

### 2.1 IEmbeddingCache Interface

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Cache for storing and retrieving embeddings.
/// </summary>
public interface IEmbeddingCache
{
    /// <summary>
    /// Tries to get a cached embedding by content hash.
    /// </summary>
    /// <param name="contentHash">SHA-256 hash of the content.</param>
    /// <param name="embedding">The cached embedding if found.</param>
    /// <returns>True if found in cache, false otherwise.</returns>
    bool TryGet(string contentHash, out float[]? embedding);

    /// <summary>
    /// Stores an embedding in the cache.
    /// </summary>
    /// <param name="contentHash">SHA-256 hash of the content.</param>
    /// <param name="embedding">The embedding to cache.</param>
    void Set(string contentHash, float[] embedding);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    EmbeddingCacheStatistics GetStatistics();

    /// <summary>
    /// Clears all cached embeddings.
    /// </summary>
    void Clear();

    /// <summary>
    /// Forces eviction to meet size constraints.
    /// </summary>
    void Compact();
}
```

### 2.2 EmbeddingCacheStatistics Record

```csharp
namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Statistics about the embedding cache.
/// </summary>
public record EmbeddingCacheStatistics
{
    /// <summary>
    /// Total number of cached embeddings.
    /// </summary>
    public int EntryCount { get; init; }

    /// <summary>
    /// Estimated size of cache in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Number of cache hits since startup.
    /// </summary>
    public long HitCount { get; init; }

    /// <summary>
    /// Number of cache misses since startup.
    /// </summary>
    public long MissCount { get; init; }

    /// <summary>
    /// Cache hit rate (0.0 - 1.0).
    /// </summary>
    public double HitRate => HitCount + MissCount > 0
        ? (double)HitCount / (HitCount + MissCount)
        : 0;

    /// <summary>
    /// Number of evictions since startup.
    /// </summary>
    public long EvictionCount { get; init; }

    /// <summary>
    /// Human-readable size string.
    /// </summary>
    public string SizeDisplay => FormatBytes(SizeBytes);

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int i = 0;
        double size = bytes;
        while (size >= 1024 && i < suffixes.Length - 1)
        {
            size /= 1024;
            i++;
        }
        return $"{size:F1} {suffixes[i]}";
    }
}
```

### 2.3 EmbeddingCacheOptions

```csharp
namespace Lexichord.Modules.RAG.Configuration;

/// <summary>
/// Configuration options for the embedding cache.
/// </summary>
public class EmbeddingCacheOptions
{
    /// <summary>
    /// Whether the cache is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum cache size in megabytes.
    /// </summary>
    public int MaxSizeMB { get; set; } = 100;

    /// <summary>
    /// Path to the cache database file.
    /// </summary>
    public string CachePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Lexichord", "cache", "embeddings.db");

    /// <summary>
    /// Embedding dimensions (must match model).
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 1536;

    /// <summary>
    /// How often to run compaction (in number of writes).
    /// </summary>
    public int CompactionInterval { get; set; } = 100;
}
```

### 2.4 SqliteEmbeddingCache Implementation

```csharp
namespace Lexichord.Modules.RAG.Services;

using Microsoft.Data.Sqlite;

/// <summary>
/// SQLite-based embedding cache with LRU eviction.
/// </summary>
public sealed class SqliteEmbeddingCache : IEmbeddingCache, IDisposable
{
    private readonly EmbeddingCacheOptions _options;
    private readonly SqliteConnection _connection;
    private readonly ILogger<SqliteEmbeddingCache> _logger;
    private readonly object _lock = new();

    private long _hitCount;
    private long _missCount;
    private long _evictionCount;
    private int _writesSinceCompaction;

    // Estimated bytes per embedding entry (hash + embedding + metadata)
    private const int BytesPerEntry = 64 + (1536 * 4) + 100; // ~6.3KB

    public SqliteEmbeddingCache(
        IOptions<EmbeddingCacheOptions> options,
        ILogger<SqliteEmbeddingCache> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Ensure directory exists
        var cacheDir = Path.GetDirectoryName(_options.CachePath)!;
        Directory.CreateDirectory(cacheDir);

        // Open SQLite connection
        _connection = new SqliteConnection($"Data Source={_options.CachePath}");
        _connection.Open();

        // Initialize schema
        InitializeSchema();

        _logger.LogInformation(
            "Embedding cache initialized: {Path}, MaxSize={MaxSizeMB}MB",
            _options.CachePath, _options.MaxSizeMB);
    }

    private void InitializeSchema()
    {
        const string sql = @"
            CREATE TABLE IF NOT EXISTS embeddings (
                content_hash TEXT PRIMARY KEY,
                embedding BLOB NOT NULL,
                dimensions INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                last_accessed_at TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_embeddings_last_accessed
            ON embeddings(last_accessed_at ASC);
        ";

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public bool TryGet(string contentHash, out float[]? embedding)
    {
        if (!_options.Enabled)
        {
            embedding = null;
            return false;
        }

        lock (_lock)
        {
            try
            {
                const string sql = @"
                    SELECT embedding, dimensions FROM embeddings
                    WHERE content_hash = @hash";

                using var cmd = _connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@hash", contentHash);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var blob = (byte[])reader["embedding"];
                    var dimensions = reader.GetInt32(1);

                    embedding = DeserializeEmbedding(blob, dimensions);

                    // Update last accessed time
                    UpdateLastAccessed(contentHash);

                    Interlocked.Increment(ref _hitCount);
                    _logger.LogDebug("Cache hit for hash: {Hash}", contentHash[..8]);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache read error for hash: {Hash}", contentHash[..8]);
            }
        }

        Interlocked.Increment(ref _missCount);
        _logger.LogDebug("Cache miss for hash: {Hash}", contentHash[..8]);
        embedding = null;
        return false;
    }

    public void Set(string contentHash, float[] embedding)
    {
        if (!_options.Enabled)
            return;

        lock (_lock)
        {
            try
            {
                var blob = SerializeEmbedding(embedding);
                var now = DateTimeOffset.UtcNow.ToString("O");

                const string sql = @"
                    INSERT OR REPLACE INTO embeddings
                    (content_hash, embedding, dimensions, created_at, last_accessed_at)
                    VALUES (@hash, @embedding, @dimensions, @now, @now)";

                using var cmd = _connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@hash", contentHash);
                cmd.Parameters.AddWithValue("@embedding", blob);
                cmd.Parameters.AddWithValue("@dimensions", embedding.Length);
                cmd.Parameters.AddWithValue("@now", now);
                cmd.ExecuteNonQuery();

                _writesSinceCompaction++;

                // Periodic compaction
                if (_writesSinceCompaction >= _options.CompactionInterval)
                {
                    Compact();
                    _writesSinceCompaction = 0;
                }

                _logger.LogDebug("Cached embedding for hash: {Hash}", contentHash[..8]);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache write error for hash: {Hash}", contentHash[..8]);
            }
        }
    }

    public EmbeddingCacheStatistics GetStatistics()
    {
        lock (_lock)
        {
            var entryCount = GetEntryCount();
            var sizeBytes = (long)entryCount * BytesPerEntry;

            return new EmbeddingCacheStatistics
            {
                EntryCount = entryCount,
                SizeBytes = sizeBytes,
                HitCount = _hitCount,
                MissCount = _missCount,
                EvictionCount = _evictionCount
            };
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM embeddings";
            cmd.ExecuteNonQuery();

            _logger.LogInformation("Cache cleared");
        }
    }

    public void Compact()
    {
        lock (_lock)
        {
            var currentCount = GetEntryCount();
            var maxEntries = (_options.MaxSizeMB * 1024 * 1024) / BytesPerEntry;

            if (currentCount <= maxEntries)
                return;

            var entriesToRemove = currentCount - maxEntries;

            // Delete oldest entries (LRU)
            const string sql = @"
                DELETE FROM embeddings
                WHERE content_hash IN (
                    SELECT content_hash FROM embeddings
                    ORDER BY last_accessed_at ASC
                    LIMIT @limit
                )";

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@limit", entriesToRemove);
            var removed = cmd.ExecuteNonQuery();

            Interlocked.Add(ref _evictionCount, removed);

            _logger.LogInformation("Cache eviction: removed {Count} entries", removed);

            // SQLite vacuum
            using var vacuumCmd = _connection.CreateCommand();
            vacuumCmd.CommandText = "VACUUM";
            vacuumCmd.ExecuteNonQuery();
        }
    }

    private void UpdateLastAccessed(string contentHash)
    {
        const string sql = @"
            UPDATE embeddings
            SET last_accessed_at = @now
            WHERE content_hash = @hash";

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@hash", contentHash);
        cmd.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private int GetEntryCount()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM embeddings";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static byte[] SerializeEmbedding(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] DeserializeEmbedding(byte[] bytes, int dimensions)
    {
        var embedding = new float[dimensions];
        Buffer.BlockCopy(bytes, 0, embedding, 0, bytes.Length);
        return embedding;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
```

### 2.5 CachedEmbeddingService Decorator

```csharp
namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Decorator that adds caching to an embedding service.
/// </summary>
public sealed class CachedEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingService _inner;
    private readonly IEmbeddingCache _cache;
    private readonly ILogger<CachedEmbeddingService> _logger;

    public string ModelName => _inner.ModelName;
    public int Dimensions => _inner.Dimensions;

    public CachedEmbeddingService(
        IEmbeddingService inner,
        IEmbeddingCache cache,
        ILogger<CachedEmbeddingService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var hash = ComputeHash(text);

        // Try cache first
        if (_cache.TryGet(hash, out var cached))
        {
            return cached!;
        }

        // Call API
        var embedding = await _inner.EmbedAsync(text, ct);

        // Cache result
        _cache.Set(hash, embedding);

        return embedding;
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        var results = new float[texts.Count][];
        var uncachedIndices = new List<int>();
        var uncachedTexts = new List<string>();

        // Check cache for each text
        for (int i = 0; i < texts.Count; i++)
        {
            var hash = ComputeHash(texts[i]);
            if (_cache.TryGet(hash, out var cached))
            {
                results[i] = cached!;
            }
            else
            {
                uncachedIndices.Add(i);
                uncachedTexts.Add(texts[i]);
            }
        }

        // Fetch uncached embeddings
        if (uncachedTexts.Count > 0)
        {
            _logger.LogDebug(
                "Batch: {Cached} cached, {Uncached} to fetch",
                texts.Count - uncachedTexts.Count, uncachedTexts.Count);

            var fetched = await _inner.EmbedBatchAsync(uncachedTexts, ct);

            for (int i = 0; i < fetched.Count; i++)
            {
                var originalIndex = uncachedIndices[i];
                results[originalIndex] = fetched[i];

                // Cache each result
                var hash = ComputeHash(uncachedTexts[i]);
                _cache.Set(hash, fetched[i]);
            }
        }

        return results;
    }

    private static string ComputeHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
```

### 2.6 DI Registration

```csharp
// In RAGModule.cs
public static IServiceCollection AddEmbeddingCache(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.Configure<EmbeddingCacheOptions>(
        configuration.GetSection("EmbeddingCache"));

    services.AddSingleton<IEmbeddingCache, SqliteEmbeddingCache>();

    // Decorate embedding service with caching
    services.Decorate<IEmbeddingService, CachedEmbeddingService>();

    return services;
}
```

---

## 3. Configuration

### 3.1 appsettings.json

```json
{
  "EmbeddingCache": {
    "Enabled": true,
    "MaxSizeMB": 100,
    "CachePath": "~/.lexichord/cache/embeddings.db",
    "EmbeddingDimensions": 1536,
    "CompactionInterval": 100
  }
}
```

### 3.2 Settings UI Integration

```csharp
// Settings panel for cache configuration
public class CacheSettingsViewModel : ViewModelBase
{
    private readonly IEmbeddingCache _cache;

    public bool CacheEnabled { get; set; }
    public int MaxSizeMB { get; set; }

    public EmbeddingCacheStatistics? Statistics { get; private set; }

    public string HitRateDisplay => Statistics != null
        ? $"{Statistics.HitRate:P1}"
        : "N/A";

    [RelayCommand]
    private void RefreshStatistics()
    {
        Statistics = _cache.GetStatistics();
        OnPropertyChanged(nameof(Statistics));
        OnPropertyChanged(nameof(HitRateDisplay));
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        if (await _dialogService.ConfirmAsync("Clear Cache", "Delete all cached embeddings?"))
        {
            _cache.Clear();
            RefreshStatistics();
        }
    }
}
```

---

## 4. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.8d")]
public class SqliteEmbeddingCacheTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly SqliteEmbeddingCache _sut;

    public SqliteEmbeddingCacheTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_cache_{Guid.NewGuid()}.db");

        var options = Options.Create(new EmbeddingCacheOptions
        {
            Enabled = true,
            MaxSizeMB = 10,
            CachePath = _tempDbPath,
            CompactionInterval = 5
        });

        _sut = new SqliteEmbeddingCache(options, NullLogger<SqliteEmbeddingCache>.Instance);
    }

    public void Dispose()
    {
        _sut.Dispose();
        if (File.Exists(_tempDbPath))
            File.Delete(_tempDbPath);
    }

    [Fact]
    public void TryGet_NotCached_ReturnsFalse()
    {
        var result = _sut.TryGet("nonexistent_hash", out var embedding);

        result.Should().BeFalse();
        embedding.Should().BeNull();
    }

    [Fact]
    public void Set_ThenTryGet_ReturnsEmbedding()
    {
        var hash = "test_hash_123";
        var embedding = CreateEmbedding();

        _sut.Set(hash, embedding);
        var result = _sut.TryGet(hash, out var retrieved);

        result.Should().BeTrue();
        retrieved.Should().BeEquivalentTo(embedding);
    }

    [Fact]
    public void GetStatistics_ReturnsCorrectCounts()
    {
        _sut.Set("hash1", CreateEmbedding());
        _sut.Set("hash2", CreateEmbedding());

        _sut.TryGet("hash1", out _); // Hit
        _sut.TryGet("missing", out _); // Miss

        var stats = _sut.GetStatistics();

        stats.EntryCount.Should().Be(2);
        stats.HitCount.Should().Be(1);
        stats.MissCount.Should().Be(1);
        stats.HitRate.Should().Be(0.5);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        _sut.Set("hash1", CreateEmbedding());
        _sut.Set("hash2", CreateEmbedding());

        _sut.Clear();

        _sut.GetStatistics().EntryCount.Should().Be(0);
    }

    [Fact]
    public void Compact_EvictsOldestEntries()
    {
        // Fill cache beyond limit (using small max size for test)
        for (int i = 0; i < 100; i++)
        {
            _sut.Set($"hash_{i}", CreateEmbedding());
        }

        // Force compaction
        _sut.Compact();

        var stats = _sut.GetStatistics();

        // Should be within size limit
        stats.SizeBytes.Should().BeLessThanOrEqualTo(10 * 1024 * 1024);
        stats.EvictionCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TryGet_UpdatesLastAccessedTime()
    {
        var hash = "test_hash";
        _sut.Set(hash, CreateEmbedding());

        // Access multiple times
        for (int i = 0; i < 5; i++)
        {
            _sut.TryGet(hash, out _);
        }

        // Entry should be "fresh" for LRU purposes
        // (Verified by not being evicted during compaction)
    }

    [Fact]
    public void CacheDisabled_TryGetReturnsFalse()
    {
        var options = Options.Create(new EmbeddingCacheOptions { Enabled = false });
        using var cache = new SqliteEmbeddingCache(options, NullLogger<SqliteEmbeddingCache>.Instance);

        cache.Set("hash", CreateEmbedding());
        var result = cache.TryGet("hash", out _);

        result.Should().BeFalse();
    }

    private static float[] CreateEmbedding()
    {
        return Enumerable.Range(0, 1536).Select(i => (float)i / 1536).ToArray();
    }
}

[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.8d")]
public class CachedEmbeddingServiceTests
{
    [Fact]
    public async Task EmbedAsync_CacheHit_SkipsApiCall()
    {
        var innerMock = new Mock<IEmbeddingService>();
        var cacheMock = new Mock<IEmbeddingCache>();
        var cachedEmbedding = new float[1536];

        cacheMock.Setup(c => c.TryGet(It.IsAny<string>(), out cachedEmbedding))
            .Returns(true);

        var sut = new CachedEmbeddingService(
            innerMock.Object,
            cacheMock.Object,
            NullLogger<CachedEmbeddingService>.Instance);

        var result = await sut.EmbedAsync("test text");

        innerMock.Verify(i => i.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        result.Should().BeSameAs(cachedEmbedding);
    }

    [Fact]
    public async Task EmbedAsync_CacheMiss_CallsApiAndCaches()
    {
        var innerMock = new Mock<IEmbeddingService>();
        var cacheMock = new Mock<IEmbeddingCache>();
        var apiEmbedding = new float[1536];

        float[]? outValue = null;
        cacheMock.Setup(c => c.TryGet(It.IsAny<string>(), out outValue))
            .Returns(false);

        innerMock.Setup(i => i.EmbedAsync("test text", It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiEmbedding);

        var sut = new CachedEmbeddingService(
            innerMock.Object,
            cacheMock.Object,
            NullLogger<CachedEmbeddingService>.Instance);

        var result = await sut.EmbedAsync("test text");

        innerMock.Verify(i => i.EmbedAsync("test text", It.IsAny<CancellationToken>()), Times.Once);
        cacheMock.Verify(c => c.Set(It.IsAny<string>(), apiEmbedding), Times.Once);
    }
}
```

---

## 5. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Information | "Embedding cache initialized: {Path}, MaxSize={MaxSizeMB}MB" | Startup |
| Information | "Cache eviction: removed {Count} entries" | After compaction |
| Information | "Cache cleared" | After clear |
| Debug | "Cache hit for hash: {Hash}" | On hit |
| Debug | "Cache miss for hash: {Hash}" | On miss |
| Debug | "Cached embedding for hash: {Hash}" | After set |
| Debug | "Batch: {Cached} cached, {Uncached} to fetch" | Batch operation |
| Warning | "Cache read/write error for hash: {Hash}" | On exception |

---

## 6. File Locations

| File | Path |
| :--- | :--- |
| IEmbeddingCache | `src/Lexichord.Abstractions/Contracts/IEmbeddingCache.cs` |
| EmbeddingCacheOptions | `src/Lexichord.Modules.RAG/Configuration/EmbeddingCacheOptions.cs` |
| EmbeddingCacheStatistics | `src/Lexichord.Modules.RAG/Models/EmbeddingCacheStatistics.cs` |
| SqliteEmbeddingCache | `src/Lexichord.Modules.RAG/Services/SqliteEmbeddingCache.cs` |
| CachedEmbeddingService | `src/Lexichord.Modules.RAG/Services/CachedEmbeddingService.cs` |
| Unit tests | `tests/Lexichord.Modules.RAG.Tests/Services/EmbeddingCacheTests.cs` |

---

## 7. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | Cache stores embeddings by content hash | [ ] |
| 2 | Cache hit returns stored embedding | [ ] |
| 3 | Cache miss calls API and stores result | [ ] |
| 4 | LRU eviction when size limit reached | [ ] |
| 5 | Cache can be disabled via configuration | [ ] |
| 6 | Statistics track hits, misses, evictions | [ ] |
| 7 | Batch operations use cache efficiently | [ ] |
| 8 | Clear removes all entries | [ ] |
| 9 | All unit tests pass | [ ] |

---

## 8. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
