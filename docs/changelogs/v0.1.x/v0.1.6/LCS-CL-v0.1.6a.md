# LCS-CL-016a: Settings Dialog Framework

**Version:** v0.1.6a  
**Category:** UI Framework  
**Status:** Implemented  
**Date:** 2026-01-29

## Overview

Implements the centralized Settings dialog framework, enabling modules to contribute settings pages that are organized hierarchically in a unified settings experience.

## Design Reference

- **Specification:** LCS-DES-016 (Settings Framework)

## Changes

### Abstractions (Lexichord.Abstractions)

#### New Contracts

- **`ISettingsPage`**: Interface for module-contributed settings pages
    - `CategoryId`: Unique identifier (uses dot notation for hierarchy)
    - `DisplayName`: User-facing name in navigation tree
    - `ParentCategoryId`: Optional parent for hierarchical organization
    - `Icon`: Optional icon name
    - `SortOrder`: Ordering within parent category
    - `RequiredTier`: License tier filtering (Core, WriterPro)
    - `SearchKeywords`: Additional search terms
    - `CreateView()`: Factory method returning Avalonia Control

- **`ISettingsPageRegistry`**: Central registry for settings pages
    - Thread-safe registration/unregistration
    - Tier-based page filtering
    - Hierarchical page retrieval
    - Full-text search across DisplayName, CategoryId, and keywords
    - Events: `PageRegistered`, `PageUnregistered`

- **`SettingsWindowOptions`**: Record for deep-linking
    - `InitialCategoryId`: Navigate directly to a category
    - `SearchQuery`: Pre-populate search box

- **`SettingsCategoryNode`**: Navigation tree node structure
    - Wraps `ISettingsPage` with hierarchical children
    - UI state properties (HasChildren, IsExpanded)

#### New Events

- **`SettingsClosedEvent`**: MediatR notification published when Settings window closes

### Host Implementation (Lexichord.Host)

#### New Services

- **`SettingsPageRegistry`**: Singleton implementation of `ISettingsPageRegistry`
    - Thread-safe with lock-based synchronization
    - Validation for CategoryId uniqueness (case-insensitive)
    - Automatic sorting by SortOrder, then DisplayName
    - Case-insensitive search across all searchable fields

#### New ViewModels

- **`SettingsViewModel`**: Manages Settings window state
    - Builds hierarchical category tree from registry
    - Handles search filtering
    - Graceful error handling for `CreateView()` failures
    - MediatR integration for close event publishing

#### New Views

- **`SettingsWindow.axaml`**: Modal settings dialog
    - Split-panel layout (220px navigation, flexible content)
    - TreeView with hierarchical category navigation
    - Search box with watermark
    - ScrollViewer for content area
    - Escape key binding for close

### Keyboard Shortcuts

| Shortcut | Action                |
| -------- | --------------------- |
| `Ctrl+,` | Open Settings window  |
| `Escape` | Close Settings window |

### DI Registration

Services registered in `HostServices.cs`:

- `ISettingsPageRegistry` → `SettingsPageRegistry` (Singleton)
- `SettingsViewModel` (Transient)

## Test Coverage

### SettingsPageRegistryTests (27 tests)

- Registration with validation and duplicate detection
- Unregistration with event publishing
- Page retrieval by ID, tier, and parent
- Search functionality across all fields
- Case-insensitive operations

### SettingsViewModelTests (12 tests)

- Initialization with tree building
- Navigation and deep-linking
- Page loading with error handling
- Hierarchy construction
- Close event publishing

## Implementation Notes

### Architecture Decisions

1. **Object return for CreateView()**: Returns `object` instead of `Control` to avoid Avalonia dependency in Abstractions
2. **Transient ViewModel**: New instance per Settings window for clean state
3. **Case-insensitive IDs**: All CategoryId operations are case-insensitive for robustness

### Error Handling

- Failed `CreateView()` displays error message instead of crashing
- Invalid navigation targets are logged and ignored
- Registry validates all inputs with meaningful exceptions

### Module Integration

Modules implement `ISettingsPage` and register via:

```csharp
public async Task InitializeAsync(IServiceProvider services)
{
    var registry = services.GetRequiredService<ISettingsPageRegistry>();
    registry.RegisterPage(new MySettingsPage());
}
```

## Verification

1. **Build**: `dotnet build` passes with 0 errors
2. **Tests**: 44 new tests pass (27 registry + 12 ViewModel)
3. **Manual**: `Ctrl+,` opens Settings dialog

## Related

- **Follows:** v0.1.5d (Keybinding Service)
- **Enables:** Module-contributed settings (Workspace, Editor, etc.)
