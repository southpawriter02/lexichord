# LCS-CL-024d: Context Menu Quick-Fixes

**Version**: v0.2.4d
**Status**: ✅ Complete
**Date**: 2026-01-30

## Summary

Added context menu quick-fixes that allow users to apply suggested text replacements for style violations. Users can right-click on underlined violations or press Ctrl+. to see available fixes. Selecting a fix applies the replacement with full undo support.

## What's New

### Abstractions (`Lexichord.Abstractions`)

#### `QuickFixAction` Record

Represents a quick-fix action with:

- `ViolationId`: Links to the source violation
- `Title`: User-friendly display text (e.g., "Replace 'utilize' with 'use'")
- `ReplacementText`: Suggested replacement
- `StartOffset`, `Length`: Document range to replace

#### `IQuickFixService` Interface

Service for providing and applying quick-fixes:

- `GetQuickFixesAtOffset(int offset)`: Returns available fixes at caret position
- `GetQuickFixForViolation(AggregatedStyleViolation)`: Creates fix for specific violation
- `ApplyQuickFix(QuickFixAction, Action<int,int,string>)`: Applies fix via callback
- `QuickFixApplied` event: Notifies when fix is applied (for re-linting)

### Editor Module (`Lexichord.Modules.Editor`)

#### `QuickFixService` Class

Implementation that bridges linting and editor:

- Queries `IViolationProvider` for violations with `Suggestion` property
- Generates user-friendly titles for context menu display
- Uses callback pattern for undo-safe document edits
- Raises events for re-linting integration

#### `ContextMenuIntegration` Class

Handles UI integration:

- Attaches to TextArea for context menu events
- Builds Avalonia ContextMenu from available fixes
- Registers Ctrl+. keyboard shortcut
- Positions menu at caret location for keyboard triggers

#### `ManuscriptView` Integration

Added public methods following tooltip service pattern:

- `InitializeQuickFixService()`: Sets up quick-fix services
- `CleanupQuickFixService()`: Disposes services on document close

## Technical Notes

### Undo Support

Fixes use `TextEditor.Document.Replace()` which automatically creates undo checkpoints. Users can undo applied fixes with Ctrl+Z.

### Multiple Violations

When multiple violations overlap at the same position, all available fixes appear as separate menu items.

### Re-linting Trigger

The `onQuickFixApplied` callback parameter allows the host to trigger re-linting after a fix is applied, ensuring the violation list updates immediately.

## Files Changed

### New Files

| File                                          | Description                    |
| --------------------------------------------- | ------------------------------ |
| `Contracts/Linting/QuickFixAction.cs`         | Quick-fix action record        |
| `Contracts/Linting/IQuickFixService.cs`       | Service interface + event args |
| `Services/QuickFix/QuickFixService.cs`        | Service implementation         |
| `Services/QuickFix/ContextMenuIntegration.cs` | UI integration                 |

### Modified Files

| File                            | Description                    |
| ------------------------------- | ------------------------------ |
| `Views/ManuscriptView.axaml.cs` | Added quick-fix initialization |

## Test Coverage

| Test Class                    | Tests |
| ----------------------------- | ----- |
| `QuickFixServiceTests`        | 15    |
| `ContextMenuIntegrationTests` | 6     |

All tests verify constructor validation, query behavior, fix application, event raising, and disposal patterns.

## Dependencies

- Depends on: v0.2.4a (`AggregatedStyleViolation.Suggestion`)
- Depends on: v0.2.3d (`IViolationProvider.GetViolationsAtOffset`)
