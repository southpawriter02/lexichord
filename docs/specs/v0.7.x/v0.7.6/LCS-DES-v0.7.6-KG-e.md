# LCS-DES-076-KG-e: Sync Service Core

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-076-KG-e |
| **System Breakdown** | LCS-SBD-076-KG |
| **Version** | v0.7.6 |
| **Codename** | Sync Service Core (CKVS Phase 4c) |
| **Estimated Hours** | 6 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Sync Service Core** implements `ISyncService` to orchestrate bidirectional synchronization between documents and the knowledge graph. It coordinates the synchronization pipeline, manages sync operations, and exposes interfaces for conflict resolution and sync status tracking.

### 1.2 Key Responsibilities

- Orchestrate document-to-graph and graph-to-document synchronization
- Manage sync workflow and operation sequencing
- Coordinate with extraction pipelines and graph repositories
- Expose high-level sync operations (SyncDocumentToGraphAsync, GetAffectedDocumentsAsync)
- Track affected entities, claims, and relationships
- Manage sync status and conflict detection
- Support conflict resolution workflows

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Sync/
      Core/
        ISyncService.cs
        SyncService.cs
        SyncOrchestrator.cs
```

---

## 2. Interface Definitions

### 2.1 Sync Service Interface

```csharp
namespace Lexichord.KnowledgeGraph.Sync.Core;

/// <summary>
/// Service for orchestrating bidirectional synchronization between documents and knowledge graph.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Synchronizes a document to the knowledge graph.
    /// </summary>
    Task<SyncResult> SyncDocumentToGraphAsync(
        Document document,
        SyncContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Gets documents affected by a graph change.
    /// </summary>
    Task<IReadOnlyList<Document>> GetAffectedDocumentsAsync(
        GraphChange change,
        CancellationToken ct = default);

    /// <summary>
    /// Gets current sync status for a document.
    /// </summary>
    Task<SyncStatus> GetSyncStatusAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves a sync conflict for a document.
    /// </summary>
    Task<SyncResult> ResolveConflictAsync(
        Guid documentId,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a document needs synchronization.
    /// </summary>
    Task<bool> NeedsSyncAsync(
        Guid documentId,
        CancellationToken ct = default);
}
```

### 2.2 Sync Orchestrator Interface

```csharp
/// <summary>
/// Internal orchestrator for sync workflow management.
/// </summary>
public interface ISyncOrchestrator
{
    /// <summary>
    /// Executes the document-to-graph sync pipeline.
    /// </summary>
    Task<SyncResult> ExecuteDocumentToGraphAsync(
        Document document,
        SyncContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Executes the graph-to-document sync pipeline.
    /// </summary>
    Task<IReadOnlyList<SyncResult>> ExecuteGraphToDocumentAsync(
        GraphChange change,
        CancellationToken ct = default);

    /// <summary>
    /// Detects conflicts in sync operation.
    /// </summary>
    Task<IReadOnlyList<SyncConflict>> DetectConflictsAsync(
        Document document,
        ExtractionResult extraction,
        CancellationToken ct = default);
}
```

---

## 3. Data Types

### 3.1 Sync Result

```csharp
/// <summary>
/// Result of a synchronization operation.
/// </summary>
public record SyncResult
{
    /// <summary>Status of the sync operation.</summary>
    public required SyncOperationStatus Status { get; init; }

    /// <summary>Entities affected by the sync.</summary>
    public IReadOnlyList<KnowledgeEntity> EntitiesAffected { get; init; } = [];

    /// <summary>Claims affected by the sync.</summary>
    public IReadOnlyList<Claim> ClaimsAffected { get; init; } = [];

    /// <summary>Relationships affected by the sync.</summary>
    public IReadOnlyList<KnowledgeRelationship> RelationshipsAffected { get; init; } = [];

    /// <summary>Conflicts detected during sync.</summary>
    public IReadOnlyList<SyncConflict> Conflicts { get; init; } = [];

    /// <summary>Duration of the sync operation.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Error message if operation failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Timestamp of sync completion.</summary>
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}

public enum SyncOperationStatus
{
    /// <summary>Sync completed successfully.</summary>
    Success,
    /// <summary>Sync completed with conflicts.</summary>
    SuccessWithConflicts,
    /// <summary>Sync partially completed.</summary>
    PartialSuccess,
    /// <summary>Sync failed.</summary>
    Failed,
    /// <summary>No changes detected.</summary>
    NoChanges
}
```

### 3.2 Sync Status

```csharp
/// <summary>
/// Current synchronization status for a document.
/// </summary>
public record SyncStatus
{
    /// <summary>Document ID.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Current sync state.</summary>
    public required SyncState State { get; init; }

    /// <summary>Timestamp of last successful sync.</summary>
    public DateTimeOffset? LastSyncAt { get; init; }

    /// <summary>Pending changes awaiting sync.</summary>
    public int PendingChanges { get; init; }

    /// <summary>Timestamp of last sync attempt.</summary>
    public DateTimeOffset? LastAttemptAt { get; init; }

    /// <summary>Error from last failed sync attempt.</summary>
    public string? LastError { get; init; }

    /// <summary>Number of unresolved conflicts.</summary>
    public int UnresolvedConflicts { get; init; }

    /// <summary>Whether sync is currently in progress.</summary>
    public bool IsSyncInProgress { get; init; }
}

public enum SyncState
{
    /// <summary>Document and graph are synchronized.</summary>
    InSync,
    /// <summary>Document has changes pending sync.</summary>
    PendingSync,
    /// <summary>Document needs manual review before sync.</summary>
    NeedsReview,
    /// <summary>Conflict exists and needs resolution.</summary>
    Conflict,
    /// <summary>Document has never been synchronized.</summary>
    NeverSynced
}
```

### 3.3 Sync Conflict

```csharp
/// <summary>
/// A conflict detected during synchronization.
/// </summary>
public record SyncConflict
{
    /// <summary>The entity or field in conflict.</summary>
    public required string ConflictTarget { get; init; }

    /// <summary>Value from the document.</summary>
    public required object DocumentValue { get; init; }

    /// <summary>Value from the graph.</summary>
    public required object GraphValue { get; init; }

    /// <summary>Timestamp when conflict was detected.</summary>
    public required DateTimeOffset DetectedAt { get; init; }

    /// <summary>Type of conflict.</summary>
    public required ConflictType Type { get; init; }

    /// <summary>Severity of the conflict.</summary>
    public ConflictSeverity Severity { get; init; } = ConflictSeverity.Medium;

    /// <summary>Description of the conflict.</summary>
    public string? Description { get; init; }
}

public enum ConflictType
{
    /// <summary>Value differs between sources.</summary>
    ValueMismatch,
    /// <summary>Entity exists in document but not graph.</summary>
    MissingInGraph,
    /// <summary>Entity exists in graph but not document.</summary>
    MissingInDocument,
    /// <summary>Relationship differs between sources.</summary>
    RelationshipMismatch,
    /// <summary>Timestamp mismatch indicates concurrent edits.</summary>
    ConcurrentEdit
}

public enum ConflictSeverity
{
    /// <summary>Conflict can be auto-resolved.</summary>
    Low,
    /// <summary>Conflict requires attention but has default resolution.</summary>
    Medium,
    /// <summary>Conflict requires manual intervention.</summary>
    High
}
```

### 3.4 Conflict Resolution Strategy

```csharp
/// <summary>
/// Strategy for resolving sync conflicts.
/// </summary>
public enum ConflictResolutionStrategy
{
    /// <summary>Use the document value as authoritative.</summary>
    UseDocument,
    /// <summary>Use the graph value as authoritative.</summary>
    UseGraph,
    /// <summary>Require manual intervention.</summary>
    Manual,
    /// <summary>Attempt to merge both values intelligently.</summary>
    Merge,
    /// <summary>Discard the document changes.</summary>
    DiscardDocument,
    /// <summary>Discard the graph changes.</summary>
    DiscardGraph
}
```

### 3.5 Sync Context

```csharp
/// <summary>
/// Context for sync operations.
/// </summary>
public record SyncContext
{
    /// <summary>User ID initiating the sync.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Document being synchronized.</summary>
    public required Document Document { get; init; }

    /// <summary>Workspace ID.</summary>
    public Guid? WorkspaceId { get; init; }

    /// <summary>Whether to auto-resolve conflicts.</summary>
    public bool AutoResolveConflicts { get; init; } = true;

    /// <summary>Default strategy for conflict resolution.</summary>
    public ConflictResolutionStrategy DefaultConflictStrategy { get; init; } = ConflictResolutionStrategy.Merge;

    /// <summary>Whether to publish sync events.</summary>
    public bool PublishEvents { get; init; } = true;

    /// <summary>Timeout for sync operation.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
}

public record GraphChange
{
    /// <summary>The entity that changed.</summary>
    public required Guid EntityId { get; init; }

    /// <summary>Type of change.</summary>
    public required ChangeType ChangeType { get; init; }

    /// <summary>Previous value (for updates).</summary>
    public object? PreviousValue { get; init; }

    /// <summary>New value.</summary>
    public required object NewValue { get; init; }

    /// <summary>User who made the change.</summary>
    public Guid? ChangedBy { get; init; }

    /// <summary>Timestamp of change.</summary>
    public required DateTimeOffset ChangedAt { get; init; }
}

public enum ChangeType
{
    EntityCreated,
    EntityUpdated,
    EntityDeleted,
    RelationshipCreated,
    RelationshipDeleted,
    PropertyChanged
}
```

---

## 4. Implementation

### 4.1 Sync Service

```csharp
public class SyncService : ISyncService
{
    private readonly ISyncOrchestrator _orchestrator;
    private readonly ISyncStatusTracker _statusTracker;
    private readonly IConflictResolver _conflictResolver;
    private readonly ILicenseService _licenseService;
    private readonly IMediator _mediator;
    private readonly ILogger<SyncService> _logger;

    public SyncService(
        ISyncOrchestrator orchestrator,
        ISyncStatusTracker statusTracker,
        IConflictResolver conflictResolver,
        ILicenseService licenseService,
        IMediator mediator,
        ILogger<SyncService> logger)
    {
        _orchestrator = orchestrator;
        _statusTracker = statusTracker;
        _conflictResolver = conflictResolver;
        _licenseService = licenseService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<SyncResult> SyncDocumentToGraphAsync(
        Document document,
        SyncContext context,
        CancellationToken ct = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "Starting document-to-graph sync for document {DocumentId}",
                document.Id);

            // Check license
            var license = await _licenseService.GetCurrentLicenseAsync(ct);
            if (!CanPerformSync(license.Tier, SyncDirection.DocumentToGraph))
            {
                throw new UnauthorizedAccessException(
                    $"License tier {license.Tier} does not support document-to-graph sync");
            }

            // Update status to in-progress
            await _statusTracker.UpdateStatusAsync(
                document.Id,
                new SyncStatus
                {
                    DocumentId = document.Id,
                    State = SyncState.PendingSync,
                    IsSyncInProgress = true,
                    LastAttemptAt = DateTimeOffset.UtcNow
                },
                ct);

            // Execute sync
            var result = await _orchestrator.ExecuteDocumentToGraphAsync(document, context, ct);

            stopwatch.Stop();
            result = result with { Duration = stopwatch.Elapsed };

            // Update status
            var newState = result.Status == SyncOperationStatus.Success
                ? SyncState.InSync
                : result.Status == SyncOperationStatus.SuccessWithConflicts
                    ? SyncState.NeedsReview
                    : SyncState.Conflict;

            await _statusTracker.UpdateStatusAsync(
                document.Id,
                new SyncStatus
                {
                    DocumentId = document.Id,
                    State = newState,
                    LastSyncAt = DateTimeOffset.UtcNow,
                    IsSyncInProgress = false,
                    UnresolvedConflicts = result.Conflicts.Count
                },
                ct);

            // Publish sync event
            if (context.PublishEvents)
            {
                await _mediator.Publish(
                    new SyncCompletedEvent
                    {
                        DocumentId = document.Id,
                        Result = result,
                        SyncDirection = SyncDirection.DocumentToGraph
                    },
                    ct);
            }

            _logger.LogInformation(
                "Document-to-graph sync completed for {DocumentId} in {Duration}ms with status {Status}",
                document.Id, stopwatch.ElapsedMilliseconds, result.Status);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Document-to-graph sync failed for {DocumentId}",
                document.Id);

            await _statusTracker.UpdateStatusAsync(
                document.Id,
                new SyncStatus
                {
                    DocumentId = document.Id,
                    State = SyncState.Conflict,
                    IsSyncInProgress = false,
                    LastError = ex.Message
                },
                ct);

            throw;
        }
    }

    public async Task<IReadOnlyList<Document>> GetAffectedDocumentsAsync(
        GraphChange change,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Finding documents affected by graph change to entity {EntityId}",
            change.EntityId);

        var results = await _orchestrator.ExecuteGraphToDocumentAsync(change, ct);
        return results
            .Where(r => r.Status != SyncOperationStatus.NoChanges)
            .Select(r => r.Document)
            .ToList();
    }

    public async Task<SyncStatus> GetSyncStatusAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        return await _statusTracker.GetStatusAsync(documentId, ct);
    }

    public async Task<SyncResult> ResolveConflictAsync(
        Guid documentId,
        ConflictResolutionStrategy strategy,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Resolving conflicts for document {DocumentId} using strategy {Strategy}",
            documentId, strategy);

        var status = await _statusTracker.GetStatusAsync(documentId, ct);
        if (status.State != SyncState.Conflict && status.UnresolvedConflicts == 0)
        {
            return new SyncResult
            {
                Status = SyncOperationStatus.NoChanges,
                Duration = TimeSpan.Zero
            };
        }

        var result = await _conflictResolver.ResolveAsync(
            documentId, strategy, ct);

        await _statusTracker.UpdateStatusAsync(
            documentId,
            status with
            {
                State = result.Status == SyncOperationStatus.Success ? SyncState.InSync : SyncState.NeedsReview,
                UnresolvedConflicts = result.Conflicts.Count
            },
            ct);

        return result;
    }

    public async Task<bool> NeedsSyncAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        var status = await _statusTracker.GetStatusAsync(documentId, ct);
        return status.State == SyncState.PendingSync ||
               status.State == SyncState.Conflict ||
               status.PendingChanges > 0;
    }

    private bool CanPerformSync(LicenseTier tier, SyncDirection direction)
    {
        return (tier, direction) switch
        {
            (LicenseTier.Core, _) => false,
            (LicenseTier.WriterPro, SyncDirection.DocumentToGraph) => true,
            (LicenseTier.WriterPro, SyncDirection.GraphToDocument) => false,
            (LicenseTier.Teams, _) => true,
            (LicenseTier.Enterprise, _) => true,
            _ => false
        };
    }
}

public enum SyncDirection
{
    DocumentToGraph,
    GraphToDocument,
    Bidirectional
}
```

### 4.2 Sync Orchestrator

```csharp
public class SyncOrchestrator : ISyncOrchestrator
{
    private readonly IEntityExtractionPipeline _extractionPipeline;
    private readonly IClaimExtractionService _claimExtractor;
    private readonly IGraphRepository _graphRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly ISyncConflictDetector _conflictDetector;
    private readonly ILogger<SyncOrchestrator> _logger;

    public SyncOrchestrator(
        IEntityExtractionPipeline extractionPipeline,
        IClaimExtractionService claimExtractor,
        IGraphRepository graphRepository,
        IDocumentRepository documentRepository,
        ISyncConflictDetector conflictDetector,
        ILogger<SyncOrchestrator> logger)
    {
        _extractionPipeline = extractionPipeline;
        _claimExtractor = claimExtractor;
        _graphRepository = graphRepository;
        _documentRepository = documentRepository;
        _conflictDetector = conflictDetector;
        _logger = logger;
    }

    public async Task<SyncResult> ExecuteDocumentToGraphAsync(
        Document document,
        SyncContext context,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var affectedEntities = new List<KnowledgeEntity>();
        var affectedClaims = new List<Claim>();
        var affectedRelationships = new List<KnowledgeRelationship>();
        var conflicts = new List<SyncConflict>();

        try
        {
            // 1. Extract entities from document
            var extractionResult = await _extractionPipeline.ExtractAsync(
                document.Content, ct);

            affectedEntities.AddRange(extractionResult.Entities);

            _logger.LogDebug(
                "Extracted {Count} entities from document {DocumentId}",
                extractionResult.Entities.Count, document.Id);

            // 2. Detect conflicts
            var detectedConflicts = await _conflictDetector.DetectAsync(
                document, extractionResult, ct);

            conflicts.AddRange(detectedConflicts);

            // 3. Extract claims
            var claims = await _claimExtractor.ExtractAsync(
                document, extractionResult, ct);

            affectedClaims.AddRange(claims);

            // 4. Upsert entities to graph
            var upsertedEntities = await _graphRepository.UpsertEntitiesAsync(
                extractionResult.Entities, ct);

            // 5. Create relationships if present
            if (extractionResult.Relationships.Count > 0)
            {
                var relationships = await _graphRepository.CreateRelationshipsAsync(
                    extractionResult.Relationships, ct);

                affectedRelationships.AddRange(relationships);
            }

            // 6. Store claims
            await _graphRepository.UpsertClaimsAsync(claims, ct);

            // 7. Mark document as synced
            await _documentRepository.MarkSyncedAsync(document.Id, DateTimeOffset.UtcNow, ct);

            stopwatch.Stop();

            return new SyncResult
            {
                Status = conflicts.Count == 0
                    ? SyncOperationStatus.Success
                    : SyncOperationStatus.SuccessWithConflicts,
                EntitiesAffected = affectedEntities,
                ClaimsAffected = affectedClaims,
                RelationshipsAffected = affectedRelationships,
                Conflicts = conflicts,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Document-to-graph sync failed for {DocumentId}",
                document.Id);

            stopwatch.Stop();

            return new SyncResult
            {
                Status = SyncOperationStatus.Failed,
                Conflicts = conflicts,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<IReadOnlyList<SyncResult>> ExecuteGraphToDocumentAsync(
        GraphChange change,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Executing graph-to-document sync for entity {EntityId}",
            change.EntityId);

        var results = new List<SyncResult>();

        // Find documents that reference this entity
        var documents = await _documentRepository.FindDocumentsReferencingEntityAsync(
            change.EntityId, ct);

        foreach (var document in documents)
        {
            var result = new SyncResult
            {
                Status = SyncOperationStatus.NoChanges,
                Duration = TimeSpan.Zero
            };

            // Flag document for review
            await _documentRepository.MarkFlaggedAsync(
                document.Id,
                $"Graph entity {change.EntityId} changed",
                ct);

            results.Add(result);
        }

        return results;
    }

    public async Task<IReadOnlyList<SyncConflict>> DetectConflictsAsync(
        Document document,
        ExtractionResult extraction,
        CancellationToken ct = default)
    {
        return await _conflictDetector.DetectAsync(document, extraction, ct);
    }
}
```

---

## 5. Algorithm / Flow

### Document-to-Graph Sync Flow

```
1. Validate license tier supports operation
2. Extract entities from document content
3. Extract claims from entities
4. Detect conflicts against existing graph state
5. If conflicts and auto-resolve enabled:
   - Apply conflict resolution strategy
   - Update conflicted entities
6. Upsert entities to graph
7. Create/update relationships
8. Store claims
9. Mark document as synced
10. Publish SyncCompletedEvent
11. Return SyncResult with affected entities/claims/relationships
```

### Graph-to-Document Sync Flow

```
1. Find all documents referencing changed entity
2. For each affected document:
   - Flag document as requiring review
   - Record the change
3. Publish DocumentFlaggedEvent
4. Return affected documents
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| License violation | Throw UnauthorizedAccessException |
| Extraction failure | Log, return Failed status |
| Conflict detection failure | Log, continue with conflicts |
| Graph repository failure | Rollback, return Failed status |
| Timeout exceeded | Cancel operation, return Failed status |

---

## 7. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `SyncDocumentToGraph_Success` | Full sync completes successfully |
| `SyncDocumentToGraph_WithConflicts` | Conflicts detected and reported |
| `SyncDocumentToGraph_LicenseCheckFails` | UnauthorizedAccessException thrown |
| `GetAffectedDocuments_ReturnsCorrect` | Correct documents identified |
| `ResolveConflict_AppliesStrategy` | Strategy applied correctly |
| `NeedsSync_CorrectStatus` | Correct status evaluation |
| `StatusTracking_UpdatesCorrectly` | Status transitions correct |

---

## 8. Performance

| Aspect | Target |
| :----- | :----- |
| Entity extraction | < 2 seconds per 10KB |
| Conflict detection | < 500ms |
| Graph upsert | < 1 second per 100 entities |
| Total sync | < 5 minutes (configurable timeout) |

---

## 9. License Gating

| Tier | DocumentToGraph | GraphToDocument | Conflict Resolution |
| :--- | :--- | :--- | :--- |
| Core | No | No | N/A |
| WriterPro | Manual only | No | Manual |
| Teams | Full | Full | All strategies |
| Enterprise | Full | Full | All + custom |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
