// =============================================================================
// File: DocToGraphSyncRecordsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for Doc-to-Graph Sync record types.
// =============================================================================
// v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph.Events;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.DocToGraph;

/// <summary>
/// Unit tests for Doc-to-Graph Sync record types.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6f")]
public class DocToGraphSyncRecordsTests
{
    #region DocToGraphSyncOptions Tests

    [Fact]
    public void DocToGraphSyncOptions_HasCorrectDefaults()
    {
        // Arrange & Act
        var options = new DocToGraphSyncOptions();

        // Assert
        Assert.True(options.ValidateBeforeUpsert);
        Assert.False(options.AutoCorrectErrors);
        Assert.True(options.PreserveLineage);
        Assert.Equal(1000, options.MaxEntities);
        Assert.True(options.CreateRelationships);
        Assert.True(options.ExtractClaims);
        Assert.True(options.EnrichWithGraphContext);
        Assert.Equal(TimeSpan.FromMinutes(10), options.Timeout);
    }

    [Fact]
    public void DocToGraphSyncOptions_CanSetAllProperties()
    {
        // Arrange & Act
        var options = new DocToGraphSyncOptions
        {
            ValidateBeforeUpsert = false,
            AutoCorrectErrors = true,
            PreserveLineage = false,
            MaxEntities = 100,
            CreateRelationships = false,
            ExtractClaims = false,
            EnrichWithGraphContext = false,
            Timeout = TimeSpan.FromMinutes(5)
        };

        // Assert
        Assert.False(options.ValidateBeforeUpsert);
        Assert.True(options.AutoCorrectErrors);
        Assert.False(options.PreserveLineage);
        Assert.Equal(100, options.MaxEntities);
        Assert.False(options.CreateRelationships);
        Assert.False(options.ExtractClaims);
        Assert.False(options.EnrichWithGraphContext);
        Assert.Equal(TimeSpan.FromMinutes(5), options.Timeout);
    }

    #endregion

    #region DocToGraphSyncResult Tests

    [Fact]
    public void DocToGraphSyncResult_CanInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var result = new DocToGraphSyncResult
        {
            Status = SyncOperationStatus.Success,
            UpsertedEntities = [],
            CreatedRelationships = [],
            ExtractedClaims = [],
            ValidationErrors = []
        };

        // Assert
        Assert.Equal(SyncOperationStatus.Success, result.Status);
        Assert.Empty(result.UpsertedEntities);
        Assert.Empty(result.CreatedRelationships);
        Assert.Empty(result.ExtractedClaims);
        Assert.Empty(result.ValidationErrors);
    }

    [Fact]
    public void DocToGraphSyncResult_CanSetOptionalProperties()
    {
        // Arrange
        var record = CreateTestExtractionRecord();

        // Act
        var result = new DocToGraphSyncResult
        {
            Status = SyncOperationStatus.Success,
            UpsertedEntities = [CreateTestEntity()],
            CreatedRelationships = [],
            ExtractedClaims = [],
            ValidationErrors = [],
            ExtractionRecord = record,
            Duration = TimeSpan.FromSeconds(5),
            TotalEntitiesAffected = 1,
            Message = "Sync completed"
        };

        // Assert
        Assert.Equal(SyncOperationStatus.Success, result.Status);
        Assert.Single(result.UpsertedEntities);
        Assert.Equal(record, result.ExtractionRecord);
        Assert.Equal(TimeSpan.FromSeconds(5), result.Duration);
        Assert.Equal(1, result.TotalEntitiesAffected);
        Assert.Equal("Sync completed", result.Message);
    }

    [Fact]
    public void DocToGraphSyncResult_PartialSuccess_HasErrorsAndEntities()
    {
        // Arrange & Act
        var result = new DocToGraphSyncResult
        {
            Status = SyncOperationStatus.PartialSuccess,
            UpsertedEntities = [CreateTestEntity()],
            CreatedRelationships = [],
            ExtractedClaims = [],
            ValidationErrors =
            [
                new ValidationError
                {
                    Code = "ERR-001",
                    Message = "Partial error",
                    Severity = ValidationSeverity.Warning
                }
            ]
        };

        // Assert
        Assert.Equal(SyncOperationStatus.PartialSuccess, result.Status);
        Assert.Single(result.UpsertedEntities);
        Assert.Single(result.ValidationErrors);
    }

    [Fact]
    public void DocToGraphSyncResult_Failed_HasErrorsOnly()
    {
        // Arrange & Act
        var result = new DocToGraphSyncResult
        {
            Status = SyncOperationStatus.Failed,
            UpsertedEntities = [],
            CreatedRelationships = [],
            ExtractedClaims = [],
            ValidationErrors =
            [
                new ValidationError
                {
                    Code = "ERR-001",
                    Message = "Critical error",
                    Severity = ValidationSeverity.Critical
                }
            ],
            Duration = TimeSpan.FromSeconds(2)
        };

        // Assert
        Assert.Equal(SyncOperationStatus.Failed, result.Status);
        Assert.Empty(result.UpsertedEntities);
        Assert.Single(result.ValidationErrors);
    }

    #endregion

    #region ExtractionRecord Tests

    [Fact]
    public void ExtractionRecord_CanInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var record = new ExtractionRecord
        {
            ExtractionId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            DocumentHash = "test-hash",
            ExtractedAt = DateTimeOffset.UtcNow,
            EntityIds = [],
            ClaimIds = [],
            RelationshipIds = [],
            ExtractionHash = "extraction-hash"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, record.ExtractionId);
        Assert.NotEqual(Guid.Empty, record.DocumentId);
        Assert.NotEmpty(record.DocumentHash);
        Assert.NotEmpty(record.ExtractionHash);
    }

    [Fact]
    public void ExtractionRecord_ExtractedByIsNullable()
    {
        // Arrange & Act
        var record = new ExtractionRecord
        {
            ExtractionId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            DocumentHash = "test-hash",
            ExtractedAt = DateTimeOffset.UtcNow,
            ExtractedBy = null,
            EntityIds = [],
            ClaimIds = [],
            RelationshipIds = [],
            ExtractionHash = "extraction-hash"
        };

        // Assert
        Assert.Null(record.ExtractedBy);
    }

    [Fact]
    public void ExtractionRecord_CanHaveMultipleIds()
    {
        // Arrange & Act
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();

        var record = new ExtractionRecord
        {
            ExtractionId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            DocumentHash = "test-hash",
            ExtractedAt = DateTimeOffset.UtcNow,
            EntityIds = [entityId1, entityId2],
            ClaimIds = [claimId],
            RelationshipIds = [relationshipId],
            ExtractionHash = "extraction-hash"
        };

        // Assert
        Assert.Equal(2, record.EntityIds.Count);
        Assert.Single(record.ClaimIds);
        Assert.Single(record.RelationshipIds);
    }

    #endregion

    #region ValidationResult Tests

    [Fact]
    public void ValidationResult_Success_CreatesValidResult()
    {
        // Arrange & Act
        var result = ValidationResult.Success(10, 5);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Equal(10, result.EntitiesValidated);
        Assert.Equal(5, result.RelationshipsValidated);
    }

    [Fact]
    public void ValidationResult_CanAddErrorsAndWarnings()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            IsValid = false,
            Errors =
            [
                new ValidationError { Code = "ERR-001", Message = "Error", Severity = ValidationSeverity.Error }
            ],
            Warnings =
            [
                new ValidationWarning { Code = "WARN-001", Message = "Warning" }
            ],
            EntitiesValidated = 5,
            RelationshipsValidated = 2
        };

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Single(result.Warnings);
    }

    #endregion

    #region ValidationError Tests

    [Fact]
    public void ValidationError_CanInitializeWithAllProperties()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var relationshipId = Guid.NewGuid();

        // Act
        var error = new ValidationError
        {
            Code = "VAL-001",
            Message = "Test error message",
            EntityId = entityId,
            RelationshipId = relationshipId,
            Severity = ValidationSeverity.Error
        };

        // Assert
        Assert.Equal("VAL-001", error.Code);
        Assert.Equal("Test error message", error.Message);
        Assert.Equal(entityId, error.EntityId);
        Assert.Equal(relationshipId, error.RelationshipId);
        Assert.Equal(ValidationSeverity.Error, error.Severity);
    }

    [Fact]
    public void ValidationError_EntityIdAndRelationshipIdAreNullable()
    {
        // Arrange & Act
        var error = new ValidationError
        {
            Code = "VAL-001",
            Message = "Test error",
            EntityId = null,
            RelationshipId = null,
            Severity = ValidationSeverity.Warning
        };

        // Assert
        Assert.Null(error.EntityId);
        Assert.Null(error.RelationshipId);
    }

    #endregion

    #region ValidationWarning Tests

    [Fact]
    public void ValidationWarning_CanInitializeWithAllProperties()
    {
        // Arrange
        var entityId = Guid.NewGuid();

        // Act
        var warning = new ValidationWarning
        {
            Code = "WARN-001",
            Message = "Test warning message",
            EntityId = entityId
        };

        // Assert
        Assert.Equal("WARN-001", warning.Code);
        Assert.Equal("Test warning message", warning.Message);
        Assert.Equal(entityId, warning.EntityId);
    }

    #endregion

    #region EntityValidationResult Tests

    [Fact]
    public void EntityValidationResult_Success_CreatesValidResult()
    {
        // Arrange
        var entityId = Guid.NewGuid();

        // Act
        var result = EntityValidationResult.Success(entityId);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(entityId, result.EntityId);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void EntityValidationResult_Failed_CreatesInvalidResult()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var errors = new List<ValidationError>
        {
            new() { Code = "ERR-001", Message = "Entity error", Severity = ValidationSeverity.Error }
        };

        // Act
        var result = EntityValidationResult.Failed(entityId, errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(entityId, result.EntityId);
        Assert.Single(result.Errors);
    }

    #endregion

    #region RelationshipValidationResult Tests

    [Fact]
    public void RelationshipValidationResult_Success_CreatesValidResult()
    {
        // Arrange & Act
        var result = RelationshipValidationResult.Success();

        // Assert
        Assert.True(result.AllValid);
        Assert.Empty(result.InvalidRelationships);
    }

    [Fact]
    public void RelationshipValidationResult_Failed_CreatesInvalidResult()
    {
        // Arrange
        var relationshipId = Guid.NewGuid();
        var invalidRelationships = new List<(Guid RelationshipId, ValidationError Error)>
        {
            (relationshipId, new ValidationError
            {
                Code = "ERR-001",
                Message = "Relationship error",
                RelationshipId = relationshipId,
                Severity = ValidationSeverity.Error
            })
        };

        // Act
        var result = RelationshipValidationResult.Failed(invalidRelationships);

        // Assert
        Assert.False(result.AllValid);
        Assert.Single(result.InvalidRelationships);
    }

    #endregion

    #region DocToGraphValidationContext Tests

    [Fact]
    public void DocToGraphValidationContext_HasCorrectDefaults()
    {
        // Arrange & Act
        var context = new DocToGraphValidationContext
        {
            DocumentId = Guid.NewGuid()
        };

        // Assert
        Assert.False(context.StrictMode);
        Assert.Null(context.AllowedEntityTypes);
    }

    [Fact]
    public void DocToGraphValidationContext_CanSetAllowedEntityTypes()
    {
        // Arrange & Act
        var allowedTypes = new HashSet<string> { "Product", "Endpoint" };
        var context = new DocToGraphValidationContext
        {
            DocumentId = Guid.NewGuid(),
            StrictMode = true,
            AllowedEntityTypes = allowedTypes
        };

        // Assert
        Assert.True(context.StrictMode);
        Assert.NotNull(context.AllowedEntityTypes);
        Assert.Equal(2, context.AllowedEntityTypes.Count);
    }

    #endregion

    #region GraphIngestionData Tests

    [Fact]
    public void GraphIngestionData_CanInitializeWithAllProperties()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var data = new GraphIngestionData
        {
            Entities = [CreateTestEntity()],
            Relationships = [],
            Claims = [],
            SourceDocumentId = documentId,
            Metadata = new Dictionary<string, object>
            {
                ["key"] = "value"
            }
        };

        // Assert
        Assert.Single(data.Entities);
        Assert.Empty(data.Relationships);
        Assert.Empty(data.Claims);
        Assert.Equal(documentId, data.SourceDocumentId);
        Assert.Contains("key", data.Metadata.Keys);
    }

    #endregion

    #region DocToGraphSyncCompletedEvent Tests

    [Fact]
    public void DocToGraphSyncCompletedEvent_Create_InitializesCorrectly()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var initiatedBy = Guid.NewGuid();
        var syncResult = new DocToGraphSyncResult
        {
            Status = SyncOperationStatus.Success,
            UpsertedEntities = [],
            CreatedRelationships = [],
            ExtractedClaims = [],
            ValidationErrors = [],
            Duration = TimeSpan.FromSeconds(1)
        };

        // Act
        var evt = DocToGraphSyncCompletedEvent.Create(documentId, syncResult, initiatedBy);

        // Assert
        Assert.Equal(documentId, evt.DocumentId);
        Assert.Equal(syncResult, evt.Result);
        Assert.Equal(initiatedBy, evt.InitiatedBy);
        Assert.True(evt.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void DocToGraphSyncCompletedEvent_InitiatedByIsNullable()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var syncResult = new DocToGraphSyncResult
        {
            Status = SyncOperationStatus.Success,
            UpsertedEntities = [],
            CreatedRelationships = [],
            ExtractedClaims = [],
            ValidationErrors = [],
            Duration = TimeSpan.FromSeconds(1)
        };

        // Act
        var evt = DocToGraphSyncCompletedEvent.Create(documentId, syncResult, initiatedBy: null);

        // Assert
        Assert.Null(evt.InitiatedBy);
    }

    #endregion

    #region ValidationSeverity Tests

    [Fact]
    public void ValidationSeverity_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)ValidationSeverity.Warning);
        Assert.Equal(1, (int)ValidationSeverity.Error);
        Assert.Equal(2, (int)ValidationSeverity.Critical);
    }

    #endregion

    #region Helper Methods

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

    private static ExtractionRecord CreateTestExtractionRecord()
    {
        return new ExtractionRecord
        {
            ExtractionId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            DocumentHash = "test-hash",
            ExtractedAt = DateTimeOffset.UtcNow,
            EntityIds = [Guid.NewGuid()],
            ClaimIds = [],
            RelationshipIds = [],
            ExtractionHash = "extraction-hash"
        };
    }

    #endregion
}
