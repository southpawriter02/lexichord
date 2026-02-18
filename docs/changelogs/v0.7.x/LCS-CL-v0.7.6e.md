# Changelog: v0.7.6e — Sync Service Core

**Feature ID:** KG-076e
**Version:** 0.7.6e
**Date:** 2026-02-18
**Status:** ✅ Complete

---

## Overview

Implements the Sync Service Core for bidirectional synchronization between documents and the Knowledge Graph as part of CKVS Phase 4c. The Sync Service orchestrates the sync pipeline, coordinates entity extraction, detects and resolves conflicts, tracks sync status, and publishes completion events. This is the fifth sub-part of v0.7.6 "The Summarizer Agent."

The implementation adds `ISyncService` interface with methods for document-to-graph sync, affected document discovery, status queries, conflict resolution, and sync necessity checks; `ISyncOrchestrator` interface for internal pipeline coordination; `ISyncStatusTracker`, `ISyncConflictDetector`, and `IConflictResolver` support interfaces; data types including `SyncResult`, `SyncStatus`, `SyncConflict`, `SyncContext`, and `GraphChange` records; enums `SyncOperationStatus`, `SyncState`, `ConflictType`, `ConflictSeverity`, `ConflictResolutionStrategy`, `ChangeType`, and `SyncDirection`; `SyncCompletedEvent` MediatR notification with factory method; `SyncService` and `SyncOrchestrator` implementations with license gating; support services `SyncStatusTracker`, `SyncConflictDetector`, and `ConflictResolver`; DI registration in `KnowledgeModule`; and feature code `FeatureCodes.SyncService`.

---

## What's New

### Enums

#### SyncOperationStatus

Sync operation outcome status:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Values:**
  - `Success` (0) — Sync completed successfully
  - `SuccessWithConflicts` (1) — Sync completed with conflicts detected
  - `PartialSuccess` (2) — Some items synced, others failed
  - `Failed` (3) — Sync operation failed
  - `NoChanges` (4) — No changes detected

#### SyncState

Document synchronization state:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Values:**
  - `InSync` (0) — Document and graph are synchronized
  - `PendingSync` (1) — Document has pending changes
  - `NeedsReview` (2) — Needs manual review
  - `Conflict` (3) — Conflict exists
  - `NeverSynced` (4) — Document never synchronized

#### ConflictType

Type of sync conflict:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Values:**
  - `ValueMismatch` (0) — Value differs between sources
  - `MissingInGraph` (1) — Entity in document but not graph
  - `MissingInDocument` (2) — Entity in graph but not document
  - `RelationshipMismatch` (3) — Relationship differs
  - `ConcurrentEdit` (4) — Concurrent modifications detected

#### ConflictSeverity

Conflict severity level:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Values:**
  - `Low` (0) — Can be auto-resolved
  - `Medium` (1) — Should be reviewed
  - `High` (2) — Requires manual intervention

#### ConflictResolutionStrategy

Resolution strategy:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Values:**
  - `UseDocument` (0) — Document value wins
  - `UseGraph` (1) — Graph value wins
  - `Manual` (2) — Require user choice
  - `Merge` (3) — Attempt intelligent merge
  - `DiscardDocument` (4) — Discard document changes
  - `DiscardGraph` (5) — Discard graph changes

#### ChangeType

Graph change type:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Values:**
  - `EntityCreated` (0) — New entity added
  - `EntityUpdated` (1) — Existing entity modified
  - `EntityDeleted` (2) — Entity removed
  - `RelationshipCreated` (3) — New relationship added
  - `RelationshipDeleted` (4) — Relationship removed
  - `PropertyChanged` (5) — Property modified

#### SyncDirection

Sync direction:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Values:**
  - `DocumentToGraph` (0) — Push document changes to graph
  - `GraphToDocument` (1) — Propagate graph changes to documents
  - `Bidirectional` (2) — Full two-way sync

### Records

#### SyncResult

Sync operation result:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Properties:**
  - `Status` (required) — Operation status
  - `EntitiesAffected` — Affected entities (default empty)
  - `ClaimsAffected` — Affected claims (default empty)
  - `RelationshipsAffected` — Affected relationships (default empty)
  - `Conflicts` — Detected conflicts (default empty)
  - `Duration` — Operation duration
  - `ErrorMessage` — Error details (nullable)
  - `CompletedAt` — Completion timestamp

#### SyncStatus

Document sync status:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Properties:**
  - `DocumentId` (required) — Document identifier
  - `State` (required) — Current sync state
  - `LastSyncAt` — Last successful sync (nullable)
  - `PendingChanges` — Count of pending changes
  - `LastAttemptAt` — Last attempt timestamp (nullable)
  - `LastError` — Last error message (nullable)
  - `UnresolvedConflicts` — Count of unresolved conflicts
  - `IsSyncInProgress` — Whether sync is running

#### SyncConflict

Sync conflict record:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Properties:**
  - `ConflictTarget` (required) — Entity/field in conflict
  - `DocumentValue` (required) — Value from document
  - `GraphValue` (required) — Value from graph
  - `DetectedAt` (required) — Detection timestamp
  - `Type` (required) — Conflict type
  - `Severity` — Conflict severity (default Medium)
  - `Description` — Human-readable description (nullable)

#### SyncContext

Sync operation context:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Properties:**
  - `UserId` (required) — Initiating user
  - `Document` (required) — Document to sync
  - `WorkspaceId` — Workspace identifier (nullable)
  - `AutoResolveConflicts` — Auto-resolve low severity (default true)
  - `DefaultConflictStrategy` — Default strategy (default Merge)
  - `PublishEvents` — Publish completion events (default true)
  - `Timeout` — Operation timeout (default 5 minutes)

#### GraphChange

Graph change record:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Properties:**
  - `EntityId` (required) — Changed entity
  - `ChangeType` (required) — Type of change
  - `PreviousValue` — Old value (nullable)
  - `NewValue` (required) — New value
  - `ChangedBy` — User who made change (nullable)
  - `ChangedAt` (required) — Change timestamp

### Interfaces

#### ISyncService

Main sync service interface:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Methods:**
  - `SyncDocumentToGraphAsync(Document, SyncContext, CancellationToken)` — Sync document to graph
  - `GetAffectedDocumentsAsync(GraphChange, CancellationToken)` — Find affected documents
  - `GetSyncStatusAsync(Guid, CancellationToken)` — Get document sync status
  - `ResolveConflictAsync(Guid, ConflictResolutionStrategy, CancellationToken)` — Resolve conflicts
  - `NeedsSyncAsync(Guid, CancellationToken)` — Check if sync needed

#### ISyncOrchestrator

Internal pipeline orchestrator:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Methods:**
  - `ExecuteDocumentToGraphAsync(Document, SyncContext, CancellationToken)` — Execute doc-to-graph
  - `ExecuteGraphToDocumentAsync(GraphChange, CancellationToken)` — Execute graph-to-doc
  - `DetectConflictsAsync(Document, ExtractionResult, CancellationToken)` — Detect conflicts

#### ISyncStatusTracker

Status tracking interface:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Methods:**
  - `GetStatusAsync(Guid, CancellationToken)` — Get document status
  - `UpdateStatusAsync(Guid, SyncStatus, CancellationToken)` — Update document status

#### ISyncConflictDetector

Conflict detection interface:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Methods:**
  - `DetectAsync(Document, ExtractionResult, CancellationToken)` — Detect conflicts

#### IConflictResolver

Conflict resolution interface:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync`
- **Methods:**
  - `ResolveAsync(Guid, ConflictResolutionStrategy, CancellationToken)` — Resolve conflicts

### Events

#### SyncCompletedEvent

MediatR notification for sync completion:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Properties:**
  - `DocumentId` (required) — Synced document
  - `Result` (required) — Sync result
  - `Direction` (required) — Sync direction
  - `Timestamp` — Event timestamp (default now)
- **Methods:**
  - `Create(Guid, SyncResult, SyncDirection)` — Factory method

### Services

#### SyncService

Main sync service implementation:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.Core`
- **Features:**
  - License gating (Core: none, WriterPro: doc-to-graph, Teams+: full)
  - Status tracking integration
  - Event publishing via MediatR
  - Three-catch error handling pattern

#### SyncOrchestrator

Pipeline orchestration implementation:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.Core`
- **Features:**
  - Entity extraction via IEntityExtractionPipeline
  - Conflict detection via ISyncConflictDetector
  - Auto-resolution of low-severity conflicts
  - Graph upsert coordination

#### SyncStatusTracker

Status tracking implementation:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync`
- **Features:**
  - Thread-safe ConcurrentDictionary storage
  - Default NeverSynced state for new documents
  - Atomic status updates

#### SyncConflictDetector

Conflict detection implementation:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync`
- **Features:**
  - Value mismatch detection
  - Missing entity detection (both directions)
  - Property comparison

#### ConflictResolver

Resolution implementation:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync`
- **Features:**
  - All resolution strategies supported
  - In-memory pending conflict storage
  - Merge fallback to document value

### Constants

#### FeatureCodes.SyncService

Feature code for sync license gating:
- **Value:** `"Feature.SyncService"`
- **Required Tier:** WriterPro (doc-to-graph), Teams (full bidirectional)

---

## Dependencies

### Requires

- `IEntityExtractionPipeline` (v0.4.5g) — Entity extraction
- `IGraphRepository` (v0.4.5e) — Graph CRUD operations
- `ILicenseContext` (v0.0.4c) — License tier checking
- `IMediator` (MediatR) — Event publishing

### Required By

- Future UI components for sync status display
- Future background sync jobs

---

## Technical Notes

### License Gating

| Tier | DocumentToGraph | GraphToDocument | Conflict Resolution |
|------|-----------------|-----------------|---------------------|
| Core | No | No | N/A |
| WriterPro | Manual only | No | Manual |
| Teams | Full | Full | All strategies |
| Enterprise | Full | Full | All + custom |

### DI Registration

All services registered as singletons in `KnowledgeModule.RegisterServices()`:
- `SyncStatusTracker` → `ISyncStatusTracker`
- `SyncConflictDetector` → `ISyncConflictDetector`
- `ConflictResolver` → `IConflictResolver`
- `SyncOrchestrator` → `ISyncOrchestrator`
- `SyncService` → `ISyncService`

### Thread Safety

- `SyncStatusTracker` uses `ConcurrentDictionary` for thread-safe status storage
- `ConflictResolver` uses lock for pending conflict access

---

## File Summary

| Category | Count | Location |
|----------|-------|----------|
| Enums | 7 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/` |
| Records | 5 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/` |
| Interfaces | 5 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/` |
| Events | 1 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/Events/` |
| Services | 5 | `src/Lexichord.Modules.Knowledge/Sync/` |
| Tests | 3 | `tests/Lexichord.Tests.Unit/Modules/Knowledge/Sync/` |
| **Total** | **26** | |

---

## Testing

Unit tests verify:
- License gating for all tiers
- Status tracking state transitions
- Sync result handling
- Conflict detection and resolution
- Record initialization and defaults
- Event factory creation
