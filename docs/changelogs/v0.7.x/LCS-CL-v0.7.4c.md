# Changelog: v0.7.4c — Preview/Diff UI

**Feature ID:** AGT-074c
**Version:** 0.7.4c
**Date:** 2026-02-15
**Status:** ✅ Complete

---

## Overview

Implements the Preview/Diff UI for the Simplifier Agent, providing an interactive preview interface that shows before/after text comparison with readability metrics. This is the third sub-part of v0.7.4 "The Simplifier Agent" and builds upon v0.7.4a's Readability Target Service and v0.7.4b's Simplification Pipeline.

The implementation adds:
- `SimplificationPreviewViewModel` — main ViewModel orchestrating preview experience
- `SimplificationChangeViewModel` — ObservableObject wrapper for individual changes
- Three MediatR events for workflow coordination (`SimplificationAcceptedEvent`, `SimplificationRejectedEvent`, `ResimplificationRequestedEvent`)
- `DiffViewMode` enum for view mode selection (SideBySide, Inline, ChangesOnly)
- DiffPlex integration for text diff visualization
- Avalonia UI components for the preview interface
- Comprehensive unit tests for ViewModels and events

---

## What's New

### SimplificationPreviewViewModel

Main ViewModel for the preview/diff UI:
- **Namespace:** `Lexichord.Modules.Agents.Simplifier`
- **Base Class:** `DisposableViewModel` (automatic subscription cleanup)
- **Dependencies:** `ISimplificationPipeline`, `IReadabilityTargetService`, `IEditorService`, `IMediator`, `ILicenseContext`, `ILogger`
- **Commands:**
  - `AcceptAllCommand` — Accepts all changes and applies to document
  - `AcceptSelectedCommand` — Accepts only selected changes
  - `RejectAllCommand` — Rejects changes and closes preview
  - `ResimplifyCommand` — Re-runs simplification with new preset
  - `ToggleChangeCommand` — Toggles individual change selection
  - `SelectAllChangesCommand` / `DeselectAllChangesCommand`
  - `SetViewModeCommand` — Switches between diff view modes
- **Properties:**
  - `OriginalText`, `SimplifiedText` — Before/after text
  - `OriginalMetrics`, `SimplifiedMetrics` — Readability metrics comparison
  - `GradeLevelReduction`, `TargetAchieved` — Achievement indicators
  - `Changes` — Collection of `SimplificationChangeViewModel`
  - `ViewMode` — Current diff view mode
  - `AvailablePresets`, `SelectedPreset` — Preset selection
  - `IsLoading`, `IsProcessing`, `ErrorMessage` — State indicators
  - `IsLicensed`, `LicenseWarning` — License gating
- **Events:**
  - `CloseRequested` — Signals when preview should close

### SimplificationChangeViewModel

Observable wrapper for `SimplificationChange`:
- **Namespace:** `Lexichord.Modules.Agents.Simplifier`
- **Observable Properties:** `IsSelected`, `IsExpanded`, `IsHighlighted`
- **Read-Only Properties:** `Index`, `Change`, `OriginalText`, `SimplifiedText`, `ChangeType`, `Explanation`, `Confidence`, `Location`, `IsReduction`, `LengthDifference`
- **Display Properties:**
  - `ChangeTypeDisplay` — Human-readable change type (e.g., "Word Simplified")
  - `ChangeTypeIcon` — Icon key (e.g., "TypeIcon")
  - `ChangeTypeBadgeClass` — CSS class (e.g., "badge-orange")
  - `OriginalTextPreview`, `SimplifiedTextPreview` — Truncated previews (50 chars)
- **Computed Properties:**
  - `CanAccept` — True when selected

### MediatR Events

Three notification records for workflow coordination:

**SimplificationAcceptedEvent:**
- **Properties:** `DocumentPath`, `OriginalText`, `SimplifiedText`, `AcceptedChangeCount`, `TotalChangeCount`, `GradeLevelReduction`
- **Computed:** `IsPartialAcceptance`, `AcceptanceRate`

**SimplificationRejectedEvent:**
- **Properties:** `DocumentPath`, `Reason`
- **Constants:** `ReasonUserCancelled`, `ReasonPreviewClosed`, `ReasonDocumentClosed`, `ReasonLicenseExpired`
- **Factories:** `UserCancelled(path)`, `PreviewClosed(path)`

**ResimplificationRequestedEvent:**
- **Properties:** `DocumentPath`, `OriginalText`, `NewPresetId`, `NewStrategy`
- **Computed:** `IsPresetChange`, `IsStrategyChange`

### DiffViewMode Enum

View mode selection:
- `SideBySide` (0) — Two-column comparison
- `Inline` (1) — Unified diff with +/- markers
- `ChangesOnly` (2) — Card-based change list

### CloseRequestedEventArgs

Event args for preview close:
- **Properties:** `Accepted`
- **Singletons:** `AcceptedClose`, `RejectedClose`

### Avalonia UI Components

**Views:**
- `SimplificationPreviewView.axaml` — Main preview panel with metrics, tabs, and actions
- `ReadabilityComparisonPanel.axaml` — Before/after metrics comparison

**Controls:**
- `DiffTextBox.axaml` — Side-by-side diff using DiffPlex
- `InlineDiffView.axaml` — Unified diff with +/- markers
- `ChangesOnlyView.axaml` — Card-based change list with checkboxes

### DiffPlex Integration

Added DiffPlex 1.7.2 NuGet package for text diff visualization:
- `Differ` — Core diff algorithm
- `SideBySideDiffBuilder` — For side-by-side view
- `InlineDiffBuilder` — For unified view
- Color-coded change highlighting (red/green/yellow)
- Line numbers in gutters
- Synchronized scrolling between panels

### DI Registration

Extended `SimplifierServiceCollectionExtensions` with `AddSimplifierPreviewUI()`:
```csharp
services.AddTransient<SimplificationPreviewViewModel>();
```

Updated `AgentsModule.RegisterServices()` with `services.AddSimplifierPreviewUI()` call.

---

## Files Created

### Abstractions — Events (3 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Agents/Simplifier/Events/SimplificationAcceptedEvent.cs` | Record | Published when changes are accepted |
| `src/Lexichord.Abstractions/Agents/Simplifier/Events/SimplificationRejectedEvent.cs` | Record | Published when changes are rejected |
| `src/Lexichord.Abstractions/Agents/Simplifier/Events/ResimplificationRequestedEvent.cs` | Record | Published for re-simplification |

### Module — ViewModels (4 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Simplifier/DiffViewMode.cs` | Enum | View mode selection |
| `src/Lexichord.Modules.Agents/Simplifier/CloseRequestedEventArgs.cs` | Class | Close event args |
| `src/Lexichord.Modules.Agents/Simplifier/SimplificationChangeViewModel.cs` | Class | Change wrapper VM |
| `src/Lexichord.Modules.Agents/Simplifier/SimplificationPreviewViewModel.cs` | Class | Main preview VM |

### Module — Views (4 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Simplifier/Views/SimplificationPreviewView.axaml` | AXAML | Main preview panel |
| `src/Lexichord.Modules.Agents/Simplifier/Views/SimplificationPreviewView.axaml.cs` | C# | Code-behind |
| `src/Lexichord.Modules.Agents/Simplifier/Views/ReadabilityComparisonPanel.axaml` | AXAML | Metrics panel |
| `src/Lexichord.Modules.Agents/Simplifier/Views/ReadabilityComparisonPanel.axaml.cs` | C# | Code-behind |

### Module — Controls (6 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Simplifier/Controls/DiffTextBox.axaml` | AXAML | Side-by-side diff |
| `src/Lexichord.Modules.Agents/Simplifier/Controls/DiffTextBox.axaml.cs` | C# | DiffPlex integration |
| `src/Lexichord.Modules.Agents/Simplifier/Controls/InlineDiffView.axaml` | AXAML | Unified diff |
| `src/Lexichord.Modules.Agents/Simplifier/Controls/InlineDiffView.axaml.cs` | C# | Code-behind |
| `src/Lexichord.Modules.Agents/Simplifier/Controls/ChangesOnlyView.axaml` | AXAML | Change list |
| `src/Lexichord.Modules.Agents/Simplifier/Controls/ChangesOnlyView.axaml.cs` | C# | Code-behind |

### Tests (3 files)

| File | Tests | Description |
|:-----|:-----:|:------------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/Simplifier/SimplificationChangeViewModelTests.cs` | 24 | Change VM tests |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Simplifier/SimplificationEventsTests.cs` | 18 | MediatR event tests |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Simplifier/SimplificationPreviewViewModelTests.cs` | 29 | Preview VM tests |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Modules.Agents/Lexichord.Modules.Agents.csproj` | Added DiffPlex 1.7.2 NuGet package |
| `src/Lexichord.Modules.Agents/Extensions/SimplifierServiceCollectionExtensions.cs` | Added `AddSimplifierPreviewUI()` method |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added `services.AddSimplifierPreviewUI()` call |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| SimplificationChangeViewModelTests | 24 | Constructor, observables, display properties |
| SimplificationEventsTests | 18 | Event records, computed properties, factories |
| SimplificationPreviewViewModelTests | 29 | Commands, state management, events |
| **Total v0.7.4c** | **71** | All v0.7.4c functionality |
| **Total v0.7.4** | **163** | Combined a+b+c |

### Test Groups

| Group | Tests | Description |
|:------|:------|:------------|
| ChangeVM Constructor | 5 | Null check, property initialization |
| ChangeVM Observable Properties | 4 | Selection, expansion, highlighting |
| ChangeVM Display Properties | 9 | Type display, icons, badges |
| ChangeVM Truncation | 3 | Preview text truncation |
| ChangeVM Computed | 4 | Reduction, length difference, location |
| AcceptedEvent | 6 | Partial acceptance, rate, properties |
| RejectedEvent | 6 | Constants, factories, null handling |
| ResimplificationEvent | 6 | Preset/strategy changes, properties |
| PreviewVM Constructor | 6 | Dependency null checks |
| PreviewVM Initialize | 3 | Preset loading, selection capture |
| PreviewVM SetResult | 7 | Text, metrics, changes, error state |
| PreviewVM Selection | 3 | Selection count, all selected |
| PreviewVM Commands | 6 | CanExecute, event publishing |
| PreviewVM ViewMode | 4 | Mode switching |
| PreviewVM CloseRequested | 2 | Accept/reject events |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.4c")]`

---

## Design Decisions

1. **Transient ViewModel Lifetime** — `SimplificationPreviewViewModel` is registered as Transient because each preview instance requires isolated state (original text, changes, selection).

2. **DisposableViewModel Base** — Inheriting from `DisposableViewModel` provides automatic subscription cleanup via `Track()` method.

3. **DiffPlex for Diffs** — Industry-standard text diff library (MIT license) with optimized algorithms for side-by-side and inline visualization.

4. **MediatR Events** — Using `INotification` for decoupled workflow coordination. Events can be handled by analytics, logging, or cleanup handlers.

5. **Keyboard Shortcuts** — `Ctrl+Enter` for Accept All, `Escape` for Reject. Defined via `UserControl.KeyBindings`.

6. **License Gating** — Checks `FeatureCodes.SimplifierAgent` via `ILicenseContext.IsFeatureEnabled()`. Unlicensed users see warning and disabled accept buttons.

7. **Undo Group** — Uses `IEditorService.BeginUndoGroup()` / `EndUndoGroup()` to wrap text replacement for atomic undo.

8. **Partial Acceptance** — `BuildMergedText()` applies only selected changes using their `Location` offsets in reverse order to maintain position accuracy.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Version | Used By |
|:----------|:--------|:--------|
| `ISimplificationPipeline` | v0.7.4b | ResimplifyCommand |
| `IReadabilityTargetService` | v0.7.4a | Preset loading |
| `IEditorService` | v0.6.7b | Apply changes, undo groups |
| `IMediator` | v0.6.6d | Event publishing |
| `ILicenseContext` | v0.0.4c | License validation |
| `DisposableViewModel` | v0.3.7d | Base class |
| `SimplificationResult` | v0.7.4b | Result display |
| `SimplificationChange` | v0.7.4b | Change wrapping |
| `AudiencePreset` | v0.7.4a | Preset selection |
| `ReadabilityMetrics` | v0.3.3c | Metrics display |

### Produced (new classes)

| Class | Consumers |
|:------|:----------|
| `SimplificationPreviewViewModel` | Preview UI, command handlers |
| `SimplificationChangeViewModel` | Changes list UI |
| `SimplificationAcceptedEvent` | Analytics, logging handlers |
| `SimplificationRejectedEvent` | Analytics, cleanup handlers |
| `ResimplificationRequestedEvent` | Re-simplification handlers |
| `DiffViewMode` | View mode selection |
| `CloseRequestedEventArgs` | View close coordination |

### New NuGet Package

| Package | Version | Usage |
|:--------|:--------|:------|
| DiffPlex | 1.7.2 | Text diff visualization |

---

## UI Components

### SimplificationPreviewView

Main preview panel layout:
1. **Header** — Title, preset selector, re-simplify button
2. **License Warning** — Shows if not WriterPro tier
3. **Metrics Panel** — ReadabilityComparisonPanel
4. **Diff Tabs** — Side by Side / Inline / Changes Only
5. **Diff Content** — Active view based on selected mode
6. **Action Buttons** — Reject, Accept Selected, Accept All

### ReadabilityComparisonPanel

Metrics comparison layout:
- Original metrics card (grade level, reading ease, word count)
- Arrow with grade reduction badge
- Simplified metrics card (highlighted green)
- Target info bar with achievement status

### DiffTextBox

Side-by-side visualization:
- Two-column layout
- Line numbers in gutters
- Color-coded change highlighting
- Synchronized scrolling between panels

### InlineDiffView

Unified diff visualization:
- Single column with +/- markers
- Red background for deletions
- Green background for insertions
- Header with +/- counts

### ChangesOnlyView

Card-based change list:
- Selection checkboxes
- Change type badges (color-coded)
- Before → After text comparison
- Expandable explanations
- Length difference indicators

---

## Keyboard Shortcuts

| Shortcut | Action |
|:---------|:-------|
| `Ctrl+Enter` | Accept All changes |
| `Escape` | Reject and close |

---

## License Gating

| Feature | Core | Writer | WriterPro | Teams |
|:--------|:-----|:-------|:----------|:------|
| Preview UI display | - | - | ✓ | ✓ |
| Accept changes | - | - | ✓ | ✓ |
| Re-simplify | - | - | ✓ | ✓ |
| Reject (always) | ✓ | ✓ | ✓ | ✓ |

---

## Build & Test Results

```
Build:     Succeeded (0 errors, 0 warnings)
v0.7.4c:   71 passed, 0 failed
v0.7.4:    163 passed, 0 failed (combined a+b+c)
```
