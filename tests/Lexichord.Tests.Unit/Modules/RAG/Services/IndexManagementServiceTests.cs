// =============================================================================
// File: IndexManagementServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for IndexManagementService.
// Version: v0.4.7b
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Indexing;
using Lexichord.Modules.RAG.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="IndexManagementService"/>.
/// </summary>
public class IndexManagementServiceTests
{
    private readonly Mock<IDocumentRepository> _documentRepoMock;
    private readonly Mock<IChunkRepository> _chunkRepoMock;
    private readonly Mock<IDocumentIndexingPipeline> _pipelineMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<IndexManagementService>> _loggerMock;

    public IndexManagementServiceTests()
    {
        _documentRepoMock = new Mock<IDocumentRepository>();
        _chunkRepoMock = new Mock<IChunkRepository>();
        _pipelineMock = new Mock<IDocumentIndexingPipeline>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<IndexManagementService>>();
    }

    private IndexManagementService CreateSut() =>
        new IndexManagementService(
            _documentRepoMock.Object,
            _chunkRepoMock.Object,
            _pipelineMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullDocumentRepository_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>("documentRepository", () =>
            new IndexManagementService(
                null!,
                _chunkRepoMock.Object,
                _pipelineMock.Object,
                _mediatorMock.Object,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullChunkRepository_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>("chunkRepository", () =>
            new IndexManagementService(
                _documentRepoMock.Object,
                null!,
                _pipelineMock.Object,
                _mediatorMock.Object,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullPipeline_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>("indexingPipeline", () =>
            new IndexManagementService(
                _documentRepoMock.Object,
                _chunkRepoMock.Object,
                null!,
                _mediatorMock.Object,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>("mediator", () =>
            new IndexManagementService(
                _documentRepoMock.Object,
                _chunkRepoMock.Object,
                _pipelineMock.Object,
                null!,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>("logger", () =>
            new IndexManagementService(
                _documentRepoMock.Object,
                _chunkRepoMock.Object,
                _pipelineMock.Object,
                _mediatorMock.Object,
                null!));
    }

    #endregion

    #region ReindexDocumentAsync Tests

    [Fact]
    public async Task ReindexDocumentAsync_WithNonExistentDocument_ReturnsNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _documentRepoMock
            .Setup(r => r.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.ReindexDocumentAsync(documentId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(documentId, result.DocumentId);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(1, result.FailedCount);
    }

    [Fact]
    public async Task ReindexDocumentAsync_DeletesChunksBeforeReindexing()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = CreateTestDocument(documentId, "/project/test.md");
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, "Test content");
            document = document with { FilePath = tempFile };

            _documentRepoMock
                .Setup(r => r.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(document);

            _chunkRepoMock
                .Setup(r => r.DeleteByDocumentIdAsync(documentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);

            _pipelineMock
                .Setup(p => p.IndexDocumentAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ChunkingMode?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(IndexingResult.CreateSuccess(documentId, 3, TimeSpan.FromSeconds(1)));

            var sut = CreateSut();

            // Act
            await sut.ReindexDocumentAsync(documentId);

            // Assert
            _chunkRepoMock.Verify(
                r => r.DeleteByDocumentIdAsync(documentId, It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReindexDocumentAsync_OnSuccess_PublishesDocumentReindexedEvent()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, "Test content");
            var document = CreateTestDocument(documentId, tempFile);

            _documentRepoMock
                .Setup(r => r.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(document);

            _chunkRepoMock
                .Setup(r => r.DeleteByDocumentIdAsync(documentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            _pipelineMock
                .Setup(p => p.IndexDocumentAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ChunkingMode?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(IndexingResult.CreateSuccess(documentId, 3, TimeSpan.FromSeconds(1)));

            var sut = CreateSut();

            // Act
            var result = await sut.ReindexDocumentAsync(documentId);

            // Assert
            Assert.True(result.Success);
            _mediatorMock.Verify(
                m => m.Publish(
                    It.Is<DocumentReindexedEvent>(e =>
                        e.DocumentId == documentId &&
                        e.ChunkCount == 3),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReindexDocumentAsync_WhenFileNotFound_ReturnsFailure()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var nonExistentPath = "/nonexistent/file.md";
        var document = CreateTestDocument(documentId, nonExistentPath);

        _documentRepoMock
            .Setup(r => r.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _chunkRepoMock
            .Setup(r => r.DeleteByDocumentIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var sut = CreateSut();

        // Act
        var result = await sut.ReindexDocumentAsync(documentId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region RemoveFromIndexAsync Tests

    [Fact]
    public async Task RemoveFromIndexAsync_WithNonExistentDocument_ReturnsNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _documentRepoMock
            .Setup(r => r.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.RemoveFromIndexAsync(documentId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemoveFromIndexAsync_DeletesChunksBeforeDocument()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = CreateTestDocument(documentId, "/project/test.md");
        var deleteOrder = new List<string>();

        _documentRepoMock
            .Setup(r => r.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _chunkRepoMock
            .Setup(r => r.DeleteByDocumentIdAsync(documentId, It.IsAny<CancellationToken>()))
            .Callback(() => deleteOrder.Add("chunks"))
            .ReturnsAsync(5);

        _documentRepoMock
            .Setup(r => r.DeleteAsync(documentId, It.IsAny<CancellationToken>()))
            .Callback(() => deleteOrder.Add("document"))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        await sut.RemoveFromIndexAsync(documentId);

        // Assert
        Assert.Equal(new[] { "chunks", "document" }, deleteOrder);
    }

    [Fact]
    public async Task RemoveFromIndexAsync_OnSuccess_PublishesDocumentRemovedFromIndexEvent()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var filePath = "/project/test.md";
        var document = CreateTestDocument(documentId, filePath);

        _documentRepoMock
            .Setup(r => r.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _chunkRepoMock
            .Setup(r => r.DeleteByDocumentIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        _documentRepoMock
            .Setup(r => r.DeleteAsync(documentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.RemoveFromIndexAsync(documentId);

        // Assert
        Assert.True(result.Success);
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<DocumentRemovedFromIndexEvent>(e =>
                    e.DocumentId == documentId &&
                    e.FilePath == filePath),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveFromIndexAsync_OnSuccess_ReturnsSuccessWithChunkCount()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = CreateTestDocument(documentId, "/project/test.md");

        _documentRepoMock
            .Setup(r => r.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _chunkRepoMock
            .Setup(r => r.DeleteByDocumentIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        _documentRepoMock
            .Setup(r => r.DeleteAsync(documentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // Act
        var result = await sut.RemoveFromIndexAsync(documentId);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("5 chunks", result.Message);
    }

    #endregion

    #region ReindexAllAsync Tests

    [Fact]
    public async Task ReindexAllAsync_WithNoDocuments_ReturnsEmptyResult()
    {
        // Arrange
        SetupEmptyDocumentRepository();

        var sut = CreateSut();

        // Act
        var result = await sut.ReindexAllAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task ReindexAllAsync_ReportsProgressCorrectly()
    {
        // Arrange
        var documents = CreateTestDocuments(10);
        SetupDocumentRepositoryWithDocuments(documents);

        var reportedProgress = new List<int>();
        var progress = new Progress<int>(p => reportedProgress.Add(p));

        // Setup each document to succeed
        foreach (var doc in documents)
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "content");
            var docWithPath = doc with { FilePath = tempFile };

            _documentRepoMock
                .Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(docWithPath);

            _pipelineMock
                .Setup(p => p.IndexDocumentAsync(
                    It.Is<string>(s => s == tempFile),
                    It.IsAny<string>(),
                    It.IsAny<ChunkingMode?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(IndexingResult.CreateSuccess(doc.Id, 1, TimeSpan.FromMilliseconds(10)));
        }

        var sut = CreateSut();

        // Act
        await sut.ReindexAllAsync(progress);

        // Assert - should report progress from 10% to 100%
        Assert.NotEmpty(reportedProgress);
        Assert.Contains(100, reportedProgress);

        // Cleanup temp files
        foreach (var doc in documents)
        {
            var docId = doc.Id;
            _documentRepoMock
                .Setup(r => r.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Document?)null);
        }
    }

    [Fact]
    public async Task ReindexAllAsync_WithPartialFailures_ContinuesAndReportsResults()
    {
        // Arrange
        var doc1 = CreateTestDocument(Guid.NewGuid(), "/project/doc1.md");
        var doc2 = CreateTestDocument(Guid.NewGuid(), "/nonexistent/doc2.md");

        var documents = new[] { doc1, doc2 };
        SetupDocumentRepositoryWithDocuments(documents);

        // doc1 succeeds
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "content");
        var doc1WithPath = doc1 with { FilePath = tempFile };

        _documentRepoMock
            .Setup(r => r.GetByIdAsync(doc1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc1WithPath);

        _pipelineMock
            .Setup(p => p.IndexDocumentAsync(
                It.Is<string>(s => s == tempFile),
                It.IsAny<string>(),
                It.IsAny<ChunkingMode?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IndexingResult.CreateSuccess(doc1.Id, 1, TimeSpan.FromMilliseconds(10)));

        // doc2 fails (file not found)
        _documentRepoMock
            .Setup(r => r.GetByIdAsync(doc2.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc2);

        var sut = CreateSut();

        // Act
        var result = await sut.ReindexAllAsync();

        // Assert
        Assert.False(result.Success); // Not all succeeded
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(1, result.FailedCount);

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public async Task ReindexAllAsync_OnCompletion_PublishesAllDocumentsReindexedEvent()
    {
        // Arrange
        var document = CreateTestDocument(Guid.NewGuid(), "/project/test.md");
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, "content");
            var docWithPath = document with { FilePath = tempFile };

            SetupDocumentRepositoryWithDocuments(new[] { document });

            _documentRepoMock
                .Setup(r => r.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(docWithPath);

            _pipelineMock
                .Setup(p => p.IndexDocumentAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<ChunkingMode?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(IndexingResult.CreateSuccess(document.Id, 1, TimeSpan.FromMilliseconds(10)));

            var sut = CreateSut();

            // Act
            await sut.ReindexAllAsync();

            // Assert
            _mediatorMock.Verify(
                m => m.Publish(
                    It.IsAny<AllDocumentsReindexedEvent>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ReindexDocumentAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _documentRepoMock
            .Setup(r => r.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var sut = CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => sut.ReindexDocumentAsync(documentId, cts.Token));
    }

    [Fact]
    public async Task RemoveFromIndexAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _documentRepoMock
            .Setup(r => r.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var sut = CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => sut.RemoveFromIndexAsync(documentId, cts.Token));
    }

    #endregion

    #region Helper Methods

    private static Document CreateTestDocument(Guid id, string filePath) =>
        new Document(
            Id: id,
            ProjectId: Guid.Empty,
            FilePath: filePath,
            Title: Path.GetFileName(filePath),
            Hash: "testhash123",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow,
            FailureReason: null);

    private static List<Document> CreateTestDocuments(int count) =>
        Enumerable.Range(0, count)
            .Select(i => CreateTestDocument(Guid.NewGuid(), $"/project/doc{i}.md"))
            .ToList();

    private void SetupEmptyDocumentRepository()
    {
        foreach (var status in new[] { DocumentStatus.Indexed, DocumentStatus.Stale, DocumentStatus.Failed, DocumentStatus.Pending })
        {
            _documentRepoMock
                .Setup(r => r.GetByStatusAsync(status, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Document>());
        }
    }

    private void SetupDocumentRepositoryWithDocuments(IEnumerable<Document> documents)
    {
        var docList = documents.ToList();

        // Only return docs for the Indexed status, others empty
        _documentRepoMock
            .Setup(r => r.GetByStatusAsync(DocumentStatus.Indexed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(docList);

        foreach (var status in new[] { DocumentStatus.Stale, DocumentStatus.Failed, DocumentStatus.Pending })
        {
            _documentRepoMock
                .Setup(r => r.GetByStatusAsync(status, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Document>());
        }

        // Default chunk deletion returns 0
        _chunkRepoMock
            .Setup(r => r.DeleteByDocumentIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
    }

    #endregion
}
