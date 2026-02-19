// =============================================================================
// File: ConflictMergerTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ConflictMerger service.
// =============================================================================
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.Knowledge.Sync.Conflict;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.Conflict;

/// <summary>
/// Unit tests for <see cref="ConflictMerger"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6h")]
public class ConflictMergerTests
{
    private readonly Mock<ILogger<ConflictMerger>> _mockLogger;
    private readonly ConflictMerger _sut;

    public ConflictMergerTests()
    {
        _mockLogger = new Mock<ILogger<ConflictMerger>>();
        _sut = new ConflictMerger(_mockLogger.Object);
    }

    #region MergeAsync Tests - MostRecent Strategy (ValueMismatch default)

    [Fact]
    public async Task MergeAsync_ValueMismatch_WithoutTimestamps_DefaultsToDocumentValue()
    {
        // Arrange
        var docValue = "document-value";
        var graphValue = "graph-value";
        var context = CreateMergeContext(ConflictType.ValueMismatch);

        // Act
        var result = await _sut.MergeAsync(docValue, graphValue, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(docValue, result.MergedValue);
        Assert.Equal(MergeStrategy.MostRecent, result.UsedStrategy);
    }

    [Fact]
    public async Task MergeAsync_MostRecent_WithDocumentNewer_ReturnsDocumentValue()
    {
        // Arrange
        var docValue = "newer-value";
        var graphValue = "older-value";
        var document = new Document(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            FilePath: "/test.md",
            Title: "Test",
            Hash: "abc",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow, // More recent
            FailureReason: null);
        var entity = new KnowledgeEntity
        {
            Type = "Concept",
            Name = "Test",
            ModifiedAt = DateTimeOffset.UtcNow.AddHours(-2) // Older
        };
        var context = new MergeContext
        {
            ConflictType = ConflictType.ValueMismatch,
            Document = document,
            Entity = entity,
            ContextData = new Dictionary<string, object>()
        };

        // Act
        var result = await _sut.MergeAsync(docValue, graphValue, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(docValue, result.MergedValue);
        Assert.Equal(MergeStrategy.MostRecent, result.UsedStrategy);
        Assert.Equal(MergeType.Temporal, result.MergeType);
    }

    [Fact]
    public async Task MergeAsync_MostRecent_WithGraphNewer_ReturnsGraphValue()
    {
        // Arrange
        var docValue = "older-value";
        var graphValue = "newer-value";
        var document = new Document(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            FilePath: "/test.md",
            Title: "Test",
            Hash: "abc",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow.AddHours(-2), // Older
            FailureReason: null);
        var entity = new KnowledgeEntity
        {
            Type = "Concept",
            Name = "Test",
            ModifiedAt = DateTimeOffset.UtcNow // More recent
        };
        var context = new MergeContext
        {
            ConflictType = ConflictType.ValueMismatch,
            Document = document,
            Entity = entity,
            ContextData = new Dictionary<string, object>()
        };

        // Act
        var result = await _sut.MergeAsync(docValue, graphValue, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(graphValue, result.MergedValue);
        Assert.Equal(MergeStrategy.MostRecent, result.UsedStrategy);
        Assert.Equal(MergeType.Temporal, result.MergeType);
    }

    #endregion

    #region MergeAsync Tests - DocumentFirst Strategy (MissingInGraph)

    [Fact]
    public async Task MergeAsync_MissingInGraph_ReturnsDocumentValue()
    {
        // Arrange
        var docValue = "document-value";
        object? graphValue = null;
        var context = CreateMergeContext(ConflictType.MissingInGraph);

        // Act
        var result = await _sut.MergeAsync(docValue, graphValue!, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(docValue, result.MergedValue);
        Assert.Equal(MergeStrategy.DocumentFirst, result.UsedStrategy);
        Assert.Equal(1.0f, result.Confidence);
    }

    #endregion

    #region MergeAsync Tests - RequiresManualMerge

    [Fact]
    public async Task MergeAsync_MissingInDocument_RequiresManual()
    {
        // Arrange
        object? docValue = null;
        var graphValue = "graph-value";
        var context = CreateMergeContext(ConflictType.MissingInDocument);

        // Act
        var result = await _sut.MergeAsync(docValue!, graphValue, context);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MergeStrategy.RequiresManualMerge, result.UsedStrategy);
        Assert.Equal(MergeType.Manual, result.MergeType);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task MergeAsync_ConcurrentEdit_RequiresManual()
    {
        // Arrange
        var docValue = "value-a";
        var graphValue = "value-b";
        var context = CreateMergeContext(ConflictType.ConcurrentEdit);

        // Act
        var result = await _sut.MergeAsync(docValue, graphValue, context);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MergeStrategy.RequiresManualMerge, result.UsedStrategy);
        Assert.Equal(MergeType.Manual, result.MergeType);
    }

    [Fact]
    public async Task MergeAsync_RelationshipMismatch_RequiresManual()
    {
        // Arrange
        var docValue = "relation-a";
        var graphValue = "relation-b";
        var context = CreateMergeContext(ConflictType.RelationshipMismatch);

        // Act
        var result = await _sut.MergeAsync(docValue, graphValue, context);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MergeStrategy.RequiresManualMerge, result.UsedStrategy);
    }

    #endregion

    #region MergeAsync Tests - PreferredStrategy Override

    [Fact]
    public async Task MergeAsync_WithPreferredStrategy_UsesPreferred()
    {
        // Arrange
        var docValue = "document-value";
        var graphValue = "graph-value";
        var context = new MergeContext
        {
            ConflictType = ConflictType.ValueMismatch,
            ContextData = new Dictionary<string, object>
            {
                ["PreferredStrategy"] = MergeStrategy.DocumentFirst
            }
        };

        // Act
        var result = await _sut.MergeAsync(docValue, graphValue, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(docValue, result.MergedValue);
        Assert.Equal(MergeStrategy.DocumentFirst, result.UsedStrategy);
    }

    [Fact]
    public async Task MergeAsync_WithPreferredGraphFirst_ReturnsGraphValue()
    {
        // Arrange
        var docValue = "document-value";
        var graphValue = "graph-value";
        var context = new MergeContext
        {
            ConflictType = ConflictType.ValueMismatch,
            ContextData = new Dictionary<string, object>
            {
                ["PreferredStrategy"] = MergeStrategy.GraphFirst
            }
        };

        // Act
        var result = await _sut.MergeAsync(docValue, graphValue, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(graphValue, result.MergedValue);
        Assert.Equal(MergeStrategy.GraphFirst, result.UsedStrategy);
    }

    #endregion

    #region MergeAsync Tests - Combine Strategy

    [Fact]
    public async Task MergeAsync_Combine_DifferentStrings_Concatenates()
    {
        // Arrange
        var docValue = "Hello";
        var graphValue = "World";
        var context = new MergeContext
        {
            ConflictType = ConflictType.ValueMismatch,
            ContextData = new Dictionary<string, object>
            {
                ["PreferredStrategy"] = MergeStrategy.Combine
            }
        };

        // Act
        var result = await _sut.MergeAsync(docValue, graphValue, context);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Hello", result.MergedValue?.ToString() ?? "");
        Assert.Contains("World", result.MergedValue?.ToString() ?? "");
        Assert.Equal(MergeStrategy.Combine, result.UsedStrategy);
    }

    [Fact]
    public async Task MergeAsync_Combine_DocContainsGraph_ReturnsDocValue()
    {
        // Arrange
        var docValue = "Hello World"; // Contains graph
        var graphValue = "World";
        var context = new MergeContext
        {
            ConflictType = ConflictType.ValueMismatch,
            ContextData = new Dictionary<string, object>
            {
                ["PreferredStrategy"] = MergeStrategy.Combine
            }
        };

        // Act
        var result = await _sut.MergeAsync(docValue, graphValue, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(docValue, result.MergedValue);
        Assert.Equal(MergeStrategy.Combine, result.UsedStrategy);
    }

    [Fact]
    public async Task MergeAsync_Combine_NumericValues_ReturnsAverage()
    {
        // Arrange
        var docValue = 10;
        var graphValue = 20;
        var context = new MergeContext
        {
            ConflictType = ConflictType.ValueMismatch,
            ContextData = new Dictionary<string, object>
            {
                ["PreferredStrategy"] = MergeStrategy.Combine
            }
        };

        // Act
        var result = await _sut.MergeAsync(docValue, graphValue, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(15.0, result.MergedValue);
        Assert.Equal(MergeStrategy.Combine, result.UsedStrategy);
        Assert.Equal(MergeType.Weighted, result.MergeType);
    }

    #endregion

    #region MergeAsync Tests - HighestConfidence Strategy

    [Fact]
    public async Task MergeAsync_HighestConfidence_DocumentHigher_ReturnsDocValue()
    {
        // Arrange
        var docValue = "high-confidence-value";
        var graphValue = "low-confidence-value";
        var context = new MergeContext
        {
            ConflictType = ConflictType.ValueMismatch,
            ContextData = new Dictionary<string, object>
            {
                ["PreferredStrategy"] = MergeStrategy.HighestConfidence,
                ["DocumentConfidence"] = 0.9f,
                ["GraphConfidence"] = 0.5f
            }
        };

        // Act
        var result = await _sut.MergeAsync(docValue, graphValue, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(docValue, result.MergedValue);
        Assert.Equal(MergeStrategy.HighestConfidence, result.UsedStrategy);
        Assert.Equal(MergeType.Weighted, result.MergeType);
    }

    [Fact]
    public async Task MergeAsync_HighestConfidence_GraphHigher_ReturnsGraphValue()
    {
        // Arrange
        var docValue = "low-confidence-value";
        var graphValue = "high-confidence-value";
        var context = new MergeContext
        {
            ConflictType = ConflictType.ValueMismatch,
            ContextData = new Dictionary<string, object>
            {
                ["PreferredStrategy"] = MergeStrategy.HighestConfidence,
                ["DocumentConfidence"] = 0.3f,
                ["GraphConfidence"] = 0.9f
            }
        };

        // Act
        var result = await _sut.MergeAsync(docValue, graphValue, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(graphValue, result.MergedValue);
        Assert.Equal(MergeStrategy.HighestConfidence, result.UsedStrategy);
    }

    #endregion

    #region GetMergeStrategy Tests

    [Theory]
    [InlineData(ConflictType.ValueMismatch, MergeStrategy.MostRecent)]
    [InlineData(ConflictType.MissingInGraph, MergeStrategy.DocumentFirst)]
    [InlineData(ConflictType.MissingInDocument, MergeStrategy.RequiresManualMerge)]
    [InlineData(ConflictType.RelationshipMismatch, MergeStrategy.RequiresManualMerge)]
    [InlineData(ConflictType.ConcurrentEdit, MergeStrategy.RequiresManualMerge)]
    public void GetMergeStrategy_ReturnsExpectedStrategy(ConflictType type, MergeStrategy expected)
    {
        // Act
        var result = _sut.GetMergeStrategy(type);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region MergeAsync Tests - No ConflictType

    [Fact]
    public async Task MergeAsync_NoConflictType_DefaultsToMostRecent()
    {
        // Arrange
        var docValue = "doc-value";
        var graphValue = "graph-value";
        var context = new MergeContext
        {
            ConflictType = null, // No conflict type
            ContextData = new Dictionary<string, object>()
        };

        // Act
        var result = await _sut.MergeAsync(docValue, graphValue, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(MergeStrategy.MostRecent, result.UsedStrategy);
    }

    #endregion

    #region Helper Methods

    private static MergeContext CreateMergeContext(ConflictType type)
    {
        return new MergeContext
        {
            ConflictType = type,
            ContextData = new Dictionary<string, object>()
        };
    }

    #endregion
}
