# Changelog: v0.2.3c - The Scanner (Regex)

**Date:** 2026-01-30  
**Spec:** [LCS-DES-023c](../specs/v0.2.x/v0.2.3/LCS-DES-023c.md)  
**Scope:** Lexichord Linter Engine - Pattern Matching Service

---

## Summary

Implements the Scanner service for the Linter Engine, providing optimized pattern matching with LRU caching and ReDoS protection. The Scanner is a shared service used by the linting orchestrator to efficiently match style rules against document content.

## Changes

### Abstractions (`Lexichord.Abstractions`)

#### Added

- **`IScannerService`** interface with methods:
    - `ScanAsync(content, rule, cancellationToken)` - Single rule scanning
    - `ScanBatchAsync(content, rules, cancellationToken)` - Batch optimization
    - `GetStatistics()` - Cache performance metrics
    - `ClearCache()` - Reset cached patterns
- **`ScannerResult`** record capturing:
    - List of `PatternMatchSpan` (offset + length)
    - Scan duration metrics
    - Cache hit/miss indicator
- **`PatternMatchSpan`** record for match positions
- **`ScannerStatistics`** record for cache metrics (hit ratio, sizes)

#### Modified

- **`ILintingConfiguration`** extended with:
    - `PatternCacheMaxSize` (default: 500)
    - `PatternTimeoutMilliseconds` (default: 100ms)
    - `UseComplexityAnalysis` (default: true)
- **`LintingOptions`** now implements `ILintingConfiguration`

---

### Style Module (`Lexichord.Modules.Style`)

#### Added

- **`ScannerService`** implementation:
    - LRU caching of compiled regex patterns
    - Configurable timeout for ReDoS protection
    - Support for all `PatternType` variants (Regex, Literal, Contains, etc.)
    - Batch scanning with parallel execution for large rule sets
    - Thread-safe statistics tracking
- **`PatternCache<TKey, TValue>`** internal LRU cache:
    - Fixed capacity with automatic eviction
    - Hit/miss tracking for performance monitoring
    - Thread-safe via `ConcurrentDictionary`
- **`PatternComplexityAnalyzer`** for ReDoS detection:
    - Heuristic detection of nested quantifiers `(a+)+`
    - Detection of overlapping alternation patterns
    - Configurable blocking of dangerous patterns

#### Modified

- **`StyleModule.RegisterServices`** updated to register:
    - `ILintingConfiguration` from `LintingOptions`
    - `IScannerService` as singleton

---

## Unit Tests

### Added (37 tests)

- **`ScannerServiceTests`** (15 tests):
    - Basic regex and literal pattern matching
    - Cache hit/miss verification
    - Batch scanning behavior
    - ReDoS protection with dangerous patterns
    - Statistics accuracy
- **`PatternCacheTests`** (12 tests):
    - LRU eviction behavior
    - Statistics tracking
    - Edge cases (capacity bounds)
- **`PatternComplexityAnalyzerTests`** (10 tests):
    - Detection of nested quantifiers
    - Safe pattern passthrough
    - Analysis result properties

---

## Configuration

New settings in `LintingOptions`:

| Property                     | Type | Default | Description                    |
| ---------------------------- | ---- | ------- | ------------------------------ |
| `PatternCacheMaxSize`        | int  | 500     | Max compiled patterns to cache |
| `PatternTimeoutMilliseconds` | int  | 100     | Per-pattern match timeout      |
| `UseComplexityAnalysis`      | bool | true    | Enable ReDoS heuristics        |

---

## Dependencies

No new NuGet packages required. Uses existing:

- `System.Text.RegularExpressions` for pattern matching
- `System.Collections.Concurrent` for thread-safe cache

---

## Breaking Changes

None. This version adds new abstractions and implementations without modifying existing public APIs.

---

## Next Steps

- v0.2.3d: Integrate Scanner into LintingOrchestrator scan pipeline
- v0.2.3e: Add progressive result streaming
