// =============================================================================
// File: SyncStatusTrackerTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SyncStatusTracker.
// =============================================================================
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// v0.7.6i: Enhanced with repository, history, metrics, and event tests.
// =============================================================================

using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Status;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Status.Events;
using Lexichord.Modules.Knowledge.Sync;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync;

/// <summary>
/// Unit tests for <see cref="SyncStatusTracker"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6i")]
public class SyncStatusTrackerTests
{
    private readonly Mock<ISyncStatusRepository> _mockRepository;
    private readonly Mock<ILicenseContext> _mockLicenseContext;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<SyncStatusTracker>> _mockLogger;
    private readonly SyncStatusTracker _sut;

    public SyncStatusTrackerTests()
    {
        _mockRepository = new Mock<ISyncStatusRepository>();
        _mockLicenseContext = new Mock<ILicenseContext>();
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<SyncStatusTracker>>();

        // Default license context setup - Teams tier
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);
        _mockLicenseContext.Setup(x => x.IsFeatureEnabled(FeatureCodes.SyncStatusTracker))
            .Returns(true);

        _sut = new SyncStatusTracker(
            _mockRepository.Object,
            _mockLicenseContext.Object,
            _mockMediator.Object,
            _mockLogger.Object);
    }

    #region GetStatusAsync Tests

    [Fact]
    public async Task GetStatusAsync_NewDocument_ReturnsNeverSyncedStatus()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _mockRepository.Setup(x => x.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncStatus?)null);
        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<SyncStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncStatus s, CancellationToken _) => s);

        // Act
        var result = await _sut.GetStatusAsync(documentId);

        // Assert
        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(SyncState.NeverSynced, result.State);
        Assert.Null(result.LastSyncAt);
        Assert.Equal(0, result.PendingChanges);
        Assert.False(result.IsSyncInProgress);

        // Verify repository was called
        _mockRepository.Verify(x => x.CreateAsync(
            It.Is<SyncStatus>(s => s.DocumentId == documentId && s.State == SyncState.NeverSynced),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStatusAsync_ExistingDocument_ReturnsStoredStatus()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var existingStatus = new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.InSync,
            LastSyncAt = DateTimeOffset.UtcNow.AddHours(-1),
            PendingChanges = 0
        };

        _mockRepository.Setup(x => x.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStatus);

        // Act
        var result = await _sut.GetStatusAsync(documentId);

        // Assert
        Assert.Equal(SyncState.InSync, result.State);
        Assert.NotNull(result.LastSyncAt);
    }

    [Fact]
    public async Task GetStatusAsync_CachesResult_SecondCallDoesNotHitRepository()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var existingStatus = new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.InSync
        };

        _mockRepository.Setup(x => x.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStatus);

        // Act
        await _sut.GetStatusAsync(documentId);
        await _sut.GetStatusAsync(documentId);

        // Assert - Repository should only be called once due to caching
        _mockRepository.Verify(x => x.GetAsync(documentId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region UpdateStatusAsync Tests

    [Fact]
    public async Task UpdateStatusAsync_StoresStatus()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var currentStatus = new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.NeverSynced
        };
        var newStatus = new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.InSync,
            LastSyncAt = DateTimeOffset.UtcNow,
            PendingChanges = 0
        };

        _mockRepository.Setup(x => x.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentStatus);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<SyncStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncStatus s, CancellationToken _) => s);

        // Act
        var result = await _sut.UpdateStatusAsync(documentId, newStatus);

        // Assert
        Assert.Equal(SyncState.InSync, result.State);
        Assert.NotNull(result.LastSyncAt);

        // Verify repository update was called
        _mockRepository.Verify(x => x.UpdateAsync(
            It.Is<SyncStatus>(s => s.State == SyncState.InSync),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithMismatchedId_ThrowsArgumentException()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var status = new SyncStatus
        {
            DocumentId = Guid.NewGuid(), // Different ID
            State = SyncState.InSync
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.UpdateStatusAsync(documentId, status));
    }

    [Fact]
    public async Task UpdateStatusAsync_StateChanged_RecordsHistory()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var currentStatus = new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.NeverSynced
        };
        var newStatus = new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.InSync
        };

        _mockRepository.Setup(x => x.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentStatus);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<SyncStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncStatus s, CancellationToken _) => s);

        // Act
        await _sut.UpdateStatusAsync(documentId, newStatus);

        // Assert - Verify history was recorded
        _mockRepository.Verify(x => x.AddHistoryAsync(
            It.Is<SyncStatusHistory>(h =>
                h.DocumentId == documentId &&
                h.PreviousState == SyncState.NeverSynced &&
                h.NewState == SyncState.InSync),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_StateChanged_PublishesEvent()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var currentStatus = new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.NeverSynced
        };
        var newStatus = new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.InSync
        };

        _mockRepository.Setup(x => x.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentStatus);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<SyncStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncStatus s, CancellationToken _) => s);

        // Act
        await _sut.UpdateStatusAsync(documentId, newStatus);

        // Assert - Verify event was published
        _mockMediator.Verify(x => x.Publish(
            It.Is<SyncStatusUpdatedEvent>(e =>
                e.DocumentId == documentId &&
                e.PreviousState == SyncState.NeverSynced &&
                e.NewState == SyncState.InSync),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_SameState_DoesNotRecordHistory()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var currentStatus = new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.InSync,
            PendingChanges = 0
        };
        var newStatus = new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.InSync,
            PendingChanges = 5 // Only pending changes changed, not state
        };

        _mockRepository.Setup(x => x.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentStatus);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<SyncStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyncStatus s, CancellationToken _) => s);

        // Act
        await _sut.UpdateStatusAsync(documentId, newStatus);

        // Assert - Verify history was NOT recorded (state didn't change)
        _mockRepository.Verify(x => x.AddHistoryAsync(
            It.IsAny<SyncStatusHistory>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Batch Operations Tests (v0.7.6i)

    [Fact]
    public async Task UpdateStatusBatchAsync_UpdatesMultipleDocuments()
    {
        // Arrange
        var updates = new List<(Guid, SyncStatus)>
        {
            (Guid.NewGuid(), new SyncStatus { DocumentId = Guid.Empty, State = SyncState.InSync }),
            (Guid.NewGuid(), new SyncStatus { DocumentId = Guid.Empty, State = SyncState.PendingSync }),
            (Guid.NewGuid(), new SyncStatus { DocumentId = Guid.Empty, State = SyncState.Conflict })
        };

        // Fix document IDs to match
        updates = updates.Select(u => (u.Item1, u.Item2 with { DocumentId = u.Item1 })).ToList();

        foreach (var (docId, status) in updates)
        {
            _mockRepository.Setup(x => x.GetAsync(docId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SyncStatus { DocumentId = docId, State = SyncState.NeverSynced });
            _mockRepository.Setup(x => x.UpdateAsync(It.Is<SyncStatus>(s => s.DocumentId == docId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SyncStatus s, CancellationToken _) => s);
        }

        // Act
        var results = await _sut.UpdateStatusBatchAsync(updates, CancellationToken.None);

        // Assert
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task UpdateStatusBatchAsync_WithoutLicense_ThrowsUnauthorized()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.IsFeatureEnabled(FeatureCodes.SyncStatusTracker))
            .Returns(false);

        var updates = new List<(Guid, SyncStatus)>
        {
            (Guid.NewGuid(), new SyncStatus { DocumentId = Guid.Empty, State = SyncState.InSync })
        };
        updates = updates.Select(u => (u.Item1, u.Item2 with { DocumentId = u.Item1 })).ToList();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UpdateStatusBatchAsync(updates, CancellationToken.None));
    }

    [Fact]
    public async Task GetStatusesAsync_ReturnsAllStatuses()
    {
        // Arrange
        var documentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        foreach (var docId in documentIds)
        {
            _mockRepository.Setup(x => x.GetAsync(docId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SyncStatus { DocumentId = docId, State = SyncState.InSync });
        }

        // Act
        var results = await _sut.GetStatusesAsync(documentIds, CancellationToken.None);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal(SyncState.InSync, r.State));
    }

    [Fact]
    public async Task GetDocumentsByStateAsync_ReturnsCorrectDocuments()
    {
        // Arrange
        var expectedDocs = new List<SyncStatus>
        {
            new() { DocumentId = Guid.NewGuid(), State = SyncState.PendingSync },
            new() { DocumentId = Guid.NewGuid(), State = SyncState.PendingSync }
        };

        _mockRepository.Setup(x => x.QueryAsync(
            It.Is<SyncStatusQuery>(q => q.State == SyncState.PendingSync),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocs);

        // Act
        var results = await _sut.GetDocumentsByStateAsync(SyncState.PendingSync);

        // Assert
        Assert.Equal(2, results.Count);
    }

    #endregion

    #region History Operations Tests (v0.7.6i)

    [Fact]
    public async Task GetStatusHistoryAsync_ReturnsHistory()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var history = new List<SyncStatusHistory>
        {
            new()
            {
                HistoryId = Guid.NewGuid(),
                DocumentId = documentId,
                PreviousState = SyncState.NeverSynced,
                NewState = SyncState.InSync,
                ChangedAt = DateTimeOffset.UtcNow
            }
        };

        _mockRepository.Setup(x => x.GetHistoryAsync(documentId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var results = await _sut.GetStatusHistoryAsync(documentId);

        // Assert
        Assert.Single(results);
        Assert.Equal(SyncState.NeverSynced, results[0].PreviousState);
        Assert.Equal(SyncState.InSync, results[0].NewState);
    }

    [Fact]
    public async Task GetStatusHistoryAsync_AppliesRetentionLimit_ForWriterPro()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.WriterPro);

        var documentId = Guid.NewGuid();
        _mockRepository.Setup(x => x.GetHistoryAsync(documentId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncStatusHistory>());

        // Act
        await _sut.GetStatusHistoryAsync(documentId, limit: 500);

        // Assert - WriterPro limit is 100
        _mockRepository.Verify(x => x.GetHistoryAsync(documentId, 100, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStatusHistoryAsync_AppliesRetentionLimit_ForTeams()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);

        var documentId = Guid.NewGuid();
        _mockRepository.Setup(x => x.GetHistoryAsync(documentId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncStatusHistory>());

        // Act
        await _sut.GetStatusHistoryAsync(documentId, limit: 500);

        // Assert - Teams limit is 300
        _mockRepository.Verify(x => x.GetHistoryAsync(documentId, 300, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStatusHistoryAsync_NoLimit_ForEnterprise()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);

        var documentId = Guid.NewGuid();
        _mockRepository.Setup(x => x.GetHistoryAsync(documentId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncStatusHistory>());

        // Act
        await _sut.GetStatusHistoryAsync(documentId, limit: 500);

        // Assert - Enterprise has no limit
        _mockRepository.Verify(x => x.GetHistoryAsync(documentId, 500, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Metrics Operations Tests (v0.7.6i)

    [Fact]
    public async Task GetMetricsAsync_ComputesCorrectMetrics()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var status = new SyncStatus
        {
            DocumentId = documentId,
            State = SyncState.InSync,
            UnresolvedConflicts = 2
        };

        var operations = new List<SyncOperationRecord>
        {
            new()
            {
                OperationId = Guid.NewGuid(),
                DocumentId = documentId,
                Direction = SyncDirection.DocumentToGraph,
                Status = SyncOperationStatus.Success,
                StartedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
                CompletedAt = DateTimeOffset.UtcNow.AddMinutes(-9),
                Duration = TimeSpan.FromMinutes(1),
                EntitiesAffected = 5,
                ClaimsAffected = 3,
                ConflictsDetected = 1,
                ConflictsResolved = 1
            },
            new()
            {
                OperationId = Guid.NewGuid(),
                DocumentId = documentId,
                Direction = SyncDirection.DocumentToGraph,
                Status = SyncOperationStatus.Failed,
                StartedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                CompletedAt = DateTimeOffset.UtcNow.AddMinutes(-4),
                Duration = TimeSpan.FromMinutes(1),
                EntitiesAffected = 0,
                ErrorMessage = "Test error"
            }
        };

        _mockRepository.Setup(x => x.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);
        _mockRepository.Setup(x => x.GetOperationRecordsAsync(documentId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(operations);
        _mockRepository.Setup(x => x.GetHistoryAsync(documentId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncStatusHistory>());

        // Act
        var metrics = await _sut.GetMetricsAsync(documentId);

        // Assert
        Assert.Equal(documentId, metrics.DocumentId);
        Assert.Equal(2, metrics.TotalOperations);
        Assert.Equal(1, metrics.SuccessfulOperations);
        Assert.Equal(1, metrics.FailedOperations);
        Assert.Equal(50, metrics.SuccessRate);
        Assert.Equal(SyncState.InSync, metrics.CurrentState);
        Assert.Equal(2, metrics.UnresolvedConflicts);
    }

    [Fact]
    public async Task RecordSyncOperationAsync_StoresOperation()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var operation = new SyncOperationRecord
        {
            OperationId = Guid.NewGuid(),
            DocumentId = documentId,
            Direction = SyncDirection.DocumentToGraph,
            Status = SyncOperationStatus.Success,
            StartedAt = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromSeconds(30),
            EntitiesAffected = 10
        };

        // Act
        await _sut.RecordSyncOperationAsync(documentId, operation);

        // Assert
        _mockRepository.Verify(x => x.AddOperationRecordAsync(operation, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordSyncOperationAsync_WithMismatchedId_ThrowsArgumentException()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var operation = new SyncOperationRecord
        {
            OperationId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(), // Different ID
            Direction = SyncDirection.DocumentToGraph,
            Status = SyncOperationStatus.Success,
            StartedAt = DateTimeOffset.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.RecordSyncOperationAsync(documentId, operation));
    }

    #endregion

    #region Helper Methods

    private static SyncStatus CreateStatus(Guid documentId, SyncState state = SyncState.NeverSynced) =>
        new()
        {
            DocumentId = documentId,
            State = state,
            LastSyncAt = null,
            PendingChanges = 0,
            UnresolvedConflicts = 0,
            IsSyncInProgress = false
        };

    #endregion
}
