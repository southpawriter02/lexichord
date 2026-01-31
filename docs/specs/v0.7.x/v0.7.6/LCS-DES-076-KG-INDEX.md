# LCS-DES-076-KG-INDEX: Sync Service Architecture

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-076-KG-INDEX |
| **System Breakdown** | LCS-SBD-076-KG |
| **Version** | v0.7.6 |
| **Codename** | Sync Service Index (CKVS Phase 4c) |
| **Estimated Hours** | 27 total |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## Overview

This index provides a comprehensive guide to the v0.7.6 Sync Service specification, which implements bidirectional synchronization between documents and the knowledge graph. The Sync Service orchestrates all synchronization operations, manages conflict resolution, and maintains consistency between the document and graph layers.

---

## 1. Architecture Overview

### 1.1 System Components

The Sync Service consists of 6 main components plus a central orchestrator:

```
┌─────────────────────────────────────────────────────────┐
│                    Sync Service (Core)                  │
│               [LCS-DES-076-KG-e - 6 hours]              │
│         Orchestrates all sync operations                │
└─────────────────────────────────────────────────────────┘
                          │
        ┌─────────────────┼─────────────────┐
        │                 │                 │
        ▼                 ▼                 ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ Doc-to-Graph │  │ Graph-to-Doc │  │   Conflict   │
│    Sync      │  │    Sync      │  │   Resolver   │
│[076-KG-f]    │  │  [076-KG-g]  │  │ [076-KG-h]   │
│  8 hours     │  │  6 hours     │  │  6 hours     │
└──────────────┘  └──────────────┘  └──────────────┘
        │                 │                 │
        └─────────────────┼─────────────────┘
                          │
        ┌─────────────────┼─────────────────┐
        │                 │                 │
        ▼                 ▼                 ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│    Status    │  │    Event     │  │  Extraction  │
│   Tracker    │  │  Publisher   │  │  Pipeline    │
│[076-KG-i]    │  │  [076-KG-j]  │  │ (v0.4.5g)    │
│  4 hours     │  │  3 hours     │  │              │
└──────────────┘  └──────────────┘  └──────────────┘
```

### 1.2 Data Flow

#### Document-to-Graph Synchronization

```
Document
    │
    ▼
┌─────────────────────────┐
│ Extraction Pipeline     │ → Extract entities, claims
│ (IEntityExtractionPipeline)
└─────────────────────────┘
    │
    ▼
┌─────────────────────────┐
│ Conflict Detector       │ → Detect conflicts
│ (IConflictDetector)     │
└─────────────────────────┘
    │
    ▼
┌─────────────────────────┐
│ Conflict Resolver       │ → Apply resolution strategy
│ (IConflictResolver)     │
└─────────────────────────┘
    │
    ▼
┌─────────────────────────┐
│ Graph Repository        │ → Upsert entities & claims
│ (IGraphRepository)      │
└─────────────────────────┘
    │
    ▼
Knowledge Graph
```

#### Graph-to-Document Synchronization

```
Graph Change
    │
    ▼
┌──────────────────────────┐
│ Affected Doc Detector    │ → Find impacted documents
│ (IAffectedDocDetector)   │
└──────────────────────────┘
    │
    ▼
┌──────────────────────────┐
│ Document Flagger         │ → Flag for review
│ (IDocumentFlagger)       │
└──────────────────────────┘
    │
    ▼
┌──────────────────────────┐
│ Event Publisher (MediatR)│ → Notify handlers
│ (ISyncEventPublisher)    │
└──────────────────────────┘
    │
    ▼
Document Owner (Notified)
```

---

## 2. Specification Components

### 2.1 Core Components

#### **LCS-DES-076-KG-e: Sync Service Core** (6 hours)
**Location:** `/specs/v0.7.x/v0.7.6/LCS-DES-076-KG-e.md`

**Responsibilities:**
- Implement `ISyncService` interface
- Orchestrate document-to-graph sync pipeline
- Coordinate graph-to-document change propagation
- Manage sync workflow and operation sequencing
- Track affected entities, claims, and relationships
- Expose high-level sync operations

**Key Interfaces:**
- `ISyncService` - Main sync orchestration interface
- `ISyncOrchestrator` - Internal workflow coordination

**Key Classes:**
- `SyncService` - Primary implementation
- `SyncOrchestrator` - Workflow executor

**Dependencies:**
- `IEntityExtractionPipeline` (v0.4.5g)
- `IClaimExtractionService` (v0.5.6-KG)
- `IGraphRepository` (v0.4.5e)
- `IDocumentRepository` (v0.4.1c)
- `IMediator` (v0.0.7a)

---

#### **LCS-DES-076-KG-f: Doc-to-Graph Sync** (8 hours)
**Location:** `/specs/v0.7.x/v0.7.6/LCS-DES-076-KG-f.md`

**Responsibilities:**
- Extract structured data from unstructured documents
- Validate extracted entities against graph schema
- Transform extracted data for graph ingestion
- Manage upsert operations to knowledge graph
- Track extraction lineage for audit trail
- Support batch and incremental sync modes
- Maintain version history of extractions

**Key Interfaces:**
- `IDocumentToGraphSyncProvider` - Main sync provider
- `IExtractionTransformer` - Data transformation
- `IExtractionValidator` - Schema validation

**Key Classes:**
- `DocumentToGraphSyncProvider` - Sync execution
- `ExtractionTransformer` - Transforms and enriches
- `ExtractionValidator` - Validates extractions

**Key Concepts:**
- Entity extraction and validation
- Relationship creation
- Claim extraction and storage
- Extraction lineage tracking
- Enrichment with graph context

---

#### **LCS-DES-076-KG-g: Graph-to-Doc Sync** (6 hours)
**Location:** `/specs/v0.7.x/v0.7.6/LCS-DES-076-KG-g.md`

**Responsibilities:**
- Detect documents affected by graph changes
- Flag documents when graph entities change
- Manage notification workflows
- Track change audit trail
- Support incremental change detection
- Prevent notification fatigue with smart batching
- Enable graph change subscriptions

**Key Interfaces:**
- `IGraphToDocumentSyncProvider` - Main sync provider
- `IAffectedDocumentDetector` - Detects impacted docs
- `IDocumentFlagger` - Manages document flags

**Key Classes:**
- `GraphToDocumentSyncProvider` - Sync execution
- `AffectedDocumentDetector` - Impact analysis
- `DocumentFlagger` - Flag management

**Key Concepts:**
- Change propagation
- Affected document detection
- Document flagging for review
- Notification management
- Change subscriptions

---

#### **LCS-DES-076-KG-h: Conflict Resolver** (6 hours)
**Location:** `/specs/v0.7.x/v0.7.6/LCS-DES-076-KG-h.md`

**Responsibilities:**
- Detect value conflicts between document and graph
- Detect structural conflicts (missing entities/relationships)
- Apply conflict resolution strategies intelligently
- Support automatic conflict merging
- Enable manual conflict review workflows
- Maintain conflict history and audit trail
- Provide conflict visualization and comparison

**Key Interfaces:**
- `IConflictDetector` - Conflict detection
- `IConflictResolver` - Conflict resolution
- `IConflictMerger` - Intelligent merging

**Key Classes:**
- `ConflictDetector` - Detects conflicts
- `ConflictResolver` - Resolves using strategies
- `ConflictMerger` - Merges intelligently

**Resolution Strategies:**
- `UseDocument` - Document as authoritative
- `UseGraph` - Graph as authoritative
- `Merge` - Intelligent combination
- `Manual` - Human intervention required
- `DiscardDocument` - Ignore document changes
- `DiscardGraph` - Ignore graph changes

---

#### **LCS-DES-076-KG-i: Sync Status Tracker** (4 hours)
**Location:** `/specs/v0.7.x/v0.7.6/LCS-DES-076-KG-i.md`

**Responsibilities:**
- Track synchronization state for documents
- Maintain sync history and audit trail
- Store sync status metadata
- Support sync status queries and reporting
- Enable monitoring and alerting workflows
- Provide sync metrics and statistics
- Support bulk status updates

**Key Interfaces:**
- `ISyncStatusTracker` - Status tracking and queries
- `ISyncStatusRepository` - Data persistence

**Key Classes:**
- `SyncStatusTracker` - Status management (with caching)
- `SyncStatusRepository` - Persistence layer

**Status States:**
- `InSync` - Document and graph synchronized
- `PendingSync` - Changes awaiting sync
- `NeedsReview` - Requires manual review
- `Conflict` - Conflict exists
- `NeverSynced` - Never synchronized

**Metrics:**
- Success rate
- Average sync duration
- Conflict statistics
- Operation counts
- Time in current state

---

#### **LCS-DES-076-KG-j: Sync Event Publisher** (3 hours)
**Location:** `/specs/v0.7.x/v0.7.6/LCS-DES-076-KG-j.md`

**Responsibilities:**
- Publish sync events via MediatR
- Define event types for sync lifecycle
- Support event filtering and routing
- Enable event batching and deduplication
- Maintain event audit trail
- Support event subscriptions
- Provide event metadata and context

**Key Interfaces:**
- `ISyncEventPublisher` - Event publishing

**Key Classes:**
- `SyncEventPublisher` - MediatR publisher
- Event handlers for each event type

**Event Types:**
- `SyncCompletedEvent` - Sync operation completed
- `SyncConflictDetectedEvent` - Conflicts found
- `SyncConflictResolvedEvent` - Conflict resolved
- `GraphToDocumentSyncedEvent` - Graph change detected
- `DocumentFlaggedEvent` - Document flagged
- `SyncStatusChangedEvent` - Status changed
- `SyncFailedEvent` - Sync failed
- `SyncRetryEvent` - Sync retry attempted

---

### 2.2 Component Dependencies

```
SyncService (Core)
├── IEntityExtractionPipeline (v0.4.5g)
├── IClaimExtractionService (v0.5.6-KG)
├── IGraphRepository (v0.4.5e)
├── IDocumentRepository (v0.4.1c)
├── IMediator (v0.0.7a)
├── ISyncOrchestrator
│   ├── IDocumentToGraphSyncProvider
│   ├── IGraphToDocumentSyncProvider
│   ├── IConflictDetector
│   └── IConflictResolver
├── ISyncStatusTracker
└── ISyncEventPublisher
```

---

## 3. Data Type Hierarchy

### 3.1 Core Records

```
SyncResult
├── Status: SyncOperationStatus
├── EntitiesAffected: List<KnowledgeEntity>
├── ClaimsAffected: List<Claim>
├── RelationshipsAffected: List<KnowledgeRelationship>
├── Conflicts: List<SyncConflict>
└── Duration: TimeSpan

SyncStatus
├── DocumentId: Guid
├── State: SyncState (InSync/PendingSync/NeedsReview/Conflict/NeverSynced)
├── LastSyncAt: DateTimeOffset?
├── PendingChanges: int
└── UnresolvedConflicts: int

SyncConflict
├── ConflictTarget: string
├── DocumentValue: object
├── GraphValue: object
├── Type: ConflictType
└── Severity: ConflictSeverity (Low/Medium/High)

DocumentFlag
├── FlagId: Guid
├── DocumentId: Guid
├── TriggeringEntityId: Guid
├── Reason: FlagReason
├── Status: FlagStatus (Pending/Acknowledged/Resolved/Dismissed/Escalated)
└── Priority: FlagPriority (Low/Medium/High/Critical)
```

### 3.2 Enumerations

**SyncState:**
- InSync
- PendingSync
- NeedsReview
- Conflict
- NeverSynced

**ConflictType:**
- ValueMismatch
- MissingInGraph
- MissingInDocument
- RelationshipMismatch
- ConcurrentEdit

**ConflictResolutionStrategy:**
- UseDocument
- UseGraph
- Manual
- Merge
- DiscardDocument
- DiscardGraph

**FlagReason:**
- EntityValueChanged
- EntityPropertiesUpdated
- EntityDeleted
- NewRelationship
- RelationshipRemoved
- ManualSyncRequested
- ConflictDetected

---

## 4. API Surface

### 4.1 ISyncService Interface

```csharp
Task<SyncResult> SyncDocumentToGraphAsync(
    Document document,
    SyncContext context,
    CancellationToken ct = default);

Task<IReadOnlyList<Document>> GetAffectedDocumentsAsync(
    GraphChange change,
    CancellationToken ct = default);

Task<SyncStatus> GetSyncStatusAsync(
    Guid documentId,
    CancellationToken ct = default);

Task<SyncResult> ResolveConflictAsync(
    Guid documentId,
    ConflictResolutionStrategy strategy,
    CancellationToken ct = default);

Task<bool> NeedsSyncAsync(
    Guid documentId,
    CancellationToken ct = default);
```

### 4.2 ISyncStatusTracker Interface

```csharp
Task<SyncStatus> GetStatusAsync(
    Guid documentId,
    CancellationToken ct = default);

Task<SyncStatus> UpdateStatusAsync(
    Guid documentId,
    SyncStatus status,
    CancellationToken ct = default);

Task<IReadOnlyList<Guid>> GetDocumentsByStateAsync(
    SyncState state,
    CancellationToken ct = default);

Task<SyncMetrics> GetMetricsAsync(
    Guid documentId,
    CancellationToken ct = default);
```

### 4.3 ISyncEventPublisher Interface

```csharp
Task PublishAsync<TEvent>(
    TEvent eventData,
    SyncEventOptions? options = null,
    CancellationToken ct = default)
    where TEvent : ISyncEvent;

Task<IReadOnlyList<SyncEventRecord>> GetEventsAsync(
    SyncEventQuery query,
    CancellationToken ct = default);
```

---

## 5. License Gating Summary

### 5.1 Feature Availability by Tier

| Feature | Core | WriterPro | Teams | Enterprise |
| :------ | :--- | :-------- | :---- | :--------- |
| **Doc-to-Graph Sync** | No | Manual only | Full | Full |
| **Graph-to-Doc Sync** | No | No | Full | Full |
| **Conflict Detection** | No | Basic | Full | Full |
| **Auto-Resolution** | N/A | Low only | Low/Medium | All |
| **Conflict Merging** | No | No | Yes | Advanced |
| **Status Tracking** | No | Yes | Yes | Yes |
| **Event Publishing** | No | Yes | Yes | Yes |
| **Metrics/Analytics** | No | Basic | Full | Advanced |
| **Lineage Tracking** | No | Yes | Yes | Yes |
| **Notifications** | No | Manual | Automated | Customizable |

---

## 6. Integration Points

### 6.1 External Dependencies

```
Lexichord.KnowledgeGraph.Sync (v0.7.6)
└── Depends On:
    ├── IEntityExtractionPipeline (v0.4.5g)
    │   └── Extracts entities from document text
    ├── IClaimExtractionService (v0.5.6-KG)
    │   └── Extracts claims from entities
    ├── IGraphRepository (v0.4.5e)
    │   └── Manages knowledge graph persistence
    ├── IDocumentRepository (v0.4.1c)
    │   └── Manages document persistence
    └── IMediator (v0.0.7a)
        └── Publishes sync events
```

### 6.2 Consumer Interfaces

The Sync Service exposes:

```
ISyncService
├── For: Document editors, conflict resolution UIs
├── Operations: Sync, conflict resolution, status checks
└── Returns: SyncResult, SyncStatus, Document lists

ISyncStatusTracker
├── For: Dashboard, monitoring, alerting
├── Operations: Status queries, metrics
└── Returns: SyncStatus, SyncMetrics, operation history

ISyncEventPublisher
├── For: Event-driven handlers, webhooks
├── Operations: Event publication, subscription
└── Returns: Event records, publication status
```

---

## 7. Implementation Checklist

### Phase 1: Core Infrastructure (Week 1)

- [ ] Implement `SyncService` and `SyncOrchestrator`
- [ ] Implement `SyncStatusTracker` with caching
- [ ] Implement event types and `ISyncEventPublisher`
- [ ] Create test suite for core components
- [ ] Integration tests with repositories

### Phase 2: Doc-to-Graph Sync (Week 2)

- [ ] Implement `DocumentToGraphSyncProvider`
- [ ] Implement `ExtractionTransformer`
- [ ] Implement `ExtractionValidator`
- [ ] Implement extraction lineage tracking
- [ ] Create comprehensive test suite

### Phase 3: Graph-to-Doc Sync (Week 2)

- [ ] Implement `GraphToDocumentSyncProvider`
- [ ] Implement `AffectedDocumentDetector`
- [ ] Implement `DocumentFlagger`
- [ ] Implement notification workflows
- [ ] Create integration tests

### Phase 4: Conflict Resolution (Week 3)

- [ ] Implement `ConflictDetector`
- [ ] Implement `ConflictResolver`
- [ ] Implement `ConflictMerger`
- [ ] Implement all resolution strategies
- [ ] Create conflict test scenarios

### Phase 5: Event Publishing (Week 3)

- [ ] Implement event handlers
- [ ] Implement event storage
- [ ] Implement event history queries
- [ ] Implement subscriptions
- [ ] Create handler tests

### Phase 6: Testing & Polish (Week 4)

- [ ] Performance testing
- [ ] License gating tests
- [ ] End-to-end scenarios
- [ ] Documentation
- [ ] Code review

---

## 8. Performance Targets

| Operation | Target | Component |
| :--------- | :----- | :-------- |
| Entity extraction | < 2s/10KB | Doc-to-Graph |
| Conflict detection | < 500ms | Conflict Resolver |
| Graph upsert | < 1s/100 entities | Doc-to-Graph |
| Document detection | < 1s/100 docs | Graph-to-Doc |
| Flag creation | < 500ms/100 flags | Graph-to-Doc |
| Status update | < 100ms | Status Tracker |
| Event publication | < 50ms | Event Publisher |
| Total sync operation | < 5 minutes | Core |

---

## 9. Error Handling Strategy

All components follow consistent error handling:

1. **Detection**: Specific exception types for each error class
2. **Logging**: Structured logging with document/entity context
3. **Recovery**: Graceful degradation where possible
4. **Notification**: User-facing events for sync failures
5. **Audit**: All errors recorded in history
6. **Retry**: Automatic retry for transient failures

---

## 10. Testing Strategy

### 10.1 Unit Tests

- Component interface contracts
- Algorithm correctness
- Edge cases and boundary conditions
- Error handling paths

### 10.2 Integration Tests

- Component interaction
- Repository operations
- Event publishing and handling
- End-to-end sync scenarios

### 10.3 Performance Tests

- Sync operation timing
- Batch processing efficiency
- Memory usage patterns
- Concurrent operation handling

### 10.4 Scenario Tests

- Complete document-to-graph workflows
- Complete graph-to-document workflows
- Conflict detection and resolution
- License gating enforcement
- Notification delivery

---

## 11. Deployment Notes

### 11.1 Database Schema

Requires tables for:
- `SyncStatus` - Document sync state
- `SyncStatusHistory` - State change audit trail
- `SyncOperationRecord` - Operation history
- `DocumentFlag` - Document flags
- `SyncEventRecord` - Published events
- `ExtractionRecord` - Extraction lineage
- `GraphChangeSubscription` - Change subscriptions

### 11.2 Configuration

Required settings:
- Sync operation timeout
- Batch size limits
- Conflict resolution defaults
- Notification preferences
- Event retention period
- Cache TTL

### 11.3 Monitoring

Monitor these metrics:
- Sync success rate
- Average sync duration
- Conflict detection rate
- Conflict resolution rate
- Notification delivery rate
- Event publication latency

---

## 12. Version History

| Version | Date | Components | Changes |
| :------ | :--- | :--------- | :------ |
| 0.7.6 | 2026-01-31 | All | Initial specification |

---

## 13. Related Specifications

- **LCS-DES-076-KG-e**: Sync Service Core
- **LCS-DES-076-KG-f**: Doc-to-Graph Sync
- **LCS-DES-076-KG-g**: Graph-to-Doc Sync
- **LCS-DES-076-KG-h**: Conflict Resolver
- **LCS-DES-076-KG-i**: Sync Status Tracker
- **LCS-DES-076-KG-j**: Sync Event Publisher
- **LCS-DES-072-KG-e**: Knowledge Context Strategy (dependency)
- **IEntityExtractionPipeline**: v0.4.5g (dependency)
- **IClaimExtractionService**: v0.5.6-KG (dependency)
- **IGraphRepository**: v0.4.5e (dependency)
- **IDocumentRepository**: v0.4.1c (dependency)

---

## Appendix: Quick Reference

### Module Locations

```
Lexichord.KnowledgeGraph/
  Sync/
    Core/
      ISyncService.cs
      SyncService.cs
      SyncOrchestrator.cs
    DocToGraph/
      IDocumentToGraphSyncProvider.cs
      DocumentToGraphSyncProvider.cs
      ExtractionTransformer.cs
      ExtractionValidator.cs
    GraphToDoc/
      IGraphToDocumentSyncProvider.cs
      GraphToDocumentSyncProvider.cs
      AffectedDocumentDetector.cs
      DocumentFlagger.cs
    Conflict/
      IConflictDetector.cs
      IConflictResolver.cs
      ConflictDetector.cs
      ConflictResolver.cs
      ConflictMerger.cs
    Status/
      ISyncStatusTracker.cs
      SyncStatusTracker.cs
      SyncStatusRepository.cs
    Events/
      ISyncEventPublisher.cs
      SyncEventPublisher.cs
      SyncEvents.cs
      SyncEventHandlers.cs
```

### Key Dependencies

```csharp
// Core
using Lexichord.KnowledgeGraph.Sync.Core;

// Document-to-Graph
using Lexichord.KnowledgeGraph.Sync.DocToGraph;

// Graph-to-Document
using Lexichord.KnowledgeGraph.Sync.GraphToDoc;

// Conflict Resolution
using Lexichord.KnowledgeGraph.Sync.Conflict;

// Status Tracking
using Lexichord.KnowledgeGraph.Sync.Status;

// Event Publishing
using Lexichord.KnowledgeGraph.Sync.Events;
```

### Typical Usage

```csharp
// Sync document to graph
var result = await syncService.SyncDocumentToGraphAsync(
    document,
    new SyncContext { UserId = userId },
    cancellationToken);

// Get sync status
var status = await syncService.GetSyncStatusAsync(
    documentId,
    cancellationToken);

// Resolve conflicts
var resolved = await syncService.ResolveConflictAsync(
    documentId,
    ConflictResolutionStrategy.Merge,
    cancellationToken);

// Publish event (internal)
await eventPublisher.PublishAsync(
    new SyncCompletedEvent { ... },
    cancellationToken: cancellationToken);
```

---

**Document prepared by:** Lead Architect
**Date:** 2026-01-31
**Status:** Draft - Ready for Implementation Planning
