# v0.5.9c Changelog: Canonical Record Management

**Version:** v0.5.9c  
**Released:** 2026-02-04  
**Module:** Lexichord.Modules.RAG  
**License Tier:** Writer Pro

---

## Overview

Implements the `ICanonicalManager` interface for managing canonical records—the authoritative representation of unique facts discovered during deduplication. Provides atomic operations for creating canonicals, merging variants, promotion, detachment, and provenance tracking.

## Components Added

### Data Contracts (Abstractions)

| File | Description |
|------|-------------|
| `CanonicalRecord.cs` | Authoritative chunk representing a unique fact |
| `ChunkVariant.cs` | Track merged duplicates with relationship type |
| `ChunkProvenance.cs` | Origin and verification tracking for chunks |
| `ICanonicalManager.cs` | Interface for CRUD operations on canonical records |

### MediatR Events

| File | Description |
|------|-------------|
| `CanonicalRecordCreatedEvent.cs` | Published when new canonical created |
| `ChunkDeduplicatedEvent.cs` | Published when variant merged |
| `VariantPromotedEvent.cs` | Published when variant promoted to canonical |
| `VariantDetachedEvent.cs` | Published when variant detached |

### Database Migration

| File | Description |
|------|-------------|
| `Migration_008_CanonicalRecords.cs` | Creates CanonicalRecords, ChunkVariants, ChunkProvenance tables |

### Service Implementation

| File | Description |
|------|-------------|
| `CanonicalManager.cs` | Dapper-based implementation with transactions |

## Database Schema

### CanonicalRecords Table
- `Id` (UUID PK)
- `CanonicalChunkId` (FK → Chunks, unique)
- `CreatedAt`, `UpdatedAt` (TIMESTAMPTZ)
- `MergeCount` (INT)

### ChunkVariants Table
- `Id` (UUID PK)
- `CanonicalRecordId` (FK → CanonicalRecords)
- `VariantChunkId` (FK → Chunks, unique)
- `RelationshipType` (INT enum)
- `SimilarityScore` (FLOAT)
- `MergedAt` (TIMESTAMPTZ)

### ChunkProvenance Table
- `Id` (UUID PK)
- `ChunkId` (FK → Chunks, unique)
- `SourceDocumentId` (FK → Documents, nullable)
- `SourceLocation`, `VerifiedBy` (VARCHAR)
- `IngestedAt`, `VerifiedAt` (TIMESTAMPTZ)

## Dependencies

- **Upstream:** v0.5.9b (IRelationshipClassifier, RelationshipType)
- **Dapper:** Database operations with transactions
- **MediatR:** Event publishing

## Files Changed

### Added
- `src/Lexichord.Abstractions/Contracts/RAG/CanonicalRecord.cs`
- `src/Lexichord.Abstractions/Contracts/RAG/ChunkVariant.cs`
- `src/Lexichord.Abstractions/Contracts/RAG/ChunkProvenance.cs`
- `src/Lexichord.Abstractions/Contracts/RAG/ICanonicalManager.cs`
- `src/Lexichord.Abstractions/Events/CanonicalRecordCreatedEvent.cs`
- `src/Lexichord.Abstractions/Events/ChunkDeduplicatedEvent.cs`
- `src/Lexichord.Abstractions/Events/VariantPromotedEvent.cs`
- `src/Lexichord.Abstractions/Events/VariantDetachedEvent.cs`
- `src/Lexichord.Infrastructure/Migrations/Migration_008_CanonicalRecords.cs`
- `src/Lexichord.Modules.RAG/Services/CanonicalManager.cs`
- `tests/Lexichord.Tests.Unit/Modules/RAG/Services/CanonicalManagerTests.cs`

### Modified
- `src/Lexichord.Modules.RAG/RAGModule.cs` — Registered ICanonicalManager

## Test Coverage

- **24 unit tests** covering:
  - Constructor null argument validation (5 tests)
  - License gating for all mutation operations (5 tests)
  - Argument validation for all public methods (14 tests)

## Verification Steps

```bash
# Build solution
dotnet build Lexichord.sln

# Run canonical manager tests
dotnet test --filter "FullyQualifiedName~CanonicalManagerTests"

# Full test suite
dotnet test
```

## Next Steps

- **v0.5.9d:** Deduplication Orchestrator — Wire detection, classification, and management
- **Integration Tests:** Full workflow with database
