// =============================================================================
// File: RelationshipViewerPanelViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the Relationship Viewer panel ViewModel.
// =============================================================================
// LOGIC: Tests loading, filtering, tree building, and state management for
//   the RelationshipViewerPanelViewModel.
//
// v0.4.7h: Relationship Viewer (Knowledge Graph Browser)
// Dependencies: xUnit, Moq, FluentAssertions
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Modules.Knowledge.UI.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.ViewModels;

/// <summary>
/// Unit tests for <see cref="RelationshipViewerPanelViewModel"/>.
/// </summary>
public class RelationshipViewerPanelViewModelTests
{
    private readonly Mock<IGraphRepository> _graphRepositoryMock;
    private readonly Mock<ILogger<RelationshipViewerPanelViewModel>> _loggerMock;
    private readonly RelationshipViewerPanelViewModel _sut;

    private readonly Guid _testEntityId = Guid.NewGuid();
    private readonly Guid _relatedEntity1Id = Guid.NewGuid();
    private readonly Guid _relatedEntity2Id = Guid.NewGuid();
    private readonly Guid _relatedEntity3Id = Guid.NewGuid();

    public RelationshipViewerPanelViewModelTests()
    {
        _graphRepositoryMock = new Mock<IGraphRepository>();
        _loggerMock = new Mock<ILogger<RelationshipViewerPanelViewModel>>();
        _sut = new RelationshipViewerPanelViewModel(
            _graphRepositoryMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RelationshipViewerPanelViewModel(
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("graphRepository");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RelationshipViewerPanelViewModel(
            _graphRepositoryMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_InitializesWithDefaultState()
    {
        // Assert
        _sut.DirectionFilter.Should().Be(RelationshipDirection.Both);
        _sut.TypeFilter.Should().BeNull();
        _sut.IsLoading.Should().BeFalse();
        _sut.RootNodes.Should().BeEmpty();
        _sut.AvailableTypes.Should().BeEmpty();
    }

    #endregion

    #region LoadRelationshipsAsync Tests

    [Fact]
    public async Task LoadRelationshipsAsync_SetsIsLoadingDuringOperation()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_sut.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        _graphRepositoryMock
            .Setup(x => x.GetRelationshipsForEntityAsync(_testEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<KnowledgeRelationship>());

        // Act
        await _sut.LoadRelationshipsAsync(_testEntityId);

        // Assert
        loadingStates.Should().Contain(true, "IsLoading should be set to true during loading");
        _sut.IsLoading.Should().BeFalse("IsLoading should be false after loading completes");
    }

    [Fact]
    public async Task LoadRelationshipsAsync_WithNoRelationships_ClearsRootNodes()
    {
        // Arrange
        _graphRepositoryMock
            .Setup(x => x.GetRelationshipsForEntityAsync(_testEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<KnowledgeRelationship>());

        // Act
        await _sut.LoadRelationshipsAsync(_testEntityId);

        // Assert
        _sut.RootNodes.Should().BeEmpty();
        _sut.AvailableTypes.Should().BeEmpty();
        _sut.FilteredCount.Should().Be(0);
        _sut.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task LoadRelationshipsAsync_WithRelationships_BuildsTreeStructure()
    {
        // Arrange
        var relationships = CreateTestRelationships();
        SetupMockRelationships(relationships);
        SetupMockEntities();

        // Act
        await _sut.LoadRelationshipsAsync(_testEntityId);

        // Assert
        _sut.RootNodes.Should().NotBeEmpty();
        _sut.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task LoadRelationshipsAsync_PopulatesAvailableTypes()
    {
        // Arrange
        var relationships = CreateTestRelationships();
        SetupMockRelationships(relationships);
        SetupMockEntities();

        // Act
        await _sut.LoadRelationshipsAsync(_testEntityId);

        // Assert
        _sut.AvailableTypes.Should().Contain("RELATES_TO");
        _sut.AvailableTypes.Should().Contain("DEPENDS_ON");
    }

    #endregion

    #region Direction Filter Tests

    [Fact]
    public async Task DirectionFilter_WhenSetToIncoming_FiltersToIncomingOnly()
    {
        // Arrange
        var relationships = CreateTestRelationships();
        SetupMockRelationships(relationships);
        SetupMockEntities();
        await _sut.LoadRelationshipsAsync(_testEntityId);

        // Act
        _sut.DirectionFilter = RelationshipDirection.Incoming;

        // Assert
        // With the test data, we have one incoming relationship
        _sut.FilteredCount.Should().BeLessThan(_sut.TotalCount);
    }

    [Fact]
    public async Task DirectionFilter_WhenSetToOutgoing_FiltersToOutgoingOnly()
    {
        // Arrange
        var relationships = CreateTestRelationships();
        SetupMockRelationships(relationships);
        SetupMockEntities();
        await _sut.LoadRelationshipsAsync(_testEntityId);

        // Act
        _sut.DirectionFilter = RelationshipDirection.Outgoing;

        // Assert
        _sut.FilteredCount.Should().BeLessThanOrEqualTo(_sut.TotalCount);
    }

    [Fact]
    public async Task DirectionFilter_WhenSetToBoth_ShowsAllRelationships()
    {
        // Arrange
        var relationships = CreateTestRelationships();
        SetupMockRelationships(relationships);
        SetupMockEntities();
        await _sut.LoadRelationshipsAsync(_testEntityId);

        // Act
        _sut.DirectionFilter = RelationshipDirection.Both;

        // Assert
        _sut.FilteredCount.Should().Be(_sut.TotalCount);
    }

    #endregion

    #region Type Filter Tests

    [Fact]
    public async Task TypeFilter_WhenSet_FiltersToMatchingType()
    {
        // Arrange
        var relationships = CreateTestRelationships();
        SetupMockRelationships(relationships);
        SetupMockEntities();
        await _sut.LoadRelationshipsAsync(_testEntityId);

        // Act
        _sut.TypeFilter = "RELATES_TO";

        // Assert
        _sut.FilteredCount.Should().BeLessThan(_sut.TotalCount);
    }

    [Fact]
    public async Task TypeFilter_WhenCleared_ShowsAllRelationships()
    {
        // Arrange
        var relationships = CreateTestRelationships();
        SetupMockRelationships(relationships);
        SetupMockEntities();
        await _sut.LoadRelationshipsAsync(_testEntityId);
        _sut.TypeFilter = "RELATES_TO";

        // Act
        _sut.TypeFilter = null;

        // Assert
        _sut.FilteredCount.Should().Be(_sut.TotalCount);
    }

    #endregion

    #region ClearFilters Command Tests

    [Fact]
    public async Task ClearFilters_ResetsAllFiltersToDefault()
    {
        // Arrange
        var relationships = CreateTestRelationships();
        SetupMockRelationships(relationships);
        SetupMockEntities();
        await _sut.LoadRelationshipsAsync(_testEntityId);
        _sut.DirectionFilter = RelationshipDirection.Incoming;
        _sut.TypeFilter = "RELATES_TO";

        // Act
        _sut.ClearFiltersCommand.Execute(null);

        // Assert
        _sut.DirectionFilter.Should().Be(RelationshipDirection.Both);
        _sut.TypeFilter.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private IReadOnlyList<KnowledgeRelationship> CreateTestRelationships()
    {
        return new List<KnowledgeRelationship>
        {
            // Outgoing: testEntity -> relatedEntity1
            new()
            {
                Id = Guid.NewGuid(),
                Type = "RELATES_TO",
                FromEntityId = _testEntityId,
                ToEntityId = _relatedEntity1Id
            },
            // Outgoing: testEntity -> relatedEntity2
            new()
            {
                Id = Guid.NewGuid(),
                Type = "DEPENDS_ON",
                FromEntityId = _testEntityId,
                ToEntityId = _relatedEntity2Id
            },
            // Incoming: relatedEntity3 -> testEntity
            new()
            {
                Id = Guid.NewGuid(),
                Type = "RELATES_TO",
                FromEntityId = _relatedEntity3Id,
                ToEntityId = _testEntityId
            }
        };
    }

    private void SetupMockRelationships(IReadOnlyList<KnowledgeRelationship> relationships)
    {
        _graphRepositoryMock
            .Setup(x => x.GetRelationshipsForEntityAsync(_testEntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(relationships);
    }

    private void SetupMockEntities()
    {
        var entities = new Dictionary<Guid, KnowledgeEntity>
        {
            [_testEntityId] = new() { Type = "Concept", Name = "Test Entity" },
            [_relatedEntity1Id] = new() { Type = "Concept", Name = "Related Entity 1" },
            [_relatedEntity2Id] = new() { Type = "Endpoint", Name = "Related Entity 2" },
            [_relatedEntity3Id] = new() { Type = "Concept", Name = "Related Entity 3" }
        };

        foreach (var (id, entity) in entities)
        {
            _graphRepositoryMock
                .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);
        }
    }

    #endregion
}
