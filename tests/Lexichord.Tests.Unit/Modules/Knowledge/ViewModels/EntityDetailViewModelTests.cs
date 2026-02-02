// =============================================================================
// File: EntityDetailViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for EntityDetailViewModel.
// Version: v0.4.7f
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.Knowledge.UI.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.ViewModels;

/// <summary>
/// Unit tests for <see cref="EntityDetailViewModel"/>.
/// </summary>
/// <remarks>
/// Tests cover constructor validation, entity loading, license gating,
/// and navigation commands.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.7f")]
public class EntityDetailViewModelTests
{
    private readonly Mock<IGraphRepository> _graphRepositoryMock;
    private readonly Mock<ISchemaRegistry> _schemaRegistryMock;
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IEditorService> _editorServiceMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<ILogger<EntityDetailViewModel>> _loggerMock;
    private readonly RelationshipViewerPanelViewModel _relationshipViewerPanelMock;

    public EntityDetailViewModelTests()
    {
        _graphRepositoryMock = new Mock<IGraphRepository>();
        _schemaRegistryMock = new Mock<ISchemaRegistry>();
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _editorServiceMock = new Mock<IEditorService>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _loggerMock = new Mock<ILogger<EntityDetailViewModel>>();

        // Create a real RelationshipViewerPanelViewModel with the mock dependencies
        var relationshipPanelLoggerMock = new Mock<ILogger<RelationshipViewerPanelViewModel>>();
        _relationshipViewerPanelMock = new RelationshipViewerPanelViewModel(
            _graphRepositoryMock.Object,
            relationshipPanelLoggerMock.Object);

        // Default license tier (WriterPro cannot edit)
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullGraphRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new EntityDetailViewModel(
                null!,
                _schemaRegistryMock.Object,
                _documentRepositoryMock.Object,
                _editorServiceMock.Object,
                _licenseContextMock.Object,
                _relationshipViewerPanelMock,
                _loggerMock.Object));

        Assert.Equal("graphRepository", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullSchemaRegistry_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new EntityDetailViewModel(
                _graphRepositoryMock.Object,
                null!,
                _documentRepositoryMock.Object,
                _editorServiceMock.Object,
                _licenseContextMock.Object,
                _relationshipViewerPanelMock,
                _loggerMock.Object));

        Assert.Equal("schemaRegistry", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullDocumentRepository_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new EntityDetailViewModel(
                _graphRepositoryMock.Object,
                _schemaRegistryMock.Object,
                null!,
                _editorServiceMock.Object,
                _licenseContextMock.Object,
                _relationshipViewerPanelMock,
                _loggerMock.Object));

        Assert.Equal("documentRepository", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullEditorService_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new EntityDetailViewModel(
                _graphRepositoryMock.Object,
                _schemaRegistryMock.Object,
                _documentRepositoryMock.Object,
                null!,
                _licenseContextMock.Object,
                _relationshipViewerPanelMock,
                _loggerMock.Object));

        Assert.Equal("editorService", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLicenseContext_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new EntityDetailViewModel(
                _graphRepositoryMock.Object,
                _schemaRegistryMock.Object,
                _documentRepositoryMock.Object,
                _editorServiceMock.Object,
                null!,
                _relationshipViewerPanelMock,
                _loggerMock.Object));

        Assert.Equal("licenseContext", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullRelationshipViewerPanel_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new EntityDetailViewModel(
                _graphRepositoryMock.Object,
                _schemaRegistryMock.Object,
                _documentRepositoryMock.Object,
                _editorServiceMock.Object,
                _licenseContextMock.Object,
                null!,
                _loggerMock.Object));

        Assert.Equal("relationshipViewerPanel", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new EntityDetailViewModel(
                _graphRepositoryMock.Object,
                _schemaRegistryMock.Object,
                _documentRepositoryMock.Object,
                _editorServiceMock.Object,
                _licenseContextMock.Object,
                _relationshipViewerPanelMock,
                null!));

        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var sut = CreateViewModel();

        Assert.Null(sut.Entity);
        Assert.Equal("", sut.Name);
        Assert.Equal("", sut.Type);
        Assert.Equal("📦", sut.Icon);
        Assert.Equal(0f, sut.Confidence);
        Assert.False(sut.IsLoading);
        Assert.Empty(sut.Properties);
        Assert.Empty(sut.Relationships);
        Assert.Empty(sut.SourceDocuments);
    }

    #endregion

    #region License Gating Tests

    [Fact]
    public void CanEdit_FalseForWriterPro()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);

        var sut = CreateViewModel();

        Assert.False(sut.CanEdit);
    }

    [Fact]
    public void CanEdit_TrueForTeams()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Teams);

        var sut = CreateViewModel();

        Assert.True(sut.CanEdit);
    }

    [Fact]
    public void CanEdit_TrueForEnterprise()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Enterprise);

        var sut = CreateViewModel();

        Assert.True(sut.CanEdit);
    }

    #endregion

    #region Entity Loading Tests

    [Fact]
    public async Task OnEntityChanged_WithEntity_SetsBasicProperties()
    {
        // Arrange
        var entity = CreateTestEntity("Test Entity", "Concept", 0.85f);
        SetupMocksForEntity(entity);
        var sut = CreateViewModel();

        // Act
        sut.Entity = entity;
        await Task.Delay(100); // Wait for async load

        // Assert
        Assert.Equal("Test Entity", sut.Name);
        Assert.Equal("Concept", sut.Type);
        Assert.Equal(0.85f, sut.Confidence);
    }

    [Fact]
    public async Task OnEntityChanged_WithEntity_LoadsProperties()
    {
        // Arrange
        var entity = CreateTestEntity("Test Entity", "Endpoint");
        entity = entity with
        {
            Properties = new Dictionary<string, object>
            {
                ["path"] = "/api/users",
                ["method"] = "GET",
                ["confidence"] = 0.9
            }
        };
        SetupMocksForEntity(entity);
        var sut = CreateViewModel();

        // Act
        sut.Entity = entity;
        await Task.Delay(100);

        // Assert
        // Properties should exclude confidence (shown in header)
        Assert.Equal(2, sut.Properties.Count);
        Assert.Contains(sut.Properties, p => p.Name == "path" && p.Value == "/api/users");
        Assert.Contains(sut.Properties, p => p.Name == "method" && p.Value == "GET");
    }

    [Fact]
    public async Task OnEntityChanged_WithEntity_LoadsRelationships()
    {
        // Arrange
        var entity = CreateTestEntity("Test Entity", "Product");
        var relatedEntity = CreateTestEntity("Related Entity", "Endpoint");
        var relationship = new KnowledgeRelationship
        {
            Id = Guid.NewGuid(),
            Type = "CONTAINS",
            FromEntityId = entity.Id,
            ToEntityId = relatedEntity.Id
        };

        SetupMocksForEntity(entity);
        _graphRepositoryMock
            .Setup(x => x.GetRelationshipsForEntityAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeRelationship> { relationship });
        _graphRepositoryMock
            .Setup(x => x.GetByIdAsync(relatedEntity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(relatedEntity);

        var sut = CreateViewModel();

        // Act
        sut.Entity = entity;
        await Task.Delay(100);

        // Assert
        Assert.Single(sut.Relationships);
        Assert.Equal("CONTAINS", sut.Relationships[0].Type);
        Assert.Equal("→", sut.Relationships[0].Direction);
        Assert.Equal(relatedEntity.Id, sut.Relationships[0].OtherEntityId);
    }

    [Fact]
    public async Task OnEntityChanged_WithEntity_LoadsSourceDocuments()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var entity = CreateTestEntity("Test Entity", "Concept");
        entity = entity with { SourceDocuments = new List<Guid> { docId } };
        
        // Document is a positional record:
        // Document(Id, ProjectId, FilePath, Title, Hash, Status, IndexedAt, FailureReason)
        var document = new Document(
            Id: docId,
            ProjectId: Guid.NewGuid(),
            FilePath: "docs/test.md",
            Title: "Test Document",
            Hash: "abc123",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow,
            FailureReason: null);

        SetupMocksForEntity(entity);
        _documentRepositoryMock
            .Setup(x => x.GetByIdAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);
        _graphRepositoryMock
            .Setup(x => x.GetMentionCountAsync(entity.Id, docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateViewModel();

        // Act
        sut.Entity = entity;
        await Task.Delay(100);

        // Assert
        Assert.Single(sut.SourceDocuments);
        Assert.Equal("Test Document", sut.SourceDocuments[0].Title);
        Assert.Equal("docs/test.md", sut.SourceDocuments[0].Path);
    }

    [Fact]
    public async Task OnEntityChanged_WithNull_ClearsAllDetails()
    {
        // Arrange
        var entity = CreateTestEntity("Test Entity", "Concept");
        SetupMocksForEntity(entity);
        var sut = CreateViewModel();

        sut.Entity = entity;
        await Task.Delay(100);

        // Act
        sut.Entity = null;

        // Assert
        Assert.Equal("", sut.Name);
        Assert.Equal("", sut.Type);
        Assert.Equal("📦", sut.Icon);
        Assert.Equal(0f, sut.Confidence);
        Assert.Empty(sut.Properties);
        Assert.Empty(sut.Relationships);
        Assert.Empty(sut.SourceDocuments);
    }

    #endregion

    #region Command Tests

    [Fact]
    public async Task NavigateToSourceCommand_CallsEditorService()
    {
        // Arrange
        var sut = CreateViewModel();
        var source = new SourceDocumentItemViewModel
        {
            DocumentId = Guid.NewGuid(),
            Title = "Test Document",
            Path = "docs/test.md",
            MentionCount = 1
        };

        // Act
        await sut.NavigateToSourceCommand.ExecuteAsync(source);

        // Assert
        _editorServiceMock.Verify(
            x => x.OpenDocumentAsync("docs/test.md"),
            Times.Once);
    }

    [Fact]
    public async Task NavigateToRelatedEntityCommand_SetsEntityToRelated()
    {
        // Arrange
        var relatedEntity = CreateTestEntity("Related Entity", "Endpoint");
        _graphRepositoryMock
            .Setup(x => x.GetByIdAsync(relatedEntity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(relatedEntity);
        SetupMocksForEntity(relatedEntity);

        var sut = CreateViewModel();
        var relationship = new RelationshipItemViewModel
        {
            Id = Guid.NewGuid(),
            Type = "CONTAINS",
            OtherEntityId = relatedEntity.Id,
            OtherEntityName = "Related Entity",
            OtherEntityType = "Endpoint",
            Direction = "→"
        };

        // Act
        await sut.NavigateToRelatedEntityCommand.ExecuteAsync(relationship);
        await Task.Delay(100);

        // Assert
        Assert.Equal(relatedEntity.Id, sut.Entity?.Id);
        Assert.Equal("Related Entity", sut.Name);
    }

    #endregion

    #region Helper Methods

    private EntityDetailViewModel CreateViewModel()
    {
        return new EntityDetailViewModel(
            _graphRepositoryMock.Object,
            _schemaRegistryMock.Object,
            _documentRepositoryMock.Object,
            _editorServiceMock.Object,
            _licenseContextMock.Object,
            _relationshipViewerPanelMock,
            _loggerMock.Object);
    }

    private static KnowledgeEntity CreateTestEntity(
        string name,
        string type,
        float confidence = 0.9f)
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
            SourceDocuments = new List<Guid>(),
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };
    }

    private void SetupMocksForEntity(KnowledgeEntity entity)
    {
        _graphRepositoryMock
            .Setup(x => x.GetRelationshipsForEntityAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeRelationship>());

        _schemaRegistryMock
            .Setup(x => x.GetEntityType(It.IsAny<string>()))
            .Returns((string type) => new EntityTypeSchema
            {
                Name = type,
                Icon = "📦",
                Properties = Array.Empty<PropertySchema>()
            });

        _schemaRegistryMock
            .Setup(x => x.GetRelationshipType(It.IsAny<string>()))
            .Returns((string type) => new RelationshipTypeSchema
            {
                Name = type,
                FromEntityTypes = new[] { "Entity" },
                ToEntityTypes = new[] { "Entity" }
            });
    }

    #endregion
}
