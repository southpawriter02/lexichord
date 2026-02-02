// =============================================================================
// File: IndexStatusServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for IndexStatusService.
// Version: v0.4.7a
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="IndexStatusService"/>.
/// </summary>
/// <remarks>
/// Tests cover document retrieval, statistics calculation, stale detection,
/// and error handling.
/// </remarks>
public class IndexStatusServiceTests
{
    private readonly Mock<IDocumentRepository> _documentRepoMock;
    private readonly Mock<IChunkRepository> _chunkRepoMock;
    private readonly Mock<IFileHashService> _fileHashServiceMock;
    private readonly Mock<ILogger<IndexStatusService>> _loggerMock;
    private readonly IndexStatusService _sut;

    public IndexStatusServiceTests()
    {
        _documentRepoMock = new Mock<IDocumentRepository>();
        _chunkRepoMock = new Mock<IChunkRepository>();
        _fileHashServiceMock = new Mock<IFileHashService>();
        _loggerMock = new Mock<ILogger<IndexStatusService>>();

        _sut = new IndexStatusService(
            _documentRepoMock.Object,
            _chunkRepoMock.Object,
            _fileHashServiceMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullDocumentRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new IndexStatusService(
                null!,
                _chunkRepoMock.Object,
                _fileHashServiceMock.Object,
                _loggerMock.Object));

        Assert.Equal("documentRepository", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullChunkRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new IndexStatusService(
                _documentRepoMock.Object,
                null!,
                _fileHashServiceMock.Object,
                _loggerMock.Object));

        Assert.Equal("chunkRepository", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullFileHashService_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new IndexStatusService(
                _documentRepoMock.Object,
                _chunkRepoMock.Object,
                null!,
                _loggerMock.Object));

        Assert.Equal("fileHashService", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new IndexStatusService(
                _documentRepoMock.Object,
                _chunkRepoMock.Object,
                _fileHashServiceMock.Object,
                null!));

        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region GetAllDocumentsAsync Tests

    [Fact]
    public async Task GetAllDocumentsAsync_WithNoDocuments_ReturnsEmptyList()
    {
        // Arrange
        SetupEmptyDocumentRepository();

        // Act
        var result = await _sut.GetAllDocumentsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllDocumentsAsync_WithMultipleStatuses_ReturnsAllDocuments()
    {
        // Arrange
        var pendingDoc = CreateTestDocument(DocumentStatus.Pending, "pending.txt");
        var indexedDoc = CreateTestDocument(DocumentStatus.Indexed, "indexed.txt");
        var failedDoc = CreateTestDocument(DocumentStatus.Failed, "failed.txt");

        _documentRepoMock.Setup(r => r.GetByStatusAsync(DocumentStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { pendingDoc });
        _documentRepoMock.Setup(r => r.GetByStatusAsync(DocumentStatus.Indexed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { indexedDoc });
        _documentRepoMock.Setup(r => r.GetByStatusAsync(DocumentStatus.Failed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { failedDoc });
        _documentRepoMock.Setup(r => r.GetByStatusAsync(DocumentStatus.Indexing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Document>());
        _documentRepoMock.Setup(r => r.GetByStatusAsync(DocumentStatus.Stale, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Document>());

        SetupChunkRepositoryForDocument(pendingDoc.Id, 0);
        SetupChunkRepositoryForDocument(indexedDoc.Id, 5);
        SetupChunkRepositoryForDocument(failedDoc.Id, 0);

        // Act
        var result = await _sut.GetAllDocumentsAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, d => d.FileName == "pending.txt");
        Assert.Contains(result, d => d.FileName == "indexed.txt");
        Assert.Contains(result, d => d.FileName == "failed.txt");
    }

    [Fact]
    public async Task GetAllDocumentsAsync_MapsStatusCorrectly()
    {
        // Arrange
        var indexedDoc = CreateTestDocument(DocumentStatus.Indexed, "test.txt");
        SetupDocumentRepositoryWithDocument(indexedDoc);
        SetupChunkRepositoryForDocument(indexedDoc.Id, 10);

        // Act
        var result = await _sut.GetAllDocumentsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(IndexingStatus.Indexed, result[0].Status);
    }

    [Fact]
    public async Task GetAllDocumentsAsync_IncludesChunkCount()
    {
        // Arrange
        var doc = CreateTestDocument(DocumentStatus.Indexed, "test.txt");
        SetupDocumentRepositoryWithDocument(doc);
        SetupChunkRepositoryForDocument(doc.Id, 15);

        // Act
        var result = await _sut.GetAllDocumentsAsync();

        // Assert
        Assert.Equal(15, result[0].ChunkCount);
    }

    [Fact]
    public async Task GetAllDocumentsAsync_CalculatesEstimatedSize()
    {
        // Arrange
        var doc = CreateTestDocument(DocumentStatus.Indexed, "test.txt");
        SetupDocumentRepositoryWithDocument(doc);
        SetupChunkRepositoryForDocument(doc.Id, 5);

        // Act
        var result = await _sut.GetAllDocumentsAsync();

        // Assert
        // 5 chunks * 2048 bytes per chunk = 10240 bytes
        Assert.Equal(5 * 2048, result[0].EstimatedSizeBytes);
    }

    #endregion

    #region GetDocumentStatusAsync Tests

    [Fact]
    public async Task GetDocumentStatusAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _documentRepoMock.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _sut.GetDocumentStatusAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetDocumentStatusAsync_WithExistingDocument_ReturnsStatus()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var doc = CreateTestDocument(DocumentStatus.Indexed, "test.txt", docId);
        _documentRepoMock.Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);
        SetupChunkRepositoryForDocument(docId, 3);

        // Act
        var result = await _sut.GetDocumentStatusAsync(docId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(docId, result.Id);
        Assert.Equal(IndexingStatus.Indexed, result.Status);
        Assert.Equal(3, result.ChunkCount);
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_WithNoDocuments_ReturnsEmptyStatistics()
    {
        // Arrange
        SetupEmptyDocumentRepository();

        // Act
        var result = await _sut.GetStatisticsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.DocumentCount);
        Assert.Equal(0, result.ChunkCount);
        Assert.Equal(0, result.StorageSizeBytes);
    }

    [Fact]
    public async Task GetStatisticsAsync_CalculatesAggregates()
    {
        // Arrange
        var doc1 = CreateTestDocument(DocumentStatus.Indexed, "a.txt");
        var doc2 = CreateTestDocument(DocumentStatus.Indexed, "b.txt");
        
        _documentRepoMock.Setup(r => r.GetByStatusAsync(DocumentStatus.Indexed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { doc1, doc2 });
        _documentRepoMock.Setup(r => r.GetByStatusAsync(It.Is<DocumentStatus>(s => s != DocumentStatus.Indexed), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Document>());
        
        SetupChunkRepositoryForDocument(doc1.Id, 10);
        SetupChunkRepositoryForDocument(doc2.Id, 20);

        // Act
        var result = await _sut.GetStatisticsAsync();

        // Assert
        Assert.Equal(2, result.DocumentCount);
        Assert.Equal(30, result.ChunkCount);
        Assert.Equal(30 * 2048, result.StorageSizeBytes);
    }

    [Fact]
    public async Task GetStatisticsAsync_IncludesStatusCounts()
    {
        // Arrange
        var indexedDoc = CreateTestDocument(DocumentStatus.Indexed, "indexed.txt");
        var failedDoc = CreateTestDocument(DocumentStatus.Failed, "failed.txt");
        
        _documentRepoMock.Setup(r => r.GetByStatusAsync(DocumentStatus.Indexed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { indexedDoc });
        _documentRepoMock.Setup(r => r.GetByStatusAsync(DocumentStatus.Failed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { failedDoc });
        _documentRepoMock.Setup(r => r.GetByStatusAsync(It.Is<DocumentStatus>(s => s != DocumentStatus.Indexed && s != DocumentStatus.Failed), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Document>());
        
        SetupChunkRepositoryForDocument(indexedDoc.Id, 5);
        SetupChunkRepositoryForDocument(failedDoc.Id, 0);

        // Act
        var result = await _sut.GetStatisticsAsync();

        // Assert
        Assert.Equal(1, result.StatusCounts[IndexingStatus.Indexed]);
        Assert.Equal(1, result.StatusCounts[IndexingStatus.Failed]);
    }

    #endregion

    #region RefreshStaleStatusAsync Tests

    [Fact]
    public async Task RefreshStaleStatusAsync_WithNoIndexedDocuments_DoesNothing()
    {
        // Arrange
        _documentRepoMock.Setup(r => r.GetByStatusAsync(DocumentStatus.Indexed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Document>());

        // Act
        await _sut.RefreshStaleStatusAsync();

        // Assert
        _documentRepoMock.Verify(
            r => r.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<DocumentStatus>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshStaleStatusAsync_WithChangedFile_MarksAsStale()
    {
        // Arrange
        var doc = CreateTestDocument(DocumentStatus.Indexed, "test.txt");
        doc = doc with { Hash = "oldhash123" };
        
        _documentRepoMock.Setup(r => r.GetByStatusAsync(DocumentStatus.Indexed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { doc });
        
        _fileHashServiceMock.Setup(f => f.GetMetadata(doc.FilePath))
            .Returns(new FileMetadata { Exists = true, Size = 1000, LastModified = DateTimeOffset.UtcNow });
        
        _fileHashServiceMock.Setup(f => f.HasChangedAsync(
            doc.FilePath,
            doc.Hash,
            It.IsAny<long>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.RefreshStaleStatusAsync();

        // Assert
        _documentRepoMock.Verify(
            r => r.UpdateStatusAsync(doc.Id, DocumentStatus.Stale, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshStaleStatusAsync_WithUnchangedFile_DoesNotMark()
    {
        // Arrange
        var doc = CreateTestDocument(DocumentStatus.Indexed, "test.txt");
        doc = doc with { Hash = "currenthash" };
        
        _documentRepoMock.Setup(r => r.GetByStatusAsync(DocumentStatus.Indexed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { doc });
        
        _fileHashServiceMock.Setup(f => f.GetMetadata(doc.FilePath))
            .Returns(new FileMetadata { Exists = true, Size = 1000, LastModified = DateTimeOffset.UtcNow });
        
        _fileHashServiceMock.Setup(f => f.HasChangedAsync(
            doc.FilePath,
            doc.Hash,
            It.IsAny<long>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _sut.RefreshStaleStatusAsync();

        // Assert
        _documentRepoMock.Verify(
            r => r.UpdateStatusAsync(It.IsAny<Guid>(), DocumentStatus.Stale, It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshStaleStatusAsync_WithMissingFile_SkipsDocument()
    {
        // Arrange
        var doc = CreateTestDocument(DocumentStatus.Indexed, "missing.txt");
        doc = doc with { Hash = "somehash" };
        
        _documentRepoMock.Setup(r => r.GetByStatusAsync(DocumentStatus.Indexed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { doc });
        
        _fileHashServiceMock.Setup(f => f.GetMetadata(doc.FilePath))
            .Returns(FileMetadata.NotFound);

        // Act
        await _sut.RefreshStaleStatusAsync();

        // Assert
        _documentRepoMock.Verify(
            r => r.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<DocumentStatus>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshStaleStatusAsync_WithEmptyHash_SkipsDocument()
    {
        // Arrange - Create document with an empty hash to trigger skip logic
        var doc = CreateTestDocument(DocumentStatus.Indexed, "nohash.txt", hash: string.Empty);
        
        _documentRepoMock.Setup(r => r.GetByStatusAsync(DocumentStatus.Indexed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { doc });

        // Act
        await _sut.RefreshStaleStatusAsync();

        // Assert - GetMetadata should never be called because empty hash triggers skip
        _fileHashServiceMock.Verify(
            f => f.GetMetadata(It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region Helper Methods

    private Document CreateTestDocument(
        DocumentStatus status, 
        string filename, 
        Guid? id = null,
        DateTime? indexedAt = null,
        string? hash = "testhash123")
    {
        return new Document(
            Id: id ?? Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            FilePath: $"/test/path/{filename}",
            Title: filename,
            Hash: hash ?? string.Empty,
            Status: status,
            IndexedAt: indexedAt ?? (status == DocumentStatus.Indexed ? DateTime.UtcNow : null),
            FailureReason: null);
    }

    private void SetupEmptyDocumentRepository()
    {
        _documentRepoMock.Setup(r => r.GetByStatusAsync(It.IsAny<DocumentStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Document>());
    }

    private void SetupDocumentRepositoryWithDocument(Document doc)
    {
        _documentRepoMock.Setup(r => r.GetByStatusAsync(doc.Status, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { doc });
        _documentRepoMock.Setup(r => r.GetByStatusAsync(It.Is<DocumentStatus>(s => s != doc.Status), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Document>());
    }

    private void SetupChunkRepositoryForDocument(Guid documentId, int chunkCount)
    {
        var chunks = Enumerable.Range(0, chunkCount)
            .Select(i => new Chunk(
                Id: Guid.NewGuid(),
                DocumentId: documentId,
                Content: $"Chunk {i}",
                Embedding: new float[1536],
                ChunkIndex: i,
                StartOffset: i * 100,
                EndOffset: (i + 1) * 100))
            .ToList();

        _chunkRepoMock.Setup(r => r.GetByDocumentIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);
    }

    #endregion
}
