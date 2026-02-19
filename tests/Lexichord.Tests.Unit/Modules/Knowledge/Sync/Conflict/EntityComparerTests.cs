// =============================================================================
// File: EntityComparerTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for EntityComparer service.
// =============================================================================
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;
using Lexichord.Modules.Knowledge.Sync.Conflict;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.Conflict;

/// <summary>
/// Unit tests for <see cref="EntityComparer"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6h")]
public class EntityComparerTests
{
    private readonly Mock<ILogger<EntityComparer>> _mockLogger;
    private readonly EntityComparer _sut;

    public EntityComparerTests()
    {
        _mockLogger = new Mock<ILogger<EntityComparer>>();
        _sut = new EntityComparer(_mockLogger.Object);
    }

    #region CompareAsync Tests - Basic Comparison

    [Fact]
    public async Task CompareAsync_WithIdenticalEntities_ReturnsNoDifferences()
    {
        // Arrange
        var entity = CreateTestEntity("test", "Concept");
        var copy = CreateTestEntity("test", "Concept");

        // Act
        var result = await _sut.CompareAsync(entity, copy);

        // Assert
        Assert.False(result.HasDifferences);
        Assert.Equal(0, result.DifferenceCount);
        Assert.Empty(result.PropertyDifferences);
    }

    [Fact]
    public async Task CompareAsync_WithNameDifference_DetectsDifference()
    {
        // Arrange
        var docEntity = CreateTestEntity("DocName", "Concept");
        var graphEntity = CreateTestEntity("GraphName", "Concept");

        // Act
        var result = await _sut.CompareAsync(docEntity, graphEntity);

        // Assert
        Assert.True(result.HasDifferences);
        var nameDiff = result.PropertyDifferences.FirstOrDefault(d => d.PropertyName == "Name");
        Assert.NotNull(nameDiff);
        Assert.Equal("DocName", nameDiff.DocumentValue);
        Assert.Equal("GraphName", nameDiff.GraphValue);
    }

    [Fact]
    public async Task CompareAsync_WithTypeDifference_DetectsDifference()
    {
        // Arrange
        var docEntity = CreateTestEntity("Name", "Concept");
        var graphEntity = CreateTestEntity("Name", "Entity");

        // Act
        var result = await _sut.CompareAsync(docEntity, graphEntity);

        // Assert
        Assert.True(result.HasDifferences);
        var typeDiff = result.PropertyDifferences.FirstOrDefault(d => d.PropertyName == "Type");
        Assert.NotNull(typeDiff);
        Assert.Equal("Concept", typeDiff.DocumentValue);
        Assert.Equal("Entity", typeDiff.GraphValue);
    }

    #endregion

    #region CompareAsync Tests - Custom Properties

    [Fact]
    public async Task CompareAsync_WithCustomPropertyDifference_DetectsDifference()
    {
        // Arrange
        var docEntity = CreateTestEntity("Name", "Concept");
        docEntity.Properties["customProp"] = "doc-custom";

        var graphEntity = CreateTestEntity("Name", "Concept");
        graphEntity.Properties["customProp"] = "graph-custom";

        // Act
        var result = await _sut.CompareAsync(docEntity, graphEntity);

        // Assert
        Assert.True(result.HasDifferences);
        var customDiff = result.PropertyDifferences.FirstOrDefault(d => d.PropertyName == "customProp");
        Assert.NotNull(customDiff);
        Assert.Equal("doc-custom", customDiff.DocumentValue);
        Assert.Equal("graph-custom", customDiff.GraphValue);
    }

    [Fact]
    public async Task CompareAsync_WithMissingCustomProperty_DetectsDifference()
    {
        // Arrange
        var docEntity = CreateTestEntity("Name", "Concept");
        docEntity.Properties["onlyInDoc"] = "value";

        var graphEntity = CreateTestEntity("Name", "Concept");
        // No "onlyInDoc" property

        // Act
        var result = await _sut.CompareAsync(docEntity, graphEntity);

        // Assert
        Assert.True(result.HasDifferences);
        var diff = result.PropertyDifferences.FirstOrDefault(d => d.PropertyName == "onlyInDoc");
        Assert.NotNull(diff);
        Assert.Equal("value", diff.DocumentValue);
        Assert.Null(diff.GraphValue);
    }

    [Fact]
    public async Task CompareAsync_WithExtraGraphProperty_DetectsDifference()
    {
        // Arrange
        var docEntity = CreateTestEntity("Name", "Concept");

        var graphEntity = CreateTestEntity("Name", "Concept");
        graphEntity.Properties["onlyInGraph"] = "graph-value";

        // Act
        var result = await _sut.CompareAsync(docEntity, graphEntity);

        // Assert
        Assert.True(result.HasDifferences);
        var diff = result.PropertyDifferences.FirstOrDefault(d => d.PropertyName == "onlyInGraph");
        Assert.NotNull(diff);
        Assert.Null(diff.DocumentValue);
        Assert.Equal("graph-value", diff.GraphValue);
    }

    [Fact]
    public async Task CompareAsync_WithIdenticalCustomProperties_NoDifference()
    {
        // Arrange
        var docEntity = CreateTestEntity("Name", "Concept");
        docEntity.Properties["shared"] = "same-value";

        var graphEntity = CreateTestEntity("Name", "Concept");
        graphEntity.Properties["shared"] = "same-value";

        // Act
        var result = await _sut.CompareAsync(docEntity, graphEntity);

        // Assert
        Assert.False(result.HasDifferences);
    }

    [Fact]
    public async Task CompareAsync_WithNumericPropertyDifference_DetectsDifference()
    {
        // Arrange
        var docEntity = CreateTestEntity("Name", "Concept");
        docEntity.Properties["count"] = 10;

        var graphEntity = CreateTestEntity("Name", "Concept");
        graphEntity.Properties["count"] = 20;

        // Act
        var result = await _sut.CompareAsync(docEntity, graphEntity);

        // Assert
        Assert.True(result.HasDifferences);
        var diff = result.PropertyDifferences.FirstOrDefault(d => d.PropertyName == "count");
        Assert.NotNull(diff);
    }

    #endregion

    #region CompareAsync Tests - Multiple Differences

    [Fact]
    public async Task CompareAsync_WithMultipleDifferences_DetectsAll()
    {
        // Arrange
        var docEntity = CreateTestEntity("DocName", "DocType");
        docEntity.Properties["prop1"] = "doc1";

        var graphEntity = CreateTestEntity("GraphName", "GraphType");
        graphEntity.Properties["prop1"] = "graph1";

        // Act
        var result = await _sut.CompareAsync(docEntity, graphEntity);

        // Assert
        Assert.True(result.HasDifferences);
        Assert.True(result.DifferenceCount >= 3); // Name, Type, prop1
    }

    #endregion

    #region CompareAsync Tests - Entity References

    [Fact]
    public async Task CompareAsync_PreservesEntityReferences()
    {
        // Arrange
        var docEntity = CreateTestEntity("Doc", "Concept");
        var graphEntity = CreateTestEntity("Graph", "Concept");

        // Act
        var result = await _sut.CompareAsync(docEntity, graphEntity);

        // Assert
        Assert.Same(docEntity, result.DocumentEntity);
        Assert.Same(graphEntity, result.GraphEntity);
    }

    #endregion

    #region CompareAsync Tests - Cancellation

    [Fact]
    public async Task CompareAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var docEntity = CreateTestEntity("Name", "Concept");
        docEntity.Properties["prop1"] = "value1";
        docEntity.Properties["prop2"] = "value2";
        docEntity.Properties["prop3"] = "value3";

        var graphEntity = CreateTestEntity("Name", "Concept");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.CompareAsync(docEntity, graphEntity, cts.Token));
    }

    #endregion

    #region CompareAsync Tests - Case Insensitivity

    [Fact]
    public async Task CompareAsync_WithCaseDifference_TreatsAsEqual()
    {
        // Arrange
        var docEntity = CreateTestEntity("TestName", "Concept");
        var graphEntity = CreateTestEntity("TESTNAME", "CONCEPT");

        // Act
        var result = await _sut.CompareAsync(docEntity, graphEntity);

        // Assert: Case-insensitive comparison means no differences
        Assert.False(result.HasDifferences);
    }

    [Fact]
    public async Task CompareAsync_WithCaseDifferentProperties_TreatsAsEqual()
    {
        // Arrange
        var docEntity = CreateTestEntity("Name", "Concept");
        docEntity.Properties["key"] = "Value";

        var graphEntity = CreateTestEntity("Name", "Concept");
        graphEntity.Properties["key"] = "VALUE";

        // Act
        var result = await _sut.CompareAsync(docEntity, graphEntity);

        // Assert: String properties are compared case-insensitively
        Assert.False(result.HasDifferences);
    }

    #endregion

    #region CompareAsync Tests - Confidence Scoring

    [Fact]
    public async Task CompareAsync_WithSimilarStrings_ReturnsLowConfidence()
    {
        // Arrange
        var docEntity = CreateTestEntity("TestName", "Concept");
        var graphEntity = CreateTestEntity("TestNam", "Concept"); // Missing 'e'

        // Act
        var result = await _sut.CompareAsync(docEntity, graphEntity);

        // Assert
        Assert.True(result.HasDifferences);
        var nameDiff = result.PropertyDifferences.First(d => d.PropertyName == "Name");
        // Similar strings should have low confidence in the difference
        Assert.True(nameDiff.Confidence < 0.5f);
    }

    [Fact]
    public async Task CompareAsync_WithCompletelyDifferentStrings_ReturnsHighConfidence()
    {
        // Arrange
        var docEntity = CreateTestEntity("ABC", "Concept");
        var graphEntity = CreateTestEntity("XYZ", "Concept");

        // Act
        var result = await _sut.CompareAsync(docEntity, graphEntity);

        // Assert
        Assert.True(result.HasDifferences);
        var nameDiff = result.PropertyDifferences.First(d => d.PropertyName == "Name");
        // Completely different strings should have high confidence in the difference
        Assert.True(nameDiff.Confidence > 0.5f);
    }

    #endregion

    #region Helper Methods

    private static KnowledgeEntity CreateTestEntity(string name, string type)
    {
        return new KnowledgeEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Properties = new Dictionary<string, object>(),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            ModifiedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
    }

    #endregion
}
