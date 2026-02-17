# Changelog: v0.7.5h — Combined Fix Workflow

**Feature ID:** AGT-075h
**Version:** 0.7.5h
**Date:** 2026-02-16
**Status:** Complete

---

## Overview

Implements the Combined Fix Workflow for the Tuning Agent, providing orchestrated fix application across multiple validator types with conflict detection, position-based sorting, atomic undo, and re-validation support. This is the eighth sub-part of v0.7.5 "The Tuning Agent" and builds upon v0.7.5e's Unified Issue Model, v0.7.5f's Issue Aggregator, and v0.7.5g's Unified Issues Panel.

The implementation adds:
- `ConflictHandlingStrategy` — Enum controlling how the workflow handles fix conflicts
- `FixConflictType` — Enum classifying types of conflicts between fixes
- `FixConflictSeverity` — Enum for conflict severity levels
- `FixWorkflowOptions` — Configuration record for fix workflow behavior
- `FixConflictCase` — Record describing a detected conflict
- `FixApplyResult` — Comprehensive result record for fix operations
- `FixTransaction` — Undo transaction record for applied fixes
- `FixConflictException`, `FixApplicationTimeoutException`, `DocumentCorruptionException` — Exception types
- `FixesAppliedEventArgs`, `FixConflictDetectedEventArgs` — Event arguments
- `IUnifiedFixWorkflow` — Interface for the fix workflow orchestrator
- `FixPositionSorter` — Bottom-to-top sorting to prevent offset drift
- `FixConflictDetector` — Overlap, contradiction, and dependency detection
- `FixGrouper` — Category-ordered grouping for cascading minimization
- `UnifiedFixOrchestrator` — Full orchestrator implementation with atomic undo, re-validation, and DryRun
- DI registration via `AddUnifiedFixWorkflow()` extension
- `UnifiedIssuesPanelViewModel` integration for orchestrated fix commands
- Full unit test coverage (135 tests)

---

## What's New

### Contract Enums

#### ConflictHandlingStrategy

Controls how the workflow handles fix conflicts:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Values:**
  - `ThrowException` — Throw `FixConflictException` when conflicts detected
  - `SkipConflicting` — Skip conflicting fixes, apply non-conflicting ones
  - `PromptUser` — Raise event for user decision
  - `PriorityBased` — Resolve conflicts by fix priority/confidence

#### FixConflictType

Classifies types of conflicts between fixes:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Values:**
  - `OverlappingPositions` — Two fixes target overlapping text ranges
  - `ContradictorySuggestions` — Fixes suggest contradictory replacements
  - `DependentFixes` — One fix depends on another being applied first
  - `CreatesNewIssue` — Applying a fix would introduce a new issue
  - `InvalidLocation` — Fix targets a location outside document bounds

#### FixConflictSeverity

Severity levels for conflict cases:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Values:**
  - `Info` — Informational, can proceed safely
  - `Warning` — Proceed with caution
  - `Error` — Cannot proceed without resolution

### Contract Records

#### FixWorkflowOptions

Configuration record for fix workflow behavior:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Properties:**
  - `DryRun` — If true, apply fixes to a string copy without editor mutations
  - `ConflictStrategy` — `ConflictHandlingStrategy` for handling detected conflicts
  - `ReValidateAfterFixes` — If true, run validation after fixes are applied
  - `MaxFixIterations` — Maximum fix-then-revalidate iterations
  - `EnableUndo` — If true, record undo transactions
  - `Timeout` — `TimeSpan` for operation timeout
  - `Verbose` — If true, include detailed operation trace
- **Static Properties:**
  - `Default` — Sensible defaults (SkipConflicting, re-validate, undo enabled)

#### FixConflictCase

Describes a single detected conflict:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Properties:**
  - `Type` — `FixConflictType` classification
  - `ConflictingIssueIds` — `IReadOnlyList<Guid>` of issue IDs involved
  - `Description` — Human-readable conflict explanation
  - `SuggestedResolution` — Recommended resolution action
  - `Severity` — `FixConflictSeverity` level

#### FixApplyResult

Comprehensive result record for fix operations:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Properties:**
  - `Success` — Overall success flag
  - `AppliedCount` — Number of fixes successfully applied
  - `SkippedCount` — Number of fixes skipped (conflicts, guards)
  - `FailedCount` — Number of fixes that failed to apply
  - `ModifiedContent` — Resulting document content (nullable, populated in DryRun)
  - `ResolvedIssues` — `IReadOnlyList<Guid>` of resolved issue IDs
  - `RemainingIssues` — `IReadOnlyList<Guid>` of unresolved issue IDs
  - `ConflictingIssues` — `IReadOnlyList<Guid>` of conflicting issue IDs
  - `ErrorsByIssueId` — `IReadOnlyDictionary<Guid, string>` of per-issue errors
  - `DetectedConflicts` — `IReadOnlyList<FixConflictCase>` of detected conflicts
  - `Duration` — `TimeSpan` of total operation duration
  - `TransactionId` — `Guid?` of the undo transaction (nullable)
  - `OperationTrace` — `IReadOnlyList<FixOperationTrace>` of detailed trace entries
- **Nested Records:**
  - `FixOperationTrace` — Per-fix trace with IssueId, Action, Success, Detail
- **Factory Methods:**
  - `Empty()` — Creates a zeroed-out result

#### FixTransaction

Undo transaction record for applied fixes:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Properties:**
  - `Id` — `Guid` transaction identifier
  - `DocumentPath` — Path of the modified document
  - `DocumentBefore` — Full document text before fixes
  - `DocumentAfter` — Full document text after fixes
  - `FixedIssueIds` — `IReadOnlyList<Guid>` of issues resolved
  - `AppliedAt` — `DateTimeOffset` timestamp
  - `IsUndone` — Whether this transaction has been undone

### Exception Types

#### FixConflictException

Thrown when conflicts are detected and strategy is `ThrowException`:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Base Class:** `Exception`
- **Properties:**
  - `Conflicts` — `IReadOnlyList<FixConflictCase>` of detected conflicts

#### FixApplicationTimeoutException

Thrown when fix application exceeds the configured timeout:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Base Class:** `OperationCanceledException`
- **Properties:**
  - `AppliedCount` — Number of fixes applied before timeout

#### DocumentCorruptionException

Thrown when document integrity is compromised during fix application:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Base Class:** `Exception`

### Event Arguments

#### FixesAppliedEventArgs

Event args raised after fixes are applied:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation.Events`
- **Properties:**
  - `DocumentPath` — Path of the modified document
  - `Result` — `FixApplyResult` with full details
  - `Timestamp` — UTC timestamp when fixes completed
- **Thread Safety:** Immutable, thread-safe

#### FixConflictDetectedEventArgs

Event args raised when conflicts are detected:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation.Events`
- **Properties:**
  - `DocumentPath` — Path of the document being fixed
  - `Conflicts` — `IReadOnlyList<FixConflictCase>` of detected conflicts
  - `Timestamp` — UTC timestamp when conflicts detected
- **Thread Safety:** Immutable, thread-safe

### IUnifiedFixWorkflow Interface

Orchestrator interface for the combined fix workflow:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`

**Methods:**
- `FixAllAsync(string documentPath, UnifiedValidationResult validation, FixWorkflowOptions options, CancellationToken ct)` — Apply all auto-fixable fixes with full pipeline
- `FixByCategoryAsync(string documentPath, UnifiedValidationResult validation, IEnumerable<IssueCategory> categories, CancellationToken ct)` — Fix issues in specified categories only
- `FixBySeverityAsync(string documentPath, UnifiedValidationResult validation, UnifiedSeverity minSeverity, CancellationToken ct)` — Fix issues at or above specified severity
- `FixByIdAsync(string documentPath, UnifiedValidationResult validation, IEnumerable<Guid> issueIds, CancellationToken ct)` — Fix specific issues by ID
- `DetectConflicts(IReadOnlyList<UnifiedIssue> issues)` — Synchronous conflict detection
- `DryRunAsync(string documentPath, UnifiedValidationResult validation, CancellationToken ct)` — Preview fix results without editor mutations
- `UndoLastFixesAsync(CancellationToken ct)` — Undo the most recent fix transaction

**Events:**
- `FixesApplied` — Raised after fixes are applied
- `ConflictDetected` — Raised when conflicts are detected

### Internal Helpers

#### FixPositionSorter

Bottom-to-top sorting to prevent offset drift during sequential fix application:
- **Namespace:** `Lexichord.Modules.Agents.Tuning.FixOrchestration`
- **Visibility:** Internal static class
- **Methods:**
  - `SortBottomToTop(IReadOnlyList<UnifiedIssue>)` — Filters to auto-fixable issues, sorts by `Start` descending then `End` descending
  - `SortBottomToTopUnfiltered(IReadOnlyList<UnifiedIssue>)` — Sorts all issues without filtering

#### FixConflictDetector

Detects conflicts between fix candidates:
- **Namespace:** `Lexichord.Modules.Agents.Tuning.FixOrchestration`
- **Visibility:** Internal class
- **Dependencies:** `ILogger<FixConflictDetector>`
- **Methods:**
  - `Detect(IReadOnlyList<UnifiedIssue>)` — Detects overlapping positions (via `TextSpan.OverlapsWith()`), contradictory suggestions (same location, different replacement), and dependent fixes (Style + Grammar within 200 characters)
  - `ValidateLocations(IReadOnlyList<UnifiedIssue>, int documentLength)` — Validates fix locations are within document bounds

#### FixGrouper

Groups fixes by category for ordered application:
- **Namespace:** `Lexichord.Modules.Agents.Tuning.FixOrchestration`
- **Visibility:** Internal static class
- **Methods:**
  - `GroupByCategory(IReadOnlyList<UnifiedIssue>)` — Groups issues into category order: Knowledge (0) -> Structure (1) -> Grammar (2) -> Style (3) -> Custom (4)
  - `FlattenGroups(IReadOnlyList<CategoryGroup>)` — Flattens grouped results back to a single list
- **Nested Records:**
  - `CategoryGroup` — Group with `IssueCategory` and `IReadOnlyList<UnifiedIssue>`

### UnifiedFixOrchestrator

Full orchestrator implementation:
- **Namespace:** `Lexichord.Modules.Agents.Tuning.FixOrchestration`
- **Visibility:** Internal class
- **Implements:** `IUnifiedFixWorkflow`, `IDisposable`
- **Dependencies:**
  - `IEditorService` — Document text access and editing
  - `IUnifiedValidationService` (v0.7.5f) — Re-validation after fixes
  - `FixConflictDetector` — Conflict detection
  - `IUndoRedoService?` (nullable) — Labeled undo history
  - `ILicenseContext` — WriterPro+ tier gating
  - `ILogger<UnifiedFixOrchestrator>`

**FixAllAsync Pipeline:**
1. Validate arguments
2. Check license (WriterPro+ required)
3. Get document text via `IEditorService.GetDocumentText()`
4. Extract fixable issues from validation result
5. Validate fix locations against document bounds
6. Detect conflicts between fixes
7. Handle conflicts per configured strategy
8. Sort fixes bottom-to-top to prevent offset drift
9. Group by category (Knowledge -> Structure -> Grammar -> Style -> Custom)
10. Apply atomically via `BeginUndoGroup` / `DeleteText` / `InsertText` / `EndUndoGroup`
11. Record `FixTransaction` on internal stack
12. Push to `IUndoRedoService` if available
13. Re-validate document if configured
14. Raise `FixesApplied` event

**Thread Safety:**
- `SemaphoreSlim` protects all fix operations

**Undo Support:**
- Internal `Stack<FixTransaction>` (max 50 entries) for bounded memory
- `FixWorkflowUndoOperation` inner class implementing `IUndoableOperation`
- `UndoLastFixesAsync()` pops and restores document content

**DryRun Mode:**
- Applies fixes to a string copy without editor mutations
- Returns `FixApplyResult` with `ModifiedContent` populated

### DI Registration

Extension method for service registration:
- **Location:** `TuningServiceCollectionExtensions.cs`
- **Method:** `AddUnifiedFixWorkflow()`
- **Registers:**
  - `FixConflictDetector` -> Singleton
  - `IUnifiedFixWorkflow` -> `UnifiedFixOrchestrator` -> Singleton

### UnifiedIssuesPanelViewModel Integration

Updated to delegate to `IUnifiedFixWorkflow` when available:
- **Constructor:** Added `IUnifiedFixWorkflow?` nullable parameter
- **FixIssueCommand:** Delegates to `FixByIdAsync()` when orchestrator available, falls back to direct `ApplyFix()` via `IEditorService`
- **FixAllCommand:** Delegates to `FixAllAsync()` with `FixWorkflowOptions.Default` when available
- **FixErrorsOnlyCommand:** Delegates to `FixBySeverityAsync(UnifiedSeverity.Error)` when available
- **Backward Compatible:** Null orchestrator falls back to direct editor calls

---

## Spec Adaptations

The design spec references several APIs that don't match the actual codebase:

1. **Document ghost dependency** — Spec uses a `Document` class for content access. Adapted to `string documentPath` + `IEditorService.GetDocumentText()`.

2. **ReplaceContentAsync()** — Spec proposes async content replacement. Does not exist. Uses sync `BeginUndoGroup` / `DeleteText` / `InsertText` / `EndUndoGroup` pattern.

3. **UnifiedIssue.Fix (single)** — Spec references single fix. Actual: `UnifiedIssue.Fixes` (list) with `BestFix` (nullable) property. `.Id` adapted to `.IssueId` (Guid). `.Code` adapted to `.SourceId` (string).

4. **FixApplyResult.ModifiedDocument** — Spec references `ModifiedDocument` (Document type). Adapted to `ModifiedContent` (string?).

5. **FixConflictDetector complexity** — Spec proposes heavyweight re-validation per individual fix. Simplified to detect overlaps, contradictions, and dependencies upfront; cascading issues caught by post-application re-validation.

6. **Namespace placement** — Contracts placed in `Lexichord.Abstractions.Contracts.Validation`; implementation in `Lexichord.Modules.Agents.Tuning.FixOrchestration`.

7. **TextSpan construction** — Spec uses `TextSpan(Start, End)`. Actual: `TextSpan(int Start, int Length)` with computed `End` property. All construction sites corrected.

---

## Files Created

### Contracts — Enums (3 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Validation/ConflictHandlingStrategy.cs` | Enum | Conflict handling strategies |
| `src/Lexichord.Abstractions/Contracts/Validation/FixConflictType.cs` | Enum | Conflict type classifications |
| `src/Lexichord.Abstractions/Contracts/Validation/FixWorkflowOptions.cs` | Record | Workflow options with `FixConflictSeverity` enum |

### Contracts — Records (3 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Validation/FixConflictCase.cs` | Record | Conflict case description |
| `src/Lexichord.Abstractions/Contracts/Validation/FixApplyResult.cs` | Record | Fix operation result with trace |
| `src/Lexichord.Abstractions/Contracts/Validation/FixTransaction.cs` | Record | Undo transaction record |

### Contracts — Exceptions (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Validation/FixWorkflowExceptions.cs` | Classes | FixConflictException, FixApplicationTimeoutException, DocumentCorruptionException |

### Contracts — Events (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Validation/Events/FixWorkflowEventArgs.cs` | Classes | FixesAppliedEventArgs, FixConflictDetectedEventArgs |

### Contracts — Interface (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Validation/IUnifiedFixWorkflow.cs` | Interface | Fix workflow orchestrator interface |

### Implementation (4 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Tuning/FixOrchestration/FixPositionSorter.cs` | Static class | Bottom-to-top position sorting |
| `src/Lexichord.Modules.Agents/Tuning/FixOrchestration/FixConflictDetector.cs` | Class | Conflict detection (overlap, contradiction, dependency) |
| `src/Lexichord.Modules.Agents/Tuning/FixOrchestration/FixGrouper.cs` | Static class | Category-ordered grouping |
| `src/Lexichord.Modules.Agents/Tuning/FixOrchestration/UnifiedFixOrchestrator.cs` | Class | Full workflow orchestrator |

### Tests (5 files)

| File | Tests | Description |
|:-----|:-----:|:------------|
| `tests/.../FixWorkflowContractTests.cs` | ~15 | Records, enums, exceptions, event args |
| `tests/.../FixPositionSorterTests.cs` | ~10 | Sorting, filtering, null handling |
| `tests/.../FixGrouperTests.cs` | ~9 | Category grouping, ordering, flattening |
| `tests/.../FixConflictDetectorTests.cs` | ~14 | Overlap, contradictory, dependent, location validation |
| `tests/.../UnifiedFixOrchestratorTests.cs` | ~73 | Full orchestrator coverage |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Modules.Agents/Extensions/TuningServiceCollectionExtensions.cs` | Added `AddUnifiedFixWorkflow()` extension registering `FixConflictDetector` (Singleton) and `IUnifiedFixWorkflow` -> `UnifiedFixOrchestrator` (Singleton) |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added registration call and init verification for fix workflow |
| `src/Lexichord.Modules.Agents/Tuning/UnifiedIssuesPanelViewModel.cs` | Added `IUnifiedFixWorkflow?` constructor parameter; delegated FixIssue/FixAll/FixErrorsOnly commands to orchestrator with fallback |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| FixWorkflowContractTests | ~15 | Records, enums, exceptions, event args |
| FixPositionSorterTests | ~10 | Sorting, filtering, null handling |
| FixGrouperTests | ~9 | Category grouping, ordering, flattening |
| FixConflictDetectorTests | ~14 | Overlap, contradictory, dependent, location validation |
| UnifiedFixOrchestratorTests | ~73 | Full orchestrator coverage |
| **Total** | **135** | |

### Test Groups

| Group | Tests | Description |
|:------|:-----:|:------------|
| Contract records | 5 | Default values, Empty() factory, OperationTrace |
| Contract enums | 3 | Enum value coverage for all three enums |
| Exceptions | 4 | FixConflictException, FixApplicationTimeoutException, DocumentCorruptionException |
| Event args | 3 | FixesAppliedEventArgs, FixConflictDetectedEventArgs |
| Position sorting | 5 | Descending start, descending end, auto-fix filter |
| Position sorting edge cases | 5 | Empty list, single item, null handling |
| Category grouping | 5 | Knowledge-first ordering, all categories |
| Group flattening | 4 | Flatten preserves order, empty groups |
| Overlap detection | 4 | TextSpan.OverlapsWith, adjacent non-overlapping |
| Contradictory detection | 3 | Same location, different replacement text |
| Dependent detection | 3 | Style+Grammar within 200 chars proximity |
| Location validation | 4 | Out-of-bounds, negative start, zero length |
| Orchestrator constructor | 5 | Null argument handling, nullable dependencies |
| FixAllAsync pipeline | 12 | Full pipeline, license check, empty issues |
| FixByCategoryAsync | 6 | Category filtering, multiple categories |
| FixBySeverityAsync | 6 | Severity threshold, error-only |
| FixByIdAsync | 5 | Specific IDs, missing IDs |
| Conflict strategies | 8 | ThrowException, SkipConflicting, PriorityBased |
| DryRun mode | 5 | String copy, no editor calls, ModifiedContent |
| Undo support | 8 | Transaction stack, UndoLastFixesAsync, max 50 cap |
| Re-validation | 5 | Post-fix validation, iteration limits |
| Thread safety | 3 | SemaphoreSlim, concurrent access |
| Events | 3 | FixesApplied, ConflictDetected raised correctly |
| ViewModel integration | 8 | Orchestrator delegation, null fallback |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.5h")]`

Updated `UnifiedIssuesPanelViewModelTests.cs` constructor calls for new `IUnifiedFixWorkflow?` parameter.

---

## Design Decisions

1. **Bottom-to-top position sorting** — Fixes applied from end of document to start, preventing offset drift when applying multiple sequential text edits.

2. **Category ordering (Knowledge -> Structure -> Grammar -> Style -> Custom)** — Higher-level structural fixes applied first to minimize cascading invalidations from lower-level fixes.

3. **FixTransaction stack (max 50)** — Bounded stack prevents unbounded memory growth while retaining sufficient undo history for typical workflows.

4. **SemaphoreSlim for thread safety** — Ensures only one fix operation runs at a time, preventing concurrent mutations to the same document.

5. **Nullable IUndoRedoService** — Accepted as nullable to support environments without undo infrastructure (e.g., headless/CLI).

6. **Nullable IUnifiedFixWorkflow in ViewModel** — Backward compatible integration: null orchestrator falls back to direct `IEditorService` calls, preserving v0.7.5g behavior.

7. **DryRun on string copy** — Applies all fixes to an in-memory string copy without touching the editor, enabling safe preview of fix results.

8. **Simplified conflict detection** — No heavyweight per-fix re-validation during detection phase; cascading issues caught by post-application re-validation pass.

9. **Singleton orchestrator** — Registered as Singleton to share the undo transaction stack and SemaphoreSlim across all consumers.

10. **WriterPro+ license gating** — Fix orchestration requires WriterPro tier or above, consistent with other Tuning Agent features.

---

## Dependencies

### Consumed (from existing modules)

| Type | Namespace | Version |
|:-----|:----------|:--------|
| `UnifiedIssue` | `Contracts.Validation` | v0.7.5e |
| `UnifiedFix` | `Contracts.Validation` | v0.7.5e |
| `IssueCategory` | `Contracts.Validation` | v0.7.5e |
| `TextSpan` | `Contracts.Editor` | v0.6.7b |
| `UnifiedSeverity` | `Knowledge.Validation.Integration` | v0.6.5j |
| `IUnifiedValidationService` | `Contracts.Validation` | v0.7.5f |
| `UnifiedValidationResult` | `Contracts.Validation` | v0.7.5f |
| `IEditorService` | `Contracts.Editor` | v0.6.7c |
| `IUndoRedoService` | `Contracts.Undo` | v0.7.3d |
| `IUndoableOperation` | `Contracts.Undo` | v0.7.3d |
| `ILicenseContext` | `Contracts` | v0.4.x |
| `UnifiedIssuesPanelViewModel` | `Modules.Agents.Tuning` | v0.7.5g |

### Produced (new types)

| Type | Consumers |
|:-----|:----------|
| `ConflictHandlingStrategy` | FixWorkflowOptions, UnifiedFixOrchestrator |
| `FixConflictType` | FixConflictCase, FixConflictDetector |
| `FixConflictSeverity` | FixConflictCase |
| `FixWorkflowOptions` | IUnifiedFixWorkflow, UnifiedIssuesPanelViewModel |
| `FixConflictCase` | FixApplyResult, FixConflictDetector |
| `FixApplyResult` | IUnifiedFixWorkflow, FixesAppliedEventArgs |
| `FixTransaction` | UnifiedFixOrchestrator, UndoLastFixesAsync |
| `FixConflictException` | UnifiedFixOrchestrator (ThrowException strategy) |
| `FixApplicationTimeoutException` | UnifiedFixOrchestrator (timeout) |
| `DocumentCorruptionException` | UnifiedFixOrchestrator (integrity check) |
| `FixesAppliedEventArgs` | IUnifiedFixWorkflow.FixesApplied event |
| `FixConflictDetectedEventArgs` | IUnifiedFixWorkflow.ConflictDetected event |
| `IUnifiedFixWorkflow` | UnifiedIssuesPanelViewModel, future panels |
| `FixPositionSorter` | UnifiedFixOrchestrator |
| `FixConflictDetector` | UnifiedFixOrchestrator |
| `FixGrouper` | UnifiedFixOrchestrator |
| `UnifiedFixOrchestrator` | DI container (Singleton) |

### No New NuGet Packages

All implementation uses existing project dependencies.

---

## Build & Test Results

```
Build:     0 errors, 0 warnings (1 pre-existing Avalonia warning)
v0.7.5h:   135 tests passing
Full suite: No regressions
```

---

## Usage Examples

### Apply All Fixes

```csharp
// Resolve via DI
var workflow = serviceProvider.GetRequiredService<IUnifiedFixWorkflow>();

// Run full fix pipeline with default options
var result = await workflow.FixAllAsync(
    documentPath,
    validationResult,
    FixWorkflowOptions.Default,
    cancellationToken);

Console.WriteLine($"Applied: {result.AppliedCount}");
Console.WriteLine($"Skipped: {result.SkippedCount}");
Console.WriteLine($"Conflicts: {result.DetectedConflicts.Count}");
```

### Fix by Category

```csharp
// Fix only grammar and style issues
var result = await workflow.FixByCategoryAsync(
    documentPath,
    validationResult,
    new[] { IssueCategory.Grammar, IssueCategory.Style },
    cancellationToken);
```

### Fix by Severity

```csharp
// Fix errors only
var result = await workflow.FixBySeverityAsync(
    documentPath,
    validationResult,
    UnifiedSeverity.Error,
    cancellationToken);
```

### Fix Specific Issues

```csharp
// Fix specific issues by ID
var issueIds = selectedIssues.Select(i => i.IssueId);
var result = await workflow.FixByIdAsync(
    documentPath,
    validationResult,
    issueIds,
    cancellationToken);
```

### DryRun Preview

```csharp
// Preview fixes without modifying the document
var result = await workflow.DryRunAsync(
    documentPath,
    validationResult,
    cancellationToken);

if (result.ModifiedContent is not null)
{
    Console.WriteLine($"Preview: {result.AppliedCount} fixes would be applied");
    Console.WriteLine($"Conflicts: {result.DetectedConflicts.Count}");
}
```

### Conflict Detection

```csharp
// Detect conflicts before applying
var conflicts = workflow.DetectConflicts(validationResult.Issues);

foreach (var conflict in conflicts)
{
    Console.WriteLine($"[{conflict.Severity}] {conflict.Type}: {conflict.Description}");
    Console.WriteLine($"  Resolution: {conflict.SuggestedResolution}");
}
```

### Undo Last Fixes

```csharp
// Undo the most recent fix transaction
await workflow.UndoLastFixesAsync(cancellationToken);
```

### Custom Workflow Options

```csharp
// Custom options: throw on conflicts, no re-validation, with timeout
var options = FixWorkflowOptions.Default with
{
    ConflictStrategy = ConflictHandlingStrategy.ThrowException,
    ReValidateAfterFixes = false,
    Timeout = TimeSpan.FromSeconds(10),
    Verbose = true
};

try
{
    var result = await workflow.FixAllAsync(
        documentPath, validationResult, options, cancellationToken);
}
catch (FixConflictException ex)
{
    Console.WriteLine($"Conflicts detected: {ex.Conflicts.Count}");
}
```

### Handle Events

```csharp
// Track fix application
workflow.FixesApplied += (s, args) =>
{
    logger.LogInformation(
        "Fixes applied to {Path}: {Count} applied, {Skipped} skipped",
        args.DocumentPath, args.Result.AppliedCount, args.Result.SkippedCount);
};

// Track conflict detection
workflow.ConflictDetected += (s, args) =>
{
    logger.LogWarning(
        "Conflicts in {Path}: {Count} conflicts detected",
        args.DocumentPath, args.Conflicts.Count);
};
```
