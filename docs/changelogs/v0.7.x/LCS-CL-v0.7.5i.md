# Changelog: v0.7.5i — Issue Filters

**Feature ID:** AGT-075i
**Version:** 0.7.5i
**Date:** 2026-02-16
**Status:** Complete

---

## Overview

Implements composable Issue Filters for the Tuning Agent, providing in-memory filtering, multi-criteria sorting, text search, wildcard code matching, and named preset management for unified validation issues. This is the ninth sub-part of v0.7.5 "The Tuning Agent" and builds upon v0.7.5e's Unified Issue Model, v0.7.5f's Issue Aggregator, and v0.7.5g's Unified Issues Panel.

The implementation adds:
- `FilterCriteria` — Abstract base record for composable filter predicates with AND-composition
- `CategoryFilterCriterion` — Matches issues by `IssueCategory`
- `SeverityFilterCriterion` — Matches by severity with corrected inverted-enum comparison
- `LocationFilterCriterion` — Matches by `TextSpan` region with partial overlap support
- `TextSearchFilterCriterion` — Case-insensitive substring search across Message, SourceId, SourceType
- `CodeFilterCriterion` — Wildcard pattern matching on SourceId (e.g., "STYLE_*")
- `IssueFilterOptions` — Configuration record with 15 filter/sort/pagination properties
- `SortCriteria` — Enum with 7 sort dimensions (Severity, Location, Category, Message, Code, ValidatorName, AutoFixable)
- `SortOptions` — Sort configuration record with primary/secondary/tertiary criteria
- `IIssueFilterService` — Interface with 14 methods for filtering, sorting, searching, counting, and preset management
- `IssueFilterService` — Full implementation with AND-composed criteria, OrderBy/ThenBy chain sorting, 6 default presets, pagination, and performance logging
- DI registration via `AddIssueFilterService()` extension
- Full unit test coverage (84 tests)

---

## What's New

### Filter Criteria

#### FilterCriteria (Abstract Base)

Composable filter predicate base for evaluating `UnifiedIssue` instances:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Type:** Abstract record
- **Methods:**
  - `Matches(UnifiedIssue issue)` — Abstract: evaluates this criterion against the issue
- **Thread Safety:** All subclasses are immutable records, inherently thread-safe

#### CategoryFilterCriterion

Matches issues by their category:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Parameters:** `IEnumerable<IssueCategory> Categories`
- **Logic:** `Categories.Contains(issue.Category)`

#### SeverityFilterCriterion

Matches issues by severity level with corrected comparison:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Parameters:** `UnifiedSeverity MinimumSeverity`
- **Logic:** `(int)issue.Severity <= (int)MinimumSeverity` (lower numeric = more severe)

#### LocationFilterCriterion

Matches issues by document location:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Parameters:** `TextSpan Location`, `bool IncludePartialOverlaps`
- **Logic:**
  - Partial overlaps: `issue.Location.OverlapsWith(Location)`
  - Strict containment: `issue.Location.Start >= Location.Start && issue.Location.End <= Location.End`

#### TextSearchFilterCriterion

Case-insensitive text search across issue fields:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Parameters:** `string SearchText`
- **Logic:** Matches if Message, SourceId, or SourceType contains the search text (OrdinalIgnoreCase)

#### CodeFilterCriterion

Wildcard pattern matching on source ID:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Parameters:** `IEnumerable<string> CodePatterns`
- **Logic:** Converts `*` wildcards to regex `.*` patterns, case-insensitive full-string match against SourceId

### Contract Records

#### IssueFilterOptions

Configuration for filtering, sorting, and paginating unified validation issues:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Properties:**
  - `Categories` — `IReadOnlyList<IssueCategory>`, empty = all pass
  - `MinimumSeverity` — `UnifiedSeverity`, default `Hint` (includes all)
  - `MaximumSeverity` — `UnifiedSeverity`, default `Error` (includes all)
  - `IssueCodes` — `IReadOnlyList<string>`, wildcard patterns to include
  - `ExcludeCodes` — `IReadOnlyList<string>`, wildcard patterns to exclude
  - `SearchText` — `string?`, case-insensitive text search
  - `LocationSpan` — `TextSpan?`, document region filter
  - `LineRange` — `(int StartLine, int EndLine)?`, approximate line range
  - `OnlyAutoFixable` — `bool`, filter to auto-fixable only
  - `OnlyManual` — `bool`, filter to manual-only
  - `SortBy` — `IReadOnlyList<SortCriteria>`, sort criteria chain
  - `SortAscending` — `bool`, default `true`
  - `ValidatorNames` — `IReadOnlyList<string>`, validator source type filter
  - `Limit` — `int`, pagination limit (0 = unlimited)
  - `Offset` — `int`, pagination offset
- **Static Properties:**
  - `Default` — No filtering, no sorting (returns all issues)
- **Naming:** `IssueFilterOptions` (not `FilterOptions`) to avoid collision with existing `Contracts.FilterOptions` (v0.2.5b terminology filtering)

#### SortCriteria Enum

Criteria for sorting unified validation issues:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Values:**
  - `Severity` — By `SeverityOrder` (Error first ascending)
  - `Location` — By `Location.Start` (top of document first)
  - `Category` — By `Category` enum value
  - `Message` — By `Message` alphabetically
  - `Code` — By `SourceId` alphabetically (adapted from spec's `issue.Code`)
  - `ValidatorName` — By `SourceType` alphabetically (adapted from spec's `issue.ValidatorName`)
  - `AutoFixable` — By `CanAutoFix` (auto-fixable first)

#### SortOptions

Sort configuration record:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Properties:**
  - `Primary` — `required SortCriteria`, primary sort criterion
  - `Secondary` — `SortCriteria?`, secondary tiebreaker
  - `Tertiary` — `SortCriteria?`, tertiary tiebreaker
  - `Ascending` — `bool`, default `true`

### IIssueFilterService Interface

Filtering, sorting, searching, counting, and preset management for unified issues:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`

**Async Filter Methods:**
- `FilterAsync(IReadOnlyList<UnifiedIssue>, IssueFilterOptions, CancellationToken)` — AND-composed filtering with sorting and pagination
- `SearchAsync(IReadOnlyList<UnifiedIssue>, string query, IssueFilterOptions?, CancellationToken)` — Text search with optional additional filters
- `FilterByCategoryAsync(IReadOnlyList<UnifiedIssue>, IEnumerable<IssueCategory>, CancellationToken)` — Category filter shorthand
- `FilterBySeverityAsync(IReadOnlyList<UnifiedIssue>, UnifiedSeverity, CancellationToken)` — Severity filter shorthand
- `FilterByLocationAsync(IReadOnlyList<UnifiedIssue>, TextSpan, bool includePartialOverlaps, CancellationToken)` — Location filter with overlap mode
- `FilterByLineRangeAsync(IReadOnlyList<UnifiedIssue>, int startLine, int endLine, CancellationToken)` — Approximate line range filter

**Sort Method:**
- `SortAsync(IReadOnlyList<UnifiedIssue>, IEnumerable<SortCriteria>, CancellationToken)` — Multi-criteria sort with OrderBy/ThenBy

**Count Methods:**
- `CountBySeverity(IReadOnlyList<UnifiedIssue>)` — Dictionary grouping by severity
- `CountByCategory(IReadOnlyList<UnifiedIssue>)` — Dictionary grouping by category

**Preset Methods:**
- `SavePreset(string name, IssueFilterOptions)` — Save named preset (overwrites existing)
- `LoadPreset(string name)` — Load preset (returns null if not found)
- `ListPresets()` — List all preset names (sorted)
- `DeletePreset(string name)` — Delete preset (returns success flag)

### IssueFilterService Implementation

Full in-memory filtering, sorting, and preset management:
- **Namespace:** `Lexichord.Modules.Agents.Tuning`
- **Visibility:** Internal sealed class
- **Implements:** `IIssueFilterService`
- **Dependencies:** `ILogger<IssueFilterService>`
- **Lifetime:** Singleton

**Filter Pipeline:**
1. Extract active criteria from `IssueFilterOptions`
2. For each issue, apply all criteria with AND logic (early exit on first failure)
3. Apply pagination (Offset, then Limit)
4. Sort results by `SortCriteria` using `OrderBy`/`ThenBy` chain
5. Return filtered and sorted results

**MatchesAllCriteria Logic (ordered by cost):**
1. Category filter — enum `Contains` check
2. Severity filter — inverted-enum numeric range: `MaximumSeverity <= issue.Severity <= MinimumSeverity`
3. Auto-fixable filter — `issue.CanAutoFix` bool check
4. Manual filter — `!issue.CanAutoFix` bool check
5. Validator name filter — `issue.SourceType` list contains
6. Issue code inclusion — wildcard regex matching on `issue.SourceId`
7. Issue code exclusion — wildcard regex matching on `issue.SourceId`
8. Text search — case-insensitive substring on Message, SourceId, SourceType
9. Location span — start/end containment check
10. Line range — approximate `Start / 80 + 1` character-offset-to-line conversion

**Sort Implementation:**
- First criterion uses `OrderBy`/`OrderByDescending`
- Subsequent criteria use `ThenBy`/`ThenByDescending` to preserve primary sort
- `GetSortKey()` maps criteria to actual properties (SourceId for Code, SourceType for ValidatorName, CanAutoFix for AutoFixable)

**Default Presets (6 built-in):**
- "Errors Only" — Error severity only, sorted by location
- "Warnings and Errors" — Error + Warning severity, sorted by severity then location
- "Auto-Fixable Only" — Auto-fixable issues, sorted by category then location
- "Style Issues" — Style category, sorted by severity then location
- "Grammar Issues" — Grammar category, sorted by location
- "Knowledge Issues" — Knowledge category, sorted by severity then location

**Performance:**
- Stopwatch-based duration logging for all filter operations
- Early exit on first failing criterion
- Target: <50ms for 1000 issues

### DI Registration

Extension method for service registration:
- **Location:** `TuningServiceCollectionExtensions.cs`
- **Method:** `AddIssueFilterService()`
- **Registers:**
  - `IIssueFilterService` → `IssueFilterService` → Singleton

---

## Spec Adaptations

The design spec references several APIs that don't match the actual codebase:

1. **`issue.Code` → `issue.SourceId`** — Spec references `Code` property for issue identifiers. Actual property is `SourceId` (string). All filter/sort code adapted.

2. **`issue.ValidatorName` → `issue.SourceType`** — Spec references `ValidatorName` for validator identification. Actual property is `SourceType` (string?). All filter/sort code adapted.

3. **`issue.Fix` (single) → `issue.CanAutoFix` (computed)** — Spec references a single `Fix` property. Actual model has `Fixes` (IReadOnlyList<UnifiedFix>) with computed `CanAutoFix` property. Auto-fixable/manual filters use `CanAutoFix`.

4. **`issue.Location` nullable → non-nullable** — Spec treats `Location` as nullable. Actual `UnifiedIssue.Location` is a non-nullable `TextSpan`. Null checks removed.

5. **`FilterOptions` → `IssueFilterOptions`** — Spec uses `FilterOptions` name. Renamed to `IssueFilterOptions` to avoid collision with existing `Lexichord.Abstractions.Contracts.FilterOptions` (v0.2.5b terminology filtering).

6. **Severity comparison inverted** — Spec uses `issue.Severity >= MinimumSeverity` which is incorrect for `UnifiedSeverity` where Error=0 is most severe and Hint=3 is least severe. Implementation uses `(int)issue.Severity <= (int)MinimumSeverity` for "at least as severe" semantics.

7. **Sort loop fix** — Spec's sort pseudo-code uses `OrderBy` in a loop, overwriting the previous sort on each iteration. Implementation uses the correct `OrderBy`/`ThenBy` chain pattern where only the first criterion uses `OrderBy` and subsequent criteria use `ThenBy`.

8. **`TextSpan` is a reference type** — `TextSpan` is a `record TextSpan(int Start, int Length)` (reference type). Nullable `TextSpan?` uses `is not null` pattern matching instead of `.HasValue`/`.Value`.

9. **`GetLineNumber` stub** — Spec provides a `GetLineNumber()` stub without implementation. `TextSpan` has no line information. Implementation uses simplified character-offset approximation: `Start / 80 + 1`.

10. **Namespace placement** — Contracts placed in `Lexichord.Abstractions.Contracts.Validation`; implementation in `Lexichord.Modules.Agents.Tuning`.

---

## Files Created

### Contracts — Filter Criteria (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Validation/FilterCriteria.cs` | Abstract record + 5 subclasses | Composable filter predicates |

### Contracts — Options (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Validation/IssueFilterOptions.cs` | Record + Enum | Filter/sort/pagination options with SortCriteria enum |
| `src/Lexichord.Abstractions/Contracts/Validation/SortOptions.cs` | Record | Sort configuration with primary/secondary/tertiary |

### Contracts — Interface (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Validation/IIssueFilterService.cs` | Interface | 14-method filtering, sorting, searching, preset API |

### Implementation (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Tuning/IssueFilterService.cs` | Internal sealed class | Full filtering, sorting, preset implementation |

### Tests (1 file)

| File | Tests | Description |
|:-----|:-----:|:------------|
| `tests/.../IssueFilterServiceTests.cs` | 84 | Comprehensive unit tests covering all service methods |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Modules.Agents/Extensions/TuningServiceCollectionExtensions.cs` | Added `AddIssueFilterService()` extension registering `IIssueFilterService` → `IssueFilterService` as Singleton; updated class-level XML docs with v0.7.5i references |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added `services.AddIssueFilterService()` call in `RegisterServices()`; added verification block in `InitializeAsync()` checking `IIssueFilterService` resolution and logging preset count |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| IssueFilterServiceTests | 84 | Full service coverage |

### Test Groups

| Group | Tests | Description |
|:------|:-----:|:------------|
| Constructor | 2 | Null logger, default presets initialization |
| FilterAsync core | 20 | Category, severity (inverted enum), code/exclude wildcards, text search, location, line range, auto-fixable, manual, validator, pagination, combined, empty |
| SearchAsync | 4 | Message search, combined with options, null validation |
| FilterByCategoryAsync | 2 | Single category, multiple categories |
| FilterBySeverityAsync | 4 | Theory with Error/Warning/Info/Hint (boundary values) |
| FilterByLocationAsync | 3 | Strict containment, partial overlaps, no match |
| FilterByLineRangeAsync | 2 | Valid range, wide range |
| SortAsync | 9 | Each criterion (severity, location, category, message, code, auto-fixable), multi-criteria, empty criteria, descending |
| CountBySeverity/Category | 6 | Correct grouping, empty list, null validation |
| Presets CRUD | 10 | Save, load, list (sorted), delete, overwrite, not found, null/empty validation, default presets functional |
| FilterCriteria subclasses | 9 | Category, severity, location (strict + overlap), text search (message, sourceId, case-insensitive), code (exact + wildcard) |
| Contract types | 4 | IssueFilterOptions defaults, Default static, SortCriteria enum values, SortOptions defaults |
| Performance | 1 | 1000 issues filtered in <50ms |
| **Total** | **84** | |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.5i")]`

---

## Design Decisions

1. **Singleton lifetime** for `IssueFilterService` — Stateless service with only presets dictionary and logger. Thread-safe for concurrent filtering. Matches `UnifiedValidationService` pattern.

2. **Renamed `IssueFilterOptions`** — Avoids collision with existing `FilterOptions` in `Lexichord.Abstractions.Contracts` (v0.2.5b terminology filtering).

3. **Fixed severity comparison** — Spec uses `>=` which is wrong for inverted enum (Error=0 is most severe). Implementation uses `(int)` casts with correct `<=`/`>=` direction for inclusive range checking.

4. **Fixed sort ordering** — Spec's `OrderBy` loop overwrites previous sort. Implementation uses proper `OrderBy`/`ThenBy` chain where first criterion establishes primary sort and subsequent criteria break ties.

5. **Simplified single-class architecture** — No strategy pattern, registries, or middleware. Single `IssueFilterService` class with `MatchesAllCriteria()` implementing all filter logic inline for clarity and performance.

6. **Early exit filter ordering** — Criteria checked in ascending cost order: enum/bool comparisons first, regex/string search last. First failing criterion short-circuits evaluation.

7. **In-memory preset storage** — Dictionary with Ordinal string comparison. Six default presets initialized at construction. No persistence — presets reset on service restart.

8. **Approximate line number** — `TextSpan` lacks line info. Uses `Start / 80 + 1` approximation (80 chars per line average). Adequate for filtering purposes.

9. **No ViewModel integration** — `IssueFilterService` is consumed purely via DI by future consumers. No modifications to `UnifiedIssuesPanelViewModel` (its existing severity filter gap is a separate concern).

10. **Composable FilterCriteria hierarchy** — Abstract record base enables future composition patterns while each subclass independently implements `Matches()` for reuse outside the main service.

---

## Dependencies

### Consumed (from existing modules)

| Type | Namespace | Version |
|:-----|:----------|:--------|
| `UnifiedIssue` | `Contracts.Validation` | v0.7.5e |
| `UnifiedFix` | `Contracts.Validation` | v0.7.5e |
| `IssueCategory` | `Contracts.Validation` | v0.7.5e |
| `UnifiedSeverity` | `Knowledge.Validation.Integration` | v0.6.5j |
| `TextSpan` | `Contracts.Editor` | v0.6.7b |

### Produced (new types)

| Type | Consumers |
|:-----|:----------|
| `FilterCriteria` | Composable filter building (abstract base) |
| `CategoryFilterCriterion` | Category matching |
| `SeverityFilterCriterion` | Severity matching |
| `LocationFilterCriterion` | Location matching |
| `TextSearchFilterCriterion` | Text search matching |
| `CodeFilterCriterion` | Wildcard code matching |
| `IssueFilterOptions` | IIssueFilterService, filter presets |
| `SortCriteria` | IssueFilterOptions.SortBy, IIssueFilterService.SortAsync |
| `SortOptions` | Sort configuration |
| `IIssueFilterService` | IssueFilterService, DI consumers |
| `IssueFilterService` | DI container (Singleton) |

### No New NuGet Packages

All implementation uses existing project dependencies.

---

## Build & Test Results

```
Build:     0 errors, 0 warnings (1 pre-existing Avalonia warning)
v0.7.5i:   84 tests passing
Full suite: 10,047 passed, 0 failed, 33 skipped (pre-existing platform-specific)
```

---

## Usage Examples

### Filter by Category and Severity

```csharp
var filterService = serviceProvider.GetRequiredService<IIssueFilterService>();

var filtered = await filterService.FilterAsync(
    issues,
    new IssueFilterOptions
    {
        Categories = [IssueCategory.Style],
        MinimumSeverity = UnifiedSeverity.Warning,
        SortBy = [SortCriteria.Severity, SortCriteria.Location]
    });
```

### Search Issues by Text

```csharp
var results = await filterService.SearchAsync(issues, "OAuth");
```

### Filter by Wildcard Code Patterns

```csharp
var filtered = await filterService.FilterAsync(
    issues,
    new IssueFilterOptions
    {
        IssueCodes = ["STYLE_*", "TERM_001"],
        ExcludeCodes = ["STYLE_099"]
    });
```

### Filter Auto-Fixable Issues Only

```csharp
var autoFixable = await filterService.FilterAsync(
    issues,
    new IssueFilterOptions { OnlyAutoFixable = true });
```

### Multi-Criteria Sort

```csharp
var sorted = await filterService.SortAsync(
    issues,
    [SortCriteria.Severity, SortCriteria.Category, SortCriteria.Location]);
```

### Count by Severity

```csharp
var counts = filterService.CountBySeverity(issues);
// { Error: 3, Warning: 5, Info: 2 }
```

### Use a Named Preset

```csharp
var preset = filterService.LoadPreset("Errors Only");
if (preset is not null)
{
    var errors = await filterService.FilterAsync(issues, preset);
}
```

### Save and Manage Custom Presets

```csharp
filterService.SavePreset("My Review Filter", new IssueFilterOptions
{
    Categories = [IssueCategory.Grammar, IssueCategory.Style],
    MinimumSeverity = UnifiedSeverity.Warning,
    OnlyAutoFixable = true,
    SortBy = [SortCriteria.Severity, SortCriteria.Location]
});

var presetNames = filterService.ListPresets();
filterService.DeletePreset("My Review Filter");
```

### Paginated Results

```csharp
var page = await filterService.FilterAsync(
    issues,
    new IssueFilterOptions
    {
        SortBy = [SortCriteria.Severity],
        Offset = 20,
        Limit = 10
    });
```

### Filter by Location

```csharp
// Issues fully within the span
var contained = await filterService.FilterByLocationAsync(
    issues,
    new TextSpan(100, 500));

// Issues overlapping the span
var overlapping = await filterService.FilterByLocationAsync(
    issues,
    new TextSpan(100, 500),
    includePartialOverlaps: true);
```

### Use FilterCriteria Directly

```csharp
var criterion = new CategoryFilterCriterion([IssueCategory.Style, IssueCategory.Grammar]);
bool matches = criterion.Matches(issue);

var codeCriterion = new CodeFilterCriterion(["STYLE_*"]);
bool codeMatches = codeCriterion.Matches(issue);
```
