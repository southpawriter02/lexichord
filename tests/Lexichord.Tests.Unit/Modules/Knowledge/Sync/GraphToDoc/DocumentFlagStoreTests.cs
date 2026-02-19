// =============================================================================
// File: DocumentFlagStoreTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for DocumentFlagStore.
// =============================================================================
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;
using Lexichord.Modules.Knowledge.Sync.GraphToDoc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Unit tests for <see cref="DocumentFlagStore"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6g")]
public class DocumentFlagStoreTests
{
    private readonly Mock<ILogger<DocumentFlagStore>> _mockLogger;
    private readonly DocumentFlagStore _sut;

    public DocumentFlagStoreTests()
    {
        _mockLogger = new Mock<ILogger<DocumentFlagStore>>();
        _sut = new DocumentFlagStore(_mockLogger.Object);
    }

    #region AddAsync and GetAsync Tests

    [Fact]
    public async Task AddAsync_StoresFlag()
    {
        // Arrange
        var flag = CreateTestFlag();

        // Act
        await _sut.AddAsync(flag);
        var retrieved = await _sut.GetAsync(flag.FlagId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(flag.FlagId, retrieved.FlagId);
        Assert.Equal(flag.DocumentId, retrieved.DocumentId);
        Assert.Equal(flag.Reason, retrieved.Reason);
    }

    [Fact]
    public async Task GetAsync_WithNonexistentId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByDocumentAsync Tests

    [Fact]
    public async Task GetByDocumentAsync_ReturnsFlagsForDocument()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var flag1 = CreateTestFlag(documentId);
        var flag2 = CreateTestFlag(documentId);
        var flag3 = CreateTestFlag(Guid.NewGuid()); // Different document

        await _sut.AddAsync(flag1);
        await _sut.AddAsync(flag2);
        await _sut.AddAsync(flag3);

        // Act
        var flags = await _sut.GetByDocumentAsync(documentId);

        // Assert
        Assert.Equal(2, flags.Count);
        Assert.All(flags, f => Assert.Equal(documentId, f.DocumentId));
    }

    [Fact]
    public async Task GetByDocumentAsync_WithNoFlags_ReturnsEmpty()
    {
        // Act
        var flags = await _sut.GetByDocumentAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(flags);
    }

    #endregion

    #region GetPendingByDocumentAsync Tests

    [Fact]
    public async Task GetPendingByDocumentAsync_ReturnsOnlyPendingAndAcknowledged()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var pendingFlag = CreateTestFlag(documentId) with { Status = FlagStatus.Pending };
        var acknowledgedFlag = CreateTestFlag(documentId) with { Status = FlagStatus.Acknowledged };
        var resolvedFlag = CreateTestFlag(documentId) with { Status = FlagStatus.Resolved };
        var dismissedFlag = CreateTestFlag(documentId) with { Status = FlagStatus.Dismissed };

        await _sut.AddAsync(pendingFlag);
        await _sut.AddAsync(acknowledgedFlag);
        await _sut.AddAsync(resolvedFlag);
        await _sut.AddAsync(dismissedFlag);

        // Act
        var flags = await _sut.GetPendingByDocumentAsync(documentId);

        // Assert
        Assert.Equal(2, flags.Count);
        Assert.All(flags, f =>
            Assert.True(f.Status == FlagStatus.Pending || f.Status == FlagStatus.Acknowledged));
    }

    [Fact]
    public async Task GetPendingByDocumentAsync_OrdersByPriorityThenCreatedAt()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var lowPriority = CreateTestFlag(documentId) with
        {
            FlagId = Guid.NewGuid(),
            Priority = FlagPriority.Low,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        var highPriority = CreateTestFlag(documentId) with
        {
            FlagId = Guid.NewGuid(),
            Priority = FlagPriority.High,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var criticalPriority = CreateTestFlag(documentId) with
        {
            FlagId = Guid.NewGuid(),
            Priority = FlagPriority.Critical,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2)
        };

        await _sut.AddAsync(lowPriority);
        await _sut.AddAsync(highPriority);
        await _sut.AddAsync(criticalPriority);

        // Act
        var flags = await _sut.GetPendingByDocumentAsync(documentId);

        // Assert
        Assert.Equal(3, flags.Count);
        Assert.Equal(FlagPriority.Critical, flags[0].Priority);
        Assert.Equal(FlagPriority.High, flags[1].Priority);
        Assert.Equal(FlagPriority.Low, flags[2].Priority);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_UpdatesFlag()
    {
        // Arrange
        var flag = CreateTestFlag();
        await _sut.AddAsync(flag);

        var updatedFlag = flag with
        {
            Status = FlagStatus.Resolved,
            Resolution = FlagResolution.UpdatedWithGraphChanges,
            ResolvedAt = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _sut.UpdateAsync(updatedFlag);
        var retrieved = await _sut.GetAsync(flag.FlagId);

        // Assert
        Assert.True(result);
        Assert.NotNull(retrieved);
        Assert.Equal(FlagStatus.Resolved, retrieved.Status);
        Assert.Equal(FlagResolution.UpdatedWithGraphChanges, retrieved.Resolution);
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentFlag_ReturnsFalse()
    {
        // Arrange
        var flag = CreateTestFlag();

        // Act
        var result = await _sut.UpdateAsync(flag);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region RemoveAsync Tests

    [Fact]
    public async Task RemoveAsync_RemovesFlag()
    {
        // Arrange
        var flag = CreateTestFlag();
        await _sut.AddAsync(flag);

        // Act
        var removed = await _sut.RemoveAsync(flag.FlagId);
        var retrieved = await _sut.GetAsync(flag.FlagId);

        // Assert
        Assert.True(removed);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task RemoveAsync_WithNonexistentFlag_ReturnsFalse()
    {
        // Act
        var result = await _sut.RemoveAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetPendingCountAsync Tests

    [Fact]
    public async Task GetPendingCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        await _sut.AddAsync(CreateTestFlag(documentId) with { Status = FlagStatus.Pending });
        await _sut.AddAsync(CreateTestFlag(documentId) with { Status = FlagStatus.Acknowledged });
        await _sut.AddAsync(CreateTestFlag(documentId) with { Status = FlagStatus.Resolved });

        // Act
        var count = await _sut.GetPendingCountAsync(documentId);

        // Assert
        Assert.Equal(2, count);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ConcurrentOperations_AreThreadSafe()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var tasks = new List<Task>();

        // Act - Run 100 concurrent add operations
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_sut.AddAsync(CreateTestFlag(documentId)));
        }
        await Task.WhenAll(tasks);

        // Assert
        var flags = await _sut.GetByDocumentAsync(documentId);
        Assert.Equal(100, flags.Count);
    }

    #endregion

    #region Helper Methods

    private static DocumentFlag CreateTestFlag(Guid? documentId = null)
    {
        return new DocumentFlag
        {
            FlagId = Guid.NewGuid(),
            DocumentId = documentId ?? Guid.NewGuid(),
            TriggeringEntityId = Guid.NewGuid(),
            Reason = FlagReason.EntityValueChanged,
            Priority = FlagPriority.Medium,
            Status = FlagStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
