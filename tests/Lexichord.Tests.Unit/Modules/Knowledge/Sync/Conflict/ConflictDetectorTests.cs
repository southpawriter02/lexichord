// =============================================================================
// File: ConflictDetectorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ConflictDetector service.
// =============================================================================
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.Knowledge.Sync.Conflict;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.Conflict;

/// <summary>
/// Unit tests for <see cref="ConflictDetector"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6h")]
public class ConflictDetectorTests
{
    private readonly Mock<IGraphRepository> _mockGraphRepository;
    private readonly Mock<IEntityComparer> _mockComparer;
    private readonly Mock<ILogger<ConflictDetector>> _mockLogger;
    private readonly ConflictDetector _sut;

    public ConflictDetectorTests()
    {
        _mockGraphRepository = new Mock<IGraphRepository>();
        _mockComparer = new Mock<IEntityComparer>();
        _mockLogger = new Mock<ILogger<ConflictDetector>>();

        _sut = new ConflictDetector(
            _mockGraphRepository.Object,
            _mockComparer.Object,
            _mockLogger.Object);
    }

    #region DetectAsync Tests

    [Fact]
    public async Task DetectAsync_NoAggregatedEntities_ReturnsEmptyList()
    {
        // Arrange
        var document = CreateTestDocument();
        var extraction = CreateTestExtraction(aggregatedEntities: null);

        // Act
        var result = await _sut.DetectAsync(document, extraction);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DetectAsync_EmptyAggregatedEntities_ReturnsEmptyList()
    {
        // Arrange
        var document = CreateTestDocument();
        var extraction = CreateTestExtraction(aggregatedEntities: []);

        // Act
        var result = await _sut.DetectAsync(document, extraction);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DetectAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var document = CreateTestDocument();
        var aggregatedEntity = CreateTestAggregatedEntity("Entity1");
        var extraction = CreateTestExtraction(aggregatedEntities: [aggregatedEntity]);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.DetectAsync(document, extraction, cts.Token));
    }

    #endregion

    #region DetectValueConflictsAsync Tests

    [Fact]
    public async Task DetectValueConflictsAsync_WithNoDifferences_ReturnsEmpty()
    {
        // Arrange
        var entity = CreateTestEntity("Entity1");
        var graphEntity = CreateTestEntity("Entity1");

        _mockGraphRepository
            .Setup(r => r.GetAllEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([graphEntity]);

        _mockComparer
            .Setup(c => c.CompareAsync(entity, graphEntity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EntityComparison
            {
                DocumentEntity = entity,
                GraphEntity = graphEntity,
                PropertyDifferences = []
            });

        // Act
        var result = await _sut.DetectValueConflictsAsync([entity]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DetectValueConflictsAsync_WithDifferences_ReturnsConflictDetails()
    {
        // Arrange
        var entity = CreateTestEntity("Entity1");
        var graphEntity = CreateTestEntity("Entity1");
        graphEntity = graphEntity with { Properties = new Dictionary<string, object> { ["Description"] = "GraphDesc" } };

        _mockGraphRepository
            .Setup(r => r.GetAllEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([graphEntity]);

        var comparison = new EntityComparison
        {
            DocumentEntity = entity,
            GraphEntity = graphEntity,
            PropertyDifferences = [
                new PropertyDifference
                {
                    PropertyName = "Description",
                    DocumentValue = "DocDesc",
                    GraphValue = "GraphDesc",
                    Confidence = 0.95f
                }
            ]
        };

        _mockComparer
            .Setup(c => c.CompareAsync(It.IsAny<KnowledgeEntity>(), It.IsAny<KnowledgeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(comparison);

        // Act
        var result = await _sut.DetectValueConflictsAsync([entity]);

        // Assert
        Assert.Single(result);
        Assert.Equal("Description", result[0].ConflictField);
    }

    [Fact]
    public async Task DetectValueConflictsAsync_EntityNotInGraph_SkipsComparison()
    {
        // Arrange
        var entity = CreateTestEntity("NewEntity");

        _mockGraphRepository
            .Setup(r => r.GetAllEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<KnowledgeEntity>());

        // Act
        var result = await _sut.DetectValueConflictsAsync([entity]);

        // Assert
        Assert.Empty(result);
        _mockComparer.Verify(
            c => c.CompareAsync(It.IsAny<KnowledgeEntity>(), It.IsAny<KnowledgeEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region DetectStructuralConflictsAsync Tests

    [Fact]
    public async Task DetectStructuralConflictsAsync_NoAggregatedEntities_ReturnsEmpty()
    {
        // Arrange
        var document = CreateTestDocument();
        var extraction = CreateTestExtraction(aggregatedEntities: null);

        _mockGraphRepository
            .Setup(r => r.GetAllEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<KnowledgeEntity>());

        // Act
        var result = await _sut.DetectStructuralConflictsAsync(document, extraction);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region EntitiesChangedAsync Tests

    [Fact]
    public async Task EntitiesChangedAsync_NoChanges_ReturnsFalse()
    {
        // Arrange
        var extractedAt = DateTimeOffset.UtcNow;
        var entityId = Guid.NewGuid();
        var extraction = new ExtractionRecord
        {
            ExtractionId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            DocumentHash = "abc123",
            ExtractedAt = extractedAt,
            EntityIds = [entityId]
        };

        var entity = CreateTestEntity("Entity");
        entity = entity with
        {
            Id = entityId,
            ModifiedAt = extractedAt.AddHours(-1) // Modified before extraction
        };

        _mockGraphRepository
            .Setup(r => r.GetByIdAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _sut.EntitiesChangedAsync(extraction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task EntitiesChangedAsync_EntityModifiedAfterExtraction_ReturnsTrue()
    {
        // Arrange
        var extractedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var entityId = Guid.NewGuid();
        var extraction = new ExtractionRecord
        {
            ExtractionId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            DocumentHash = "abc123",
            ExtractedAt = extractedAt,
            EntityIds = [entityId]
        };

        var entity = CreateTestEntity("Entity");
        entity = entity with
        {
            Id = entityId,
            ModifiedAt = DateTimeOffset.UtcNow // Modified after extraction
        };

        _mockGraphRepository
            .Setup(r => r.GetByIdAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _sut.EntitiesChangedAsync(extraction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EntitiesChangedAsync_EmptyEntityList_ReturnsFalse()
    {
        // Arrange
        var extraction = new ExtractionRecord
        {
            ExtractionId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            DocumentHash = "abc123",
            ExtractedAt = DateTimeOffset.UtcNow,
            EntityIds = []
        };

        // Act
        var result = await _sut.EntitiesChangedAsync(extraction);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Severity Assignment Tests

    [Fact]
    public async Task DetectValueConflictsAsync_HighConfidenceDifference_AssignsLowSeverity()
    {
        // Arrange
        var entity = CreateTestEntity("Entity");
        var graphEntity = CreateTestEntity("Entity");

        _mockGraphRepository
            .Setup(r => r.GetAllEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([graphEntity]);

        _mockComparer
            .Setup(c => c.CompareAsync(It.IsAny<KnowledgeEntity>(), It.IsAny<KnowledgeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EntityComparison
            {
                DocumentEntity = entity,
                GraphEntity = graphEntity,
                PropertyDifferences = [
                    new PropertyDifference
                    {
                        PropertyName = "Name",
                        DocumentValue = entity.Name,
                        GraphValue = "Different",
                        Confidence = 0.95f // High confidence
                    }
                ]
            });

        // Act
        var result = await _sut.DetectValueConflictsAsync([entity]);

        // Assert
        Assert.Single(result);
        // LOGIC: High confidence (>=0.8) indicates minor difference = Low severity
        Assert.Equal(ConflictSeverity.Low, result[0].Severity);
    }

    [Fact]
    public async Task DetectValueConflictsAsync_MediumConfidenceDifference_AssignsMediumSeverity()
    {
        // Arrange
        var entity = CreateTestEntity("Entity");
        var graphEntity = CreateTestEntity("Entity");

        _mockGraphRepository
            .Setup(r => r.GetAllEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([graphEntity]);

        _mockComparer
            .Setup(c => c.CompareAsync(It.IsAny<KnowledgeEntity>(), It.IsAny<KnowledgeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EntityComparison
            {
                DocumentEntity = entity,
                GraphEntity = graphEntity,
                PropertyDifferences = [
                    new PropertyDifference
                    {
                        PropertyName = "Name",
                        DocumentValue = entity.Name,
                        GraphValue = "Different",
                        Confidence = 0.6f // Medium confidence
                    }
                ]
            });

        // Act
        var result = await _sut.DetectValueConflictsAsync([entity]);

        // Assert
        Assert.Single(result);
        Assert.Equal(ConflictSeverity.Medium, result[0].Severity);
    }

    [Fact]
    public async Task DetectValueConflictsAsync_LowConfidenceDifference_AssignsHighSeverity()
    {
        // Arrange
        var entity = CreateTestEntity("Entity");
        var graphEntity = CreateTestEntity("Entity");

        _mockGraphRepository
            .Setup(r => r.GetAllEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([graphEntity]);

        _mockComparer
            .Setup(c => c.CompareAsync(It.IsAny<KnowledgeEntity>(), It.IsAny<KnowledgeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EntityComparison
            {
                DocumentEntity = entity,
                GraphEntity = graphEntity,
                PropertyDifferences = [
                    new PropertyDifference
                    {
                        PropertyName = "Name",
                        DocumentValue = entity.Name,
                        GraphValue = "Different",
                        Confidence = 0.3f // Low confidence
                    }
                ]
            });

        // Act
        var result = await _sut.DetectValueConflictsAsync([entity]);

        // Assert
        Assert.Single(result);
        Assert.Equal(ConflictSeverity.High, result[0].Severity);
    }

    #endregion

    #region Helper Methods

    private static Document CreateTestDocument()
    {
        return new Document(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            FilePath: "/test/document.md",
            Title: "Test Document",
            Hash: "abc123",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow,
            FailureReason: null);
    }

    private static KnowledgeEntity CreateTestEntity(string name)
    {
        return new KnowledgeEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = "Concept",
            Properties = new Dictionary<string, object>(),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            ModifiedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
    }

    private static AggregatedEntity CreateTestAggregatedEntity(string name)
    {
        return new AggregatedEntity
        {
            EntityType = "Concept",
            CanonicalValue = name,
            Mentions = [],
            MaxConfidence = 0.9f,
            MergedProperties = new Dictionary<string, object>()
        };
    }

    private static ExtractionResult CreateTestExtraction(IReadOnlyList<AggregatedEntity>? aggregatedEntities)
    {
        return new ExtractionResult
        {
            Mentions = [],
            AggregatedEntities = aggregatedEntities,
            Duration = TimeSpan.Zero,
            ChunksProcessed = 1
        };
    }

    #endregion
}
