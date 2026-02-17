# Changelog: v0.7.5a — Style Deviation Scanner

**Feature ID:** AGT-075a
**Version:** 0.7.5a
**Date:** 2026-02-15
**Status:** Complete

---

## Overview

Implements the Style Deviation Scanner for the Tuning Agent, bridging the linting infrastructure (v0.2.3a) with AI-powered fix generation. This is the first sub-part of v0.7.5 "The Tuning Agent" and provides enriched violation context needed for intelligent fix suggestions.

The implementation adds:
- `IStyleDeviationScanner` — interface for document and range scanning with caching and events
- `StyleDeviationScanner` — core service with MediatR event handlers for real-time updates
- `StyleDeviation` — enriched violation record with surrounding context and rule details
- `DeviationScanResult` — scan results with grouping helpers and cache metadata
- `DeviationPriority` — priority mapping from lint severity
- `ScannerOptions` — configurable context window, cache TTL, and filtering options
- Full unit test coverage

---

## What's New

### IStyleDeviationScanner Interface

Defines the contract for style deviation scanning:
- **Namespace:** `Lexichord.Abstractions.Contracts.Agents`
- **Methods:**
  - `ScanDocumentAsync()` — Scans entire document with caching support
  - `ScanRangeAsync()` — Scans specific text range (no caching)
  - `GetCachedResultAsync()` — Retrieves cached result if valid
  - `InvalidateCache()` — Invalidates cache for specific document
  - `InvalidateAllCaches()` — Invalidates all cached results
- **Events:**
  - `DeviationsDetected` — Raised when new deviations are detected

### StyleDeviationScanner

Core implementation of `IStyleDeviationScanner`:
- **Namespace:** `Lexichord.Modules.Agents.Tuning`
- **Dependencies:** `ILintingOrchestrator`, `IEditorService`, `IMemoryCache`, `ILicenseContext`, `IOptions<ScannerOptions>`, `ILogger`
- **Implements:**
  - `IStyleDeviationScanner`
  - `INotificationHandler<LintingCompletedEvent>` — Real-time violation updates
  - `INotificationHandler<StyleSheetReloadedEvent>` — Global cache invalidation
  - `IDisposable` — Resource cleanup
- **Key Features:**
  - License validation (requires WriterPro tier)
  - Content-hash-based cache key generation
  - Auto-fixability determination based on rule properties
  - Priority mapping from ViolationSeverity
  - Surrounding context extraction with sentence boundary adjustment
  - Semaphore-based thread safety for concurrent scans

### StyleDeviation Record

Enriched violation data for AI context:
- **Namespace:** `Lexichord.Abstractions.Contracts.Agents`
- **Properties:**
  - `DeviationId` — Unique identifier (Guid)
  - `Violation` — Original StyleViolation from linting
  - `Location` — TextSpan of the violation
  - `OriginalText` — The violating text
  - `SurroundingContext` — Context window for AI understanding
  - `ViolatedRule` — StyleRule details (nullable)
  - `IsAutoFixable` — Whether AI can fix this violation
  - `Priority` — DeviationPriority level
- **Computed Properties:**
  - `Category` — Rule category or "General"
  - `RuleId` — Rule identifier
  - `Message` — Human-readable violation message
  - `LinterSuggestedFix` — Optional linter suggestion

### DeviationScanResult Record

Aggregate scan results with helpers:
- **Namespace:** `Lexichord.Abstractions.Contracts.Agents`
- **Properties:**
  - `DocumentPath` — Scanned document path
  - `Deviations` — All detected deviations
  - `ScannedAt` — Scan timestamp
  - `ScanDuration` — Processing time
  - `IsCached` — Whether from cache
  - `ContentHash` — Document content hash
  - `RulesVersion` — Style rules version
- **Computed Properties:**
  - `TotalCount` — Total deviation count
  - `AutoFixableCount` — AI-fixable count
  - `ManualOnlyCount` — Manual-only count
  - `ByCategory` — Grouped by category
  - `ByPriority` — Grouped by priority
  - `ByPosition` — Sorted by document position
- **Factory Methods:**
  - `Empty()` — Creates empty result
  - `LicenseRequired()` — Creates result for unlicensed users

### DeviationPriority Enum

Priority levels mapped from lint severity:
- **Values:**
  - `Low = 0` — Hints
  - `Normal = 1` — Information
  - `High = 2` — Warnings
  - `Critical = 3` — Errors

### DeviationsDetectedEventArgs

Event arguments for deviation detection:
- **Properties:**
  - `DocumentPath` — Document with deviations
  - `NewDeviations` — Newly detected deviations
  - `TotalDeviationCount` — Total count
  - `IsIncremental` — Whether from incremental update
- **Computed Properties:**
  - `NewDeviationCount` — Count of new deviations
  - `HasNewDeviations` — Whether any new deviations exist

### ScannerOptions Configuration

Configurable scanner behavior:
- **Namespace:** `Lexichord.Modules.Agents.Tuning.Configuration`
- **Properties:**
  - `ContextWindowSize = 500` — Characters before/after violation
  - `CacheTtlMinutes = 5` — Cache time-to-live
  - `MaxDeviationsPerScan = 100` — Maximum deviations per scan
  - `IncludeManualOnly = true` — Include non-auto-fixable
  - `MinimumSeverity = Hint` — Minimum severity filter
  - `EnableRealTimeUpdates = true` — Subscribe to linting events
  - `ExcludedCategories = []` — Categories to exclude

### DI Registration

Extended `TuningServiceCollectionExtensions` with `AddStyleDeviationScanner()`:
```csharp
services.Configure<ScannerOptions>(options => configure?.Invoke(options));
services.AddSingleton<IStyleDeviationScanner, StyleDeviationScanner>();
```

Updated `AgentsModule.RegisterServices()` with `services.AddStyleDeviationScanner()` call and version bump to 0.7.5.

---

## Files Created

### Abstractions — Enums (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Agents/DeviationPriority.cs` | Enum | Priority levels |

### Abstractions — Records (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Agents/StyleDeviation.cs` | Record | Enriched violation data |
| `src/Lexichord.Abstractions/Contracts/Agents/DeviationScanResult.cs` | Record | Scan result container |

### Abstractions — Events (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Agents/DeviationsDetectedEventArgs.cs` | Class | Detection event args |

### Abstractions — Interface (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Agents/IStyleDeviationScanner.cs` | Interface | Scanner contract |

### Module — Configuration (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Tuning/Configuration/ScannerOptions.cs` | Class | Scanner options |

### Module — Service (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Tuning/StyleDeviationScanner.cs` | Class | Core implementation |

### Module — DI Extension (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Extensions/TuningServiceCollectionExtensions.cs` | Class | DI registration |

### Tests (1 file)

| File | Tests | Description |
|:-----|:-----:|:------------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/Tuning/StyleDeviationScannerTests.cs` | 33 | Scanner tests |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added `services.AddStyleDeviationScanner()` call, bumped version to 0.7.5 |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| StyleDeviationScannerTests | 33 | Constructor, scan, cache, events, license |
| **Total v0.7.5a** | **33** | All v0.7.5a functionality |

### Test Groups

| Group | Tests | Description |
|:------|:------|:------------|
| Constructor | 5 | Dependency null checks |
| ScanDocumentAsync | 5 | Success, empty, cached, license check |
| ScanRangeAsync | 3 | Range filtering, no caching |
| Cache | 5 | Hit/miss, invalidation, content hash |
| Auto-fixability | 5 | Various rule types and properties |
| Priority Mapping | 4 | Each severity level |
| Event Handlers | 3 | LintingCompleted, StyleSheetReloaded |
| Dispose | 3 | Resource cleanup verification |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.5a")]`

---

## Design Decisions

1. **Singleton Service Lifetime** — `StyleDeviationScanner` is registered as singleton for shared caching across the application.

2. **Content-Hash Cache Keys** — Cache keys include document content hash and rules version, ensuring automatic invalidation when content or rules change.

3. **Semaphore Lock** — Single-document scan lock prevents duplicate concurrent scans of the same document.

4. **Nullable ViolatedRule** — Rule lookup may fail if rule was deleted; deviations still work with null rule using defaults.

5. **MediatR Event Handlers** — Uses `INotificationHandler<T>` for real-time linting event subscription without tight coupling.

6. **License Gating** — Returns `LicenseRequired()` empty result for unlicensed users rather than throwing, enabling graceful UI degradation.

7. **Context Window Boundaries** — Context extraction attempts to expand to sentence/paragraph boundaries for better AI understanding.

8. **Auto-Fixability Heuristics** — Based on rule properties (`IsAiFixable`, `RuleType`, `Category`, `Complexity`) with sensible defaults.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Version | Used By |
|:----------|:--------|:--------|
| `ILintingOrchestrator` | v0.2.3a | Violation detection |
| `LintingCompletedEvent` | v0.2.3b | Real-time updates |
| `StyleSheetReloadedEvent` | v0.2.1d | Rules change notification |
| `StyleViolation` | v0.2.1b | Violation data |
| `StyleRule` | v0.2.1a | Rule details |
| `IEditorService` | v0.1.3a | Document content access |
| `ILicenseContext` | v0.0.4c | License validation |
| `TextSpan` | v0.1.0 | Location data |

### Produced (new interfaces/classes)

| Class | Consumers |
|:------|:----------|
| `IStyleDeviationScanner` | Tuning Agent (v0.7.5b+), UI commands |
| `StyleDeviation` | Fix generation pipeline |
| `DeviationScanResult` | UI display, analytics |
| `DeviationPriority` | Sorting, filtering |
| `DeviationsDetectedEventArgs` | UI real-time updates |
| `ScannerOptions` | DI configuration |

---

## Auto-Fixability Logic

```
IF rule.IsAiFixable == true → Auto-fixable
IF violation.SuggestedFix is not empty → Auto-fixable
IF rule.RuleType is PatternMatch or Terminology → Auto-fixable
IF rule.Category is "Grammar" or "Readability" or "Voice" → Auto-fixable
IF rule.RuleType is Structure or Formatting → Manual-only
IF rule.Complexity is High → Manual-only
ELSE → Auto-fixable (default)
```

---

## Priority Mapping

| ViolationSeverity | DeviationPriority |
|:------------------|:------------------|
| Error | Critical |
| Warning | High |
| Information | Normal |
| Hint | Low |

---

## License Gating

| Feature | Core | Writer | WriterPro | Teams |
|:--------|:-----|:-------|:----------|:------|
| Style deviation scanning | - | - | ✓ | ✓ |
| Cached results | - | - | ✓ | ✓ |
| Real-time updates | - | - | ✓ | ✓ |
| Range scanning | - | - | ✓ | ✓ |

---

## Build & Test Results

```
Build:     Succeeded (0 errors, 0 warnings)
v0.7.5a:   33 passed, 0 failed
```

