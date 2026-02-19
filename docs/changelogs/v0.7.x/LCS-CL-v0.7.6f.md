# Changelog: v0.7.6f — Doc-to-Graph Sync

**Feature ID:** KG-076f
**Version:** 0.7.6f
**Date:** 2026-02-18
**Status:** ✅ Complete

---

## Overview

Implements the Document-to-Graph Synchronization pipeline as part of CKVS Phase 4c. This module builds upon the Sync Service Core (v0.7.6e) to provide unidirectional synchronization from documents to the knowledge graph. The pipeline orchestrates document content extraction, entity transformation, schema validation, graph upsert, and lineage tracking for rollback capability.

The implementation adds `IDocumentToGraphSyncProvider` interface with methods for document sync, extraction validation, lineage retrieval, and rollback; `IExtractionTransformer` interface for data transformation from extraction results to graph format; `IExtractionValidator` interface for schema compliance validation; supporting data types including `DocToGraphSyncOptions`, `DocToGraphSyncResult`, `ExtractionRecord`, `GraphIngestionData`, `ValidationResult`, `ValidationError`, `ValidationWarning`, `EntityValidationResult`, `RelationshipValidationResult`, and `DocToGraphValidationContext` records; the `ValidationSeverity` enum; `DocToGraphSyncCompletedEvent` MediatR notification; `DocumentToGraphSyncProvider`, `ExtractionTransformer`, `ExtractionValidator`, and `ExtractionLineageStore` service implementations; DI registration in `KnowledgeModule`; and feature code `FeatureCodes.DocToGraphSync`.

---

## What's New

### Enums

#### ValidationSeverity

Validation error/warning severity level:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph`
- **Values:**
  - `Warning` (0) — Non-blocking issue, may need review
  - `Error` (1) — Blocking issue, extraction fails
  - `Critical` (2) — Severe issue, requires immediate attention

### Records

#### DocToGraphSyncOptions

Configuration options for sync operations:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph`
- **Properties:**
  - `ValidateBeforeUpsert` — Validate extraction before graph write (default true)
  - `AutoCorrectErrors` — Auto-correct validation issues (default false)
  - `PreserveLineage` — Track extraction history for rollback (default true)
  - `MaxEntities` — Maximum entities to extract (default 1000)
  - `CreateRelationships` — Derive relationships from co-occurrence (default true)
  - `ExtractClaims` — Extract claims from document (default true)
  - `EnrichWithGraphContext` — Enrich with existing graph data (default true)
  - `Timeout` — Operation timeout (default 10 minutes)

#### DocToGraphSyncResult

Sync operation result:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph`
- **Properties:**
  - `Status` (required) — Operation status (SyncOperationStatus)
  - `UpsertedEntities` — Knowledge entities upserted to graph
  - `CreatedRelationships` — Relationships created between entities
  - `ExtractedClaims` — Claims extracted from document
  - `ValidationErrors` — Validation errors encountered
  - `ExtractionRecord` — Lineage record for rollback (nullable)
  - `Duration` — Operation duration
  - `TotalEntitiesAffected` — Count of affected entities
  - `Message` — Summary message (nullable)

#### ExtractionRecord

Extraction lineage record:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph`
- **Properties:**
  - `ExtractionId` (required) — Unique extraction identifier
  - `DocumentId` (required) — Source document ID
  - `DocumentHash` (required) — Document content hash at extraction
  - `ExtractedAt` (required) — Extraction timestamp
  - `ExtractedBy` — User who initiated extraction (nullable)
  - `EntityIds` (required) — IDs of extracted entities
  - `ClaimIds` (required) — IDs of extracted claims
  - `RelationshipIds` (required) — IDs of created relationships
  - `ExtractionHash` (required) — Hash of extraction output

#### GraphIngestionData

Data structured for graph ingestion:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph`
- **Properties:**
  - `Entities` (required) — Knowledge entities to upsert
  - `Relationships` (required) — Relationships to create
  - `Claims` (required) — Claims to store
  - `SourceDocumentId` (required) — Source document identifier
  - `Metadata` (required) — Additional ingestion metadata

#### ValidationResult

Extraction validation result:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph`
- **Properties:**
  - `IsValid` (required) — Whether validation passed
  - `Errors` (required) — List of validation errors
  - `Warnings` (required) — List of validation warnings
  - `EntitiesValidated` (required) — Count of validated entities
  - `RelationshipsValidated` (required) — Count of validated relationships
- **Methods:**
  - `Success(int, int)` — Create success result factory

#### ValidationError

Validation error details:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph`
- **Properties:**
  - `Code` (required) — Error code (e.g., "VAL-001")
  - `Message` (required) — Human-readable description
  - `EntityId` — Related entity ID (nullable)
  - `RelationshipId` — Related relationship ID (nullable)
  - `Severity` (required) — Error severity level

#### ValidationWarning

Validation warning details:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph`
- **Properties:**
  - `Code` (required) — Warning code (e.g., "WARN-001")
  - `Message` (required) — Human-readable description
  - `EntityId` — Related entity ID (nullable)

#### EntityValidationResult

Single entity validation result:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph`
- **Properties:**
  - `EntityId` (required) — Validated entity ID
  - `IsValid` (required) — Whether entity is valid
  - `Errors` (required) — List of validation errors
- **Methods:**
  - `Success(Guid)` — Create success result factory
  - `Failed(Guid, IReadOnlyList<ValidationError>)` — Create failed result factory

#### RelationshipValidationResult

Relationship validation result:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph`
- **Properties:**
  - `AllValid` (required) — Whether all relationships are valid
  - `InvalidRelationships` (required) — List of invalid relationship IDs with errors
- **Methods:**
  - `Success()` — Create success result factory
  - `Failed(IReadOnlyList<(Guid, ValidationError)>)` — Create failed result factory

#### DocToGraphValidationContext

Validation context configuration:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph`
- **Properties:**
  - `DocumentId` (required) — Document being validated
  - `StrictMode` — Enforce strict schema compliance (default false)
  - `AllowedEntityTypes` — Restrict to specific entity types (nullable)

### Interfaces

#### IDocumentToGraphSyncProvider

Main document-to-graph sync provider:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph`
- **Methods:**
  - `SyncAsync(Document, DocToGraphSyncOptions?, CancellationToken)` — Sync document to graph
  - `ValidateExtractionAsync(ExtractionResult, CancellationToken)` — Validate extraction results
  - `GetExtractionLineageAsync(Guid, CancellationToken)` — Get document extraction history
  - `RollbackSyncAsync(Guid, DateTimeOffset, CancellationToken)` — Rollback to previous version

#### IExtractionTransformer

Data transformation interface:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph`
- **Methods:**
  - `TransformAsync(ExtractionResult, Document, CancellationToken)` — Transform extraction to ingestion data
  - `TransformEntitiesAsync(IReadOnlyList<AggregatedEntity>, CancellationToken)` — Transform entities
  - `DeriveRelationshipsAsync(IReadOnlyList<KnowledgeEntity>, ExtractionResult, CancellationToken)` — Derive relationships
  - `EnrichEntitiesAsync(IReadOnlyList<KnowledgeEntity>, CancellationToken)` — Enrich with graph context

#### IExtractionValidator

Schema validation interface:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph`
- **Methods:**
  - `ValidateAsync(ExtractionResult, DocToGraphValidationContext, CancellationToken)` — Validate extraction
  - `ValidateEntityAsync(KnowledgeEntity, CancellationToken)` — Validate single entity
  - `ValidateRelationshipsAsync(IReadOnlyList<KnowledgeRelationship>, IReadOnlyList<KnowledgeEntity>, CancellationToken)` — Validate relationships

### Events

#### DocToGraphSyncCompletedEvent

MediatR notification for sync completion:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph.Events`
- **Properties:**
  - `DocumentId` (required) — Synced document ID
  - `Result` (required) — Sync operation result
  - `Timestamp` — Event timestamp (default now)
  - `InitiatedBy` — User who initiated sync (nullable)
- **Methods:**
  - `Create(Guid, DocToGraphSyncResult, Guid?)` — Factory method

### Services

#### DocumentToGraphSyncProvider

Main sync provider implementation:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.DocToGraph`
- **Features:**
  - License gating (Core: none, WriterPro: basic sync, Teams+: full with enrichment)
  - Entity extraction via IEntityExtractionPipeline
  - Transformation via IExtractionTransformer
  - Validation via IExtractionValidator
  - Graph upsert via IGraphRepository
  - Lineage tracking via ExtractionLineageStore
  - Event publishing via MediatR
  - Three-catch error handling pattern

#### ExtractionTransformer

Data transformation implementation:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.DocToGraph`
- **Features:**
  - AggregatedEntity → KnowledgeEntity mapping
  - Entity type normalization (api→Endpoint, function→Method, class→Component)
  - Relationship derivation from entity co-occurrence
  - Entity enrichment via graph context lookup
  - Metadata preservation from extraction

#### ExtractionValidator

Schema validation implementation:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.DocToGraph`
- **Features:**
  - Known entity type validation (Product, Endpoint, Parameter, etc.)
  - Strict mode for schema enforcement
  - Lenient mode with warnings for unknown types
  - Allowed entity types filtering
  - Low confidence detection
  - Relationship reference validation
  - Self-referential relationship detection

#### ExtractionLineageStore

In-memory lineage storage:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.DocToGraph`
- **Features:**
  - Thread-safe ConcurrentDictionary storage
  - Chronological ordering (most recent first)
  - Point-in-time extraction lookup
  - Extraction hash retrieval for change detection

### Constants

#### FeatureCodes.DocToGraphSync

Feature code for doc-to-graph sync license gating:
- **Value:** `"Feature.DocToGraphSync"`
- **Required Tier:** WriterPro (basic sync), Teams (full with enrichment)

---

## Dependencies

### Requires

- `IEntityExtractionPipeline` (v0.4.5g) — Entity extraction from document text
- `IGraphRepository` (v0.4.5e) — Graph CRUD operations
- `ILicenseContext` (v0.0.4c) — License tier checking
- `IMediator` (MediatR) — Event publishing
- `SyncOperationStatus` (v0.7.6e) — Sync status enum
- `KnowledgeEntity`, `KnowledgeRelationship` (v0.4.5e) — Graph data types

### Required By

- Future sync UI components
- Future background sync jobs
- Graph visualization tools

---

## Technical Notes

### License Gating

| Tier | Doc-to-Graph Sync | Graph Enrichment | Claim Extraction |
|------|-------------------|------------------|------------------|
| Core | No | No | No |
| WriterPro | Basic | No | Yes |
| Teams | Full | Yes | Yes |
| Enterprise | Full | Yes | Yes |

### Validation Codes

| Code | Severity | Description |
|------|----------|-------------|
| VAL-001 | Error | Empty or null entity type |
| VAL-002 | Error | Empty or null entity value |
| VAL-003 | Error/Warning | Unknown entity type (strict/lenient) |
| VAL-004 | Error | Entity type not in allowed list |
| VAL-005 | Error | Low confidence extraction (strict mode) |
| VAL-006 | Error | Empty relationship type |
| VAL-007 | Error | Invalid FromEntityId reference |
| VAL-008 | Error | Invalid ToEntityId reference |
| VAL-009 | Warning | Self-referential relationship |
| WARN-001 | Warning | Unknown entity type (lenient mode) |
| WARN-003 | Warning | Low confidence extraction |

### Entity Type Normalization

| Input | Normalized |
|-------|------------|
| api, apis | Endpoint |
| function, functions | Method |
| class, classes, module, modules | Component |
| var, variable, variables, const, constant, constants | Parameter |

### DI Registration

All services registered as singletons in `KnowledgeModule.RegisterServices()`:
- `ExtractionLineageStore` (concrete)
- `ExtractionTransformer` → `IExtractionTransformer`
- `ExtractionValidator` → `IExtractionValidator`
- `DocumentToGraphSyncProvider` → `IDocumentToGraphSyncProvider`

### Thread Safety

- `ExtractionLineageStore` uses `ConcurrentDictionary` for thread-safe lineage storage

---

## File Summary

| Category | Count | Location |
|----------|-------|----------|
| Enums | 1 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/DocToGraph/` |
| Records | 9 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/DocToGraph/` |
| Interfaces | 3 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/DocToGraph/` |
| Events | 1 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/DocToGraph/Events/` |
| Services | 4 | `src/Lexichord.Modules.Knowledge/Sync/DocToGraph/` |
| Tests | 5 | `tests/Lexichord.Tests.Unit/Modules/Knowledge/Sync/DocToGraph/` |
| **Total** | **23** | |

---

## Testing

Unit tests verify:
- License gating for all tiers (Core blocked, WriterPro+ allowed)
- Sync result handling for success, partial success, and failure
- Entity transformation and type normalization
- Relationship derivation from co-occurrence
- Validation in strict and lenient modes
- Validation error codes and severity
- Lineage storage and retrieval
- Record initialization and defaults
- Event factory creation
