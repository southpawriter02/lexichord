# LCS-CL-055b: Filter UI Component

**Version:** v0.5.5b
**Date:** 2026-02
**Status:** ✅ Complete

## Summary

Implemented the Filter UI Component for the Filter System feature, providing an interactive panel for filtering search results by folder path, file extension, modification date, and saved presets. This sub-part builds upon the Filter Model (v0.5.5a) and provides the visual interface consumed by users.

## Changes

### Modules.RAG (`Lexichord.Modules.RAG`)

| File                           | Change                                                                    |
| ------------------------------ | ------------------------------------------------------------------------- |
| `DateRangeOption.cs`           | Enum for date range presets (AnyTime, LastDay, Last7Days, Last30Days, Custom) |
| `FilterChipType.cs`            | Enum for filter chip types (Path, Extension, DateRange, Tag)              |
| `FilterChipViewModel.cs`       | Immutable record for filter chip display (Type, DisplayText, Value)       |
| `ExtensionToggleViewModel.cs`  | ViewModel for file extension toggle buttons with selection state          |
| `FolderNodeViewModel.cs`       | ViewModel for folder tree nodes with selection propagation                |
| `SearchFilterPanelViewModel.cs`| Main ViewModel orchestrating all filter panel UI state                    |
| `SearchFilterPanel.axaml`      | Avalonia UI view with folder tree, extension toggles, date picker, presets |
| `SearchFilterPanel.axaml.cs`   | Minimal code-behind following MVVM pattern                                |
| `RAGModule.cs`                 | Updated DI registration for `SearchFilterPanelViewModel` (transient)      |

### Abstractions (`Lexichord.Abstractions`)

| File               | Change                                                         |
| ------------------ | -------------------------------------------------------------- |
| `FeatureCodes.cs`  | Added `DateRangeFilter` feature code (WriterPro tier)          |
| `FeatureCodes.cs`  | Added `SavedPresets` feature code (WriterPro tier)             |

### ViewModels

#### DateRangeOption Enum

| Value       | Description                                    | Maps To                 |
| ----------- | ---------------------------------------------- | ----------------------- |
| AnyTime     | No date filtering (default)                    | `null`                  |
| LastDay     | Modified in last 24 hours                      | `DateRange.LastDays(1)` |
| Last7Days   | Modified in last 7 days                        | `DateRange.LastDays(7)` |
| Last30Days  | Modified in last 30 days                       | `DateRange.LastDays(30)`|
| Custom      | User-specified start and end dates             | `new DateRange(...)`    |

#### FilterChipType Enum

| Value      | Description                              | Removal Behavior                  |
| ---------- | ---------------------------------------- | --------------------------------- |
| Path       | Folder path filter                       | Deselects folder in tree          |
| Extension  | File extension filter                    | Deselects extension toggle        |
| DateRange  | Temporal filter                          | Resets to AnyTime                 |
| Tag        | Reserved for future tag-based filtering  | N/A                               |

#### ExtensionToggleViewModel

| Property    | Type   | Description                                |
| ----------- | ------ | ------------------------------------------ |
| Extension   | string | File extension without dot (e.g., "md")    |
| DisplayName | string | Human-readable name (e.g., "Markdown")     |
| Icon        | string | Visual icon/symbol (e.g., "M↓")            |
| IsSelected  | bool   | Whether the extension is selected          |

#### FolderNodeViewModel

| Property            | Type                                      | Description                              |
| ------------------- | ----------------------------------------- | ---------------------------------------- |
| Name                | string                                    | Folder name for display                  |
| Path                | string                                    | Full path to folder                      |
| IsSelected          | bool                                      | Selection state (cascades to children)   |
| IsExpanded          | bool                                      | Tree node expansion state                |
| Children            | ObservableCollection&lt;FolderNodeViewModel&gt; | Child folder nodes                   |
| IsPartiallySelected | bool                                      | Computed: some but not all children selected |

| Method           | Description                                      |
| ---------------- | ------------------------------------------------ |
| GetGlobPattern() | Returns glob pattern for filtering (e.g., `/path/**`) |

#### SearchFilterPanelViewModel

| Property          | Type                                      | Description                                |
| ----------------- | ----------------------------------------- | ------------------------------------------ |
| IsExpanded        | bool                                      | Whether the filter panel is expanded       |
| FolderTree        | ObservableCollection&lt;FolderNodeViewModel&gt; | Workspace folder hierarchy             |
| ExtensionToggles  | ObservableCollection&lt;ExtensionToggleViewModel&gt; | File type toggle buttons           |
| SelectedDateRange | DateRangeOption                           | Current date range selection               |
| CustomStartDate   | DateTime?                                 | Custom range start (when Custom selected)  |
| CustomEndDate     | DateTime?                                 | Custom range end (when Custom selected)    |
| SavedPresets      | ObservableCollection&lt;FilterPreset&gt;  | Available saved presets                    |
| ActiveChips       | ObservableCollection&lt;FilterChipViewModel&gt; | Visual display of active filters       |
| CanUseDateFilter  | bool                                      | Whether date filtering is licensed         |
| CanUseSavedPresets| bool                                      | Whether saved presets are licensed         |
| HasActiveFilters  | bool                                      | Computed: whether any filters are active   |
| CurrentFilter     | SearchFilter                              | Computed: built from current UI state      |

| Command             | Description                                      |
| ------------------- | ------------------------------------------------ |
| LoadFolderTreeAsync | Loads folder tree from workspace root            |
| ClearFilters        | Clears all active filters                        |
| RemoveChip          | Removes a specific filter chip                   |
| ApplyPreset         | Applies a saved preset's filter criteria         |

### Default Extensions

The filter panel includes these pre-configured extension toggles:

| Extension | DisplayName | Icon |
| --------- | ----------- | ---- |
| md        | Markdown    | M↓   |
| txt       | Text        | T    |
| json      | JSON        | {}   |
| yaml      | YAML        | Y    |
| rst       | RST         | R    |

## Tests

| File                              | Tests                                           |
| --------------------------------- | ----------------------------------------------- |
| `ExtensionToggleViewModelTests.cs`| 3 tests - Constructor null validation           |
| `ExtensionToggleViewModelTests.cs`| 4 tests - Property initialization               |
| `ExtensionToggleViewModelTests.cs`| 2 tests - IsSelected property behavior          |
| `ExtensionToggleViewModelTests.cs`| 2 tests - Property change notifications         |
| `FolderNodeViewModelTests.cs`     | 2 tests - Constructor null validation           |
| `FolderNodeViewModelTests.cs`     | 5 tests - Property initialization               |
| `FolderNodeViewModelTests.cs`     | 4 tests - Selection propagation                 |
| `FolderNodeViewModelTests.cs`     | 5 tests - IsPartiallySelected behavior          |
| `FolderNodeViewModelTests.cs`     | 3 tests - GetGlobPattern method                 |
| `FolderNodeViewModelTests.cs`     | 3 tests - Property change notifications         |
| `SearchFilterPanelViewModelTests.cs`| 4 tests - Constructor null validation         |
| `SearchFilterPanelViewModelTests.cs`| 4 tests - Initial state                       |
| `SearchFilterPanelViewModelTests.cs`| 4 tests - License feature initialization      |
| `SearchFilterPanelViewModelTests.cs`| 5 tests - Extension toggles initialization    |
| `SearchFilterPanelViewModelTests.cs`| 3 tests - Extension toggle selection          |
| `SearchFilterPanelViewModelTests.cs`| 3 tests - Date range selection (licensed)     |
| `SearchFilterPanelViewModelTests.cs`| 4 tests - ClearFiltersCommand                 |
| `SearchFilterPanelViewModelTests.cs`| 3 tests - RemoveChipCommand                   |
| `SearchFilterPanelViewModelTests.cs`| 5 tests - CurrentFilter property              |
| `SearchFilterPanelViewModelTests.cs`| 3 tests - HasActiveFilters property           |
| `SearchFilterPanelViewModelTests.cs`| 2 tests - Property change notifications       |

**Total: ~66 unit tests**

## License Gating

| Feature Code              | Minimum Tier | Description                           |
| ------------------------- | ------------ | ------------------------------------- |
| `Feature.DateRangeFilter` | WriterPro    | Enables date range filtering          |
| `Feature.SavedPresets`    | WriterPro    | Enables saved filter presets          |

License-gated features display a lock icon with "Pro" label in the UI when unavailable.

## Dependencies

| Version  | Component            | Uses                                    |
| -------- | -------------------- | --------------------------------------- |
| v0.5.5a  | Filter Model         | `SearchFilter`, `DateRange`, `FilterPreset`, `IFilterValidator` |
| v0.1.2a  | Workspace Service    | `IWorkspaceService` for workspace root  |
| v0.0.4c  | License Context      | `ILicenseContext` for feature gating    |

## Dependents

| Version  | Component            | Uses                                    |
| -------- | -------------------- | --------------------------------------- |
| v0.5.5c  | Filter Query Builder | `SearchFilterPanelViewModel.CurrentFilter` |
| v0.5.5d  | Saved Filters        | `SearchFilterPanelViewModel` (preset selection) |

## Technical Notes

### Selection Propagation

When a parent folder is selected/deselected, the change propagates to all children recursively via the `OnIsSelectedChanged` partial method. A guard flag (`_isPropagating`) prevents infinite recursion.

### Tri-State Checkbox

`IsPartiallySelected` returns `true` when some but not all children are selected. The UI should display this as an indeterminate checkbox state.

### Filter Chip Updates

Filter chips are automatically updated when any filter state changes:
- Extension toggle selection triggers chip update via PropertyChanged subscription
- Date range changes trigger chip update via `OnSelectedDateRangeChanged`
- Folder selection changes are tracked by the parent ViewModel

### Folder Tree Building

The folder tree is built asynchronously to avoid UI blocking:
- Maximum depth of 3 levels
- Hidden folders (starting with `.`) are excluded
- Inaccessible folders are silently skipped
- First two levels are expanded by default

## Related Documents

- [LCS-DES-055b](../../specs/v0.5.x/v0.5.5/LCS-DES-v0.5.5b.md) - Design Specification
- [LCS-SBD-055](../../specs/v0.5.x/v0.5.5/LCS-SBD-v0.5.5.md) - Scope Breakdown
- [LCS-DES-055-INDEX](../../specs/v0.5.x/v0.5.5/LCS-DES-v0.5.5-INDEX.md) - Version Index
- [LCS-CL-055a](./LCS-CL-055a.md) - Filter Model Changelog
