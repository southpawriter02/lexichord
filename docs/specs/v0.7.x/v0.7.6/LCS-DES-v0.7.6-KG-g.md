# LCS-DES-076-KG-g: Graph-to-Doc Sync

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-076-KG-g |
| **System Breakdown** | LCS-SBD-076-KG |
| **Version** | v0.7.6 |
| **Codename** | Graph-to-Doc Sync (CKVS Phase 4c) |
| **Estimated Hours** | 6 |
| **Status** | Complete |
| **Last Updated** | 2026-02-19 |

---

## 1. Overview

### 1.1 Purpose

The **Graph-to-Doc Sync** module handles unidirectional synchronization from the knowledge graph to documents. When graph entities or relationships change, it identifies affected documents and flags them for review, enabling users to incorporate graph updates into their documents.

### 1.2 Key Responsibilities

- Detect documents referencing changed graph entities
- Flag documents when graph changes affect their content
- Track change notifications and audit trail
- Manage document review workflows
- Support incremental change detection
- Prevent notification fatigue with smart batching
- Support graph change subscriptions

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Sync/
      GraphToDoc/
        IGraphToDocumentSyncProvider.cs
        GraphToDocumentSyncProvider.cs
        AffectedDocumentDetector.cs
        DocumentFlagger.cs
```

---

## 2. Interface Definitions

### 2.1 Graph-to-Document Sync Provider

```csharp
namespace Lexichord.KnowledgeGraph.Sync.GraphToDoc;

/// <summary>
/// Provider for graph-to-document synchronization.
/// </summary>
public interface IGraphToDocumentSyncProvider
{
    /// <summary>
    /// Handles a graph change and flags affected documents.
    /// </summary>
    Task<GraphToDocSyncResult> OnGraphChangeAsync(
        GraphChange change,
        GraphToDocSyncOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets documents affected by a graph entity.
    /// </summary>
    Task<IReadOnlyList<AffectedDocument>> GetAffectedDocumentsAsync(
        Guid entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all pending flags for a document.
    /// </summary>
    Task<IReadOnlyList<DocumentFlag>> GetPendingFlagsAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves a flag as reviewed.
    /// </summary>
    Task<bool> ResolveFlagAsync(
        Guid flagId,
        FlagResolution resolution,
        CancellationToken ct = default);

    /// <summary>
    /// Subscribes to graph changes for a document.
    /// </summary>
    Task SubscribeToGraphChangesAsync(
        Guid documentId,
        GraphChangeSubscription subscription,
        CancellationToken ct = default);
}
```

### 2.2 Affected Document Detector

```csharp
/// <summary>
/// Detects documents affected by graph changes.
/// </summary>
public interface IAffectedDocumentDetector
{
    /// <summary>
    /// Detects documents referencing a changed entity.
    /// </summary>
    Task<IReadOnlyList<AffectedDocument>> DetectAsync(
        GraphChange change,
        CancellationToken ct = default);

    /// <summary>
    /// Detects documents referencing any entity in a set.
    /// </summary>
    Task<IReadOnlyList<AffectedDocument>> DetectBatchAsync(
        IReadOnlyList<GraphChange> changes,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the relationship between a document and entity.
    /// </summary>
    Task<DocumentEntityRelationship?> GetRelationshipAsync(
        Guid documentId,
        Guid entityId,
        CancellationToken ct = default);
}
```

### 2.3 Document Flagger

```csharp
/// <summary>
/// Manages flagging documents for review.
/// </summary>
public interface IDocumentFlagger
{
    /// <summary>
    /// Flags a document for review.
    /// </summary>
    Task<DocumentFlag> FlagDocumentAsync(
        Guid documentId,
        FlagReason reason,
        DocumentFlagOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Flags multiple documents.
    /// </summary>
    Task<IReadOnlyList<DocumentFlag>> FlagDocumentsAsync(
        IReadOnlyList<Guid> documentIds,
        FlagReason reason,
        DocumentFlagOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves a flag.
    /// </summary>
    Task<bool> ResolveFlagAsync(
        Guid flagId,
        FlagResolution resolution,
        CancellationToken ct = default);

    /// <summary>
    /// Batch resolves flags.
    /// </summary>
    Task<int> ResolveFlagsAsync(
        IReadOnlyList<Guid> flagIds,
        FlagResolution resolution,
        CancellationToken ct = default);
}
```

---

## 3. Data Types

### 3.1 Graph-to-Doc Sync Result

```csharp
/// <summary>
/// Result of graph-to-document synchronization.
/// </summary>
public record GraphToDocSyncResult
{
    /// <summary>Status of the sync operation.</summary>
    public required SyncOperationStatus Status { get; init; }

    /// <summary>Documents affected by the graph change.</summary>
    public IReadOnlyList<AffectedDocument> AffectedDocuments { get; init; } = [];

    /// <summary>Flags created for documents.</summary>
    public IReadOnlyList<DocumentFlag> FlagsCreated { get; init; } = [];

    /// <summary>Total documents notified.</summary>
    public int TotalDocumentsNotified { get; init; }

    /// <summary>Graph change that triggered the sync.</summary>
    public required GraphChange TriggeringChange { get; init; }

    /// <summary>Duration of the sync operation.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Error message if operation failed.</summary>
    public string? ErrorMessage { get; init; }
}
```

### 3.2 Affected Document

```csharp
/// <summary>
/// A document affected by a graph change.
/// </summary>
public record AffectedDocument
{
    /// <summary>Document ID.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Document name.</summary>
    public required string DocumentName { get; init; }

    /// <summary>How the document references the changed entity.</summary>
    public required DocumentEntityRelationship Relationship { get; init; }

    /// <summary>Number of references to the changed entity.</summary>
    public int ReferenceCount { get; init; }

    /// <summary>Suggested action for the document.</summary>
    public SuggestedAction? SuggestedAction { get; init; }

    /// <summary>Timestamp when the document was last modified.</summary>
    public DateTimeOffset LastModifiedAt { get; init; }

    /// <summary>Timestamp when document was last synced.</summary>
    public DateTimeOffset? LastSyncedAt { get; init; }
}

public enum DocumentEntityRelationship
{
    /// <summary>Document explicitly references the entity.</summary>
    ExplicitReference,
    /// <summary>Document implicitly references via text similarity.</summary>
    ImplicitReference,
    /// <summary>Entity is derived from document content.</summary>
    DerivedFrom,
    /// <summary>Document and entity both reference common sources.</summary>
    IndirectReference
}

public record SuggestedAction
{
    /// <summary>Type of suggested action.</summary>
    public required ActionType ActionType { get; init; }

    /// <summary>Description of the action.</summary>
    public required string Description { get; init; }

    /// <summary>Suggested text to update.</summary>
    public string? SuggestedText { get; init; }

    /// <summary>Confidence in the suggestion (0-1).</summary>
    public float Confidence { get; init; } = 0.5f;
}

public enum ActionType
{
    /// <summary>Update references to match graph.</summary>
    UpdateReferences,
    /// <summary>Add new information from graph.</summary>
    AddInformation,
    /// <summary>Remove outdated information.</summary>
    RemoveInformation,
    /// <summary>Requires manual review.</summary>
    ManualReview
}
```

### 3.3 Document Flag

```csharp
/// <summary>
/// A flag on a document requiring review.
/// </summary>
public record DocumentFlag
{
    /// <summary>Unique flag ID.</summary>
    public required Guid FlagId { get; init; }

    /// <summary>Document ID.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Entity that triggered the flag.</summary>
    public required Guid TriggeringEntityId { get; init; }

    /// <summary>Reason for the flag.</summary>
    public required FlagReason Reason { get; init; }

    /// <summary>Description of the flag.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Priority of the flag.</summary>
    public FlagPriority Priority { get; init; } = FlagPriority.Medium;

    /// <summary>Current status of the flag.</summary>
    public FlagStatus Status { get; init; } = FlagStatus.Pending;

    /// <summary>Timestamp when flag was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Timestamp when flag was resolved.</summary>
    public DateTimeOffset? ResolvedAt { get; init; }

    /// <summary>User who resolved the flag.</summary>
    public Guid? ResolvedBy { get; init; }

    /// <summary>Resolution applied.</summary>
    public FlagResolution? Resolution { get; init; }

    /// <summary>Notification sent to document owner.</summary>
    public bool NotificationSent { get; init; }

    /// <summary>Timestamp of notification.</summary>
    public DateTimeOffset? NotificationSentAt { get; init; }
}

public enum FlagReason
{
    /// <summary>Entity value changed in graph.</summary>
    EntityValueChanged,
    /// <summary>Entity properties updated.</summary>
    EntityPropertiesUpdated,
    /// <summary>Entity deleted from graph.</summary>
    EntityDeleted,
    /// <summary>New relationship created.</summary>
    NewRelationship,
    /// <summary>Relationship removed.</summary>
    RelationshipRemoved,
    /// <summary>Manual sync requested.</summary>
    ManualSyncRequested,
    /// <summary>Conflict detected.</summary>
    ConflictDetected
}

public enum FlagPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum FlagStatus
{
    /// <summary>Flag pending review.</summary>
    Pending,
    /// <summary>Flag acknowledged but not resolved.</summary>
    Acknowledged,
    /// <summary>Flag resolved.</summary>
    Resolved,
    /// <summary>Flag dismissed.</summary>
    Dismissed,
    /// <summary>Flag escalated.</summary>
    Escalated
}

public enum FlagResolution
{
    /// <summary>Document updated with graph changes.</summary>
    UpdatedWithGraphChanges,
    /// <summary>Graph changes rejected, document unchanged.</summary>
    RejectedGraphChanges,
    /// <summary>Manual merge performed.</summary>
    ManualMerge,
    /// <summary>Flag dismissed as non-critical.</summary>
    Dismissed
}
```

### 3.4 Graph-to-Doc Sync Options

```csharp
/// <summary>
/// Options for graph-to-document synchronization.
/// </summary>
public record GraphToDocSyncOptions
{
    /// <summary>Whether to flag affected documents automatically.</summary>
    public bool AutoFlagDocuments { get; init; } = true;

    /// <summary>Whether to send notifications to document owners.</summary>
    public bool SendNotifications { get; init; } = true;

    /// <summary>Flag priority for different change types.</summary>
    public Dictionary<FlagReason, FlagPriority> ReasonPriorities { get; init; } = new()
    {
        [FlagReason.EntityValueChanged] = FlagPriority.High,
        [FlagReason.EntityDeleted] = FlagPriority.Critical,
        [FlagReason.NewRelationship] = FlagPriority.Medium
    };

    /// <summary>Batch size for processing multiple changes.</summary>
    public int BatchSize { get; init; } = 100;

    /// <summary>Maximum documents to flag per change.</summary>
    public int MaxDocumentsPerChange { get; init; } = 1000;

    /// <summary>Minimum confidence for suggested actions.</summary>
    public float MinActionConfidence { get; init; } = 0.6f;

    /// <summary>Whether to include suggested actions in flags.</summary>
    public bool IncludeSuggestedActions { get; init; } = true;

    /// <summary>Whether to deduplicate notifications.</summary>
    public bool DeduplicateNotifications { get; init; } = true;

    /// <summary>Time window for deduplication.</summary>
    public TimeSpan DeduplicationWindow { get; init; } = TimeSpan.FromHours(1);
}

public record DocumentFlagOptions
{
    /// <summary>Priority for the flag.</summary>
    public FlagPriority Priority { get; init; } = FlagPriority.Medium;

    /// <summary>User creating the flag.</summary>
    public Guid? CreatedBy { get; init; }

    /// <summary>Whether to include suggested actions.</summary>
    public bool IncludeSuggestedActions { get; init; } = true;

    /// <summary>Tags for categorizing flags.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Whether to send notification immediately.</summary>
    public bool SendNotification { get; init; } = true;

    /// <summary>Additional context about the flag.</summary>
    public Dictionary<string, object> Context { get; init; } = new();
}

public record GraphChangeSubscription
{
    /// <summary>Document ID being monitored.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Entity IDs to monitor.</summary>
    public IReadOnlyList<Guid> EntityIds { get; init; } = [];

    /// <summary>Change types to monitor.</summary>
    public IReadOnlyList<ChangeType> ChangeTypes { get; init; } = [];

    /// <summary>User to notify on changes.</summary>
    public Guid NotifyUser { get; init; }

    /// <summary>Subscription created at.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Whether subscription is active.</summary>
    public bool IsActive { get; init; } = true;
}

public record DocumentEntityRelationshipRecord
{
    public Guid DocumentId { get; init; }
    public Guid EntityId { get; init; }
    public DocumentEntityRelationship RelationshipType { get; init; }
    public int ReferenceCount { get; init; }
    public DateTimeOffset LastDetectedAt { get; init; }
}
```

---

## 4. Implementation

### 4.1 Graph-to-Document Sync Provider

```csharp
public class GraphToDocumentSyncProvider : IGraphToDocumentSyncProvider
{
    private readonly IAffectedDocumentDetector _affectedDocumentDetector;
    private readonly IDocumentFlagger _documentFlagger;
    private readonly IDocumentRepository _documentRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<GraphToDocumentSyncProvider> _logger;

    public GraphToDocumentSyncProvider(
        IAffectedDocumentDetector affectedDocumentDetector,
        IDocumentFlagger documentFlagger,
        IDocumentRepository documentRepository,
        IMediator mediator,
        ILogger<GraphToDocumentSyncProvider> logger)
    {
        _affectedDocumentDetector = affectedDocumentDetector;
        _documentFlagger = documentFlagger;
        _documentRepository = documentRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<GraphToDocSyncResult> OnGraphChangeAsync(
        GraphChange change,
        GraphToDocSyncOptions options,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Graph-to-document sync triggered for entity {EntityId} with change type {ChangeType}",
            change.EntityId, change.ChangeType);

        try
        {
            // 1. Detect affected documents
            _logger.LogDebug("Detecting documents affected by change to entity {EntityId}", change.EntityId);

            var affectedDocuments = await _affectedDocumentDetector.DetectAsync(change, ct);

            if (affectedDocuments.Count > options.MaxDocumentsPerChange)
            {
                affectedDocuments = affectedDocuments
                    .Take(options.MaxDocumentsPerChange)
                    .ToList();

                _logger.LogWarning(
                    "Affected documents exceed maximum for entity {EntityId}: {Count}",
                    change.EntityId, affectedDocuments.Count);
            }

            _logger.LogDebug(
                "Found {Count} documents affected by entity {EntityId}",
                affectedDocuments.Count, change.EntityId);

            // 2. Determine flag priority
            var priority = options.ReasonPriorities.TryGetValue(
                FlagReasonFromChangeType(change.ChangeType),
                out var p) ? p : FlagPriority.Medium;

            // 3. Flag documents if enabled
            var flagsCreated = new List<DocumentFlag>();

            if (options.AutoFlagDocuments && affectedDocuments.Count > 0)
            {
                _logger.LogDebug("Flagging {Count} documents", affectedDocuments.Count);

                var documentIds = affectedDocuments.Select(d => d.DocumentId).ToList();

                flagsCreated = (await _documentFlagger.FlagDocumentsAsync(
                    documentIds,
                    FlagReasonFromChangeType(change.ChangeType),
                    new DocumentFlagOptions
                    {
                        Priority = priority,
                        IncludeSuggestedActions = options.IncludeSuggestedActions,
                        SendNotification = options.SendNotifications
                    },
                    ct)).ToList();

                _logger.LogInformation(
                    "Created {Count} flags for entity {EntityId}",
                    flagsCreated.Count, change.EntityId);
            }

            // 4. Publish event
            await _mediator.Publish(
                new GraphToDocumentSyncedEvent
                {
                    TriggeringChange = change,
                    AffectedDocuments = affectedDocuments,
                    FlagsCreated = flagsCreated
                },
                ct);

            stopwatch.Stop();

            return new GraphToDocSyncResult
            {
                Status = flagsCreated.Count > 0
                    ? SyncOperationStatus.Success
                    : SyncOperationStatus.NoChanges,
                AffectedDocuments = affectedDocuments,
                FlagsCreated = flagsCreated,
                TotalDocumentsNotified = flagsCreated.Count,
                TriggeringChange = change,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Graph-to-document sync failed for entity {EntityId}",
                change.EntityId);

            stopwatch.Stop();

            return new GraphToDocSyncResult
            {
                Status = SyncOperationStatus.Failed,
                TriggeringChange = change,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<IReadOnlyList<AffectedDocument>> GetAffectedDocumentsAsync(
        Guid entityId,
        CancellationToken ct = default)
    {
        return await _affectedDocumentDetector.DetectAsync(
            new GraphChange
            {
                EntityId = entityId,
                ChangeType = ChangeType.EntityUpdated,
                NewValue = null!,
                ChangedAt = DateTimeOffset.UtcNow
            },
            ct);
    }

    public async Task<IReadOnlyList<DocumentFlag>> GetPendingFlagsAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        return await _documentRepository.GetPendingFlagsAsync(documentId, ct);
    }

    public async Task<bool> ResolveFlagAsync(
        Guid flagId,
        FlagResolution resolution,
        CancellationToken ct = default)
    {
        return await _documentFlagger.ResolveFlagAsync(flagId, resolution, ct);
    }

    public async Task SubscribeToGraphChangesAsync(
        Guid documentId,
        GraphChangeSubscription subscription,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Document {DocumentId} subscribed to graph changes for {Count} entities",
            documentId, subscription.EntityIds.Count);

        await _documentRepository.CreateSubscriptionAsync(subscription, ct);
    }

    private FlagReason FlagReasonFromChangeType(ChangeType changeType)
    {
        return changeType switch
        {
            ChangeType.EntityUpdated => FlagReason.EntityValueChanged,
            ChangeType.EntityDeleted => FlagReason.EntityDeleted,
            ChangeType.EntityCreated => FlagReason.NewRelationship,
            ChangeType.RelationshipCreated => FlagReason.NewRelationship,
            ChangeType.RelationshipDeleted => FlagReason.RelationshipRemoved,
            _ => FlagReason.EntityValueChanged
        };
    }
}
```

### 4.2 Affected Document Detector

```csharp
public class AffectedDocumentDetector : IAffectedDocumentDetector
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<AffectedDocumentDetector> _logger;

    public AffectedDocumentDetector(
        IDocumentRepository documentRepository,
        IGraphRepository graphRepository,
        ILogger<AffectedDocumentDetector> logger)
    {
        _documentRepository = documentRepository;
        _graphRepository = graphRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AffectedDocument>> DetectAsync(
        GraphChange change,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Detecting documents affected by entity {EntityId} change",
            change.EntityId);

        var affected = new List<AffectedDocument>();

        // Find explicit references
        var explicitRefs = await _documentRepository.FindDocumentsReferencingEntityAsync(
            change.EntityId, ct);

        foreach (var doc in explicitRefs)
        {
            var relationship = await GetRelationshipAsync(doc.Id, change.EntityId, ct);

            affected.Add(new AffectedDocument
            {
                DocumentId = doc.Id,
                DocumentName = doc.Name,
                Relationship = relationship?.RelationshipType ?? DocumentEntityRelationship.ExplicitReference,
                ReferenceCount = relationship?.ReferenceCount ?? 1,
                LastModifiedAt = doc.LastModifiedAt,
                LastSyncedAt = doc.LastSyncedAt
            });
        }

        _logger.LogDebug(
            "Found {Count} documents affected by entity {EntityId}",
            affected.Count, change.EntityId);

        return affected;
    }

    public async Task<IReadOnlyList<AffectedDocument>> DetectBatchAsync(
        IReadOnlyList<GraphChange> changes,
        CancellationToken ct = default)
    {
        var allAffected = new Dictionary<Guid, AffectedDocument>();

        foreach (var change in changes)
        {
            var affected = await DetectAsync(change, ct);

            foreach (var doc in affected)
            {
                if (!allAffected.ContainsKey(doc.DocumentId))
                {
                    allAffected[doc.DocumentId] = doc;
                }
            }
        }

        return allAffected.Values.ToList();
    }

    public async Task<DocumentEntityRelationship?> GetRelationshipAsync(
        Guid documentId,
        Guid entityId,
        CancellationToken ct = default)
    {
        var relationship = await _documentRepository.GetEntityRelationshipAsync(
            documentId, entityId, ct);

        return relationship?.RelationshipType;
    }
}
```

### 4.3 Document Flagger

```csharp
public class DocumentFlagger : IDocumentFlagger
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentFlagger> _logger;

    public DocumentFlagger(
        IDocumentRepository documentRepository,
        IMediator mediator,
        ILogger<DocumentFlagger> logger)
    {
        _documentRepository = documentRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<DocumentFlag> FlagDocumentAsync(
        Guid documentId,
        FlagReason reason,
        DocumentFlagOptions options,
        CancellationToken ct = default)
    {
        var flag = new DocumentFlag
        {
            FlagId = Guid.NewGuid(),
            DocumentId = documentId,
            TriggeringEntityId = Guid.Empty,
            Reason = reason,
            Priority = options.Priority,
            CreatedAt = DateTimeOffset.UtcNow,
            NotificationSent = options.SendNotification
        };

        await _documentRepository.CreateFlagAsync(flag, ct);

        _logger.LogInformation(
            "Flagged document {DocumentId} with reason {Reason}",
            documentId, reason);

        if (options.SendNotification)
        {
            await _mediator.Publish(
                new DocumentFlaggedEvent { Flag = flag },
                ct);
        }

        return flag;
    }

    public async Task<IReadOnlyList<DocumentFlag>> FlagDocumentsAsync(
        IReadOnlyList<Guid> documentIds,
        FlagReason reason,
        DocumentFlagOptions options,
        CancellationToken ct = default)
    {
        var flags = new List<DocumentFlag>();

        foreach (var docId in documentIds)
        {
            var flag = await FlagDocumentAsync(docId, reason, options, ct);
            flags.Add(flag);
        }

        return flags;
    }

    public async Task<bool> ResolveFlagAsync(
        Guid flagId,
        FlagResolution resolution,
        CancellationToken ct = default)
    {
        var flag = await _documentRepository.GetFlagAsync(flagId, ct);
        if (flag == null)
        {
            return false;
        }

        var resolvedFlag = flag with
        {
            Status = FlagStatus.Resolved,
            Resolution = resolution,
            ResolvedAt = DateTimeOffset.UtcNow
        };

        await _documentRepository.UpdateFlagAsync(resolvedFlag, ct);

        _logger.LogInformation(
            "Resolved flag {FlagId} with resolution {Resolution}",
            flagId, resolution);

        return true;
    }

    public async Task<int> ResolveFlagsAsync(
        IReadOnlyList<Guid> flagIds,
        FlagResolution resolution,
        CancellationToken ct = default)
    {
        var resolved = 0;

        foreach (var flagId in flagIds)
        {
            if (await ResolveFlagAsync(flagId, resolution, ct))
            {
                resolved++;
            }
        }

        return resolved;
    }
}
```

---

## 5. Algorithm / Flow

### Graph-to-Document Change Handling Flow

```
1. Receive GraphChange event
2. Detect all documents referencing changed entity
3. If no affected documents:
   - Return NoChanges status
4. For each affected document:
   - Determine flag priority based on change type
   - Create DocumentFlag with reason and priority
5. If AutoFlagDocuments enabled:
   - Flag all affected documents
   - If SendNotifications enabled:
     - Publish notification events
6. Publish GraphToDocumentSyncedEvent
7. Return GraphToDocSyncResult with affected documents and flags
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Document detection fails | Log, continue with empty list |
| Flag creation fails | Log, mark as failed |
| Notification send fails | Log, continue without notification |
| Batch processing exceeds limit | Truncate to MaxDocumentsPerChange |

---

## 7. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `OnGraphChange_FlagsAffectedDocuments` | Documents flagged correctly |
| `OnGraphChange_SendsNotifications` | Notifications sent |
| `OnGraphChange_HighPriority` | Priority set correctly |
| `GetAffectedDocuments_ReturnsCorrect` | Correct documents returned |
| `GetPendingFlags_ReturnsUnresolved` | Pending flags returned |
| `ResolveFlag_UpdatesStatus` | Flag status updated |
| `Deduplication_PreventsDuplicates` | Duplicate notifications prevented |

---

## 8. Performance

| Aspect | Target |
| :----- | :----- |
| Document detection | < 1s per 100 documents |
| Flag creation | < 500ms per 100 flags |
| Batch detection | < 2s per 500 changes |
| Notification send | < 3s per 100 notifications |

---

## 9. License Gating

| Tier | GraphToDoc | Notifications | Smart Batching |
| :--- | :--- | :--- | :--- |
| Core | No | N/A | N/A |
| WriterPro | No | N/A | N/A |
| Teams | Yes | Yes | Basic |
| Enterprise | Yes | Yes | Advanced |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
