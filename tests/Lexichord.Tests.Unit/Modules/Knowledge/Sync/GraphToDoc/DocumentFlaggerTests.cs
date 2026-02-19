// =============================================================================
// File: DocumentFlaggerTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for DocumentFlagger.
// =============================================================================
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc.Events;
using Lexichord.Modules.Knowledge.Sync.GraphToDoc;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Unit tests for <see cref="DocumentFlagger"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6g")]
public class DocumentFlaggerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<DocumentFlagger>> _mockLogger;
    private readonly Mock<ILogger<DocumentFlagStore>> _mockStoreLogger;
    private readonly DocumentFlagStore _flagStore;
    private readonly DocumentFlagger _sut;

    public DocumentFlaggerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<DocumentFlagger>>();
        _mockStoreLogger = new Mock<ILogger<DocumentFlagStore>>();

        _flagStore = new DocumentFlagStore(_mockStoreLogger.Object);
        _sut = new DocumentFlagger(_flagStore, _mockMediator.Object, _mockLogger.Object);
    }

    #region FlagDocumentAsync Tests

    [Fact]
    public async Task FlagDocumentAsync_CreatesFlag()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var options = new DocumentFlagOptions
        {
            Priority = FlagPriority.High,
            TriggeringEntityId = entityId,
            SendNotification = false
        };

        // Act
        var flag = await _sut.FlagDocumentAsync(documentId, FlagReason.EntityValueChanged, options);

        // Assert
        Assert.NotEqual(Guid.Empty, flag.FlagId);
        Assert.Equal(documentId, flag.DocumentId);
        Assert.Equal(entityId, flag.TriggeringEntityId);
        Assert.Equal(FlagReason.EntityValueChanged, flag.Reason);
        Assert.Equal(FlagPriority.High, flag.Priority);
        Assert.Equal(FlagStatus.Pending, flag.Status);
    }

    [Fact]
    public async Task FlagDocumentAsync_WithNotification_PublishesEvent()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var options = new DocumentFlagOptions
        {
            TriggeringEntityId = Guid.NewGuid(),
            SendNotification = true
        };

        // Act
        await _sut.FlagDocumentAsync(documentId, FlagReason.EntityDeleted, options);

        // Assert
        _mockMediator.Verify(
            x => x.Publish(It.IsAny<DocumentFlaggedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FlagDocumentAsync_WithoutNotification_DoesNotPublishEvent()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var options = new DocumentFlagOptions
        {
            TriggeringEntityId = Guid.NewGuid(),
            SendNotification = false
        };

        // Act
        await _sut.FlagDocumentAsync(documentId, FlagReason.EntityDeleted, options);

        // Assert
        _mockMediator.Verify(
            x => x.Publish(It.IsAny<DocumentFlaggedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region FlagDocumentsAsync Tests

    [Fact]
    public async Task FlagDocumentsAsync_CreatesMultipleFlags()
    {
        // Arrange
        var documentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var options = new DocumentFlagOptions
        {
            TriggeringEntityId = Guid.NewGuid(),
            SendNotification = false
        };

        // Act
        var flags = await _sut.FlagDocumentsAsync(documentIds, FlagReason.NewRelationship, options);

        // Assert
        Assert.Equal(3, flags.Count);
        Assert.All(flags, f => Assert.Equal(FlagReason.NewRelationship, f.Reason));
        Assert.All(flags, f => Assert.Equal(FlagStatus.Pending, f.Status));
    }

    #endregion

    #region ResolveFlagAsync Tests

    [Fact]
    public async Task ResolveFlagAsync_UpdatesFlagStatus()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var options = new DocumentFlagOptions
        {
            TriggeringEntityId = Guid.NewGuid(),
            SendNotification = false
        };
        var flag = await _sut.FlagDocumentAsync(documentId, FlagReason.EntityValueChanged, options);

        // Act
        var resolved = await _sut.ResolveFlagAsync(flag.FlagId, FlagResolution.UpdatedWithGraphChanges);

        // Assert
        Assert.True(resolved);
        var retrievedFlag = await _sut.GetFlagAsync(flag.FlagId);
        Assert.NotNull(retrievedFlag);
        Assert.Equal(FlagStatus.Resolved, retrievedFlag.Status);
        Assert.Equal(FlagResolution.UpdatedWithGraphChanges, retrievedFlag.Resolution);
        Assert.NotNull(retrievedFlag.ResolvedAt);
    }

    [Fact]
    public async Task ResolveFlagAsync_WithNonexistentFlag_ReturnsFalse()
    {
        // Arrange
        var nonexistentFlagId = Guid.NewGuid();

        // Act
        var result = await _sut.ResolveFlagAsync(nonexistentFlagId, FlagResolution.Dismissed);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ResolveFlagsAsync Tests

    [Fact]
    public async Task ResolveFlagsAsync_ReturnsResolvedCount()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var options = new DocumentFlagOptions
        {
            TriggeringEntityId = Guid.NewGuid(),
            SendNotification = false
        };
        var flag1 = await _sut.FlagDocumentAsync(documentId, FlagReason.EntityValueChanged, options);
        var flag2 = await _sut.FlagDocumentAsync(documentId, FlagReason.EntityDeleted, options);
        var nonexistentId = Guid.NewGuid();

        // Act
        var resolvedCount = await _sut.ResolveFlagsAsync(
            new List<Guid> { flag1.FlagId, flag2.FlagId, nonexistentId },
            FlagResolution.ManualMerge);

        // Assert
        Assert.Equal(2, resolvedCount);
    }

    #endregion

    #region GetPendingFlagsAsync Tests

    [Fact]
    public async Task GetPendingFlagsAsync_ReturnsOnlyPendingFlags()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var options = new DocumentFlagOptions
        {
            TriggeringEntityId = Guid.NewGuid(),
            SendNotification = false
        };
        var flag1 = await _sut.FlagDocumentAsync(documentId, FlagReason.EntityValueChanged, options);
        var flag2 = await _sut.FlagDocumentAsync(documentId, FlagReason.EntityDeleted, options);

        // Resolve one flag
        await _sut.ResolveFlagAsync(flag1.FlagId, FlagResolution.Dismissed);

        // Act
        var pendingFlags = await _sut.GetPendingFlagsAsync(documentId);

        // Assert
        Assert.Single(pendingFlags);
        Assert.Equal(flag2.FlagId, pendingFlags[0].FlagId);
    }

    #endregion
}
