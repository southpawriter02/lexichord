// =============================================================================
// File: SyncStatusRecordsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for sync status records and events.
// =============================================================================
// v0.7.6i: Sync Status Tracker (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Status;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Status.Events;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.Status;

/// <summary>
/// Unit tests for sync status records and events.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6i")]
public class SyncStatusRecordsTests
{
    #region SortOrder Enum Tests

    [Fact]
    public void SortOrder_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)SortOrder.ByDocumentNameAscending);
        Assert.Equal(1, (int)SortOrder.ByDocumentNameDescending);
        Assert.Equal(2, (int)SortOrder.ByLastSyncAscending);
        Assert.Equal(3, (int)SortOrder.ByLastSyncDescending);
        Assert.Equal(4, (int)SortOrder.ByStateAscending);
        Assert.Equal(5, (int)SortOrder.ByStateDescending);
        Assert.Equal(6, (int)SortOrder.ByPendingChangesDescending);
    }

    #endregion

    #region SyncStatusQuery Record Tests

    [Fact]
    public void SyncStatusQuery_DefaultValues_AreCorrect()
    {
        // Act
        var query = new SyncStatusQuery();

        // Assert
        Assert.Null(query.State);
        Assert.Null(query.WorkspaceId);
        Assert.Null(query.LastSyncBefore);
        Assert.Null(query.LastSyncAfter);
        Assert.Null(query.MinPendingChanges);
        Assert.Null(query.MinUnresolvedConflicts);
        Assert.Null(query.SyncInProgress);
        Assert.Equal(SortOrder.ByLastSyncDescending, query.SortOrder);
        Assert.Equal(100, query.PageSize);
        Assert.Equal(0, query.PageOffset);
    }

    [Fact]
    public void SyncStatusQuery_CanSetAllProperties()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var lastSyncBefore = DateTimeOffset.UtcNow.AddDays(-7);
        var lastSyncAfter = DateTimeOffset.UtcNow.AddDays(-30);

        // Act
        var query = new SyncStatusQuery
        {
            State = SyncState.PendingSync,
            WorkspaceId = workspaceId,
            LastSyncBefore = lastSyncBefore,
            LastSyncAfter = lastSyncAfter,
            MinPendingChanges = 5,
            MinUnresolvedConflicts = 1,
            SyncInProgress = false,
            SortOrder = SortOrder.ByPendingChangesDescending,
            PageSize = 50,
            PageOffset = 10
        };

        // Assert
        Assert.Equal(SyncState.PendingSync, query.State);
        Assert.Equal(workspaceId, query.WorkspaceId);
        Assert.Equal(lastSyncBefore, query.LastSyncBefore);
        Assert.Equal(lastSyncAfter, query.LastSyncAfter);
        Assert.Equal(5, query.MinPendingChanges);
        Assert.Equal(1, query.MinUnresolvedConflicts);
        Assert.False(query.SyncInProgress);
        Assert.Equal(SortOrder.ByPendingChangesDescending, query.SortOrder);
        Assert.Equal(50, query.PageSize);
        Assert.Equal(10, query.PageOffset);
    }

    #endregion

    #region SyncStatusHistory Record Tests

    [Fact]
    public void SyncStatusHistory_RequiredProperties_MustBeSet()
    {
        // Arrange
        var historyId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var changedAt = DateTimeOffset.UtcNow;

        // Act
        var history = new SyncStatusHistory
        {
            HistoryId = historyId,
            DocumentId = documentId,
            PreviousState = SyncState.NeverSynced,
            NewState = SyncState.InSync,
            ChangedAt = changedAt
        };

        // Assert
        Assert.Equal(historyId, history.HistoryId);
        Assert.Equal(documentId, history.DocumentId);
        Assert.Equal(SyncState.NeverSynced, history.PreviousState);
        Assert.Equal(SyncState.InSync, history.NewState);
        Assert.Equal(changedAt, history.ChangedAt);
    }

    [Fact]
    public void SyncStatusHistory_OptionalProperties_DefaultCorrectly()
    {
        // Act
        var history = new SyncStatusHistory
        {
            HistoryId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            PreviousState = SyncState.NeverSynced,
            NewState = SyncState.InSync,
            ChangedAt = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.Null(history.ChangedBy);
        Assert.Null(history.Reason);
        Assert.Null(history.SyncOperationId);
        Assert.NotNull(history.Metadata);
        Assert.Empty(history.Metadata);
    }

    #endregion

    #region SyncOperationRecord Record Tests

    [Fact]
    public void SyncOperationRecord_RequiredProperties_MustBeSet()
    {
        // Arrange
        var operationId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow;

        // Act
        var record = new SyncOperationRecord
        {
            OperationId = operationId,
            DocumentId = documentId,
            Direction = SyncDirection.DocumentToGraph,
            Status = SyncOperationStatus.Success,
            StartedAt = startedAt
        };

        // Assert
        Assert.Equal(operationId, record.OperationId);
        Assert.Equal(documentId, record.DocumentId);
        Assert.Equal(SyncDirection.DocumentToGraph, record.Direction);
        Assert.Equal(SyncOperationStatus.Success, record.Status);
        Assert.Equal(startedAt, record.StartedAt);
    }

    [Fact]
    public void SyncOperationRecord_OptionalProperties_DefaultCorrectly()
    {
        // Act
        var record = new SyncOperationRecord
        {
            OperationId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            Direction = SyncDirection.DocumentToGraph,
            Status = SyncOperationStatus.Success,
            StartedAt = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.Null(record.CompletedAt);
        Assert.Null(record.Duration);
        Assert.Null(record.InitiatedBy);
        Assert.Equal(0, record.EntitiesAffected);
        Assert.Equal(0, record.ClaimsAffected);
        Assert.Equal(0, record.RelationshipsAffected);
        Assert.Equal(0, record.ConflictsDetected);
        Assert.Equal(0, record.ConflictsResolved);
        Assert.Null(record.ErrorMessage);
        Assert.Null(record.ErrorCode);
        Assert.NotNull(record.Metadata);
        Assert.Empty(record.Metadata);
    }

    #endregion

    #region SyncMetrics Record Tests

    [Fact]
    public void SyncMetrics_RequiredProperties_MustBeSet()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var metrics = new SyncMetrics
        {
            DocumentId = documentId,
            CurrentState = SyncState.InSync
        };

        // Assert
        Assert.Equal(documentId, metrics.DocumentId);
        Assert.Equal(SyncState.InSync, metrics.CurrentState);
    }

    [Fact]
    public void SyncMetrics_ComputedProperties_DefaultCorrectly()
    {
        // Act
        var metrics = new SyncMetrics
        {
            DocumentId = Guid.NewGuid(),
            CurrentState = SyncState.InSync
        };

        // Assert
        Assert.Equal(0, metrics.TotalOperations);
        Assert.Equal(0, metrics.SuccessfulOperations);
        Assert.Equal(0, metrics.FailedOperations);
        Assert.Equal(TimeSpan.Zero, metrics.AverageDuration);
        Assert.Null(metrics.LongestDuration);
        Assert.Null(metrics.ShortestDuration);
        Assert.Null(metrics.LastSuccessfulSync);
        Assert.Null(metrics.LastFailedSync);
        Assert.Equal(0, metrics.TotalConflicts);
        Assert.Equal(0, metrics.ResolvedConflicts);
        Assert.Equal(0, metrics.UnresolvedConflicts);
        Assert.Equal(0, metrics.SuccessRate);
        Assert.Equal(0, metrics.AverageEntitiesAffected);
        Assert.Equal(0, metrics.AverageClaimsAffected);
        Assert.Equal(TimeSpan.Zero, metrics.TimeInCurrentState);
    }

    [Fact]
    public void SyncMetrics_CanSetAllProperties()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var lastSuccess = DateTimeOffset.UtcNow.AddHours(-1);
        var lastFailed = DateTimeOffset.UtcNow.AddHours(-2);

        // Act
        var metrics = new SyncMetrics
        {
            DocumentId = documentId,
            TotalOperations = 100,
            SuccessfulOperations = 90,
            FailedOperations = 10,
            AverageDuration = TimeSpan.FromSeconds(30),
            LongestDuration = TimeSpan.FromMinutes(2),
            ShortestDuration = TimeSpan.FromSeconds(5),
            LastSuccessfulSync = lastSuccess,
            LastFailedSync = lastFailed,
            TotalConflicts = 15,
            ResolvedConflicts = 12,
            UnresolvedConflicts = 3,
            SuccessRate = 90.0f,
            AverageEntitiesAffected = 5.5f,
            AverageClaimsAffected = 2.3f,
            CurrentState = SyncState.InSync,
            TimeInCurrentState = TimeSpan.FromHours(1)
        };

        // Assert
        Assert.Equal(100, metrics.TotalOperations);
        Assert.Equal(90, metrics.SuccessfulOperations);
        Assert.Equal(10, metrics.FailedOperations);
        Assert.Equal(TimeSpan.FromSeconds(30), metrics.AverageDuration);
        Assert.Equal(TimeSpan.FromMinutes(2), metrics.LongestDuration);
        Assert.Equal(TimeSpan.FromSeconds(5), metrics.ShortestDuration);
        Assert.Equal(lastSuccess, metrics.LastSuccessfulSync);
        Assert.Equal(lastFailed, metrics.LastFailedSync);
        Assert.Equal(15, metrics.TotalConflicts);
        Assert.Equal(12, metrics.ResolvedConflicts);
        Assert.Equal(3, metrics.UnresolvedConflicts);
        Assert.Equal(90.0f, metrics.SuccessRate);
        Assert.Equal(5.5f, metrics.AverageEntitiesAffected);
        Assert.Equal(2.3f, metrics.AverageClaimsAffected);
        Assert.Equal(TimeSpan.FromHours(1), metrics.TimeInCurrentState);
    }

    #endregion

    #region SyncStatusUpdatedEvent Tests

    [Fact]
    public void SyncStatusUpdatedEvent_Create_SetsAllProperties()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();

        // Act
        var evt = SyncStatusUpdatedEvent.Create(
            documentId,
            SyncState.NeverSynced,
            SyncState.InSync,
            changedBy);

        // Assert
        Assert.Equal(documentId, evt.DocumentId);
        Assert.Equal(SyncState.NeverSynced, evt.PreviousState);
        Assert.Equal(SyncState.InSync, evt.NewState);
        Assert.Equal(changedBy, evt.ChangedBy);
        Assert.True(evt.Timestamp <= DateTimeOffset.UtcNow);
        Assert.True(evt.Timestamp >= DateTimeOffset.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public void SyncStatusUpdatedEvent_Create_WithoutChangedBy_SetsNull()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var evt = SyncStatusUpdatedEvent.Create(
            documentId,
            SyncState.NeverSynced,
            SyncState.InSync);

        // Assert
        Assert.Null(evt.ChangedBy);
    }

    [Fact]
    public void SyncStatusUpdatedEvent_DefaultTimestamp_IsUtcNow()
    {
        // Arrange
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var evt = new SyncStatusUpdatedEvent
        {
            DocumentId = Guid.NewGuid(),
            PreviousState = SyncState.NeverSynced,
            NewState = SyncState.InSync
        };

        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(evt.Timestamp >= beforeCreation);
        Assert.True(evt.Timestamp <= afterCreation);
    }

    #endregion
}
