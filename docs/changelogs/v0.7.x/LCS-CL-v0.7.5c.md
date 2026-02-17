# Changelog: v0.7.5c — Accept/Reject UI

**Feature ID:** AGT-075c
**Version:** 0.7.5c
**Date:** 2026-02-15
**Status:** Complete

---

## Overview

Implements the Accept/Reject UI for the Tuning Agent, building upon v0.7.5a's Style Deviation Scanner and v0.7.5b's Automatic Fix Suggestions. This is the third sub-part of v0.7.5 "The Tuning Agent" and provides an interactive panel for reviewing, accepting, rejecting, or modifying AI-generated fix suggestions.

The implementation adds:
- `TuningPanelViewModel` — orchestrating scan, review, accept/reject/modify/skip commands, bulk actions, navigation, and filtering
- `SuggestionCardViewModel` — per-suggestion UI state wrapper with computed display properties
- `TuningUndoableOperation` — atomic undo/redo for applied fix suggestions
- `SuggestionStatus` enum — Pending, Accepted, Rejected, Modified, Skipped
- `SuggestionFilter` enum — All, Pending, HighConfidence, HighPriority
- `SuggestionAcceptedEvent` / `SuggestionRejectedEvent` — MediatR analytics events
- Full unit test coverage (76 tests)

---

## What's New

### TuningPanelViewModel

Main orchestrator for the Tuning Panel experience:
- **Namespace:** `Lexichord.Modules.Agents.Tuning`
- **Base Class:** `DisposableViewModel`
- **Dependencies:** `IStyleDeviationScanner`, `IFixSuggestionGenerator`, `IEditorService`, `IUndoRedoService?` (nullable), `ILicenseContext`, `IMediator`, `ILogger<TuningPanelViewModel>`
- **Observable Properties:**
  - `Suggestions` — ObservableCollection of suggestion cards
  - `SelectedSuggestion` — Currently selected card
  - `IsScanning`, `IsGeneratingFixes`, `IsBulkProcessing` — Operation flags
  - `TotalDeviations`, `ReviewedCount`, `AcceptedCount`, `RejectedCount` — Counters
  - `CurrentFilter` — Active filter selection
  - `StatusMessage` — UI status text
  - `ProgressPercent` — Operation progress (0-100)
  - `HasWriterProLicense`, `HasTeamsLicense` — License state
- **Computed Properties:**
  - `HighConfidenceCount` — Pending high-confidence count
  - `RemainingCount` — Pending suggestion count
  - `FilteredSuggestions` — Filter-aware suggestion view
- **Commands:**
  - `ScanDocumentCommand` — Full scan-generate-display pipeline
  - `AcceptSuggestionCommand` — Apply suggestion via sync editor APIs
  - `RejectSuggestionCommand` — Reject without document modification
  - `SkipSuggestionCommand` — Defer without recording feedback
  - `AcceptAllHighConfidenceCommand` — Bulk accept in reverse document order
  - `NavigateNextCommand` / `NavigatePreviousCommand` — Keyboard navigation
  - `CloseCommand` — Panel close request
- **Public Methods:**
  - `ModifySuggestionAsync()` — Apply user-modified text
  - `RegenerateSuggestionAsync()` — Regenerate with guidance
  - `InitializeAsync()` — License check initialization
- **Events:**
  - `CloseRequested` — Panel close event

### SuggestionCardViewModel

Per-suggestion UI state wrapper:
- **Namespace:** `Lexichord.Modules.Agents.Tuning`
- **Base Class:** `ObservableObject`
- **Observable Properties:**
  - `IsExpanded` — Card expansion state
  - `IsReviewed` — Whether any action taken
  - `Status` — SuggestionStatus enum value
  - `ModifiedText` — User-edited replacement text
  - `ShowAlternatives` — Alternatives list expansion
  - `SelectedAlternative` — Current alternative selection
  - `IsRegenerating` — Loading indicator state
- **Computed Display Properties:**
  - `RuleName`, `RuleCategory`, `Priority`
  - `Confidence`, `QualityScore`, `IsHighConfidence`
  - `Diff`, `OriginalText`, `SuggestedText`, `Explanation`
  - `Alternatives`, `HasAlternatives`
  - `ConfidenceDisplay`, `QualityDisplay`, `PriorityDisplay`
- **Methods:**
  - `UpdateSuggestion()` — Replace suggestion after regeneration
- **Commands:**
  - `ToggleAlternativesCommand` — Toggle alternatives visibility

### TuningUndoableOperation

Atomic undo/redo for applied fix suggestions:
- **Namespace:** `Lexichord.Modules.Agents.Tuning`
- **Implements:** `IUndoableOperation`
- **Properties:**
  - `Id` — Unique operation ID (Guid)
  - `DisplayName` — Format: "Tuning Fix ({RuleId})"
  - `Timestamp` — UTC creation time
- **Methods:**
  - `ExecuteAsync()` — Apply fix text
  - `UndoAsync()` — Restore original text
  - `RedoAsync()` — Reapply fix text
- **Editor Pattern:** `BeginUndoGroup` / `DeleteText` / `InsertText` / `EndUndoGroup`

### SuggestionStatus Enum

Review status for individual suggestions:
- **Namespace:** `Lexichord.Abstractions.Contracts.Agents`
- **Values:**
  - `Pending = 0` — Awaiting review
  - `Accepted = 1` — Applied to document
  - `Rejected = 2` — Declined by user
  - `Modified = 3` — Applied with user edits
  - `Skipped = 4` — Deferred for later

### SuggestionFilter Enum

Filter modes for the suggestions list:
- **Namespace:** `Lexichord.Abstractions.Contracts.Agents`
- **Values:**
  - `All = 0` — Show all suggestions
  - `Pending = 1` — Only pending suggestions
  - `HighConfidence = 2` — Confidence and quality >= 0.9
  - `HighPriority = 3` — Priority >= High

### SuggestionAcceptedEvent

MediatR notification for accepted suggestions:
- **Namespace:** `Lexichord.Abstractions.Contracts.Agents.Events`
- **Properties:**
  - `Deviation` — Source style deviation
  - `Suggestion` — Accepted fix suggestion
  - `ModifiedText` — User-modified text (nullable)
  - `IsModified` — Whether text was modified
  - `Timestamp` — Event timestamp
- **Computed:**
  - `AppliedText` — ModifiedText ?? Suggestion.SuggestedText
- **Factory Methods:**
  - `Create()` — Standard acceptance
  - `CreateModified()` — Acceptance with modification

### SuggestionRejectedEvent

MediatR notification for rejected suggestions:
- **Namespace:** `Lexichord.Abstractions.Contracts.Agents.Events`
- **Properties:**
  - `Deviation` — Source style deviation
  - `Suggestion` — Rejected fix suggestion
  - `Timestamp` — Event timestamp
- **Factory Methods:**
  - `Create()` — Standard rejection

### FeatureCodes.TuningAgent

License feature code for the Tuning Agent:
- **Constant:** `"Feature.TuningAgent"`
- **Location:** `Lexichord.Abstractions.Constants.FeatureCodes`
- **Used By:** `TuningPanelViewModel.InitializeAsync()`

### DI Registration

Extended `TuningServiceCollectionExtensions` with `AddTuningReviewUI()`:
```csharp
services.AddTransient<TuningPanelViewModel>();
```

Updated `AgentsModule.RegisterServices()` with `services.AddTuningReviewUI()` call.

---

## Spec Adaptations

The design spec (LCS-DES-v0.7.5c.md) references several APIs that don't match the actual codebase:

1. **IEditorService is SYNC** — Spec uses `GetActiveDocumentPathAsync()`, `ReplaceTextAsync()`, `BeginUndoGroupAsync()`, `EndUndoGroupAsync()`. Adapted to use sync APIs: `CurrentDocumentPath` (property), `BeginUndoGroup()`, `DeleteText()`, `InsertText()`, `EndUndoGroup()`.

2. **IUndoRedoService** — Spec conflates editor undo groups with the higher-level undo service. Implementation correctly uses both: editor undo groups for atomic Ctrl+Z, and `IUndoRedoService.Push()` for labeled undo history. Accepted as nullable.

3. **ILearningLoopService** — Does not exist (v0.7.5d). Omitted entirely rather than creating stubs.

4. **ViewModelBase** — Does not exist. Used `DisposableViewModel` for TuningPanelViewModel and `ObservableObject` for SuggestionCardViewModel.

5. **ShowUpgradePromptEvent** — Does not exist. Used existing `ShowUpgradeModalEvent`.

6. **Namespace** — Spec says `Lexichord.Host.ViewModels.Agents`. Actual pattern: `Lexichord.Modules.Agents.Tuning`.

7. **Multi-parameter commands** — `ModifySuggestionAsync` and `RegenerateSuggestionAsync` require two parameters, which is incompatible with `[RelayCommand]`. Made them public methods callable from view code-behind.

---

## Files Created

### Abstractions — Enums (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Agents/SuggestionStatus.cs` | Enum | Review status |
| `src/Lexichord.Abstractions/Contracts/Agents/SuggestionFilter.cs` | Enum | Filter modes |

### Abstractions — Events (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Agents/Events/SuggestionAcceptedEvent.cs` | MediatR Event | Accepted analytics |
| `src/Lexichord.Abstractions/Contracts/Agents/Events/SuggestionRejectedEvent.cs` | MediatR Event | Rejected analytics |

### Module — ViewModels (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Tuning/TuningPanelViewModel.cs` | ViewModel | Panel orchestrator |
| `src/Lexichord.Modules.Agents/Tuning/SuggestionCardViewModel.cs` | ViewModel | Per-card state |

### Module — Undo (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Tuning/TuningUndoableOperation.cs` | Undo Op | Atomic undo/redo |

### Tests (1 file)

| File | Tests | Description |
|:-----|:-----:|:------------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/Tuning/TuningPanelViewModelTests.cs` | 76 | Full ViewModel tests |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Abstractions/Constants/FeatureCodes.cs` | Added `TuningAgent` constant |
| `src/Lexichord.Modules.Agents/Extensions/TuningServiceCollectionExtensions.cs` | Added `AddTuningReviewUI()` method |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added `services.AddTuningReviewUI()` call and init verification |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| TuningPanelViewModelTests | 76 | Constructor, scan, accept, reject, modify, skip, bulk, navigation, filter, card, undo, dispose |
| **Total v0.7.5c** | **76** | All v0.7.5c functionality |

### Test Groups

| Group | Tests | Description |
|:------|:-----:|:------------|
| Constructor | 8 | Null checks, nullable undo, default state |
| InitializeAsync | 4 | License checks, status messages |
| ScanDocumentAsync | 8 | Success, no doc, empty, license gate, errors |
| AcceptSuggestionAsync | 7 | Editor calls, status, events, undo, guards |
| RejectSuggestionAsync | 4 | Status, no editor calls, event, guard |
| ModifySuggestionAsync | 4 | Modified text, status, event, null guard |
| SkipSuggestion | 3 | Status, no counters, no event |
| AcceptAllHighConfidence | 5 | Reverse order, undo group, events, empty |
| RegenerateSuggestionAsync | 3 | Update, null guard, error |
| NavigateNext/Previous | 5 | Skip non-pending, wrap-around, empty |
| FilteredSuggestions | 4 | All, Pending, HighConfidence, HighPriority |
| Computed Properties | 2 | HighConfidenceCount, RemainingCount |
| Close | 1 | CloseRequested event |
| SuggestionCardViewModel | 7 | Constructor, computed, update, toggle, defaults |
| TuningUndoableOperation | 7 | Execute, undo, redo, properties, null guards |
| Dispose | 2 | Clean dispose, double dispose |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.5c")]`

---

## Design Decisions

1. **Transient ViewModel Lifetime** — `TuningPanelViewModel` is registered as Transient because each panel instance manages its own scan/review state. Multiple panels should have isolated suggestion collections.

2. **No DI for SuggestionCardViewModel** — Card instances are created manually by `TuningPanelViewModel` during the scan flow, not via dependency injection. This matches the `SimplificationChangeViewModel` pattern.

3. **Nullable IUndoRedoService** — The undo service may not be registered in all configurations. Accepted as nullable and guarded with null checks. Editor-level undo groups always work regardless.

4. **Two-Level Undo** — Editor undo groups provide atomic Ctrl+Z at the editor level. `IUndoRedoService.Push()` provides labeled undo history in the UI. Both are used together.

5. **Reverse Document Order for Bulk Accept** — High-confidence suggestions are applied in reverse document order to preserve text offsets. Later changes don't shift the offsets of earlier ones.

6. **Public Methods for Multi-Parameter Commands** — `ModifySuggestionAsync` and `RegenerateSuggestionAsync` are public methods (not relay commands) because CommunityToolkit.Mvvm's `[RelayCommand]` only supports 0 or 1 parameter. View code-behind calls them directly.

7. **No ILearningLoopService** — Omitted entirely rather than creating stubs. All learning loop call sites will be added in v0.7.5d.

8. **No AXAML Views** — ViewModels are the core deliverable. AXAML views (TuningPanelView, SuggestionCardView) are presentation-layer concerns in the Host project and can be wired up separately.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Version | Used By |
|:----------|:--------|:--------|
| `IStyleDeviationScanner` | v0.7.5a | Deviation detection |
| `IFixSuggestionGenerator` | v0.7.5b | Fix generation |
| `IEditorService` | v0.6.7b | Document editing (sync APIs) |
| `IUndoRedoService` | v0.7.3d | Labeled undo history (nullable) |
| `ILicenseContext` | v0.0.4c | License validation |
| `IMediator` | v0.0.7a | Event publishing |
| `DisposableViewModel` | v0.7.3d | Base class with dispose tracking |
| `ShowUpgradeModalEvent` | v0.7.3d | License upgrade prompt |
| `FeatureCodes` | v0.0.4c | Feature code constants |

### Produced (new types)

| Type | Consumers |
|:-----|:----------|
| `TuningPanelViewModel` | Tuning Panel View (v0.7.5c+) |
| `SuggestionCardViewModel` | Suggestion Card View (v0.7.5c+) |
| `TuningUndoableOperation` | TuningPanelViewModel |
| `SuggestionStatus` | ViewModels, filters |
| `SuggestionFilter` | TuningPanelViewModel |
| `SuggestionAcceptedEvent` | Analytics, learning loop (v0.7.5d) |
| `SuggestionRejectedEvent` | Analytics, learning loop (v0.7.5d) |
| `FeatureCodes.TuningAgent` | License validation |

---

## License Gating

| Feature | Core | Writer | WriterPro | Teams |
|:--------|:-----|:-------|:----------|:------|
| Tuning Panel scan | - | - | ✓ | ✓ |
| Accept/Reject actions | - | - | ✓ | ✓ |
| Bulk accept | - | - | ✓ | ✓ |
| Regeneration | - | - | ✓ | ✓ |

---

## Build & Test Results

```
Build:     Succeeded (0 errors, 1 warning - unrelated Avalonia)
v0.7.5c:   76 passed, 0 failed
v0.7.5a-c: 149 passed, 0 failed
Full suite: 5405 passed, 0 failed, 27 skipped
```
