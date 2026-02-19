// =============================================================================
// File: SyncStatusRepositoryTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SyncStatusRepository.
// =============================================================================
// v0.7.6i: Sync Status Tracker (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Status;
using Lexichord.Modules.Knowledge.Sync.Status;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.Status;

/// <summary>
/// Unit tests for <see cref="SyncStatusRepository"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6i")]
public class SyncStatusRepositoryTests
{
    private readonly Mock<ILogger<SyncStatusRepository>> _mockLogger;
    private readonly SyncStatusRepository _sut;

    public SyncStatusRepositoryTests()
    {
        _mockLogger = new Mock<ILogger<SyncStatusRepository>>();
        _sut = new SyncStatusRepository(_mockLogger.Object);
    }

    #region Status CRUD Tests

    [Fact]
    public async Task GetAsync_NonExistentDocument_ReturnsNull()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var result = await _sut.GetAsync(documentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_NewStatus_StoresAndReturnsStatus()
    {
        // Arrange
        var status = CreateStatus(Guid.NewGuid(), SyncState.NeverSynced);

        // Act
        var result = await _sut.CreateAsync(status);

        // Assert
        Assert.Equal(status.DocumentId, result.DocumentId);
        Assert.Equal(status.State, result.State);

        // Verify it's retrievable
        var retrieved = await _sut.GetAsync(status.DocumentId);
        Assert.NotNull(retrieved);
        Assert.Equal(status.State, retrieved.State);
    }

    [Fact]
    public async Task UpdateAsync_ExistingStatus_UpdatesAndReturns()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var initialStatus = CreateStatus(documentId, SyncState.NeverSynced);
        await _sut.CreateAsync(initialStatus);

        var updatedStatus = initialStatus with { State = SyncState.InSync };

        // Act
        var result = await _sut.UpdateAsync(updatedStatus);

        // Assert
        Assert.Equal(SyncState.InSync, result.State);

        var retrieved = await _sut.GetAsync(documentId);
        Assert.NotNull(retrieved);
        Assert.Equal(SyncState.InSync, retrieved.State);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentStatus_CreatesNew()
    {
        // Arrange
        var status = CreateStatus(Guid.NewGuid(), SyncState.InSync);

        // Act
        var result = await _sut.UpdateAsync(status);

        // Assert
        Assert.Equal(status.State, result.State);

        var retrieved = await _sut.GetAsync(status.DocumentId);
        Assert.NotNull(retrieved);
    }

    [Fact]
    public async Task DeleteAsync_ExistingStatus_DeletesAndReturnsTrue()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        await _sut.CreateAsync(CreateStatus(documentId, SyncState.InSync));

        // Act
        var result = await _sut.DeleteAsync(documentId);

        // Assert
        Assert.True(result);

        var retrieved = await _sut.GetAsync(documentId);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentStatus_ReturnsFalse()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var result = await _sut.DeleteAsync(documentId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task QueryAsync_ByState_ReturnsMatchingStatuses()
    {
        // Arrange
        var inSyncDoc1 = CreateStatus(Guid.NewGuid(), SyncState.InSync);
        var inSyncDoc2 = CreateStatus(Guid.NewGuid(), SyncState.InSync);
        var pendingDoc = CreateStatus(Guid.NewGuid(), SyncState.PendingSync);

        await _sut.CreateAsync(inSyncDoc1);
        await _sut.CreateAsync(inSyncDoc2);
        await _sut.CreateAsync(pendingDoc);

        // Act
        var results = await _sut.QueryAsync(new SyncStatusQuery { State = SyncState.InSync });

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(SyncState.InSync, r.State));
    }

    [Fact]
    public async Task QueryAsync_ByMinPendingChanges_ReturnsMatchingStatuses()
    {
        // Arrange
        var lowChanges = CreateStatus(Guid.NewGuid()) with { PendingChanges = 2 };
        var highChanges = CreateStatus(Guid.NewGuid()) with { PendingChanges = 10 };

        await _sut.CreateAsync(lowChanges);
        await _sut.CreateAsync(highChanges);

        // Act
        var results = await _sut.QueryAsync(new SyncStatusQuery { MinPendingChanges = 5 });

        // Assert
        Assert.Single(results);
        Assert.Equal(10, results[0].PendingChanges);
    }

    [Fact]
    public async Task QueryAsync_BySyncInProgress_ReturnsMatchingStatuses()
    {
        // Arrange
        var syncing = CreateStatus(Guid.NewGuid()) with { IsSyncInProgress = true };
        var idle = CreateStatus(Guid.NewGuid()) with { IsSyncInProgress = false };

        await _sut.CreateAsync(syncing);
        await _sut.CreateAsync(idle);

        // Act
        var results = await _sut.QueryAsync(new SyncStatusQuery { SyncInProgress = true });

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSyncInProgress);
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            await _sut.CreateAsync(CreateStatus(Guid.NewGuid(), SyncState.InSync));
        }

        // Act
        var page1 = await _sut.QueryAsync(new SyncStatusQuery { PageSize = 3, PageOffset = 0 });
        var page2 = await _sut.QueryAsync(new SyncStatusQuery { PageSize = 3, PageOffset = 3 });

        // Assert
        Assert.Equal(3, page1.Count);
        Assert.Equal(3, page2.Count);
    }

    [Fact]
    public async Task QueryAsync_WithSortOrder_ReturnsSortedResults()
    {
        // Arrange
        var doc1 = CreateStatus(Guid.NewGuid()) with { PendingChanges = 1 };
        var doc2 = CreateStatus(Guid.NewGuid()) with { PendingChanges = 10 };
        var doc3 = CreateStatus(Guid.NewGuid()) with { PendingChanges = 5 };

        await _sut.CreateAsync(doc1);
        await _sut.CreateAsync(doc2);
        await _sut.CreateAsync(doc3);

        // Act
        var results = await _sut.QueryAsync(new SyncStatusQuery
        {
            SortOrder = SortOrder.ByPendingChangesDescending
        });

        // Assert
        Assert.Equal(10, results[0].PendingChanges);
        Assert.Equal(5, results[1].PendingChanges);
        Assert.Equal(1, results[2].PendingChanges);
    }

    [Fact]
    public async Task CountByStateAsync_ReturnsCorrectCounts()
    {
        // Arrange
        await _sut.CreateAsync(CreateStatus(Guid.NewGuid(), SyncState.InSync));
        await _sut.CreateAsync(CreateStatus(Guid.NewGuid(), SyncState.InSync));
        await _sut.CreateAsync(CreateStatus(Guid.NewGuid(), SyncState.PendingSync));
        await _sut.CreateAsync(CreateStatus(Guid.NewGuid(), SyncState.Conflict));

        // Act
        var counts = await _sut.CountByStateAsync();

        // Assert
        Assert.Equal(2, counts[SyncState.InSync]);
        Assert.Equal(1, counts[SyncState.PendingSync]);
        Assert.Equal(1, counts[SyncState.Conflict]);
    }

    #endregion

    #region History Tests

    [Fact]
    public async Task AddHistoryAsync_StoresHistoryEntry()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var history = new SyncStatusHistory
        {
            HistoryId = Guid.NewGuid(),
            DocumentId = documentId,
            PreviousState = SyncState.NeverSynced,
            NewState = SyncState.InSync,
            ChangedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _sut.AddHistoryAsync(history);
        var results = await _sut.GetHistoryAsync(documentId);

        // Assert
        Assert.Single(results);
        Assert.Equal(SyncState.NeverSynced, results[0].PreviousState);
        Assert.Equal(SyncState.InSync, results[0].NewState);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsInDescendingOrder()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        var history1 = new SyncStatusHistory
        {
            HistoryId = Guid.NewGuid(),
            DocumentId = documentId,
            PreviousState = SyncState.NeverSynced,
            NewState = SyncState.PendingSync,
            ChangedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        };

        var history2 = new SyncStatusHistory
        {
            HistoryId = Guid.NewGuid(),
            DocumentId = documentId,
            PreviousState = SyncState.PendingSync,
            NewState = SyncState.InSync,
            ChangedAt = DateTimeOffset.UtcNow
        };

        await _sut.AddHistoryAsync(history1);
        await _sut.AddHistoryAsync(history2);

        // Act
        var results = await _sut.GetHistoryAsync(documentId);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(SyncState.InSync, results[0].NewState); // Most recent first
        Assert.Equal(SyncState.PendingSync, results[1].NewState);
    }

    [Fact]
    public async Task GetHistoryAsync_RespectsLimit()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        for (int i = 0; i < 10; i++)
        {
            await _sut.AddHistoryAsync(new SyncStatusHistory
            {
                HistoryId = Guid.NewGuid(),
                DocumentId = documentId,
                PreviousState = SyncState.NeverSynced,
                NewState = SyncState.InSync,
                ChangedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        // Act
        var results = await _sut.GetHistoryAsync(documentId, limit: 5);

        // Assert
        Assert.Equal(5, results.Count);
    }

    #endregion

    #region Operation Record Tests

    [Fact]
    public async Task AddOperationRecordAsync_StoresRecord()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var record = new SyncOperationRecord
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
        await _sut.AddOperationRecordAsync(record);
        var results = await _sut.GetOperationRecordsAsync(documentId);

        // Assert
        Assert.Single(results);
        Assert.Equal(SyncOperationStatus.Success, results[0].Status);
        Assert.Equal(10, results[0].EntitiesAffected);
    }

    [Fact]
    public async Task GetOperationRecordsAsync_ReturnsInDescendingOrder()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        var record1 = new SyncOperationRecord
        {
            OperationId = Guid.NewGuid(),
            DocumentId = documentId,
            Direction = SyncDirection.DocumentToGraph,
            Status = SyncOperationStatus.Success,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        };

        var record2 = new SyncOperationRecord
        {
            OperationId = Guid.NewGuid(),
            DocumentId = documentId,
            Direction = SyncDirection.DocumentToGraph,
            Status = SyncOperationStatus.Failed,
            StartedAt = DateTimeOffset.UtcNow
        };

        await _sut.AddOperationRecordAsync(record1);
        await _sut.AddOperationRecordAsync(record2);

        // Act
        var results = await _sut.GetOperationRecordsAsync(documentId);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(SyncOperationStatus.Failed, results[0].Status); // Most recent first
        Assert.Equal(SyncOperationStatus.Success, results[1].Status);
    }

    [Fact]
    public async Task GetOperationRecordsAsync_RespectsLimit()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        for (int i = 0; i < 10; i++)
        {
            await _sut.AddOperationRecordAsync(new SyncOperationRecord
            {
                OperationId = Guid.NewGuid(),
                DocumentId = documentId,
                Direction = SyncDirection.DocumentToGraph,
                Status = SyncOperationStatus.Success,
                StartedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        // Act
        var results = await _sut.GetOperationRecordsAsync(documentId, limit: 5);

        // Assert
        Assert.Equal(5, results.Count);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ConcurrentCreates_AllSucceed()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var status = CreateStatus(Guid.NewGuid(), SyncState.InSync);
                await _sut.CreateAsync(status);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var counts = await _sut.CountByStateAsync();
        Assert.Equal(100, counts.GetValueOrDefault(SyncState.InSync, 0));
    }

    [Fact]
    public async Task ConcurrentUpdates_LastWins()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        await _sut.CreateAsync(CreateStatus(documentId, SyncState.NeverSynced));

        var tasks = new List<Task>();

        // Act - Multiple concurrent updates
        for (int i = 0; i < 10; i++)
        {
            var state = i % 2 == 0 ? SyncState.InSync : SyncState.PendingSync;
            tasks.Add(Task.Run(async () =>
            {
                await _sut.UpdateAsync(CreateStatus(documentId, state));
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Should not throw, status should be one of the two states
        var result = await _sut.GetAsync(documentId);
        Assert.NotNull(result);
        Assert.True(result.State == SyncState.InSync || result.State == SyncState.PendingSync);
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
