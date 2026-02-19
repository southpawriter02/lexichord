// =============================================================================
// File: ConflictResolvedEventTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ConflictResolvedEvent MediatR notification.
// =============================================================================
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict.Events;
using MediatR;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.Conflict;

/// <summary>
/// Unit tests for <see cref="ConflictResolvedEvent"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6h")]
public class ConflictResolvedEventTests
{
    #region Create Factory Method Tests

    [Fact]
    public void Create_WithValidInputs_CreatesEvent()
    {
        // Arrange
        var conflict = CreateTestConflict();
        var result = ConflictResolutionResult.Success(
            conflict,
            ConflictResolutionStrategy.UseDocument,
            "resolved-value",
            isAutomatic: true);

        // Act
        var evt = ConflictResolvedEvent.Create(conflict, result);

        // Assert
        Assert.NotNull(evt);
        Assert.Equal(conflict, evt.Conflict);
        Assert.Equal(result, evt.Result);
        Assert.NotEqual(default, evt.Timestamp);
    }

    [Fact]
    public void Create_WithInitiatedBy_IncludesUserId()
    {
        // Arrange
        var conflict = CreateTestConflict();
        var result = ConflictResolutionResult.Success(
            conflict,
            ConflictResolutionStrategy.Manual,
            "manual-value",
            isAutomatic: false);
        var userId = Guid.NewGuid();

        // Act
        var evt = ConflictResolvedEvent.Create(conflict, result, userId);

        // Assert
        Assert.Equal(userId, evt.InitiatedBy);
    }

    [Fact]
    public void Create_WithoutInitiatedBy_HasNullUserId()
    {
        // Arrange
        var conflict = CreateTestConflict();
        var result = ConflictResolutionResult.Success(
            conflict,
            ConflictResolutionStrategy.UseGraph,
            "graph-value",
            isAutomatic: true);

        // Act
        var evt = ConflictResolvedEvent.Create(conflict, result);

        // Assert
        Assert.Null(evt.InitiatedBy);
    }

    #endregion

    #region MediatR INotification Tests

    [Fact]
    public void ConflictResolvedEvent_ImplementsINotification()
    {
        // Assert
        Assert.True(typeof(INotification).IsAssignableFrom(typeof(ConflictResolvedEvent)));
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void Create_SetsTimestampToCurrentTime()
    {
        // Arrange
        var conflict = CreateTestConflict();
        var result = ConflictResolutionResult.Success(
            conflict,
            ConflictResolutionStrategy.UseDocument,
            "value",
            isAutomatic: true);
        var before = DateTimeOffset.UtcNow;

        // Act
        var evt = ConflictResolvedEvent.Create(conflict, result);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(evt.Timestamp >= before);
        Assert.True(evt.Timestamp <= after);
    }

    #endregion

    #region Event Data Tests

    [Fact]
    public void Create_PreservesConflictData()
    {
        // Arrange
        var conflict = new SyncConflict
        {
            ConflictTarget = "Entity:TestEntity.Name",
            DocumentValue = "Document Name",
            GraphValue = "Graph Name",
            DetectedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            Type = ConflictType.ValueMismatch,
            Severity = ConflictSeverity.High,
            Description = "Name mismatch between document and graph"
        };
        var result = ConflictResolutionResult.Success(
            conflict,
            ConflictResolutionStrategy.UseDocument,
            "Document Name",
            isAutomatic: false);

        // Act
        var evt = ConflictResolvedEvent.Create(conflict, result);

        // Assert
        Assert.Equal("Entity:TestEntity.Name", evt.Conflict.ConflictTarget);
        Assert.Equal("Document Name", evt.Conflict.DocumentValue);
        Assert.Equal("Graph Name", evt.Conflict.GraphValue);
        Assert.Equal(ConflictType.ValueMismatch, evt.Conflict.Type);
        Assert.Equal(ConflictSeverity.High, evt.Conflict.Severity);
    }

    [Fact]
    public void Create_PreservesResultData()
    {
        // Arrange
        var conflict = CreateTestConflict();
        var result = ConflictResolutionResult.Failure(
            conflict,
            ConflictResolutionStrategy.Merge,
            "Merge failed: incompatible types");

        // Act
        var evt = ConflictResolvedEvent.Create(conflict, result);

        // Assert
        Assert.False(evt.Result.Succeeded);
        Assert.Equal(ConflictResolutionStrategy.Merge, evt.Result.Strategy);
        Assert.Equal("Merge failed: incompatible types", evt.Result.ErrorMessage);
    }

    #endregion

    #region Resolution Strategy Tests

    [Theory]
    [InlineData(ConflictResolutionStrategy.UseDocument)]
    [InlineData(ConflictResolutionStrategy.UseGraph)]
    [InlineData(ConflictResolutionStrategy.Manual)]
    [InlineData(ConflictResolutionStrategy.Merge)]
    [InlineData(ConflictResolutionStrategy.DiscardDocument)]
    [InlineData(ConflictResolutionStrategy.DiscardGraph)]
    public void Create_PreservesResolutionStrategy(ConflictResolutionStrategy strategy)
    {
        // Arrange
        var conflict = CreateTestConflict();
        var result = ConflictResolutionResult.Success(
            conflict, strategy, "value", isAutomatic: true);

        // Act
        var evt = ConflictResolvedEvent.Create(conflict, result);

        // Assert
        Assert.Equal(strategy, evt.Result.Strategy);
    }

    #endregion

    #region Helper Methods

    private static SyncConflict CreateTestConflict()
    {
        return new SyncConflict
        {
            ConflictTarget = "Entity:Test.Property",
            DocumentValue = "doc-value",
            GraphValue = "graph-value",
            DetectedAt = DateTimeOffset.UtcNow,
            Type = ConflictType.ValueMismatch,
            Severity = ConflictSeverity.Medium
        };
    }

    #endregion
}
