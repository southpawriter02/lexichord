# LCS-CL-025a: Terminology Grid View

**Version**: v0.2.5a
**Status**: ✅ Complete
**Date**: 2026-01-30

## Summary

Added the Lexicon grid view ("The Librarian") for managing style terminology. Users can view, sort, and filter style terms in a DataGrid, with editing operations gated by license tier. The grid is accessible from the Right dock region.

## What's New

### Style Module (`Lexichord.Modules.Style`)

#### `StyleTermRowViewModel` Class

ViewModel wrapper for `StyleTerm` entity with computed display properties:

- Severity badge flags (`IsError`, `IsWarning`, `IsSuggestion`, `IsInfo`)
- `SeverityColor` brush for colored badges
- `SeveritySortOrder` for custom sorting (Error=0, Warning=1, Suggestion=2, Info=3)
- `ActiveIcon` and `ActiveColor` for status display

#### `LexiconViewModel` Class

Main grid ViewModel managing:

- Term loading via `ITerminologyService.GetAllAsync()`
- Default sorting by Severity (desc) → Category (asc)
- License-gated commands (Edit, Delete, ToggleActive require WriterPro+)
- Copy Pattern command (available to all tiers)
- Status bar text generation with term count and tier display
- `LexiconChangedEvent` handler for automatic refresh

#### `LexiconView.axaml`

Avalonia DataGrid with:

- Toolbar with Add, Edit, Delete, Toggle actions
- Status bar showing term count and license tier
- Context menu for right-click operations
- Double-click to edit support

### Module Registration

- `LexiconView` registered in Right dock region via `IRegionManager.RegisterToolAsync`
- ViewModel and View registered as Transient services
- Module version updated to 0.2.5

## Technical Notes

### Avalonia 11 Clipboard

Uses `CopyRequested` event pattern for clipboard access:

```csharp
// ViewModel raises event
CopyRequested?.Invoke(this, SelectedTerm.Term);

// View handles via TopLevel
var topLevel = TopLevel.GetTopLevel(this);
await topLevel?.Clipboard.SetTextAsync(text);
```

### License Gating

Commands check `ILicenseContext.GetCurrentTier() >= LicenseTier.WriterPro` before enabling.

## Files Changed

### New Files

| File                                  | Description          |
| ------------------------------------- | -------------------- |
| `ViewModels/StyleTermRowViewModel.cs` | DataGrid row wrapper |
| `ViewModels/LexiconViewModel.cs`      | Grid ViewModel       |
| `Views/LexiconView.axaml`             | DataGrid UI          |
| `Views/LexiconView.axaml.cs`          | Code-behind          |

### Modified Files

| File                             | Description                             |
| -------------------------------- | --------------------------------------- |
| `StyleModule.cs`                 | Added DI and dock registration          |
| `Lexichord.Modules.Style.csproj` | Added Avalonia, DataGrid, MVVM packages |

## Test Coverage

| Test Class              | Tests |
| ----------------------- | ----- |
| `LexiconViewModelTests` | 21    |

Tests cover loading, sorting, selection, CanExecute logic, license gating, CopyRequested event, and disposal.

## Dependencies

- Depends on: v0.2.2d (`ITerminologyService`, `LexiconChangedEvent`)
- Depends on: v0.0.4c (`ILicenseContext`, `LicenseTier`)
- Depends on: v0.1.1b (`IRegionManager`)
