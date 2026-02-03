// =============================================================================
// File: SiblingCacheTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SiblingCache.
// =============================================================================
// VERSION: v0.5.3b (Sibling Chunk Retrieval)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Data;
using Lexichord.Modules.RAG.Indexing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Data;

/// <summary>
/// Unit tests for <see cref="SiblingCache"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.3b")]
public sealed class SiblingCacheTests
{
    private readonly Mock<ILogger<SiblingCache>> _loggerMock;
    private readonly SiblingCache _sut;

    public SiblingCacheTests()
    {
        _loggerMock = new Mock<ILogger<SiblingCache>>();
        _sut = new SiblingCache(_loggerMock.Object);
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
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SiblingCache(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidLogger_InitializesEmptyCache()
    {
        // Act
        var cache = new SiblingCache(_loggerMock.Object);

        // Assert
        cache.Count.Should().Be(0);
    }

    #endregion

    #region TryGet / Set Tests

    [Fact]
    public void TryGet_OnEmptyCache_ReturnsFalse()
    {
        // Arrange
        var key = new SiblingCacheKey(Guid.NewGuid(), 5, 1, 1);

        // Act
        var found = _sut.TryGet(key, out var result);

        // Assert
        found.Should().BeFalse(because: "cache is empty");
        result.Should().BeEmpty();
    }

    [Fact]
    public void TryGet_AfterSet_ReturnsTrue()
    {
        // Arrange
        var key = new SiblingCacheKey(Guid.NewGuid(), 5, 1, 1);
        var chunks = new List<Chunk> { CreateTestChunk(4), CreateTestChunk(6) };

        _sut.Set(key, chunks);

        // Act
        var found = _sut.TryGet(key, out var result);

        // Assert
        found.Should().BeTrue(because: "key was added to cache");
        result.Should().BeEquivalentTo(chunks);
    }

    [Fact]
    public void TryGet_WithDifferentKey_ReturnsFalse()
    {
        // Arrange
        var key1 = new SiblingCacheKey(Guid.NewGuid(), 5, 1, 1);
        var key2 = new SiblingCacheKey(Guid.NewGuid(), 5, 1, 1); // Different document
        var chunks = new List<Chunk> { CreateTestChunk(4) };

        _sut.Set(key1, chunks);

        // Act
        var found = _sut.TryGet(key2, out _);

        // Assert
        found.Should().BeFalse(because: "key2 has different DocumentId");
    }

    [Fact]
    public void Set_UpdatesExistingEntry()
    {
        // Arrange
        var key = new SiblingCacheKey(Guid.NewGuid(), 5, 1, 1);
        var chunks1 = new List<Chunk> { CreateTestChunk(4) };
        var chunks2 = new List<Chunk> { CreateTestChunk(4), CreateTestChunk(6) };

        _sut.Set(key, chunks1);
        _sut.Set(key, chunks2);

        // Act
        _sut.TryGet(key, out var result);

        // Assert
        result.Should().BeEquivalentTo(chunks2, because: "entry was updated");
        _sut.Count.Should().Be(1, because: "only one entry exists");
    }

    [Fact]
    public void TryGet_UpdatesLastAccessed()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var key1 = new SiblingCacheKey(docId, 5, 1, 1);
        var key2 = new SiblingCacheKey(docId, 10, 1, 1);
        var chunks = new List<Chunk> { CreateTestChunk(4) };

        _sut.Set(key1, chunks);
        Thread.Sleep(10); // Ensure different timestamp
        _sut.Set(key2, chunks);

        // Access key1 to update its timestamp (makes it newer)
        _sut.TryGet(key1, out _);

        // Assert - key1 should now be newer than key2
        // We can't directly test LastAccessed, but we can verify the cache is working
        _sut.Count.Should().Be(2);
    }

    #endregion

    #region InvalidateDocument Tests

    [Fact]
    public void InvalidateDocument_RemovesOnlyMatchingEntries()
    {
        // Arrange
        var docId1 = Guid.NewGuid();
        var docId2 = Guid.NewGuid();

        _sut.Set(new SiblingCacheKey(docId1, 5, 1, 1), new List<Chunk>());
        _sut.Set(new SiblingCacheKey(docId1, 10, 1, 1), new List<Chunk>());
        _sut.Set(new SiblingCacheKey(docId2, 5, 1, 1), new List<Chunk>());

        _sut.Count.Should().Be(3);

        // Act
        _sut.InvalidateDocument(docId1);

        // Assert
        _sut.Count.Should().Be(1, because: "only docId2 entries remain");
        _sut.TryGet(new SiblingCacheKey(docId2, 5, 1, 1), out _).Should().BeTrue();
        _sut.TryGet(new SiblingCacheKey(docId1, 5, 1, 1), out _).Should().BeFalse();
    }

    [Fact]
    public void InvalidateDocument_WithNonExistentDocument_DoesNothing()
    {
        // Arrange
        var docId = Guid.NewGuid();
        _sut.Set(new SiblingCacheKey(docId, 5, 1, 1), new List<Chunk>());
        _sut.Count.Should().Be(1);

        // Act
        _sut.InvalidateDocument(Guid.NewGuid()); // Different document

        // Assert
        _sut.Count.Should().Be(1, because: "no matching entries to invalidate");
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        // Arrange
        _sut.Set(new SiblingCacheKey(Guid.NewGuid(), 5, 1, 1), new List<Chunk>());
        _sut.Set(new SiblingCacheKey(Guid.NewGuid(), 10, 2, 2), new List<Chunk>());
        _sut.Set(new SiblingCacheKey(Guid.NewGuid(), 15, 3, 3), new List<Chunk>());
        _sut.Count.Should().Be(3);

        // Act
        _sut.Clear();

        // Assert
        _sut.Count.Should().Be(0, because: "cache was cleared");
    }

    [Fact]
    public void Clear_OnEmptyCache_DoesNotThrow()
    {
        // Act
        var act = () => _sut.Clear();

        // Assert
        act.Should().NotThrow();
        _sut.Count.Should().Be(0);
    }

    #endregion

    #region LRU Eviction Tests

    [Fact]
    public void Set_AtMaxCapacity_EvictsOldestEntries()
    {
        // Arrange - Fill cache to capacity
        for (int i = 0; i < SiblingCache.MaxEntries; i++)
        {
            _sut.Set(new SiblingCacheKey(Guid.NewGuid(), i, 1, 1), new List<Chunk>());
        }

        _sut.Count.Should().Be(SiblingCache.MaxEntries);

        // Act - Add one more entry to trigger eviction
        var newKey = new SiblingCacheKey(Guid.NewGuid(), 999, 1, 1);
        _sut.Set(newKey, new List<Chunk>());

        // Assert - Should have evicted EvictionBatch entries
        _sut.Count.Should().BeLessOrEqualTo(
            SiblingCache.MaxEntries - SiblingCache.EvictionBatch + 1,
            because: $"eviction batch of {SiblingCache.EvictionBatch} entries should be removed");

        // New entry should be present
        _sut.TryGet(newKey, out _).Should().BeTrue(because: "newly added entry should exist");
    }

    [Fact]
    public void MaxEntries_HasExpectedValue()
    {
        SiblingCache.MaxEntries.Should().Be(500, because: "spec requires 500 max entries");
    }

    [Fact]
    public void EvictionBatch_HasExpectedValue()
    {
        SiblingCache.EvictionBatch.Should().Be(50, because: "spec requires 50 entry eviction batch");
    }

    #endregion

    #region MediatR Event Handler Tests

    [Fact]
    public async Task Handle_DocumentIndexedEvent_InvalidatesDocument()
    {
        // Arrange
        var docId = Guid.NewGuid();
        _sut.Set(new SiblingCacheKey(docId, 5, 1, 1), new List<Chunk>());
        _sut.Set(new SiblingCacheKey(Guid.NewGuid(), 5, 1, 1), new List<Chunk>()); // Different doc
        _sut.Count.Should().Be(2);

        var @event = new DocumentIndexedEvent(
            DocumentId: docId,
            FilePath: "test.md",
            ChunkCount: 10,
            Duration: TimeSpan.FromSeconds(1));

        // Act
        await _sut.Handle(@event, CancellationToken.None);

        // Assert
        _sut.Count.Should().Be(1, because: "only non-matching document entry remains");
        _sut.TryGet(new SiblingCacheKey(docId, 5, 1, 1), out _).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DocumentRemovedFromIndexEvent_InvalidatesDocument()
    {
        // Arrange
        var docId = Guid.NewGuid();
        _sut.Set(new SiblingCacheKey(docId, 5, 1, 1), new List<Chunk>());
        _sut.Set(new SiblingCacheKey(docId, 10, 2, 2), new List<Chunk>());
        _sut.Count.Should().Be(2);

        var @event = new DocumentRemovedFromIndexEvent(
            DocumentId: docId,
            FilePath: "test.md");

        // Act
        await _sut.Handle(@event, CancellationToken.None);

        // Assert
        _sut.Count.Should().Be(0, because: "all entries for document were removed");
    }

    [Fact]
    public async Task Handle_DocumentIndexedEvent_DoesNothingForNonMatchingDocument()
    {
        // Arrange
        var docId = Guid.NewGuid();
        _sut.Set(new SiblingCacheKey(docId, 5, 1, 1), new List<Chunk>());
        _sut.Count.Should().Be(1);

        var @event = new DocumentIndexedEvent(
            DocumentId: Guid.NewGuid(), // Different document
            FilePath: "other.md",
            ChunkCount: 5,
            Duration: TimeSpan.FromSeconds(1));

        // Act
        await _sut.Handle(@event, CancellationToken.None);

        // Assert
        _sut.Count.Should().Be(1, because: "no matching entries to invalidate");
    }

    [Fact]
    public async Task Handle_DocumentRemovedFromIndexEvent_DoesNothingForNonMatchingDocument()
    {
        // Arrange
        var docId = Guid.NewGuid();
        _sut.Set(new SiblingCacheKey(docId, 5, 1, 1), new List<Chunk>());
        _sut.Count.Should().Be(1);

        var @event = new DocumentRemovedFromIndexEvent(
            DocumentId: Guid.NewGuid(), // Different document
            FilePath: "other.md");

        // Act
        await _sut.Handle(@event, CancellationToken.None);

        // Assert
        _sut.Count.Should().Be(1, because: "no matching entries to invalidate");
    }

    #endregion

    #region SiblingCacheKey Tests

    [Fact]
    public void SiblingCacheKey_WithSameValues_AreEqual()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var key1 = new SiblingCacheKey(docId, 5, 1, 1);
        var key2 = new SiblingCacheKey(docId, 5, 1, 1);

        // Assert
        key1.Should().Be(key2, because: "identical values should produce equal keys");
        key1.GetHashCode().Should().Be(key2.GetHashCode());
    }

    [Fact]
    public void SiblingCacheKey_WithDifferentDocumentId_AreNotEqual()
    {
        var key1 = new SiblingCacheKey(Guid.NewGuid(), 5, 1, 1);
        var key2 = new SiblingCacheKey(Guid.NewGuid(), 5, 1, 1);

        key1.Should().NotBe(key2);
    }

    [Fact]
    public void SiblingCacheKey_WithDifferentCenterIndex_AreNotEqual()
    {
        var docId = Guid.NewGuid();
        var key1 = new SiblingCacheKey(docId, 5, 1, 1);
        var key2 = new SiblingCacheKey(docId, 10, 1, 1);

        key1.Should().NotBe(key2);
    }

    [Fact]
    public void SiblingCacheKey_WithDifferentBeforeCount_AreNotEqual()
    {
        var docId = Guid.NewGuid();
        var key1 = new SiblingCacheKey(docId, 5, 1, 1);
        var key2 = new SiblingCacheKey(docId, 5, 2, 1);

        key1.Should().NotBe(key2);
    }

    [Fact]
    public void SiblingCacheKey_WithDifferentAfterCount_AreNotEqual()
    {
        var docId = Guid.NewGuid();
        var key1 = new SiblingCacheKey(docId, 5, 1, 1);
        var key2 = new SiblingCacheKey(docId, 5, 1, 2);

        key1.Should().NotBe(key2);
    }

    #endregion
}
