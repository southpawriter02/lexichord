// =============================================================================
// File: CachedEmbeddingServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for CachedEmbeddingService.
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
/// Unit tests for <see cref="CachedEmbeddingService"/>.
/// </summary>
public class CachedEmbeddingServiceTests
{
    private readonly Mock<IEmbeddingService> _innerServiceMock;
    private readonly Mock<IEmbeddingCache> _cacheMock;
    private readonly Mock<ILogger<CachedEmbeddingService>> _loggerMock;
    private readonly EmbeddingCacheOptions _options;

    public CachedEmbeddingServiceTests()
    {
        _innerServiceMock = new Mock<IEmbeddingService>();
        _cacheMock = new Mock<IEmbeddingCache>();
        _loggerMock = new Mock<ILogger<CachedEmbeddingService>>();
        _options = new EmbeddingCacheOptions { Enabled = true };

        _innerServiceMock.Setup(s => s.ModelName).Returns("text-embedding-3-small");
        _innerServiceMock.Setup(s => s.Dimensions).Returns(1536);
        _innerServiceMock.Setup(s => s.MaxTokens).Returns(8191);
    }

    private CachedEmbeddingService CreateSut() =>
        new(_innerServiceMock.Object, _cacheMock.Object, Options.Create(_options), _loggerMock.Object);

    private static float[] CreateEmbedding(float seed = 1.0f)
    {
        var embedding = new float[1536];
        for (var i = 0; i < 1536; i++)
        {
            embedding[i] = seed + (i * 0.001f);
        }
        return embedding;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullInner_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>("inner", () =>
            new CachedEmbeddingService(null!, _cacheMock.Object, Options.Create(_options), _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>("cache", () =>
            new CachedEmbeddingService(_innerServiceMock.Object, null!, Options.Create(_options), _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CachedEmbeddingService(_innerServiceMock.Object, _cacheMock.Object, null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>("logger", () =>
            new CachedEmbeddingService(_innerServiceMock.Object, _cacheMock.Object, Options.Create(_options), null!));
    }

    #endregion

    #region Property Pass-through Tests

    [Fact]
    public void ModelName_ReturnsInnerModelName()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.ModelName.Should().Be("text-embedding-3-small");
    }

    [Fact]
    public void Dimensions_ReturnsInnerDimensions()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.Dimensions.Should().Be(1536);
    }

    [Fact]
    public void MaxTokens_ReturnsInnerMaxTokens()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.MaxTokens.Should().Be(8191);
    }

    #endregion

    #region EmbedAsync Tests

    [Fact]
    public async Task EmbedAsync_WhenCacheHit_ReturnsCachedEmbedding()
    {
        // Arrange
        var text = "Hello, World!";
        var cachedEmbedding = CreateEmbedding();
        float[]? outEmbedding = cachedEmbedding;

        _cacheMock
            .Setup(c => c.TryGet(It.IsAny<string>(), out outEmbedding))
            .Returns(true);

        var sut = CreateSut();

        // Act
        var result = await sut.EmbedAsync(text);

        // Assert
        result.Should().BeEquivalentTo(cachedEmbedding);
        _innerServiceMock.Verify(s => s.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EmbedAsync_WhenCacheMiss_CallsInnerService()
    {
        // Arrange
        var text = "Hello, World!";
        var embedding = CreateEmbedding();
        float[]? outEmbedding = null;

        _cacheMock
            .Setup(c => c.TryGet(It.IsAny<string>(), out outEmbedding))
            .Returns(false);

        _innerServiceMock
            .Setup(s => s.EmbedAsync(text, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var sut = CreateSut();

        // Act
        var result = await sut.EmbedAsync(text);

        // Assert
        result.Should().BeEquivalentTo(embedding);
        _innerServiceMock.Verify(s => s.EmbedAsync(text, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EmbedAsync_WhenCacheMiss_CachesResult()
    {
        // Arrange
        var text = "Hello, World!";
        var embedding = CreateEmbedding();
        float[]? outEmbedding = null;

        _cacheMock
            .Setup(c => c.TryGet(It.IsAny<string>(), out outEmbedding))
            .Returns(false);

        _innerServiceMock
            .Setup(s => s.EmbedAsync(text, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var sut = CreateSut();

        // Act
        await sut.EmbedAsync(text);

        // Assert
        _cacheMock.Verify(c => c.Set(It.IsAny<string>(), embedding), Times.Once);
    }

    [Fact]
    public async Task EmbedAsync_WhenDisabled_SkipsCache()
    {
        // Arrange
        _options.Enabled = false;
        var text = "Hello, World!";
        var embedding = CreateEmbedding();

        _innerServiceMock
            .Setup(s => s.EmbedAsync(text, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embedding);

        var sut = CreateSut();

        // Act
        var result = await sut.EmbedAsync(text);

        // Assert
        result.Should().BeEquivalentTo(embedding);
        _cacheMock.Verify(c => c.TryGet(It.IsAny<string>(), out It.Ref<float[]?>.IsAny), Times.Never);
        _cacheMock.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<float[]>()), Times.Never);
    }

    #endregion

    #region EmbedBatchAsync Tests

    [Fact]
    public async Task EmbedBatchAsync_WhenAllCached_ReturnsAllFromCache()
    {
        // Arrange
        var texts = new List<string> { "Text1", "Text2", "Text3" };
        var embeddings = texts.Select((_, i) => CreateEmbedding(i)).ToList();

        // Setup cache to return embeddings for all texts
        var callIndex = 0;
        _cacheMock
            .Setup(c => c.TryGet(It.IsAny<string>(), out It.Ref<float[]?>.IsAny))
            .Returns((string hash, out float[]? emb) =>
            {
                emb = embeddings[callIndex++ % embeddings.Count];
                return true;
            });

        var sut = CreateSut();

        // Act
        var result = await sut.EmbedBatchAsync(texts);

        // Assert
        result.Should().HaveCount(3);
        _innerServiceMock.Verify(
            s => s.EmbedBatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EmbedBatchAsync_WhenNoneCached_CallsInnerServiceForAll()
    {
        // Arrange
        var texts = new List<string> { "Text1", "Text2", "Text3" };
        var embeddings = texts.Select((_, i) => CreateEmbedding(i)).ToArray();
        float[]? nullEmbedding = null;

        _cacheMock
            .Setup(c => c.TryGet(It.IsAny<string>(), out nullEmbedding))
            .Returns(false);

        _innerServiceMock
            .Setup(s => s.EmbedBatchAsync(
                It.Is<IReadOnlyList<string>>(l => l.Count == 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);

        var sut = CreateSut();

        // Act
        var result = await sut.EmbedBatchAsync(texts);

        // Assert
        result.Should().HaveCount(3);
        _innerServiceMock.Verify(
            s => s.EmbedBatchAsync(It.Is<IReadOnlyList<string>>(l => l.Count == 3), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EmbedBatchAsync_WhenPartialCache_OnlyFetchesMisses()
    {
        // Arrange
        var texts = new List<string> { "Text1", "Text2", "Text3" };
        var cachedEmbedding = CreateEmbedding(0);
        var missEmbeddings = new[] { CreateEmbedding(1), CreateEmbedding(2) };

        var callCount = 0;
        _cacheMock
            .Setup(c => c.TryGet(It.IsAny<string>(), out It.Ref<float[]?>.IsAny))
            .Returns((string hash, out float[]? emb) =>
            {
                // First text is cached, rest are not
                if (callCount++ == 0)
                {
                    emb = cachedEmbedding;
                    return true;
                }
                emb = null;
                return false;
            });

        _innerServiceMock
            .Setup(s => s.EmbedBatchAsync(
                It.Is<IReadOnlyList<string>>(l => l.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(missEmbeddings);

        var sut = CreateSut();

        // Act
        var result = await sut.EmbedBatchAsync(texts);

        // Assert
        result.Should().HaveCount(3);
        _innerServiceMock.Verify(
            s => s.EmbedBatchAsync(It.Is<IReadOnlyList<string>>(l => l.Count == 2), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EmbedBatchAsync_WhenDisabled_SkipsCache()
    {
        // Arrange
        _options.Enabled = false;
        var texts = new List<string> { "Text1", "Text2" };
        var embeddings = texts.Select((_, i) => CreateEmbedding(i)).ToArray();

        _innerServiceMock
            .Setup(s => s.EmbedBatchAsync(texts, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);

        var sut = CreateSut();

        // Act
        var result = await sut.EmbedBatchAsync(texts);

        // Assert
        result.Should().HaveCount(2);
        _cacheMock.Verify(c => c.TryGet(It.IsAny<string>(), out It.Ref<float[]?>.IsAny), Times.Never);
    }

    #endregion
}
