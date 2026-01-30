# LCS-CL-024b: The Squiggly Line (Theme-Aware Wavy Underlines)

**Version**: v0.2.4b
**Status**: ✅ Complete
**Date**: 2026-01-30

## Summary

Enhanced the wavy underline rendering system with theme-aware colors. Underlines now automatically adapt to light and dark themes, with distinct color palettes optimized for visibility against each background. The renderer now includes pen caching for improved performance and severity-based z-ordering to ensure errors always draw on top.

## What's New

### Abstractions (`Lexichord.Abstractions`)

#### Enhanced `UnderlineColor` Record

- Added **light theme colors**: `LightError`, `LightWarning`, `LightInfo`, `LightHint`
- Added **dark theme colors**: `DarkError`, `DarkWarning`, `DarkInfo`, `DarkHint`
- Added **semi-transparent backgrounds**: `LightErrorBackground`, `DarkErrorBackground`, etc.
- Maintained backward compatibility with legacy aliases (`ErrorRed`, `WarningYellow`, etc.)

#### Enhanced `IViolationColorProvider` Interface

- Added `SetTheme(ThemeVariant theme)` for theme switching
- Added `GetBackgroundColor(ViolationSeverity)` for optional violation highlights
- Added `GetTooltipBorderColor(ViolationSeverity)` for tooltip styling
- Added `GetSeverityIcon(ViolationSeverity)` returning Material Design SVG paths

### Style Module (`Lexichord.Modules.Style`)

#### Enhanced `ViolationColorProvider`

- Constructor now accepts optional `IThemeManager` for initial theme detection
- Implements all new interface methods
- Light theme colors: optimized for high contrast on white backgrounds
- Dark theme colors: softer tones that remain visible without being harsh
- Material Design icons for Error (X), Warning (triangle), Info (checkmark), Hint (i)

### Editor Module (`Lexichord.Modules.Editor`)

#### Enhanced `WavyUnderlineBackgroundRenderer`

- Changed render layer from `KnownLayer.Text` to `KnownLayer.Background`
- Updated wave parameters: 2.0px amplitude (was 1.5px), 1.5px thickness (was 1.0px)
- Added **pen caching** by color for improved performance
- Added **severity-based z-ordering**: errors draw on top of warnings, warnings on top of info
- Improved wave smoothness with proper quadratic bezier control points

## Color Palettes

### Light Theme (High Contrast on White)

| Severity | Underline | Background (12% opacity) |
| -------- | --------- | ------------------------ |
| Error    | #E51400   | #20E51400                |
| Warning  | #F0A30A   | #20F0A30A                |
| Info     | #0078D4   | #200078D4                |
| Hint     | #808080   | (no background)          |

### Dark Theme (Softer on Dark Backgrounds)

| Severity | Underline | Background (19% opacity) |
| -------- | --------- | ------------------------ |
| Error    | #FF6B6B   | #30FF6B6B                |
| Warning  | #FFB347   | #30FFB347                |
| Info     | #4FC3F7   | #304FC3F7                |
| Hint     | #B0B0B0   | (no background)          |

## Testing

Added comprehensive test suites:

### `ViolationColorProviderTests` (28 tests)

- Light/dark theme color selection
- Theme switching behavior
- Background color transparency verification
- Tooltip border color consistency
- Severity icon path validation
- RGB value verification for all severity levels

### `WavyUnderlineBackgroundRendererTests` (15 tests)

- Layer property validation
- Segment management (add, clear, set)
- Thread safety for concurrent operations
- Color constant verification
- Legacy alias backward compatibility

## Files Changed

| File                                      | Change Type |
| ----------------------------------------- | ----------- |
| `IViolationColorProvider.cs`              | Modified    |
| `UnderlineSegment.cs`                     | Modified    |
| `ViolationColorProvider.cs`               | Modified    |
| `WavyUnderlineBackgroundRenderer.cs`      | Modified    |
| `ViolationColorProviderTests.cs`          | Modified    |
| `WavyUnderlineBackgroundRendererTests.cs` | Added       |

## Dependencies

- Depends on: `IThemeManager` from v0.1.6b
- Used by: `StyleViolationRenderer` (v0.2.4a)

## Verification

```bash
# Build
dotnet build

# Run related tests
dotnet test --filter "FullyQualifiedName~ViolationColorProvider|FullyQualifiedName~WavyUnderlineBackgroundRenderer"
```

## Notes

- The flaky `DebounceControllerTests` failures are pre-existing and unrelated to this change
- Color values match industry conventions (VS Code, Visual Studio, JetBrains IDEs)
- Semi-transparent backgrounds use different opacities: 12% for light theme, 19% for dark theme
