# Changelog: v0.7.6g — Graph-to-Doc Sync

**Feature ID:** KG-076g
**Version:** 0.7.6g
**Date:** 2026-02-19
**Status:** ✅ Complete

---

## Overview

Implements the Graph-to-Document Synchronization module as part of CKVS Phase 4c. This module is the reverse direction of v0.7.6f (Doc-to-Graph Sync) and completes the bidirectional sync architecture. When entities or relationships change in the knowledge graph, this module identifies affected documents and flags them for user review.

The implementation adds `IGraphToDocumentSyncProvider` interface with methods for handling graph changes, retrieving affected documents, managing flags, and subscribing to graph changes; `IAffectedDocumentDetector` interface for document detection based on graph changes; `IDocumentFlagger` interface for flag creation and management; supporting data types including `GraphToDocSyncResult`, `GraphToDocSyncOptions`, `AffectedDocument`, `DocumentFlag`, `DocumentFlagOptions`, `SuggestedAction`, and `GraphChangeSubscription` records; the `DocumentEntityRelationship`, `FlagReason`, `FlagPriority`, `FlagStatus`, `FlagResolution`, and `ActionType` enums; `GraphToDocSyncCompletedEvent` and `DocumentFlaggedEvent` MediatR notifications; `GraphToDocumentSyncProvider`, `AffectedDocumentDetector`, `DocumentFlagger`, and `DocumentFlagStore` service implementations; DI registration in `KnowledgeModule`; and feature code `FeatureCodes.GraphToDocSync`.

---

## What's New

### Enums

#### DocumentEntityRelationship

How a document references a knowledge entity:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Values:**
  - `ExplicitReference` (0) — Document directly mentions the entity
  - `ImplicitReference` (1) — Document contains related content
  - `DerivedFrom` (2) — Entity was extracted from this document
  - `IndirectReference` (3) — Document references via another entity

#### FlagReason

Why a document was flagged for review:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Values:**
  - `EntityValueChanged` (0) — Entity's primary value was modified
  - `EntityPropertiesUpdated` (1) — Entity metadata was changed
  - `EntityDeleted` (2) — Entity was removed from graph
  - `NewRelationship` (3) — New relationship involving entity
  - `RelationshipRemoved` (4) — Relationship was deleted
  - `ManualSyncRequested` (5) — User requested manual sync
  - `ConflictDetected` (6) — Conflict between document and graph

#### FlagPriority

Flag urgency level:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Values:**
  - `Low` (0) — Informational, can be addressed later
  - `Medium` (1) — Should be reviewed soon
  - `High` (2) — Requires prompt attention
  - `Critical` (3) — Requires immediate attention

#### FlagStatus

Current flag state:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Values:**
  - `Pending` (0) — Awaiting review
  - `Acknowledged` (1) — User has seen the flag
  - `Resolved` (2) — Flag has been addressed
  - `Dismissed` (3) — Flag was dismissed without action
  - `Escalated` (4) — Flag requires higher-level review

#### FlagResolution

How a flag was resolved:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Values:**
  - `UpdatedWithGraphChanges` (0) — Document updated to match graph
  - `RejectedGraphChanges` (1) — Graph changes were not applied
  - `ManualMerge` (2) — User manually merged changes
  - `Dismissed` (3) — Flag dismissed without changes

#### ActionType

Type of suggested action for document update:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Values:**
  - `UpdateReferences` (0) — Update entity references in document
  - `AddInformation` (1) — Add new information from graph
  - `RemoveInformation` (2) — Remove outdated information
  - `ManualReview` (3) — Requires manual user review

### Records

#### SuggestedAction

Suggested action for updating a document:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Properties:**
  - `ActionType` (required) — Type of suggested action
  - `Description` (required) — Human-readable description
  - `SuggestedText` — Suggested replacement text (nullable)
  - `Confidence` — Confidence score 0.0-1.0 (default 0.5)

#### AffectedDocument

Document affected by a graph change:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Properties:**
  - `DocumentId` (required) — Document identifier
  - `DocumentName` (required) — Document display name
  - `Relationship` (required) — How document relates to entity
  - `ReferenceCount` — Number of references to entity (default 0)
  - `SuggestedAction` — Recommended action (nullable)
  - `LastModifiedAt` — Document last modified timestamp
  - `LastSyncedAt` — Last sync timestamp (nullable)

#### DocumentFlag

Flag marking a document for review:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Properties:**
  - `FlagId` (required) — Unique flag identifier
  - `DocumentId` (required) — Flagged document ID
  - `TriggeringEntityId` (required) — Entity that triggered the flag
  - `Reason` (required) — Why the document was flagged
  - `Description` — Detailed description (default empty)
  - `Priority` — Flag priority (default Medium)
  - `Status` — Current flag status (default Pending)
  - `CreatedAt` (required) — Flag creation timestamp
  - `ResolvedAt` — Resolution timestamp (nullable)
  - `ResolvedBy` — User who resolved flag (nullable)
  - `Resolution` — How flag was resolved (nullable)
  - `NotificationSent` — Whether notification was sent (default false)
  - `NotificationSentAt` — Notification timestamp (nullable)

#### DocumentFlagOptions

Options for flagging a document:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Properties:**
  - `Priority` — Flag priority (default Medium)
  - `TriggeringEntityId` (required) — Entity that triggered flagging
  - `CreatedBy` — User creating the flag (nullable)
  - `IncludeSuggestedActions` — Include action suggestions (default true)
  - `Tags` — Additional tags for categorization
  - `SendNotification` — Send notification (default true)
  - `Context` — Additional context data

#### GraphToDocSyncResult

Result of graph-to-document sync operation:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Properties:**
  - `Status` (required) — Operation status (SyncOperationStatus)
  - `AffectedDocuments` — Documents affected by change
  - `FlagsCreated` — Flags created for review
  - `TotalDocumentsNotified` — Count of notified documents (default 0)
  - `TriggeringChange` (required) — The graph change that triggered sync
  - `Duration` — Operation duration
  - `ErrorMessage` — Error message if failed (nullable)

#### GraphToDocSyncOptions

Configuration options for sync operations:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Properties:**
  - `AutoFlagDocuments` — Automatically flag affected documents (default true)
  - `SendNotifications` — Send notifications to users (default true)
  - `ReasonPriorities` — Priority mapping per FlagReason
  - `BatchSize` — Batch size for processing (default 100)
  - `MaxDocumentsPerChange` — Maximum documents to process (default 1000)
  - `MinActionConfidence` — Minimum confidence for suggestions (default 0.6)
  - `IncludeSuggestedActions` — Include action suggestions (default true)
  - `DeduplicateNotifications` — Deduplicate notifications (default true)
  - `DeduplicationWindow` — Deduplication time window (default 1 hour)
  - `Timeout` — Operation timeout (default 5 minutes)

#### GraphChangeSubscription

Subscription for graph change notifications:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Properties:**
  - `DocumentId` (required) — Subscribing document ID
  - `EntityIds` — Entities to watch
  - `ChangeTypes` — Change types to monitor
  - `NotifyUser` — Notify user of changes (default true)
  - `CreatedAt` — Subscription creation timestamp
  - `IsActive` — Whether subscription is active (default true)

### Interfaces

#### IGraphToDocumentSyncProvider

Main graph-to-document sync provider:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Methods:**
  - `OnGraphChangeAsync(GraphChange, GraphToDocSyncOptions?, CancellationToken)` — Handle graph change
  - `GetAffectedDocumentsAsync(Guid entityId, CancellationToken)` — Get affected documents
  - `GetPendingFlagsAsync(Guid documentId, CancellationToken)` — Get pending flags
  - `ResolveFlagAsync(Guid flagId, FlagResolution, CancellationToken)` — Resolve a flag
  - `SubscribeToGraphChangesAsync(Guid documentId, GraphChangeSubscription, CancellationToken)` — Subscribe to changes

#### IAffectedDocumentDetector

Document detection interface:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Methods:**
  - `DetectAsync(GraphChange, CancellationToken)` — Detect affected documents
  - `DetectBatchAsync(IReadOnlyList<GraphChange>, CancellationToken)` — Batch detection
  - `GetRelationshipAsync(Guid documentId, Guid entityId, CancellationToken)` — Get relationship type

#### IDocumentFlagger

Flag management interface:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc`
- **Methods:**
  - `FlagDocumentAsync(Guid documentId, FlagReason, DocumentFlagOptions, CancellationToken)` — Flag a document
  - `FlagDocumentsAsync(IReadOnlyList<Guid>, FlagReason, DocumentFlagOptions, CancellationToken)` — Flag multiple documents
  - `ResolveFlagAsync(Guid flagId, FlagResolution, CancellationToken)` — Resolve a flag
  - `ResolveFlagsAsync(IReadOnlyList<Guid>, FlagResolution, CancellationToken)` — Resolve multiple flags
  - `GetFlagAsync(Guid flagId, CancellationToken)` — Get flag by ID
  - `GetPendingFlagsAsync(Guid documentId, CancellationToken)` — Get pending flags for document

### Events

#### GraphToDocSyncCompletedEvent

MediatR notification for sync completion:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc.Events`
- **Properties:**
  - `TriggeringChange` (required) — The graph change that triggered sync
  - `Result` (required) — Sync operation result
  - `Timestamp` — Event timestamp (default now)
  - `InitiatedBy` — User who initiated sync (nullable)
- **Methods:**
  - `Create(GraphChange, GraphToDocSyncResult, Guid?)` — Factory method

#### DocumentFlaggedEvent

MediatR notification when a document is flagged:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc.Events`
- **Properties:**
  - `Flag` (required) — The created flag
  - `Timestamp` — Event timestamp (default now)
- **Methods:**
  - `Create(DocumentFlag)` — Factory method

### Services

#### GraphToDocumentSyncProvider

Main sync provider implementation:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.GraphToDoc`
- **Features:**
  - License gating (Teams+ required for full functionality)
  - Affected document detection via IAffectedDocumentDetector
  - Flag creation via IDocumentFlagger
  - Configurable document limit per change
  - Event publishing via MediatR
  - Three-catch error handling pattern (OperationCanceled, Timeout, Exception)

#### AffectedDocumentDetector

Document detection implementation:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.GraphToDoc`
- **Features:**
  - Entity lookup via IGraphRepository
  - Document retrieval via IDocumentRepository
  - Source document detection (DerivedFrom relationship)
  - Batch detection with deduplication
  - Reference counting across multiple changes

#### DocumentFlagger

Flag management implementation:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.GraphToDoc`
- **Features:**
  - Flag creation with priority assignment
  - Flag resolution with timestamp tracking
  - MediatR event publishing for notifications
  - Storage via DocumentFlagStore

#### DocumentFlagStore

In-memory flag storage:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.GraphToDoc`
- **Features:**
  - Thread-safe ConcurrentDictionary storage
  - Query by document ID
  - Pending flag filtering and sorting by priority
  - Update and remove operations

### Constants

#### FeatureCodes.GraphToDocSync

Feature code for graph-to-doc sync license gating:
- **Value:** `"Feature.GraphToDocSync"`
- **Required Tier:** Teams (full functionality)

---

## Dependencies

### Requires

- `IGraphRepository` (v0.4.5e) — Graph CRUD operations
- `IDocumentRepository` (v0.4.1c) — Document CRUD operations
- `ILicenseContext` (v0.0.4c) — License tier checking
- `IMediator` (MediatR) — Event publishing
- `GraphChange` (v0.7.6e) — Graph change record
- `SyncOperationStatus` (v0.7.6e) — Sync status enum
- `ChangeType` (v0.7.6e) — Change type enum
- `KnowledgeEntity` (v0.4.5e) — Graph entity type

### Required By

- Future sync UI components
- Future background sync jobs
- Document review workflows

---

## Technical Notes

### License Gating

| Tier | Graph-to-Doc Sync | Document Flagging | Subscriptions |
|------|-------------------|-------------------|---------------|
| Core | No | No | No |
| WriterPro | No | No | No |
| Teams | Full | Yes | Yes |
| Enterprise | Full | Yes | Yes |

### Flag Priority by Reason

Default priority mapping in `GraphToDocSyncOptions`:

| FlagReason | Default Priority |
|------------|------------------|
| EntityValueChanged | High |
| EntityDeleted | Critical |
| NewRelationship | Medium |

### DI Registration

All services registered as singletons in `KnowledgeModule.RegisterServices()`:
- `DocumentFlagStore` (concrete)
- `AffectedDocumentDetector` → `IAffectedDocumentDetector`
- `DocumentFlagger` → `IDocumentFlagger`
- `GraphToDocumentSyncProvider` → `IGraphToDocumentSyncProvider`

### Thread Safety

- `DocumentFlagStore` uses `ConcurrentDictionary` for thread-safe flag storage
- All async operations support cancellation tokens

---

## File Summary

| Category | Count | Location |
|----------|-------|----------|
| Enums | 6 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/GraphToDoc/` |
| Records | 7 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/GraphToDoc/` |
| Interfaces | 3 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/GraphToDoc/` |
| Events | 2 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/GraphToDoc/Events/` |
| Services | 4 | `src/Lexichord.Modules.Knowledge/Sync/GraphToDoc/` |
| Tests | 5 | `tests/Lexichord.Tests.Unit/Modules/Knowledge/Sync/GraphToDoc/` |
| **Total** | **27** | |

---

## Testing

Unit tests verify:
- License gating for all tiers (Core/WriterPro blocked, Teams+ allowed)
- Sync result handling for success and no-changes scenarios
- Affected document detection from entity source documents
- Batch detection with deduplication
- Document flagging and notification publishing
- Flag resolution with status updates
- Priority-based flag ordering
- Flag store thread safety
- Record initialization and defaults
- Enum value coverage
- Event factory creation

All 75 tests pass with `[Trait("SubPart", "v0.7.6g")]`.
