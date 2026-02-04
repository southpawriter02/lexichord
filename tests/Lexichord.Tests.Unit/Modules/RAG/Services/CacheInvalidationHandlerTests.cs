// =============================================================================
// File: CacheInvalidationHandlerTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for CacheInvalidationHandler.
// =============================================================================
// VERSION: v0.5.8c (Multi-Layer Caching System)
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Indexing;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="CacheInvalidationHandler"/>.
/// </summary>
public sealed class CacheInvalidationHandlerTests
{
    private readonly Mock<IQueryResultCache> _queryCacheMock;
    private readonly Mock<IContextExpansionCache> _contextCacheMock;
    private readonly Mock<ILogger<CacheInvalidationHandler>> _loggerMock;
    private readonly CacheInvalidationHandler _sut;

    public CacheInvalidationHandlerTests()
    {
        _queryCacheMock = new Mock<IQueryResultCache>();
        _contextCacheMock = new Mock<IContextExpansionCache>();
        _loggerMock = new Mock<ILogger<CacheInvalidationHandler>>();

        _sut = new CacheInvalidationHandler(
            _queryCacheMock.Object,
            _contextCacheMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullQueryCache_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new CacheInvalidationHandler(
                null!,
                _contextCacheMock.Object,
                _loggerMock.Object));

        Assert.Equal("queryCache", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullContextCache_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new CacheInvalidationHandler(
                _queryCacheMock.Object,
                null!,
                _loggerMock.Object));

        Assert.Equal("contextCache", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new CacheInvalidationHandler(
                _queryCacheMock.Object,
                _contextCacheMock.Object,
                null!));

        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region DocumentIndexedEvent Tests

    [Fact]
    public async Task Handle_DocumentIndexedEvent_InvalidatesBothCaches()
    {
        var docId = Guid.NewGuid();
        var notification = new DocumentIndexedEvent(
            DocumentId: docId,
            FilePath: "/test/doc.md",
            ChunkCount: 5,
            Duration: TimeSpan.FromMilliseconds(100));

        await _sut.Handle(notification, CancellationToken.None);

        _queryCacheMock.Verify(c => c.InvalidateForDocument(docId), Times.Once);
        _contextCacheMock.Verify(c => c.InvalidateForDocument(docId), Times.Once);
    }

    [Fact]
    public async Task Handle_DocumentIndexedEvent_WithNullNotification_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.Handle((DocumentIndexedEvent)null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DocumentIndexedEvent_ContinuesOnCacheFailure()
    {
        var docId = Guid.NewGuid();
        var notification = new DocumentIndexedEvent(
            docId, "/test/doc.md", 5, TimeSpan.FromMilliseconds(100));

        _queryCacheMock
            .Setup(c => c.InvalidateForDocument(docId))
            .Throws(new InvalidOperationException("Cache error"));

        // Should not throw
        await _sut.Handle(notification, CancellationToken.None);
    }

    #endregion

    #region DocumentRemovedFromIndexEvent Tests

    [Fact]
    public async Task Handle_DocumentRemovedEvent_InvalidatesBothCaches()
    {
        var docId = Guid.NewGuid();
        var notification = new DocumentRemovedFromIndexEvent(
            DocumentId: docId,
            FilePath: "/test/doc.md");

        await _sut.Handle(notification, CancellationToken.None);

        _queryCacheMock.Verify(c => c.InvalidateForDocument(docId), Times.Once);
        _contextCacheMock.Verify(c => c.InvalidateForDocument(docId), Times.Once);
    }

    [Fact]
    public async Task Handle_DocumentRemovedEvent_WithNullNotification_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.Handle((DocumentRemovedFromIndexEvent)null!, CancellationToken.None));
    }

    #endregion
}
