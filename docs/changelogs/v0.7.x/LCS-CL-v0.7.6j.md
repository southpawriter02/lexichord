# Changelog: v0.7.6j — Sync Event Publisher

**Feature ID:** KG-076j
**Version:** 0.7.6j
**Date:** 2026-02-19
**Status:** ✅ Complete

---

## Overview

Implements the Sync Event Publisher module as part of CKVS Phase 4c. This module provides centralized event publishing infrastructure for the Knowledge Synchronization system, enabling unified event publication, event history tracking, subscription management, and license-gated access.

The implementation adds `ISyncEvent` base interface extending `INotification` for type-safe event handling; `EventPriority` enum for publication ordering; `EventSortOrder` enum for query result ordering; `SyncEventRecord` for audit trail persistence; `SyncEventOptions` for publication configuration; `SyncEventSubscriptionOptions` for dynamic subscription management; `SyncEventQuery` for filtering and paginating event history; six new sync event types (`SyncConflictDetectedEvent`, `SyncConflictResolvedEvent`, `SyncStatusChangedEvent`, `SyncFailedEvent`, `SyncRetryEvent`, `GraphToDocumentSyncedEvent`); modification of existing events to implement `ISyncEvent`; `IEventStore` interface for event persistence; `ISyncEventPublisher` interface for unified publishing; `SyncEventStore` in-memory implementation; `SyncEventPublisher` implementation with MediatR integration; DI registration updates in `KnowledgeModule`; and feature code `FeatureCodes.SyncEventPublisher`.

---

## What's New

### Enums

#### EventPriority

Priority levels for sync event publication ordering:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Values:**
  - `Low` (0) — Background, non-critical events
  - `Normal` (1) — Standard priority (default)
  - `High` (2) — Important events requiring prompt handling
  - `Critical` (3) — Urgent events requiring immediate attention

#### EventSortOrder

Sort order options for sync event query results:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Values:**
  - `ByPublishedAscending` (0) — Oldest events first
  - `ByPublishedDescending` (1) — Newest events first (default)
  - `ByDocumentAscending` (2) — Sorted by document ID A-Z
  - `ByDocumentDescending` (3) — Sorted by document ID Z-A
  - `ByEventTypeAscending` (4) — Sorted by event type name

### Interfaces

#### ISyncEvent

Base interface for all sync events enabling unified handling:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Extends:** `INotification` (MediatR)
- **Properties:**
  - `EventId` — Unique event identifier
  - `PublishedAt` — Event publication timestamp
  - `DocumentId` — Associated document ID
  - `Metadata` — Extensible key-value pairs

#### IEventStore

Repository interface for sync event persistence and retrieval:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Methods:**
  - `StoreAsync(SyncEventRecord, CancellationToken)` — Persist event record
  - `GetAsync(Guid, CancellationToken)` — Get event by ID
  - `QueryAsync(SyncEventQuery, CancellationToken)` — Query with filters
  - `GetByDocumentAsync(Guid, int, CancellationToken)` — Get events for document

#### ISyncEventPublisher

Unified interface for sync event publishing:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Methods:**
  - `PublishAsync<TEvent>(TEvent, SyncEventOptions?, CancellationToken)` — Publish single event
  - `PublishBatchAsync<TEvent>(IReadOnlyList<TEvent>, SyncEventOptions?, CancellationToken)` — Batch publish
  - `SubscribeAsync<TEvent>(Func<TEvent, CancellationToken, Task>, SyncEventSubscriptionOptions?, CancellationToken)` — Dynamic subscription
  - `UnsubscribeAsync<TEvent>(Guid, CancellationToken)` — Remove subscription
  - `GetEventsAsync(SyncEventQuery, CancellationToken)` — Query event history

### Records

#### SyncEventRecord

Audit record for persisted sync events:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Properties:**
  - `EventId` (required) — Unique event identifier
  - `EventType` (required) — Event type name
  - `DocumentId` (required) — Associated document ID
  - `PublishedAt` (required) — Publication timestamp
  - `Payload` (required) — Serialized event data
  - `HandlersExecuted` — Count of executed handlers
  - `HandlersFailed` — Count of failed handlers
  - `TotalDuration` — Total handling duration
  - `AllHandlersSucceeded` — Whether all handlers succeeded
  - `HandlerErrors` — List of error messages

#### SyncEventOptions

Publication options for sync events:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Properties:**
  - `StoreInHistory` — Store in event history (default true)
  - `AwaitAll` — Await all handlers (default true)
  - `CatchHandlerExceptions` — Catch handler errors (default true)
  - `HandlerTimeout` — Per-handler timeout (nullable)
  - `Priority` — Event priority (default Normal)
  - `AllowBatching` — Allow batching with other events (default false)
  - `Tags` — Event classification tags
  - `ContextData` — Additional context key-value pairs
- **Static Properties:**
  - `Default` — Singleton default options instance

#### SyncEventSubscriptionOptions

Configuration for dynamic event subscriptions:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Properties:**
  - `EventType` (required) — Type of events to subscribe to
  - `Filter` — Optional predicate for filtering events
  - `IsActive` — Whether subscription is active (default true)
  - `CreatedAt` — Subscription creation timestamp
  - `Name` — Optional subscription name

#### SyncEventQuery

Query criteria for event history retrieval:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Properties:**
  - `EventType` — Filter by event type name (nullable)
  - `DocumentId` — Filter by document ID (nullable)
  - `PublishedAfter` — Lower bound timestamp (nullable)
  - `PublishedBefore` — Upper bound timestamp (nullable)
  - `SuccessfulOnly` — Filter for successful events (nullable)
  - `SortOrder` — Result ordering (default ByPublishedDescending)
  - `PageSize` — Results per page (default 100)
  - `PageOffset` — Pagination offset (default 0)

### Event Types

#### SyncConflictDetectedEvent

Event published when sync conflicts are detected:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Implements:** `ISyncEvent`
- **Properties:**
  - `EventId`, `DocumentId`, `Timestamp`, `Metadata` — ISyncEvent properties
  - `Conflicts` (required) — List of detected conflicts
  - `ConflictCount` — Total number of conflicts
  - `SuggestedStrategy` — Recommended resolution strategy
- **Methods:**
  - `Create(Guid, IReadOnlyList<SyncConflict>, ConflictResolutionStrategy?)` — Factory method

#### SyncConflictResolvedEvent

Event published when a sync conflict is resolved:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Implements:** `ISyncEvent`
- **Properties:**
  - `EventId`, `DocumentId`, `Timestamp`, `Metadata` — ISyncEvent properties
  - `Conflict` (required) — The resolved conflict
  - `Strategy` (required) — Resolution strategy used
  - `ResolvedValue` — Final resolved value
  - `ResolvedBy` — User who resolved (nullable for auto)
- **Methods:**
  - `Create(Guid, SyncConflict, ConflictResolutionStrategy, string?, Guid?)` — Factory method

#### SyncStatusChangedEvent

Event published when a document's sync status changes state:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Implements:** `ISyncEvent`
- **Properties:**
  - `EventId`, `DocumentId`, `Timestamp`, `Metadata` — ISyncEvent properties
  - `PreviousState` (required) — State before transition
  - `NewState` (required) — State after transition
  - `Reason` — Human-readable explanation
  - `ChangedBy` — User who initiated (nullable)
- **Methods:**
  - `Create(Guid, SyncState, SyncState, string?, Guid?)` — Factory method

#### SyncFailedEvent

Event published when a sync operation fails:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Implements:** `ISyncEvent`
- **Properties:**
  - `EventId`, `DocumentId`, `Timestamp`, `Metadata` — ISyncEvent properties
  - `ErrorMessage` (required) — Error description
  - `FailedDirection` (required) — Direction of failed sync
  - `ErrorCode` — Machine-readable error code
  - `ExceptionDetails` — Exception stack trace
  - `RetryRecommended` — Whether retry is recommended
- **Methods:**
  - `Create(Guid, string, SyncDirection, string?, string?, bool)` — Factory method

#### SyncRetryEvent

Event published when a sync operation is being retried:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Implements:** `ISyncEvent`
- **Properties:**
  - `EventId`, `DocumentId`, `Timestamp`, `Metadata` — ISyncEvent properties
  - `AttemptNumber` — Current retry attempt
  - `MaxAttempts` — Maximum retry attempts
  - `RetryDelay` — Delay before retry
  - `FailureReason` — Reason for previous failure
- **Methods:**
  - `Create(Guid, int, int, TimeSpan, string)` — Factory method

#### GraphToDocumentSyncedEvent

Event published when graph changes are synced to a document:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **Implements:** `ISyncEvent`
- **Properties:**
  - `EventId`, `DocumentId`, `Timestamp`, `Metadata` — ISyncEvent properties
  - `TriggeringChange` (required) — Graph change that triggered sync
  - `AffectedDocuments` — List of affected document IDs
  - `FlagsCreated` — Review flags created
  - `TotalAffectedDocuments` — Count of affected documents
- **Methods:**
  - `Create(Guid, GraphChange)` — Factory method

### Modified Events

#### SyncCompletedEvent (Enhanced)

Enhanced to implement `ISyncEvent`:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events`
- **New Properties:**
  - `EventId` — Unique event identifier
  - `Metadata` — Extensible key-value pairs
- **Interface Mapping:**
  - `ISyncEvent.PublishedAt` → `Timestamp`
- **Backward Compatible:** Existing properties unchanged

#### DocumentFlaggedEvent (Enhanced)

Enhanced to implement `ISyncEvent`:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc.Events`
- **New Properties:**
  - `EventId` — Unique event identifier
  - `PublishedAt` — Publication timestamp
  - `Metadata` — Extensible key-value pairs
- **Backward Compatible:** Existing properties unchanged

### Services

#### SyncEventStore

In-memory sync event store implementation:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.Events`
- **Features:**
  - Thread-safe ConcurrentDictionary storage
  - Secondary index by document ID
  - Query filtering by all SyncEventQuery properties
  - Sorting by all EventSortOrder options
  - Pagination with configurable page size (max 1000)
  - Event count and document count utilities

#### SyncEventPublisher

Central sync event publisher implementation:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.Events`
- **Features:**
  - MediatR integration for handler invocation
  - Automatic event history storage when enabled
  - Configurable exception handling
  - Cancellation token support
  - License gating by tier
  - Batch publishing with deduplication
  - Dynamic subscription management
  - Tier-based event retention enforcement

### Constants

#### FeatureCodes.SyncEventPublisher

Feature code for sync event publisher license gating:
- **Value:** `"Feature.SyncEventPublisher"`
- **Required Tiers:**
  - Core: No access
  - WriterPro: Publish events, 7-day history
  - Teams: Full access, 30-day history, subscriptions, batching
  - Enterprise: Unlimited history, advanced features

---

## Dependencies

### Requires

- `SyncConflict`, `ConflictResolutionStrategy`, `ConflictType` (v0.7.6h) — Conflict types
- `GraphChange`, `ChangeType` (v0.7.6g) — Graph change types
- `SyncState`, `SyncDirection`, `SyncOperationStatus`, `SyncResult` (v0.7.6e) — Base sync types
- `ILicenseContext` (v0.0.4c) — License tier checking
- `IMediator` (MediatR) — Event publishing
- `INotification` (MediatR) — Event base interface

### Required By

- Future sync UI components (event viewer, conflict dashboard)
- Future event-driven sync scheduling
- Future alerting and notification systems
- Future event replay and audit functionality

---

## Technical Notes

### License Gating

| Tier | Publish | Subscribe | History | Batch |
|------|---------|-----------|---------|-------|
| Core | No | No | N/A | No |
| WriterPro | Yes | No | 7 days | No |
| Teams | Yes | Yes | 30 days | Yes |
| Enterprise | Yes | Yes | Unlimited | Yes |

### History Retention Limits

- WriterPro: 7-day rolling window
- Teams: 30-day rolling window
- Enterprise: Unlimited (uses requested parameters)

### DI Registration

All services registered as singletons in `KnowledgeModule.RegisterServices()`:
- `SyncEventStore` → `IEventStore`
- `SyncEventPublisher` → `ISyncEventPublisher` (depends on IMediator, IEventStore, ILicenseContext)

### Thread Safety

- `SyncEventStore` uses `ConcurrentDictionary` for thread-safe storage
- `SyncEventPublisher` subscription management uses `ConcurrentDictionary`
- All async operations support cancellation tokens

### Performance Targets

| Operation | Target |
|-----------|--------|
| Event publication | < 50ms |
| Handler invocation | < 200ms per handler |
| Batch publication (100 events) | < 500ms |
| Event query (1000 events) | < 300ms |

---

## File Summary

| Category | Count | Location |
|----------|-------|----------|
| Enums | 2 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/Events/` |
| Interfaces | 3 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/Events/` |
| Records | 4 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/Events/` |
| New Events | 6 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/Events/` |
| Modified Events | 2 | `SyncCompletedEvent.cs`, `DocumentFlaggedEvent.cs` |
| Services | 2 | `src/Lexichord.Modules.Knowledge/Sync/Events/` |
| Constants | 1 | `FeatureCodes.cs` |
| Tests | 3 | `tests/Lexichord.Tests.Unit/Modules/Knowledge/Sync/Events/` |
| **Total** | **23** | |

---

## Testing

Unit tests verify:
- Event record initialization and required properties
- Event record default values (HandlersExecuted, HandlersFailed, etc.)
- Event options default values (StoreInHistory, AwaitAll, etc.)
- Event options singleton Default property
- Subscription options required and default properties
- Event query default values (PageSize, SortOrder, etc.)
- ISyncEvent implementation on all event types
- Event factory method property assignment
- ISyncEvent.PublishedAt mapping to Timestamp
- EventPriority enum value coverage
- EventSortOrder enum value coverage
- Event store CRUD operations (Store, Get, Query)
- Event store query filtering by all criteria
- Event store query sorting by all sort orders
- Event store query pagination
- Event store GetByDocument with limits
- Event store utility methods (GetEventCount, GetDocumentCount)
- Publisher MediatR invocation
- Publisher event history storage when enabled
- Publisher exception handling (catch enabled vs disabled)
- Publisher cancellation token support
- Publisher license gating (Core blocked, WriterPro+ allowed)
- Publisher batch operations with tier restrictions
- Publisher subscription and unsubscription
- Publisher event query with tier-based retention

All 62 tests pass with `[Trait("SubPart", "v0.7.6j")]`.
