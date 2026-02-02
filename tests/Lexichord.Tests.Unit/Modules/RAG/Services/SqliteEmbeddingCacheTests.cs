// =============================================================================
// File: SqliteEmbeddingCacheTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SqliteEmbeddingCache.
// Version: v0.4.8d
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Configuration;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="SqliteEmbeddingCache"/>.
/// </summary>
public class SqliteEmbeddingCacheTests : IDisposable
{
    private readonly Mock<ILogger<SqliteEmbeddingCache>> _loggerMock;
    private readonly string _tempDbPath;
    private readonly EmbeddingCacheOptions _options;

    public SqliteEmbeddingCacheTests()
    {
        _loggerMock = new Mock<ILogger<SqliteEmbeddingCache>>();
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_cache_{Guid.NewGuid()}.db");
        _options = new EmbeddingCacheOptions
        {
            Enabled = true,
            MaxSizeMB = 10,
            CachePath = _tempDbPath,
            EmbeddingDimensions = 1536
        };
    }

    public void Dispose()
    {
        // Cleanup temp database file
        if (File.Exists(_tempDbPath))
        {
            try
            {
                File.Delete(_tempDbPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        GC.SuppressFinalize(this);
    }

    private SqliteEmbeddingCache CreateSut() =>
        new(Options.Create(_options), _loggerMock.Object);

    private static float[] CreateEmbedding(int dimensions = 1536, float seed = 1.0f)
    {
        var embedding = new float[dimensions];
        for (var i = 0; i < dimensions; i++)
        {
            embedding[i] = seed + (i * 0.001f);
        }
        return embedding;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SqliteEmbeddingCache(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SqliteEmbeddingCache(Options.Create(_options), null!));
    }

    [Fact]
    public void Constructor_CreatesDatabase()
    {
        // Arrange & Act
        using var sut = CreateSut();

        // Assert
        File.Exists(_tempDbPath).Should().BeTrue();
    }

    #endregion

    #region TryGet Tests

    [Fact]
    public void TryGet_WhenNotCached_ReturnsFalse()
    {
        // Arrange
        using var sut = CreateSut();
        var contentHash = "abc123def456";

        // Act
        var result = sut.TryGet(contentHash, out var embedding);

        // Assert
        result.Should().BeFalse();
        embedding.Should().BeNull();
    }

    [Fact]
    public void TryGet_AfterSet_ReturnsTrue()
    {
        // Arrange
        using var sut = CreateSut();
        var contentHash = "abc123def456";
        var expectedEmbedding = CreateEmbedding();
        sut.Set(contentHash, expectedEmbedding);

        // Act
        var result = sut.TryGet(contentHash, out var embedding);

        // Assert
        result.Should().BeTrue();
        embedding.Should().BeEquivalentTo(expectedEmbedding);
    }

    [Fact]
    public void TryGet_WhenDisabled_ReturnsFalse()
    {
        // Arrange
        _options.Enabled = false;
        using var sut = CreateSut();
        var contentHash = "abc123def456";
        var testEmbedding = CreateEmbedding();
        
        // Even though we call Set, it shouldn't cache when disabled
        sut.Set(contentHash, testEmbedding);

        // Act
        var result = sut.TryGet(contentHash, out var embedding);

        // Assert
        result.Should().BeFalse();
        embedding.Should().BeNull();
    }

    #endregion

    #region Set Tests

    [Fact]
    public void Set_WithValidData_CachesEmbedding()
    {
        // Arrange
        using var sut = CreateSut();
        var contentHash = "test_hash_123";
        var embedding = CreateEmbedding();

        // Act
        sut.Set(contentHash, embedding);

        // Assert
        sut.TryGet(contentHash, out var retrieved).Should().BeTrue();
        retrieved.Should().BeEquivalentTo(embedding);
    }

    [Fact]
    public void Set_WithExistingHash_UpdatesEmbedding()
    {
        // Arrange
        using var sut = CreateSut();
        var contentHash = "test_hash_123";
        var originalEmbedding = CreateEmbedding(seed: 1.0f);
        var updatedEmbedding = CreateEmbedding(seed: 2.0f);

        sut.Set(contentHash, originalEmbedding);

        // Act
        sut.Set(contentHash, updatedEmbedding);

        // Assert
        sut.TryGet(contentHash, out var retrieved).Should().BeTrue();
        retrieved.Should().BeEquivalentTo(updatedEmbedding);
    }

    #endregion

    #region GetStatistics Tests

    [Fact]
    public void GetStatistics_WhenEmpty_ReturnsZeroCounts()
    {
        // Arrange
        using var sut = CreateSut();

        // Act
        var stats = sut.GetStatistics();

        // Assert
        stats.EntryCount.Should().Be(0);
        stats.SizeBytes.Should().Be(0);
        stats.HitCount.Should().Be(0);
        stats.MissCount.Should().Be(0);
        stats.EvictionCount.Should().Be(0);
    }

    [Fact]
    public void GetStatistics_AfterCacheHit_IncreasesHitCount()
    {
        // Arrange
        using var sut = CreateSut();
        var contentHash = "test_hash";
        sut.Set(contentHash, CreateEmbedding());

        // Act
        sut.TryGet(contentHash, out _);
        sut.TryGet(contentHash, out _);
        var stats = sut.GetStatistics();

        // Assert
        stats.HitCount.Should().Be(2);
        stats.HitRate.Should().Be(1.0);
    }

    [Fact]
    public void GetStatistics_AfterCacheMiss_IncreasesMissCount()
    {
        // Arrange
        using var sut = CreateSut();

        // Act
        sut.TryGet("nonexistent1", out _);
        sut.TryGet("nonexistent2", out _);
        var stats = sut.GetStatistics();

        // Assert
        stats.MissCount.Should().Be(2);
        stats.HitRate.Should().Be(0.0);
    }

    [Fact]
    public void GetStatistics_AfterSet_ReflectsEntryCount()
    {
        // Arrange
        using var sut = CreateSut();

        // Act
        sut.Set("hash1", CreateEmbedding());
        sut.Set("hash2", CreateEmbedding());
        sut.Set("hash3", CreateEmbedding());
        var stats = sut.GetStatistics();

        // Assert
        stats.EntryCount.Should().Be(3);
        stats.SizeBytes.Should().BeGreaterThan(0);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        // Arrange
        using var sut = CreateSut();
        sut.Set("hash1", CreateEmbedding());
        sut.Set("hash2", CreateEmbedding());
        sut.Set("hash3", CreateEmbedding());

        // Act
        sut.Clear();

        // Assert
        var stats = sut.GetStatistics();
        stats.EntryCount.Should().Be(0);
        stats.SizeBytes.Should().Be(0);
    }

    [Fact]
    public void Clear_ResetsStatistics()
    {
        // Arrange
        using var sut = CreateSut();
        sut.Set("hash1", CreateEmbedding());
        sut.TryGet("hash1", out _); // Hit
        sut.TryGet("nonexistent", out _); // Miss

        // Act
        sut.Clear();
        var stats = sut.GetStatistics();

        // Assert
        stats.HitCount.Should().Be(0);
        stats.MissCount.Should().Be(0);
    }

    #endregion

    #region Compact Tests

    [Fact]
    public void Compact_DoesNotThrow()
    {
        // Arrange
        using var sut = CreateSut();
        sut.Set("hash1", CreateEmbedding());

        // Act & Assert
        var act = () => sut.Compact();
        act.Should().NotThrow();
    }

    #endregion

    #region ComputeContentHash Tests

    [Fact]
    public void ComputeContentHash_ReturnsConsistentHash()
    {
        // Arrange
        var content = "Hello, World!";

        // Act
        var hash1 = SqliteEmbeddingCache.ComputeContentHash(content);
        var hash2 = SqliteEmbeddingCache.ComputeContentHash(content);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeContentHash_ReturnsDifferentHashForDifferentContent()
    {
        // Arrange
        var content1 = "Hello, World!";
        var content2 = "Hello, World?";

        // Act
        var hash1 = SqliteEmbeddingCache.ComputeContentHash(content1);
        var hash2 = SqliteEmbeddingCache.ComputeContentHash(content2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeContentHash_ReturnsValidSHA256Format()
    {
        // Arrange
        var content = "Test content";

        // Act
        var hash = SqliteEmbeddingCache.ComputeContentHash(content);

        // Assert
        hash.Should().HaveLength(64); // SHA-256 produces 32 bytes = 64 hex chars
        hash.Should().MatchRegex("^[a-f0-9]+$"); // lowercase hex
    }

    #endregion

    #region EmbeddingCacheStatistics Tests

    [Fact]
    public void EmbeddingCacheStatistics_FormatSize_FormatsCorrectly()
    {
        // Arrange & Act & Assert
        new EmbeddingCacheStatistics(0, 500, 0, 0, 0, 0).FormatSize().Should().Be("500 B");
        new EmbeddingCacheStatistics(0, 2048, 0, 0, 0, 0).FormatSize().Should().Be("2.00 KB");
        new EmbeddingCacheStatistics(0, 5 * 1024 * 1024, 0, 0, 0, 0).FormatSize().Should().Be("5.00 MB");
        new EmbeddingCacheStatistics(0, 2L * 1024 * 1024 * 1024, 0, 0, 0, 0).FormatSize().Should().Be("2.00 GB");
    }

    #endregion
}
