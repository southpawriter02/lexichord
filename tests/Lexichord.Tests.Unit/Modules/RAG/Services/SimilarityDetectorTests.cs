// =============================================================================
// File: SimilarityDetectorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SimilarityDetector service.
// =============================================================================
// VERSION: v0.5.9a (Similarity Detection Infrastructure)
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="SimilarityDetector"/>.
/// </summary>
public sealed class SimilarityDetectorTests
{
    private readonly Mock<IChunkRepository> _chunkRepoMock;
    private readonly Mock<IDocumentRepository> _documentRepoMock;
    private readonly Mock<IOptions<SimilarityDetectorOptions>> _optionsMock;
    private readonly Mock<ILogger<SimilarityDetector>> _loggerMock;
    private readonly SimilarityDetector _sut;

    public SimilarityDetectorTests()
    {
        _chunkRepoMock = new Mock<IChunkRepository>();
        _documentRepoMock = new Mock<IDocumentRepository>();
        _optionsMock = new Mock<IOptions<SimilarityDetectorOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(SimilarityDetectorOptions.Default);
        _loggerMock = new Mock<ILogger<SimilarityDetector>>();

        _sut = new SimilarityDetector(
            _chunkRepoMock.Object,
            _documentRepoMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);
    }

    private static Chunk CreateTestChunk(
        Guid? id = null,
        Guid? documentId = null,
        float[]? embedding = null,
        int chunkIndex = 0,
        string? content = null,
        bool useNullEmbedding = false)
    {
        // LOGIC: If useNullEmbedding is true, use null; otherwise use provided or default.
        var actualEmbedding = useNullEmbedding ? null : (embedding ?? new float[] { 0.1f, 0.2f, 0.3f });

        return new Chunk(
            Id: id ?? Guid.NewGuid(),
            DocumentId: documentId ?? Guid.NewGuid(),
            Content: content ?? $"Test content for chunk {chunkIndex}",
            Embedding: actualEmbedding,
            ChunkIndex: chunkIndex,
            StartOffset: chunkIndex * 100,
            EndOffset: (chunkIndex + 1) * 100);
    }

    private static ChunkSearchResult CreateSearchResult(
        Guid? chunkId = null,
        Guid? documentId = null,
        int chunkIndex = 0,
        string? content = null,
        double score = 0.97)
    {
        var chunk = CreateTestChunk(
            id: chunkId,
            documentId: documentId,
            chunkIndex: chunkIndex,
            content: content);
        return new ChunkSearchResult(chunk, score);
    }

    private static Document CreateTestDocument(Guid? id = null, string? filePath = null)
    {
        return new Document(
            Id: id ?? Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            FilePath: filePath ?? "/test/document.md",
            Title: "Test Document",
            Hash: "abc123",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow,
            FailureReason: null);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullChunkRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new SimilarityDetector(
                null!,
                _documentRepoMock.Object,
                _optionsMock.Object,
                _loggerMock.Object));

        Assert.Equal("chunkRepository", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullDocumentRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new SimilarityDetector(
                _chunkRepoMock.Object,
                null!,
                _optionsMock.Object,
                _loggerMock.Object));

        Assert.Equal("documentRepository", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new SimilarityDetector(
                _chunkRepoMock.Object,
                _documentRepoMock.Object,
                null!,
                _loggerMock.Object));

        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new SimilarityDetector(
                _chunkRepoMock.Object,
                _documentRepoMock.Object,
                _optionsMock.Object,
                null!));

        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region FindSimilarAsync Tests

    [Fact]
    public async Task FindSimilarAsync_WithNullChunk_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.FindSimilarAsync(null!));
    }

    [Fact]
    public async Task FindSimilarAsync_WithNullEmbedding_ThrowsArgumentException()
    {
        var chunk = CreateTestChunk(useNullEmbedding: true);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.FindSimilarAsync(chunk));

        Assert.Contains("embedding", ex.Message);
    }

    [Fact]
    public async Task FindSimilarAsync_QueriesRepositoryWithCorrectParameters()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var options = new SimilarityDetectorOptions
        {
            SimilarityThreshold = 0.90,
            MaxResultsPerChunk = 3
        };

        _chunkRepoMock
            .Setup(r => r.SearchSimilarAsync(
                chunk.Embedding!,
                options.MaxResultsPerChunk + 5,
                options.SimilarityThreshold,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ChunkSearchResult>());

        // Act
        await _sut.FindSimilarAsync(chunk, options);

        // Assert
        _chunkRepoMock.Verify(
            r => r.SearchSimilarAsync(
                chunk.Embedding!,
                options.MaxResultsPerChunk + 5,
                options.SimilarityThreshold,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FindSimilarAsync_FiltersSelfMatches()
    {
        // Arrange
        var chunkId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var chunk = CreateTestChunk(id: chunkId, documentId: docId);

        var searchResults = new[]
        {
            CreateSearchResult(chunkId: chunkId, documentId: docId, content: "Self content", score: 1.0), // Self match
            CreateSearchResult(documentId: docId, chunkIndex: 1, content: "Other content", score: 0.96)
        };

        _chunkRepoMock
            .Setup(r => r.SearchSimilarAsync(
                It.IsAny<float[]>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _documentRepoMock
            .Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestDocument(id: docId));

        // Act
        var results = await _sut.FindSimilarAsync(chunk);

        // Assert
        Assert.Single(results);
        Assert.NotEqual(chunkId, results[0].MatchedChunkId);
    }

    [Fact]
    public async Task FindSimilarAsync_ExcludesSameDocumentWhenConfigured()
    {
        // Arrange
        var chunkId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var otherDocId = Guid.NewGuid();
        var chunk = CreateTestChunk(id: chunkId, documentId: docId);

        var searchResults = new[]
        {
            CreateSearchResult(documentId: docId, chunkIndex: 1, content: "Same doc", score: 0.98),
            CreateSearchResult(documentId: otherDocId, chunkIndex: 0, content: "Other doc", score: 0.96)
        };

        var options = new SimilarityDetectorOptions { ExcludeSameDocument = true };

        _chunkRepoMock
            .Setup(r => r.SearchSimilarAsync(
                It.IsAny<float[]>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _documentRepoMock
            .Setup(r => r.GetByIdAsync(otherDocId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestDocument(id: otherDocId));

        // Act
        var results = await _sut.FindSimilarAsync(chunk, options);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsCrossDocumentMatch);
    }

    [Fact]
    public async Task FindSimilarAsync_LimitsResultsToMaxResultsPerChunk()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var options = new SimilarityDetectorOptions { MaxResultsPerChunk = 2 };
        var docId = Guid.NewGuid();

        var searchResults = Enumerable.Range(0, 5)
            .Select(i => CreateSearchResult(documentId: docId, chunkIndex: i, content: $"Content {i}", score: 0.99 - (i * 0.01)))
            .ToArray();

        _chunkRepoMock
            .Setup(r => r.SearchSimilarAsync(
                It.IsAny<float[]>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _documentRepoMock
            .Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestDocument(id: docId));

        // Act
        var results = await _sut.FindSimilarAsync(chunk, options);

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task FindSimilarAsync_IncludesDocumentPathInResults()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var matchDocId = Guid.NewGuid();
        var expectedPath = "/docs/matched.md";

        var searchResults = new[]
        {
            CreateSearchResult(documentId: matchDocId, chunkIndex: 0, content: "Match content", score: 0.97)
        };

        _chunkRepoMock
            .Setup(r => r.SearchSimilarAsync(
                It.IsAny<float[]>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _documentRepoMock
            .Setup(r => r.GetByIdAsync(matchDocId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestDocument(id: matchDocId, filePath: expectedPath));

        // Act
        var results = await _sut.FindSimilarAsync(chunk);

        // Assert
        Assert.Single(results);
        Assert.Equal(expectedPath, results[0].MatchedDocumentPath);
    }

    #endregion

    #region FindSimilarBatchAsync Tests

    [Fact]
    public async Task FindSimilarBatchAsync_WithNullChunks_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.FindSimilarBatchAsync(null!));
    }

    [Fact]
    public async Task FindSimilarBatchAsync_WithEmptyCollection_ReturnsEmpty()
    {
        // Act
        var results = await _sut.FindSimilarBatchAsync(Array.Empty<Chunk>());

        // Assert
        Assert.Empty(results);
        _chunkRepoMock.Verify(
            r => r.SearchSimilarAsync(
                It.IsAny<float[]>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task FindSimilarBatchAsync_SkipsChunksWithoutEmbeddings()
    {
        // Arrange
        var validChunk = CreateTestChunk();
        var invalidChunk = CreateTestChunk(useNullEmbedding: true);

        _chunkRepoMock
            .Setup(r => r.SearchSimilarAsync(
                It.IsAny<float[]>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ChunkSearchResult>());

        // Act
        var results = await _sut.FindSimilarBatchAsync(new[] { validChunk, invalidChunk });

        // Assert - only valid chunk should trigger search
        _chunkRepoMock.Verify(
            r => r.SearchSimilarAsync(
                It.IsAny<float[]>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FindSimilarBatchAsync_AggregatesResultsFromAllChunks()
    {
        // Arrange
        var chunk1 = CreateTestChunk();
        var chunk2 = CreateTestChunk();
        var docId = Guid.NewGuid();

        _chunkRepoMock
            .Setup(r => r.SearchSimilarAsync(
                It.IsAny<float[]>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                CreateSearchResult(documentId: docId, chunkIndex: 0, content: "Match", score: 0.97)
            });

        _documentRepoMock
            .Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestDocument(id: docId));

        // Act
        var results = await _sut.FindSimilarBatchAsync(new[] { chunk1, chunk2 });

        // Assert - one match per chunk
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task FindSimilarBatchAsync_RespectsCancellationToken()
    {
        // Arrange
        var chunks = Enumerable.Range(0, 100).Select(_ => CreateTestChunk()).ToList();
        var cts = new CancellationTokenSource();

        _chunkRepoMock
            .Setup(r => r.SearchSimilarAsync(
                It.IsAny<float[]>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ChunkSearchResult>());

        // Cancel after first batch
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.FindSimilarBatchAsync(chunks, cancellationToken: cts.Token));
    }

    #endregion
}
