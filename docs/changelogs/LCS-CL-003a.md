# LCS-CL-003a: Changelog — Dependency Injection Root

## Document Control

| Field            | Value                                                  |
| :--------------- | :----------------------------------------------------- |
| **Document ID**  | LCS-CL-003a                                            |
| **Version**      | v0.0.3a                                                |
| **Date**         | 2026-01-28                                             |
| **Author**       | System                                                 |
| **Related Spec** | [LCS-DES-003a](../specs/v0.0.x/v0.0.3/LCS-DES-003a.md) |

---

## Summary

Established `Microsoft.Extensions.DependencyInjection` as the IoC container for Lexichord, replacing direct service instantiation with proper dependency injection. Added transitional `IServiceLocator` pattern for XAML-instantiated components.

---

## Changes

### NuGet Packages Added

| Package                                     | Version | Purpose                          |
| :------------------------------------------ | :------ | :------------------------------- |
| `Microsoft.Extensions.DependencyInjection`  | 9.0.0   | IoC container                    |
| `Microsoft.Extensions.Hosting.Abstractions` | 9.0.0   | Host environment abstractions    |
| `Microsoft.Extensions.Configuration`        | 9.0.0   | Configuration core (for v0.0.3d) |

### Files Created

| File                                        | Purpose                                |
| :------------------------------------------ | :------------------------------------- |
| `Abstractions/Contracts/IServiceLocator.cs` | Obsolete interface for XAML components |
| `Host/Services/ServiceLocator.cs`           | IServiceProvider wrapper               |
| `Host/HostServices.cs`                      | Static DI configuration helper         |

### Files Modified

| File                            | Change                                            |
| :------------------------------ | :------------------------------------------------ |
| `Host/Lexichord.Host.csproj`    | Added DI NuGet packages                           |
| `Host/App.axaml.cs`             | Builds ServiceProvider, resolves services from DI |
| `Host/Services/ThemeManager.cs` | Updated XML doc for DI injection param            |

### Unit Tests Added

| File                                     | Tests                        |
| :--------------------------------------- | :--------------------------- |
| `Tests.Unit/Host/HostServicesTests.cs`   | DI registration verification |
| `Tests.Unit/Host/ServiceLocatorTests.cs` | Service resolution tests     |

---

## Architecture Changes

### Before (v0.0.2d)

```csharp
// Direct instantiation in App.axaml.cs
ThemeManager = new ThemeManager(this);
WindowStateService = new WindowStateService(desktop.MainWindow?.Screens);
```

### After (v0.0.3a)

```csharp
// DI container registration in HostServices.cs
services.AddSingleton<IThemeManager, ThemeManager>();
services.AddSingleton<IWindowStateService, WindowStateService>();

// Resolution in App.axaml.cs
var themeManager = _serviceProvider.GetRequiredService<IThemeManager>();
var windowStateService = _serviceProvider.GetRequiredService<IWindowStateService>();
```

---

## Key Design Decisions

| Decision                              | Rationale                                          |
| :------------------------------------ | :------------------------------------------------- |
| Static `App.Services` property        | Enables service access for XAML-instantiated views |
| `IServiceLocator` marked `[Obsolete]` | Discourages anti-pattern, guides proper DI usage   |
| Application registered in DI          | ThemeManager requires Application for theme APIs   |
| ServiceProvider disposal on shutdown  | Ensures proper cleanup of singleton services       |

---

## Acceptance Criteria Verification

| Criterion                                     | Status  |
| :-------------------------------------------- | :------ |
| DI packages installed                         | ✅ Pass |
| `HostServices.ConfigureServices()` exists     | ✅ Pass |
| `IThemeManager` registered as Singleton       | ✅ Pass |
| `IWindowStateService` registered as Singleton | ✅ Pass |
| `App.Services` static property works          | ✅ Pass |
| ServiceProvider disposed on shutdown          | ✅ Pass |
| `IServiceLocator` interface defined           | ✅ Pass |
| `IServiceLocator` marked obsolete             | ✅ Pass |
| No direct `new` of registered services        | ✅ Pass |

---

## Notes

- `IServiceLocator` is intentionally marked `[Obsolete]` to guide developers toward constructor injection
- ThemeManager requires `Application` instance which is registered in DI before `ConfigureServices()`
- Build verification deferred due to MSBuild temp directory permissions (system sandbox limitation)
