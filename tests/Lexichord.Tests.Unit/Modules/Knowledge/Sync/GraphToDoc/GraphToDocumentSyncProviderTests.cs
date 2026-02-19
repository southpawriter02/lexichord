// =============================================================================
// File: GraphToDocumentSyncProviderTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for GraphToDocumentSyncProvider.
// =============================================================================
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;
using Lexichord.Modules.Knowledge.Sync.GraphToDoc;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Unit tests for <see cref="GraphToDocumentSyncProvider"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6g")]
public class GraphToDocumentSyncProviderTests
{
    private readonly Mock<IAffectedDocumentDetector> _mockDetector;
    private readonly Mock<IDocumentFlagger> _mockFlagger;
    private readonly Mock<ILicenseContext> _mockLicenseContext;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<GraphToDocumentSyncProvider>> _mockLogger;
    private readonly GraphToDocumentSyncProvider _sut;

    public GraphToDocumentSyncProviderTests()
    {
        _mockDetector = new Mock<IAffectedDocumentDetector>();
        _mockFlagger = new Mock<IDocumentFlagger>();
        _mockLicenseContext = new Mock<ILicenseContext>();
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<GraphToDocumentSyncProvider>>();

        _sut = new GraphToDocumentSyncProvider(
            _mockDetector.Object,
            _mockFlagger.Object,
            _mockLicenseContext.Object,
            _mockMediator.Object,
            _mockLogger.Object);
    }

    #region License Gating Tests

    [Theory]
    [InlineData(LicenseTier.Core)]
    [InlineData(LicenseTier.WriterPro)]
    public async Task OnGraphChangeAsync_WithInsufficientTier_ThrowsUnauthorizedException(LicenseTier tier)
    {
        // Arrange
        var change = CreateTestChange();
        _mockLicenseContext.Setup(x => x.Tier).Returns(tier);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.OnGraphChangeAsync(change));
    }

    [Theory]
    [InlineData(LicenseTier.Teams)]
    [InlineData(LicenseTier.Enterprise)]
    public async Task OnGraphChangeAsync_WithSufficientTier_DoesNotThrowUnauthorized(LicenseTier tier)
    {
        // Arrange
        var change = CreateTestChange();
        _mockLicenseContext.Setup(x => x.Tier).Returns(tier);
        _mockDetector
            .Setup(x => x.DetectAsync(It.IsAny<GraphChange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AffectedDocument>());

        // Act
        var result = await _sut.OnGraphChangeAsync(change);

        // Assert
        Assert.Equal(SyncOperationStatus.NoChanges, result.Status);
    }

    #endregion

    #region OnGraphChangeAsync Tests

    [Fact]
    public async Task OnGraphChangeAsync_WithNoAffectedDocuments_ReturnsNoChanges()
    {
        // Arrange
        var change = CreateTestChange();
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);
        _mockDetector
            .Setup(x => x.DetectAsync(It.IsAny<GraphChange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AffectedDocument>());

        // Act
        var result = await _sut.OnGraphChangeAsync(change);

        // Assert
        Assert.Equal(SyncOperationStatus.NoChanges, result.Status);
        Assert.Empty(result.AffectedDocuments);
        Assert.Empty(result.FlagsCreated);
    }

    [Fact]
    public async Task OnGraphChangeAsync_WithAffectedDocuments_CreatesFlagsAndReturnsSuccess()
    {
        // Arrange
        var change = CreateTestChange();
        var affectedDocs = new List<AffectedDocument>
        {
            CreateTestAffectedDocument(Guid.NewGuid(), "Doc1"),
            CreateTestAffectedDocument(Guid.NewGuid(), "Doc2")
        };
        var flags = affectedDocs.Select(d => CreateTestFlag(d.DocumentId)).ToList();

        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);
        _mockDetector
            .Setup(x => x.DetectAsync(It.IsAny<GraphChange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(affectedDocs);
        _mockFlagger
            .Setup(x => x.FlagDocumentsAsync(
                It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<FlagReason>(),
                It.IsAny<DocumentFlagOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        // Act
        var result = await _sut.OnGraphChangeAsync(change);

        // Assert
        Assert.Equal(SyncOperationStatus.Success, result.Status);
        Assert.Equal(2, result.AffectedDocuments.Count);
        Assert.Equal(2, result.FlagsCreated.Count);
        Assert.Equal(2, result.TotalDocumentsNotified);
    }

    [Fact]
    public async Task OnGraphChangeAsync_WithAutoFlagDisabled_DoesNotCreateFlags()
    {
        // Arrange
        var change = CreateTestChange();
        var options = new GraphToDocSyncOptions { AutoFlagDocuments = false };
        var affectedDocs = new List<AffectedDocument>
        {
            CreateTestAffectedDocument(Guid.NewGuid(), "Doc1")
        };

        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);
        _mockDetector
            .Setup(x => x.DetectAsync(It.IsAny<GraphChange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(affectedDocs);

        // Act
        var result = await _sut.OnGraphChangeAsync(change, options);

        // Assert
        Assert.Equal(SyncOperationStatus.NoChanges, result.Status);
        Assert.Single(result.AffectedDocuments);
        Assert.Empty(result.FlagsCreated);
        _mockFlagger.Verify(
            x => x.FlagDocumentsAsync(
                It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<FlagReason>(),
                It.IsAny<DocumentFlagOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnGraphChangeAsync_TruncatesExcessAffectedDocuments()
    {
        // Arrange
        var change = CreateTestChange();
        var options = new GraphToDocSyncOptions { MaxDocumentsPerChange = 2 };
        var affectedDocs = Enumerable.Range(1, 5)
            .Select(i => CreateTestAffectedDocument(Guid.NewGuid(), $"Doc{i}"))
            .ToList();

        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);
        _mockDetector
            .Setup(x => x.DetectAsync(It.IsAny<GraphChange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(affectedDocs);
        _mockFlagger
            .Setup(x => x.FlagDocumentsAsync(
                It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<FlagReason>(),
                It.IsAny<DocumentFlagOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, FlagReason _, DocumentFlagOptions _, CancellationToken _) =>
                ids.Select(id => CreateTestFlag(id)).ToList());

        // Act
        var result = await _sut.OnGraphChangeAsync(change, options);

        // Assert
        Assert.Equal(2, result.AffectedDocuments.Count);
    }

    #endregion

    #region GetAffectedDocumentsAsync Tests

    [Fact]
    public async Task GetAffectedDocumentsAsync_DelegatestoDetector()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var expectedDocs = new List<AffectedDocument>
        {
            CreateTestAffectedDocument(Guid.NewGuid(), "Test")
        };

        _mockDetector
            .Setup(x => x.DetectAsync(
                It.Is<GraphChange>(c => c.EntityId == entityId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocs);

        // Act
        var result = await _sut.GetAffectedDocumentsAsync(entityId);

        // Assert
        Assert.Equal(expectedDocs, result);
    }

    #endregion

    #region ResolveFlagAsync Tests

    [Fact]
    public async Task ResolveFlagAsync_DelegatesToFlagger()
    {
        // Arrange
        var flagId = Guid.NewGuid();
        var resolution = FlagResolution.UpdatedWithGraphChanges;

        _mockFlagger
            .Setup(x => x.ResolveFlagAsync(flagId, resolution, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ResolveFlagAsync(flagId, resolution);

        // Assert
        Assert.True(result);
        _mockFlagger.Verify(x => x.ResolveFlagAsync(flagId, resolution, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static GraphChange CreateTestChange()
    {
        return new GraphChange
        {
            EntityId = Guid.NewGuid(),
            ChangeType = ChangeType.EntityUpdated,
            NewValue = "NewValue",
            ChangedAt = DateTimeOffset.UtcNow
        };
    }

    private static AffectedDocument CreateTestAffectedDocument(Guid documentId, string name)
    {
        return new AffectedDocument
        {
            DocumentId = documentId,
            DocumentName = name,
            Relationship = DocumentEntityRelationship.DerivedFrom,
            ReferenceCount = 1,
            LastModifiedAt = DateTimeOffset.UtcNow
        };
    }

    private static DocumentFlag CreateTestFlag(Guid documentId)
    {
        return new DocumentFlag
        {
            FlagId = Guid.NewGuid(),
            DocumentId = documentId,
            TriggeringEntityId = Guid.NewGuid(),
            Reason = FlagReason.EntityValueChanged,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
