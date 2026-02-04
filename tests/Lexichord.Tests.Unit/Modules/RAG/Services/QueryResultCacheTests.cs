// =============================================================================
// File: QueryResultCacheTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for QueryResultCache.
// =============================================================================
// VERSION: v0.5.8c (Multi-Layer Caching System)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Configuration;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="QueryResultCache"/>.
/// </summary>
public sealed class QueryResultCacheTests : IDisposable
{
    private readonly Mock<ILogger<QueryResultCache>> _loggerMock;
    private readonly QueryCacheOptions _options;
    private readonly QueryResultCache _sut;

    public QueryResultCacheTests()
    {
        _loggerMock = new Mock<ILogger<QueryResultCache>>();
        _options = new QueryCacheOptions
        {
            MaxEntries = 5,
            TtlSeconds = 300,
            Enabled = true
        };
        _sut = new QueryResultCache(
            Options.Create(_options),
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    private static SearchResult CreateTestResult(int hitCount = 1, Guid? documentId = null)
    {
        var docId = documentId ?? Guid.NewGuid();
        var hits = Enumerable.Range(0, hitCount)
            .Select(i => CreateTestHit(docId, i))
            .ToList();

        return new SearchResult
        {
            Hits = hits,
            Query = "test query",
            WasTruncated = false,
            Duration = TimeSpan.FromMilliseconds(10)
        };
    }

    private static SearchHit CreateTestHit(Guid docId, int index)
    {
        return new SearchHit
        {
            Chunk = new TextChunk(
                Content: $"Test content {index}",
                StartOffset: index * 100,
                EndOffset: (index + 1) * 100,
                Metadata: new ChunkMetadata(Heading: null, Index: index, Level: 0)),
            Document = new Document(
                Id: docId,
                ProjectId: Guid.NewGuid(),
                FilePath: $"/test/doc{index}.md",
                Title: $"Document {index}",
                Hash: "abc123",
                Status: DocumentStatus.Indexed,
                IndexedAt: DateTime.UtcNow,
                FailureReason: null),
            Score = 0.9f - (index * 0.01f)
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new QueryResultCache(null!, _loggerMock.Object));

        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new QueryResultCache(Options.Create(_options), null!));

        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region TryGet/Set Tests

    [Fact]
    public void TryGet_WhenCacheMiss_ReturnsFalse()
    {
        var found = _sut.TryGet("nonexistent", out var result);

        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void TryGet_AfterSet_ReturnsTrue()
    {
        var testResult = CreateTestResult();
        var docIds = testResult.Hits.Select(h => h.Document.Id).Distinct().ToList();

        _sut.Set("key1", testResult, docIds);
        var found = _sut.TryGet("key1", out var result);

        Assert.True(found);
        Assert.NotNull(result);
        Assert.Equal("test query", result.Query);
    }

    [Fact]
    public void Set_ReplacesExistingEntry()
    {
        var result1 = CreateTestResult(hitCount: 1);
        var result2 = CreateTestResult(hitCount: 2);
        var docIds1 = new List<Guid> { Guid.NewGuid() };
        var docIds2 = new List<Guid> { Guid.NewGuid() };

        _sut.Set("key1", result1, docIds1);
        _sut.Set("key1", result2, docIds2);

        _sut.TryGet("key1", out var retrieved);

        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved.Hits.Count);
    }

    [Fact]
    public void TryGet_WithNullOrWhiteSpaceKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.TryGet(null!, out _));
        Assert.Throws<ArgumentException>(() => _sut.TryGet("", out _));
        Assert.Throws<ArgumentException>(() => _sut.TryGet("   ", out _));
    }

    #endregion

    #region LRU Eviction Tests

    [Fact]
    public void Set_WhenAtCapacity_EvictsLruEntry()
    {
        // Fill cache to capacity (5 entries)
        for (int i = 0; i < 5; i++)
        {
            var result = CreateTestResult();
            _sut.Set($"key{i}", result, new List<Guid> { Guid.NewGuid() });
        }

        // Access key0 to make it recently used
        _sut.TryGet("key0", out _);

        // Add another entry - should evict key1 (oldest not accessed)
        var newResult = CreateTestResult();
        _sut.Set("key5", newResult, new List<Guid> { Guid.NewGuid() });

        // key0 should still exist (was accessed)
        Assert.True(_sut.TryGet("key0", out _));
        // key5 should exist (just added)
        Assert.True(_sut.TryGet("key5", out _));
        // One of the old keys should have been evicted
        var stats = _sut.GetStatistics();
        Assert.Equal(5, stats.EntryCount);
        Assert.True(stats.EvictionCount >= 1);
    }

    [Fact]
    public void GetStatistics_ReportsEvictionCount()
    {
        // Fill cache beyond capacity
        for (int i = 0; i < 7; i++)
        {
            var result = CreateTestResult();
            _sut.Set($"key{i}", result, new List<Guid> { Guid.NewGuid() });
        }

        var stats = _sut.GetStatistics();

        Assert.Equal(5, stats.EntryCount);
        Assert.Equal(2, stats.EvictionCount);
    }

    #endregion

    #region TTL Expiration Tests

    [Fact]
    public void TryGet_DoesNotReturnExpiredEntries()
    {
        // Create cache with very short TTL
        var shortTtlOptions = new QueryCacheOptions { TtlSeconds = 0, Enabled = true };
        using var shortTtlCache = new QueryResultCache(
            Options.Create(shortTtlOptions),
            _loggerMock.Object);

        var result = CreateTestResult();
        shortTtlCache.Set("key1", result, new List<Guid> { Guid.NewGuid() });

        // Wait for expiration (TTL is 0 seconds, so immediate)
        Thread.Sleep(10);

        var found = shortTtlCache.TryGet("key1", out var retrieved);

        Assert.False(found);
        Assert.Null(retrieved);
    }

    #endregion

    #region Invalidation Tests

    [Fact]
    public void InvalidateForDocument_RemovesMatchingEntries()
    {
        var docId = Guid.NewGuid();
        var result1 = CreateTestResult(documentId: docId);
        var result2 = CreateTestResult();

        _sut.Set("key1", result1, new List<Guid> { docId });
        _sut.Set("key2", result2, new List<Guid> { Guid.NewGuid() });

        _sut.InvalidateForDocument(docId);

        Assert.False(_sut.TryGet("key1", out _));
        Assert.True(_sut.TryGet("key2", out _));
    }

    [Fact]
    public void InvalidateForDocument_UpdatesInvalidationCount()
    {
        var docId = Guid.NewGuid();
        var result = CreateTestResult(documentId: docId);

        _sut.Set("key1", result, new List<Guid> { docId });
        _sut.InvalidateForDocument(docId);

        var stats = _sut.GetStatistics();
        Assert.Equal(1, stats.InvalidationCount);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        for (int i = 0; i < 3; i++)
        {
            var result = CreateTestResult();
            _sut.Set($"key{i}", result, new List<Guid> { Guid.NewGuid() });
        }

        _sut.Clear();

        var stats = _sut.GetStatistics();
        Assert.Equal(0, stats.EntryCount);
        Assert.False(_sut.TryGet("key0", out _));
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public void GetStatistics_TracksHitsAndMisses()
    {
        var result = CreateTestResult();
        _sut.Set("key1", result, new List<Guid> { Guid.NewGuid() });

        _sut.TryGet("key1", out _); // Hit
        _sut.TryGet("key2", out _); // Miss
        _sut.TryGet("key1", out _); // Hit

        var stats = _sut.GetStatistics();

        Assert.Equal(2, stats.HitCount);
        Assert.Equal(1, stats.MissCount);
        Assert.True(stats.HitRate > 0.6);
    }

    [Fact]
    public void GetStatistics_ReportsCorrectEntryCount()
    {
        for (int i = 0; i < 3; i++)
        {
            var result = CreateTestResult();
            _sut.Set($"key{i}", result, new List<Guid> { Guid.NewGuid() });
        }

        var stats = _sut.GetStatistics();

        Assert.Equal(3, stats.EntryCount);
        Assert.Equal(5, stats.MaxEntries);
    }

    #endregion

    #region Disabled Cache Tests

    [Fact]
    public void TryGet_WhenDisabled_ReturnsFalse()
    {
        var disabledOptions = new QueryCacheOptions { Enabled = false };
        using var disabledCache = new QueryResultCache(
            Options.Create(disabledOptions),
            _loggerMock.Object);

        var result = CreateTestResult();
        disabledCache.Set("key1", result, new List<Guid> { Guid.NewGuid() });

        var found = disabledCache.TryGet("key1", out _);

        Assert.False(found);
    }

    [Fact]
    public void Set_WhenDisabled_DoesNotStore()
    {
        var disabledOptions = new QueryCacheOptions { Enabled = false };
        using var disabledCache = new QueryResultCache(
            Options.Create(disabledOptions),
            _loggerMock.Object);

        var result = CreateTestResult();
        disabledCache.Set("key1", result, new List<Guid> { Guid.NewGuid() });

        var stats = disabledCache.GetStatistics();
        Assert.Equal(0, stats.EntryCount);
    }

    #endregion
}
