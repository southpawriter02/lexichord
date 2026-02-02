// =============================================================================
// File: EntityCrudServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for EntityCrudService.
// Version: v0.4.7g
// =============================================================================
// LOGIC: Tests the EntityCrudService implementation for entity creation,
//   updating, merging, and deletion with validation and license gating.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Modules.Knowledge.Axioms;
using Lexichord.Modules.Knowledge.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="EntityCrudService"/>.
/// </summary>
/// <remarks>
/// Tests cover CRUD operations, validation, license gating, and event publishing.
/// </remarks>
public class EntityCrudServiceTests
{
    private readonly Mock<IGraphRepository> _graphRepositoryMock;
    private readonly Mock<ISchemaRegistry> _schemaRegistryMock;
    private readonly Mock<IAxiomStore> _axiomStoreMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly EntityCrudService _sut;

    public EntityCrudServiceTests()
    {
        _graphRepositoryMock = new Mock<IGraphRepository>();
        _schemaRegistryMock = new Mock<ISchemaRegistry>();
        _axiomStoreMock = new Mock<IAxiomStore>();
        _mediatorMock = new Mock<IMediator>();
        _licenseContextMock = new Mock<ILicenseContext>();

        // Default: Licensed at Teams tier
        _licenseContextMock.Setup(l => l.GetCurrentTier()).Returns(LicenseTier.Teams);

        // Default: Return valid entity types
        _schemaRegistryMock.Setup(s => s.EntityTypes)
            .Returns(new Dictionary<string, EntityTypeSchema>
            {
                ["Concept"] = new EntityTypeSchema
                {
                    Name = "Concept",
                    Properties = Array.Empty<PropertySchema>(),
                    RequiredProperties = new List<string>()
                },
                ["Endpoint"] = new EntityTypeSchema
                {
                    Name = "Endpoint",
                    Properties = Array.Empty<PropertySchema>(),
                    RequiredProperties = new List<string> { "path", "method" }
                }
            });

        _sut = new EntityCrudService(
            _graphRepositoryMock.Object,
            _schemaRegistryMock.Object,
            _axiomStoreMock.Object,
            _mediatorMock.Object,
            _licenseContextMock.Object,
            NullLogger<EntityCrudService>.Instance);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidEntity_Succeeds()
    {
        // Arrange
        var command = new CreateEntityCommand
        {
            Type = "Concept",
            Name = "Machine Learning",
            Properties = new Dictionary<string, object>
            {
                ["description"] = "A subset of AI"
            }
        };

        _graphRepositoryMock
            .Setup(r => r.CreateEntityAsync(It.IsAny<KnowledgeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeEntity e, CancellationToken _) => e);

        // Act
        var result = await _sut.CreateAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Entity);
        Assert.Equal("Machine Learning", result.Entity!.Name);
        Assert.Equal("Concept", result.Entity.Type);

        _graphRepositoryMock.Verify(
            r => r.CreateEntityAsync(It.IsAny<KnowledgeEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<EntityCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UnknownType_Fails()
    {
        // Arrange
        var command = new CreateEntityCommand
        {
            Type = "UnknownType",
            Name = "Test Entity",
            Properties = new Dictionary<string, object>()
        };

        // Act
        var result = await _sut.CreateAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Unknown entity type", result.Errors.First());

        _graphRepositoryMock.Verify(
            r => r.CreateEntityAsync(It.IsAny<KnowledgeEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithSourceDocument_LinksDocument()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var command = new CreateEntityCommand
        {
            Type = "Concept",
            Name = "Test Concept",
            Properties = new Dictionary<string, object>(),
            SourceDocumentId = docId
        };

        _graphRepositoryMock
            .Setup(r => r.CreateEntityAsync(It.IsAny<KnowledgeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeEntity e, CancellationToken _) => e);

        // Act
        var result = await _sut.CreateAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Entity);
        Assert.Contains(docId, result.Entity!.SourceDocuments);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidChanges_Succeeds()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var existingEntity = new KnowledgeEntity
        {
            Id = entityId,
            Type = "Concept",
            Name = "Original Name",
            Properties = new Dictionary<string, object>
            {
                ["description"] = "Original description"
            }
        };

        _graphRepositoryMock
            .Setup(r => r.GetByIdAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        var command = new UpdateEntityCommand
        {
            EntityId = entityId,
            Name = "Updated Name",
            SetProperties = new Dictionary<string, object?>
            {
                ["description"] = "Updated description"
            }
        };

        // Act
        var result = await _sut.UpdateAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Entity);
        Assert.Equal("Updated Name", result.Entity!.Name);
        Assert.Equal("Updated description", result.Entity.Properties["description"]);

        _graphRepositoryMock.Verify(
            r => r.UpdateEntityAsync(It.IsAny<KnowledgeEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<EntityUpdatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_EntityNotFound_Fails()
    {
        // Arrange
        var entityId = Guid.NewGuid();

        _graphRepositoryMock
            .Setup(r => r.GetByIdAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeEntity?)null);

        var command = new UpdateEntityCommand
        {
            EntityId = entityId,
            Name = "New Name"
        };

        // Act
        var result = await _sut.UpdateAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Entity not found", result.Errors.First());

        _graphRepositoryMock.Verify(
            r => r.UpdateEntityAsync(It.IsAny<KnowledgeEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RemoveProperties_RemovesFromEntity()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var existingEntity = new KnowledgeEntity
        {
            Id = entityId,
            Type = "Concept",
            Name = "Test",
            Properties = new Dictionary<string, object>
            {
                ["keep"] = "value1",
                ["remove"] = "value2"
            }
        };

        _graphRepositoryMock
            .Setup(r => r.GetByIdAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        var command = new UpdateEntityCommand
        {
            EntityId = entityId,
            RemoveProperties = new List<string> { "remove" }
        };

        // Act
        var result = await _sut.UpdateAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Entity!.Properties.ContainsKey("keep"));
        Assert.False(result.Entity.Properties.ContainsKey("remove"));
    }

    #endregion

    #region MergeAsync Tests

    [Fact]
    public async Task MergeAsync_TwoEntities_CombinesProperties()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        var target = new KnowledgeEntity
        {
            Id = targetId,
            Type = "Concept",
            Name = "Target",
            Properties = new Dictionary<string, object>
            {
                ["targetProp"] = "target value"
            }
        };

        var source = new KnowledgeEntity
        {
            Id = sourceId,
            Type = "Concept",
            Name = "Source",
            Properties = new Dictionary<string, object>
            {
                ["sourceProp"] = "source value"
            }
        };

        _graphRepositoryMock.Setup(r => r.GetByIdAsync(targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        _graphRepositoryMock.Setup(r => r.GetByIdAsync(sourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var command = new MergeEntitiesCommand
        {
            TargetEntityId = targetId,
            SourceEntityIds = new List<Guid> { sourceId },
            MergeStrategy = PropertyMergeStrategy.MergeAll
        };

        // Act
        var result = await _sut.MergeAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.MergedEntity);
        Assert.True(result.MergedEntity!.Properties.ContainsKey("targetProp"));
        Assert.True(result.MergedEntity.Properties.ContainsKey("sourceProp"));
        Assert.Single(result.RemovedEntityIds);
        Assert.Contains(sourceId, result.RemovedEntityIds);

        _graphRepositoryMock.Verify(
            r => r.DeleteEntityAsync(sourceId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MergeAsync_NoSourceEntities_Fails()
    {
        // Arrange
        var command = new MergeEntitiesCommand
        {
            TargetEntityId = Guid.NewGuid(),
            SourceEntityIds = new List<Guid>(),
            MergeStrategy = PropertyMergeStrategy.KeepTarget
        };

        // Act
        var result = await _sut.MergeAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("At least one source entity is required", result.Errors.First());
    }

    [Fact]
    public async Task MergeAsync_TargetInSourceList_Fails()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var command = new MergeEntitiesCommand
        {
            TargetEntityId = entityId,
            SourceEntityIds = new List<Guid> { entityId },
            MergeStrategy = PropertyMergeStrategy.KeepTarget
        };

        // Act
        var result = await _sut.MergeAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Target entity cannot be in the source entity list", result.Errors.First());
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ValidEntity_Succeeds()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new KnowledgeEntity
        {
            Id = entityId,
            Type = "Concept",
            Name = "To Delete"
        };

        _graphRepositoryMock
            .Setup(r => r.GetByIdAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var command = new DeleteEntityCommand
        {
            EntityId = entityId,
            CascadeMode = RelationshipCascadeMode.DeleteRelationships
        };

        // Act
        var result = await _sut.DeleteAsync(command);

        // Assert
        Assert.True(result.Success);

        _graphRepositoryMock.Verify(
            r => r.DeleteEntityAsync(entityId, It.IsAny<CancellationToken>()),
            Times.Once);
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<EntityDeletedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_FailIfHasRelationships_WithRelationships_Fails()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new KnowledgeEntity
        {
            Id = entityId,
            Type = "Concept",
            Name = "Has Relations"
        };

        _graphRepositoryMock
            .Setup(r => r.GetByIdAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _graphRepositoryMock
            .Setup(r => r.GetRelationshipsForEntityAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeRelationship>
            {
                new KnowledgeRelationship
                {
                    Type = "RELATED_TO",
                    FromEntityId = entityId,
                    ToEntityId = Guid.NewGuid()
                }
            });

        var command = new DeleteEntityCommand
        {
            EntityId = entityId,
            CascadeMode = RelationshipCascadeMode.FailIfHasRelationships,
            Force = false
        };

        // Act
        var result = await _sut.DeleteAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("relationships", result.Errors.First());

        _graphRepositoryMock.Verify(
            r => r.DeleteEntityAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_OrphanRelationships_DeletesOnlyRelationships()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var entity = new KnowledgeEntity
        {
            Id = entityId,
            Type = "Concept",
            Name = "Test"
        };

        _graphRepositoryMock
            .Setup(r => r.GetByIdAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var command = new DeleteEntityCommand
        {
            EntityId = entityId,
            CascadeMode = RelationshipCascadeMode.OrphanRelationships
        };

        // Act
        var result = await _sut.DeleteAsync(command);

        // Assert
        Assert.True(result.Success);

        _graphRepositoryMock.Verify(
            r => r.DeleteRelationshipsForEntityAsync(entityId, It.IsAny<CancellationToken>()),
            Times.Once);
        _graphRepositoryMock.Verify(
            r => r.DeleteEntityAsync(entityId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ValidateAsync Tests

    [Fact]
    public async Task ValidateAsync_UnknownType_ReturnsErrors()
    {
        // Arrange
        var entity = new KnowledgeEntity
        {
            Type = "UnknownType",
            Name = "Test"
        };

        // Act
        var result = await _sut.ValidateAsync(entity);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("UNKNOWN_TYPE", result.Errors[0].Code);
    }

    [Fact]
    public async Task ValidateAsync_MissingRequiredProperty_ReturnsErrors()
    {
        // Arrange
        var entity = new KnowledgeEntity
        {
            Type = "Endpoint",
            Name = "GET /api/users",
            Properties = new Dictionary<string, object>
            {
                // Missing "path" and "method" which are required
            }
        };

        // Act
        var result = await _sut.ValidateAsync(entity);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, e => Assert.Equal("MISSING_REQUIRED", e.Code));
    }

    [Fact]
    public async Task ValidateAsync_ValidEntity_ReturnsValid()
    {
        // Arrange
        var entity = new KnowledgeEntity
        {
            Type = "Concept",
            Name = "Valid Concept"
        };

        // Act
        var result = await _sut.ValidateAsync(entity);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region License Gating Tests

    [Fact]
    public async Task CreateAsync_Unlicensed_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        _licenseContextMock.Setup(l => l.GetCurrentTier()).Returns(LicenseTier.Core);

        var command = new CreateEntityCommand
        {
            Type = "Concept",
            Name = "Test",
            Properties = new Dictionary<string, object>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<FeatureNotLicensedException>(() => _sut.CreateAsync(command));
    }

    [Fact]
    public async Task UpdateAsync_WriterProTier_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        _licenseContextMock.Setup(l => l.GetCurrentTier()).Returns(LicenseTier.WriterPro);

        var command = new UpdateEntityCommand
        {
            EntityId = Guid.NewGuid(),
            Name = "New Name"
        };

        // Act & Assert
        await Assert.ThrowsAsync<FeatureNotLicensedException>(() => _sut.UpdateAsync(command));
    }

    [Fact]
    public async Task DeleteAsync_Unlicensed_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        _licenseContextMock.Setup(l => l.GetCurrentTier()).Returns(LicenseTier.Core);

        var command = new DeleteEntityCommand
        {
            EntityId = Guid.NewGuid()
        };

        // Act & Assert
        await Assert.ThrowsAsync<FeatureNotLicensedException>(() => _sut.DeleteAsync(command));
    }

    [Fact]
    public async Task MergeAsync_Unlicensed_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        _licenseContextMock.Setup(l => l.GetCurrentTier()).Returns(LicenseTier.Core);

        var command = new MergeEntitiesCommand
        {
            TargetEntityId = Guid.NewGuid(),
            SourceEntityIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act & Assert
        await Assert.ThrowsAsync<FeatureNotLicensedException>(() => _sut.MergeAsync(command));
    }

    #endregion

    #region GetHistoryAsync Tests

    [Fact]
    public async Task GetHistoryAsync_ReturnsHistoryFromRepository()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var expectedHistory = new List<EntityChangeRecord>
        {
            new EntityChangeRecord
            {
                EntityId = entityId,
                Operation = "Created",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _graphRepositoryMock
            .Setup(r => r.GetChangeHistoryAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _sut.GetHistoryAsync(entityId);

        // Assert
        Assert.Single(result);
        Assert.Equal("Created", result[0].Operation);
    }

    #endregion
}
