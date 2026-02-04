// =============================================================================
// File: ResilientSearchServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ResilientSearchService.
// =============================================================================
// VERSION: v0.5.8d (Error Resilience)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Configuration;
using Lexichord.Modules.RAG.Resilience;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Resilience;

/// <summary>
/// Unit tests for <see cref="ResilientSearchService"/>.
/// </summary>
public sealed class ResilientSearchServiceTests
{
    private readonly Mock<IHybridSearchService> _hybridSearchMock;
    private readonly Mock<IBM25SearchService> _bm25SearchMock;
    private readonly Mock<IQueryResultCache> _queryCacheMock;
    private readonly Mock<ILogger<ResilientSearchService>> _loggerMock;
    private readonly ResilienceOptions _options;
    private readonly ResilientSearchService _sut;

    public ResilientSearchServiceTests()
    {
        _hybridSearchMock = new Mock<IHybridSearchService>();
        _bm25SearchMock = new Mock<IBM25SearchService>();
        _queryCacheMock = new Mock<IQueryResultCache>();
        _loggerMock = new Mock<ILogger<ResilientSearchService>>();

        _options = new ResilienceOptions
        {
            RetryMaxAttempts = 1, // Minimize retries for faster tests
            RetryInitialDelay = TimeSpan.FromMilliseconds(10),
            TimeoutPerOperation = TimeSpan.FromSeconds(30),
            CircuitBreakerFailureThreshold = 2,
            CircuitBreakerMinimumThroughput = 2,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(1),
            Enabled = true
        };

        // Create real CacheKeyGenerator (it's stateless)
        var cacheKeyGeneratorLogger = new Mock<ILogger<CacheKeyGenerator>>();
        var realCacheKeyGenerator = new CacheKeyGenerator(cacheKeyGeneratorLogger.Object);

        _sut = new ResilientSearchService(
            _hybridSearchMock.Object,
            _bm25SearchMock.Object,
            _queryCacheMock.Object,
            realCacheKeyGenerator,
            Options.Create(_options),
            _loggerMock.Object);
    }

    private static SearchResult CreateTestResult(string query = "test", int hitCount = 1)
    {
        var hits = Enumerable.Range(0, hitCount)
            .Select(i => new SearchHit
            {
                Chunk = new TextChunk(
                    Content: $"Test content {i}",
                    StartOffset: i * 100,
                    EndOffset: (i + 1) * 100,
                    Metadata: new ChunkMetadata(Heading: null, Index: i, Level: 0)),
                Document = new Document(
                    Id: Guid.NewGuid(),
                    ProjectId: Guid.NewGuid(),
                    FilePath: $"/test/doc{i}.md",
                    Title: $"Document {i}",
                    Hash: "abc123",
                    Status: DocumentStatus.Indexed,
                    IndexedAt: DateTime.UtcNow,
                    FailureReason: null),
                Score = 0.9f - (i * 0.01f)
            })
            .ToList();

        return new SearchResult
        {
            Hits = hits,
            Query = query,
            WasTruncated = false,
            Duration = TimeSpan.FromMilliseconds(10)
        };
    }

    private static SearchOptions CreateDefaultOptions() => new()
    {
        TopK = 10,
        MinScore = 0.5f
    };

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullHybridSearch_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ResilientSearchService(
                null!,
                _bm25SearchMock.Object,
                _queryCacheMock.Object,
                new CacheKeyGenerator(new Mock<ILogger<CacheKeyGenerator>>().Object),
                Options.Create(_options),
                _loggerMock.Object));

        Assert.Equal("hybridSearch", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullBm25Search_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ResilientSearchService(
                _hybridSearchMock.Object,
                null!,
                _queryCacheMock.Object,
                new CacheKeyGenerator(new Mock<ILogger<CacheKeyGenerator>>().Object),
                Options.Create(_options),
                _loggerMock.Object));

        Assert.Equal("bm25Search", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullQueryCache_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ResilientSearchService(
                _hybridSearchMock.Object,
                _bm25SearchMock.Object,
                null!,
                new CacheKeyGenerator(new Mock<ILogger<CacheKeyGenerator>>().Object),
                Options.Create(_options),
                _loggerMock.Object));

        Assert.Equal("queryCache", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ResilientSearchService(
                _hybridSearchMock.Object,
                _bm25SearchMock.Object,
                _queryCacheMock.Object,
                new CacheKeyGenerator(new Mock<ILogger<CacheKeyGenerator>>().Object),
                Options.Create(_options),
                null!));

        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region Cache Hit Tests

    [Fact]
    public async Task SearchAsync_WhenCacheHit_ReturnsCachedResult()
    {
        // Arrange
        var query = "test query";
        var options = CreateDefaultOptions();
        var cachedResult = CreateTestResult(query);

        _queryCacheMock
            .Setup(c => c.TryGet(It.IsAny<string>(), out cachedResult))
            .Returns(true);

        // Act
        var result = await _sut.SearchAsync(query, options);

        // Assert
        Assert.True(result.IsFromCache);
        Assert.False(result.IsDegraded);
        Assert.Equal(SearchMode.Hybrid, result.ActualMode);
        _hybridSearchMock.Verify(h => h.SearchAsync(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Successful Hybrid Search Tests

    [Fact]
    public async Task SearchAsync_WhenHybridSucceeds_ReturnsNonDegradedResult()
    {
        // Arrange
        var query = "test query";
        var options = CreateDefaultOptions();
        var expectedResult = CreateTestResult(query);
        SearchResult? nullResult = null;

        _queryCacheMock
            .Setup(c => c.TryGet(It.IsAny<string>(), out nullResult))
            .Returns(false);

        _hybridSearchMock
            .Setup(h => h.SearchAsync(query, options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.SearchAsync(query, options);

        // Assert
        Assert.False(result.IsDegraded);
        Assert.False(result.IsFromCache);
        Assert.Equal(SearchMode.Hybrid, result.ActualMode);
        Assert.Equal(expectedResult.Hits.Count, result.Result.Hits.Count);

        // Verify cache was updated
        _queryCacheMock.Verify(c => c.Set(
            It.IsAny<string>(),
            expectedResult,
            It.IsAny<IReadOnlyList<Guid>>()),
            Times.Once);
    }

    #endregion

    #region Fallback to BM25 Tests

    [Fact]
    public async Task SearchAsync_WhenHybridThrowsHttpRequestException_FallsBackToBM25()
    {
        // Arrange
        var query = "test query";
        var options = CreateDefaultOptions();
        var bm25Result = CreateTestResult(query);
        SearchResult? nullResult = null;

        _queryCacheMock
            .Setup(c => c.TryGet(It.IsAny<string>(), out nullResult))
            .Returns(false);

        _hybridSearchMock
            .Setup(h => h.SearchAsync(query, options, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        _bm25SearchMock
            .Setup(b => b.SearchAsync(query, options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bm25Result);

        // Act
        var result = await _sut.SearchAsync(query, options);

        // Assert
        Assert.True(result.IsDegraded);
        Assert.Equal(SearchMode.Keyword, result.ActualMode);
        Assert.NotNull(result.DegradationReason);
        Assert.False(result.HealthStatus.EmbeddingApiAvailable);
    }

    #endregion

    #region Fallback to Cache Tests

    [Fact]
    public async Task SearchAsync_WhenDatabaseUnavailable_FallsBackToCache()
    {
        // Arrange
        var query = "test query";
        var options = CreateDefaultOptions();
        var cachedResult = CreateTestResult(query);

        // First TryGet returns false (initial cache check)
        // Second TryGet returns true (fallback cache check)
        var callCount = 0;
        _queryCacheMock
            .Setup(c => c.TryGet(It.IsAny<string>(), out It.Ref<SearchResult?>.IsAny))
            .Callback(new TryGetCallback((string key, out SearchResult? result) =>
            {
                callCount++;
                result = callCount == 1 ? null : cachedResult;
            }))
            .Returns(() => callCount > 1);

        _hybridSearchMock
            .Setup(h => h.SearchAsync(query, options, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NpgsqlException("Connection failed"));

        // Act
        var result = await _sut.SearchAsync(query, options);

        // Assert
        Assert.True(result.IsDegraded);
        Assert.True(result.IsFromCache);
        Assert.NotNull(result.DegradationReason);
        Assert.False(result.HealthStatus.DatabaseAvailable);
    }

    private delegate void TryGetCallback(string key, out SearchResult? result);

    [Fact]
    public async Task SearchAsync_WhenDatabaseUnavailableAndNoCache_ReturnsUnavailable()
    {
        // Arrange
        var query = "test query";
        var options = CreateDefaultOptions();
        SearchResult? localNullResult = null;

        _queryCacheMock
            .Setup(c => c.TryGet(It.IsAny<string>(), out localNullResult))
            .Returns(false);

        _hybridSearchMock
            .Setup(h => h.SearchAsync(query, options, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NpgsqlException("Connection failed"));

        // Act
        var result = await _sut.SearchAsync(query, options);

        // Assert
        Assert.True(result.IsDegraded);
        Assert.Empty(result.Result.Hits);
        Assert.Equal(DegradedSearchMode.Unavailable, result.HealthStatus.CurrentMode);
    }

    #endregion

    #region Health Status Tests

    [Fact]
    public void GetHealthStatus_InitialState_ReturnsHealthy()
    {
        // Act
        var status = _sut.GetHealthStatus();

        // Assert
        Assert.True(status.EmbeddingApiAvailable);
        Assert.True(status.DatabaseAvailable);
        Assert.True(status.CacheAvailable);
        Assert.Equal(CircuitBreakerState.Closed, status.CircuitBreakerState);
        Assert.Equal(DegradedSearchMode.Full, status.CurrentMode);
    }

    [Fact]
    public async Task GetHealthStatus_AfterEmbeddingFailure_ReflectsState()
    {
        // Arrange
        var query = "test query";
        var options = CreateDefaultOptions();
        var bm25Result = CreateTestResult(query);
        SearchResult? nullResult = null;

        _queryCacheMock
            .Setup(c => c.TryGet(It.IsAny<string>(), out nullResult))
            .Returns(false);

        _hybridSearchMock
            .Setup(h => h.SearchAsync(query, options, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        _bm25SearchMock
            .Setup(b => b.SearchAsync(query, options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bm25Result);

        // Act
        await _sut.SearchAsync(query, options);
        var status = _sut.GetHealthStatus();

        // Assert
        Assert.False(status.EmbeddingApiAvailable);
        Assert.Equal(DegradedSearchMode.KeywordOnly, status.CurrentMode);
    }

    #endregion

    #region Reset Circuit Breaker Tests

    [Fact]
    public async Task ResetCircuitBreaker_AfterFailure_RestoresHealthyState()
    {
        // Arrange - cause a failure first
        var query = "test query";
        var options = CreateDefaultOptions();
        var bm25Result = CreateTestResult(query);
        SearchResult? nullResult = null;

        _queryCacheMock
            .Setup(c => c.TryGet(It.IsAny<string>(), out nullResult))
            .Returns(false);

        _hybridSearchMock
            .Setup(h => h.SearchAsync(query, options, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        _bm25SearchMock
            .Setup(b => b.SearchAsync(query, options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bm25Result);

        await _sut.SearchAsync(query, options);
        var statusBeforeReset = _sut.GetHealthStatus();
        Assert.False(statusBeforeReset.EmbeddingApiAvailable);

        // Act
        _sut.ResetCircuitBreaker();
        var statusAfterReset = _sut.GetHealthStatus();

        // Assert
        Assert.True(statusAfterReset.EmbeddingApiAvailable);
        Assert.True(statusAfterReset.DatabaseAvailable);
        Assert.Equal(CircuitBreakerState.Closed, statusAfterReset.CircuitBreakerState);
        Assert.Equal(DegradedSearchMode.Full, statusAfterReset.CurrentMode);
    }

    #endregion

    #region Parameter Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_WithInvalidQuery_ThrowsArgumentException(string? query)
    {
        var options = CreateDefaultOptions();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _sut.SearchAsync(query!, options));
    }

    [Fact]
    public async Task SearchAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.SearchAsync("test", null!));
    }

    #endregion
}
