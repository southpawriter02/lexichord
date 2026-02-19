// =============================================================================
// File: ConflictRecordsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for conflict-related enums and records.
// =============================================================================
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.Conflict;

/// <summary>
/// Unit tests for conflict-related enums and records.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6h")]
public class ConflictRecordsTests
{
    #region MergeStrategy Enum Tests

    [Fact]
    public void MergeStrategy_HasExpectedValues()
    {
        // Assert: All expected enum values exist
        Assert.Equal(0, (int)MergeStrategy.DocumentFirst);
        Assert.Equal(1, (int)MergeStrategy.GraphFirst);
        Assert.Equal(2, (int)MergeStrategy.Combine);
        Assert.Equal(3, (int)MergeStrategy.MostRecent);
        Assert.Equal(4, (int)MergeStrategy.HighestConfidence);
        Assert.Equal(5, (int)MergeStrategy.RequiresManualMerge);
    }

    [Fact]
    public void MergeStrategy_HasSixValues()
    {
        // Assert
        var values = Enum.GetValues<MergeStrategy>();
        Assert.Equal(6, values.Length);
    }

    #endregion

    #region MergeType Enum Tests

    [Fact]
    public void MergeType_HasExpectedValues()
    {
        // Assert: All expected enum values exist
        Assert.Equal(0, (int)MergeType.Selection);
        Assert.Equal(1, (int)MergeType.Intelligent);
        Assert.Equal(2, (int)MergeType.Weighted);
        Assert.Equal(3, (int)MergeType.Manual);
        Assert.Equal(4, (int)MergeType.Temporal);
    }

    [Fact]
    public void MergeType_HasFiveValues()
    {
        // Assert
        var values = Enum.GetValues<MergeType>();
        Assert.Equal(5, values.Length);
    }

    #endregion

    #region PropertyDifference Record Tests

    [Fact]
    public void PropertyDifference_InitializesCorrectly()
    {
        // Arrange & Act
        var diff = new PropertyDifference
        {
            PropertyName = "TestProperty",
            DocumentValue = "doc-value",
            GraphValue = "graph-value",
            Confidence = 0.85f
        };

        // Assert
        Assert.Equal("TestProperty", diff.PropertyName);
        Assert.Equal("doc-value", diff.DocumentValue);
        Assert.Equal("graph-value", diff.GraphValue);
        Assert.Equal(0.85f, diff.Confidence);
    }

    [Fact]
    public void PropertyDifference_SupportsNullValues()
    {
        // Arrange & Act
        var diff = new PropertyDifference
        {
            PropertyName = "NullableProperty",
            DocumentValue = null,
            GraphValue = "has-value",
            Confidence = 0.5f
        };

        // Assert
        Assert.Null(diff.DocumentValue);
        Assert.NotNull(diff.GraphValue);
    }

    #endregion

    #region EntityComparison Record Tests

    [Fact]
    public void EntityComparison_WithNoDifferences_ReportsCorrectly()
    {
        // Arrange
        var docEntity = CreateTestEntity("doc");
        var graphEntity = CreateTestEntity("graph");

        // Act
        var comparison = new EntityComparison
        {
            DocumentEntity = docEntity,
            GraphEntity = graphEntity,
            PropertyDifferences = []
        };

        // Assert
        Assert.False(comparison.HasDifferences);
        Assert.Equal(0, comparison.DifferenceCount);
    }

    [Fact]
    public void EntityComparison_WithDifferences_ReportsCorrectly()
    {
        // Arrange
        var docEntity = CreateTestEntity("doc");
        var graphEntity = CreateTestEntity("graph");
        var differences = new List<PropertyDifference>
        {
            new() { PropertyName = "Name", DocumentValue = "A", GraphValue = "B", Confidence = 0.9f },
            new() { PropertyName = "Value", DocumentValue = "X", GraphValue = "Y", Confidence = 0.8f }
        };

        // Act
        var comparison = new EntityComparison
        {
            DocumentEntity = docEntity,
            GraphEntity = graphEntity,
            PropertyDifferences = differences
        };

        // Assert
        Assert.True(comparison.HasDifferences);
        Assert.Equal(2, comparison.DifferenceCount);
    }

    #endregion

    #region ConflictDetail Record Tests

    [Fact]
    public void ConflictDetail_InitializesWithRequiredProperties()
    {
        // Arrange
        var entity = CreateTestEntity("test");
        var detectedAt = DateTimeOffset.UtcNow;

        // Act
        var detail = new ConflictDetail
        {
            ConflictId = Guid.NewGuid(),
            Entity = entity,
            ConflictField = "Name",
            DocumentValue = "doc-name",
            GraphValue = "graph-name",
            Type = ConflictType.ValueMismatch,
            Severity = ConflictSeverity.Medium,
            DetectedAt = detectedAt,
            SuggestedStrategy = ConflictResolutionStrategy.UseDocument,
            ResolutionConfidence = 0.75f
        };

        // Assert
        Assert.NotEqual(Guid.Empty, detail.ConflictId);
        Assert.Equal("Name", detail.ConflictField);
        Assert.Equal(ConflictType.ValueMismatch, detail.Type);
        Assert.Equal(ConflictSeverity.Medium, detail.Severity);
        Assert.Equal(0.75f, detail.ResolutionConfidence);
    }

    [Fact]
    public void ConflictDetail_SupportsOptionalTimestamps()
    {
        // Arrange
        var entity = CreateTestEntity("test");
        var detectedAt = DateTimeOffset.UtcNow;
        var docModified = DateTimeOffset.UtcNow.AddHours(-1);
        var graphModified = DateTimeOffset.UtcNow.AddHours(-2);

        // Act
        var detail = new ConflictDetail
        {
            ConflictId = Guid.NewGuid(),
            Entity = entity,
            ConflictField = "Value",
            DocumentValue = "a",
            GraphValue = "b",
            Type = ConflictType.ConcurrentEdit,
            Severity = ConflictSeverity.High,
            DetectedAt = detectedAt,
            DocumentModifiedAt = docModified,
            GraphModifiedAt = graphModified,
            SuggestedStrategy = ConflictResolutionStrategy.Manual,
            ResolutionConfidence = 0.3f
        };

        // Assert
        Assert.Equal(docModified, detail.DocumentModifiedAt);
        Assert.Equal(graphModified, detail.GraphModifiedAt);
    }

    #endregion

    #region MergeContext Record Tests

    [Fact]
    public void MergeContext_InitializesWithDefaults()
    {
        // Arrange & Act
        var context = new MergeContext
        {
            ConflictType = ConflictType.ValueMismatch
        };

        // Assert
        Assert.Equal(ConflictType.ValueMismatch, context.ConflictType);
        Assert.Null(context.Entity);
        Assert.Null(context.Document);
        Assert.Null(context.UserId);
        Assert.NotNull(context.ContextData);
        Assert.Empty(context.ContextData);
    }

    [Fact]
    public void MergeContext_SupportsContextData()
    {
        // Arrange & Act
        var context = new MergeContext
        {
            ConflictType = ConflictType.MissingInGraph,
            ContextData = new Dictionary<string, object>
            {
                { "priority", "high" },
                { "retryCount", 3 }
            }
        };

        // Assert
        Assert.Equal(2, context.ContextData.Count);
        Assert.Equal("high", context.ContextData["priority"]);
        Assert.Equal(3, context.ContextData["retryCount"]);
    }

    #endregion

    #region MergeResult Record Tests

    [Fact]
    public void MergeResult_DocumentWins_CreatesCorrectResult()
    {
        // Arrange
        var documentValue = "doc-value";

        // Act
        var result = MergeResult.DocumentWins(documentValue);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(documentValue, result.MergedValue);
        Assert.Equal(MergeStrategy.DocumentFirst, result.UsedStrategy);
        Assert.Equal(1.0, result.Confidence);
        Assert.Equal(MergeType.Selection, result.MergeType);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void MergeResult_GraphWins_CreatesCorrectResult()
    {
        // Arrange
        var graphValue = "graph-value";

        // Act
        var result = MergeResult.GraphWins(graphValue);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(graphValue, result.MergedValue);
        Assert.Equal(MergeStrategy.GraphFirst, result.UsedStrategy);
        Assert.Equal(1.0, result.Confidence);
        Assert.Equal(MergeType.Selection, result.MergeType);
    }

    [Fact]
    public void MergeResult_RequiresManual_CreatesCorrectResult()
    {
        // Arrange
        var reason = "Values cannot be automatically merged";

        // Act
        var result = MergeResult.RequiresManual(reason);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.MergedValue);
        Assert.Equal(MergeStrategy.RequiresManualMerge, result.UsedStrategy);
        Assert.Equal(0.0, result.Confidence);
        Assert.Equal(MergeType.Manual, result.MergeType);
        Assert.Equal(reason, result.ErrorMessage);
    }

    [Fact]
    public void MergeResult_Failed_CreatesCorrectResult()
    {
        // Arrange
        var errorMessage = "Merge operation failed";

        // Act
        var result = MergeResult.Failed(errorMessage);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.MergedValue);
        // LOGIC: Failed() doesn't set UsedStrategy, so it defaults to first enum value
        Assert.Equal(MergeStrategy.DocumentFirst, result.UsedStrategy);
        Assert.Equal(0.0f, result.Confidence);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }

    #endregion

    #region ConflictMergeResult Record Tests

    [Fact]
    public void ConflictMergeResult_FromMergeResult_CopiesProperties()
    {
        // Arrange
        var mergeResult = MergeResult.DocumentWins("merged-value");
        var docValue = "doc-value";
        var graphValue = "graph-value";
        var explanation = "Document value was more recent";

        // Act
        var conflictResult = ConflictMergeResult.FromMergeResult(
            mergeResult, docValue, graphValue, explanation);

        // Assert
        Assert.True(conflictResult.Success);
        Assert.Equal("merged-value", conflictResult.MergedValue);
        Assert.Equal(docValue, conflictResult.DocumentValue);
        Assert.Equal(graphValue, conflictResult.GraphValue);
        Assert.Equal(explanation, conflictResult.Explanation);
        Assert.Equal(MergeStrategy.DocumentFirst, conflictResult.UsedStrategy);
    }

    #endregion

    #region ConflictResolutionResult Record Tests

    [Fact]
    public void ConflictResolutionResult_Success_CreatesCorrectResult()
    {
        // Arrange
        var conflict = CreateTestConflict();
        var strategy = ConflictResolutionStrategy.UseDocument;
        var resolvedValue = "resolved-value";

        // Act
        var result = ConflictResolutionResult.Success(
            conflict, strategy, resolvedValue, isAutomatic: true);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(conflict, result.Conflict);
        Assert.Equal(strategy, result.Strategy);
        Assert.Equal(resolvedValue, result.ResolvedValue);
        Assert.True(result.IsAutomatic);
        Assert.Null(result.ErrorMessage);
        Assert.NotEqual(default, result.ResolvedAt);
    }

    [Fact]
    public void ConflictResolutionResult_Failure_CreatesCorrectResult()
    {
        // Arrange
        var conflict = CreateTestConflict();
        var strategy = ConflictResolutionStrategy.Merge;
        var errorMessage = "Merge failed due to incompatible values";

        // Act
        var result = ConflictResolutionResult.Failure(conflict, strategy, errorMessage);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(conflict, result.Conflict);
        Assert.Equal(strategy, result.Strategy);
        Assert.Null(result.ResolvedValue);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }

    [Fact]
    public void ConflictResolutionResult_RequiresManualIntervention_CreatesCorrectResult()
    {
        // Arrange
        var conflict = CreateTestConflict();

        // Act
        var result = ConflictResolutionResult.RequiresManualIntervention(conflict);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(ConflictResolutionStrategy.Manual, result.Strategy);
        Assert.Equal("Conflict requires manual intervention", result.ErrorMessage);
        Assert.False(result.IsAutomatic);
    }

    #endregion

    #region ConflictResolutionOptions Record Tests

    [Fact]
    public void ConflictResolutionOptions_Default_HasExpectedValues()
    {
        // Act
        var options = ConflictResolutionOptions.Default;

        // Assert
        Assert.Equal(ConflictResolutionStrategy.Merge, options.DefaultStrategy);
        Assert.True(options.AutoResolveLow);
        Assert.False(options.AutoResolveMedium);
        Assert.False(options.AutoResolveHigh);
        Assert.Equal(0.8f, options.MinMergeConfidence);
        Assert.True(options.PreserveConflictHistory);
        Assert.Equal(3, options.MaxResolutionAttempts);
        Assert.Equal(TimeSpan.FromSeconds(30), options.ResolutionTimeout);
    }

    [Fact]
    public void ConflictResolutionOptions_CanAutoResolve_RespectsSettings()
    {
        // Arrange
        var options = new ConflictResolutionOptions
        {
            AutoResolveLow = true,
            AutoResolveMedium = true,
            AutoResolveHigh = false
        };

        // Assert
        Assert.True(options.CanAutoResolve(ConflictSeverity.Low));
        Assert.True(options.CanAutoResolve(ConflictSeverity.Medium));
        Assert.False(options.CanAutoResolve(ConflictSeverity.High));
    }

    [Fact]
    public void ConflictResolutionOptions_GetStrategy_ReturnsTypeSpecificStrategy()
    {
        // Arrange
        var strategies = new Dictionary<ConflictType, ConflictResolutionStrategy>
        {
            { ConflictType.ValueMismatch, ConflictResolutionStrategy.UseDocument },
            { ConflictType.MissingInGraph, ConflictResolutionStrategy.UseDocument }
        };
        var options = new ConflictResolutionOptions
        {
            DefaultStrategy = ConflictResolutionStrategy.Manual,
            StrategyByType = strategies
        };

        // Assert
        Assert.Equal(ConflictResolutionStrategy.UseDocument,
            options.GetStrategy(ConflictType.ValueMismatch));
        Assert.Equal(ConflictResolutionStrategy.Manual,
            options.GetStrategy(ConflictType.ConcurrentEdit));
    }

    #endregion

    #region Helper Methods

    private static KnowledgeEntity CreateTestEntity(string suffix)
    {
        return new KnowledgeEntity
        {
            Id = Guid.NewGuid(),
            Name = $"TestEntity-{suffix}",
            Type = "Concept",
            Properties = new Dictionary<string, object>
            {
                ["Value"] = $"test-value-{suffix}",
                ["Confidence"] = 0.9
            },
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            ModifiedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
    }

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
