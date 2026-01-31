# LCS-CL-033d: Readability HUD Widget

**Version:** v0.3.3d  
**Release Date:** 2026-01-31  
**Status:** ✅ Complete  
**Specification:** [LCS-DES-033d](../../../specs/v0.3.x/LCS-DES-033d.md)

---

## Summary

Implements the Readability HUD Widget — a real-time visual display of readability metrics integrated into the Problems Panel header. The widget provides color-coded feedback for Flesch-Kincaid Grade Level, Gunning Fog Index, and Flesch Reading Ease with license gating for the Writer Pro tier.

---

## Added

### Abstractions (`Lexichord.Abstractions`)

- **`IReadabilityHudViewModel`** interface defining:
    - Properties: `IsLicensed`, `IsAnalyzing`, `HasMetrics`
    - Metric values: `FleschKincaidGradeLevel`, `GunningFogIndex`, `FleschReadingEase`
    - Word stats: `WordCount`, `SentenceCount`, `WordsPerSentence`
    - Display properties: `GradeLevelDisplay`, `FogIndexDisplay`, `ReadingEaseDisplay`
    - Color properties: `GradeLevelColor`, `ReadingEaseColor`, `FogIndexColor`
    - `InterpretationDisplay`, `GradeLevelDescription`
    - Methods: `UpdateAsync(string text, CancellationToken ct)`, `Reset()`

- **`FeatureCodes.ReadabilityHud`** feature code constant for license gating

### ViewModels (`Lexichord.Modules.Style`)

- **`ReadabilityHudViewModel`** CommunityToolkit.Mvvm implementation with:
    - Reactive properties via `[ObservableProperty]` source generator
    - `IReadabilityService` integration for metric calculation
    - `ILicenseService` integration for Writer Pro tier gating
    - Color-coded display based on readability thresholds
    - Grade level descriptions (Elementary → Graduate)
    - Exception handling with automatic reset on failure

### Views (`Lexichord.Modules.Style`)

- **`ReadabilityHudView.axaml`** Avalonia UI component with:
    - Compact header widget design
    - Color-coded metric displays
    - Tooltip overlays with detailed descriptions
    - License gating visibility controls

### Unit Tests (`Lexichord.Tests.Unit`)

- **`ReadabilityHudViewModelTests.cs`** with 77 test cases:
    - Constructor validation (6 tests)
    - `UpdateAsync` behavior (8 tests)
    - License gating (3 tests)
    - Display formatting (4 tests)
    - Interpretation mapping (14 theory tests)
    - Grade level descriptions (10 theory tests)
    - Color mapping (22 theory tests)
    - Reset behavior (2 tests)
    - PropertyChanged notifications (2 tests)
    - Interface implementation (1 test)

---

## Technical Notes

### Color Thresholds

| Metric       | Green (Easy) | Yellow      | Orange        | Red (Hard) |
| :----------- | :----------- | :---------- | :------------ | :--------- |
| Grade Level  | 0-6          | 7-9 / 10-12 | 13-15         | 16+        |
| Reading Ease | 80-100       | 60-79       | 40-59 / 20-39 | 0-19       |
| Fog Index    | 0-8          | 9-12        | —             | 13+        |

### Grade Level Descriptions

| Range | Description   |
| :---- | :------------ |
| 0-5   | Elementary    |
| 6-8   | Middle School |
| 9-12  | High School   |
| 13-16 | College       |
| 17+   | Graduate      |

### License Gating

- Feature requires Writer Pro tier (`FeatureCodes.ReadabilityHud`)
- License status checked on initialization and refreshed on each update
- Unlicensed users see placeholder widget with upgrade prompt

---

## Dependencies

| Interface             | Version | Purpose                        |
| :-------------------- | :------ | :----------------------------- |
| `IReadabilityService` | v0.3.3c | Metric calculation             |
| `ILicenseService`     | v0.1.6c | Feature entitlement checks     |
| `ILogger<T>`          | v0.0.3b | Structured logging             |
| CommunityToolkit.Mvvm | ^8.0    | Observable property generation |

---

## Changed Files

| File                                                                       | Change                 |
| :------------------------------------------------------------------------- | :--------------------- |
| `src/Lexichord.Abstractions/Contracts/IReadabilityHudViewModel.cs`         | **NEW**                |
| `src/Lexichord.Abstractions/Constants/FeatureCodes.cs`                     | Added `ReadabilityHud` |
| `src/Lexichord.Modules.Style/ViewModels/ReadabilityHudViewModel.cs`        | **NEW**                |
| `src/Lexichord.Modules.Style/Views/ReadabilityHudView.axaml`               | **NEW**                |
| `src/Lexichord.Modules.Style/ViewModels/ProblemsPanelViewModel.cs`         | Added HUD integration  |
| `src/Lexichord.Modules.Style/StyleModule.cs`                               | Added DI registration  |
| `tests/Lexichord.Tests.Unit/Modules/Style/ReadabilityHudViewModelTests.cs` | **NEW**                |
| `tests/Lexichord.Tests.Unit/Modules/Style/ProblemsPanelViewModelTests.cs`  | Updated constructor    |

---

## Verification

```bash
# Build verification
dotnet build
# Result: Build succeeded

# ReadabilityHudViewModel tests
dotnet test --filter "FullyQualifiedName~ReadabilityHudViewModelTests"
# Result: 77 passed, 0 failed

# Full test suite
dotnet test tests/Lexichord.Tests.Unit/
# Result: 2550 passed, 1 failed*, 33 skipped
# *Pre-existing failure in DebounceControllerTests
```

---

## What This Enables

- **Real-time Feedback:** Writers see live readability metrics as they type
- **v0.3.4 (Writing Coach):** AI-powered suggestions leveraging HUD metrics
- **License Value:** Writer Pro tier differentiation with premium feature
