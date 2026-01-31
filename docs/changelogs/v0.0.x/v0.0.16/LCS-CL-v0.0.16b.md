# LCS-CL-016b: Live Theme Preview

**Version:** v0.1.6b  
**Category:** UI/Theme  
**Status:** Implemented  
**Date:** 2026-01-29

## Overview

Implements real-time theme switching with live preview, enabling users to instantly see theme changes before persisting preferences. Integrates with the Settings framework (v0.1.6a) as the first module-contributed settings page.

## Design Reference

- **Specification:** LCS-DES-016b (Live Theme Preview)

## Changes

### Abstractions (Lexichord.Abstractions)

#### New Contracts

- **`ThemeVariant`**: Enum representing effective display theme
    - `Light`: Light color scheme
    - `Dark`: Dark color scheme

- **`ThemeChangedEventArgs`**: Event args for theme changes
    - `OldTheme`: Previous ThemeMode
    - `NewTheme`: New ThemeMode
    - `EffectiveTheme`: Resolved ThemeVariant being displayed

#### Updated Contracts

- **`IThemeManager`**: Enhanced with async API and effective theme tracking
    - `EffectiveTheme`: Property for resolved ThemeVariant
    - `SetThemeAsync()`: Async theme application
    - `GetSystemTheme()`: Get OS theme preference
    - `ThemeChanged`: Updated to use `ThemeChangedEventArgs`

#### New Events

- **`ThemeChangedEvent`**: MediatR notification for cross-module theme broadcasts
    - Carries `NewTheme`, `OldTheme`, and `EffectiveTheme`

### Host Implementation (Lexichord.Host)

#### Updated Services

- **`ThemeManager`**: Complete rewrite for v0.1.6b
    - Async `SetThemeAsync()` with MediatR event publishing
    - `EffectiveTheme` property computing resolved theme
    - `GetSystemTheme()` detecting OS preference via `PlatformSettings`
    - Platform theme subscription for System mode

#### New Settings

- **`AppearanceSettings`**: Record for persisting appearance preferences
    - `Theme`: ThemeMode preference
    - `AccentColor`: Future use
    - `UiScale`: Future use

- **`AppearanceSettingsPage`**: First `ISettingsPage` implementation
    - CategoryId: "appearance"
    - DisplayName: "Appearance"
    - Creates AppearanceSettingsView

- **`AppearanceSettingsViewModel`**: ViewModel for appearance settings
    - `IsLightSelected`, `IsDarkSelected`, `IsSystemSelected` bindings
    - `SelectLightThemeCommand`, `SelectDarkThemeCommand`, `SelectSystemThemeCommand`
    - Real-time theme preview via `IThemeManager`

- **`AppearanceSettingsView.axaml`**: Theme selection UI
    - RadioButton group for Light/Dark/System selection
    - Command bindings for instant preview

### Updated Consumers

Components updated to use new `IThemeManager` interface:

- `StatusBar.axaml.cs`: Updated theme toggle button
- `App.axaml.cs`: Async theme restoration at startup
- `MainWindow.axaml.cs`: Async theme loading
- `XshdHighlightingService.cs`: Editor syntax theme sync

### DI Registration

Services registered in `HostServices.cs`:

- `AppearanceSettingsPage` → `ISettingsPage` (Keyed singleton)
- `AppearanceSettingsViewModel` (Transient)

## Test Coverage

### ThemeManagerTests (14 tests)

- SetThemeAsync for Light/Dark/System modes
- Event raising only on theme change
- MediatR ThemeChangedEvent publishing
- EffectiveTheme resolution
- GetSystemTheme fallback behavior

### AppearanceSettingsViewModelTests (17 tests)

- Constructor initialization from current theme
- Selection property bindings
- Theme command execution
- Duplicate selection handling
- Error recovery on SetThemeAsync failure

### XshdHighlightingServiceTests (35 tests)

- Updated to use new IThemeManager interface
- ThemeChangedEventArgs integration

## Implementation Notes

### Architecture Decisions

1. **Async SetThemeAsync**: Enables async MediatR publishing
2. **ThemeVariant vs ThemeMode**: Separation of user preference (ThemeMode) from display (ThemeVariant)
3. **Command-based selection**: RadioButton commands for instant preview

### OS Theme Detection

Platform theme detected via:

```csharp
public ThemeVariant GetSystemTheme()
{
    var platformSettings = _application.PlatformSettings;
    var platformTheme = platformSettings?.GetColorValues().ThemeVariant;
    return platformTheme == Avalonia.Platform.PlatformThemeVariant.Dark
        ? ThemeVariant.Dark
        : ThemeVariant.Light;
}
```

### Type Alias Pattern

Test files use alias to avoid Avalonia/Lexichord `ThemeVariant` conflict:

```csharp
using LexThemeVariant = Lexichord.Abstractions.Contracts.ThemeVariant;
```

## Verification

1. **Build**: `dotnet build` passes with 0 errors
2. **Tests**: 66 tests pass (14 + 17 + 35)
3. **Manual**: Theme switches apply instantly in Settings

## Related

- **Requires:** v0.1.6a (Settings Dialog Framework)
- **Follows:** v0.1.5d (Keybinding Service)
- **Enables:** Future appearance customization (accent colors, UI scale)
