// =============================================================================
// File: ContextExpansionCacheServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ContextExpansionCacheService.
// =============================================================================
// VERSION: v0.5.8c (Multi-Layer Caching System)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Configuration;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="ContextExpansionCacheService"/>.
/// </summary>
public sealed class ContextExpansionCacheServiceTests
{
    private readonly Mock<ILogger<ContextExpansionCacheService>> _loggerMock;
    private readonly ContextCacheOptions _options;
    private readonly ContextExpansionCacheService _sut;

    public ContextExpansionCacheServiceTests()
    {
        _loggerMock = new Mock<ILogger<ContextExpansionCacheService>>();
        _options = new ContextCacheOptions
        {
            MaxEntriesPerSession = 3,
            Enabled = true
        };
        _sut = new ContextExpansionCacheService(
            Options.Create(_options),
            _loggerMock.Object);
    }

    private static Chunk CreateTestChunk(
        Guid? id = null,
        Guid? documentId = null,
        int chunkIndex = 0)
    {
        return new Chunk(
            Id: id ?? Guid.NewGuid(),
            DocumentId: documentId ?? Guid.NewGuid(),
            Content: $"Test chunk content {chunkIndex}",
            Embedding: null,
            ChunkIndex: chunkIndex,
            StartOffset: chunkIndex * 100,
            EndOffset: (chunkIndex + 1) * 100);
    }

    private static ExpandedChunk CreateTestExpandedChunk(Chunk? core = null)
    {
        var coreChunk = core ?? CreateTestChunk();
        return new ExpandedChunk(
            Core: coreChunk,
            Before: new List<Chunk>(),
            After: new List<Chunk>(),
            ParentHeading: null,
            HeadingBreadcrumb: Array.Empty<string>());
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContextExpansionCacheService(null!, _loggerMock.Object));

        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContextExpansionCacheService(Options.Create(_options), null!));

        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region TryGet/Set Tests

    [Fact]
    public void TryGet_WhenSessionNotExists_ReturnsFalse()
    {
        var found = _sut.TryGet("session1", Guid.NewGuid(), out var result);

        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void TryGet_AfterSet_ReturnsTrue()
    {
        var chunkId = Guid.NewGuid();
        var expanded = CreateTestExpandedChunk();

        _sut.Set("session1", chunkId, expanded);
        var found = _sut.TryGet("session1", chunkId, out var result);

        Assert.True(found);
        Assert.NotNull(result);
    }

    [Fact]
    public void TryGet_DifferentSession_ReturnsFalse()
    {
        var chunkId = Guid.NewGuid();
        var expanded = CreateTestExpandedChunk();

        _sut.Set("session1", chunkId, expanded);
        var found = _sut.TryGet("session2", chunkId, out var result);

        Assert.False(found);
        Assert.Null(result);
    }

    #endregion

    #region Session Isolation Tests

    [Fact]
    public void Sessions_AreIsolated()
    {
        var chunkId = Guid.NewGuid();
        var expanded1 = CreateTestExpandedChunk(CreateTestChunk(chunkIndex: 1));
        var expanded2 = CreateTestExpandedChunk(CreateTestChunk(chunkIndex: 2));

        _sut.Set("session1", chunkId, expanded1);
        _sut.Set("session2", chunkId, expanded2);

        _sut.TryGet("session1", chunkId, out var result1);
        _sut.TryGet("session2", chunkId, out var result2);

        Assert.Equal(1, result1!.Core.ChunkIndex);
        Assert.Equal(2, result2!.Core.ChunkIndex);
    }

    [Fact]
    public void ActiveSessionCount_TracksSessionCount()
    {
        _sut.Set("session1", Guid.NewGuid(), CreateTestExpandedChunk());
        _sut.Set("session2", Guid.NewGuid(), CreateTestExpandedChunk());
        _sut.Set("session3", Guid.NewGuid(), CreateTestExpandedChunk());

        Assert.Equal(3, _sut.ActiveSessionCount);
    }

    [Fact]
    public void TotalEntryCount_TracksAllEntries()
    {
        _sut.Set("session1", Guid.NewGuid(), CreateTestExpandedChunk());
        _sut.Set("session1", Guid.NewGuid(), CreateTestExpandedChunk());
        _sut.Set("session2", Guid.NewGuid(), CreateTestExpandedChunk());

        Assert.Equal(3, _sut.TotalEntryCount);
    }

    #endregion

    #region Per-Session Eviction Tests

    [Fact]
    public void Set_WhenSessionAtCapacity_EvictsOldest()
    {
        var sessionId = "session1";
        var chunk1 = Guid.NewGuid();
        var chunk2 = Guid.NewGuid();
        var chunk3 = Guid.NewGuid();
        var chunk4 = Guid.NewGuid();

        _sut.Set(sessionId, chunk1, CreateTestExpandedChunk());
        _sut.Set(sessionId, chunk2, CreateTestExpandedChunk());
        _sut.Set(sessionId, chunk3, CreateTestExpandedChunk());
        
        // This should evict chunk1
        _sut.Set(sessionId, chunk4, CreateTestExpandedChunk());

        Assert.False(_sut.TryGet(sessionId, chunk1, out _));
        Assert.True(_sut.TryGet(sessionId, chunk4, out _));
    }

    #endregion

    #region Invalidation Tests

    [Fact]
    public void InvalidateSession_RemovesAllSessionEntries()
    {
        var sessionId = "session1";
        var chunk1 = Guid.NewGuid();
        var chunk2 = Guid.NewGuid();

        _sut.Set(sessionId, chunk1, CreateTestExpandedChunk());
        _sut.Set(sessionId, chunk2, CreateTestExpandedChunk());
        _sut.Set("session2", Guid.NewGuid(), CreateTestExpandedChunk());

        _sut.InvalidateSession(sessionId);

        Assert.False(_sut.TryGet(sessionId, chunk1, out _));
        Assert.False(_sut.TryGet(sessionId, chunk2, out _));
        Assert.Equal(1, _sut.ActiveSessionCount);
    }

    [Fact]
    public void InvalidateForDocument_RemovesMatchingEntries()
    {
        var docId = Guid.NewGuid();
        var chunk1 = CreateTestChunk(documentId: docId);
        var chunk2 = CreateTestChunk();

        var expanded1 = CreateTestExpandedChunk(chunk1);
        var expanded2 = CreateTestExpandedChunk(chunk2);

        _sut.Set("session1", chunk1.Id, expanded1);
        _sut.Set("session1", chunk2.Id, expanded2);
        _sut.Set("session2", chunk1.Id, expanded1);

        _sut.InvalidateForDocument(docId);

        Assert.False(_sut.TryGet("session1", chunk1.Id, out _));
        Assert.True(_sut.TryGet("session1", chunk2.Id, out _));
        Assert.False(_sut.TryGet("session2", chunk1.Id, out _));
    }

    [Fact]
    public void Clear_RemovesAllSessionsAndEntries()
    {
        _sut.Set("session1", Guid.NewGuid(), CreateTestExpandedChunk());
        _sut.Set("session2", Guid.NewGuid(), CreateTestExpandedChunk());

        _sut.Clear();

        Assert.Equal(0, _sut.ActiveSessionCount);
        Assert.Equal(0, _sut.TotalEntryCount);
    }

    #endregion

    #region Disabled Cache Tests

    [Fact]
    public void TryGet_WhenDisabled_ReturnsFalse()
    {
        var disabledOptions = new ContextCacheOptions { Enabled = false };
        var disabledCache = new ContextExpansionCacheService(
            Options.Create(disabledOptions),
            _loggerMock.Object);

        var chunkId = Guid.NewGuid();
        disabledCache.Set("session1", chunkId, CreateTestExpandedChunk());

        var found = disabledCache.TryGet("session1", chunkId, out _);

        Assert.False(found);
    }

    #endregion
}
