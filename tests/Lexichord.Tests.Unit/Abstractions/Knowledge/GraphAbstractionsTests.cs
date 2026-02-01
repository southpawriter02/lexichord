// =============================================================================
// File: GraphAbstractionsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for Knowledge Graph abstraction types.
// =============================================================================
// LOGIC: Validates the correctness of all graph abstraction types defined in
//   Lexichord.Abstractions.Contracts: GraphWriteResult, GraphQueryException,
//   KnowledgeEntity, KnowledgeRelationship, and GraphAccessMode.
//
// Test Categories:
//   - GraphWriteResult: Defaults, TotalAffected, Empty sentinel, equality
//   - GraphQueryException: Constructor variants, message propagation
//   - KnowledgeEntity: Default values, required properties, collections
//   - KnowledgeRelationship: Default values, required properties
//   - GraphAccessMode: Enum values
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge;

/// <summary>
/// Unit tests for Knowledge Graph abstraction records, exceptions, and enums.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5e")]
public sealed class GraphAbstractionsTests
{
    #region GraphWriteResult Tests

    [Fact]
    public void GraphWriteResult_DefaultValues_AllZero()
    {
        // Arrange & Act
        var result = new GraphWriteResult();

        // Assert
        result.NodesCreated.Should().Be(0);
        result.NodesDeleted.Should().Be(0);
        result.RelationshipsCreated.Should().Be(0);
        result.RelationshipsDeleted.Should().Be(0);
        result.PropertiesSet.Should().Be(0);
    }

    [Fact]
    public void GraphWriteResult_TotalAffected_SumsAllCounters()
    {
        // Arrange
        var result = new GraphWriteResult
        {
            NodesCreated = 1,
            NodesDeleted = 2,
            RelationshipsCreated = 3,
            RelationshipsDeleted = 4,
            PropertiesSet = 5
        };

        // Act
        var total = result.TotalAffected;

        // Assert
        total.Should().Be(15);
    }

    [Fact]
    public void GraphWriteResult_TotalAffected_ZeroWhenEmpty()
    {
        // Arrange
        var result = new GraphWriteResult();

        // Act & Assert
        result.TotalAffected.Should().Be(0);
    }

    [Fact]
    public void GraphWriteResult_Empty_IsDefaultInstance()
    {
        // Arrange & Act
        var empty = GraphWriteResult.Empty;

        // Assert
        empty.NodesCreated.Should().Be(0);
        empty.NodesDeleted.Should().Be(0);
        empty.RelationshipsCreated.Should().Be(0);
        empty.RelationshipsDeleted.Should().Be(0);
        empty.PropertiesSet.Should().Be(0);
        empty.TotalAffected.Should().Be(0);
    }

    [Fact]
    public void GraphWriteResult_Empty_IsSingleton()
    {
        // Arrange & Act
        var empty1 = GraphWriteResult.Empty;
        var empty2 = GraphWriteResult.Empty;

        // Assert
        empty1.Should().BeSameAs(empty2);
    }

    [Fact]
    public void GraphWriteResult_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var result1 = new GraphWriteResult { NodesCreated = 1, PropertiesSet = 2 };
        var result2 = new GraphWriteResult { NodesCreated = 1, PropertiesSet = 2 };
        var result3 = new GraphWriteResult { NodesCreated = 1, PropertiesSet = 3 };

        // Assert
        result1.Should().Be(result2);
        result1.Should().NotBe(result3);
    }

    [Fact]
    public void GraphWriteResult_WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = new GraphWriteResult { NodesCreated = 1 };

        // Act
        var modified = original with { NodesDeleted = 5 };

        // Assert
        modified.NodesCreated.Should().Be(1);
        modified.NodesDeleted.Should().Be(5);
        modified.TotalAffected.Should().Be(6);
    }

    #endregion

    #region GraphQueryException Tests

    [Fact]
    public void GraphQueryException_MessageConstructor_SetsMessage()
    {
        // Arrange & Act
        var ex = new GraphQueryException("test error");

        // Assert
        ex.Message.Should().Be("test error");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void GraphQueryException_MessageAndInnerConstructor_SetsBoth()
    {
        // Arrange
        var inner = new InvalidOperationException("inner error");

        // Act
        var ex = new GraphQueryException("outer error", inner);

        // Assert
        ex.Message.Should().Be("outer error");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void GraphQueryException_IsException()
    {
        // Arrange & Act
        var ex = new GraphQueryException("test");

        // Assert
        ex.Should().BeAssignableTo<Exception>();
    }

    #endregion

    #region KnowledgeEntity Tests

    [Fact]
    public void KnowledgeEntity_DefaultId_IsNewGuid()
    {
        // Arrange & Act
        var entity = new KnowledgeEntity { Type = "Product", Name = "Test" };

        // Assert
        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void KnowledgeEntity_DefaultProperties_EmptyDictionary()
    {
        // Arrange & Act
        var entity = new KnowledgeEntity { Type = "Product", Name = "Test" };

        // Assert
        entity.Properties.Should().NotBeNull();
        entity.Properties.Should().BeEmpty();
    }

    [Fact]
    public void KnowledgeEntity_DefaultSourceDocuments_EmptyList()
    {
        // Arrange & Act
        var entity = new KnowledgeEntity { Type = "Product", Name = "Test" };

        // Assert
        entity.SourceDocuments.Should().NotBeNull();
        entity.SourceDocuments.Should().BeEmpty();
    }

    [Fact]
    public void KnowledgeEntity_DefaultTimestamps_AreRecentUtc()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        // Act
        var entity = new KnowledgeEntity { Type = "Product", Name = "Test" };

        // Assert
        entity.CreatedAt.Should().BeAfter(before);
        entity.ModifiedAt.Should().BeAfter(before);
    }

    [Fact]
    public void KnowledgeEntity_WithProperties_StoresCorrectly()
    {
        // Arrange & Act
        var entity = new KnowledgeEntity
        {
            Type = "Endpoint",
            Name = "GET /api/users",
            Properties = new Dictionary<string, object>
            {
                ["path"] = "/api/users",
                ["method"] = "GET"
            }
        };

        // Assert
        entity.Properties.Should().HaveCount(2);
        entity.Properties["path"].Should().Be("/api/users");
        entity.Properties["method"].Should().Be("GET");
    }

    [Fact]
    public void KnowledgeEntity_RecordEquality_ByValue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var entity1 = new KnowledgeEntity
        {
            Id = id, Type = "Product", Name = "Test",
            CreatedAt = now, ModifiedAt = now
        };
        var entity2 = new KnowledgeEntity
        {
            Id = id, Type = "Product", Name = "Test",
            CreatedAt = now, ModifiedAt = now
        };

        // Assert — record equality compares each field; Dictionary uses reference equality,
        // so we use BeEquivalentTo for deep structural comparison.
        entity1.Should().BeEquivalentTo(entity2);
    }

    [Fact]
    public void KnowledgeEntity_WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = new KnowledgeEntity { Type = "Product", Name = "Original" };

        // Act
        var modified = original with { Name = "Modified" };

        // Assert
        modified.Type.Should().Be("Product");
        modified.Name.Should().Be("Modified");
        modified.Id.Should().Be(original.Id);
    }

    [Fact]
    public void KnowledgeEntity_SourceDocuments_CanBePopulated()
    {
        // Arrange
        var docId1 = Guid.NewGuid();
        var docId2 = Guid.NewGuid();

        // Act
        var entity = new KnowledgeEntity
        {
            Type = "Product",
            Name = "Test",
            SourceDocuments = [docId1, docId2]
        };

        // Assert
        entity.SourceDocuments.Should().HaveCount(2);
        entity.SourceDocuments.Should().Contain(docId1);
        entity.SourceDocuments.Should().Contain(docId2);
    }

    #endregion

    #region KnowledgeRelationship Tests

    [Fact]
    public void KnowledgeRelationship_DefaultId_IsNewGuid()
    {
        // Arrange & Act
        var rel = new KnowledgeRelationship
        {
            Type = "CONTAINS",
            FromEntityId = Guid.NewGuid(),
            ToEntityId = Guid.NewGuid()
        };

        // Assert
        rel.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void KnowledgeRelationship_DefaultProperties_EmptyDictionary()
    {
        // Arrange & Act
        var rel = new KnowledgeRelationship
        {
            Type = "CONTAINS",
            FromEntityId = Guid.NewGuid(),
            ToEntityId = Guid.NewGuid()
        };

        // Assert
        rel.Properties.Should().NotBeNull();
        rel.Properties.Should().BeEmpty();
    }

    [Fact]
    public void KnowledgeRelationship_DefaultCreatedAt_IsRecentUtc()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        // Act
        var rel = new KnowledgeRelationship
        {
            Type = "CONTAINS",
            FromEntityId = Guid.NewGuid(),
            ToEntityId = Guid.NewGuid()
        };

        // Assert
        rel.CreatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void KnowledgeRelationship_WithProperties_StoresCorrectly()
    {
        // Arrange & Act
        var rel = new KnowledgeRelationship
        {
            Type = "ACCEPTS",
            FromEntityId = Guid.NewGuid(),
            ToEntityId = Guid.NewGuid(),
            Properties = new Dictionary<string, object>
            {
                ["location"] = "query",
                ["required"] = true
            }
        };

        // Assert
        rel.Properties.Should().HaveCount(2);
        rel.Properties["location"].Should().Be("query");
        rel.Properties["required"].Should().Be(true);
    }

    [Fact]
    public void KnowledgeRelationship_RecordEquality_ByValue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var rel1 = new KnowledgeRelationship
        {
            Id = id, Type = "CONTAINS", FromEntityId = from,
            ToEntityId = to, CreatedAt = now
        };
        var rel2 = new KnowledgeRelationship
        {
            Id = id, Type = "CONTAINS", FromEntityId = from,
            ToEntityId = to, CreatedAt = now
        };

        // Assert — record equality compares each field; Dictionary uses reference equality,
        // so we use BeEquivalentTo for deep structural comparison.
        rel1.Should().BeEquivalentTo(rel2);
    }

    #endregion

    #region GraphAccessMode Tests

    [Fact]
    public void GraphAccessMode_Read_HasValue0()
    {
        // Assert
        ((int)GraphAccessMode.Read).Should().Be(0);
    }

    [Fact]
    public void GraphAccessMode_Write_HasValue1()
    {
        // Assert
        ((int)GraphAccessMode.Write).Should().Be(1);
    }

    [Fact]
    public void GraphAccessMode_HasExactlyTwoValues()
    {
        // Assert
        Enum.GetValues<GraphAccessMode>().Should().HaveCount(2);
    }

    #endregion

    #region IGraphConnectionFactory Interface Tests

    [Fact]
    public void IGraphConnectionFactory_CanBeMocked()
    {
        // Arrange & Act
        var mock = new Mock<IGraphConnectionFactory>();
        mock.Setup(f => f.DatabaseName).Returns("neo4j");
        mock.Setup(f => f.TestConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Assert
        mock.Object.DatabaseName.Should().Be("neo4j");
    }

    [Fact]
    public async Task IGraphConnectionFactory_MockTestConnection_ReturnsTrue()
    {
        // Arrange
        var mock = new Mock<IGraphConnectionFactory>();
        mock.Setup(f => f.TestConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await mock.Object.TestConnectionAsync();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IGraphSession Interface Tests

    [Fact]
    public void IGraphSession_CanBeMocked()
    {
        // Arrange & Act
        var mock = new Mock<IGraphSession>();
        mock.Setup(s => s.QueryAsync<int>("RETURN 1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { 1 });

        // Assert
        mock.Object.Should().NotBeNull();
    }

    [Fact]
    public void IGraphSession_IsIAsyncDisposable()
    {
        // Assert
        typeof(IGraphSession).Should().Implement<IAsyncDisposable>();
    }

    #endregion

    #region IGraphTransaction Interface Tests

    [Fact]
    public void IGraphTransaction_IsIAsyncDisposable()
    {
        // Assert
        typeof(IGraphTransaction).Should().Implement<IAsyncDisposable>();
    }

    #endregion

    #region IGraphRecord Interface Tests

    [Fact]
    public void IGraphRecord_CanBeMocked()
    {
        // Arrange
        var mock = new Mock<IGraphRecord>();
        mock.Setup(r => r.Get<string>("name")).Returns("test");
        mock.Setup(r => r.Keys).Returns(new List<string> { "name" });

        // Act & Assert
        mock.Object.Get<string>("name").Should().Be("test");
        mock.Object.Keys.Should().ContainSingle("name");
    }

    #endregion
}
