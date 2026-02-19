// =============================================================================
// File: ConflictStoreTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ConflictStore service.
// =============================================================================
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;
using Lexichord.Modules.Knowledge.Sync.Conflict;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.Conflict;

/// <summary>
/// Unit tests for <see cref="ConflictStore"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6h")]
public class ConflictStoreTests
{
    private readonly Mock<ILogger<ConflictStore>> _mockLogger;
    private readonly ConflictStore _sut;

    public ConflictStoreTests()
    {
        _mockLogger = new Mock<ILogger<ConflictStore>>();
        _sut = new ConflictStore(_mockLogger.Object);
    }

    #region Add SyncConflict Tests

    [Fact]
    public void Add_SyncConflict_ReturnsTrue()
    {
        // Arrange
        var conflict = CreateTestConflict();
        var documentId = Guid.NewGuid();

        // Act
        var result = _sut.Add(conflict, documentId);

        // Assert
        Assert.True(result);
        Assert.Equal(1, _sut.TotalCount);
    }

    [Fact]
    public void Add_MultipleSyncConflicts_TracksAll()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var conflict1 = CreateTestConflict("Target1");
        var conflict2 = CreateTestConflict("Target2");
        var conflict3 = CreateTestConflict("Target3");

        // Act
        _sut.Add(conflict1, documentId);
        _sut.Add(conflict2, documentId);
        _sut.Add(conflict3, documentId);

        // Assert
        Assert.Equal(3, _sut.TotalCount);
        var unresolved = _sut.GetUnresolvedForDocument(documentId);
        Assert.Equal(3, unresolved.Count);
    }

    #endregion

    #region Add ConflictDetail Tests

    [Fact]
    public void Add_ConflictDetail_ReturnsTrue()
    {
        // Arrange
        var detail = CreateTestConflictDetail();
        var documentId = Guid.NewGuid();

        // Act
        var result = _sut.Add(detail, documentId);

        // Assert
        Assert.True(result);
        Assert.Equal(1, _sut.TotalCount);
    }

    [Fact]
    public void Add_DuplicateConflictDetail_ReturnsFalse()
    {
        // Arrange
        var detail = CreateTestConflictDetail();
        var documentId = Guid.NewGuid();

        // Act
        var first = _sut.Add(detail, documentId);
        var second = _sut.Add(detail, documentId); // Same ConflictId

        // Assert
        Assert.True(first);
        Assert.False(second);
        Assert.Equal(1, _sut.TotalCount);
    }

    #endregion

    #region Get Tests

    [Fact]
    public void Get_ExistingConflict_ReturnsStoredConflict()
    {
        // Arrange
        var detail = CreateTestConflictDetail();
        var documentId = Guid.NewGuid();
        _sut.Add(detail, documentId);

        // Act
        var result = _sut.Get(detail.ConflictId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(detail.ConflictId, result.ConflictId);
        Assert.Equal(documentId, result.DocumentId);
        Assert.False(result.IsResolved);
    }

    [Fact]
    public void Get_NonExistentConflict_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = _sut.Get(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetUnresolvedForDocument Tests

    [Fact]
    public void GetUnresolvedForDocument_ReturnsOnlyUnresolved()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var detail1 = CreateTestConflictDetail();
        var detail2 = CreateTestConflictDetail();
        _sut.Add(detail1, documentId);
        _sut.Add(detail2, documentId);

        // Mark one as resolved
        var resolution = ConflictResolutionResult.Success(
            CreateTestConflict(), ConflictResolutionStrategy.UseDocument, "value", true);
        _sut.MarkResolved(detail1.ConflictId, resolution);

        // Act
        var unresolved = _sut.GetUnresolvedForDocument(documentId);

        // Assert
        Assert.Single(unresolved);
    }

    [Fact]
    public void GetUnresolvedForDocument_NonExistentDocument_ReturnsEmpty()
    {
        // Arrange
        var nonExistentDocId = Guid.NewGuid();

        // Act
        var result = _sut.GetUnresolvedForDocument(nonExistentDocId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetUnresolvedForDocument_AllResolved_ReturnsEmpty()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var detail = CreateTestConflictDetail();
        _sut.Add(detail, documentId);

        var resolution = ConflictResolutionResult.Success(
            CreateTestConflict(), ConflictResolutionStrategy.UseDocument, "value", true);
        _sut.MarkResolved(detail.ConflictId, resolution);

        // Act
        var unresolved = _sut.GetUnresolvedForDocument(documentId);

        // Assert
        Assert.Empty(unresolved);
    }

    #endregion

    #region GetDetailsForDocument Tests

    [Fact]
    public void GetDetailsForDocument_ReturnsAllDetails()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var detail1 = CreateTestConflictDetail();
        var detail2 = CreateTestConflictDetail();
        _sut.Add(detail1, documentId);
        _sut.Add(detail2, documentId);

        // Act
        var details = _sut.GetDetailsForDocument(documentId);

        // Assert
        Assert.Equal(2, details.Count);
    }

    [Fact]
    public void GetDetailsForDocument_NonExistentDocument_ReturnsEmpty()
    {
        // Arrange
        var nonExistentDocId = Guid.NewGuid();

        // Act
        var result = _sut.GetDetailsForDocument(nonExistentDocId);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region MarkResolved Tests

    [Fact]
    public void MarkResolved_ExistingConflict_ReturnsTrue()
    {
        // Arrange
        var detail = CreateTestConflictDetail();
        var documentId = Guid.NewGuid();
        _sut.Add(detail, documentId);

        var resolution = ConflictResolutionResult.Success(
            CreateTestConflict(), ConflictResolutionStrategy.UseGraph, "graph-value", true);

        // Act
        var result = _sut.MarkResolved(detail.ConflictId, resolution);

        // Assert
        Assert.True(result);

        var stored = _sut.Get(detail.ConflictId);
        Assert.NotNull(stored);
        Assert.True(stored.IsResolved);
        Assert.NotNull(stored.Resolution);
        Assert.NotNull(stored.ResolvedAt);
    }

    [Fact]
    public void MarkResolved_NonExistentConflict_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var resolution = ConflictResolutionResult.Success(
            CreateTestConflict(), ConflictResolutionStrategy.UseDocument, "value", true);

        // Act
        var result = _sut.MarkResolved(nonExistentId, resolution);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void MarkResolved_StoresResolutionDetails()
    {
        // Arrange
        var detail = CreateTestConflictDetail();
        var documentId = Guid.NewGuid();
        _sut.Add(detail, documentId);

        var resolution = ConflictResolutionResult.Success(
            CreateTestConflict(),
            ConflictResolutionStrategy.Merge,
            "merged-value",
            isAutomatic: false);

        // Act
        _sut.MarkResolved(detail.ConflictId, resolution);

        // Assert
        var stored = _sut.Get(detail.ConflictId);
        Assert.NotNull(stored?.Resolution);
        Assert.Equal(ConflictResolutionStrategy.Merge, stored.Resolution.Strategy);
        Assert.Equal("merged-value", stored.Resolution.ResolvedValue);
    }

    #endregion

    #region RemoveForDocument Tests

    [Fact]
    public void RemoveForDocument_ExistingDocument_RemovesAllConflicts()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var detail1 = CreateTestConflictDetail();
        var detail2 = CreateTestConflictDetail();
        var detail3 = CreateTestConflictDetail();
        _sut.Add(detail1, documentId);
        _sut.Add(detail2, documentId);
        _sut.Add(detail3, documentId);

        // Act
        var count = _sut.RemoveForDocument(documentId);

        // Assert
        Assert.Equal(3, count);
        Assert.Equal(0, _sut.TotalCount);
        Assert.Empty(_sut.GetUnresolvedForDocument(documentId));
    }

    [Fact]
    public void RemoveForDocument_NonExistentDocument_ReturnsZero()
    {
        // Arrange
        var nonExistentDocId = Guid.NewGuid();

        // Act
        var count = _sut.RemoveForDocument(nonExistentDocId);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void RemoveForDocument_OnlyRemovesSpecifiedDocument()
    {
        // Arrange
        var docId1 = Guid.NewGuid();
        var docId2 = Guid.NewGuid();
        _sut.Add(CreateTestConflictDetail(), docId1);
        _sut.Add(CreateTestConflictDetail(), docId1);
        _sut.Add(CreateTestConflictDetail(), docId2);

        // Act
        _sut.RemoveForDocument(docId1);

        // Assert
        Assert.Equal(1, _sut.TotalCount);
        Assert.Single(_sut.GetUnresolvedForDocument(docId2));
    }

    #endregion

    #region GetUnresolvedCount Tests

    [Fact]
    public void GetUnresolvedCount_ReturnsCorrectCount()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var detail1 = CreateTestConflictDetail();
        var detail2 = CreateTestConflictDetail();
        var detail3 = CreateTestConflictDetail();
        _sut.Add(detail1, documentId);
        _sut.Add(detail2, documentId);
        _sut.Add(detail3, documentId);

        // Mark one resolved
        _sut.MarkResolved(detail1.ConflictId, ConflictResolutionResult.Success(
            CreateTestConflict(), ConflictResolutionStrategy.UseDocument, "v", true));

        // Act
        var count = _sut.GetUnresolvedCount(documentId);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void GetUnresolvedCount_NonExistentDocument_ReturnsZero()
    {
        // Arrange
        var nonExistentDocId = Guid.NewGuid();

        // Act
        var count = _sut.GetUnresolvedCount(nonExistentDocId);

        // Assert
        Assert.Equal(0, count);
    }

    #endregion

    #region TotalCount Tests

    [Fact]
    public void TotalCount_EmptyStore_ReturnsZero()
    {
        // Assert
        Assert.Equal(0, _sut.TotalCount);
    }

    [Fact]
    public void TotalCount_AfterAdditions_ReturnsCorrectCount()
    {
        // Arrange
        _sut.Add(CreateTestConflictDetail(), Guid.NewGuid());
        _sut.Add(CreateTestConflictDetail(), Guid.NewGuid());
        _sut.Add(CreateTestConflictDetail(), Guid.NewGuid());

        // Assert
        Assert.Equal(3, _sut.TotalCount);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllConflicts()
    {
        // Arrange
        _sut.Add(CreateTestConflictDetail(), Guid.NewGuid());
        _sut.Add(CreateTestConflictDetail(), Guid.NewGuid());
        _sut.Add(CreateTestConflictDetail(), Guid.NewGuid());

        // Act
        _sut.Clear();

        // Assert
        Assert.Equal(0, _sut.TotalCount);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Add_ConcurrentCalls_ThreadSafe()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var tasks = new List<Task>();
        var addCount = 100;

        // Act
        for (int i = 0; i < addCount; i++)
        {
            tasks.Add(Task.Run(() => _sut.Add(CreateTestConflictDetail(), documentId)));
        }
        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(addCount, _sut.TotalCount);
    }

    [Fact]
    public async Task GetUnresolvedForDocument_ConcurrentReads_ThreadSafe()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        for (int i = 0; i < 50; i++)
        {
            _sut.Add(CreateTestConflictDetail(), documentId);
        }

        var tasks = new List<Task<IReadOnlyList<SyncConflict>>>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => _sut.GetUnresolvedForDocument(documentId)));
        }
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r => Assert.Equal(50, r.Count));
    }

    #endregion

    #region StoredConflict Record Tests

    [Fact]
    public void StoredConflict_Timestamps_SetCorrectly()
    {
        // Arrange
        var detail = CreateTestConflictDetail();
        var documentId = Guid.NewGuid();
        var beforeAdd = DateTimeOffset.UtcNow;

        // Act
        _sut.Add(detail, documentId);
        var afterAdd = DateTimeOffset.UtcNow;

        // Assert
        var stored = _sut.Get(detail.ConflictId);
        Assert.NotNull(stored);
        Assert.True(stored.StoredAt >= beforeAdd);
        Assert.True(stored.StoredAt <= afterAdd);
    }

    #endregion

    #region Helper Methods

    private static SyncConflict CreateTestConflict(string target = "Entity:Test.Property")
    {
        return new SyncConflict
        {
            ConflictTarget = target,
            DocumentValue = "doc-value",
            GraphValue = "graph-value",
            DetectedAt = DateTimeOffset.UtcNow,
            Type = ConflictType.ValueMismatch,
            Severity = ConflictSeverity.Medium
        };
    }

    private static ConflictDetail CreateTestConflictDetail()
    {
        return new ConflictDetail
        {
            ConflictId = Guid.NewGuid(),
            Entity = new KnowledgeEntity
            {
                Id = Guid.NewGuid(),
                Name = "TestEntity",
                Type = "Concept",
                Properties = new Dictionary<string, object>
                {
                    ["Value"] = "test-value",
                    ["Confidence"] = 0.9f
                },
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                ModifiedAt = DateTimeOffset.UtcNow.AddDays(-1)
            },
            ConflictField = "Value",
            DocumentValue = "doc-value",
            GraphValue = "graph-value",
            Type = ConflictType.ValueMismatch,
            Severity = ConflictSeverity.Medium,
            DetectedAt = DateTimeOffset.UtcNow,
            SuggestedStrategy = ConflictResolutionStrategy.UseDocument,
            ResolutionConfidence = 0.8f
        };
    }

    #endregion
}
