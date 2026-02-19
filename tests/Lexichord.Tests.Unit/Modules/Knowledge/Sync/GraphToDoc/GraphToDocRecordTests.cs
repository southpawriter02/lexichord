// =============================================================================
// File: GraphToDocRecordTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for v0.7.6g records and events.
// =============================================================================
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc.Events;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Unit tests for v0.7.6g records and events.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6g")]
public class GraphToDocRecordTests
{
    #region AffectedDocument Tests

    [Fact]
    public void AffectedDocument_RequiredProperties_AreInitialized()
    {
        // Act
        var doc = new AffectedDocument
        {
            DocumentId = Guid.NewGuid(),
            DocumentName = "TestDoc",
            Relationship = DocumentEntityRelationship.ExplicitReference
        };

        // Assert
        Assert.NotEqual(Guid.Empty, doc.DocumentId);
        Assert.Equal("TestDoc", doc.DocumentName);
        Assert.Equal(DocumentEntityRelationship.ExplicitReference, doc.Relationship);
    }

    [Fact]
    public void AffectedDocument_OptionalProperties_HaveDefaults()
    {
        // Act
        var doc = new AffectedDocument
        {
            DocumentId = Guid.NewGuid(),
            DocumentName = "Test",
            Relationship = DocumentEntityRelationship.DerivedFrom
        };

        // Assert
        Assert.Equal(0, doc.ReferenceCount);
        Assert.Null(doc.SuggestedAction);
        Assert.Null(doc.LastSyncedAt);
    }

    #endregion

    #region DocumentFlag Tests

    [Fact]
    public void DocumentFlag_RequiredProperties_AreInitialized()
    {
        // Act
        var flag = new DocumentFlag
        {
            FlagId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            TriggeringEntityId = Guid.NewGuid(),
            Reason = FlagReason.EntityDeleted,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.NotEqual(Guid.Empty, flag.FlagId);
        Assert.NotEqual(Guid.Empty, flag.DocumentId);
        Assert.Equal(FlagReason.EntityDeleted, flag.Reason);
    }

    [Fact]
    public void DocumentFlag_Defaults_AreCorrect()
    {
        // Act
        var flag = new DocumentFlag
        {
            FlagId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            TriggeringEntityId = Guid.NewGuid(),
            Reason = FlagReason.EntityValueChanged,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.Equal(string.Empty, flag.Description);
        Assert.Equal(FlagPriority.Medium, flag.Priority);
        Assert.Equal(FlagStatus.Pending, flag.Status);
        Assert.Null(flag.ResolvedAt);
        Assert.Null(flag.ResolvedBy);
        Assert.Null(flag.Resolution);
        Assert.False(flag.NotificationSent);
    }

    #endregion

    #region GraphToDocSyncResult Tests

    [Fact]
    public void GraphToDocSyncResult_Defaults_AreCorrect()
    {
        // Act
        var result = new GraphToDocSyncResult
        {
            Status = SyncOperationStatus.Success,
            TriggeringChange = CreateTestChange()
        };

        // Assert
        Assert.Empty(result.AffectedDocuments);
        Assert.Empty(result.FlagsCreated);
        Assert.Equal(0, result.TotalDocumentsNotified);
        Assert.Null(result.ErrorMessage);
    }

    #endregion

    #region GraphToDocSyncOptions Tests

    [Fact]
    public void GraphToDocSyncOptions_Defaults_AreCorrect()
    {
        // Act
        var options = new GraphToDocSyncOptions();

        // Assert
        Assert.True(options.AutoFlagDocuments);
        Assert.True(options.SendNotifications);
        Assert.Equal(100, options.BatchSize);
        Assert.Equal(1000, options.MaxDocumentsPerChange);
        Assert.Equal(0.6f, options.MinActionConfidence);
        Assert.True(options.IncludeSuggestedActions);
        Assert.True(options.DeduplicateNotifications);
        Assert.Equal(TimeSpan.FromHours(1), options.DeduplicationWindow);
        Assert.Equal(TimeSpan.FromMinutes(5), options.Timeout);
    }

    [Fact]
    public void GraphToDocSyncOptions_ReasonPriorities_AreConfigured()
    {
        // Act
        var options = new GraphToDocSyncOptions();

        // Assert
        Assert.Equal(FlagPriority.High, options.ReasonPriorities[FlagReason.EntityValueChanged]);
        Assert.Equal(FlagPriority.Critical, options.ReasonPriorities[FlagReason.EntityDeleted]);
        Assert.Equal(FlagPriority.Medium, options.ReasonPriorities[FlagReason.NewRelationship]);
    }

    #endregion

    #region SuggestedAction Tests

    [Fact]
    public void SuggestedAction_Defaults_AreCorrect()
    {
        // Act
        var action = new SuggestedAction
        {
            ActionType = ActionType.UpdateReferences,
            Description = "Update entity references"
        };

        // Assert
        Assert.Null(action.SuggestedText);
        Assert.Equal(0.5f, action.Confidence);
    }

    #endregion

    #region GraphChangeSubscription Tests

    [Fact]
    public void GraphChangeSubscription_Defaults_AreCorrect()
    {
        // Act
        var subscription = new GraphChangeSubscription
        {
            DocumentId = Guid.NewGuid()
        };

        // Assert
        Assert.Empty(subscription.EntityIds);
        Assert.Empty(subscription.ChangeTypes);
        Assert.True(subscription.IsActive);
    }

    #endregion

    #region Event Factory Tests

    [Fact]
    public void GraphToDocSyncCompletedEvent_Create_SetsProperties()
    {
        // Arrange
        var change = CreateTestChange();
        var result = new GraphToDocSyncResult
        {
            Status = SyncOperationStatus.Success,
            TriggeringChange = change
        };
        var userId = Guid.NewGuid();

        // Act
        var evt = GraphToDocSyncCompletedEvent.Create(change, result, userId);

        // Assert
        Assert.Equal(change, evt.TriggeringChange);
        Assert.Equal(result, evt.Result);
        Assert.Equal(userId, evt.InitiatedBy);
    }

    [Fact]
    public void DocumentFlaggedEvent_Create_SetsProperties()
    {
        // Arrange
        var flag = new DocumentFlag
        {
            FlagId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            TriggeringEntityId = Guid.NewGuid(),
            Reason = FlagReason.EntityValueChanged,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var evt = DocumentFlaggedEvent.Create(flag);

        // Assert
        Assert.Equal(flag, evt.Flag);
    }

    #endregion

    #region Enum Coverage Tests

    [Theory]
    [InlineData(DocumentEntityRelationship.ExplicitReference, 0)]
    [InlineData(DocumentEntityRelationship.ImplicitReference, 1)]
    [InlineData(DocumentEntityRelationship.DerivedFrom, 2)]
    [InlineData(DocumentEntityRelationship.IndirectReference, 3)]
    public void DocumentEntityRelationship_HasCorrectValues(
        DocumentEntityRelationship relationship, int expected)
    {
        Assert.Equal(expected, (int)relationship);
    }

    [Theory]
    [InlineData(FlagReason.EntityValueChanged, 0)]
    [InlineData(FlagReason.EntityPropertiesUpdated, 1)]
    [InlineData(FlagReason.EntityDeleted, 2)]
    [InlineData(FlagReason.NewRelationship, 3)]
    [InlineData(FlagReason.RelationshipRemoved, 4)]
    [InlineData(FlagReason.ManualSyncRequested, 5)]
    [InlineData(FlagReason.ConflictDetected, 6)]
    public void FlagReason_HasCorrectValues(FlagReason reason, int expected)
    {
        Assert.Equal(expected, (int)reason);
    }

    [Theory]
    [InlineData(FlagPriority.Low, 0)]
    [InlineData(FlagPriority.Medium, 1)]
    [InlineData(FlagPriority.High, 2)]
    [InlineData(FlagPriority.Critical, 3)]
    public void FlagPriority_HasCorrectValues(FlagPriority priority, int expected)
    {
        Assert.Equal(expected, (int)priority);
    }

    [Theory]
    [InlineData(FlagStatus.Pending, 0)]
    [InlineData(FlagStatus.Acknowledged, 1)]
    [InlineData(FlagStatus.Resolved, 2)]
    [InlineData(FlagStatus.Dismissed, 3)]
    [InlineData(FlagStatus.Escalated, 4)]
    public void FlagStatus_HasCorrectValues(FlagStatus status, int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    [Theory]
    [InlineData(FlagResolution.UpdatedWithGraphChanges, 0)]
    [InlineData(FlagResolution.RejectedGraphChanges, 1)]
    [InlineData(FlagResolution.ManualMerge, 2)]
    [InlineData(FlagResolution.Dismissed, 3)]
    public void FlagResolution_HasCorrectValues(FlagResolution resolution, int expected)
    {
        Assert.Equal(expected, (int)resolution);
    }

    [Theory]
    [InlineData(ActionType.UpdateReferences, 0)]
    [InlineData(ActionType.AddInformation, 1)]
    [InlineData(ActionType.RemoveInformation, 2)]
    [InlineData(ActionType.ManualReview, 3)]
    public void ActionType_HasCorrectValues(ActionType type, int expected)
    {
        Assert.Equal(expected, (int)type);
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

    #endregion
}
