# LCS-DES-076-KG-f: Doc-to-Graph Sync

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-076-KG-f |
| **System Breakdown** | LCS-SBD-076-KG |
| **Version** | v0.7.6 |
| **Codename** | Doc-to-Graph Sync (CKVS Phase 4c) |
| **Estimated Hours** | 8 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Doc-to-Graph Sync** module handles unidirectional synchronization from documents to the knowledge graph. It extracts entities, claims, and relationships from document content, manages the update pipeline, and maintains consistency between document state and graph state.

### 1.2 Key Responsibilities

- Extract structured data from unstructured document content
- Validate extracted entities against graph schema
- Detect and apply transformations to extracted data
- Manage upsert operations to the knowledge graph
- Track extraction lineage back to documents
- Support batch and incremental sync modes
- Manage version history of extractions

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Sync/
      DocToGraph/
        IDocumentToGraphSyncProvider.cs
        DocumentToGraphSyncProvider.cs
        ExtractionTransformer.cs
        ExtractionValidator.cs
```

---

## 2. Interface Definitions

### 2.1 Document-to-Graph Sync Provider

```csharp
namespace Lexichord.KnowledgeGraph.Sync.DocToGraph;

/// <summary>
/// Provider for document-to-graph synchronization.
/// </summary>
public interface IDocumentToGraphSyncProvider
{
    /// <summary>
    /// Synchronizes a document to the graph.
    /// </summary>
    Task<DocToGraphSyncResult> SyncAsync(
        Document document,
        DocToGraphSyncOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Validates extracted data against graph schema.
    /// </summary>
    Task<ValidationResult> ValidateExtractionAsync(
        ExtractionResult extraction,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the extraction lineage for a document.
    /// </summary>
    Task<IReadOnlyList<ExtractionRecord>> GetExtractionLineageAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Rolls back a previous sync operation.
    /// </summary>
    Task<bool> RollbackSyncAsync(
        Guid documentId,
        DateTimeOffset targetVersion,
        CancellationToken ct = default);
}
```

### 2.2 Extraction Transformer

```csharp
/// <summary>
/// Transforms extracted data for graph ingestion.
/// </summary>
public interface IExtractionTransformer
{
    /// <summary>
    /// Transforms extraction result for graph upsert.
    /// </summary>
    Task<GraphIngestionData> TransformAsync(
        ExtractionResult extraction,
        Document document,
        CancellationToken ct = default);

    /// <summary>
    /// Applies transformations to entities.
    /// </summary>
    Task<IReadOnlyList<KnowledgeEntity>> TransformEntitiesAsync(
        IReadOnlyList<ExtractedEntity> entities,
        CancellationToken ct = default);

    /// <summary>
    /// Applies transformations to relationships.
    /// </summary>
    Task<IReadOnlyList<KnowledgeRelationship>> TransformRelationshipsAsync(
        IReadOnlyList<ExtractedRelationship> relationships,
        CancellationToken ct = default);

    /// <summary>
    /// Enriches entities with graph context.
    /// </summary>
    Task<IReadOnlyList<KnowledgeEntity>> EnrichEntitiesAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default);
}
```

### 2.3 Extraction Validator

```csharp
/// <summary>
/// Validates extracted data against graph constraints.
/// </summary>
public interface IExtractionValidator
{
    /// <summary>
    /// Validates extraction result.
    /// </summary>
    Task<ValidationResult> ValidateAsync(
        ExtractionResult extraction,
        ValidationContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Validates a single entity.
    /// </summary>
    Task<EntityValidationResult> ValidateEntityAsync(
        KnowledgeEntity entity,
        CancellationToken ct = default);

    /// <summary>
    /// Validates relationships reference valid entities.
    /// </summary>
    Task<RelationshipValidationResult> ValidateRelationshipsAsync(
        IReadOnlyList<KnowledgeRelationship> relationships,
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default);
}
```

---

## 3. Data Types

### 3.1 Doc-to-Graph Sync Result

```csharp
/// <summary>
/// Result of document-to-graph synchronization.
/// </summary>
public record DocToGraphSyncResult
{
    /// <summary>Sync operation status.</summary>
    public required SyncOperationStatus Status { get; init; }

    /// <summary>Entities extracted and upserted.</summary>
    public IReadOnlyList<KnowledgeEntity> UpsertedEntities { get; init; } = [];

    /// <summary>Relationships created.</summary>
    public IReadOnlyList<KnowledgeRelationship> CreatedRelationships { get; init; } = [];

    /// <summary>Claims extracted.</summary>
    public IReadOnlyList<Claim> ExtractedClaims { get; init; } = [];

    /// <summary>Validation errors encountered.</summary>
    public IReadOnlyList<ValidationError> ValidationErrors { get; init; } = [];

    /// <summary>Extraction lineage record.</summary>
    public ExtractionRecord? ExtractionRecord { get; init; }

    /// <summary>Duration of sync operation.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Total entities affected (including updates).</summary>
    public int TotalEntitiesAffected { get; init; }

    /// <summary>Message summarizing the sync result.</summary>
    public string? Message { get; init; }
}
```

### 3.2 Extraction Record

```csharp
/// <summary>
/// Record of a document extraction for lineage tracking.
/// </summary>
public record ExtractionRecord
{
    /// <summary>Unique extraction ID.</summary>
    public required Guid ExtractionId { get; init; }

    /// <summary>Document that was extracted.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Document version extracted.</summary>
    public required long DocumentVersion { get; init; }

    /// <summary>Timestamp of extraction.</summary>
    public required DateTimeOffset ExtractedAt { get; init; }

    /// <summary>User who initiated extraction.</summary>
    public Guid? ExtractedBy { get; init; }

    /// <summary>Entities extracted in this operation.</summary>
    public IReadOnlyList<Guid> EntityIds { get; init; } = [];

    /// <summary>Claims extracted in this operation.</summary>
    public IReadOnlyList<Guid> ClaimIds { get; init; } = [];

    /// <summary>Relationships created in this operation.</summary>
    public IReadOnlyList<Guid> RelationshipIds { get; init; } = [];

    /// <summary>Hash of extraction for change detection.</summary>
    public string ExtractionHash { get; init; } = string.Empty;

    /// <summary>Validation errors encountered.</summary>
    public IReadOnlyList<string> ValidationErrors { get; init; } = [];
}
```

### 3.3 Graph Ingestion Data

```csharp
/// <summary>
/// Data structured for graph ingestion.
/// </summary>
public record GraphIngestionData
{
    /// <summary>Entities to upsert.</summary>
    public required IReadOnlyList<KnowledgeEntity> Entities { get; init; }

    /// <summary>Relationships to create or update.</summary>
    public IReadOnlyList<KnowledgeRelationship> Relationships { get; init; } = [];

    /// <summary>Claims to store.</summary>
    public IReadOnlyList<Claim> Claims { get; init; } = [];

    /// <summary>Metadata about the ingestion.</summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>Source document reference.</summary>
    public Guid SourceDocumentId { get; init; }

    /// <summary>Timestamp of ingestion preparation.</summary>
    public DateTimeOffset PreparedAt { get; init; } = DateTimeOffset.UtcNow;
}
```

### 3.4 Validation Types

```csharp
/// <summary>
/// Result of validation operation.
/// </summary>
public record ValidationResult
{
    /// <summary>Whether validation passed.</summary>
    public required bool IsValid { get; init; }

    /// <summary>Validation errors if invalid.</summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];

    /// <summary>Warnings (non-blocking issues).</summary>
    public IReadOnlyList<ValidationWarning> Warnings { get; init; } = [];

    /// <summary>Number of entities validated.</summary>
    public int EntitiesValidated { get; init; }

    /// <summary>Number of relationships validated.</summary>
    public int RelationshipsValidated { get; init; }
}

public record ValidationError
{
    /// <summary>Error code.</summary>
    public required string Code { get; init; }

    /// <summary>Human-readable message.</summary>
    public required string Message { get; init; }

    /// <summary>Entity ID if entity-specific.</summary>
    public Guid? EntityId { get; init; }

    /// <summary>Relationship ID if relationship-specific.</summary>
    public Guid? RelationshipId { get; init; }

    /// <summary>Severity of the error.</summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
}

public record ValidationWarning
{
    /// <summary>Warning code.</summary>
    public required string Code { get; init; }

    /// <summary>Warning message.</summary>
    public required string Message { get; init; }

    /// <summary>Entity ID if applicable.</summary>
    public Guid? EntityId { get; init; }
}

public enum ValidationSeverity
{
    Warning,
    Error,
    Critical
}

public record EntityValidationResult
{
    public required Guid EntityId { get; init; }
    public required bool IsValid { get; init; }
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];
}

public record RelationshipValidationResult
{
    public required bool AllValid { get; init; }
    public IReadOnlyList<(Guid RelationshipId, ValidationError Error)> InvalidRelationships { get; init; } = [];
}
```

### 3.5 Doc-to-Graph Sync Options

```csharp
/// <summary>
/// Options for document-to-graph sync.
/// </summary>
public record DocToGraphSyncOptions
{
    /// <summary>Whether to validate before upsert.</summary>
    public bool ValidateBeforeUpsert { get; init; } = true;

    /// <summary>Whether to auto-correct validation errors.</summary>
    public bool AutoCorrectErrors { get; init; } = false;

    /// <summary>Whether to preserve extraction lineage.</summary>
    public bool PreserveLineage { get; init; } = true;

    /// <summary>Maximum number of entities to extract.</summary>
    public int MaxEntities { get; init; } = 1000;

    /// <summary>Whether to create relationships.</summary>
    public bool CreateRelationships { get; init; } = true;

    /// <summary>Whether to extract and store claims.</summary>
    public bool ExtractClaims { get; init; } = true;

    /// <summary>Whether to enrich entities with graph context.</summary>
    public bool EnrichWithGraphContext { get; init; } = true;

    /// <summary>Timeout for sync operation.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(10);
}
```

---

## 4. Implementation

### 4.1 Document-to-Graph Sync Provider

```csharp
public class DocumentToGraphSyncProvider : IDocumentToGraphSyncProvider
{
    private readonly IEntityExtractionPipeline _extractionPipeline;
    private readonly IClaimExtractionService _claimExtractor;
    private readonly IExtractionTransformer _transformer;
    private readonly IExtractionValidator _validator;
    private readonly IGraphRepository _graphRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<DocumentToGraphSyncProvider> _logger;

    public DocumentToGraphSyncProvider(
        IEntityExtractionPipeline extractionPipeline,
        IClaimExtractionService claimExtractor,
        IExtractionTransformer transformer,
        IExtractionValidator validator,
        IGraphRepository graphRepository,
        IDocumentRepository documentRepository,
        ILogger<DocumentToGraphSyncProvider> logger)
    {
        _extractionPipeline = extractionPipeline;
        _claimExtractor = claimExtractor;
        _transformer = transformer;
        _validator = validator;
        _graphRepository = graphRepository;
        _documentRepository = documentRepository;
        _logger = logger;
    }

    public async Task<DocToGraphSyncResult> SyncAsync(
        Document document,
        DocToGraphSyncOptions options,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting document-to-graph sync for {DocumentId}",
            document.Id);

        try
        {
            // 1. Extract entities from document
            _logger.LogDebug("Extracting entities from document {DocumentId}", document.Id);

            var extractionResult = await _extractionPipeline.ExtractAsync(
                document.Content, ct);

            if (extractionResult.Entities.Count > options.MaxEntities)
            {
                extractionResult = extractionResult with
                {
                    Entities = extractionResult.Entities.Take(options.MaxEntities).ToList()
                };

                _logger.LogWarning(
                    "Extraction exceeded max entities for {DocumentId}: {Count}",
                    document.Id, extractionResult.Entities.Count);
            }

            // 2. Validate extraction
            var validationResult = ValidationResult.Success();

            if (options.ValidateBeforeUpsert)
            {
                _logger.LogDebug("Validating extraction for {DocumentId}", document.Id);

                validationResult = await _validator.ValidateAsync(
                    extractionResult,
                    new ValidationContext { DocumentId = document.Id },
                    ct);

                if (!validationResult.IsValid && !options.AutoCorrectErrors)
                {
                    _logger.LogWarning(
                        "Validation failed for {DocumentId}: {ErrorCount} errors",
                        document.Id, validationResult.Errors.Count);

                    stopwatch.Stop();

                    return new DocToGraphSyncResult
                    {
                        Status = SyncOperationStatus.PartialSuccess,
                        ValidationErrors = validationResult.Errors,
                        Duration = stopwatch.Elapsed
                    };
                }
            }

            // 3. Transform extraction for graph
            _logger.LogDebug("Transforming extraction for {DocumentId}", document.Id);

            var ingestionData = await _transformer.TransformAsync(
                extractionResult, document, ct);

            // 4. Enrich entities if requested
            if (options.EnrichWithGraphContext)
            {
                _logger.LogDebug("Enriching entities for {DocumentId}", document.Id);

                ingestionData = ingestionData with
                {
                    Entities = await _transformer.EnrichEntitiesAsync(
                        ingestionData.Entities, ct)
                };
            }

            // 5. Upsert entities to graph
            _logger.LogDebug(
                "Upserting {Count} entities for {DocumentId}",
                ingestionData.Entities.Count, document.Id);

            var upsertedEntities = await _graphRepository.UpsertEntitiesAsync(
                ingestionData.Entities, ct);

            // 6. Create relationships if requested
            var createdRelationships = new List<KnowledgeRelationship>();

            if (options.CreateRelationships && ingestionData.Relationships.Count > 0)
            {
                _logger.LogDebug(
                    "Creating {Count} relationships for {DocumentId}",
                    ingestionData.Relationships.Count, document.Id);

                createdRelationships = (await _graphRepository.CreateRelationshipsAsync(
                    ingestionData.Relationships, ct)).ToList();
            }

            // 7. Extract and store claims if requested
            var extractedClaims = new List<Claim>();

            if (options.ExtractClaims)
            {
                _logger.LogDebug("Extracting claims for {DocumentId}", document.Id);

                extractedClaims = (await _claimExtractor.ExtractAsync(
                    document, extractionResult, ct)).ToList();

                if (extractedClaims.Count > 0)
                {
                    await _graphRepository.UpsertClaimsAsync(extractedClaims, ct);
                }
            }

            // 8. Create extraction record for lineage if requested
            ExtractionRecord? extractionRecord = null;

            if (options.PreserveLineage)
            {
                _logger.LogDebug("Recording extraction lineage for {DocumentId}", document.Id);

                extractionRecord = new ExtractionRecord
                {
                    ExtractionId = Guid.NewGuid(),
                    DocumentId = document.Id,
                    DocumentVersion = document.Version,
                    ExtractedAt = DateTimeOffset.UtcNow,
                    ExtractedBy = document.LastModifiedBy,
                    EntityIds = upsertedEntities.Select(e => e.Id).ToList(),
                    ClaimIds = extractedClaims.Select(c => c.Id).ToList(),
                    RelationshipIds = createdRelationships.Select(r => r.Id).ToList(),
                    ExtractionHash = ComputeExtractionHash(extractionResult)
                };

                await _documentRepository.RecordExtractionAsync(extractionRecord, ct);
            }

            stopwatch.Stop();

            return new DocToGraphSyncResult
            {
                Status = SyncOperationStatus.Success,
                UpsertedEntities = upsertedEntities,
                CreatedRelationships = createdRelationships,
                ExtractedClaims = extractedClaims,
                ValidationErrors = validationResult.Errors,
                ExtractionRecord = extractionRecord,
                Duration = stopwatch.Elapsed,
                TotalEntitiesAffected = upsertedEntities.Count,
                Message = $"Synced {upsertedEntities.Count} entities, {extractedClaims.Count} claims, {createdRelationships.Count} relationships"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Document-to-graph sync failed for {DocumentId}",
                document.Id);

            stopwatch.Stop();

            return new DocToGraphSyncResult
            {
                Status = SyncOperationStatus.Failed,
                Duration = stopwatch.Elapsed,
                Message = $"Sync failed: {ex.Message}"
            };
        }
    }

    public async Task<ValidationResult> ValidateExtractionAsync(
        ExtractionResult extraction,
        CancellationToken ct = default)
    {
        return await _validator.ValidateAsync(
            extraction,
            new ValidationContext(),
            ct);
    }

    public async Task<IReadOnlyList<ExtractionRecord>> GetExtractionLineageAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        return await _documentRepository.GetExtractionLineageAsync(documentId, ct);
    }

    public async Task<bool> RollbackSyncAsync(
        Guid documentId,
        DateTimeOffset targetVersion,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Rolling back document-to-graph sync for {DocumentId} to {TargetVersion}",
            documentId, targetVersion);

        var lineage = await _documentRepository.GetExtractionLineageAsync(documentId, ct);
        var targetRecord = lineage.FirstOrDefault(r => r.ExtractedAt <= targetVersion);

        if (targetRecord == null)
        {
            return false;
        }

        // Remove entities not in the target version
        var allCurrent = await _graphRepository.GetEntitiesExtractedFromAsync(documentId, ct);
        var toRemove = allCurrent
            .Where(e => !targetRecord.EntityIds.Contains(e.Id))
            .ToList();

        if (toRemove.Count > 0)
        {
            await _graphRepository.DeleteEntitiesAsync(toRemove.Select(e => e.Id), ct);
        }

        return true;
    }

    private string ComputeExtractionHash(ExtractionResult extraction)
    {
        var combined = string.Join("|",
            extraction.Entities.Select(e => e.Name).OrderBy(n => n));

        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash);
    }
}
```

### 4.2 Extraction Transformer

```csharp
public class ExtractionTransformer : IExtractionTransformer
{
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<ExtractionTransformer> _logger;

    public ExtractionTransformer(
        IGraphRepository graphRepository,
        ILogger<ExtractionTransformer> logger)
    {
        _graphRepository = graphRepository;
        _logger = logger;
    }

    public async Task<GraphIngestionData> TransformAsync(
        ExtractionResult extraction,
        Document document,
        CancellationToken ct = default)
    {
        var transformedEntities = await TransformEntitiesAsync(
            extraction.Entities, ct);

        var transformedRelationships = await TransformRelationshipsAsync(
            extraction.Relationships, ct);

        return new GraphIngestionData
        {
            Entities = transformedEntities,
            Relationships = transformedRelationships,
            SourceDocumentId = document.Id,
            Metadata = new Dictionary<string, object>
            {
                ["documentName"] = document.Name,
                ["documentVersion"] = document.Version,
                ["extractedEntityCount"] = extraction.Entities.Count,
                ["extractedRelationshipCount"] = extraction.Relationships.Count
            }
        };
    }

    public async Task<IReadOnlyList<KnowledgeEntity>> TransformEntitiesAsync(
        IReadOnlyList<ExtractedEntity> entities,
        CancellationToken ct = default)
    {
        var transformed = new List<KnowledgeEntity>();

        foreach (var entity in entities)
        {
            var knowledgeEntity = new KnowledgeEntity
            {
                Id = Guid.NewGuid(),
                Name = entity.Name,
                Type = entity.Type,
                Description = entity.Description,
                Properties = new Dictionary<string, object>(entity.Properties ?? new()),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            transformed.Add(knowledgeEntity);
        }

        return transformed;
    }

    public async Task<IReadOnlyList<KnowledgeRelationship>> TransformRelationshipsAsync(
        IReadOnlyList<ExtractedRelationship> relationships,
        CancellationToken ct = default)
    {
        var transformed = new List<KnowledgeRelationship>();

        foreach (var rel in relationships)
        {
            var relationship = new KnowledgeRelationship
            {
                Id = Guid.NewGuid(),
                FromEntityId = rel.FromEntityId,
                ToEntityId = rel.ToEntityId,
                Type = rel.Type,
                Properties = new Dictionary<string, object>(rel.Properties ?? new()),
                CreatedAt = DateTimeOffset.UtcNow
            };

            transformed.Add(relationship);
        }

        return transformed;
    }

    public async Task<IReadOnlyList<KnowledgeEntity>> EnrichEntitiesAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct = default)
    {
        var enriched = new List<KnowledgeEntity>();

        foreach (var entity in entities)
        {
            // Try to find similar entities in graph
            var similar = await _graphRepository.FindSimilarEntitiesAsync(
                entity.Name, entity.Type, ct);

            var enrichedEntity = entity with
            {
                Properties = new Dictionary<string, object>(entity.Properties)
                {
                    ["similarEntityIds"] = similar.Select(s => s.Id.ToString()).ToList(),
                    ["enrichedAt"] = DateTimeOffset.UtcNow
                }
            };

            enriched.Add(enrichedEntity);
        }

        return enriched;
    }
}
```

---

## 5. Algorithm / Flow

### Document-to-Graph Sync Pipeline

```
1. Extract entities from document content using IEntityExtractionPipeline
2. Limit extraction to MaxEntities if exceeded
3. Validate extraction if ValidateBeforeUpsert option enabled
4. If validation fails and AutoCorrectErrors disabled:
   - Return PartialSuccess with errors
5. Transform entities to graph format
6. Transform relationships to graph format
7. If EnrichWithGraphContext enabled:
   - Find similar entities in graph
   - Add reference metadata
8. Upsert entities to graph repository
9. Create relationships if CreateRelationships enabled
10. If ExtractClaims enabled:
    - Extract claims from document and entities
    - Upsert claims to graph
11. If PreserveLineage enabled:
    - Compute extraction hash
    - Create ExtractionRecord
    - Store lineage record
12. Return DocToGraphSyncResult with all artifacts
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Extraction failure | Catch, log, return Failed |
| Validation errors | Return PartialSuccess if not auto-correcting |
| Graph upsert failure | Log, rollback, return Failed |
| Timeout exceeded | Cancel operation, return Failed |
| Relationship reference invalid | Log warning, skip invalid relationships |

---

## 7. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `Sync_SuccessfulExtraction` | Full sync completes |
| `Sync_WithValidation` | Validation enforced |
| `Sync_WithoutValidation` | Validation skipped |
| `ValidateExtraction_ReturnsErrors` | Errors detected |
| `GetExtractionLineage_ReturnsHistory` | Lineage tracked |
| `RollbackSync_RestoresState` | Rollback works |
| `EnrichEntities_AddsMetadata` | Enrichment works |

---

## 8. Performance

| Aspect | Target |
| :----- | :----- |
| Entity extraction | < 2s per 10KB |
| Validation | < 500ms per 100 entities |
| Entity transformation | < 200ms per 100 entities |
| Entity enrichment | < 1s per 100 entities |
| Graph upsert | < 1s per 100 entities |
| Total sync | < 10 minutes |

---

## 9. License Gating

| Tier | DocToGraph | Validation | Enrichment | Lineage |
| :--- | :--- | :--- | :--- | :--- |
| Core | No | N/A | N/A | N/A |
| WriterPro | Manual only | Basic | No | Yes |
| Teams | Full | Full | Yes | Yes |
| Enterprise | Full | Full | Yes + custom | Yes |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
