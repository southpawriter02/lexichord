// =============================================================================
// File: SyncRecordsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for sync-related records (SyncResult, SyncStatus, etc.).
// =============================================================================
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.RAG;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync;

/// <summary>
/// Unit tests for sync records.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6e")]
public class SyncRecordsTests
{
    #region SyncResult Tests

    [Fact]
    public void SyncResult_RequiresStatus()
    {
        // Arrange & Act
        var result = new SyncResult
        {
            Status = SyncOperationStatus.Success
        };

        // Assert
        Assert.Equal(SyncOperationStatus.Success, result.Status);
        Assert.Empty(result.EntitiesAffected);
        Assert.Empty(result.ClaimsAffected);
        Assert.Empty(result.Conflicts);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void SyncResult_SupportsWithExpression()
    {
        // Arrange
        var original = new SyncResult
        {
            Status = SyncOperationStatus.Success,
            Duration = TimeSpan.FromMilliseconds(100)
        };

        // Act
        var modified = original with { Duration = TimeSpan.FromMilliseconds(200) };

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(100), original.Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(200), modified.Duration);
    }

    #endregion

    #region SyncStatus Tests

    [Fact]
    public void SyncStatus_RequiresDocumentIdAndState()
    {
        // Arrange & Act
        var status = new SyncStatus
        {
            DocumentId = Guid.NewGuid(),
            State = SyncState.InSync
        };

        // Assert
        Assert.NotEqual(Guid.Empty, status.DocumentId);
        Assert.Equal(SyncState.InSync, status.State);
        Assert.False(status.IsSyncInProgress);
        Assert.Equal(0, status.UnresolvedConflicts);
    }

    #endregion

    #region SyncConflict Tests

    [Fact]
    public void SyncConflict_RequiresAllProperties()
    {
        // Arrange & Act
        var conflict = new SyncConflict
        {
            ConflictTarget = "Entity:Name",
            DocumentValue = "doc-value",
            GraphValue = "graph-value",
            DetectedAt = DateTimeOffset.UtcNow,
            Type = ConflictType.ValueMismatch
        };

        // Assert
        Assert.Equal("Entity:Name", conflict.ConflictTarget);
        Assert.Equal(ConflictSeverity.Medium, conflict.Severity); // Default
        Assert.Equal(ConflictType.ValueMismatch, conflict.Type);
    }

    #endregion

    #region SyncContext Tests

    [Fact]
    public void SyncContext_HasCorrectDefaults()
    {
        // Arrange
        var document = new Document(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            FilePath: "/test/doc.md",
            Title: "Test",
            Hash: "hash",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow,
            FailureReason: null);

        // Act
        var context = new SyncContext
        {
            UserId = Guid.NewGuid(),
            Document = document
        };

        // Assert
        Assert.True(context.AutoResolveConflicts);
        Assert.Equal(ConflictResolutionStrategy.Merge, context.DefaultConflictStrategy);
        Assert.True(context.PublishEvents);
        Assert.Equal(TimeSpan.FromMinutes(5), context.Timeout);
    }

    #endregion

    #region GraphChange Tests

    [Fact]
    public void GraphChange_RequiresEntityIdAndChangeType()
    {
        // Arrange & Act
        var change = new GraphChange
        {
            EntityId = Guid.NewGuid(),
            ChangeType = ChangeType.EntityUpdated,
            NewValue = "new-value",
            ChangedAt = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.NotEqual(Guid.Empty, change.EntityId);
        Assert.Equal(ChangeType.EntityUpdated, change.ChangeType);
        Assert.Null(change.PreviousValue);
    }

    #endregion
}
