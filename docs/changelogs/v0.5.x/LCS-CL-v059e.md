# Changelog: v0.5.9e – Contradiction Detection & Resolution

**Feature ID:** RAG-DEDUP-05  
**Version:** 0.5.9e  
**Date:** 2026-02-04  
**Status:** ✅ Complete

---

## Overview

This release implements **Contradiction Detection & Resolution**, the fifth and final component of the Semantic Memory Deduplication feature (v0.5.9). When the deduplication pipeline identifies chunks containing conflicting information, they are now properly recorded, tracked, and made available for admin resolution through a structured workflow.

---

## What's New

### 🆕 New Features

#### IContradictionService Interface
A comprehensive service interface for managing the full lifecycle of detected contradictions:

- **`FlagAsync`**: Record a new contradiction from the deduplication pipeline
- **`GetByIdAsync`**: Retrieve a contradiction by ID
- **`GetByChunkIdAsync`**: Find all contradictions involving a specific chunk
- **`GetPendingAsync`**: List pending contradictions for admin review
- **`GetStatisticsAsync`**: Get dashboard-ready statistics
- **`BeginReviewAsync`**: Transition a contradiction to "under review" status
- **`ResolveAsync`**: Apply a resolution decision
- **`DismissAsync`**: Mark as false positive
- **`AutoResolveAsync`**: System-initiated resolution (e.g., document deletion)
- **`DeleteAsync`**: Permanently remove a contradiction record

#### Data Contracts

- **`Contradiction`**: Core record representing a detected contradiction between two chunks
  - Tracks conflicting chunk IDs, similarity score, classification confidence
  - Maintains lifecycle status (Pending, UnderReview, Resolved, Dismissed, AutoResolved)
  - Includes resolution details when applicable

- **`ContradictionStatus`**: Enum defining lifecycle states
  - `Pending`: Awaiting review
  - `UnderReview`: Admin is actively reviewing
  - `Resolved`: Resolution applied
  - `Dismissed`: Marked as false positive
  - `AutoResolved`: System-resolved (document changes)

- **`ContradictionResolution`**: Record for resolution decisions
  - Tracks resolution type, rationale, and affected chunks
  - Factory methods for each resolution type

- **`ContradictionResolutionType`**: Enum defining resolution strategies
  - `KeepOlder`: Retain the earlier chunk as authoritative
  - `KeepNewer`: Retain the later chunk as authoritative
  - `KeepBoth`: Both chunks are intentionally different
  - `CreateSynthesis`: Create a new unified chunk
  - `DeleteBoth`: Remove both chunks

- **`ContradictionStatistics`**: Dashboard summary record

#### Events

- **`ContradictionDetectedEvent`**: Published when a new contradiction is flagged
- **`ContradictionResolvedEvent`**: Published when a contradiction is resolved/dismissed

#### Database Migration

- **`Migration_010_Contradictions`**: Creates the `Contradictions` table with:
  - Primary key and chunk foreign keys
  - Classification metadata (similarity, confidence, reason)
  - Lifecycle tracking (status, timestamps, reviewer)
  - Resolution details (type, rationale, affected chunks)
  - Unique index on normalized chunk pair (prevents symmetric duplicates)
  - Partial indexes for efficient pending queue queries

### 🔄 Modified Components

#### DeduplicationService
- Now injects `IContradictionService` as a constructor dependency
- `HandleContradictoryAsync` method updated to call `FlagAsync` when contradictions are detected
- Enhanced logging with confidence scores

#### RAGModule
- Registers `ContradictionService` with scoped lifetime

---

## Technical Details

### Integration Points

The `ContradictionService` integrates with:

1. **DeduplicationService (v0.5.9d)**: Calls `FlagAsync` when `RelationshipType.Contradictory` is detected
2. **ICanonicalManager (v0.5.9c)**: Resolution may trigger canonical record updates (merge, archive, delete)
3. **IChunkRepository**: Used for chunk loading during synthesis resolution
4. **MediatR**: Publishes events for downstream consumers

### Resolution Workflow

```
Detection → Pending → UnderReview → Resolved/Dismissed
                  ↘ AutoResolved (if document deleted)
```

### Duplicate Detection

Contradictions between chunks A and B are detected symmetrically:
- (A, B) and (B, A) are treated as the same contradiction
- Unique index uses `LEAST/GREATEST` for normalized ordering

### Event Flow

```
DeduplicationService → FlagAsync → ContradictionDetectedEvent
                                      ↓
                              Admin Dashboard
                                      ↓
ResolveAsync/DismissAsync → ContradictionResolvedEvent
                                      ↓
                              Audit Logging
```

---

## Files Changed

### New Files

| File | Description |
|------|-------------|
| `Lexichord.Abstractions/Contracts/RAG/Contradiction.cs` | Core contradiction record |
| `Lexichord.Abstractions/Contracts/RAG/ContradictionStatus.cs` | Lifecycle status enum |
| `Lexichord.Abstractions/Contracts/RAG/ContradictionResolution.cs` | Resolution decision record |
| `Lexichord.Abstractions/Contracts/RAG/ContradictionResolutionType.cs` | Resolution type enum |
| `Lexichord.Abstractions/Contracts/RAG/IContradictionService.cs` | Service interface |
| `Lexichord.Abstractions/Events/ContradictionDetectedEvent.cs` | Detection event |
| `Lexichord.Abstractions/Events/ContradictionResolvedEvent.cs` | Resolution event |
| `Lexichord.Modules.RAG/Services/ContradictionService.cs` | Service implementation |
| `Lexichord.Infrastructure/Migrations/Migration_010_Contradictions.cs` | Database migration |
| `Lexichord.Tests.Unit/Services/ContradictionServiceTests.cs` | Unit tests |

### Modified Files

| File | Change |
|------|--------|
| `Lexichord.Modules.RAG/Services/DeduplicationService.cs` | Added IContradictionService dependency and integration |
| `Lexichord.Modules.RAG/RAGModule.cs` | Registered ContradictionService |
| `Lexichord.Tests.Unit/Modules/RAG/Services/DeduplicationServiceTests.cs` | Updated for new constructor |

---

## Testing

### Unit Tests Added

- `ContradictionServiceTests`: Constructor validation, parameter validation, exception handling
- `ContradictionTests`: Factory methods, property calculations
- `ContradictionResolutionTests`: Factory methods, property calculations
- `ContradictionDetectedEventTests`: Event creation, properties
- `ContradictionResolvedEventTests`: Event creation, factory methods

### Test Coverage

- 30 new test cases for contradiction-related functionality
- 24 existing DeduplicationService tests updated and passing
- All tests pass ✅

---

## Dependencies

### Prerequisites

- v0.5.9a: ISimilarityDetector ✅
- v0.5.9b: IRelationshipClassifier ✅
- v0.5.9c: ICanonicalManager ✅
- v0.5.9d: IDeduplicationService ✅

### Database

- Requires `Migration_010_Contradictions` to be applied
- Depends on `Chunks` table from `Migration_003_VectorSchema`

---

## Migration Notes

### Applying the Migration

```bash
dotnet run --project src/Lexichord.Host -- migrate up
```

### Rollback

```bash
dotnet run --project src/Lexichord.Host -- migrate down --version 10
```

---

## Next Steps

With v0.5.9e complete, the Semantic Memory Deduplication feature is fully implemented:

1. **v0.5.9a**: Similarity Detection ✅
2. **v0.5.9b**: Relationship Classification ✅
3. **v0.5.9c**: Canonical Record Management ✅
4. **v0.5.9d**: Deduplication Service ✅
5. **v0.5.9e**: Contradiction Detection ✅

### Recommended Follow-ups

- Admin UI components for the resolution workflow
- Email/push notifications for high-confidence contradictions
- Telemetry dashboards for contradiction metrics
- Integration tests with live database

---

## API Reference

### IContradictionService

```csharp
public interface IContradictionService
{
    Task<Contradiction> FlagAsync(Guid chunkAId, Guid chunkBId, float similarityScore, 
        float confidence, string? reason = null, Guid? projectId = null, CancellationToken ct = default);
    
    Task<Contradiction?> GetByIdAsync(Guid contradictionId, CancellationToken ct = default);
    
    Task<IReadOnlyList<Contradiction>> GetByChunkIdAsync(Guid chunkId, 
        bool includeResolved = false, CancellationToken ct = default);
    
    Task<IReadOnlyList<Contradiction>> GetPendingAsync(Guid? projectId = null, CancellationToken ct = default);
    
    Task<ContradictionStatistics> GetStatisticsAsync(Guid? projectId = null, CancellationToken ct = default);
    
    Task<Contradiction> BeginReviewAsync(Guid contradictionId, string reviewerId, CancellationToken ct = default);
    
    Task<Contradiction> ResolveAsync(Guid contradictionId, ContradictionResolution resolution, CancellationToken ct = default);
    
    Task<Contradiction> DismissAsync(Guid contradictionId, string reason, string dismissedBy, CancellationToken ct = default);
    
    Task<Contradiction> AutoResolveAsync(Guid contradictionId, string reason, CancellationToken ct = default);
    
    Task<bool> DeleteAsync(Guid contradictionId, CancellationToken ct = default);
}
```

### Resolution Factory Methods

```csharp
// Keep the older chunk as authoritative
ContradictionResolution.KeepOlder(rationale, resolvedBy, retainedChunkId, archivedChunkId);

// Keep the newer chunk as authoritative
ContradictionResolution.KeepNewer(rationale, resolvedBy, retainedChunkId, archivedChunkId);

// Keep both chunks as intentionally different
ContradictionResolution.KeepBoth(rationale, resolvedBy);

// Create a synthesized replacement
ContradictionResolution.Synthesize(rationale, resolvedBy, synthesizedContent);

// Delete both conflicting chunks
ContradictionResolution.DeleteBoth(rationale, resolvedBy);
```

---

**Author:** Lexichord Team  
**Reviewed:** Automated CI  
**Approved:** 2026-02-04
