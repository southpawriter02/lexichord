// =============================================================================
// File: ContextExpansionServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ContextExpansionService.
// =============================================================================
// VERSION: v0.5.3a (Context Expansion Service)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Events;
using Lexichord.Modules.RAG.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="ContextExpansionService"/>.
/// </summary>
public sealed class ContextExpansionServiceTests
{
    private readonly Mock<IChunkRepository> _chunkRepoMock;
    private readonly Mock<IHeadingHierarchyService> _headingServiceMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<ContextExpansionService>> _loggerMock;
    private readonly ContextExpansionService _sut;

    public ContextExpansionServiceTests()
    {
        _chunkRepoMock = new Mock<IChunkRepository>();
        _headingServiceMock = new Mock<IHeadingHierarchyService>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<ContextExpansionService>>();

        _sut = new ContextExpansionService(
            _chunkRepoMock.Object,
            _headingServiceMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    private static Chunk CreateTestChunk(
        int chunkIndex = 5,
        Guid? id = null,
        Guid? documentId = null)
    {
        return new Chunk(
            Id: id ?? Guid.NewGuid(),
            DocumentId: documentId ?? Guid.NewGuid(),
            Content: $"Chunk content {chunkIndex}",
            Embedding: null,
            ChunkIndex: chunkIndex,
            StartOffset: chunkIndex * 100,
            EndOffset: (chunkIndex + 1) * 100);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullChunkRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContextExpansionService(
                null!,
                _headingServiceMock.Object,
                _mediatorMock.Object,
                _loggerMock.Object));

        Assert.Equal("chunkRepository", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullHeadingService_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContextExpansionService(
                _chunkRepoMock.Object,
                null!,
                _mediatorMock.Object,
                _loggerMock.Object));

        Assert.Equal("headingService", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContextExpansionService(
                _chunkRepoMock.Object,
                _headingServiceMock.Object,
                null!,
                _loggerMock.Object));

        Assert.Equal("mediator", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContextExpansionService(
                _chunkRepoMock.Object,
                _headingServiceMock.Object,
                _mediatorMock.Object,
                null!));

        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region ExpandAsync Tests

    [Fact]
    public async Task ExpandAsync_WithNullChunk_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.ExpandAsync(null!));
    }

    [Fact]
    public async Task ExpandAsync_QueriesSiblingsWithValidatedOptions()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var chunk = CreateTestChunk(chunkIndex: 5, documentId: docId);
        var options = new ContextOptions(PrecedingChunks: 2, FollowingChunks: 3);

        _chunkRepoMock
            .Setup(r => r.GetSiblingsAsync(docId, 5, 2, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Chunk>());
        _headingServiceMock
            .Setup(h => h.GetBreadcrumbAsync(docId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        await _sut.ExpandAsync(chunk, options);

        // Assert
        _chunkRepoMock.Verify(
            r => r.GetSiblingsAsync(docId, 5, 2, 3, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExpandAsync_PartitionsSiblingsCorrectly()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var chunk = CreateTestChunk(chunkIndex: 5, documentId: docId);
        var before1 = CreateTestChunk(chunkIndex: 3, documentId: docId);
        var before2 = CreateTestChunk(chunkIndex: 4, documentId: docId);
        var after1 = CreateTestChunk(chunkIndex: 6, documentId: docId);

        _chunkRepoMock
            .Setup(r => r.GetSiblingsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Chunk> { before1, before2, after1 });
        _headingServiceMock
            .Setup(h => h.GetBreadcrumbAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await _sut.ExpandAsync(chunk);

        // Assert
        Assert.Equal(2, result.Before.Count);
        Assert.Single(result.After);
        Assert.Equal(3, result.Before[0].ChunkIndex);
        Assert.Equal(4, result.Before[1].ChunkIndex);
        Assert.Equal(6, result.After[0].ChunkIndex);
    }

    [Fact]
    public async Task ExpandAsync_IncludesBreadcrumbWhenEnabled()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var chunk = CreateTestChunk(chunkIndex: 5, documentId: docId);
        var breadcrumb = new[] { "Chapter 1", "Section 1" };

        _chunkRepoMock
            .Setup(r => r.GetSiblingsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Chunk>());
        _headingServiceMock
            .Setup(h => h.GetBreadcrumbAsync(docId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(breadcrumb);

        // Act
        var result = await _sut.ExpandAsync(chunk, new ContextOptions(IncludeHeadings: true));

        // Assert
        Assert.True(result.HasBreadcrumb);
        Assert.Equal(2, result.HeadingBreadcrumb.Count);
        Assert.Equal("Section 1", result.ParentHeading);
    }

    [Fact]
    public async Task ExpandAsync_SkipsBreadcrumbWhenDisabled()
    {
        // Arrange
        var chunk = CreateTestChunk();

        _chunkRepoMock
            .Setup(r => r.GetSiblingsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Chunk>());

        // Act
        var result = await _sut.ExpandAsync(chunk, new ContextOptions(IncludeHeadings: false));

        // Assert
        Assert.False(result.HasBreadcrumb);
        _headingServiceMock.Verify(
            h => h.GetBreadcrumbAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExpandAsync_PublishesContextExpandedEvent()
    {
        // Arrange
        var chunk = CreateTestChunk();
        ContextExpandedEvent? publishedEvent = null;

        _chunkRepoMock
            .Setup(r => r.GetSiblingsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Chunk>());
        _headingServiceMock
            .Setup(h => h.GetBreadcrumbAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<ContextExpandedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((e, _) => publishedEvent = (ContextExpandedEvent)e)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ExpandAsync(chunk);

        // Assert
        Assert.NotNull(publishedEvent);
        Assert.Equal(chunk.Id, publishedEvent.ChunkId);
        Assert.Equal(chunk.DocumentId, publishedEvent.DocumentId);
        Assert.False(publishedEvent.FromCache);
    }

    [Fact]
    public async Task ExpandAsync_GracefullyDegradesBreadcrumbOnError()
    {
        // Arrange
        var chunk = CreateTestChunk();

        _chunkRepoMock
            .Setup(r => r.GetSiblingsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Chunk>());
        _headingServiceMock
            .Setup(h => h.GetBreadcrumbAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Heading parse failed"));

        // Act
        var result = await _sut.ExpandAsync(chunk);

        // Assert - should not throw, but breadcrumb should be empty
        Assert.False(result.HasBreadcrumb);
        Assert.Null(result.ParentHeading);
    }

    #endregion

    #region Caching Tests

    [Fact]
    public async Task ExpandAsync_CachesResult()
    {
        // Arrange
        var chunk = CreateTestChunk();

        _chunkRepoMock
            .Setup(r => r.GetSiblingsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Chunk>());
        _headingServiceMock
            .Setup(h => h.GetBreadcrumbAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        // Act - first call
        await _sut.ExpandAsync(chunk);
        // Act - second call
        await _sut.ExpandAsync(chunk);

        // Assert - repository should only be called once
        _chunkRepoMock.Verify(
            r => r.GetSiblingsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExpandAsync_CacheHit_PublishesFromCacheTrue()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var events = new List<ContextExpandedEvent>();

        _chunkRepoMock
            .Setup(r => r.GetSiblingsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Chunk>());
        _headingServiceMock
            .Setup(h => h.GetBreadcrumbAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<ContextExpandedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((e, _) => events.Add((ContextExpandedEvent)e))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ExpandAsync(chunk);
        await _sut.ExpandAsync(chunk);

        // Assert
        Assert.Equal(2, events.Count);
        Assert.False(events[0].FromCache);
        Assert.True(events[1].FromCache);
        Assert.Equal(0, events[1].ElapsedMilliseconds);
    }

    [Fact]
    public async Task InvalidateCache_RemovesCachedEntries()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var chunk = CreateTestChunk(documentId: docId);

        _chunkRepoMock
            .Setup(r => r.GetSiblingsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Chunk>());
        _headingServiceMock
            .Setup(h => h.GetBreadcrumbAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        await _sut.ExpandAsync(chunk);

        // Act
        _sut.InvalidateCache(docId);
        await _sut.ExpandAsync(chunk);

        // Assert - second call should go to repository again
        _chunkRepoMock.Verify(
            r => r.GetSiblingsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ClearCache_RemovesAllEntries()
    {
        // Arrange
        var chunk1 = CreateTestChunk(chunkIndex: 1);
        var chunk2 = CreateTestChunk(chunkIndex: 2);

        _chunkRepoMock
            .Setup(r => r.GetSiblingsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Chunk>());
        _headingServiceMock
            .Setup(h => h.GetBreadcrumbAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        await _sut.ExpandAsync(chunk1);
        await _sut.ExpandAsync(chunk2);

        // Act
        _sut.ClearCache();
        await _sut.ExpandAsync(chunk1);
        await _sut.ExpandAsync(chunk2);

        // Assert - both should go to repository again
        _chunkRepoMock.Verify(
            r => r.GetSiblingsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }

    #endregion
}
