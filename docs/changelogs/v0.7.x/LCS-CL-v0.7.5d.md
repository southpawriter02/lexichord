# Changelog: v0.7.5d — Learning Loop

**Feature ID:** AGT-075d
**Version:** 0.7.5d
**Date:** 2026-02-15
**Status:** Complete

---

## Overview

Implements the Learning Loop for the Tuning Agent, building upon v0.7.5a's Style Deviation Scanner, v0.7.5b's Automatic Fix Suggestions, and v0.7.5c's Accept/Reject UI. This is the fourth and final sub-part of v0.7.5 "The Tuning Agent" and provides feedback persistence, pattern analysis, and prompt enhancement so the Tuning Agent improves over time based on user accept/reject/modify decisions.

The implementation adds:
- `LearningLoopService` — feedback recording, pattern analysis, statistics, export/import, privacy controls, and MediatR event handling
- `PatternAnalyzer` — pattern extraction from feedback data and prompt enhancement generation
- `SqliteFeedbackStore` — SQLite-backed persistence for feedback records and pattern caches
- `ILearningLoopService` — public interface for the learning loop API
- `IFeedbackStore` — internal storage abstraction
- `FeedbackRecord` / `PatternCacheRecord` — internal storage records
- `LearningStorageOptions` — configuration for database path, retention, and cache limits
- 16 public contract types (records & enums) for feedback, patterns, statistics, export, and privacy
- Full unit test coverage (94 tests across 3 test classes)

---

## What's New

### LearningLoopService

Main orchestrator for the Learning Loop feature:
- **Namespace:** `Lexichord.Modules.Agents.Tuning`
- **Visibility:** `internal sealed class` (consumed via `ILearningLoopService` interface)
- **Implements:** `ILearningLoopService`, `INotificationHandler<SuggestionAcceptedEvent>`, `INotificationHandler<SuggestionRejectedEvent>`
- **Dependencies:** `IFeedbackStore`, `PatternAnalyzer`, `ISettingsService`, `ILicenseContext`, `ILogger<LearningLoopService>`
- **Public Methods:**
  - `RecordFeedbackAsync()` — Store feedback, update pattern cache, enforce retention policy
  - `GetLearningContextAsync()` — Fetch patterns via store + analyzer, generate prompt enhancement
  - `GetStatisticsAsync()` — Delegate to feedback store for aggregated statistics
  - `ExportLearningDataAsync()` — Export patterns and statistics with anonymization options
  - `ImportLearningDataAsync()` — Import learning data from team exports
  - `ClearLearningDataAsync()` — Clear feedback, patterns, and/or statistics
  - `GetPrivacyOptions()` — Read privacy settings via `ISettingsService` (sync)
  - `SetPrivacyOptionsAsync()` — Write privacy settings via `ISettingsService`
- **MediatR Handlers:**
  - `Handle(SuggestionAcceptedEvent)` — Convert to `FixFeedback` with `FeedbackDecision.Accepted` or `.Modified`, call `RecordFeedbackAsync`
  - `Handle(SuggestionRejectedEvent)` — Convert to `FixFeedback` with `FeedbackDecision.Rejected`, call `RecordFeedbackAsync`
  - Handlers silently skip when Teams license not available
- **Privacy:**
  - SHA256 anonymization via `SHA256.HashData()` when privacy enabled
  - Content anonymization replaces words 5+ characters with `[WORD]`
  - Retention policy enforcement after each feedback recording
- **License Gating:**
  - All public API methods throw `InvalidOperationException` when Teams license not available
  - MediatR handlers silently skip (no exception) for non-Teams users

### PatternAnalyzer

Pattern extraction and prompt enhancement engine:
- **Namespace:** `Lexichord.Modules.Agents.Tuning`
- **Visibility:** `internal sealed class`
- **Dependencies:** `ILogger<PatternAnalyzer>`
- **Methods:**
  - `ExtractAcceptedPatterns()` — Groups by (OriginalText, ResultText), filters success rate >= 0.7 and count >= 3, returns top 5
  - `ExtractRejectedPatterns()` — Groups by (OriginalText, ResultText), filters success rate <= 0.3 and count >= 3, returns top 5
  - `ExtractUserModifications()` — Filters `Modified` decisions, returns top 3 most recent
  - `GeneratePromptEnhancement()` — Requires 10+ total samples; builds structured text with preferred patterns, avoided patterns, modification examples, and confidence calibration sections

### SqliteFeedbackStore

SQLite-backed persistence layer:
- **Namespace:** `Lexichord.Modules.Agents.Tuning.Storage`
- **Visibility:** `internal sealed class : IFeedbackStore, IDisposable`
- **Dependencies:** `IOptions<LearningStorageOptions>`, `ILogger<SqliteFeedbackStore>`
- **Library:** `Microsoft.Data.Sqlite` v9.0.0 with raw ADO.NET (follows `SqliteEmbeddingCache` pattern from `Lexichord.Modules.RAG`)
- **Schema:**
  - `feedback` table (15 columns: Id, SuggestionId, DeviationId, RuleId, Category, Decision, OriginalText, SuggestedText, ModifiedText, AnonymizedContext, Timestamp, ContentHash, OriginalConfidence, IsBulkOperation; 3 indexes on RuleId, Category, Timestamp)
  - `pattern_cache` table (composite PK on RuleId+PatternType+OriginalPattern+SuggestedPattern; Count, SuccessRate, LastUpdated columns)
- **Methods:**
  - `InitializeAsync()` — Creates tables and indexes via `CREATE TABLE IF NOT EXISTS`
  - `StoreFeedbackAsync()` — Parameterized INSERT with `$param` syntax
  - `GetFeedbackByRuleAsync()` — Retrieves feedback for a specific rule (with limit)
  - `GetAcceptedPatternsAsync()` / `GetRejectedPatternsAsync()` — Pattern cache queries with minimum frequency threshold
  - `GetStatisticsAsync()` — Aggregates feedback into `LearningStatistics` with per-rule and per-category breakdowns
  - `UpdatePatternCacheAsync()` — Rebuilds pattern cache from raw feedback using INSERT OR REPLACE
  - `ClearDataAsync()` — Selective clearing based on `ClearLearningDataOptions`
  - `GetFeedbackCountAsync()` — Count with optional date range filter
  - `ExportPatternsAsync()` — Export all patterns with optional anonymization
  - `DeleteOlderThanAsync()` — Retention policy enforcement
  - `DeleteOldestAsync()` — Cache size enforcement
- **Connection Pattern:** `SqliteConnectionStringBuilder` with `Pooling=true`, per-method `using var connection`
- **Dispose:** `SqliteConnection.ClearAllPools()`

### Contract Types

All public types in `Lexichord.Abstractions.Contracts.Agents`:

| Type | Kind | Description |
|:-----|:-----|:------------|
| `FeedbackDecision` | Enum | Accepted, Rejected, Modified |
| `FixFeedback` | Record | Core feedback data with deviation, suggestion, decision, timestamp |
| `LearningContext` | Record | AcceptedPatterns, RejectedPatterns, UserModifications, PromptEnhancement |
| `AcceptedPattern` | Record | RuleId, OriginalText, AcceptedText, Frequency, LastSeen |
| `RejectedPattern` | Record | RuleId, OriginalText, RejectedText, Frequency, LastSeen |
| `UserModificationExample` | Record | RuleId, OriginalText, SuggestedText, UserText |
| `LearningStatistics` | Record | TotalFeedback, AcceptRate, RejectRate, ModifyRate, TopAcceptedRules, TopRejectedRules, RuleStats, CategoryStats |
| `RuleLearningStats` | Record | Per-rule breakdown: Total, Accepted, Rejected, Modified, AcceptRate |
| `CategoryLearningStats` | Record | Per-category breakdown: Total, Accepted, Rejected, Modified, AcceptRate |
| `LearningStatisticsFilter` | Record | DateRange?, RuleIds?, Categories? |
| `LearningExportOptions` | Record | IncludeRawFeedback, IncludePatterns, IncludeStatistics, DateRange?, AnonymizeContent |
| `LearningExport` | Record | ExportedAt, Version, Patterns, Statistics?, RawFeedbackCount? |
| `ExportedPattern` | Record | RuleId, Type, Frequency, LastSeen |
| `ClearLearningDataOptions` | Record | ClearFeedback, ClearPatterns, ClearStatistics, OlderThan? |
| `LearningPrivacyOptions` | Record | AnonymizeContent, RetentionDays, ExcludedRuleCategories |

### ILearningLoopService Interface

Public interface for the learning loop API:
- **Namespace:** `Lexichord.Abstractions.Contracts.Agents`
- **Methods:**
  - `RecordFeedbackAsync(FixFeedback, CancellationToken)` → `Task`
  - `GetLearningContextAsync(string ruleId, CancellationToken)` → `Task<LearningContext>`
  - `GetStatisticsAsync(LearningStatisticsFilter?, CancellationToken)` → `Task<LearningStatistics>`
  - `ExportLearningDataAsync(LearningExportOptions, CancellationToken)` → `Task<LearningExport>`
  - `ImportLearningDataAsync(LearningExport, CancellationToken)` → `Task`
  - `ClearLearningDataAsync(ClearLearningDataOptions, CancellationToken)` → `Task`
  - `GetPrivacyOptions()` → `LearningPrivacyOptions` (sync)
  - `SetPrivacyOptionsAsync(LearningPrivacyOptions, CancellationToken)` → `Task`

### FeatureCodes.LearningLoop

License feature code for the Learning Loop:
- **Constant:** `"Feature.LearningLoop"`
- **Location:** `Lexichord.Abstractions.Constants.FeatureCodes`
- **Used By:** `LearningLoopService` (all public methods and MediatR handlers)

### DI Registration

Extended `TuningServiceCollectionExtensions` with `AddLearningLoop()`:
```csharp
services.Configure<LearningStorageOptions>(configure);
services.AddSingleton<IFeedbackStore, SqliteFeedbackStore>();
services.AddSingleton<PatternAnalyzer>();
services.AddSingleton<LearningLoopService>();
services.AddSingleton<ILearningLoopService>(sp => sp.GetRequiredService<LearningLoopService>());
services.AddSingleton<INotificationHandler<SuggestionAcceptedEvent>>(sp => sp.GetRequiredService<LearningLoopService>());
services.AddSingleton<INotificationHandler<SuggestionRejectedEvent>>(sp => sp.GetRequiredService<LearningLoopService>());
```

Updated `AgentsModule.RegisterServices()` with `services.AddLearningLoop()` call.
Updated `AgentsModule.InitializeAsync()` with verification block resolving `ILearningLoopService` and `IFeedbackStore`, calling `feedbackStore.InitializeAsync()`.

### Integration Points

**TuningPanelViewModel** (v0.7.5c → updated in v0.7.5d):
- Added `ILearningLoopService? learningLoop` nullable constructor parameter
- Stored as `_learningLoop` field for future use
- No behavioral changes — events are handled via MediatR, not direct ViewModel calls

**FixSuggestionGenerator** (v0.7.5b → updated in v0.7.5d):
- Added `ILearningLoopService? learningLoop` nullable constructor parameter
- Changed `BuildPromptContext()` from `private static` to `private` instance method
- Added learning context injection: if `_learningLoop` is available, calls `GetLearningContextAsync()` and adds `PromptEnhancement` to the template context dictionary as `learning_enhancement`
- Wrapped in try/catch for resilience — failures log a warning and proceed without enhancement

---

## Spec Adaptations

The design spec (LCS-DES-v0.7.5d.md) references several APIs that don't match the actual codebase:

1. **SQLite library** — Spec references `SQLiteAsyncConnection` (sqlite-net-pcl). Codebase uses `Microsoft.Data.Sqlite` v9.0.0 with raw ADO.NET. Follows the `SqliteEmbeddingCache` pattern from `Lexichord.Modules.RAG`.

2. **DateRange** — Spec defines a new `DateRange(DateTimeOffset?, DateTimeOffset?)`. Existing `DateRange(DateTime?, DateTime?)` already exists at `Lexichord.Abstractions.Contracts.SearchFilter.cs` (v0.5.5a). Reused the existing type.

3. **DateTime vs DateTimeOffset** — Spec uses `DateTimeOffset` for timestamps. Codebase consistently uses `DateTime` (e.g., `SuggestionAcceptedEvent.Timestamp`). Used `DateTime`.

4. **ISettingsService** — Spec references `IPrivacySettingsService`. Codebase has `ISettingsService` with `Get<T>(key, default)` / `Set<T>(key, value)`. Used `ISettingsService` with `"Learning:Privacy:"` key prefix.

5. **Singleton MediatR handler forwarding** — `LearningLoopService` must be both `ILearningLoopService` (singleton) and `INotificationHandler<SuggestionAcceptedEvent>` / `INotificationHandler<SuggestionRejectedEvent>`. DI forwards handler registrations to the same singleton instance.

6. **Internal types** — `IFeedbackStore`, `SqliteFeedbackStore`, `FeedbackRecord`, `PatternCacheRecord`, `PatternAnalyzer`, and `LearningLoopService` are all `internal` to `Lexichord.Modules.Agents` (accessible in tests via existing `InternalsVisibleTo`). `LearningLoopService` was made internal because its constructor takes internal parameter types (`IFeedbackStore`, `PatternAnalyzer`).

7. **License tier** — Learning Loop requires Teams tier (value 2), matching the spec.

---

## Files Created

### Abstractions — Contracts (2 files)

| File | Lines | Type | Description |
|:-----|:-----:|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Agents/LearningLoopContracts.cs` | 781 | Records & Enums | 16 public types: FeedbackDecision, FixFeedback, LearningContext, AcceptedPattern, RejectedPattern, UserModificationExample, LearningStatistics, RuleLearningStats, CategoryLearningStats, LearningStatisticsFilter, LearningExportOptions, LearningExport, ExportedPattern, ClearLearningDataOptions, LearningPrivacyOptions |
| `src/Lexichord.Abstractions/Contracts/Agents/ILearningLoopService.cs` | 175 | Interface | Public API: RecordFeedback, GetLearningContext, GetStatistics, Export, Import, Clear, Privacy |

### Module — Configuration (1 file)

| File | Lines | Type | Description |
|:-----|:-----:|:-----|:------------|
| `src/Lexichord.Modules.Agents/Tuning/Configuration/LearningStorageOptions.cs` | 91 | Options | DatabasePath, RetentionDays (365), MaxPatternCacheSize (100), PatternMinFrequency (2) |

### Module — Storage (4 files)

| File | Lines | Type | Description |
|:-----|:-----:|:-----|:------------|
| `src/Lexichord.Modules.Agents/Tuning/Storage/IFeedbackStore.cs` | 158 | Internal Interface | Storage abstraction with 12 methods |
| `src/Lexichord.Modules.Agents/Tuning/Storage/FeedbackRecord.cs` | 103 | Internal Record | SQLite row mapping (15 columns) |
| `src/Lexichord.Modules.Agents/Tuning/Storage/PatternCacheRecord.cs` | 68 | Internal Record | Pattern cache row (composite PK) |
| `src/Lexichord.Modules.Agents/Tuning/Storage/SqliteFeedbackStore.cs` | 924 | Internal Class | SQLite implementation with IDisposable |

### Module — Services (2 files)

| File | Lines | Type | Description |
|:-----|:-----:|:-----|:------------|
| `src/Lexichord.Modules.Agents/Tuning/PatternAnalyzer.cs` | 426 | Internal Class | Pattern extraction and prompt enhancement |
| `src/Lexichord.Modules.Agents/Tuning/LearningLoopService.cs` | 646 | Internal Class | Main service with MediatR handlers |

### Tests (3 files)

| File | Lines | Tests | Description |
|:-----|:-----:|:-----:|:------------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/Tuning/LearningLoopServiceTests.cs` | 1210 | 45 | Service tests: constructor, feedback, context, stats, export, import, clear, privacy, handlers |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Tuning/PatternAnalyzerTests.cs` | 604 | 21 | Analyzer tests: extraction, prompt enhancement, edge cases |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Tuning/SqliteFeedbackStoreTests.cs` | 767 | 28 | Store tests: CRUD, aggregation, cache, retention, export |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Abstractions/Constants/FeatureCodes.cs` | Added `LearningLoop = "Feature.LearningLoop"` constant |
| `src/Lexichord.Modules.Agents/Lexichord.Modules.Agents.csproj` | Added `Microsoft.Data.Sqlite` v9.0.0 package; added unsigned `DynamicProxyGenAssembly2` InternalsVisibleTo for Moq compatibility |
| `src/Lexichord.Modules.Agents/Extensions/TuningServiceCollectionExtensions.cs` | Added `AddLearningLoop()` DI extension method |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added `services.AddLearningLoop()` call and init verification block |
| `src/Lexichord.Modules.Agents/Tuning/TuningPanelViewModel.cs` | Added `ILearningLoopService?` nullable constructor parameter and field |
| `src/Lexichord.Modules.Agents/Tuning/FixSuggestionGenerator.cs` | Added `ILearningLoopService?` parameter; changed `BuildPromptContext` from static to instance; added learning context injection with try/catch |
| `tests/.../TuningPanelViewModelTests.cs` | Updated 7 constructor calls to include `null` for new `learningLoop` parameter |
| `tests/.../FixSuggestionGeneratorTests.cs` | Updated 7 constructor calls to include `null` for new `learningLoop` parameter |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| LearningLoopServiceTests | 45 | Constructor, feedback, context, statistics, export, import, clear, privacy, MediatR handlers, anonymization |
| PatternAnalyzerTests | 21 | Extraction (accepted, rejected, modifications), prompt enhancement, edge cases |
| SqliteFeedbackStoreTests | 28 | Initialize, CRUD, aggregation, pattern cache, retention, export, clearing |
| **Total v0.7.5d** | **94** | All v0.7.5d functionality |

### Test Groups

#### LearningLoopServiceTests (45 tests)

| Group | Tests | Description |
|:------|:-----:|:------------|
| Constructor | 5 | Null checks for all 5 dependencies |
| RecordFeedback | 6 | Store delegation, pattern cache update, license gating, null guard, anonymization |
| GetLearningContext | 6 | Pattern extraction, prompt generation, empty results, license gating, null/empty guards |
| GetStatistics | 3 | Delegation, license gating, null filter |
| Export | 4 | Pattern export, statistics inclusion, license gating, null options guard |
| Import | 3 | Data import, license gating, null guard |
| Clear | 4 | Selective clearing, full clear, license gating, null options guard |
| GetPrivacy | 2 | Default options, settings retrieval |
| SetPrivacy | 3 | Settings persistence, license gating, null options guard |
| HandleAccepted | 4 | Accepted event, modified event, no-license silent skip, feedback mapping |
| HandleRejected | 3 | Rejected event, no-license silent skip, feedback mapping |
| Privacy/Anonymization | 2 | SHA256 content hashing, context anonymization |

#### PatternAnalyzerTests (21 tests)

| Group | Tests | Description |
|:------|:-----:|:------------|
| Constructor | 2 | Null logger guard, successful construction |
| ExtractAccepted | 7 | Frequency threshold (≥3), success rate (≥0.7), top-5 limit, empty input, mixed decisions |
| ExtractRejected | 4 | Frequency threshold (≥3), success rate (≤0.3), top-5 limit, empty input |
| ExtractModifications | 5 | Modified decision filtering, top-3 limit, empty input, non-modified exclusion |
| GeneratePromptEnhancement | 3 | Minimum 10 samples, structured output, empty patterns returns null |

#### SqliteFeedbackStoreTests (28 tests)

| Group | Tests | Description |
|:------|:-----:|:------------|
| Constructor | 2 | Null options guard, null logger guard |
| Initialize | 2 | Table creation, idempotent re-init |
| StoreFeedback | 3 | Full record storage, nullable fields, null record guard |
| GetByRule | 2 | Rule-filtered retrieval, limit enforcement |
| GetPatterns | 2 | Accepted/rejected pattern cache retrieval |
| Statistics | 3 | Full aggregation, date range filtering, empty results |
| UpdateCache | 3 | Cache rebuild, null rule guard, empty rule guard |
| ClearData | 3 | Full clear, rule-filtered clear, date-filtered clear |
| Export | 1 | Pattern export |
| Retention | 3 | Delete by date, delete count, delete oldest |
| FeedbackCount | 2 | Total count, date-filtered count |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.5d")]`

---

## Design Decisions

1. **Internal LearningLoopService** — Made `internal sealed class` because the constructor takes internal parameter types (`IFeedbackStore`, `PatternAnalyzer`). Consumers access it only through the public `ILearningLoopService` interface via DI.

2. **Singleton MediatR Handler Forwarding** — `LearningLoopService` registered as a singleton concrete type, with `ILearningLoopService` and both `INotificationHandler<T>` registrations forwarding to the same instance via `sp.GetRequiredService<LearningLoopService>()`.

3. **Microsoft.Data.Sqlite over sqlite-net-pcl** — Spec references sqlite-net-pcl, but the codebase standard (established by `SqliteEmbeddingCache` in Modules.RAG) uses `Microsoft.Data.Sqlite` with raw ADO.NET. Follows this existing pattern for consistency.

4. **Nullable ILearningLoopService** — Both `TuningPanelViewModel` and `FixSuggestionGenerator` accept `ILearningLoopService?` as nullable. This allows the system to function without the learning loop (e.g., pre-Teams users, or if DI resolution fails).

5. **Static to Instance Method Change** — `FixSuggestionGenerator.BuildPromptContext()` was changed from `private static` to `private` instance method to access the `_learningLoop` field for learning context injection.

6. **Sync GetAwaiter().GetResult() in BuildPromptContext** — Used `.GetAwaiter().GetResult()` for the async `GetLearningContextAsync()` call within `BuildPromptContext()` because the method is called within an already-async pipeline. Wrapped in try/catch for resilience.

7. **Silent MediatR Handler Skip** — MediatR handlers (`Handle(SuggestionAcceptedEvent)` and `Handle(SuggestionRejectedEvent)`) silently skip when Teams license is not available, rather than throwing. This prevents exceptions from disrupting the accept/reject flow for non-Teams users.

8. **Privacy Settings via ISettingsService** — Privacy options stored under `"Learning:Privacy:"` key prefix using the existing `ISettingsService` rather than creating a new `IPrivacySettingsService` as the spec suggests.

9. **Pattern Thresholds** — Accepted patterns require success rate ≥ 0.7 with count ≥ 3; rejected patterns require success rate ≤ 0.3 with count ≥ 3. Prompt enhancement requires 10+ total samples. These thresholds ensure statistical significance before influencing suggestions.

10. **Unsigned DynamicProxyGenAssembly2** — Added unsigned `InternalsVisibleTo` for `DynamicProxyGenAssembly2` alongside the existing signed entry. Moq 4.20.70 uses Castle.Core 5.x which generates unsigned proxy assemblies, while the original signed entry was for Castle.Core 4.x.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Version | Used By |
|:----------|:--------|:--------|
| `ISettingsService` | v0.1.6a | Privacy settings persistence |
| `ILicenseContext` | v0.0.4c | Teams license validation |
| `IStyleDeviationScanner` | v0.7.5a | Deviation context (via events) |
| `IFixSuggestionGenerator` | v0.7.5b | Learning context injection |
| `SuggestionAcceptedEvent` | v0.7.5c | MediatR handler trigger |
| `SuggestionRejectedEvent` | v0.7.5c | MediatR handler trigger |
| `DateRange` | v0.5.5a | Statistics filtering |
| `FeatureCodes` | v0.0.4c | Feature code constants |

### Produced (new types)

| Type | Consumers |
|:-----|:----------|
| `ILearningLoopService` | TuningPanelViewModel, FixSuggestionGenerator, future agents |
| `FixFeedback` | LearningLoopService (via MediatR events) |
| `LearningContext` | FixSuggestionGenerator (prompt enhancement) |
| `LearningStatistics` | Analytics dashboard (future) |
| `LearningExport` | Team sharing (future) |
| `LearningPrivacyOptions` | Privacy settings UI (future) |
| `FeatureCodes.LearningLoop` | License validation |

### NuGet Packages

| Package | Version | Status |
|:--------|:--------|:-------|
| `Microsoft.Data.Sqlite` | 9.0.0 | **New** (added in v0.7.5d) |

---

## License Gating

| Feature | Core | Writer | WriterPro | Teams |
|:--------|:-----|:-------|:----------|:------|
| Record feedback | - | - | - | ✓ |
| Get learning context | - | - | - | ✓ |
| Get statistics | - | - | - | ✓ |
| Export/import data | - | - | - | ✓ |
| Clear learning data | - | - | - | ✓ |
| Privacy controls | - | - | - | ✓ |
| MediatR auto-recording | - | - | - | ✓ |

---

## Build & Test Results

```
Build:     Succeeded (0 errors, 0 warnings)
v0.7.5d:   94 passed (new), 0 failed
v0.7.5a-c: 149 passed, 0 failed (no regressions)
Full suite: 5506 unit passed, 0 failed, 27 skipped
```
