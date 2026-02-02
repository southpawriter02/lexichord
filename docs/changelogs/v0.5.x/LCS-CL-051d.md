# LCS-CL-051d: Search Mode Toggle

## Document Control

| Field              | Value                           |
| :----------------- | :------------------------------ |
| **Document ID**    | LCS-CL-051d                     |
| **Version**        | v0.5.1d                         |
| **Feature Name**   | Search Mode Toggle              |
| **Module**         | Lexichord.Modules.RAG           |
| **Status**         | Complete                        |
| **Last Updated**   | 2026-02-02                      |
| **Related Spec**   | [LCS-DES-v0.5.1d](../../specs/v0.5.x/v0.5.1/LCS-DES-v0.5.1d.md) |

---

## Summary

Implemented a search mode toggle in the Reference Panel UI, allowing users to switch between Semantic, Keyword, and Hybrid search strategies. The search mode selection is license-gated (Hybrid requires WriterPro+), persisted via `ISystemSettingsRepository`, and emits telemetry events via MediatR. This completes the v0.5.1 Hybrid Engine feature.

---

## Changes

### New Files

#### Lexichord.Abstractions

| File | Description |
|:-----|:------------|
| `Contracts/RAG/SearchMode.cs` | Enum defining three search strategies: Semantic (0), Keyword (1), Hybrid (2) |

#### Lexichord.Tests.Unit

| File | Description |
|:-----|:------------|
| `Modules/RAG/ViewModels/ReferenceViewModelSearchModeTests.cs` | 39 unit tests covering constructor validation, initialization, license gating, persistence, telemetry, and enum correctness |

### Modified Files

| File | Change |
|:-----|:-------|
| `Constants/FeatureCodes.cs` | Added `HybridSearch` feature code constant (`Feature.HybridSearch`) in new region |
| `Search/SearchEvents.cs` | Added `SearchModeChangedEvent` MediatR notification with PreviousMode, NewMode, LicenseTier, Timestamp properties |
| `ViewModels/ReferenceViewModel.cs` | Extended constructor (6→10 parameters) with `IBM25SearchService`, `IHybridSearchService`, `ILicenseContext`, `ISystemSettingsRepository?`. Added `SelectedSearchMode` observable property, `IsHybridLocked` property, `AvailableSearchModes`, mode initialization from settings, license-gated mode switching, mode dispatch in `SearchAsync`, and telemetry publishing |
| `Views/ReferenceView.axaml` | Added `ComboBox` for search mode selection (Width=100), hybrid lock warning banner, search mode indicator in status bar, and updated grid layout from 2-column to 3-column search bar |
| `RAGModule.cs` | Updated module version from 0.4.7 to 0.5.1, updated ReferenceViewModel comment |

---

## Technical Details

### Search Mode Dispatch

The `ReferenceViewModel.SearchAsync` method dispatches to the appropriate service based on the selected mode:

```csharp
var result = mode switch
{
    SearchMode.Semantic => await _semanticSearchService.SearchAsync(query, options, ct),
    SearchMode.Keyword  => await _bm25SearchService.SearchAsync(query, options, ct),
    SearchMode.Hybrid   => await _hybridSearchService.SearchAsync(query, options, ct),
    _ => throw new InvalidOperationException($"Unknown search mode: {mode}")
};
```

### License Gating Strategy

| Tier       | Semantic | Keyword | Hybrid | Default Mode |
|:-----------|:---------|:--------|:-------|:-------------|
| Core       | ✓        | ✓       | 🔒     | Semantic     |
| WriterPro  | ✓        | ✓       | ✓      | Hybrid       |
| Teams      | ✓        | ✓       | ✓      | Hybrid       |
| Enterprise | ✓        | ✓       | ✓      | Hybrid       |

When a Core-tier user attempts to select Hybrid mode:
1. Mode reverts to Semantic
2. `IsHybridLocked` is set to `true` (shows warning banner)
3. `SearchDeniedEvent` is published via MediatR

### Mode Persistence

- Settings key: `Search.DefaultMode`
- Storage: `ISystemSettingsRepository.SetValueAsync<string>`
- Restoration: `ISystemSettingsRepository.GetValueAsync<string>` with `Enum.TryParse` validation
- Null-safe: `ISystemSettingsRepository` is nullable (graceful fallback to tier defaults)

### Initialization Logic

1. Load persisted mode from `ISystemSettingsRepository`
2. If persisted mode is Hybrid and user lacks WriterPro+, fall back to tier default
3. If no persisted mode, use tier default (Hybrid for WriterPro+, Semantic for Core)
4. Log the initialized mode at Information level

### Design Adaptations

The specification referenced `ISettingsService` and `FeatureFlags.RAG.HybridSearch`, but the actual codebase:
- Uses `ISystemSettingsRepository` with async `GetValueAsync`/`SetValueAsync` methods
- Uses `FeatureCodes` static class for feature code constants
- License gating in `ReferenceViewModel` uses tier-level checking via `ILicenseContext.GetCurrentTier()` (matching the existing `SearchLicenseGuard` pattern) rather than feature code checking

### Dependencies

| Dependency | Version | Purpose |
|:-----------|:--------|:--------|
| ISemanticSearchService | v0.4.5a | Semantic search dispatch |
| IBM25SearchService | v0.5.1b | Keyword search dispatch |
| IHybridSearchService | v0.5.1c | Hybrid search dispatch |
| SearchLicenseGuard | v0.4.5b | Overall search license validation |
| ILicenseContext | v0.0.4c | Tier-level checking for Hybrid gating |
| ISystemSettingsRepository | v0.0.5b | Mode persistence |
| IMediator | v0.0.7a | Telemetry event publishing |

---

## Verification

### Unit Tests

All 39 tests passed:

- Constructor null-parameter validation (10 tests)
- Constructor initialization (5 tests)
- InitializeSearchModeAsync — tier defaults, persistence, fallback (9 tests)
- Search mode change license gating (7 tests)
- Preference persistence (2 tests)
- Telemetry events (1 test)
- CanUseHybrid per tier (4 tests)
- SearchMode enum correctness (3 tests)
- SearchModeChangedEvent properties (2 tests)

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
Passed: 5098, Skipped: 33, Failed: 0
```

---

## Related Documents

- [LCS-DES-v0.5.1d](../../specs/v0.5.x/v0.5.1/LCS-DES-v0.5.1d.md) — Design specification
- [LCS-CL-051c](./LCS-CL-051c.md) — Hybrid Fusion Algorithm (prerequisite)
- [LCS-CL-051b](./LCS-CL-051b.md) — BM25 Search Implementation (prerequisite)
- [LCS-CL-051a](./LCS-CL-051a.md) — BM25 Index Schema (prerequisite)
- [LCS-DES-v0.5.1-INDEX](../../specs/v0.5.x/v0.5.1/LCS-DES-v0.5.1-INDEX.md) — Feature index
