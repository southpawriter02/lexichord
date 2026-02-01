# Changelog: v0.4.2a - Ingestion Service Interface

**Release Date:** 2026-01-31
**Status:** Implemented
**Specification:** [LCS-DES-v0.4.2a](../specs/v0.4.x/v0.4.2/LCS-DES-v0.4.2a.md)

---

## Summary

Defines the core abstractions for the file ingestion pipeline in `Lexichord.Abstractions.Contracts.Ingestion`. This establishes the contracts for single file and directory ingestion, document removal, and progress reporting.

---

## Changes

### Added

#### Lexichord.Abstractions/Contracts/Ingestion/

| File                            | Type      | Description                                    |
| ------------------------------- | --------- | ---------------------------------------------- |
| `IngestionPhase.cs`             | Enum      | 7-phase pipeline stages (Scanning → Complete)  |
| `IngestionResult.cs`            | Record    | Operation outcome with factory methods         |
| `IngestionProgressEventArgs.cs` | Class     | Progress reporting with percentage calculation |
| `IngestionOptions.cs`           | Record    | Configuration with sensible defaults           |
| `IIngestionService.cs`          | Interface | Main contract for ingestion operations         |

#### Lexichord.Tests.Unit/Abstractions/Ingestion/

| File                                 | Tests | Coverage                  |
| ------------------------------------ | ----- | ------------------------- |
| `IngestionPhaseTests.cs`             | 5     | Enum values, ordering     |
| `IngestionResultRecordTests.cs`      | 11    | Factory methods, equality |
| `IngestionProgressEventArgsTests.cs` | 9     | Validation, percentage    |
| `IngestionOptionsRecordTests.cs`     | 10    | Defaults, helper methods  |
| `IIngestionServiceContractTests.cs`  | 8     | Interface mockability     |

---

## Verification

```bash
dotnet test --filter "FullyQualifiedName~Ingestion" --nologo
```

**Result:** 28 tests passed ✓

---

## Dependencies

- None (pure abstractions, no external packages required)

## Dependents

- v0.4.2b: File Hashing Service (will implement hash computation)
- v0.4.2c: File Watcher (will trigger ingestion events)
- v0.4.2d: Ingestion Queue (will implement background processing)
