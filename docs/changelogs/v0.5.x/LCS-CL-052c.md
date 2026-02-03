# LCS-CL-052c: Stale Citation Detection

## Document Control

| Field              | Value                           |
| :----------------- | :------------------------------ |
| **Document ID**    | LCS-CL-052c                     |
| **Version**        | v0.5.2c                         |
| **Feature Name**   | Stale Citation Detection        |
| **Module**         | Lexichord.Modules.RAG           |
| **Status**         | Complete                        |
| **Last Updated**   | 2026-02-02                      |
| **Related Spec**   | [LCS-DES-v0.5.2c](../../specs/v0.5.x/v0.5.2/LCS-DES-v0.5.2c.md) |

---

## Summary

Implemented the Stale Citation Detection system — validation logic that compares citation timestamps against current file state to detect stale references (v0.5.2c). This sub-part introduces the `ICitationValidator` interface for single and batch citation validation, the `CitationValidator` implementation with timestamp-based freshness checking and license gating, the `CitationValidationResult` record with computed status properties, the `CitationValidationFailedEvent` MediatR notification for stale/missing citations, and the `StaleIndicatorViewModel` with Validate, Re-verify, and Dismiss commands.

---

## Changes

### New Files

#### Lexichord.Abstractions

| File | Description |
|:-----|:------------|
| `Contracts/CitationValidationStatus.cs` | Enum defining four validation states: Valid (0), Stale (1), Missing (2), Error (3). Used by CitationValidationResult and StaleIndicatorViewModel |
| `Contracts/CitationValidationResult.cs` | Immutable record with five constructor parameters (Citation, IsValid, Status, CurrentModifiedAt, ErrorMessage) and computed properties IsStale, IsMissing, HasError, StatusMessage with user-friendly status formatting |
| `Contracts/ICitationValidator.cs` | Interface defining ValidateAsync (single), ValidateBatchAsync (parallel with throttling), and ValidateIfLicensedAsync (license-gated, returns null for Core tier) |
| `Events/CitationValidationFailedEvent.cs` | MediatR INotification published when validation detects stale or missing citations. Carries CitationValidationResult and UTC timestamp |

#### Lexichord.Modules.RAG

| File | Description |
|:-----|:------------|
| `Services/CitationValidator.cs` | Singleton implementation of ICitationValidator. Compares file LastWriteTimeUtc against Citation.IndexedAt for freshness detection. Batch validation uses SemaphoreSlim(10) for throttled parallelism. License-gated via FeatureCodes.CitationValidation. Catches UnauthorizedAccessException/IOException as Error status. Publishes CitationValidationFailedEvent for Stale and Missing results |
| `ViewModels/StaleIndicatorViewModel.cs` | Transient ViewModel for stale indicator UI component. Observable properties: ValidationResult, IsVisible, IsVerifying. Computed properties: IsStale, IsMissing, StatusIcon (⚠️/❌), StatusMessage. Commands: ValidateAsync (license-gated), ReverifyAsync (re-indexes via IIndexManagementService then re-validates), Dismiss (hides indicator) |

#### Lexichord.Tests.Unit

| File | Description |
|:-----|:------------|
| `Abstractions/Contracts/CitationValidationStatusTests.cs` | 3 tests: enum value count, ordinal positions, string parsing |
| `Abstractions/Contracts/CitationValidationResultTests.cs` | 14 tests: IsStale/IsMissing/HasError computed properties for all statuses, StatusMessage formatting for Valid/Stale/Missing/Error, record equality, IsValid property |
| `Modules/RAG/Services/CitationValidatorTests.cs` | 22 tests: constructor validation, ValidateAsync for Valid/Stale/Missing scenarios, event publishing verification, CurrentModifiedAt population, batch validation (multiple valid, mixed results, empty, null, order preservation), license gating (licensed/unlicensed/no file access), event timestamp |
| `Modules/RAG/ViewModels/StaleIndicatorViewModelTests.cs` | 19 tests: constructor validation, ValidateCommand for Stale/Valid/Unlicensed/Missing, ReverifyCommand (reindex + revalidate, null guard), DismissCommand (hides, preserves result), computed properties (IsStale/IsMissing/StatusIcon/StatusMessage), property change notifications, initial state |

### Modified Files

| File | Change |
|:-----|:-------|
| `Constants/FeatureCodes.cs` | Added `CitationValidation` feature code constant (`Feature.CitationValidation`) in Citation Engine region (v0.5.2c) |
| `RAGModule.cs` | Added singleton registration of `ICitationValidator` → `CitationValidator` and transient registration of `StaleIndicatorViewModel` in new v0.5.2c section |

---

## Technical Details

### Validation Flow

1. Check if file exists at `Citation.DocumentPath`
2. If missing: log warning, publish `CitationValidationFailedEvent`, return Missing status
3. Get file's `LastWriteTimeUtc` via `FileInfo`
4. If `currentModifiedAt > citation.IndexedAt`: log warning, publish event, return Stale status
5. Otherwise: return Valid status with `CurrentModifiedAt` populated
6. On `UnauthorizedAccessException` or `IOException`: return Error status with exception message

### Batch Validation

- Uses `SemaphoreSlim(10)` for throttled parallel execution
- Prevents excessive file system I/O when validating large result sets
- Results returned in same order as input citations
- Logs summary of stale/missing counts if any found

### License Gating Strategy

| Tier | Behavior |
|:-----|:---------|
| Core | `ValidateIfLicensedAsync` returns null; stale indicators hidden |
| WriterPro+ | Full validation with stale/missing detection |

License gating uses `ILicenseContext.IsFeatureEnabled(FeatureCodes.CitationValidation)` in `ValidateIfLicensedAsync`. Direct `ValidateAsync` calls bypass license checking.

### StaleIndicatorViewModel

- **ValidateCommand**: Calls `ValidateIfLicensedAsync`, sets `IsVisible` based on result validity
- **ReverifyCommand**: Calls `IIndexManagementService.ReindexDocumentAsync` with `Citation.ChunkId` (which stores the document GUID, as set by `CitationService.CreateCitation` in v0.5.2a), then re-validates
- **DismissCommand**: Hides indicator, preserves `ValidationResult`
- **Property Change Notifications**: `OnValidationResultChanged` partial method raises `PropertyChanged` for computed properties (`IsStale`, `IsMissing`, `StatusIcon`, `StatusMessage`)

### Event Publishing

- `CitationValidationFailedEvent` published via `await _mediator.Publish()` (synchronous, not fire-and-forget) for Stale and Missing results
- NOT published for Valid or Error statuses
- Enables downstream consumers: UI updates, analytics, audit logging

### Dependencies

| Dependency | Version | Purpose |
|:-----------|:--------|:--------|
| Citation record | v0.5.2a | Citation data to validate |
| ILicenseContext | v0.0.4c | License-gated validation |
| IMediator | v0.0.7a | CitationValidationFailedEvent publishing |
| ILogger&lt;T&gt; | v0.0.3b | Structured logging |
| IIndexManagementService | v0.4.7b | Re-index stale documents (ViewModel) |

### Design Adaptations

The specification referenced `FeatureFlags.RAG.CitationValidation` but the codebase uses the `FeatureCodes` static class convention. Adapted to `FeatureCodes.CitationValidation = "Feature.CitationValidation"`.

The specification referenced `ILicenseContext.HasFeature()` but the actual interface method is `IsFeatureEnabled()`.

The specification listed `IWorkspaceService` as a constructor dependency for `CitationValidator`, but the implementation uses `File.Exists()` and `FileInfo` directly for file system access, matching the pattern in `CitationService.ValidateCitationAsync` (v0.5.2a).

The specification's `StaleIndicatorViewModel.ReverifyAsync` called `_indexService.ReindexDocumentAsync(ValidationResult.Citation.DocumentPath)` but `IIndexManagementService.ReindexDocumentAsync` takes a `Guid documentId`. Adapted to use `Citation.ChunkId` which stores the document's GUID (set by `CitationService.CreateCitation` at v0.5.2a line 158: `ChunkId: document.Id`).

---

## Verification

### Unit Tests

All 58 new tests passed:

- CitationValidationStatus enum tests (3 tests)
  - Enum has exactly 4 values
  - Correct ordinal values (Valid=0, Stale=1, Missing=2, Error=3)
  - String parsing for all values

- CitationValidationResult record tests (14 tests)
  - IsStale returns true only for Stale status (4 tests)
  - IsMissing returns true only for Missing status (4 tests)
  - HasError returns true only for Error status (4 tests)
  - StatusMessage for Valid: "Citation is current"
  - StatusMessage for Stale: includes formatted date
  - StatusMessage for Stale with null date: "Source modified recently"
  - StatusMessage for Missing: "Source file not found"
  - StatusMessage for Error with message: returns error message
  - StatusMessage for Error without message: "Validation failed"
  - Record equality (same values, different status)
  - IsValid property tests

- CitationValidator service tests (22 tests)
  - Constructor null validation (3 tests)
  - ValidateAsync: Valid for unchanged file
  - ValidateAsync: Valid populates CurrentModifiedAt
  - ValidateAsync: Valid does not publish event
  - ValidateAsync: Stale for modified file
  - ValidateAsync: Stale publishes CitationValidationFailedEvent
  - ValidateAsync: Stale populates CurrentModifiedAt
  - ValidateAsync: Missing for nonexistent file
  - ValidateAsync: Missing publishes event
  - ValidateAsync: Missing has null CurrentModifiedAt
  - ValidateAsync: Null citation throws ArgumentNullException
  - ValidateAsync: Result contains original citation reference
  - ValidateBatchAsync: Multiple valid files (5 tests)
  - ValidateBatchAsync: Mixed results (valid, stale, missing)
  - ValidateBatchAsync: Empty input returns empty list
  - ValidateBatchAsync: Null input throws ArgumentNullException
  - ValidateBatchAsync: Preserves input order
  - ValidateIfLicensedAsync: Licensed returns result
  - ValidateIfLicensedAsync: Unlicensed returns null
  - ValidateIfLicensedAsync: Unlicensed does not access file system
  - Event timestamp is within expected range

- StaleIndicatorViewModel tests (19 tests)
  - Constructor null validation (3 tests)
  - ValidateCommand: Stale citation shows indicator
  - ValidateCommand: Valid citation hides indicator
  - ValidateCommand: Unlicensed hides indicator
  - ValidateCommand: Missing file shows indicator
  - ReverifyCommand: Re-indexes and re-validates
  - ReverifyCommand: Null ValidationResult does nothing
  - DismissCommand: Hides indicator
  - DismissCommand: Preserves ValidationResult
  - IsStale no result returns false
  - IsMissing no result returns false
  - StatusIcon stale returns ⚠️
  - StatusIcon missing returns ❌
  - StatusMessage no result returns empty
  - StatusMessage stale delegates to result
  - Property change notifications for computed properties
  - Initial state is not visible

### Build Verification

```
dotnet build
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Regression Check

```
dotnet test tests/Lexichord.Tests.Unit
Passed: 5279, Skipped: 33, Failed: 1 (pre-existing: IgnorePatternServiceTests.RegisterPatterns_DuringReads_NoExceptions)
```

Note: The single failing test (`IgnorePatternServiceTests.RegisterPatterns_DuringReads_NoExceptions`) is a pre-existing race condition in the Style module unrelated to v0.5.2c changes.

---

## Deliverable Checklist

| # | Deliverable | Status |
|:--|:------------|:-------|
| 1 | `CitationValidationStatus` enum | [x] |
| 2 | `CitationValidationResult` record with computed properties | [x] |
| 3 | `ICitationValidator` interface | [x] |
| 4 | `CitationValidator` implementation | [x] |
| 5 | Batch validation with parallel execution | [x] |
| 6 | `CitationValidationFailedEvent` MediatR notification | [x] |
| 7 | `StaleIndicatorViewModel` component | [x] |
| 8 | Stale indicator AXAML view | [ ] (deferred — UI markup, not part of service layer) |
| 9 | "Re-verify" action implementation | [x] |
| 10 | Unit tests for validator (22 tests) | [x] |
| 11 | Unit tests for ViewModel (19 tests) | [x] |
| 12 | Unit tests for enum and record (17 tests) | [x] |
| 13 | DI registration | [x] |
| 14 | `CitationValidation` feature code | [x] |

---

## Related Documents

- [LCS-DES-v0.5.2c](../../specs/v0.5.x/v0.5.2/LCS-DES-v0.5.2c.md) — Design specification
- [LCS-DES-v0.5.2-INDEX](../../specs/v0.5.x/v0.5.2/LCS-DES-v0.5.2-INDEX.md) — Feature index
- [LCS-SBD-v0.5.2](../../specs/v0.5.x/v0.5.2/LCS-SBD-v0.5.2.md) — Scope breakdown
- [LCS-CL-052b](./LCS-CL-052b.md) — Citation Styles (prerequisite: v0.5.2b complete)
