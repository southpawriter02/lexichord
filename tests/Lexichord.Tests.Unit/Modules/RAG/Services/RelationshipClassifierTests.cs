// =============================================================================
// File: RelationshipClassifierTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for RelationshipClassifier service.
// =============================================================================
// VERSION: v0.5.9b (Relationship Classification)
// =============================================================================

using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="RelationshipClassifier"/>.
/// </summary>
public sealed class RelationshipClassifierTests
{
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<IOptions<ClassificationOptions>> _optionsMock;
    private readonly Mock<ILogger<RelationshipClassifier>> _loggerMock;
    private readonly RelationshipClassifier _sut;

    public RelationshipClassifierTests()
    {
        _cacheMock = new Mock<IMemoryCache>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _optionsMock = new Mock<IOptions<ClassificationOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(ClassificationOptions.Default);
        _loggerMock = new Mock<ILogger<RelationshipClassifier>>();

        // Default: license enabled
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SemanticDeduplication))
            .Returns(true);

        // Default: cache miss
        object? nullValue = null;
        _cacheMock
            .Setup(c => c.TryGetValue(It.IsAny<object>(), out nullValue))
            .Returns(false);

        // Default: cache set returns a mock entry
        var cacheEntryMock = new Mock<ICacheEntry>();
        _cacheMock
            .Setup(c => c.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntryMock.Object);

        _sut = new RelationshipClassifier(
            _cacheMock.Object,
            _licenseContextMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);
    }

    private static Chunk CreateTestChunk(
        Guid? id = null,
        Guid? documentId = null,
        int chunkIndex = 0,
        string? content = null)
    {
        return new Chunk(
            Id: id ?? Guid.NewGuid(),
            DocumentId: documentId ?? Guid.NewGuid(),
            Content: content ?? $"Test content for chunk {chunkIndex}",
            Embedding: new float[] { 0.1f, 0.2f, 0.3f },
            ChunkIndex: chunkIndex,
            StartOffset: chunkIndex * 100,
            EndOffset: (chunkIndex + 1) * 100);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RelationshipClassifier(
                null!,
                _licenseContextMock.Object,
                _optionsMock.Object,
                _loggerMock.Object));

        Assert.Equal("cache", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLicenseContext_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RelationshipClassifier(
                _cacheMock.Object,
                null!,
                _optionsMock.Object,
                _loggerMock.Object));

        Assert.Equal("licenseContext", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RelationshipClassifier(
                _cacheMock.Object,
                _licenseContextMock.Object,
                null!,
                _loggerMock.Object));

        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RelationshipClassifier(
                _cacheMock.Object,
                _licenseContextMock.Object,
                _optionsMock.Object,
                null!));

        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region ClassifyAsync Tests

    [Fact]
    public async Task ClassifyAsync_WithNullChunkA_ThrowsArgumentNullException()
    {
        var chunkB = CreateTestChunk();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.ClassifyAsync(null!, chunkB, 0.95f));
    }

    [Fact]
    public async Task ClassifyAsync_WithNullChunkB_ThrowsArgumentNullException()
    {
        var chunkA = CreateTestChunk();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.ClassifyAsync(chunkA, null!, 0.95f));
    }

    [Fact]
    public async Task ClassifyAsync_WithoutLicense_ReturnsUnknown()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SemanticDeduplication))
            .Returns(false);

        var chunkA = CreateTestChunk();
        var chunkB = CreateTestChunk();

        // Act
        var result = await _sut.ClassifyAsync(chunkA, chunkB, 0.95f);

        // Assert
        Assert.Equal(RelationshipType.Unknown, result.Type);
        Assert.Equal(0f, result.Confidence);
        Assert.NotNull(result.Explanation);
        Assert.Contains("License", result.Explanation);
    }

    [Fact]
    public async Task ClassifyAsync_WithPerfectSimilarity_ReturnsEquivalent()
    {
        // Arrange
        var chunkA = CreateTestChunk();
        var chunkB = CreateTestChunk();

        // Act
        var result = await _sut.ClassifyAsync(chunkA, chunkB, 1.0f);

        // Assert
        Assert.Equal(RelationshipType.Equivalent, result.Type);
        Assert.Equal(1.0f, result.Confidence);
        Assert.Equal(ClassificationMethod.RuleBased, result.Method);
    }

    [Fact]
    public async Task ClassifyAsync_WithHighSimilarity_ReturnsEquivalent()
    {
        // Arrange
        var chunkA = CreateTestChunk();
        var chunkB = CreateTestChunk();

        // Act
        var result = await _sut.ClassifyAsync(chunkA, chunkB, 0.97f);

        // Assert
        Assert.Equal(RelationshipType.Equivalent, result.Type);
        Assert.True(result.Confidence >= 0.95f);
        Assert.Equal(ClassificationMethod.RuleBased, result.Method);
    }

    [Fact]
    public async Task ClassifyAsync_WithLowSimilarity_ReturnsDistinct()
    {
        // Arrange
        var chunkA = CreateTestChunk();
        var chunkB = CreateTestChunk();

        // Act
        var result = await _sut.ClassifyAsync(chunkA, chunkB, 0.75f);

        // Assert
        Assert.Equal(RelationshipType.Distinct, result.Type);
        Assert.Equal(ClassificationMethod.RuleBased, result.Method);
    }

    [Fact]
    public async Task ClassifyAsync_SameDocumentAdjacentChunks_ReturnsComplementary()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var chunkA = CreateTestChunk(documentId: docId, chunkIndex: 0);
        var chunkB = CreateTestChunk(documentId: docId, chunkIndex: 1);

        // Act
        var result = await _sut.ClassifyAsync(chunkA, chunkB, 0.97f);

        // Assert
        Assert.Equal(RelationshipType.Complementary, result.Type);
        Assert.Equal(ClassificationMethod.RuleBased, result.Method);
    }

    [Fact]
    public async Task ClassifyAsync_SignificantLengthDifference_ReturnsSubset()
    {
        // Arrange
        var chunkA = CreateTestChunk(content: "Short content.");
        var chunkB = CreateTestChunk(content: "This is a much longer content that contains significantly more text than the first chunk, making it appear as if the first chunk is a subset of this one.");

        // Act - Use >= 0.95 to trigger rule-based path with subset detection
        var result = await _sut.ClassifyAsync(chunkA, chunkB, 0.96f);

        // Assert
        Assert.Equal(RelationshipType.Subset, result.Type);
        Assert.Equal(ClassificationMethod.RuleBased, result.Method);
    }

    [Fact]
    public async Task ClassifyAsync_WithCaching_ReturnsCachedResult()
    {
        // Arrange
        var chunkA = CreateTestChunk();
        var chunkB = CreateTestChunk();
        var cachedResult = new RelationshipClassification(
            RelationshipType.Equivalent,
            0.99f,
            "Cached explanation",
            ClassificationMethod.RuleBased);

        object? cacheValue = cachedResult;
        _cacheMock
            .Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);

        // Act
        var result = await _sut.ClassifyAsync(chunkA, chunkB, 0.97f);

        // Assert
        Assert.Equal(RelationshipType.Equivalent, result.Type);
        Assert.Equal(ClassificationMethod.Cached, result.Method);
    }

    [Fact]
    public async Task ClassifyAsync_CacheKeyIsSorted()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var chunkA = CreateTestChunk(id: id1);
        var chunkB = CreateTestChunk(id: id2);

        string? capturedKey1 = null;
        string? capturedKey2 = null;

        // Capture cache key on first call
        _cacheMock
            .SetupSequence(c => c.CreateEntry(It.IsAny<object>()))
            .Returns(() =>
            {
                capturedKey1 = _cacheMock.Invocations.LastOrDefault()?.Arguments[0]?.ToString();
                return new Mock<ICacheEntry>().Object;
            })
            .Returns(() =>
            {
                capturedKey2 = _cacheMock.Invocations.LastOrDefault()?.Arguments[0]?.ToString();
                return new Mock<ICacheEntry>().Object;
            });

        // Act
        await _sut.ClassifyAsync(chunkA, chunkB, 0.97f);
        await _sut.ClassifyAsync(chunkB, chunkA, 0.97f); // Reversed order

        // Assert - both calls should use the same sorted key
        Assert.NotNull(capturedKey1);
        Assert.NotNull(capturedKey2);
        Assert.Equal(capturedKey1, capturedKey2);
    }

    #endregion

    #region ClassifyBatchAsync Tests

    [Fact]
    public async Task ClassifyBatchAsync_WithNullPairs_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.ClassifyBatchAsync(null!));
    }

    [Fact]
    public async Task ClassifyBatchAsync_WithEmptyPairs_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.ClassifyBatchAsync(Array.Empty<ChunkPair>());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ClassifyBatchAsync_ProcessesAllPairs()
    {
        // Arrange
        var pairs = new List<ChunkPair>
        {
            new ChunkPair(CreateTestChunk(), CreateTestChunk(), 0.97f),
            new ChunkPair(CreateTestChunk(), CreateTestChunk(), 0.98f),
            new ChunkPair(CreateTestChunk(), CreateTestChunk(), 0.99f)
        };

        // Act
        var results = await _sut.ClassifyBatchAsync(pairs);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal(RelationshipType.Equivalent, r.Type));
    }

    [Fact]
    public async Task ClassifyBatchAsync_RespectsCancellationToken()
    {
        // Arrange
        var pairs = Enumerable.Range(0, 100)
            .Select(_ => new ChunkPair(CreateTestChunk(), CreateTestChunk(), 0.97f))
            .ToList();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.ClassifyBatchAsync(pairs, ct: cts.Token));
    }

    #endregion

    #region Options Configuration Tests

    [Fact]
    public async Task ClassifyAsync_RespectsCustomRuleBasedThreshold()
    {
        // Arrange
        var chunkA = CreateTestChunk();
        var chunkB = CreateTestChunk();
        var options = new ClassificationOptions { RuleBasedThreshold = 0.99f };

        // Act - 0.97 is below custom threshold 0.99
        var result = await _sut.ClassifyAsync(chunkA, chunkB, 0.97f, options);

        // Assert - should still classify (fallback to rule-based for ambiguous)
        Assert.Equal(ClassificationMethod.RuleBased, result.Method);
    }

    [Fact]
    public async Task ClassifyAsync_RespectsExplanationOption()
    {
        // Arrange
        var chunkA = CreateTestChunk();
        var chunkB = CreateTestChunk();
        var optionsWithExplanation = new ClassificationOptions { IncludeExplanation = true };
        var optionsWithoutExplanation = new ClassificationOptions { IncludeExplanation = false };

        // Act
        var resultWith = await _sut.ClassifyAsync(chunkA, chunkB, 0.97f, optionsWithExplanation);
        var resultWithout = await _sut.ClassifyAsync(chunkA, chunkB, 0.97f, optionsWithoutExplanation);

        // Assert
        Assert.NotNull(resultWith.Explanation);
        Assert.Null(resultWithout.Explanation);
    }

    #endregion
}
