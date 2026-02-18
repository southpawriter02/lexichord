// =============================================================================
// File: SyncServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SyncService.
// =============================================================================
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.Knowledge.Sync.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// Alias to disambiguate from Lexichord.Abstractions.Contracts.IConflictResolver
using ISyncConflictResolver = Lexichord.Abstractions.Contracts.Knowledge.Sync.IConflictResolver;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync;

/// <summary>
/// Unit tests for <see cref="SyncService"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6e")]
public class SyncServiceTests
{
    private readonly Mock<ISyncOrchestrator> _mockOrchestrator;
    private readonly Mock<ISyncStatusTracker> _mockStatusTracker;
    private readonly Mock<ISyncConflictResolver> _mockConflictResolver;
    private readonly Mock<ILicenseContext> _mockLicenseContext;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<SyncService>> _mockLogger;
    private readonly SyncService _sut;

    public SyncServiceTests()
    {
        _mockOrchestrator = new Mock<ISyncOrchestrator>();
        _mockStatusTracker = new Mock<ISyncStatusTracker>();
        _mockConflictResolver = new Mock<ISyncConflictResolver>();
        _mockLicenseContext = new Mock<ILicenseContext>();
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<SyncService>>();

        _sut = new SyncService(
            _mockOrchestrator.Object,
            _mockStatusTracker.Object,
            _mockConflictResolver.Object,
            _mockLicenseContext.Object,
            _mockMediator.Object,
            _mockLogger.Object);
    }

    #region SyncDocumentToGraphAsync Tests

    [Fact]
    public async Task SyncDocumentToGraphAsync_WithWriterProTier_ExecutesSync()
    {
        // Arrange
        var document = CreateTestDocument();
        var context = CreateTestContext(document);
        var expectedResult = new SyncResult
        {
            Status = SyncOperationStatus.Success,
            EntitiesAffected = [],
            Duration = TimeSpan.FromMilliseconds(100)
        };

        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.WriterPro);
        _mockOrchestrator
            .Setup(x => x.ExecuteDocumentToGraphAsync(document, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.SyncDocumentToGraphAsync(document, context);

        // Assert
        Assert.Equal(SyncOperationStatus.Success, result.Status);
        _mockOrchestrator.Verify(
            x => x.ExecuteDocumentToGraphAsync(document, context, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SyncDocumentToGraphAsync_WithCoreTier_ThrowsUnauthorizedException()
    {
        // Arrange
        var document = CreateTestDocument();
        var context = CreateTestContext(document);

        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Core);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.SyncDocumentToGraphAsync(document, context));
    }

    [Fact]
    public async Task SyncDocumentToGraphAsync_WithConflicts_ReturnsSuccessWithConflicts()
    {
        // Arrange
        var document = CreateTestDocument();
        var context = CreateTestContext(document);
        var conflict = new SyncConflict
        {
            ConflictTarget = "Entity:Name.Property",
            DocumentValue = "doc-value",
            GraphValue = "graph-value",
            DetectedAt = DateTimeOffset.UtcNow,
            Type = ConflictType.ValueMismatch,
            Severity = ConflictSeverity.Medium
        };
        var expectedResult = new SyncResult
        {
            Status = SyncOperationStatus.SuccessWithConflicts,
            Conflicts = [conflict],
            Duration = TimeSpan.FromMilliseconds(100)
        };

        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);
        _mockOrchestrator
            .Setup(x => x.ExecuteDocumentToGraphAsync(document, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.SyncDocumentToGraphAsync(document, context);

        // Assert
        Assert.Equal(SyncOperationStatus.SuccessWithConflicts, result.Status);
        Assert.Single(result.Conflicts);
    }

    [Fact]
    public async Task SyncDocumentToGraphAsync_UpdatesStatusCorrectly()
    {
        // Arrange
        var document = CreateTestDocument();
        var context = CreateTestContext(document);
        var expectedResult = new SyncResult
        {
            Status = SyncOperationStatus.Success,
            Duration = TimeSpan.FromMilliseconds(100)
        };

        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.WriterPro);
        _mockOrchestrator
            .Setup(x => x.ExecuteDocumentToGraphAsync(document, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _sut.SyncDocumentToGraphAsync(document, context);

        // Assert
        _mockStatusTracker.Verify(
            x => x.UpdateStatusAsync(
                document.Id,
                It.Is<SyncStatus>(s => s.State == SyncState.InSync),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetSyncStatusAsync Tests

    [Fact]
    public async Task GetSyncStatusAsync_ReturnsStatusFromTracker()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var expectedStatus = new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.PendingSync,
            PendingChanges = 5
        };

        _mockStatusTracker
            .Setup(x => x.GetStatusAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStatus);

        // Act
        var result = await _sut.GetSyncStatusAsync(documentId);

        // Assert
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(SyncState.PendingSync, result.State);
        Assert.Equal(5, result.PendingChanges);
    }

    #endregion

    #region NeedsSyncAsync Tests

    [Fact]
    public async Task NeedsSyncAsync_WithPendingSync_ReturnsTrue()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var status = new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.PendingSync
        };

        _mockStatusTracker
            .Setup(x => x.GetStatusAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var result = await _sut.NeedsSyncAsync(documentId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task NeedsSyncAsync_WithInSync_ReturnsFalse()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var status = new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.InSync,
            PendingChanges = 0
        };

        _mockStatusTracker
            .Setup(x => x.GetStatusAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var result = await _sut.NeedsSyncAsync(documentId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Helper Methods

    private static Document CreateTestDocument()
    {
        return new Document(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            FilePath: "/test/document.md",
            Title: "Test Document",
            Hash: "abc123",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow,
            FailureReason: null);
    }

    private static SyncContext CreateTestContext(Document document)
    {
        return new SyncContext
        {
            UserId = Guid.NewGuid(),
            Document = document,
            AutoResolveConflicts = true,
            PublishEvents = true
        };
    }

    #endregion
}
