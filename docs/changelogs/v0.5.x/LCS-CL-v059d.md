# v0.5.9d Changelog: Deduplication Service

**Version:** v0.5.9d  
**Released:** 2026-02-04  
**Module:** Lexichord.Modules.RAG  
**License Tier:** Writer Pro

---

## Overview

Implements the central `IDeduplicationService` that orchestrates the full deduplication pipeline during chunk ingestion. Combines similarity detection (v0.5.9a), relationship classification (v0.5.9b), and canonical record management (v0.5.9c) into a unified service with license gating, manual review queues, and comprehensive telemetry.

## Components Added

### Data Contracts (Abstractions)

| File | Description |
|------|-------------|
| `DeduplicationAction.cs` | Enum: StoredAsNew, MergedIntoExisting, Linked, Flagged, QueuedForReview |
| `DeduplicationResult.cs` | Immutable result with factory methods (StoredAsNew, Merged, QueuedForReview) |
| `DeduplicationOptions.cs` | Configuration: thresholds, LLM confirmation, review queue, contradiction detection |
| `DuplicateCandidate.cs` | Candidate chunk for manual review with classification metadata |
| `ManualMergeDecision.cs` | Admin decision from review queue |
| `ManualDecisionType.cs` | Enum: Merge, Link, Keep, Delete |
| `PendingReview.cs` | Item in manual review queue |
| `IDeduplicationService.cs` | Interface: ProcessChunkAsync, FindDuplicatesAsync, ProcessManualDecisionAsync, GetPendingReviewsAsync |

### Constants

| File | Description |
|------|-------------|
| `FeatureCodes.cs` | Added `DeduplicationService = "RAG.Dedup.Service"` |

### Database Migration

| File | Description |
|------|-------------|
| `Migration_009_PendingReviews.cs` | Creates pending_reviews table for manual review queue |

### Service Implementation

| File | Description |
|------|-------------|
| `DeduplicationService.cs` | Full pipeline orchestration with Dapper database operations |

## Pipeline Flow

```
New Chunk → License Check → Similarity Detection → Relationship Classification → Routing
                ↓                                                                    ↓
           Bypass if            Equivalent → Merge into canonical
           unlicensed           Complementary → Link chunks
                                Contradictory → Flag for resolution
                                Superseding → Replace canonical
                                Distinct → Store as new canonical
                                Low confidence → Queue for manual review
```

## Database Schema

### pending_reviews Table
- `Id` (UUID PK)
- `NewChunkId` (FK → Chunks)
- `Candidates` (JSONB) — Serialized DuplicateCandidate[]
- `QueuedAt` (TIMESTAMPTZ)
- `AutoClassificationReason` (VARCHAR, nullable)

## Dependencies

- **Upstream:**
  - v0.5.9a: ISimilarityDetector
  - v0.5.9b: IRelationshipClassifier, ClassificationOptions
  - v0.5.9c: ICanonicalManager
- **IChunkRepository:** Existing chunk operations
- **IDbConnectionFactory:** Database connections for Dapper
- **ILicenseContext:** Writer Pro feature gating

## Files Changed

### Added
- `src/Lexichord.Abstractions/Contracts/RAG/DeduplicationAction.cs`
- `src/Lexichord.Abstractions/Contracts/RAG/DeduplicationResult.cs`
- `src/Lexichord.Abstractions/Contracts/RAG/DeduplicationOptions.cs`
- `src/Lexichord.Abstractions/Contracts/RAG/DuplicateCandidate.cs`
- `src/Lexichord.Abstractions/Contracts/RAG/ManualMergeDecision.cs`
- `src/Lexichord.Abstractions/Contracts/RAG/ManualDecisionType.cs`
- `src/Lexichord.Abstractions/Contracts/RAG/PendingReview.cs`
- `src/Lexichord.Abstractions/Contracts/RAG/IDeduplicationService.cs`
- `src/Lexichord.Infrastructure/Persistence/Migrations/Migration_009_PendingReviews.cs`
- `src/Lexichord.Modules.RAG/Services/DeduplicationService.cs`
- `tests/Lexichord.Tests.Unit/Modules/RAG/Services/DeduplicationServiceTests.cs`

### Modified
- `src/Lexichord.Abstractions/Constants/FeatureCodes.cs` — Added DeduplicationService constant
- `src/Lexichord.Modules.RAG/RAGModule.cs` — Registered IDeduplicationService

## Test Coverage

- **23 unit tests** covering:
  - Constructor null argument validation (8 tests)
  - License gating bypass behavior (3 tests)
  - Null/empty embedding validation (4 tests)
  - No-duplicates "stored as new" path (2 tests)
  - DeduplicationOptions default and custom values (2 tests)
  - DeduplicationResult factory methods (4 tests)

## Verification Steps

1. Build solution: `dotnet build`
2. Run Deduplication tests: `dotnet test --filter "FullyQualifiedName~DeduplicationServiceTests"`

## Related Documentation

- Design Spec: `docs/specs/v0.5.x/v0.5.9/LCS-DES-v0.5.9d.md`
- Parent: `docs/specs/v0.5.x/v0.5.9/LCS-DES-v0.5.9-INDEX.md`
