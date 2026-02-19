// =============================================================================
// File: ExtractionTransformerTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ExtractionTransformer.
// =============================================================================
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.Knowledge.Sync.DocToGraph;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.DocToGraph;

/// <summary>
/// Unit tests for <see cref="ExtractionTransformer"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6f")]
public class ExtractionTransformerTests
{
    private readonly Mock<IGraphRepository> _mockGraphRepository;
    private readonly Mock<ILogger<ExtractionTransformer>> _mockLogger;
    private readonly ExtractionTransformer _sut;

    public ExtractionTransformerTests()
    {
        _mockGraphRepository = new Mock<IGraphRepository>();
        _mockLogger = new Mock<ILogger<ExtractionTransformer>>();
        _sut = new ExtractionTransformer(_mockGraphRepository.Object, _mockLogger.Object);
    }

    #region TransformAsync Tests

    [Fact]
    public async Task TransformAsync_TransformsExtractionToIngestionData()
    {
        // Arrange
        var document = CreateTestDocument();
        var extraction = CreateTestExtractionResult();

        // Act
        var result = await _sut.TransformAsync(extraction, document);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(document.Id, result.SourceDocumentId);
        Assert.NotEmpty(result.Entities);
        Assert.NotNull(result.Metadata);
        Assert.Contains("documentTitle", result.Metadata.Keys);
    }

    [Fact]
    public async Task TransformAsync_WithEmptyExtraction_ReturnsEmptyIngestionData()
    {
        // Arrange
        var document = CreateTestDocument();
        var extraction = new ExtractionResult
        {
            Mentions = [],
            AggregatedEntities = []
        };

        // Act
        var result = await _sut.TransformAsync(extraction, document);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Entities);
        Assert.Empty(result.Relationships);
        Assert.Equal(document.Id, result.SourceDocumentId);
    }

    [Fact]
    public async Task TransformAsync_IncludesMetadataFromDocument()
    {
        // Arrange
        var document = CreateTestDocument();
        var extraction = CreateTestExtractionResult();

        // Act
        var result = await _sut.TransformAsync(extraction, document);

        // Assert
        Assert.Equal(document.Title, result.Metadata["documentTitle"]);
        Assert.Equal(document.Hash, result.Metadata["documentHash"]);
    }

    #endregion

    #region TransformEntitiesAsync Tests

    [Fact]
    public async Task TransformEntitiesAsync_TransformsAggregatedEntitiesToKnowledgeEntities()
    {
        // Arrange
        var aggregatedEntities = new List<AggregatedEntity>
        {
            CreateTestAggregatedEntity("Product", "Test Product"),
            CreateTestAggregatedEntity("Endpoint", "/api/test")
        };

        // Act
        var result = await _sut.TransformEntitiesAsync(aggregatedEntities);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Name == "Test Product");
        Assert.Contains(result, e => e.Name == "/api/test");
    }

    [Fact]
    public async Task TransformEntitiesAsync_PreservesEntityProperties()
    {
        // Arrange
        var aggregatedEntities = new List<AggregatedEntity>
        {
            new()
            {
                CanonicalValue = "TestEntity",
                EntityType = "Component",
                Mentions = [CreateTestMention()],
                MaxConfidence = 0.95f,
                MergedProperties = new Dictionary<string, object>
                {
                    ["customProperty"] = "customValue"
                }
            }
        };

        // Act
        var result = await _sut.TransformEntitiesAsync(aggregatedEntities);

        // Assert
        var entity = result[0];
        Assert.Equal("customValue", entity.Properties["customProperty"]);
        Assert.Equal(0.95f, entity.Properties["confidence"]);
    }

    [Fact]
    public async Task TransformEntitiesAsync_NormalizesEntityTypes()
    {
        // Arrange
        var aggregatedEntities = new List<AggregatedEntity>
        {
            CreateTestAggregatedEntity("api", "ApiEntity"),
            CreateTestAggregatedEntity("function", "FunctionEntity"),
            CreateTestAggregatedEntity("class", "ClassEntity")
        };

        // Act
        var result = await _sut.TransformEntitiesAsync(aggregatedEntities);

        // Assert
        Assert.Contains(result, e => e.Type == "Endpoint"); // api -> Endpoint
        Assert.Contains(result, e => e.Type == "Method"); // function -> Method
        Assert.Contains(result, e => e.Type == "Component"); // class -> Component
    }

    [Fact]
    public async Task TransformEntitiesAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var aggregatedEntities = new List<AggregatedEntity>();

        // Act
        var result = await _sut.TransformEntitiesAsync(aggregatedEntities);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task TransformEntitiesAsync_AssignsNewGuidsToEntities()
    {
        // Arrange
        var aggregatedEntities = new List<AggregatedEntity>
        {
            CreateTestAggregatedEntity("Product", "Entity1"),
            CreateTestAggregatedEntity("Product", "Entity2")
        };

        // Act
        var result = await _sut.TransformEntitiesAsync(aggregatedEntities);

        // Assert
        Assert.NotEqual(Guid.Empty, result[0].Id);
        Assert.NotEqual(Guid.Empty, result[1].Id);
        Assert.NotEqual(result[0].Id, result[1].Id);
    }

    #endregion

    #region DeriveRelationshipsAsync Tests

    [Fact]
    public async Task DeriveRelationshipsAsync_DerivesRelationshipsFromCoOccurrence()
    {
        // Arrange
        var entity1 = CreateTestKnowledgeEntity("Entity1", "Product");
        var entity2 = CreateTestKnowledgeEntity("Entity2", "Endpoint");
        var entities = new List<KnowledgeEntity> { entity1, entity2 };

        var extraction = new ExtractionResult
        {
            Mentions =
            [
                CreateTestMention(entity1.Name),
                CreateTestMention(entity2.Name)
            ],
            AggregatedEntities = []
        };

        // Act
        var result = await _sut.DeriveRelationshipsAsync(entities, extraction);

        // Assert
        Assert.Single(result);
        Assert.Equal("RELATED_TO", result[0].Type);
    }

    [Fact]
    public async Task DeriveRelationshipsAsync_WithNoMentions_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<KnowledgeEntity>
        {
            CreateTestKnowledgeEntity("Entity1", "Product"),
            CreateTestKnowledgeEntity("Entity2", "Endpoint")
        };

        var extraction = new ExtractionResult
        {
            Mentions = [],
            AggregatedEntities = []
        };

        // Act
        var result = await _sut.DeriveRelationshipsAsync(entities, extraction);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DeriveRelationshipsAsync_IncludesConfidenceInProperties()
    {
        // Arrange
        var entity1 = CreateTestKnowledgeEntity("Entity1", "Component");
        var entity2 = CreateTestKnowledgeEntity("Entity2", "Method");
        var entities = new List<KnowledgeEntity> { entity1, entity2 };

        var extraction = new ExtractionResult
        {
            Mentions =
            [
                CreateTestMention(entity1.Name),
                CreateTestMention(entity2.Name)
            ],
            AggregatedEntities = []
        };

        // Act
        var result = await _sut.DeriveRelationshipsAsync(entities, extraction);

        // Assert
        Assert.Single(result);
        Assert.Equal("co-occurrence", result[0].Properties["derivedFrom"]);
        Assert.Equal(0.6, result[0].Properties["confidence"]);
    }

    [Fact]
    public async Task DeriveRelationshipsAsync_DeterminesRelationshipTypeByEntityTypes()
    {
        // Arrange
        var component = CreateTestKnowledgeEntity("MyComponent", "Component");
        var method = CreateTestKnowledgeEntity("MyMethod", "Method");
        var entities = new List<KnowledgeEntity> { component, method };

        var extraction = new ExtractionResult
        {
            Mentions =
            [
                CreateTestMention(component.Name),
                CreateTestMention(method.Name)
            ],
            AggregatedEntities = []
        };

        // Act
        var result = await _sut.DeriveRelationshipsAsync(entities, extraction);

        // Assert
        Assert.Single(result);
        Assert.Equal("CONTAINS", result[0].Type);
    }

    #endregion

    #region EnrichEntitiesAsync Tests

    [Fact]
    public async Task EnrichEntitiesAsync_AddsEnrichmentMetadata()
    {
        // Arrange
        var entities = new List<KnowledgeEntity>
        {
            CreateTestKnowledgeEntity("TestEntity", "Product")
        };

        _mockGraphRepository
            .Setup(x => x.SearchEntitiesAsync(It.IsAny<EntitySearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeEntity>());

        // Act
        var result = await _sut.EnrichEntitiesAsync(entities);

        // Assert
        Assert.Single(result);
        Assert.Contains("enrichedAt", result[0].Properties.Keys);
        Assert.Contains("isUpdate", result[0].Properties.Keys);
        Assert.False((bool)result[0].Properties["isUpdate"]);
    }

    [Fact]
    public async Task EnrichEntitiesAsync_MarksExactMatchesAsUpdates()
    {
        // Arrange
        var existingEntity = CreateTestKnowledgeEntity("TestEntity", "Product");
        var newEntity = CreateTestKnowledgeEntity("TestEntity", "Product");
        var entities = new List<KnowledgeEntity> { newEntity };

        _mockGraphRepository
            .Setup(x => x.SearchEntitiesAsync(It.IsAny<EntitySearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeEntity> { existingEntity });

        // Act
        var result = await _sut.EnrichEntitiesAsync(entities);

        // Assert
        Assert.Single(result);
        Assert.True((bool)result[0].Properties["isUpdate"]);
        Assert.Equal(existingEntity.Id.ToString(), result[0].Properties["existingEntityId"]);
    }

    [Fact]
    public async Task EnrichEntitiesAsync_IncludesSimilarEntityIds()
    {
        // Arrange
        var similarEntity1 = CreateTestKnowledgeEntity("SimilarEntity1", "Product");
        var similarEntity2 = CreateTestKnowledgeEntity("SimilarEntity2", "Product");
        var newEntity = CreateTestKnowledgeEntity("TestEntity", "Product");
        var entities = new List<KnowledgeEntity> { newEntity };

        _mockGraphRepository
            .Setup(x => x.SearchEntitiesAsync(It.IsAny<EntitySearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeEntity> { similarEntity1, similarEntity2 });

        // Act
        var result = await _sut.EnrichEntitiesAsync(entities);

        // Assert
        var similarIds = (List<string>)result[0].Properties["similarEntityIds"];
        Assert.Equal(2, similarIds.Count);
        Assert.Contains(similarEntity1.Id.ToString(), similarIds);
        Assert.Contains(similarEntity2.Id.ToString(), similarIds);
    }

    [Fact]
    public async Task EnrichEntitiesAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<KnowledgeEntity>();

        // Act
        var result = await _sut.EnrichEntitiesAsync(entities);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Helper Methods

    private static Document CreateTestDocument()
    {
        return new Document(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            FilePath: "/test/path/document.md",
            Title: "Test Document",
            Hash: "test-hash-123",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow,
            FailureReason: null
        );
    }

    private static ExtractionResult CreateTestExtractionResult()
    {
        return new ExtractionResult
        {
            Mentions =
            [
                CreateTestMention("Test Product"),
                CreateTestMention("/api/test")
            ],
            AggregatedEntities =
            [
                CreateTestAggregatedEntity("Product", "Test Product"),
                CreateTestAggregatedEntity("Endpoint", "/api/test")
            ]
        };
    }

    private static AggregatedEntity CreateTestAggregatedEntity(string type, string value)
    {
        return new AggregatedEntity
        {
            CanonicalValue = value,
            EntityType = type,
            Mentions = [CreateTestMention(value)],
            MaxConfidence = 0.9f,
            MergedProperties = new Dictionary<string, object>()
        };
    }

    private static EntityMention CreateTestMention(string? normalizedValue = null)
    {
        return new EntityMention
        {
            Value = normalizedValue ?? "Test Mention",
            NormalizedValue = normalizedValue ?? "Test Mention",
            EntityType = "Product",
            Confidence = 0.9f,
            StartOffset = 0,
            EndOffset = 10
        };
    }

    private static KnowledgeEntity CreateTestKnowledgeEntity(string name, string type)
    {
        return new KnowledgeEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Properties = new Dictionary<string, object>(),
            SourceDocuments = [],
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
