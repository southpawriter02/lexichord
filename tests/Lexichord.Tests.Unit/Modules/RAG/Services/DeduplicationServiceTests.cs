// =============================================================================
// File: DeduplicationServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for DeduplicationService.
// =============================================================================
// VERSION: v0.5.9d (Deduplication Service)
// =============================================================================

using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="DeduplicationService"/>.
/// </summary>
/// <remarks>
/// These tests focus on constructor validation, license gating, and early-return paths.
/// Full integration tests requiring database interactions are covered in integration test projects.
/// </remarks>
public sealed class DeduplicationServiceTests
{
    private readonly Mock<ISimilarityDetector> _similarityDetectorMock;
    private readonly Mock<IRelationshipClassifier> _relationshipClassifierMock;
    private readonly Mock<ICanonicalManager> _canonicalManagerMock;
    private readonly Mock<IContradictionService> _contradictionServiceMock;
    private readonly Mock<IChunkRepository> _chunkRepositoryMock;
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<ILogger<DeduplicationService>> _loggerMock;

    public DeduplicationServiceTests()
    {
        _similarityDetectorMock = new Mock<ISimilarityDetector>();
        _relationshipClassifierMock = new Mock<IRelationshipClassifier>();
        _canonicalManagerMock = new Mock<ICanonicalManager>();
        _contradictionServiceMock = new Mock<IContradictionService>();
        _chunkRepositoryMock = new Mock<IChunkRepository>();
        _connectionFactoryMock = new Mock<IDbConnectionFactory>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _loggerMock = new Mock<ILogger<DeduplicationService>>();

        // Default: license enabled
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DeduplicationService))
            .Returns(true);
    }

    private DeduplicationService CreateSut()
    {
        return new DeduplicationService(
            _similarityDetectorMock.Object,
            _relationshipClassifierMock.Object,
            _canonicalManagerMock.Object,
            _contradictionServiceMock.Object,
            _chunkRepositoryMock.Object,
            _connectionFactoryMock.Object,
            _licenseContextMock.Object,
            _loggerMock.Object);
    }

    private static Chunk CreateTestChunk(
        Guid? id = null,
        Guid? documentId = null,
        int chunkIndex = 0,
        string? content = null,
        float[]? embedding = null)
    {
        return new Chunk(
            Id: id ?? Guid.NewGuid(),
            DocumentId: documentId ?? Guid.NewGuid(),
            Content: content ?? $"Test content for chunk {chunkIndex}",
            Embedding: embedding ?? [0.1f, 0.2f, 0.3f],
            ChunkIndex: chunkIndex,
            StartOffset: chunkIndex * 100,
            EndOffset: (chunkIndex + 1) * 100);
    }

    private static SimilarChunkResult CreateSimilarResult(
        Guid sourceChunkId,
        Guid matchedChunkId,
        double similarity)
    {
        return new SimilarChunkResult(
            SourceChunkId: sourceChunkId,
            MatchedChunkId: matchedChunkId,
            SimilarityScore: similarity,
            MatchedChunkContent: "Test matched content",
            MatchedDocumentPath: "/test/matched/document.md",
            MatchedChunkIndex: 0);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullSimilarityDetector_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationService(
                null!,
                _relationshipClassifierMock.Object,
                _canonicalManagerMock.Object,
                _contradictionServiceMock.Object,
                _chunkRepositoryMock.Object,
                _connectionFactoryMock.Object,
                _licenseContextMock.Object,
                _loggerMock.Object));

        Assert.Equal("similarityDetector", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullRelationshipClassifier_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationService(
                _similarityDetectorMock.Object,
                null!,
                _canonicalManagerMock.Object,
                _contradictionServiceMock.Object,
                _chunkRepositoryMock.Object,
                _connectionFactoryMock.Object,
                _licenseContextMock.Object,
                _loggerMock.Object));

        Assert.Equal("relationshipClassifier", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullCanonicalManager_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationService(
                _similarityDetectorMock.Object,
                _relationshipClassifierMock.Object,
                null!,
                _contradictionServiceMock.Object,
                _chunkRepositoryMock.Object,
                _connectionFactoryMock.Object,
                _licenseContextMock.Object,
                _loggerMock.Object));

        Assert.Equal("canonicalManager", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullContradictionService_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationService(
                _similarityDetectorMock.Object,
                _relationshipClassifierMock.Object,
                _canonicalManagerMock.Object,
                null!,
                _chunkRepositoryMock.Object,
                _connectionFactoryMock.Object,
                _licenseContextMock.Object,
                _loggerMock.Object));

        Assert.Equal("contradictionService", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullChunkRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationService(
                _similarityDetectorMock.Object,
                _relationshipClassifierMock.Object,
                _canonicalManagerMock.Object,
                _contradictionServiceMock.Object,
                null!,
                _connectionFactoryMock.Object,
                _licenseContextMock.Object,
                _loggerMock.Object));

        Assert.Equal("chunkRepository", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationService(
                _similarityDetectorMock.Object,
                _relationshipClassifierMock.Object,
                _canonicalManagerMock.Object,
                _contradictionServiceMock.Object,
                _chunkRepositoryMock.Object,
                null!,
                _licenseContextMock.Object,
                _loggerMock.Object));

        Assert.Equal("connectionFactory", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLicenseContext_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationService(
                _similarityDetectorMock.Object,
                _relationshipClassifierMock.Object,
                _canonicalManagerMock.Object,
                _contradictionServiceMock.Object,
                _chunkRepositoryMock.Object,
                _connectionFactoryMock.Object,
                null!,
                _loggerMock.Object));

        Assert.Equal("licenseContext", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationService(
                _similarityDetectorMock.Object,
                _relationshipClassifierMock.Object,
                _canonicalManagerMock.Object,
                _contradictionServiceMock.Object,
                _chunkRepositoryMock.Object,
                _connectionFactoryMock.Object,
                _licenseContextMock.Object,
                null!));

        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithValidDependencies_Succeeds()
    {
        var sut = CreateSut();
        Assert.NotNull(sut);
    }

    #endregion

    #region ProcessChunkAsync Tests

    [Fact]
    public async Task ProcessChunkAsync_WithNullChunk_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.ProcessChunkAsync(null!));
    }

    [Fact]
    public async Task ProcessChunkAsync_WithNullEmbedding_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var chunk = new Chunk(
            Id: Guid.NewGuid(),
            DocumentId: Guid.NewGuid(),
            Content: "Test",
            Embedding: null,
            ChunkIndex: 0,
            StartOffset: 0,
            EndOffset: 10);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ProcessChunkAsync(chunk));

        Assert.Contains("embedding", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessChunkAsync_WhenUnlicensed_ReturnsStoredAsNewWithoutProcessing()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DeduplicationService))
            .Returns(false);

        var sut = CreateSut();
        var chunk = CreateTestChunk();

        // Act
        var result = await sut.ProcessChunkAsync(chunk);

        // Assert
        Assert.Equal(DeduplicationAction.StoredAsNew, result.ActionTaken);
        Assert.Equal(chunk.Id, result.CanonicalChunkId);

        // Verify no similarity detection was performed
        _similarityDetectorMock.Verify(
            s => s.FindSimilarAsync(It.IsAny<Chunk>(), It.IsAny<SimilarityDetectorOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessChunkAsync_WithNoSimilarChunks_CreatesCanonicalAndReturnsStoredAsNew()
    {
        // Arrange
        var sut = CreateSut();
        var chunk = CreateTestChunk();

        _similarityDetectorMock
            .Setup(s => s.FindSimilarAsync(chunk, It.IsAny<SimilarityDetectorOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SimilarChunkResult>());

        var canonicalRecord = new CanonicalRecord(
            Id: Guid.NewGuid(),
            CanonicalChunkId: chunk.Id,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            MergeCount: 0);

        _canonicalManagerMock
            .Setup(c => c.CreateCanonicalAsync(chunk, It.IsAny<CancellationToken>()))
            .ReturnsAsync(canonicalRecord);

        // Act
        var result = await sut.ProcessChunkAsync(chunk);

        // Assert
        Assert.Equal(DeduplicationAction.StoredAsNew, result.ActionTaken);
        Assert.Equal(chunk.Id, result.CanonicalChunkId);

        _canonicalManagerMock.Verify(
            c => c.CreateCanonicalAsync(chunk, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessChunkAsync_TracksProcessingDuration()
    {
        // Arrange
        var sut = CreateSut();
        var chunk = CreateTestChunk();

        _similarityDetectorMock
            .Setup(s => s.FindSimilarAsync(chunk, It.IsAny<SimilarityDetectorOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SimilarChunkResult>());

        var canonicalRecord = new CanonicalRecord(
            Id: Guid.NewGuid(),
            CanonicalChunkId: chunk.Id,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            MergeCount: 0);

        _canonicalManagerMock
            .Setup(c => c.CreateCanonicalAsync(chunk, It.IsAny<CancellationToken>()))
            .ReturnsAsync(canonicalRecord);

        // Act
        var result = await sut.ProcessChunkAsync(chunk);

        // Assert
        Assert.NotNull(result.ProcessingDuration);
        Assert.True(result.ProcessingDuration.Value > TimeSpan.Zero);
    }

    #endregion

    #region FindDuplicatesAsync Tests

    [Fact]
    public async Task FindDuplicatesAsync_WithNullChunk_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.FindDuplicatesAsync(null!));
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithNullEmbedding_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var chunk = new Chunk(
            Id: Guid.NewGuid(),
            DocumentId: Guid.NewGuid(),
            Content: "Test",
            Embedding: null,
            ChunkIndex: 0,
            StartOffset: 0,
            EndOffset: 10);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.FindDuplicatesAsync(chunk));
    }

    [Fact]
    public async Task FindDuplicatesAsync_WhenUnlicensed_ReturnsEmptyList()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DeduplicationService))
            .Returns(false);

        var sut = CreateSut();
        var chunk = CreateTestChunk();

        // Act
        var result = await sut.FindDuplicatesAsync(chunk);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task FindDuplicatesAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var sut = CreateSut();
        var chunk = CreateTestChunk();

        _similarityDetectorMock
            .Setup(s => s.FindSimilarAsync(chunk, It.IsAny<SimilarityDetectorOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SimilarChunkResult>());

        // Act
        var result = await sut.FindDuplicatesAsync(chunk);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region ProcessManualDecisionAsync Tests

    [Fact]
    public async Task ProcessManualDecisionAsync_WithNullDecision_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.ProcessManualDecisionAsync(null!));
    }

    #endregion

    #region DeduplicationOptions Tests

    [Fact]
    public void DeduplicationOptions_DefaultValues_AreCorrect()
    {
        var options = DeduplicationOptions.Default;

        Assert.Equal(0.85f, options.SimilarityThreshold);
        Assert.Equal(5, options.MaxCandidates);
        Assert.True(options.RequireLlmConfirmation);
        Assert.True(options.EnableManualReviewQueue);
        Assert.True(options.EnableContradictionDetection);
        Assert.Null(options.ProjectScope);
    }

    [Fact]
    public void DeduplicationOptions_CustomValues_ArePreserved()
    {
        var projectId = Guid.NewGuid();
        var options = new DeduplicationOptions
        {
            SimilarityThreshold = 0.90f,
            MaxCandidates = 10,
            RequireLlmConfirmation = false,
            EnableManualReviewQueue = false,
            EnableContradictionDetection = false,
            ProjectScope = projectId
        };

        Assert.Equal(0.90f, options.SimilarityThreshold);
        Assert.Equal(10, options.MaxCandidates);
        Assert.False(options.RequireLlmConfirmation);
        Assert.False(options.EnableManualReviewQueue);
        Assert.False(options.EnableContradictionDetection);
        Assert.Equal(projectId, options.ProjectScope);
    }

    #endregion

    #region DeduplicationResult Factory Methods Tests

    [Fact]
    public void DeduplicationResult_StoredAsNew_CreatesCorrectResult()
    {
        var chunkId = Guid.NewGuid();
        var duration = TimeSpan.FromMilliseconds(100);

        var result = DeduplicationResult.StoredAsNew(chunkId, duration);

        Assert.Equal(chunkId, result.CanonicalChunkId);
        Assert.Equal(DeduplicationAction.StoredAsNew, result.ActionTaken);
        Assert.Null(result.MergedFromId);
        Assert.Null(result.LinkedChunkIds);
        Assert.Equal(duration, result.ProcessingDuration);
    }

    [Fact]
    public void DeduplicationResult_Merged_CreatesCorrectResult()
    {
        var canonicalId = Guid.NewGuid();
        var mergedFromId = Guid.NewGuid();
        var duration = TimeSpan.FromMilliseconds(150);

        var result = DeduplicationResult.Merged(canonicalId, mergedFromId, duration);

        Assert.Equal(canonicalId, result.CanonicalChunkId);
        Assert.Equal(DeduplicationAction.MergedIntoExisting, result.ActionTaken);
        Assert.Equal(mergedFromId, result.MergedFromId);
        Assert.Equal(duration, result.ProcessingDuration);
    }

    [Fact]
    public void DeduplicationResult_QueuedForReview_CreatesCorrectResult()
    {
        var chunkId = Guid.NewGuid();
        var duration = TimeSpan.FromMilliseconds(75);

        var result = DeduplicationResult.QueuedForReview(chunkId, duration);

        Assert.Equal(chunkId, result.CanonicalChunkId);
        Assert.Equal(DeduplicationAction.QueuedForReview, result.ActionTaken);
        Assert.Equal(duration, result.ProcessingDuration);
    }

    #endregion
}
