// =============================================================================
// File: DocumentIndexingPipelineTests.cs
// Project: Lexichord.Tests.Unit
// Description: Comprehensive unit tests for v0.4.4d document indexing pipeline.
//              Tests document indexing orchestration, chunking, embedding, and event publishing.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Indexing;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;

namespace Lexichord.Tests.Unit.Modules;

/// <summary>
/// Unit tests for <see cref="DocumentIndexingPipeline"/>.
/// </summary>
/// <remarks>
/// Introduced in v0.4.4d as part of the Document Indexing Pipeline.
/// Tests the orchestration of chunking, token validation, embedding, and storage.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.4d")]
public class DocumentIndexingPipelineTests
{
    private readonly Mock<ChunkingStrategyFactory> _mockChunkingStrategyFactory = new();
    private readonly Mock<ITokenCounter> _mockTokenCounter = new();
    private readonly Mock<IEmbeddingService> _mockEmbeddingService = new();
    private readonly Mock<IChunkRepository> _mockChunkRepository = new();
    private readonly Mock<IDocumentRepository> _mockDocumentRepository = new();
    private readonly Mock<IMediator> _mockMediator = new();
    private readonly Mock<ILicenseContext> _mockLicenseContext = new();
    private readonly Mock<ILogger<DocumentIndexingPipeline>> _mockLogger = new();
    private readonly EmbeddingOptions _defaultEmbeddingOptions = EmbeddingOptions.Default;

    #region Constructor Tests

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultEmbeddingOptions);

        // Act
        var act = () => new DocumentIndexingPipeline(
            _mockChunkingStrategyFactory.Object,
            _mockTokenCounter.Object,
            _mockEmbeddingService.Object,
            _mockChunkRepository.Object,
            _mockDocumentRepository.Object,
            _mockMediator.Object,
            _mockLicenseContext.Object,
            optionsAccessor,
            _mockLogger.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public void Constructor_WithNullChunkingStrategyFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultEmbeddingOptions);

        // Act & Assert
        var act = () => new DocumentIndexingPipeline(
            null!,
            _mockTokenCounter.Object,
            _mockEmbeddingService.Object,
            _mockChunkRepository.Object,
            _mockDocumentRepository.Object,
            _mockMediator.Object,
            _mockLicenseContext.Object,
            optionsAccessor,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("chunkingStrategyFactory");
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public void Constructor_WithNullTokenCounter_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultEmbeddingOptions);

        // Act & Assert
        var act = () => new DocumentIndexingPipeline(
            _mockChunkingStrategyFactory.Object,
            null!,
            _mockEmbeddingService.Object,
            _mockChunkRepository.Object,
            _mockDocumentRepository.Object,
            _mockMediator.Object,
            _mockLicenseContext.Object,
            optionsAccessor,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tokenCounter");
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public void Constructor_WithNullEmbeddingService_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultEmbeddingOptions);

        // Act & Assert
        var act = () => new DocumentIndexingPipeline(
            _mockChunkingStrategyFactory.Object,
            _mockTokenCounter.Object,
            null!,
            _mockChunkRepository.Object,
            _mockDocumentRepository.Object,
            _mockMediator.Object,
            _mockLicenseContext.Object,
            optionsAccessor,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("embeddingService");
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public void Constructor_WithNullChunkRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultEmbeddingOptions);

        // Act & Assert
        var act = () => new DocumentIndexingPipeline(
            _mockChunkingStrategyFactory.Object,
            _mockTokenCounter.Object,
            _mockEmbeddingService.Object,
            null!,
            _mockDocumentRepository.Object,
            _mockMediator.Object,
            _mockLicenseContext.Object,
            optionsAccessor,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("chunkRepository");
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public void Constructor_WithNullDocumentRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultEmbeddingOptions);

        // Act & Assert
        var act = () => new DocumentIndexingPipeline(
            _mockChunkingStrategyFactory.Object,
            _mockTokenCounter.Object,
            _mockEmbeddingService.Object,
            _mockChunkRepository.Object,
            null!,
            _mockMediator.Object,
            _mockLicenseContext.Object,
            optionsAccessor,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("documentRepository");
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultEmbeddingOptions);

        // Act & Assert
        var act = () => new DocumentIndexingPipeline(
            _mockChunkingStrategyFactory.Object,
            _mockTokenCounter.Object,
            _mockEmbeddingService.Object,
            _mockChunkRepository.Object,
            _mockDocumentRepository.Object,
            null!,
            _mockLicenseContext.Object,
            optionsAccessor,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public void Constructor_WithNullLicenseContext_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultEmbeddingOptions);

        // Act & Assert
        var act = () => new DocumentIndexingPipeline(
            _mockChunkingStrategyFactory.Object,
            _mockTokenCounter.Object,
            _mockEmbeddingService.Object,
            _mockChunkRepository.Object,
            _mockDocumentRepository.Object,
            _mockMediator.Object,
            null!,
            optionsAccessor,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseContext");
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public void Constructor_WithNullEmbeddingOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new DocumentIndexingPipeline(
            _mockChunkingStrategyFactory.Object,
            _mockTokenCounter.Object,
            _mockEmbeddingService.Object,
            _mockChunkRepository.Object,
            _mockDocumentRepository.Object,
            _mockMediator.Object,
            _mockLicenseContext.Object,
            null!,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("embeddingOptions");
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsAccessor = Options.Create(_defaultEmbeddingOptions);

        // Act & Assert
        var act = () => new DocumentIndexingPipeline(
            _mockChunkingStrategyFactory.Object,
            _mockTokenCounter.Object,
            _mockEmbeddingService.Object,
            _mockChunkRepository.Object,
            _mockDocumentRepository.Object,
            _mockMediator.Object,
            _mockLicenseContext.Object,
            optionsAccessor,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region IndexDocumentAsync - License Check Tests

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public async Task IndexDocumentAsync_WithoutLicense_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        var pipeline = CreatePipeline();
        _mockLicenseContext.Setup(lc => lc.GetCurrentTier())
            .Returns(LicenseTier.Free); // Below WriterPro

        // Act & Assert
        var act = () => pipeline.IndexDocumentAsync(
            "test.md",
            "content",
            null,
            CancellationToken.None);

        await act.Should().ThrowAsync<FeatureNotLicensedException>();
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public async Task IndexDocumentAsync_WithWriterProLicense_Proceeds()
    {
        // Arrange
        var pipeline = CreatePipeline();
        _mockLicenseContext.Setup(lc => lc.GetCurrentTier())
            .Returns(LicenseTier.WriterPro);

        SetupSuccessfulIndexing();

        // Act
        var result = await pipeline.IndexDocumentAsync(
            "test.md",
            "content",
            null,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    #endregion

    #region IndexDocumentAsync - Success Tests

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public async Task IndexDocumentAsync_WithValidContent_ReturnsSuccess()
    {
        // Arrange
        var pipeline = CreatePipeline();
        _mockLicenseContext.Setup(lc => lc.GetCurrentTier())
            .Returns(LicenseTier.WriterPro);

        SetupSuccessfulIndexing();

        // Act
        var result = await pipeline.IndexDocumentAsync(
            "test.md",
            "sample content",
            null,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public async Task IndexDocumentAsync_DeletesExistingChunks()
    {
        // Arrange
        var pipeline = CreatePipeline();
        _mockLicenseContext.Setup(lc => lc.GetCurrentTier())
            .Returns(LicenseTier.WriterPro);

        var documentId = Guid.NewGuid();
        _mockDocumentRepository.Setup(dr => dr.GetByFilePathAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Document(documentId, Guid.Empty, "test.md", "Test", "hash", DocumentStatus.Pending));

        SetupSuccessfulIndexing();

        // Act
        await pipeline.IndexDocumentAsync(
            "test.md",
            "content",
            null,
            CancellationToken.None);

        // Assert
        _mockChunkRepository.Verify(
            cr => cr.DeleteByDocumentIdAsync(documentId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public async Task IndexDocumentAsync_PublishesDocumentIndexedEvent()
    {
        // Arrange
        var pipeline = CreatePipeline();
        _mockLicenseContext.Setup(lc => lc.GetCurrentTier())
            .Returns(LicenseTier.WriterPro);

        SetupSuccessfulIndexing();

        // Act
        await pipeline.IndexDocumentAsync(
            "test.md",
            "content",
            null,
            CancellationToken.None);

        // Assert
        _mockMediator.Verify(
            m => m.Publish(It.IsAny<DocumentIndexedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public async Task IndexDocumentAsync_WithTruncation_SetsFlag()
    {
        // Arrange
        var pipeline = CreatePipeline();
        _mockLicenseContext.Setup(lc => lc.GetCurrentTier())
            .Returns(LicenseTier.WriterPro);

        // Setup token counter to indicate truncation
        _mockTokenCounter.Setup(tc => tc.TruncateToTokenLimit(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(("truncated", true));

        SetupSuccessfulIndexing();

        // Act
        var result = await pipeline.IndexDocumentAsync(
            "test.md",
            "content",
            null,
            CancellationToken.None);

        // Assert
        result.WasTruncated.Should().BeTrue();
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public async Task IndexDocumentAsync_EmptyContent_ReturnsSuccessWithZeroChunks()
    {
        // Arrange
        var pipeline = CreatePipeline();
        _mockLicenseContext.Setup(lc => lc.GetCurrentTier())
            .Returns(LicenseTier.WriterPro);

        var mockStrategy = new Mock<IChunkingStrategy>();
        mockStrategy.Setup(s => s.Mode).Returns(ChunkingMode.Fixed);
        mockStrategy.Setup(s => s.Split(It.IsAny<string>(), It.IsAny<ChunkingOptions>()))
            .Returns(Array.Empty<DocumentChunk>());

        _mockChunkingStrategyFactory.Setup(f => f.GetStrategy(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockStrategy.Object);

        _mockDocumentRepository.Setup(dr => dr.GetByFilePathAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        _mockDocumentRepository.Setup(dr => dr.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Document(Guid.NewGuid(), Guid.Empty, "test.md", "Test", "hash", DocumentStatus.Pending));

        _mockChunkRepository.Setup(cr => cr.DeleteByDocumentIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockDocumentRepository.Setup(dr => dr.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<DocumentStatus>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await pipeline.IndexDocumentAsync(
            "test.md",
            "",
            null,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    #endregion

    #region IndexDocumentAsync - Failure Tests

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public async Task IndexDocumentAsync_OnFailure_PublishesFailedEvent()
    {
        // Arrange
        var pipeline = CreatePipeline();
        _mockLicenseContext.Setup(lc => lc.GetCurrentTier())
            .Returns(LicenseTier.WriterPro);

        _mockDocumentRepository.Setup(dr => dr.GetByFilePathAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await pipeline.IndexDocumentAsync(
            "test.md",
            "content",
            null,
            CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        _mockMediator.Verify(
            m => m.Publish(It.IsAny<DocumentIndexingFailedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public async Task IndexDocumentAsync_OnFailure_ReturnsFailureResult()
    {
        // Arrange
        var pipeline = CreatePipeline();
        _mockLicenseContext.Setup(lc => lc.GetCurrentTier())
            .Returns(LicenseTier.WriterPro);

        _mockDocumentRepository.Setup(dr => dr.GetByFilePathAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await pipeline.IndexDocumentAsync(
            "test.md",
            "content",
            null,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Feature", "v0.4.4d")]
    public async Task IndexDocumentAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var pipeline = CreatePipeline();
        _mockLicenseContext.Setup(lc => lc.GetCurrentTier())
            .Returns(LicenseTier.WriterPro);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = () => pipeline.IndexDocumentAsync(
            "test.md",
            "content",
            null,
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Helper Methods

    private DocumentIndexingPipeline CreatePipeline()
    {
        var optionsAccessor = Options.Create(_defaultEmbeddingOptions);
        return new DocumentIndexingPipeline(
            _mockChunkingStrategyFactory.Object,
            _mockTokenCounter.Object,
            _mockEmbeddingService.Object,
            _mockChunkRepository.Object,
            _mockDocumentRepository.Object,
            _mockMediator.Object,
            _mockLicenseContext.Object,
            optionsAccessor,
            _mockLogger.Object);
    }

    private void SetupSuccessfulIndexing()
    {
        var documentId = Guid.NewGuid();
        var chunkCount = 2;

        // Setup document repository
        _mockDocumentRepository.Setup(dr => dr.GetByFilePathAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        _mockDocumentRepository.Setup(dr => dr.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Document(documentId, Guid.Empty, "test.md", "Test", "hash", DocumentStatus.Pending));

        _mockDocumentRepository.Setup(dr => dr.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<DocumentStatus>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup chunk repository
        _mockChunkRepository.Setup(cr => cr.DeleteByDocumentIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockChunkRepository.Setup(cr => cr.AddRangeAsync(It.IsAny<IEnumerable<Chunk>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup chunking strategy
        var chunks = Enumerable.Range(0, chunkCount)
            .Select(i => new DocumentChunk(i, i * 100, (i + 1) * 100, $"chunk {i}"))
            .ToArray();

        var mockStrategy = new Mock<IChunkingStrategy>();
        mockStrategy.Setup(s => s.Mode).Returns(ChunkingMode.Fixed);
        mockStrategy.Setup(s => s.Split(It.IsAny<string>(), It.IsAny<ChunkingOptions>()))
            .Returns(chunks);

        _mockChunkingStrategyFactory.Setup(f => f.GetStrategy(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockStrategy.Object);

        // Setup token counter
        _mockTokenCounter.Setup(tc => tc.TruncateToTokenLimit(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((text, maxTokens) => (text, false));

        // Setup embedding service
        _mockEmbeddingService.Setup(es => es.MaxTokens).Returns(8191);
        _mockEmbeddingService.Setup(es => es.EmbedBatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(0, chunkCount)
                .Select(_ => new float[1536])
                .ToList());

        // Setup mediator
        _mockMediator.Setup(m => m.Publish(It.IsAny<DocumentIndexedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    #endregion
}
