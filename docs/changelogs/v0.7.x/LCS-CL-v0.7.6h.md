# Changelog: v0.7.6h — Conflict Resolver

**Feature ID:** KG-076h
**Version:** 0.7.6h
**Date:** 2026-02-19
**Status:** ✅ Complete

---

## Overview

Implements the Conflict Resolver module as part of CKVS Phase 4c. This module extends the basic conflict detection and resolution capabilities from v0.7.6e with enhanced conflict analysis, intelligent merging strategies, detailed resolution results, and comprehensive entity comparison infrastructure.

The implementation adds `IConflictDetector` interface for enhanced conflict detection with value and structural analysis; `IConflictMerger` interface for intelligent value merging with multiple strategies; `IEntityComparer` interface for property-by-property entity comparison; `EnhancedConflictResolver` for detailed resolution results with license gating and MediatR event publishing; `ConflictStore` for thread-safe conflict storage; supporting data types including `ConflictDetail`, `ConflictResolutionResult`, `ConflictResolutionOptions`, `MergeResult`, `ConflictMergeResult`, `MergeContext`, `PropertyDifference`, and `EntityComparison` records; the `MergeStrategy` and `MergeType` enums; `ConflictResolvedEvent` MediatR notification; DI registration in `KnowledgeModule`; and feature code `FeatureCodes.ConflictResolver`.

---

## What's New

### Enums

#### MergeStrategy

Strategy for merging conflicting values:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict`
- **Values:**
  - `DocumentFirst` (0) — Use the document value
  - `GraphFirst` (1) — Use the graph value
  - `Combine` (2) — Combine both values
  - `MostRecent` (3) — Use the most recently modified value
  - `HighestConfidence` (4) — Use the value with higher confidence
  - `RequiresManualMerge` (5) — Cannot be automatically merged

#### MergeType

Type of merge operation performed:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict`
- **Values:**
  - `Selection` (0) — Simple value selection
  - `Intelligent` (1) — AI-assisted merge
  - `Weighted` (2) — Confidence-weighted merge
  - `Manual` (3) — User-performed merge
  - `Temporal` (4) — Timestamp-based merge

### Records

#### PropertyDifference

Represents a difference in a single property between entities:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict`
- **Properties:**
  - `PropertyName` (required) — Name of the differing property
  - `DocumentValue` — Value from the document (nullable)
  - `GraphValue` — Value from the graph (nullable)
  - `Confidence` — Confidence score 0.0-1.0 (default 0.0)

#### EntityComparison

Result of comparing two entities:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict`
- **Properties:**
  - `DocumentEntity` (required) — Entity from document extraction
  - `GraphEntity` (required) — Entity from the knowledge graph
  - `PropertyDifferences` (required) — List of property differences
- **Computed:**
  - `HasDifferences` — True if any differences exist
  - `DifferenceCount` — Number of differences

#### ConflictDetail

Detailed information about a detected conflict:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict`
- **Properties:**
  - `ConflictId` (required) — Unique conflict identifier
  - `Entity` (required) — The entity involved in the conflict
  - `ConflictField` (required) — Name of the conflicting field
  - `DocumentValue` — Value from the document (nullable)
  - `GraphValue` — Value from the graph (nullable)
  - `Type` (required) — ConflictType (from v0.7.6e)
  - `Severity` (required) — ConflictSeverity (from v0.7.6e)
  - `DetectedAt` (required) — When the conflict was detected
  - `DocumentModifiedAt` — Document modification timestamp (nullable)
  - `GraphModifiedAt` — Graph modification timestamp (nullable)
  - `SuggestedStrategy` (required) — Recommended resolution strategy
  - `ResolutionConfidence` — Confidence in suggested resolution 0.0-1.0

#### MergeContext

Context information for merge operations:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict`
- **Properties:**
  - `Entity` — The entity being merged (nullable)
  - `Document` — The source document (nullable)
  - `ConflictType` (required) — Type of conflict being merged
  - `UserId` — User initiating the merge (nullable)
  - `ContextData` — Additional context data dictionary

#### MergeResult

Result of a merge operation:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict`
- **Properties:**
  - `Success` — Whether merge succeeded
  - `MergedValue` — The merged value (nullable)
  - `UsedStrategy` — Strategy that was used
  - `Confidence` — Confidence in result 0.0-1.0
  - `ErrorMessage` — Error message if failed (nullable)
  - `MergeType` — Type of merge performed
- **Factory Methods:**
  - `DocumentWins(object)` — Create result using document value
  - `GraphWins(object)` — Create result using graph value
  - `RequiresManual(string)` — Create manual-required result
  - `Failed(string)` — Create failure result

#### ConflictMergeResult

Extended merge result with original values:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict`
- **Properties:**
  - All `MergeResult` properties
  - `DocumentValue` — Original document value
  - `GraphValue` — Original graph value
  - `Explanation` — Human-readable explanation (nullable)
- **Factory Methods:**
  - `FromMergeResult(MergeResult, object, object, string?)` — Create from base result

#### ConflictResolutionResult

Detailed result of a conflict resolution:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict`
- **Properties:**
  - `Conflict` (required) — The conflict that was resolved
  - `Strategy` (required) — Strategy used for resolution
  - `Succeeded` — Whether resolution succeeded
  - `ResolvedValue` — The resolved value (nullable)
  - `ErrorMessage` — Error message if failed (nullable)
  - `ResolvedAt` — Resolution timestamp
  - `ResolvedBy` — User who resolved (nullable)
  - `IsAutomatic` — Whether resolved automatically
- **Factory Methods:**
  - `Success(SyncConflict, ConflictResolutionStrategy, object?, bool)` — Create success result
  - `Failure(SyncConflict, ConflictResolutionStrategy, string)` — Create failure result
  - `RequiresManualIntervention(SyncConflict)` — Create manual-required result

#### ConflictResolutionOptions

Configuration options for conflict resolution:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict`
- **Properties:**
  - `DefaultStrategy` — Default resolution strategy (default Manual)
  - `StrategyByType` — Strategy mapping per conflict type
  - `AutoResolveLow` — Auto-resolve low severity (default true)
  - `AutoResolveMedium` — Auto-resolve medium severity (default false)
  - `AutoResolveHigh` — Auto-resolve high severity (default false)
  - `MinMergeConfidence` — Minimum confidence for merge (default 0.7)
  - `PreserveConflictHistory` — Preserve resolution history (default true)
  - `MaxResolutionAttempts` — Maximum retry attempts (default 3)
  - `ResolutionTimeout` — Operation timeout (default 30 seconds)
- **Methods:**
  - `CanAutoResolve(ConflictSeverity)` — Check if severity can be auto-resolved
  - `GetStrategy(ConflictType)` — Get strategy for conflict type
- **Static Properties:**
  - `Default` — Default options instance

### Interfaces

#### IEntityComparer

Service for comparing entities property-by-property:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict`
- **Methods:**
  - `CompareAsync(KnowledgeEntity, KnowledgeEntity, CancellationToken)` — Compare two entities

#### IConflictDetector

Enhanced conflict detection interface:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict`
- **Methods:**
  - `DetectAsync(Document, ExtractionResult, CancellationToken)` — Detect all conflicts for extraction
  - `DetectValueConflictsAsync(IReadOnlyList<KnowledgeEntity>, CancellationToken)` — Detect value conflicts
  - `DetectStructuralConflictsAsync(Document, ExtractionResult, CancellationToken)` — Detect structural conflicts
  - `EntitiesChangedAsync(ExtractionRecord, CancellationToken)` — Check if entities changed since extraction

#### IConflictMerger

Service for intelligently merging conflicted values:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict`
- **Methods:**
  - `MergeAsync(object, object, MergeContext, CancellationToken)` — Merge two values
  - `GetMergeStrategy(ConflictType)` — Get recommended strategy for conflict type

### Events

#### ConflictResolvedEvent

MediatR notification when a conflict is resolved:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict.Events`
- **Properties:**
  - `Conflict` (required) — The resolved conflict
  - `Result` (required) — Resolution result
  - `Timestamp` — Event timestamp
  - `InitiatedBy` — User who initiated resolution (nullable)
- **Methods:**
  - `Create(SyncConflict, ConflictResolutionResult, Guid?)` — Factory method

### Services

#### EntityComparer

Property-by-property entity comparison:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.Conflict`
- **Features:**
  - Standard property comparison (Name, Type, Value, Confidence)
  - Custom property comparison from Properties dictionary
  - Confidence scoring for differences
  - Null value handling
  - Tolerance for small confidence differences (±0.05)

#### ConflictStore

Thread-safe in-memory conflict storage:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.Conflict`
- **Features:**
  - ConcurrentDictionary-based storage
  - Document-based conflict grouping
  - Resolution history tracking
  - Unresolved conflict queries
  - StoredConflict record with metadata

#### ConflictDetector

Enhanced conflict detection:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.Conflict`
- **Features:**
  - Value conflict detection via IEntityComparer
  - Structural conflict detection (missing in graph/document)
  - Entity change tracking since extraction
  - Severity assignment based on confidence
  - ConflictDetail creation with suggested strategies

#### ConflictMerger

Intelligent value merging:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.Conflict`
- **Features:**
  - DocumentFirst strategy — Use document value
  - GraphFirst strategy — Use graph value
  - Combine strategy — Merge string/list values
  - MostRecent strategy — Use timestamp-based selection
  - HighestConfidence strategy — Use confidence-based selection
  - RequiresManualMerge — Return for complex cases
  - Null value handling

#### EnhancedConflictResolver

Extended conflict resolution with detailed results:
- **Namespace:** `Lexichord.Modules.Knowledge.Sync.Conflict`
- **Features:**
  - License gating per severity and strategy
  - Detailed ConflictResolutionResult
  - Merge support via IConflictMerger
  - Conflict store integration
  - MediatR event publishing
  - ResolveAllAsync with ConflictResolutionOptions
  - Three-catch error handling pattern

### Constants

#### FeatureCodes.ConflictResolver

Feature code for conflict resolver license gating:
- **Value:** `"Feature.ConflictResolver"`
- **Required Tiers:**
  - Core: No access
  - WriterPro: Basic resolution (Low severity, Manual)
  - Teams: Full resolution (Low/Medium, Merge support)
  - Enterprise: Advanced resolution (all severities)

---

## Dependencies

### Requires

- `IGraphRepository` (v0.4.5e) — Entity lookups
- `ILicenseContext` (v0.0.4c) — License tier checking
- `IMediator` (MediatR) — Event publishing
- `SyncConflict`, `ConflictType`, `ConflictSeverity`, `ConflictResolutionStrategy` (v0.7.6e)
- `KnowledgeEntity` (v0.4.5e) — Graph entity type
- `ExtractionResult`, `ExtractionRecord` (v0.7.6f) — Extraction data types
- `Document` (v0.4.1c) — Document entity

### Required By

- Conflict resolution UI components
- Background sync jobs
- Document review workflows

---

## Technical Notes

### License Gating

| Tier | Detect | Auto-Resolve Low | Auto-Resolve Medium | Auto-Resolve High | Merge | Manual |
|------|--------|------------------|---------------------|-------------------|-------|--------|
| Core | No | No | No | No | No | No |
| WriterPro | Yes | Yes | No | No | No | Yes |
| Teams | Yes | Yes | Yes | No | Yes | Yes |
| Enterprise | Yes | Yes | Yes | Yes | Yes | Yes |

### Merge Strategy by Conflict Type

Default strategy recommendations from `IConflictMerger.GetMergeStrategy()`:

| ConflictType | Recommended Strategy |
|--------------|---------------------|
| ValueMismatch | DocumentFirst |
| MissingInGraph | DocumentFirst |
| MissingInDocument | RequiresManualMerge |
| ConcurrentEdit | RequiresManualMerge |

### DI Registration

All services registered as singletons in `KnowledgeModule.RegisterServices()`:
- `EntityComparer` → `IEntityComparer`
- `ConflictStore` (concrete)
- `ConflictDetector` → `IConflictDetector`
- `ConflictMerger` → `IConflictMerger`
- `EnhancedConflictResolver` (concrete)

### Thread Safety

- `ConflictStore` uses `ConcurrentDictionary` for thread-safe storage
- All async operations support cancellation tokens
- Lock-based index updates in ConflictStore

---

## File Summary

| Category | Count | Location |
|----------|-------|----------|
| Enums | 2 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/Conflict/` |
| Records | 9 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/Conflict/` |
| Interfaces | 3 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/Conflict/` |
| Events | 1 | `src/Lexichord.Abstractions/Contracts/Knowledge/Sync/Conflict/Events/` |
| Services | 5 | `src/Lexichord.Modules.Knowledge/Sync/Conflict/` |
| Tests | 7 | `tests/Lexichord.Tests.Unit/Modules/Knowledge/Sync/Conflict/` |
| **Total** | **27** | |

---

## Testing

Unit tests verify:
- License gating for all tiers (Core blocked, WriterPro basic, Teams full, Enterprise advanced)
- All merge strategies (DocumentFirst, GraphFirst, Combine, MostRecent, HighestConfidence, RequiresManual)
- Entity comparison with standard and custom properties
- Conflict store CRUD and thread safety
- Conflict detection for value and structural conflicts
- Entity change tracking since extraction
- ConflictResolutionOptions behavior (CanAutoResolve, GetStrategy)
- MediatR event publishing on successful resolution
- Three-catch error handling pattern
- Record initialization, defaults, and factory methods
- Enum value coverage

All 80+ tests pass with `[Trait("SubPart", "v0.7.6h")]`.
