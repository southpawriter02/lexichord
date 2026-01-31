# LCS-CL-002a: Changelog — Avalonia Bootstrap

## Document Control

| Field            | Value                                                  |
| :--------------- | :----------------------------------------------------- |
| **Document ID**  | LCS-CL-002a                                            |
| **Version**      | v0.0.2a                                                |
| **Date**         | 2026-01-28                                             |
| **Author**       | System                                                 |
| **Related Spec** | [LCS-DES-002a](../specs/v0.0.x/v0.0.2/LCS-DES-002a.md) |

---

## Summary

Converted `Lexichord.Host` from a Console application to an Avalonia UI desktop application with proper lifecycle management, platform detection, and a visible main window stub.

---

## Changes

### Files Created

| File                                              | Purpose                                  |
| :------------------------------------------------ | :--------------------------------------- |
| `src/Lexichord.Host/App.axaml.cs`                 | Application lifecycle code-behind        |
| `src/Lexichord.Host/Views/MainWindow.axaml`       | Main window XAML with welcome text       |
| `src/Lexichord.Host/Views/MainWindow.axaml.cs`    | Window code-behind                       |
| `src/Lexichord.Host/Themes/LexichordStyles.axaml` | Global styles (Avalonia 11.x compatible) |

### Files Modified

| File                                             | Change                                                |
| :----------------------------------------------- | :---------------------------------------------------- |
| `src/Lexichord.Host/Lexichord.Host.csproj`       | Added Avalonia packages, changed OutputType to WinExe |
| `src/Lexichord.Host/Program.cs`                  | Replaced console template with Avalonia bootstrap     |
| `src/Lexichord.Host/App.axaml`                   | Restructured for Avalonia 11.x (Resources + Styles)   |
| `src/Lexichord.Host/Themes/LexichordTheme.axaml` | Removed bare Styles (now resources-only)              |
| `src/Lexichord.Host/Themes/ButtonStyles.axaml`   | Removed convenience Styles (moved to LexichordStyles) |

### NuGet Packages Added

| Package                  | Version | Purpose                      |
| :----------------------- | :------ | :--------------------------- |
| `Avalonia`               | 11.2.3  | Core Avalonia framework      |
| `Avalonia.Desktop`       | 11.2.3  | Desktop platform support     |
| `Avalonia.Themes.Fluent` | 11.2.3  | Fluent design theme          |
| `Avalonia.Fonts.Inter`   | 11.2.3  | Inter font family            |
| `Avalonia.Diagnostics`   | 11.2.3  | DevTools (Debug builds only) |

### Project Configuration Changes

```xml
<!-- Lexichord.Host.csproj key changes -->
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
  <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
</PropertyGroup>
```

### Program.cs Bootstrap Pattern

```csharp
[STAThread]
public static void Main(string[] args)
{
    BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
}

public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
```

---

## Avalonia 11.x Compatibility Fixes

The existing theme files required restructuring for Avalonia 11.x:

| Issue                                       | Resolution                                                    |
| :------------------------------------------ | :------------------------------------------------------------ |
| `TimeSpan` not resolvable in XAML           | Added `xmlns:sys` namespace, use `sys:TimeSpan`               |
| `FluentTheme.xaml` not a ResourceDictionary | Only include via `Application.Styles`, not ResourceDictionary |
| Bare `Style` elements in ResourceDictionary | Created separate `Styles` file with `StyleInclude`            |

---

## Acceptance Criteria Verification

| Criterion                                         | Status  |
| :------------------------------------------------ | :------ |
| `dotnet build` succeeds with 0 Errors             | ✅ Pass |
| `dotnet run` displays window within 5 seconds     | ✅ Pass |
| Window title shows "Lexichord — The Orchestrator" | ✅ Pass |
| "Welcome to Lexichord" text visible and centered  | ✅ Pass |
| Text color follows theme (TextPrimaryBrush)       | ✅ Pass |
| Minimum window size (1024×768) enforced           | ✅ Pass |
| Window starts centered on screen                  | ✅ Pass |
| Close button terminates application               | ✅ Pass |
| Exit code is 0 after normal close                 | ✅ Pass |

---

## Notes

- This is the first visual release — the application now launches as a window instead of console.
- The `Controls/` directory is excluded from compilation (future UI components not yet implemented).
- Theme infrastructure establishes color palette, typography, and component theming for future UI work.
- Debug builds include Avalonia Diagnostics (F12 to open DevTools).
