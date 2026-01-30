# Changelog: v0.2.3d - The Aggregator (Violation Aggregator)

**Date:** 2026-01-30  
**Spec:** [LCS-DES-023d](../specs/v0.2.x/v0.2.3/LCS-DES-023d.md)  
**Scope:** Lexichord Linter Engine - Violation Aggregation Service

---

## Summary

Implements the Violation Aggregator service for the Linter Engine, transforming raw scanner matches into user-facing `AggregatedStyleViolation` objects. The aggregator handles position calculation, deduplication, sorting, caching, and query methods for efficient violation management.

## Changes

### Abstractions (`Lexichord.Abstractions`)

#### Added

- **`ScanMatch.cs`** - Bridge record connecting Scanner output to Aggregator input
    - Contains: `RuleId`, `StartOffset`, `Length`, `MatchedText`, `Rule`, `CaptureGroups`
    - Factory method: `FromSpan(PatternMatchSpan, content, rule)` for easy creation
    - Computed: `EndOffset`

- **`AggregatedStyleViolation.cs`** - Rich violation record with position data
    - Contains: `Id`, `DocumentId`, `RuleId`, `StartOffset`, `Length`, `Line`, `Column`, `EndLine`, `EndColumn`, `ViolatingText`, `Message`, `Severity`, `Suggestion`, `Category`
    - Computed: `EndOffset`, `HasSuggestion`
    - Methods: `WithOffset(delta)`, `OverlapsWith(other)`, `ContainsOffset(offset)`
    - Designed for editor integration with 1-based line/column positions

- **`IViolationAggregator.cs`** - Core interface for violation aggregation
    - `Aggregate(matches, documentId, content)` → `IReadOnlyList<AggregatedStyleViolation>`
    - `ClearViolations(documentId)` - Cache invalidation
    - `GetViolations(documentId)` - Retrieve cached violations
    - `GetViolation(documentId, violationId)` - O(1) lookup by ID
    - `GetViolationAt(documentId, offset)` - Hover tooltip support
    - `GetViolationsInRange(documentId, start, end)` - Visible range rendering
    - `GetViolationCounts(documentId)` - Status bar badge support

#### Modified

- **`ILintingConfiguration.cs`** - Added `MaxViolationsPerDocument` property
    - Controls maximum violations reported per document
    - Prevents memory exhaustion on noisy documents
    - Default: 1000, Range: 100-10000

- **`LintingOptions.cs`** - Added `MaxViolationsPerDocument` property with default 1000

---

### Services (`Lexichord.Modules.Style`)

#### Added

- **`PositionIndex.cs`** - Efficient offset-to-line/column mapping
    - Pre-computes line start offsets during construction: O(n)
    - Binary search for position lookups: O(log n)
    - Thread-safe: Immutable after construction
    - Properties: `LineCount`
    - Methods: `GetPosition(offset)`, `GetSpanPositions(start, end)`

- **`MessageTemplateEngine.cs`** - Message template expansion
    - Static helper for placeholder substitution
    - Supports: `{text}`, `{0}`, `{suggestion}`, `{rule}`, `{severity}`
    - Supports named capture groups from regex patterns
    - Case-insensitive placeholder matching
    - Methods: `Expand(template, match, additionalValues?)`, `HasPlaceholders(template)`

- **`ViolationDiff.cs`** - Incremental update calculation
    - Static helper for calculating changes between violation sets
    - Enables efficient UI updates (only repaint changed violations)
    - Record: `ViolationChanges(Added, Removed, Modified)`

- **`ViolationAggregator.cs`** - Main aggregation implementation
    - Thread-safe with `ConcurrentDictionary<string, ViolationCache>` per document
    - Deduplication: Overlapping violations resolved by severity (highest wins)
    - Sorting: By line, then column
    - Caching: Results cached for fast retrieval without re-aggregation
    - Stable violation IDs: SHA256-based, deterministic across re-scans

#### Modified

- **`StyleModule.cs`** - Registered `IViolationAggregator` as singleton service

---

### Tests (`Lexichord.Tests.Unit`)

#### Added

- **`PositionIndexTests.cs`** - 20 tests
    - Basic position calculation (first char, middle of line, multi-line)
    - Edge cases (empty content, negative offset, beyond content, null)
    - LineCount property
    - GetSpanPositions for multi-line spans
    - Performance verification with 10,000 line document

- **`MessageTemplateEngineTests.cs`** - 20 tests
    - Basic placeholders (`{text}`, `{0}`, `{suggestion}`, `{rule}`, `{severity}`)
    - Multiple placeholders in single template
    - Capture group substitution
    - Additional values override
    - Edge cases (empty, null, unknown placeholders)
    - Case-insensitive matching

- **`ViolationDiffTests.cs`** - 11 tests
    - Empty list handling
    - Added/removed/modified violation detection
    - Combined change scenarios
    - ViolationChanges struct properties

- **`ViolationAggregatorTests.cs`** - 32 tests
    - Basic aggregation (empty, single, multiple matches)
    - Position calculation (first line, second line, multi-line spans)
    - Deduplication (overlapping, equal severity, non-overlapping)
    - Sorting (by line, by column)
    - Max violations limit
    - Caching operations
    - Query methods (GetViolation, GetViolationAt, GetViolationsInRange, GetViolationCounts)
    - Message expansion
    - Violation ID stability

---

## Dependencies

| Component               | Version | Purpose                             |
| :---------------------- | :------ | :---------------------------------- |
| `ILintingConfiguration` | v0.2.3b | Configuration for max violations    |
| `IScannerService`       | v0.2.3c | Produces `PatternMatchSpan` input   |
| `StyleRule`             | v0.2.1b | Rule metadata for messages/severity |
| `ViolationSeverity`     | v0.2.1b | Severity enum for deduplication     |
| `RuleCategory`          | v0.2.1b | Category for violation grouping     |

## Technical Notes

### Deduplication Algorithm

1. Sort violations by `StartOffset`
2. Iterate through, tracking current "winning" violation
3. For overlapping violations, compare severity
4. Higher severity wins; equal severity keeps earlier start

Severity ordering: `Error` > `Warning` > `Info` > `Hint`

### Position Calculation Performance

Binary search through pre-computed line starts achieves O(log n) per lookup.
For a 10,000 line document, this means ~14 comparisons instead of scanning thousands of characters.

### Violation ID Generation

IDs are generated using SHA256 hash of `{documentId}:{ruleId}:{startOffset}:{length}`, truncated to 12 hex characters. This ensures:

- Stable IDs across re-scans for same position/rule
- Different IDs for different positions
- Efficient O(1) lookup via dictionary

---

## Migration Notes

None required. This is a new service with no breaking changes to existing APIs.
