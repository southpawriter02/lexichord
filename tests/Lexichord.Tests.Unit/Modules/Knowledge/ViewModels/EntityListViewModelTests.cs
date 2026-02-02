// =============================================================================
// File: EntityListViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for EntityListViewModel.
// Version: v0.4.7e
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Knowledge.UI.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.ViewModels;

/// <summary>
/// Unit tests for <see cref="EntityListViewModel"/>.
/// </summary>
/// <remarks>
/// Tests cover loading, filtering, and property change notifications.
/// </remarks>
public class EntityListViewModelTests
{
    private readonly Mock<IGraphRepository> _graphRepositoryMock;
    private readonly Mock<ISchemaRegistry> _schemaRegistryMock;
    private readonly Mock<ILogger<EntityListViewModel>> _loggerMock;
    private readonly EntityListViewModel _sut;

    public EntityListViewModelTests()
    {
        _graphRepositoryMock = new Mock<IGraphRepository>();
        _schemaRegistryMock = new Mock<ISchemaRegistry>();
        _loggerMock = new Mock<ILogger<EntityListViewModel>>();

        _sut = new EntityListViewModel(
            _graphRepositoryMock.Object,
            _schemaRegistryMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullGraphRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new EntityListViewModel(
                null!,
                _schemaRegistryMock.Object,
                _loggerMock.Object));

        Assert.Equal("graphRepository", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullSchemaRegistry_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new EntityListViewModel(
                _graphRepositoryMock.Object,
                null!,
                _loggerMock.Object));

        Assert.Equal("schemaRegistry", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new EntityListViewModel(
                _graphRepositoryMock.Object,
                _schemaRegistryMock.Object,
                null!));

        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        Assert.False(_sut.IsLoading);
        Assert.Null(_sut.SearchText);
        Assert.Null(_sut.TypeFilter);
        Assert.Equal(0.0f, _sut.MinConfidenceFilter);
        Assert.Null(_sut.DocumentFilter);
        Assert.Empty(_sut.Entities);
        Assert.Empty(_sut.FilteredEntities);
        Assert.Empty(_sut.AvailableTypes);
    }

    #endregion

    #region LoadEntitiesAsync Tests

    [Fact]
    public async Task LoadEntitiesAsync_SetsIsLoadingDuringLoad()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_sut.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        _graphRepositoryMock
            .Setup(x => x.GetAllEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeEntity>());

        // Act
        await _sut.LoadEntitiesAsync();

        // Assert
        Assert.Contains(true, loadingStates);
        Assert.False(_sut.IsLoading);
    }

    [Fact]
    public async Task LoadEntitiesAsync_PopulatesEntitiesCollection()
    {
        // Arrange
        var entities = new List<KnowledgeEntity>
        {
            CreateTestEntity("Entity 1", "Concept"),
            CreateTestEntity("Entity 2", "Endpoint")
        };

        _graphRepositoryMock
            .Setup(x => x.GetAllEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);
        _graphRepositoryMock
            .Setup(x => x.GetRelationshipCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _graphRepositoryMock
            .Setup(x => x.GetMentionCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _schemaRegistryMock
            .Setup(x => x.GetEntityType(It.IsAny<string>()))
            .Returns((string type) => new EntityTypeSchema
            {
                Name = type,
                Properties = Array.Empty<PropertySchema>()
            });

        // Act
        await _sut.LoadEntitiesAsync();

        // Assert
        Assert.Equal(2, _sut.Entities.Count);
        Assert.Equal(2, _sut.TotalCount);
    }

    [Fact]
    public async Task LoadEntitiesAsync_PopulatesAvailableTypes()
    {
        // Arrange
        var entities = new List<KnowledgeEntity>
        {
            CreateTestEntity("Entity 1", "Concept"),
            CreateTestEntity("Entity 2", "Endpoint"),
            CreateTestEntity("Entity 3", "Concept")
        };

        _graphRepositoryMock
            .Setup(x => x.GetAllEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);
        _graphRepositoryMock
            .Setup(x => x.GetRelationshipCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _graphRepositoryMock
            .Setup(x => x.GetMentionCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _schemaRegistryMock
            .Setup(x => x.GetEntityType(It.IsAny<string>()))
            .Returns((string type) => new EntityTypeSchema
            {
                Name = type,
                Properties = Array.Empty<PropertySchema>()
            });

        // Act
        await _sut.LoadEntitiesAsync();

        // Assert
        Assert.Equal(2, _sut.AvailableTypes.Count); // Concept, Endpoint (distinct)
        Assert.Contains("Concept", _sut.AvailableTypes);
        Assert.Contains("Endpoint", _sut.AvailableTypes);
    }

    #endregion

    #region Filtering Tests

    [Fact]
    public async Task TypeFilter_FiltersEntitiesCorrectly()
    {
        // Arrange
        await SetupEntitiesAsync();

        // Act
        _sut.TypeFilter = "Endpoint";

        // Assert
        Assert.All(_sut.FilteredEntities, e => Assert.Equal("Endpoint", e.Type));
    }

    [Fact]
    public async Task SearchText_FiltersEntitiesByName()
    {
        // Arrange
        await SetupEntitiesAsync();

        // Act
        _sut.SearchText = "Entity 1";

        // Assert
        Assert.Single(_sut.FilteredEntities);
        Assert.Equal("Entity 1", _sut.FilteredEntities[0].Name);
    }

    [Fact]
    public async Task MinConfidenceFilter_FiltersEntitiesByConfidence()
    {
        // Arrange
        await SetupEntitiesAsync();

        // Act
        _sut.MinConfidenceFilter = 0.95f;

        // Assert
        Assert.All(_sut.FilteredEntities, e => Assert.True(e.Confidence >= 0.95f));
    }

    [Fact]
    public void ClearFilters_ResetsAllFiltersToDefaults()
    {
        // Arrange
        _sut.SearchText = "test";
        _sut.TypeFilter = "Concept";
        _sut.MinConfidenceFilter = 0.8f;
        _sut.DocumentFilter = Guid.NewGuid();

        // Act
        _sut.ClearFilters();

        // Assert
        Assert.Null(_sut.SearchText);
        Assert.Null(_sut.TypeFilter);
        Assert.Equal(0.0f, _sut.MinConfidenceFilter);
        Assert.Null(_sut.DocumentFilter);
    }

    #endregion

    #region Helper Methods

    private static KnowledgeEntity CreateTestEntity(string name, string type, float confidence = 0.9f)
    {
        return new KnowledgeEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Properties = new Dictionary<string, object>
            {
                ["confidence"] = confidence
            },
            SourceDocuments = new List<Guid> { Guid.NewGuid() },
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task SetupEntitiesAsync()
    {
        var entities = new List<KnowledgeEntity>
        {
            CreateTestEntity("Entity 1", "Concept", 0.85f),
            CreateTestEntity("Entity 2", "Endpoint", 0.95f),
            CreateTestEntity("Entity 3", "Concept", 0.99f)
        };

        _graphRepositoryMock
            .Setup(x => x.GetAllEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);
        _graphRepositoryMock
            .Setup(x => x.GetRelationshipCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _graphRepositoryMock
            .Setup(x => x.GetMentionCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _schemaRegistryMock
            .Setup(x => x.GetEntityType(It.IsAny<string>()))
            .Returns((string type) => new EntityTypeSchema
            {
                Name = type,
                Properties = Array.Empty<PropertySchema>()
            });

        await _sut.LoadEntitiesAsync();
    }

    #endregion
}
