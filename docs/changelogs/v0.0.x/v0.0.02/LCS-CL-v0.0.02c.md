# LCS-CL-002c: Changelog — Runtime Theme Switching

## Document Control

| Field            | Value                                                  |
| :--------------- | :----------------------------------------------------- |
| **Document ID**  | LCS-CL-002c                                            |
| **Version**      | v0.0.2c                                                |
| **Date**         | 2026-01-28                                             |
| **Author**       | System                                                 |
| **Related Spec** | [LCS-DES-002c](../specs/v0.0.x/v0.0.2/LCS-DES-002c.md) |

---

## Summary

Implemented runtime theme switching with Dark/Light toggle via StatusBar button and system theme detection.

---

## Changes

### Files Created

| File                                      | Purpose                               |
| :---------------------------------------- | :------------------------------------ |
| `Abstractions/Contracts/ThemeMode.cs`     | enum: System, Dark, Light             |
| `Abstractions/Contracts/IThemeManager.cs` | Theme manager interface               |
| `Host/Services/ThemeManager.cs`           | Uses Avalonia's RequestedThemeVariant |

### Files Modified

| File                                  | Change                                   |
| :------------------------------------ | :--------------------------------------- |
| `Host/App.axaml.cs`                   | Creates ThemeManager, wires to StatusBar |
| `Host/Views/MainWindow.axaml`         | Added x:Name to StatusBar                |
| `Host/Views/MainWindow.axaml.cs`      | Added StatusBar property accessor        |
| `Host/Views/Shell/StatusBar.axaml`    | Added Click handler, x:Name on icon      |
| `Host/Views/Shell/StatusBar.axaml.cs` | Theme toggle logic + Initialize()        |

---

## API

```csharp
public interface IThemeManager
{
    ThemeMode CurrentTheme { get; }
    event EventHandler<ThemeMode>? ThemeChanged;
    void SetTheme(ThemeMode mode);
    void ToggleTheme();
    ThemeMode GetEffectiveTheme();
}
```

---

## Acceptance Criteria Verification

| Criterion                              | Status  |
| :------------------------------------- | :------ |
| `dotnet build` succeeds                | ✅ Pass |
| IThemeManager exists in Abstractions   | ✅ Pass |
| ThemeManager exists in Host/Services   | ✅ Pass |
| Click toggle → colors switch instantly | ✅ Pass |
| Icon shows 🌙 (dark) / ☀️ (light)      | ✅ Pass |
| System theme detected on startup       | ✅ Pass |
| ThemeChanged event fires on toggle     | ✅ Pass |

---

## Notes

- Persistence deferred to v0.0.2d (WindowStateService)
- ThemeVariant mapping: Dark → `ThemeVariant.Dark`, Light → `ThemeVariant.Light`, System → `ThemeVariant.Default`
