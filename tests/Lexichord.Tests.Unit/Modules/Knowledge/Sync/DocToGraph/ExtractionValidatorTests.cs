// =============================================================================
// File: ExtractionValidatorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ExtractionValidator.
// =============================================================================
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;
using Lexichord.Modules.Knowledge.Sync.DocToGraph;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.DocToGraph;

/// <summary>
/// Unit tests for <see cref="ExtractionValidator"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6f")]
public class ExtractionValidatorTests
{
    private readonly Mock<ILogger<ExtractionValidator>> _mockLogger;
    private readonly ExtractionValidator _sut;

    public ExtractionValidatorTests()
    {
        _mockLogger = new Mock<ILogger<ExtractionValidator>>();
        _sut = new ExtractionValidator(_mockLogger.Object);
    }

    #region ValidateAsync Tests

    [Fact]
    public async Task ValidateAsync_WithValidExtraction_ReturnsSuccess()
    {
        // Arrange
        var extraction = CreateTestExtractionResult();
        var context = CreateTestValidationContext();

        // Act
        var result = await _sut.ValidateAsync(extraction, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyEntityType_ReturnsError()
    {
        // Arrange
        var extraction = new ExtractionResult
        {
            Mentions = [],
            AggregatedEntities =
            [
                new AggregatedEntity
                {
                    CanonicalValue = "Test Value",
                    EntityType = "", // Empty type
                    Mentions = [],
                    MaxConfidence = 0.9f,
                    MergedProperties = new Dictionary<string, object>()
                }
            ]
        };
        var context = CreateTestValidationContext();

        // Act
        var result = await _sut.ValidateAsync(extraction, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "VAL-001");
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyEntityValue_ReturnsError()
    {
        // Arrange
        var extraction = new ExtractionResult
        {
            Mentions = [],
            AggregatedEntities =
            [
                new AggregatedEntity
                {
                    CanonicalValue = "", // Empty value
                    EntityType = "Product",
                    Mentions = [],
                    MaxConfidence = 0.9f,
                    MergedProperties = new Dictionary<string, object>()
                }
            ]
        };
        var context = CreateTestValidationContext();

        // Act
        var result = await _sut.ValidateAsync(extraction, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "VAL-002");
    }

    [Fact]
    public async Task ValidateAsync_InStrictMode_RejectsUnknownEntityTypes()
    {
        // Arrange
        var extraction = new ExtractionResult
        {
            Mentions = [],
            AggregatedEntities =
            [
                new AggregatedEntity
                {
                    CanonicalValue = "Test Value",
                    EntityType = "UnknownType", // Not in known types
                    Mentions = [],
                    MaxConfidence = 0.9f,
                    MergedProperties = new Dictionary<string, object>()
                }
            ]
        };
        var context = new DocToGraphValidationContext
        {
            DocumentId = Guid.NewGuid(),
            StrictMode = true
        };

        // Act
        var result = await _sut.ValidateAsync(extraction, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "VAL-003");
    }

    [Fact]
    public async Task ValidateAsync_InLenientMode_WarnsOnUnknownEntityTypes()
    {
        // Arrange
        var extraction = new ExtractionResult
        {
            Mentions = [],
            AggregatedEntities =
            [
                new AggregatedEntity
                {
                    CanonicalValue = "Test Value",
                    EntityType = "UnknownType", // Not in known types
                    Mentions = [],
                    MaxConfidence = 0.9f,
                    MergedProperties = new Dictionary<string, object>()
                }
            ]
        };
        var context = new DocToGraphValidationContext
        {
            DocumentId = Guid.NewGuid(),
            StrictMode = false
        };

        // Act
        var result = await _sut.ValidateAsync(extraction, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Code == "WARN-001");
    }

    [Fact]
    public async Task ValidateAsync_WithAllowedEntityTypes_RejectsDisallowedTypes()
    {
        // Arrange
        var extraction = new ExtractionResult
        {
            Mentions = [],
            AggregatedEntities =
            [
                new AggregatedEntity
                {
                    CanonicalValue = "Test Value",
                    EntityType = "Endpoint", // Not in allowed list
                    Mentions = [],
                    MaxConfidence = 0.9f,
                    MergedProperties = new Dictionary<string, object>()
                }
            ]
        };
        var context = new DocToGraphValidationContext
        {
            DocumentId = Guid.NewGuid(),
            AllowedEntityTypes = new HashSet<string> { "Product", "Component" } // Endpoint not allowed
        };

        // Act
        var result = await _sut.ValidateAsync(extraction, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "VAL-004");
    }

    [Fact]
    public async Task ValidateAsync_InStrictMode_RejectsLowConfidenceEntities()
    {
        // Arrange
        var extraction = new ExtractionResult
        {
            Mentions = [],
            AggregatedEntities =
            [
                new AggregatedEntity
                {
                    CanonicalValue = "Test Value",
                    EntityType = "Product",
                    Mentions = [],
                    MaxConfidence = 0.3f, // Low confidence
                    MergedProperties = new Dictionary<string, object>()
                }
            ]
        };
        var context = new DocToGraphValidationContext
        {
            DocumentId = Guid.NewGuid(),
            StrictMode = true
        };

        // Act
        var result = await _sut.ValidateAsync(extraction, context);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "VAL-005");
    }

    [Fact]
    public async Task ValidateAsync_InLenientMode_WarnsOnLowConfidence()
    {
        // Arrange
        var extraction = new ExtractionResult
        {
            Mentions = [],
            AggregatedEntities =
            [
                new AggregatedEntity
                {
                    CanonicalValue = "Test Value",
                    EntityType = "Product",
                    Mentions = [],
                    MaxConfidence = 0.3f, // Low confidence
                    MergedProperties = new Dictionary<string, object>()
                }
            ]
        };
        var context = new DocToGraphValidationContext
        {
            DocumentId = Guid.NewGuid(),
            StrictMode = false
        };

        // Act
        var result = await _sut.ValidateAsync(extraction, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Code == "WARN-003");
    }

    [Fact]
    public async Task ValidateAsync_WithNullAggregatedEntities_ReturnsSuccess()
    {
        // Arrange
        var extraction = new ExtractionResult
        {
            Mentions = [],
            AggregatedEntities = null
        };
        var context = CreateTestValidationContext();

        // Act
        var result = await _sut.ValidateAsync(extraction, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(0, result.EntitiesValidated);
    }

    [Fact]
    public async Task ValidateAsync_CountsValidatedEntities()
    {
        // Arrange
        var extraction = CreateTestExtractionResult();
        var context = CreateTestValidationContext();

        // Act
        var result = await _sut.ValidateAsync(extraction, context);

        // Assert
        Assert.Equal(extraction.AggregatedEntities!.Count, result.EntitiesValidated);
    }

    #endregion

    #region ValidateEntityAsync Tests

    [Fact]
    public async Task ValidateEntityAsync_WithValidEntity_ReturnsSuccess()
    {
        // Arrange
        var entity = CreateTestEntity();

        // Act
        var result = await _sut.ValidateEntityAsync(entity);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(entity.Id, result.EntityId);
    }

    [Fact]
    public async Task ValidateEntityAsync_WithEmptyType_ReturnsError()
    {
        // Arrange
        var entity = new KnowledgeEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Entity",
            Type = "", // Empty type
            Properties = new Dictionary<string, object>(),
            SourceDocuments = [],
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _sut.ValidateEntityAsync(entity);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "VAL-001");
    }

    [Fact]
    public async Task ValidateEntityAsync_WithEmptyName_ReturnsError()
    {
        // Arrange
        var entity = new KnowledgeEntity
        {
            Id = Guid.NewGuid(),
            Name = "", // Empty name
            Type = "Product",
            Properties = new Dictionary<string, object>(),
            SourceDocuments = [],
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _sut.ValidateEntityAsync(entity);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "VAL-002");
    }

    [Fact]
    public async Task ValidateEntityAsync_WithUnknownType_ReturnsWarning()
    {
        // Arrange
        var entity = new KnowledgeEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Entity",
            Type = "UnknownType", // Not in known types
            Properties = new Dictionary<string, object>(),
            SourceDocuments = [],
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _sut.ValidateEntityAsync(entity);

        // Assert
        // Unknown type generates a warning (severity = Warning), not an outright failure
        Assert.Contains(result.Errors, e => e.Code == "VAL-003" && e.Severity == ValidationSeverity.Warning);
    }

    #endregion

    #region ValidateRelationshipsAsync Tests

    [Fact]
    public async Task ValidateRelationshipsAsync_WithValidRelationships_ReturnsSuccess()
    {
        // Arrange
        var entity1 = CreateTestEntity();
        var entity2 = CreateTestEntity();
        var entities = new List<KnowledgeEntity> { entity1, entity2 };

        var relationships = new List<KnowledgeRelationship>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Type = "RELATED_TO",
                FromEntityId = entity1.Id,
                ToEntityId = entity2.Id,
                Properties = new Dictionary<string, object>(),
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        // Act
        var result = await _sut.ValidateRelationshipsAsync(relationships, entities);

        // Assert
        Assert.True(result.AllValid);
        Assert.Empty(result.InvalidRelationships);
    }

    [Fact]
    public async Task ValidateRelationshipsAsync_WithEmptyType_ReturnsError()
    {
        // Arrange
        var entity1 = CreateTestEntity();
        var entity2 = CreateTestEntity();
        var entities = new List<KnowledgeEntity> { entity1, entity2 };

        var relationships = new List<KnowledgeRelationship>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Type = "", // Empty type
                FromEntityId = entity1.Id,
                ToEntityId = entity2.Id,
                Properties = new Dictionary<string, object>(),
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        // Act
        var result = await _sut.ValidateRelationshipsAsync(relationships, entities);

        // Assert
        Assert.False(result.AllValid);
        Assert.Contains(result.InvalidRelationships, r => r.Error.Code == "VAL-006");
    }

    [Fact]
    public async Task ValidateRelationshipsAsync_WithInvalidFromEntityId_ReturnsError()
    {
        // Arrange
        var entity1 = CreateTestEntity();
        var entity2 = CreateTestEntity();
        var entities = new List<KnowledgeEntity> { entity1, entity2 };

        var relationships = new List<KnowledgeRelationship>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Type = "RELATED_TO",
                FromEntityId = Guid.NewGuid(), // Non-existent entity
                ToEntityId = entity2.Id,
                Properties = new Dictionary<string, object>(),
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        // Act
        var result = await _sut.ValidateRelationshipsAsync(relationships, entities);

        // Assert
        Assert.False(result.AllValid);
        Assert.Contains(result.InvalidRelationships, r => r.Error.Code == "VAL-007");
    }

    [Fact]
    public async Task ValidateRelationshipsAsync_WithInvalidToEntityId_ReturnsError()
    {
        // Arrange
        var entity1 = CreateTestEntity();
        var entity2 = CreateTestEntity();
        var entities = new List<KnowledgeEntity> { entity1, entity2 };

        var relationships = new List<KnowledgeRelationship>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Type = "RELATED_TO",
                FromEntityId = entity1.Id,
                ToEntityId = Guid.NewGuid(), // Non-existent entity
                Properties = new Dictionary<string, object>(),
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        // Act
        var result = await _sut.ValidateRelationshipsAsync(relationships, entities);

        // Assert
        Assert.False(result.AllValid);
        Assert.Contains(result.InvalidRelationships, r => r.Error.Code == "VAL-008");
    }

    [Fact]
    public async Task ValidateRelationshipsAsync_WithSelfReference_ReturnsWarning()
    {
        // Arrange
        var entity = CreateTestEntity();
        var entities = new List<KnowledgeEntity> { entity };

        var relationships = new List<KnowledgeRelationship>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Type = "RELATED_TO",
                FromEntityId = entity.Id,
                ToEntityId = entity.Id, // Self-reference
                Properties = new Dictionary<string, object>(),
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        // Act
        var result = await _sut.ValidateRelationshipsAsync(relationships, entities);

        // Assert
        Assert.False(result.AllValid);
        Assert.Contains(result.InvalidRelationships, r => r.Error.Code == "VAL-009");
    }

    [Fact]
    public async Task ValidateRelationshipsAsync_WithEmptyList_ReturnsSuccess()
    {
        // Arrange
        var entities = new List<KnowledgeEntity> { CreateTestEntity() };
        var relationships = new List<KnowledgeRelationship>();

        // Act
        var result = await _sut.ValidateRelationshipsAsync(relationships, entities);

        // Assert
        Assert.True(result.AllValid);
    }

    #endregion

    #region Helper Methods

    private static ExtractionResult CreateTestExtractionResult()
    {
        return new ExtractionResult
        {
            Mentions = [],
            AggregatedEntities =
            [
                new AggregatedEntity
                {
                    CanonicalValue = "Test Product",
                    EntityType = "Product",
                    Mentions = [],
                    MaxConfidence = 0.9f,
                    MergedProperties = new Dictionary<string, object>()
                },
                new AggregatedEntity
                {
                    CanonicalValue = "/api/test",
                    EntityType = "Endpoint",
                    Mentions = [],
                    MaxConfidence = 0.85f,
                    MergedProperties = new Dictionary<string, object>()
                }
            ]
        };
    }

    private static DocToGraphValidationContext CreateTestValidationContext()
    {
        return new DocToGraphValidationContext
        {
            DocumentId = Guid.NewGuid(),
            StrictMode = false
        };
    }

    private static KnowledgeEntity CreateTestEntity()
    {
        return new KnowledgeEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Entity",
            Type = "Product",
            Properties = new Dictionary<string, object>(),
            SourceDocuments = [],
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
