# LCS-CL-024c: Hover Tooltips for Style Violations

**Version**: v0.2.4c
**Status**: ✅ Complete
**Date**: 2026-01-30

## Summary

Added hover tooltips that display detailed information when users hover over underlined style violations. Tooltips show the rule name, violation message, and optional recommendation. Supports multi-violation navigation when multiple violations overlap at the same position, and can be triggered via keyboard shortcuts (Ctrl+K, Ctrl+I).

## What's New

### Abstractions (`Lexichord.Abstractions`)

#### `NavigateDirection` Enum

Simple enum for multi-violation navigation:

- `Previous` — Navigate to previous violation
- `Next` — Navigate to next violation

#### `NavigateViolationEventArgs` Class

Event arguments for navigation requests between multiple violations at the same position.

#### Enhanced `IViolationProvider` Interface

- Added `GetViolationsAtOffset(int offset)` — Returns all violations whose range contains the given offset, enabling multi-violation tooltip scenarios

### Style Module (`Lexichord.Modules.Style`)

#### Enhanced `ViolationProvider`

Implemented `GetViolationsAtOffset` with:

- Thread-safe snapshot access under lock
- LINQ filtering using `ContainsOffset` predicate
- Debug logging for query diagnostics

### Editor Module (`Lexichord.Modules.Editor`)

#### `ViolationTooltipViewModel`

CommunityToolkit.MVVM-based ViewModel with:

- Observable properties: `RuleName`, `Message`, `Recommendation`, `Explanation`
- Visual properties: `BorderColor`, `IconPath`
- Navigation state: `CurrentIndex`, `TotalCount`
- Computed properties: `HasRecommendation`, `HasExplanation`, `HasMultiple`
- Relay commands: `NavigatePreviousCommand`, `NavigateNextCommand`

#### `ViolationTooltipView.axaml`

Styled XAML view featuring:

- Card-style container with severity-colored border
- Severity icon and rule name header
- Message text with proper wrapping
- Optional recommendation section with distinct background
- Navigation controls (Previous/Next) for multi-violation scenarios

#### `ViolationTooltipService`

Core orchestration service providing:

- **Hover detection** via `PointerMoved` on TextView
- **Hover delay** of 500ms (configurable) before showing tooltip
- **Offset calculation** from mouse position to document offset
- **Tooltip positioning** below the hovered text
- **Keyboard shortcuts**: Ctrl+K, Ctrl+I to show at caret (VS Code convention)
- **Multi-violation navigation** with wrap-around support
- **Escape to hide** tooltip

#### Enhanced `ManuscriptView`

- Added `InitializeTooltipService()` method for service setup
- Added `CleanupTooltipService()` method for disposal

## Testing

### `ViolationTooltipServiceTests` (8 tests)

- Initial state verification (`IsTooltipVisible` false)
- Hover delay default value (500ms)
- Hover delay configurability
- Constructor null validation (all 3 parameters)
- Safe disposal patterns

### `ViolationTooltipViewModelTests` (15 tests)

- `HasRecommendation` computed property (null, empty, set)
- `HasExplanation` computed property (null, empty, set)
- `HasMultiple` computed property (0, 1, >1)
- `NavigatePreviousCommand` raises correct event
- `NavigateNextCommand` raises correct event
- `Update` method sets all properties
- Property change notifications

### `GetViolationsAtOffset` Tests in `ViolationProviderTests` (7 tests)

- No violations returns empty list
- Single match returns single violation
- Multiple overlapping returns all matches
- No match returns empty list
- Boundary: start offset included
- Boundary: end offset excluded
- Disposed state throws `ObjectDisposedException`

## Files Changed

| File                                | Change Type |
| ----------------------------------- | ----------- |
| `NavigateDirection.cs`              | Added       |
| `NavigateViolationEventArgs.cs`     | Added       |
| `IViolationProvider.cs`             | Modified    |
| `ViolationProvider.cs`              | Modified    |
| `ViolationTooltipViewModel.cs`      | Added       |
| `ViolationTooltipView.axaml`        | Added       |
| `ViolationTooltipView.axaml.cs`     | Added       |
| `ViolationTooltipService.cs`        | Added       |
| `ManuscriptView.axaml.cs`           | Modified    |
| `ViolationTooltipServiceTests.cs`   | Added       |
| `ViolationTooltipViewModelTests.cs` | Added       |
| `ViolationProviderTests.cs`         | Modified    |

## Dependencies

- Depends on: `IViolationProvider` (v0.2.4a), `IViolationColorProvider` (v0.2.4b)
- Uses: `CommunityToolkit.Mvvm`, `AvaloniaEdit`

## Verification

```bash
# Build
dotnet build

# Run related tests
dotnet test --filter "FullyQualifiedName~ViolationTooltip|FullyQualifiedName~GetViolationsAtOffset"
# Total: 28 tests, Passed: 28
```

## Notes

- Tooltip service must be initialized by calling `ManuscriptView.InitializeTooltipService()` with the violation and color providers
- Keyboard shortcut follows VS Code convention (Ctrl+K, Ctrl+I)
- Multi-violation navigation wraps around (going "previous" from first goes to last)
