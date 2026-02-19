// =============================================================================
// File: EventRecordsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for sync event data types and ISyncEvent implementations.
// =============================================================================
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.Events;

/// <summary>
/// Unit tests for sync event data types and ISyncEvent implementations.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6j")]
public class EventRecordsTests
{
    #region SyncEventRecord Tests

    [Fact]
    public void SyncEventRecord_RequiredProperties_SetCorrectly()
    {
        // Arrange & Act
        var eventId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var publishedAt = DateTimeOffset.UtcNow;

        var record = new SyncEventRecord
        {
            EventId = eventId,
            EventType = "TestEvent",
            DocumentId = documentId,
            PublishedAt = publishedAt,
            Payload = "{}"
        };

        // Assert
        Assert.Equal(eventId, record.EventId);
        Assert.Equal("TestEvent", record.EventType);
        Assert.Equal(documentId, record.DocumentId);
        Assert.Equal(publishedAt, record.PublishedAt);
        Assert.Equal("{}", record.Payload);
    }

    [Fact]
    public void SyncEventRecord_OptionalProperties_HaveDefaults()
    {
        // Arrange & Act
        var record = new SyncEventRecord
        {
            EventId = Guid.NewGuid(),
            EventType = "TestEvent",
            DocumentId = Guid.NewGuid(),
            PublishedAt = DateTimeOffset.UtcNow,
            Payload = "{}"
        };

        // Assert
        Assert.Equal(0, record.HandlersExecuted);
        Assert.Equal(0, record.HandlersFailed);
        Assert.Equal(TimeSpan.Zero, record.TotalDuration);
        Assert.False(record.AllHandlersSucceeded);
        Assert.Empty(record.HandlerErrors);
    }

    #endregion

    #region SyncEventOptions Tests

    [Fact]
    public void SyncEventOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new SyncEventOptions();

        // Assert
        Assert.True(options.StoreInHistory);
        Assert.True(options.AwaitAll);
        Assert.True(options.CatchHandlerExceptions);
        Assert.Null(options.HandlerTimeout);
        Assert.Equal(EventPriority.Normal, options.Priority);
        Assert.False(options.AllowBatching);
        Assert.Empty(options.Tags);
        Assert.Empty(options.ContextData);
    }

    [Fact]
    public void SyncEventOptions_Default_IsSingleton()
    {
        // Act
        var options1 = SyncEventOptions.Default;
        var options2 = SyncEventOptions.Default;

        // Assert
        Assert.Same(options1, options2);
    }

    #endregion

    #region SyncEventSubscriptionOptions Tests

    [Fact]
    public void SyncEventSubscriptionOptions_RequiredProperties_SetCorrectly()
    {
        // Arrange & Act
        var options = new SyncEventSubscriptionOptions
        {
            EventType = typeof(SyncCompletedEvent)
        };

        // Assert
        Assert.Equal(typeof(SyncCompletedEvent), options.EventType);
    }

    [Fact]
    public void SyncEventSubscriptionOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new SyncEventSubscriptionOptions
        {
            EventType = typeof(SyncCompletedEvent)
        };

        // Assert
        Assert.Null(options.Filter);
        Assert.True(options.IsActive);
        Assert.Null(options.Name);
        Assert.True(options.CreatedAt <= DateTimeOffset.UtcNow);
    }

    #endregion

    #region SyncEventQuery Tests

    [Fact]
    public void SyncEventQuery_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var query = new SyncEventQuery();

        // Assert
        Assert.Null(query.EventType);
        Assert.Null(query.DocumentId);
        Assert.Null(query.PublishedAfter);
        Assert.Null(query.PublishedBefore);
        Assert.Null(query.SuccessfulOnly);
        Assert.Equal(EventSortOrder.ByPublishedDescending, query.SortOrder);
        Assert.Equal(100, query.PageSize);
        Assert.Equal(0, query.PageOffset);
    }

    #endregion

    #region SyncCompletedEvent Tests

    [Fact]
    public void SyncCompletedEvent_ImplementsISyncEvent()
    {
        // Assert
        Assert.True(typeof(ISyncEvent).IsAssignableFrom(typeof(SyncCompletedEvent)));
    }

    [Fact]
    public void SyncCompletedEvent_Create_SetsAllProperties()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var result = new SyncResult { Status = SyncOperationStatus.Success };
        var direction = SyncDirection.DocumentToGraph;

        // Act
        var e = SyncCompletedEvent.Create(documentId, result, direction);

        // Assert
        Assert.NotEqual(Guid.Empty, e.EventId);
        Assert.Equal(documentId, e.DocumentId);
        Assert.Equal(result, e.Result);
        Assert.Equal(direction, e.Direction);
        Assert.True(e.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void SyncCompletedEvent_ISyncEvent_PublishedAt_MapsToTimestamp()
    {
        // Arrange
        var e = SyncCompletedEvent.Create(
            Guid.NewGuid(),
            new SyncResult { Status = SyncOperationStatus.Success },
            SyncDirection.DocumentToGraph);

        // Act
        var publishedAt = ((ISyncEvent)e).PublishedAt;

        // Assert
        Assert.Equal(e.Timestamp, publishedAt);
    }

    #endregion

    #region SyncConflictDetectedEvent Tests

    [Fact]
    public void SyncConflictDetectedEvent_ImplementsISyncEvent()
    {
        // Assert
        Assert.True(typeof(ISyncEvent).IsAssignableFrom(typeof(SyncConflictDetectedEvent)));
    }

    [Fact]
    public void SyncConflictDetectedEvent_Create_SetsAllProperties()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var conflicts = new List<SyncConflict>
        {
            new SyncConflict
            {
                ConflictTarget = "Test",
                DocumentValue = "doc",
                GraphValue = "graph",
                DetectedAt = DateTimeOffset.UtcNow,
                Type = ConflictType.ValueMismatch
            }
        };

        // Act
        var e = SyncConflictDetectedEvent.Create(documentId, conflicts);

        // Assert
        Assert.NotEqual(Guid.Empty, e.EventId);
        Assert.Equal(documentId, e.DocumentId);
        Assert.Equal(1, e.ConflictCount);
        Assert.Single(e.Conflicts);
    }

    #endregion

    #region SyncConflictResolvedEvent Tests

    [Fact]
    public void SyncConflictResolvedEvent_ImplementsISyncEvent()
    {
        // Assert
        Assert.True(typeof(ISyncEvent).IsAssignableFrom(typeof(SyncConflictResolvedEvent)));
    }

    [Fact]
    public void SyncConflictResolvedEvent_Create_SetsAllProperties()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var conflict = new SyncConflict
        {
            ConflictTarget = "Test",
            DocumentValue = "doc",
            GraphValue = "graph",
            DetectedAt = DateTimeOffset.UtcNow,
            Type = ConflictType.ValueMismatch
        };

        // Act
        var e = SyncConflictResolvedEvent.Create(
            documentId,
            conflict,
            ConflictResolutionStrategy.UseDocument,
            "resolved value",
            Guid.NewGuid());

        // Assert
        Assert.NotEqual(Guid.Empty, e.EventId);
        Assert.Equal(documentId, e.DocumentId);
        Assert.Equal(conflict, e.Conflict);
        Assert.Equal(ConflictResolutionStrategy.UseDocument, e.Strategy);
        Assert.Equal("resolved value", e.ResolvedValue);
    }

    #endregion

    #region SyncStatusChangedEvent Tests

    [Fact]
    public void SyncStatusChangedEvent_ImplementsISyncEvent()
    {
        // Assert
        Assert.True(typeof(ISyncEvent).IsAssignableFrom(typeof(SyncStatusChangedEvent)));
    }

    [Fact]
    public void SyncStatusChangedEvent_Create_SetsAllProperties()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var e = SyncStatusChangedEvent.Create(
            documentId,
            SyncState.NeverSynced,
            SyncState.InSync,
            "Sync completed",
            Guid.NewGuid());

        // Assert
        Assert.NotEqual(Guid.Empty, e.EventId);
        Assert.Equal(documentId, e.DocumentId);
        Assert.Equal(SyncState.NeverSynced, e.PreviousState);
        Assert.Equal(SyncState.InSync, e.NewState);
        Assert.Equal("Sync completed", e.Reason);
    }

    #endregion

    #region SyncFailedEvent Tests

    [Fact]
    public void SyncFailedEvent_ImplementsISyncEvent()
    {
        // Assert
        Assert.True(typeof(ISyncEvent).IsAssignableFrom(typeof(SyncFailedEvent)));
    }

    [Fact]
    public void SyncFailedEvent_Create_SetsAllProperties()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var e = SyncFailedEvent.Create(
            documentId,
            "Connection failed",
            SyncDirection.DocumentToGraph,
            "SYNC-001",
            "Stack trace here",
            true);

        // Assert
        Assert.NotEqual(Guid.Empty, e.EventId);
        Assert.Equal(documentId, e.DocumentId);
        Assert.Equal("Connection failed", e.ErrorMessage);
        Assert.Equal("SYNC-001", e.ErrorCode);
        Assert.Equal("Stack trace here", e.ExceptionDetails);
        Assert.Equal(SyncDirection.DocumentToGraph, e.FailedDirection);
        Assert.True(e.RetryRecommended);
    }

    #endregion

    #region SyncRetryEvent Tests

    [Fact]
    public void SyncRetryEvent_ImplementsISyncEvent()
    {
        // Assert
        Assert.True(typeof(ISyncEvent).IsAssignableFrom(typeof(SyncRetryEvent)));
    }

    [Fact]
    public void SyncRetryEvent_Create_SetsAllProperties()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var e = SyncRetryEvent.Create(
            documentId,
            attemptNumber: 2,
            maxAttempts: 5,
            retryDelay: TimeSpan.FromSeconds(30),
            failureReason: "Timeout");

        // Assert
        Assert.NotEqual(Guid.Empty, e.EventId);
        Assert.Equal(documentId, e.DocumentId);
        Assert.Equal(2, e.AttemptNumber);
        Assert.Equal(5, e.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(30), e.RetryDelay);
        Assert.Equal("Timeout", e.FailureReason);
    }

    #endregion

    #region GraphToDocumentSyncedEvent Tests

    [Fact]
    public void GraphToDocumentSyncedEvent_ImplementsISyncEvent()
    {
        // Assert
        Assert.True(typeof(ISyncEvent).IsAssignableFrom(typeof(GraphToDocumentSyncedEvent)));
    }

    [Fact]
    public void GraphToDocumentSyncedEvent_Create_SetsAllProperties()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var graphChange = new GraphChange
        {
            EntityId = Guid.NewGuid(),
            ChangeType = ChangeType.EntityUpdated,
            NewValue = "new value",
            ChangedAt = DateTimeOffset.UtcNow
        };

        // Act
        var e = GraphToDocumentSyncedEvent.Create(documentId, graphChange);

        // Assert
        Assert.NotEqual(Guid.Empty, e.EventId);
        Assert.Equal(documentId, e.DocumentId);
        Assert.Equal(graphChange, e.TriggeringChange);
        Assert.Empty(e.AffectedDocuments);
        Assert.Empty(e.FlagsCreated);
        Assert.Equal(0, e.TotalAffectedDocuments);
    }

    #endregion

    #region EventPriority Enum Tests

    [Fact]
    public void EventPriority_Values_AreCorrect()
    {
        // Assert
        Assert.Equal(0, (int)EventPriority.Low);
        Assert.Equal(1, (int)EventPriority.Normal);
        Assert.Equal(2, (int)EventPriority.High);
        Assert.Equal(3, (int)EventPriority.Critical);
    }

    #endregion

    #region EventSortOrder Enum Tests

    [Fact]
    public void EventSortOrder_Values_AreCorrect()
    {
        // Assert
        Assert.Equal(0, (int)EventSortOrder.ByPublishedAscending);
        Assert.Equal(1, (int)EventSortOrder.ByPublishedDescending);
        Assert.Equal(2, (int)EventSortOrder.ByDocumentAscending);
        Assert.Equal(3, (int)EventSortOrder.ByDocumentDescending);
        Assert.Equal(4, (int)EventSortOrder.ByEventTypeAscending);
    }

    #endregion
}
