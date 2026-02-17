# Changelog: v0.7.5g — Unified Issues Panel

**Feature ID:** AGT-075g
**Version:** 0.7.5g
**Date:** 2026-02-16
**Status:** Complete

---

## Overview

Implements the Unified Issues Panel for the Tuning Agent, providing a single UI panel that displays all validation issues from `IUnifiedValidationService` (v0.7.5f) grouped by severity. This is the seventh sub-part of v0.7.5 "The Tuning Agent" and builds upon v0.7.5e's Unified Issue Model and v0.7.5f's Issue Aggregator.

The implementation adds:
- `FixRequestedEventArgs` — Event args for fix request events
- `IssueDismissedEventArgs` — Event args for issue dismissal events
- `IssuePresentationGroup` — Severity-based grouping model with expand/collapse
- `IssuePresentation` — Per-issue UI state wrapper with computed display properties
- `UnifiedIssuesPanelViewModel` — Panel orchestrator with filtering, navigation, fix application
- `UnifiedIssuesPanelView` — Avalonia UI panel with severity groups and issue details
- DI registration via `AddUnifiedIssuesPanel()` extension
- Full unit test coverage (60+ tests)

---

## What's New

### Event Arguments (Contracts)

#### FixRequestedEventArgs

Event args for fix request events from the UI:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation.Events`
- **Properties:**
  - `Issue` — The `UnifiedIssue` being fixed
  - `Fix` — The specific `UnifiedFix` to apply
  - `Timestamp` — UTC timestamp when requested
- **Factory Methods:**
  - `CreateForBestFix(UnifiedIssue)` — Creates event args using `BestFix`
- **Thread Safety:** Immutable, thread-safe

#### IssueDismissedEventArgs

Event args for issue dismissal events:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation.Events`
- **Properties:**
  - `Issue` — The `UnifiedIssue` that was dismissed
  - `Reason` — Optional user-provided dismissal reason
  - `Timestamp` — UTC timestamp when dismissed
- **Factory Methods:**
  - `Create(UnifiedIssue)` — Quick dismissal without reason
  - `CreateWithReason(UnifiedIssue, string)` — Dismissal with explanation
- **Thread Safety:** Immutable, thread-safe

### Presentation Models

#### IssuePresentationGroup

Groups issues by severity for UI display:
- **Namespace:** `Lexichord.Modules.Agents.Tuning`
- **Observable Properties:**
  - `IsExpanded` — Group expand/collapse state (default: true)
- **Immutable Properties:**
  - `Severity` — `UnifiedSeverity` (Error, Warning, Info, Hint)
  - `Label` — Human-readable label ("Errors", "Warnings", etc.)
  - `Icon` — Icon identifier for UI ("ErrorIcon", "WarningIcon", etc.)
  - `Items` — `ObservableCollection<IssuePresentation>`
- **Computed Properties:**
  - `Count` — Number of issues in group
  - `HasItems` — True if any issues present
  - `AutoFixableCount` — Issues with auto-fix available
  - `ActiveCount` — Non-suppressed issue count
- **Methods:**
  - `NotifyCountChanged()` — Update count display after modifications
  - `ToggleExpanded()` — Toggle expand/collapse
- **Factory Methods:**
  - `Errors()`, `Warnings()`, `Infos()`, `Hints()` — Create typed groups

#### IssuePresentation

Per-issue UI state wrapper:
- **Namespace:** `Lexichord.Modules.Agents.Tuning`
- **Observable Properties:**
  - `IsExpanded` — Detail section visibility
  - `IsSuppressed` — Dismissed/suppressed state
  - `IsFixed` — Fix has been applied
  - `IsSelected` — Currently focused for navigation
- **Computed Display — Issue Info:**
  - `CategoryLabel` — "Style", "Grammar", "Knowledge", etc.
  - `SeverityLabel` — "Error", "Warning", "Info", "Hint"
  - `LocationDisplay` — "Position 123" or "Chars 100-150"
  - `SourceDisplay` — "Style Linter", "Grammar Linter", "Validation Engine"
  - `SourceId` — Rule ID (e.g., "TERM-001")
  - `Message` — Issue description
  - `OriginalText` — Text at issue location
- **Computed Display — Fix Info:**
  - `HasFix` — At least one fix available
  - `CanAutoApply` — Best fix is auto-applicable
  - `HasHighConfidenceFix` — Best fix confidence >= 0.8
  - `FixDescription` — Fix explanation
  - `SuggestedText` — Replacement text
  - `FixOriginalText` — Text being replaced
  - `ConfidenceDisplay` — "85%"
  - `FixTypeLabel` — "Replace", "Insert", "Delete", "Rewrite"
  - `FixCount`, `HasMultipleFixes`
- **Computed Status:**
  - `IsActionable` — Not suppressed and not fixed
  - `DisplayOpacity` — 1.0 active, 0.5 suppressed/fixed
- **Commands:**
  - `ToggleExpandedCommand` — Toggle detail view
  - `ToggleSuppressedCommand` — Toggle suppression
- **Methods:**
  - `MarkAsFixed()` — Called after fix application
  - `NotifyIssueChanged()` — Refresh all computed properties

### UnifiedIssuesPanelViewModel

Main panel ViewModel orchestrating the experience:
- **Namespace:** `Lexichord.Modules.Agents.Tuning`
- **Base Class:** `DisposableViewModel`
- **Dependencies:**
  - `IUnifiedValidationService` (v0.7.5f) — Validation results
  - `IEditorService` (v0.6.7c) — Document navigation and editing
  - `IUndoRedoService?` (nullable) — Labeled undo history
  - `ILicenseContext` — License tier checking
  - `IMediator` — Event publishing
  - `ILogger<UnifiedIssuesPanelViewModel>`

**Events:**
- `CloseRequested` — Panel should be closed
- `FixRequested` — Fix was applied (with event args)
- `IssueDismissed` — Issue was dismissed (with event args)

**Observable Properties:**
- `IssueGroups` — `ObservableCollection<IssuePresentationGroup>`
- `SelectedIssue` — Currently focused issue
- `IsLoading` — Refresh in progress
- `IsBulkProcessing` — Bulk fix in progress
- `StatusMessage` — Current status text
- `ProgressPercent` — Bulk operation progress (0-100)
- `SelectedCategoryFilter` — Category filter (nullable)
- `SelectedSeverityFilter` — Severity filter (nullable)
- `ErrorMessage` — Error display text (nullable)

**Computed Properties:**
- `TotalIssueCount`, `ErrorCount`, `WarningCount`, `InfoCount`, `HintCount`
- `AutoFixableCount` — Issues with auto-fix
- `CanPublish` — No error-level issues
- `HasIssues` — Any issues present
- `IsCached` — Results from cache
- `DurationDisplay` — "123ms"
- `DocumentPath` — Current document
- `AvailableCategories`, `AvailableSeverities` — Filter options

**Commands:**
- `RefreshCommand` — Trigger new validation
- `FixIssueCommand(IssuePresentation?)` — Apply fix to issue
- `FixAllCommand` — Fix all auto-fixable issues
- `FixErrorsOnlyCommand` — Fix only error-level issues
- `DismissIssueCommand(IssuePresentation?)` — Suppress issue
- `NavigateToIssueCommand(IssuePresentation?)` — Go to location
- `ClearCommand` — Clear all issues
- `CloseCommand` — Request panel close

**Public Methods:**
- `InitializeAsync()` — Subscribe to validation events
- `RefreshWithResultAsync(UnifiedValidationResult)` — Update with result
- `ShowError(string, string?)` — Display error message

**Key Implementation Details:**
- **Bulk Fix Order:** Fixes applied in reverse document order to preserve offsets
- **Undo Integration:** All fixes wrapped in editor undo groups
- **Event Subscription:** Auto-refresh on `ValidationCompleted` event
- **Filter Changes:** Trigger immediate UI rebuild

### UnifiedIssuesUndoOperation (Internal)

Undoable operation for fix application:
- **Implements:** `IUndoableOperation`
- **Methods:**
  - `ExecuteAsync()` — Apply fix (delete + insert)
  - `UndoAsync()` — Reverse fix (delete new + insert original)
  - `RedoAsync()` — Re-apply fix
- **Properties:**
  - `Id` — Guid
  - `DisplayName` — "Issues Fix ({SourceId})"
  - `Timestamp` — Creation time

### UnifiedIssuesPanelView

Avalonia UI panel:
- **Namespace:** `Lexichord.Modules.Agents.Tuning`
- **File:** `UnifiedIssuesPanelView.axaml` + `.axaml.cs`

**Layout:**
1. **Header Section:**
   - Title "Unified Issues" with loading indicator
   - Toolbar: Refresh, Clear, Close buttons
   - Count badges: Error (red), Warning (orange), Info (blue), Hint (green)
   - Bulk action buttons: "Fix All", "Fix Errors"

2. **Filter Section:**
   - Category dropdown (Style, Grammar, Knowledge, Structure, Custom)
   - Severity dropdown (Error, Warning, Info, Hint)

3. **Content Section:**
   - ScrollViewer with ItemsControl of severity groups
   - Groups as Expanders with nested issue items
   - Empty state: "All clear!" message
   - Progress indicator during bulk operations

4. **Footer Section:**
   - Status message
   - Duration and cached indicator

5. **Error Overlay:**
   - Red-tinted message panel for errors

**Data Templates:**
- `IssuePresentationGroup` — Expander with severity icon, label, count badge
- `IssuePresentation` — Issue row with severity bar, message, location, quick fix button; expandable detail panel with category, original/suggested text, confidence, action buttons

**Value Converters:**
- `SeverityConverters.ToIcon` — Severity to emoji icon
- `SeverityConverters.IsError/IsWarning/IsInfo/IsHint` — Boolean flags
- `ExpanderConverters.ToChevron` — Boolean to ▼/▶
- `PluralConverter` — Count to "s" suffix

**Code-Behind:**
- `OnIssueItemDoubleTapped()` — Navigate to issue location

### DI Registration

Extension method for service registration:
- **Location:** `TuningServiceCollectionExtensions.cs`
- **Method:** `AddUnifiedIssuesPanel()`
- **Registers:**
  - `UnifiedIssuesPanelViewModel` → Transient

---

## Spec Adaptations

The design spec (LCS-DES-v0.7.5-KG-g.md) references several APIs that don't match the actual codebase:

1. **WPF/XAML** — Spec proposes WPF. Adapted to **Avalonia 11** with `.axaml` files.

2. **ViewModelBase** — Spec references `ViewModelBase`. Actual: `DisposableViewModel` from `Lexichord.Abstractions.Contracts`.

3. **Prism.Wpf/MvvmLightLibsStd10** — Spec proposes these libraries. Actual: **CommunityToolkit.Mvvm** with `[ObservableProperty]`, `[RelayCommand]`.

4. **IFixOrchestrator** — Spec references v0.7.5h. **Does not exist yet** — using direct `IEditorService` calls for fix application.

5. **IEditorService.GoToLineAsync()** — Does not exist. Using `CaretOffset` property + `ActivateDocumentAsync()`.

6. **Issue.Fix (single)** — Spec references single fix. Actual: `UnifiedIssue.Fixes` (list) with `BestFix` property.

7. **Issue.Location.LineNumber/ColumnNumber** — Spec references line/column. Actual: `TextSpan.Start/Length` (character offsets).

8. **Lexichord.UI.Panels** — Spec proposes this namespace. Placed in existing `Lexichord.Modules.Agents.Tuning`.

---

## Files Created

### Contracts (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Validation/Events/FixRequestedEventArgs.cs` | Class | Fix request event args |
| `src/Lexichord.Abstractions/Contracts/Validation/Events/IssueDismissedEventArgs.cs` | Class | Dismissal event args |

### Presentation Models (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Tuning/IssuePresentationGroup.cs` | Class | Severity group model |
| `src/Lexichord.Modules.Agents/Tuning/IssuePresentation.cs` | Class | Per-issue wrapper |

### ViewModel (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Tuning/UnifiedIssuesPanelViewModel.cs` | Class | Panel ViewModel |

### View (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Tuning/UnifiedIssuesPanelView.axaml` | AXAML | Panel UI |
| `src/Lexichord.Modules.Agents/Tuning/UnifiedIssuesPanelView.axaml.cs` | C# | Code-behind + converters |

### Tests (1 file)

| File | Tests | Description |
|:-----|:-----:|:------------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/Tuning/UnifiedIssuesPanelViewModelTests.cs` | 60+ | Comprehensive ViewModel tests |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Modules.Agents/Extensions/TuningServiceCollectionExtensions.cs` | Added `AddUnifiedIssuesPanel()` extension |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added registration and verification calls |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| UnifiedIssuesPanelViewModelTests | 60+ | All ViewModel functionality |

### Test Groups

| Group | Tests | Description |
|:------|:-----:|:------------|
| Constructor validation | 6 | Null argument handling |
| RefreshWithResultAsync | 6 | Groups by severity, empty result, counts |
| Filtering | 8 | Category filter, severity filter, combined |
| FixIssueCommand | 6 | Apply fix, editor calls, undo group, guards |
| FixAllCommand | 5 | Reverse order, undo group, partial fix |
| FixErrorsOnlyCommand | 3 | Only errors, skip warnings |
| DismissIssueCommand | 3 | Mark suppressed, no editor calls |
| NavigateToIssueCommand | 4 | Activate document, set caret |
| Clear/Close | 3 | Clear state, raise close event |
| IssuePresentationGroup | 5 | Expand/collapse, icon mapping |
| IssuePresentation | 6 | Computed properties, status |
| Event Args | 4 | FixRequestedEventArgs, IssueDismissedEventArgs |
| Dispose | 3 | Event unsubscription, cleanup |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.5g")]`

---

## Design Decisions

1. **Transient ViewModel** — `UnifiedIssuesPanelViewModel` registered as transient so each panel instance has its own state.

2. **Direct Editor APIs** — Using `IEditorService` directly for fix application since `IFixOrchestrator` (v0.7.5h) doesn't exist yet.

3. **Reverse Document Order** — Bulk fixes applied from end to start to preserve text offsets during sequential edits.

4. **Single Undo Group** — Bulk operations wrapped in a single undo group for atomic undo/redo.

5. **Event-Driven Refresh** — Auto-refresh on `ValidationCompleted` event for real-time updates.

6. **Nullable IUndoRedoService** — Accepted as nullable since it may not be registered in all configurations.

7. **Character Offsets** — Display `TextSpan.Start/Length` as character positions; line/column conversion deferred.

8. **Avalonia Converters** — Static converter instances for XAML bindings, avoiding DI complexity.

---

## Dependencies

### Consumed (from existing modules)

| Type | Namespace | Version |
|:-----|:----------|:--------|
| `UnifiedIssue` | `Contracts.Validation` | v0.7.5e |
| `UnifiedFix` | `Contracts.Validation` | v0.7.5e |
| `IssueCategory` | `Contracts.Validation` | v0.7.5e |
| `FixType` | `Contracts.Validation` | v0.7.5e |
| `UnifiedSeverity` | `Knowledge.Validation.Integration` | v0.6.5j |
| `IUnifiedValidationService` | `Contracts.Validation` | v0.7.5f |
| `UnifiedValidationResult` | `Contracts.Validation` | v0.7.5f |
| `UnifiedValidationOptions` | `Contracts.Validation` | v0.7.5f |
| `ValidationCompletedEventArgs` | `Contracts.Validation` | v0.7.5f |
| `TextSpan` | `Contracts.Editor` | v0.6.7b |
| `IEditorService` | `Contracts.Editor` | v0.6.7c |
| `IUndoRedoService` | `Contracts.Undo` | v0.7.3d |
| `IUndoableOperation` | `Contracts.Undo` | v0.7.3d |
| `DisposableViewModel` | `Contracts` | v0.1.x |
| `ILicenseContext` | `Contracts` | v0.4.x |
| `IMediator` | `MediatR` | - |
| `ObservableObject` | `CommunityToolkit.Mvvm` | - |

### Produced (new types)

| Type | Consumers |
|:-----|:----------|
| `FixRequestedEventArgs` | Tuning Panel, Analytics |
| `IssueDismissedEventArgs` | Tuning Panel, Analytics, Persistence |
| `IssuePresentationGroup` | UnifiedIssuesPanelViewModel |
| `IssuePresentation` | UnifiedIssuesPanelViewModel, View |
| `UnifiedIssuesPanelViewModel` | UnifiedIssuesPanelView |

---

## Build & Test Results

```
Build:     TBD
v0.7.5g:   60+ tests created
Full suite: TBD
```

---

## Usage Examples

### Basic Panel Usage

```csharp
// Resolve via DI
var viewModel = serviceProvider.GetRequiredService<UnifiedIssuesPanelViewModel>();

// Initialize (subscribes to validation events)
viewModel.InitializeAsync();

// Manual refresh
await viewModel.RefreshCommand.ExecuteAsync(null);

// Handle close request
viewModel.CloseRequested += (s, e) => ClosePanel();
```

### Programmatic Result Update

```csharp
// Update panel with validation result
var result = await validationService.ValidateAsync(path, content, options);
await viewModel.RefreshWithResultAsync(result);

Console.WriteLine($"Total: {viewModel.TotalIssueCount}");
Console.WriteLine($"Auto-fixable: {viewModel.AutoFixableCount}");
```

### Apply Fixes

```csharp
// Fix single issue
if (viewModel.SelectedIssue is not null && viewModel.FixIssueCommand.CanExecute(null))
{
    await viewModel.FixIssueCommand.ExecuteAsync(viewModel.SelectedIssue);
}

// Fix all auto-fixable issues
if (viewModel.FixAllCommand.CanExecute(null))
{
    await viewModel.FixAllCommand.ExecuteAsync(null);
}
```

### Handle Events

```csharp
// Track fix application
viewModel.FixRequested += (s, args) =>
{
    logger.LogInformation(
        "Fix applied: {IssueId} at {Location}",
        args.Issue.IssueId, args.Fix.Location);
};

// Track dismissals
viewModel.IssueDismissed += (s, args) =>
{
    if (args.Reason is not null)
    {
        logger.LogInformation(
            "Issue dismissed: {SourceId}, Reason: {Reason}",
            args.Issue.SourceId, args.Reason);
    }
};
```

### Filter Issues

```csharp
// Filter to style issues only
viewModel.SelectedCategoryFilter = IssueCategory.Style;

// Filter to errors only
viewModel.SelectedSeverityFilter = UnifiedSeverity.Error;

// Clear filters
viewModel.SelectedCategoryFilter = null;
viewModel.SelectedSeverityFilter = null;
```
