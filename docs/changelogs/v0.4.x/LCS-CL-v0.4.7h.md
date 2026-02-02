# LCS-CL-v0.4.7h - Relationship Viewer

**Release Date:** 2026-02-02  
**Scope:** Knowledge Graph Browser - Entity Browser  
**Parent Spec:** [LCS-SBD-v0.4.7-KG](../specs/v0.4.x/v0.4.7/LCS-SBD-v0.4.7-KG.md)

---

## Overview

This changelog documents the implementation of the Relationship Viewer (v0.4.7h), which provides hierarchical display and filtering of entity relationships within the Knowledge Graph Browser.

---

## Changes

### Added

#### Abstractions

| File                       | Description                                                          |
| -------------------------- | -------------------------------------------------------------------- |
| `RelationshipDirection.cs` | Enum for relationship direction filtering (Both, Incoming, Outgoing) |

#### ViewModels

| File                                  | Description                                                                                             |
| ------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| `RelationshipTreeNodeViewModel.cs`    | Tree node ViewModel for hierarchical relationship display with factory methods for group and leaf nodes |
| `RelationshipViewerPanelViewModel.cs` | Panel ViewModel with relationship loading, filtering, and tree building logic                           |

#### Views

| File                               | Description                                                         |
| ---------------------------------- | ------------------------------------------------------------------- |
| `RelationshipViewerPanel.axaml`    | AXAML view with TreeView, direction/type filters, and loading state |
| `RelationshipViewerPanel.axaml.cs` | Standard Avalonia code-behind                                       |

#### Tests

| File                                       | Description                                                   |
| ------------------------------------------ | ------------------------------------------------------------- |
| `RelationshipTreeNodeViewModelTests.cs`    | Unit tests for tree node factory methods and property changes |
| `RelationshipViewerPanelViewModelTests.cs` | Unit tests for loading, filtering, and commands               |

### Modified

| File                            | Change Description                                                  |
| ------------------------------- | ------------------------------------------------------------------- |
| `EntityDetailView.axaml`        | Integrated `RelationshipViewerPanel` into Relationships section     |
| `EntityDetailViewModel.cs`      | Added `RelationshipViewerPanelViewModel` property with DI injection |
| `EntityDetailViewModelTests.cs` | Updated constructor tests for new parameter                         |
| `KnowledgeModule.cs`            | Registered `RelationshipViewerPanelViewModel` as transient          |

---

## Summary of Features

1. **Hierarchical Tree View:** Relationships grouped by direction (Incoming/Outgoing) and type
2. **Direction Filtering:** ComboBox to filter by Incoming, Outgoing, or Both
3. **Type Filtering:** ComboBox to filter by specific relationship type
4. **Count Display:** Status bar showing filtered/total relationship counts
5. **Clear Filters Command:** Button to reset all filters to defaults
6. **Loading State:** Overlay with progress indicator during async loading
7. **Empty State:** User-friendly message when no relationships exist

---

## Test Coverage

- **RelationshipTreeNodeViewModelTests:** 15 tests covering factory methods and property changes
- **RelationshipViewerPanelViewModelTests:** 15 tests covering loading, filtering, and commands
- **EntityDetailViewModelTests:** Updated 8 constructor tests with new parameter

---

## Dependencies

| Dependency              | Version | Purpose                         |
| ----------------------- | ------- | ------------------------------- |
| `IGraphRepository`      | v0.4.7e | Relationship and entity queries |
| `CommunityToolkit.Mvvm` | 8.3.2   | MVVM infrastructure             |
| `Avalonia.Controls`     | 11.x    | TreeView and filtering UI       |

---

## Related Documents

- [LCS-DES-v0.4.7-KG-h.md](../specs/v0.4.x/v0.4.7/LCS-DES-v0.4.7-KG-h.md) - Design Specification
- [LCS-CL-v0.4.7g.md](LCS-CL-v0.4.7g.md) - Previous changelog (Entity CRUD)
