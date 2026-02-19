// =============================================================================
// File: AffectedDocumentDetectorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for AffectedDocumentDetector.
// =============================================================================
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.Knowledge.Sync.GraphToDoc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Unit tests for <see cref="AffectedDocumentDetector"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6g")]
public class AffectedDocumentDetectorTests
{
    private readonly Mock<IDocumentRepository> _mockDocumentRepository;
    private readonly Mock<IGraphRepository> _mockGraphRepository;
    private readonly Mock<ILogger<AffectedDocumentDetector>> _mockLogger;
    private readonly AffectedDocumentDetector _sut;

    public AffectedDocumentDetectorTests()
    {
        _mockDocumentRepository = new Mock<IDocumentRepository>();
        _mockGraphRepository = new Mock<IGraphRepository>();
        _mockLogger = new Mock<ILogger<AffectedDocumentDetector>>();

        _sut = new AffectedDocumentDetector(
            _mockDocumentRepository.Object,
            _mockGraphRepository.Object,
            _mockLogger.Object);
    }

    #region DetectAsync Tests

    [Fact]
    public async Task DetectAsync_WithEntityNotFound_ReturnsEmpty()
    {
        // Arrange
        var change = CreateTestChange();
        _mockGraphRepository
            .Setup(x => x.GetByIdAsync(change.EntityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeEntity?)null);

        // Act
        var result = await _sut.DetectAsync(change);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DetectAsync_WithSourceDocuments_ReturnsAffectedDocuments()
    {
        // Arrange
        var docId1 = Guid.NewGuid();
        var docId2 = Guid.NewGuid();
        var entity = CreateTestEntity(docId1, docId2);
        var change = new GraphChange
        {
            EntityId = entity.Id,
            ChangeType = ChangeType.EntityUpdated,
            NewValue = "New",
            ChangedAt = DateTimeOffset.UtcNow
        };
        var doc1 = CreateTestDocument(docId1, "Doc1");
        var doc2 = CreateTestDocument(docId2, "Doc2");

        _mockGraphRepository
            .Setup(x => x.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mockDocumentRepository
            .Setup(x => x.GetByIdAsync(docId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc1);
        _mockDocumentRepository
            .Setup(x => x.GetByIdAsync(docId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc2);

        // Act
        var result = await _sut.DetectAsync(change);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.DocumentId == docId1);
        Assert.Contains(result, d => d.DocumentId == docId2);
        Assert.All(result, d => Assert.Equal(DocumentEntityRelationship.DerivedFrom, d.Relationship));
    }

    [Fact]
    public async Task DetectAsync_WithMissingDocument_SkipsIt()
    {
        // Arrange
        var docId1 = Guid.NewGuid();
        var docId2 = Guid.NewGuid();
        var entity = CreateTestEntity(docId1, docId2);
        var change = new GraphChange
        {
            EntityId = entity.Id,
            ChangeType = ChangeType.EntityUpdated,
            NewValue = "New",
            ChangedAt = DateTimeOffset.UtcNow
        };
        var doc1 = CreateTestDocument(docId1, "Doc1");

        _mockGraphRepository
            .Setup(x => x.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mockDocumentRepository
            .Setup(x => x.GetByIdAsync(docId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc1);
        _mockDocumentRepository
            .Setup(x => x.GetByIdAsync(docId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _sut.DetectAsync(change);

        // Assert
        Assert.Single(result);
        Assert.Equal(docId1, result[0].DocumentId);
    }

    #endregion

    #region DetectBatchAsync Tests

    [Fact]
    public async Task DetectBatchAsync_DeduplicatesDocuments()
    {
        // Arrange
        var sharedDocId = Guid.NewGuid();
        var entity1 = CreateTestEntity(sharedDocId);
        var entity2 = CreateTestEntity(sharedDocId);
        var changes = new List<GraphChange>
        {
            new() { EntityId = entity1.Id, ChangeType = ChangeType.EntityUpdated, NewValue = "V1", ChangedAt = DateTimeOffset.UtcNow },
            new() { EntityId = entity2.Id, ChangeType = ChangeType.EntityUpdated, NewValue = "V2", ChangedAt = DateTimeOffset.UtcNow }
        };
        var doc = CreateTestDocument(sharedDocId, "SharedDoc");

        _mockGraphRepository
            .Setup(x => x.GetByIdAsync(entity1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity1);
        _mockGraphRepository
            .Setup(x => x.GetByIdAsync(entity2.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity2);
        _mockDocumentRepository
            .Setup(x => x.GetByIdAsync(sharedDocId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        // Act
        var result = await _sut.DetectBatchAsync(changes);

        // Assert
        Assert.Single(result);
        Assert.Equal(sharedDocId, result[0].DocumentId);
        Assert.Equal(2, result[0].ReferenceCount); // Incremented for each change
    }

    #endregion

    #region GetRelationshipAsync Tests

    [Fact]
    public async Task GetRelationshipAsync_WithSourceDocument_ReturnsDerivedFrom()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var entity = CreateTestEntity(docId);

        _mockGraphRepository
            .Setup(x => x.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _sut.GetRelationshipAsync(docId, entity.Id);

        // Assert
        Assert.Equal(DocumentEntityRelationship.DerivedFrom, result);
    }

    [Fact]
    public async Task GetRelationshipAsync_WithNoRelationship_ReturnsNull()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var entity = CreateTestEntity(); // No source documents

        _mockGraphRepository
            .Setup(x => x.GetByIdAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _sut.GetRelationshipAsync(docId, entityId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Helper Methods

    private static GraphChange CreateTestChange()
    {
        return new GraphChange
        {
            EntityId = Guid.NewGuid(),
            ChangeType = ChangeType.EntityUpdated,
            NewValue = "NewValue",
            ChangedAt = DateTimeOffset.UtcNow
        };
    }

    private static KnowledgeEntity CreateTestEntity(params Guid[] sourceDocumentIds)
    {
        return new KnowledgeEntity
        {
            Id = Guid.NewGuid(),
            Name = "TestEntity",
            Type = "Concept",
            SourceDocuments = sourceDocumentIds.ToList()
        };
    }

    private static Document CreateTestDocument(Guid id, string title)
    {
        return new Document(
            Id: id,
            ProjectId: Guid.NewGuid(),
            FilePath: $"/path/to/{title}.md",
            Title: title,
            Hash: "abc123",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow,
            FailureReason: null);
    }

    #endregion
}
