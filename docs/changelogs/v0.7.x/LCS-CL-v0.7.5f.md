# Changelog: v0.7.5f — Issue Aggregator

**Feature ID:** AGT-075f
**Version:** 0.7.5f
**Date:** 2026-02-16
**Status:** Complete

---

## Overview

Implements the Issue Aggregator service for the Unified Validation feature, providing a single entry point for running multiple validators and aggregating their results. This is the sixth sub-part of v0.7.5 "The Tuning Agent" and builds upon v0.7.5e's Unified Issue Model.

The implementation adds:
- `IUnifiedValidationService` — Service interface for aggregated validation
- `UnifiedValidationOptions` — Configuration record for validation behavior
- `UnifiedValidationResult` — Combined result with groupings and metrics
- `ValidationCompletedEventArgs` — Event args for validation completion
- `UnifiedValidationService` — Implementation with parallel execution, caching, deduplication
- DI registration via `AddUnifiedValidationService()` extension
- Full unit test coverage (80+ tests)

---

## What's New

### IUnifiedValidationService Interface

Service interface for aggregated validation:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Methods:**
  - `ValidateAsync()` — Validate a document using all enabled validators
  - `ValidateRangeAsync()` — Validate only issues within a text range
  - `GetCachedResultAsync()` — Retrieve a cached validation result
  - `InvalidateCache()` — Invalidate cache for a specific document
  - `InvalidateAllCaches()` — Clear all validation caches
- **Events:**
  - `ValidationCompleted` — Raised when validation finishes

### UnifiedValidationOptions Record

Configuration for validation behavior:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Validator Selection:**
  - `IncludeStyleLinter` — Run Style Linter (default: true)
  - `IncludeGrammarLinter` — Run Grammar Linter (default: true)
  - `IncludeValidationEngine` — Run CKVS Validation Engine (default: true)
- **Filtering:**
  - `MinimumSeverity` — Minimum severity to include (default: Hint)
  - `FilterByCategory` — Categories to include (default: all)
- **Caching:**
  - `EnableCaching` — Enable result caching (default: true)
  - `CacheTtlMs` — Cache time-to-live (default: 300,000ms / 5 min)
- **Execution:**
  - `ParallelValidation` — Run validators in parallel (default: true)
  - `ValidatorTimeoutMs` — Per-validator timeout (default: 30,000ms)
- **Processing:**
  - `EnableDeduplication` — Deduplicate across validators (default: true)
  - `MaxIssuesPerDocument` — Max issues to return (default: 1000)
  - `IncludeFixes` — Include fix suggestions (default: true)
- **Helper Methods:**
  - `PassesSeverityFilter()` — Check if severity passes filter
  - `PassesCategoryFilter()` — Check if category passes filter
- **Computed Properties:**
  - `CacheTtl` — CacheTtlMs as TimeSpan
  - `ValidatorTimeout` — ValidatorTimeoutMs as TimeSpan

### UnifiedValidationResult Record

Combined validation result:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Properties:**
  - `DocumentPath` — Validated document path
  - `Issues` — List of UnifiedIssue from all validators
  - `Duration` — Total validation duration
  - `ValidatedAt` — Timestamp of validation
  - `IsCached` — Whether result is from cache
  - `Options` — Options used for validation
  - `ValidatorDetails` — Per-validator diagnostics (debug)
- **Computed Groupings:**
  - `ByCategory` — Issues grouped by IssueCategory
  - `BySeverity` — Issues grouped by UnifiedSeverity
  - `CountBySeverity` — Issue count per severity
  - `CountBySourceType` — Issue count per source validator
- **Computed Metrics:**
  - `TotalIssueCount` — Total number of issues
  - `ErrorCount`, `WarningCount`, `InfoCount`, `HintCount`
  - `AutoFixableCount` — Issues with auto-fix available
  - `CanPublish` — True when no error-level issues
  - `IsEmpty`, `HasIssues` — Convenience properties
- **Factory Methods:**
  - `Empty()` — Create empty result for a document
- **Instance Methods:**
  - `AsCached()` — Create copy marked as cached

### ValidationCompletedEventArgs Class

Event arguments for validation completion:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Properties:**
  - `DocumentPath` — Validated document path
  - `Result` — The validation result
  - `SuccessfulValidators` — Names of validators that completed
  - `FailedValidators` — Map of failed validators to error messages
  - `CompletedAt` — Timestamp when validation completed
- **Computed Properties:**
  - `AllValidatorsSucceeded` — True when no failures
  - `HasFailures` — True when any validator failed
- **Factory Methods:**
  - `Success()` — Create event args with no failures
  - `WithFailures()` — Create event args with failure details

### UnifiedValidationService Implementation

Service that aggregates validation results:
- **Namespace:** `Lexichord.Modules.Agents.Tuning`
- **Dependencies:**
  - `IStyleDeviationScanner` — Style Linter (v0.7.5a)
  - `IValidationEngine` — CKVS Validation (v0.6.5e)
  - `ILicenseContext` — License tier checking
  - `IMemoryCache` — Result caching
  - `ILogger<UnifiedValidationService>`
- **License Gating:**
  - Core: Style Linter only
  - WriterPro: Style + Grammar Linter
  - Teams/Enterprise: All validators including CKVS
- **Key Features:**
  - Parallel execution with per-validator timeout
  - Result caching with TTL and options validation
  - Deduplication across validators (±1 char tolerance)
  - Severity and category filtering
  - Issue limit truncation (by severity order)
  - Graceful error handling (validator failures don't block others)
  - Thread-safe via SemaphoreSlim
- **Implements:** `IUnifiedValidationService`, `IDisposable`

### DI Registration

Extension method for service registration:
- **Location:** `TuningServiceCollectionExtensions.cs`
- **Method:** `AddUnifiedValidationService()`
- **Registers:**
  - `IUnifiedValidationService` → `UnifiedValidationService` (Singleton)

---

## Spec Adaptations

The design spec (LCS-DES-v0.7.5-KG-f.md) references several APIs that don't match the actual codebase:

1. **Document Class** — Spec proposes `Document` class parameter. Adapted to use `(string documentPath, string content)` pair.

2. **IStyleLinter** — Spec references `IStyleLinter`. Actual type: `IStyleDeviationScanner` (v0.7.5a).

3. **IGrammarLinter** — Does not exist yet. Accepted as nullable placeholder (returns empty results).

4. **IValidationEngine.ValidateAsync(Document)** — Spec API doesn't match. Actual method: `ValidateDocumentAsync(ValidationContext, CancellationToken)`.

5. **LicenseTier.GetCurrentTierAsync()** — Spec proposes async method. Actual: `GetCurrentTier()` (synchronous).

6. **UnifiedIssue.Fix** — Spec references single fix. Actual: `UnifiedIssue.Fixes` (list).

7. **Module Location** — Spec proposes `Lexichord.Modules.Validation`. Placed in existing `Lexichord.Modules.Agents.Tuning`.

---

## Files Created

### Contracts (4 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Validation/IUnifiedValidationService.cs` | Interface | Service contract |
| `src/Lexichord.Abstractions/Contracts/Validation/UnifiedValidationOptions.cs` | Record | Options configuration |
| `src/Lexichord.Abstractions/Contracts/Validation/UnifiedValidationResult.cs` | Record | Combined result |
| `src/Lexichord.Abstractions/Contracts/Validation/ValidationCompletedEventArgs.cs` | Class | Event args |

### Implementation (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Tuning/UnifiedValidationService.cs` | Class | Service implementation |

### Tests (1 file)

| File | Tests | Description |
|:-----|:-----:|:------------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/Tuning/UnifiedValidationServiceTests.cs` | 80+ | Comprehensive service tests |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Modules.Agents/Extensions/TuningServiceCollectionExtensions.cs` | Added `AddUnifiedValidationService()` extension |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added registration and verification calls |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| UnifiedValidationServiceTests | 80+ | All service functionality |

### Test Groups

| Group | Tests | Description |
|:------|:-----:|:------------|
| Constructor validation | 6 | Null argument handling |
| ValidateAsync - argument validation | 4 | Null/disposed checks |
| ValidateAsync - combines results | 8 | Single/multi source aggregation |
| ValidateAsync - deduplication | 6 | Same location, tolerance, disabled |
| ValidateAsync - license filtering | 8 | Core, WriterPro, Teams tiers |
| ValidateAsync - timeout handling | 2 | Partial results on timeout |
| ValidateAsync - crash handling | 3 | Graceful validator failure |
| ValidateAsync - caching | 5 | Cache hit, miss, invalidation |
| ValidateRangeAsync | 3 | Range filtering, not cached |
| Cache invalidation | 3 | Single, all, after validation |
| UnifiedValidationOptions | 10 | Defaults, filters, computed props |
| UnifiedValidationResult | 8 | Groupings, counts, CanPublish |
| ValidationCompletedEventArgs | 3 | Success, failures, timestamps |
| Event publishing | 3 | Event raised, success/failure |
| Parallel vs sequential | 2 | Concurrent execution test |
| MaxIssuesPerDocument | 3 | Truncation, unlimited, severity order |
| Severity/category filtering | 2 | Filter application |
| Dispose behavior | 2 | Multiple dispose, post-dispose |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.5f")]`

---

## Design Decisions

1. **Singleton Lifetime** — `UnifiedValidationService` registered as singleton for shared caching across requests.

2. **Grammar Linter Placeholder** — Returns empty results since `IGrammarLinter` doesn't exist yet. When implemented, it will be added as a nullable constructor parameter.

3. **Deduplication Strategy** — Issues from different validators at the same location (±1 char) are deduplicated, keeping the highest-severity instance.

4. **Parallel by Default** — Validators run concurrently for better performance, with sequential option for debugging.

5. **Per-Validator Timeout** — Each validator has independent timeout; slow/crashed validators don't block others.

6. **Cache Key Strategy** — Cache keys include document path and validation timestamp for proper invalidation.

7. **Thread Safety** — `SemaphoreSlim` prevents duplicate concurrent validations of the same document.

8. **Event Pattern** — Standard EventHandler<T> pattern for `ValidationCompleted` event.

---

## Dependencies

### Consumed (from existing modules)

| Type | Namespace | Version |
|:-----|:----------|:--------|
| `UnifiedIssue` | `Contracts.Validation` | v0.7.5e |
| `UnifiedIssueFactory` | `Contracts.Validation` | v0.7.5e |
| `IssueCategory` | `Contracts.Validation` | v0.7.5e |
| `UnifiedSeverity` | `Knowledge.Validation.Integration` | v0.6.5j |
| `TextSpan` | `Contracts.Editor` | v0.6.7b |
| `IStyleDeviationScanner` | `Contracts.Agents` | v0.7.5a |
| `StyleDeviation` | `Contracts.Agents` | v0.7.5a |
| `StyleScanResult` | `Contracts.Agents` | v0.7.5a |
| `IValidationEngine` | `Contracts.Knowledge.Validation` | v0.6.5e |
| `ValidationContext` | `Contracts.Knowledge.Validation` | v0.6.5e |
| `ValidationResult` | `Contracts.Knowledge.Validation` | v0.6.5e |
| `ValidationFinding` | `Contracts.Knowledge.Validation` | v0.6.5e |
| `ILicenseContext` | `Contracts` | v0.4.x |
| `LicenseTier` | `Contracts` | v0.4.x |
| `IMemoryCache` | `Microsoft.Extensions.Caching.Memory` | - |

### Produced (new types)

| Type | Consumers |
|:-----|:----------|
| `IUnifiedValidationService` | Tuning Panel, Quick Fix Panel, Editor integration |
| `UnifiedValidationOptions` | All validation callers |
| `UnifiedValidationResult` | UI components, issue panels |
| `ValidationCompletedEventArgs` | Event subscribers |

---

## Build & Test Results

```
Build:     Succeeded (0 errors, 0 warnings)
v0.7.5f:   80+ passed, 0 failed
Full suite: TBD
```

---

## Usage Examples

### Basic Validation

```csharp
var validationService = serviceProvider.GetRequiredService<IUnifiedValidationService>();

var result = await validationService.ValidateAsync(
    documentPath: "/path/to/document.md",
    content: documentContent,
    options: UnifiedValidationOptions.Default);

Console.WriteLine($"Found {result.TotalIssueCount} issues");
Console.WriteLine($"Can publish: {result.CanPublish}");
```

### Custom Options

```csharp
var options = new UnifiedValidationOptions
{
    MinimumSeverity = UnifiedSeverity.Warning,  // Exclude Info/Hint
    IncludeValidationEngine = false,            // Skip CKVS
    MaxIssuesPerDocument = 50,                  // Limit results
    ParallelValidation = true,                  // Run concurrently
    ValidatorTimeoutMs = 10_000                 // 10-second timeout
};

var result = await validationService.ValidateAsync(path, content, options);
```

### Subscribe to Events

```csharp
validationService.ValidationCompleted += (sender, args) =>
{
    if (args.HasFailures)
    {
        foreach (var (validator, error) in args.FailedValidators)
        {
            logger.LogWarning("{Validator} failed: {Error}", validator, error);
        }
    }

    UpdateUI(args.Result);
};
```

### Range Validation

```csharp
// Validate only the selected text range
var range = new TextSpan(start: 100, length: 200);
var result = await validationService.ValidateRangeAsync(
    path, content, range, UnifiedValidationOptions.Default);
```

### Cache Management

```csharp
// Invalidate when document changes
validationService.InvalidateCache(documentPath);

// Clear all caches (e.g., on style sheet reload)
validationService.InvalidateAllCaches();

// Check for cached result before full validation
var cached = await validationService.GetCachedResultAsync(documentPath);
if (cached is not null)
{
    return cached;
}
```
