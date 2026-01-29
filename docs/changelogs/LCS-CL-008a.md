# LCS-CL-008a: Status Bar Module

**Version:** v0.0.8a  
**Category:** Modules  
**Feature Name:** Status Bar Module ("The Hello World Golden Skeleton")  
**Date:** 2026-01-29

---

## Summary

Implements the Status Bar module as the canonical reference implementation for Lexichord modules. Establishes shell region infrastructure for modules to contribute UI to host windows, and provides placeholder services for database health monitoring and vault status tracking.

---

## New Features

### Shell Region Infrastructure (Lexichord.Abstractions)

- **ShellRegion** — Enum defining standard regions for module UI contribution
    - `Top` — Toolbar/ribbon area
    - `Left` — Navigation panel
    - `Center` — Main content area
    - `Right` — Properties/details panel
    - `Bottom` — Status bar region

- **IShellRegionView** — Interface for module views targeting shell regions
    - `TargetRegion` — Which shell region the view belongs to
    - `Order` — Sort order within the region (lower = earlier)
    - `ViewContent` — Lazy-initialized view content

- **IShellRegionManager** — Interface for collecting and providing shell region views
    - `Initialize()` — Collects all registered views from DI container
    - `GetViews(ShellRegion)` — Returns sorted views for a specific region

### Shell Region Manager (Lexichord.Host)

- **ShellRegionManager** — Implementation of IShellRegionManager
    - Thread-safe initialization with duplicate call protection
    - Lazy collection of `IShellRegionView` implementations from DI
    - Region-based grouping with order-based sorting
    - Debug logging for registration and initialization

### Status Bar Module (Lexichord.Modules.StatusBar)

- **StatusBarModule** — IModule implementation serving as the canonical reference
    - Module ID: `statusbar`
    - Version: 0.0.8
    - Demonstrates proper service registration patterns
    - Demonstrates async initialization with error handling

- **StatusBarRegionView** — IShellRegionView implementation
    - Targets `ShellRegion.Bottom` with order 100
    - Lazy view creation via IServiceProvider

### Status Bar Services (Placeholders for v0.0.8b/c)

- **IHealthRepository / HealthRepository** — Database health tracking (stub)
- **IHeartbeatService / HeartbeatService** — Application heartbeat (stub)
- **IVaultStatusService / VaultStatusService** — API key vault status (stub)
    - VaultStatus enum: `Unknown`, `Empty`, `HasKeys`, `Error`

### Status Bar Views

- **StatusBarView** — Main status bar UI
    - Database health indicator (green/red circle icons)
    - Vault status indicator with click-to-open dialog
    - Uptime display
    - Responsive hover effects

- **StatusBarViewModel** — ViewModel for StatusBarView
    - Reactive properties for health/vault status
    - RefreshVaultStatusCommand for manual refresh
    - Service event subscriptions

- **ApiKeyDialog / ApiKeyDialogViewModel** — Placeholder dialog for API key entry

---

## Files Added

### Lexichord.Abstractions

| File                                                          | Description       |
| :------------------------------------------------------------ | :---------------- |
| `src/Lexichord.Abstractions/Contracts/ShellRegion.cs`         | Shell region enum |
| `src/Lexichord.Abstractions/Contracts/IShellRegionView.cs`    | View interface    |
| `src/Lexichord.Abstractions/Contracts/IShellRegionManager.cs` | Manager interface |

### Lexichord.Host

| File                                                | Description            |
| :-------------------------------------------------- | :--------------------- |
| `src/Lexichord.Host/Services/ShellRegionManager.cs` | Manager implementation |

### Lexichord.Modules.StatusBar

| File                                                                  | Description                     |
| :-------------------------------------------------------------------- | :------------------------------ |
| `src/Lexichord.Modules.StatusBar/Lexichord.Modules.StatusBar.csproj`  | Project file                    |
| `src/Lexichord.Modules.StatusBar/StatusBarModule.cs`                  | IModule implementation          |
| `src/Lexichord.Modules.StatusBar/StatusBarRegionView.cs`              | IShellRegionView implementation |
| `src/Lexichord.Modules.StatusBar/Services/IHealthRepository.cs`       | Health repository interface     |
| `src/Lexichord.Modules.StatusBar/Services/HealthRepository.cs`        | Health repository (stub)        |
| `src/Lexichord.Modules.StatusBar/Services/IHeartbeatService.cs`       | Heartbeat service interface     |
| `src/Lexichord.Modules.StatusBar/Services/HeartbeatService.cs`        | Heartbeat service (stub)        |
| `src/Lexichord.Modules.StatusBar/Services/IVaultStatusService.cs`     | Vault status interface          |
| `src/Lexichord.Modules.StatusBar/Services/VaultStatusService.cs`      | Vault status service (stub)     |
| `src/Lexichord.Modules.StatusBar/ViewModels/StatusBarViewModel.cs`    | Status bar ViewModel            |
| `src/Lexichord.Modules.StatusBar/ViewModels/ApiKeyDialogViewModel.cs` | Dialog ViewModel                |
| `src/Lexichord.Modules.StatusBar/Views/StatusBarView.axaml`           | Status bar XAML                 |
| `src/Lexichord.Modules.StatusBar/Views/StatusBarView.axaml.cs`        | Status bar code-behind          |
| `src/Lexichord.Modules.StatusBar/Views/ApiKeyDialog.axaml`            | Dialog XAML                     |
| `src/Lexichord.Modules.StatusBar/Views/ApiKeyDialog.axaml.cs`         | Dialog code-behind              |

### Unit Tests

| File                                                                       | Description             |
| :------------------------------------------------------------------------- | :---------------------- |
| `tests/Lexichord.Tests.Unit/Modules/StatusBar/StatusBarModuleTests.cs`     | Module contract tests   |
| `tests/Lexichord.Tests.Unit/Modules/StatusBar/StatusBarRegionViewTests.cs` | Shell region view tests |

## Files Modified

| File                                                     | Description                                  |
| :------------------------------------------------------- | :------------------------------------------- |
| `Lexichord.sln`                                          | Added StatusBar module project               |
| `src/Lexichord.Host/HostServices.cs`                     | Register ShellRegionManager                  |
| `src/Lexichord.Host/Views/MainWindow.axaml.cs`           | Add shell region initialization              |
| `tests/Lexichord.Tests.Unit/Lexichord.Tests.Unit.csproj` | Add StatusBar module reference + NSubstitute |

---

## Architecture

### Module → Host Integration Flow

```
1. ModuleLoader discovers Lexichord.Modules.StatusBar.dll
2. Instantiates StatusBarModule
3. Calls RegisterServices() → registers IShellRegionView
4. Builds ServiceProvider
5. Calls InitializeAsync()
6. ShellRegionManager.Initialize() collects views
7. MainWindow calls GetViews(Bottom)
8. StatusBarView rendered in bottom region
```

### Module Dependency Rules

```
Modules → Abstractions (only)
Host → Infrastructure, Abstractions
Infrastructure → Abstractions
```

---

## Usage

### Creating a Module Shell Region View

```csharp
public sealed class MyModuleRegionView : IShellRegionView
{
    private readonly IServiceProvider _provider;
    private object? _cachedView;

    public MyModuleRegionView(IServiceProvider provider)
        => _provider = provider;

    public ShellRegion TargetRegion => ShellRegion.Left;
    public int Order => 50;

    public object ViewContent =>
        _cachedView ??= _provider.GetRequiredService<MyView>();
}
```

### Registering in Module

```csharp
public void RegisterServices(IServiceCollection services)
{
    // Register the view
    services.AddTransient<MyView>();
    services.AddTransient<MyViewModel>();

    // Register the shell region view
    services.AddSingleton<IShellRegionView, MyModuleRegionView>();
}
```

---

## Verification Commands

```bash
# 1. Build solution
dotnet build

# 2. Verify module outputs to Modules directory
ls Modules/Lexichord.Modules.StatusBar.dll

# 3. Run StatusBar module tests
dotnet test --filter "FullyQualifiedName~StatusBar"

# 4. Run all tests
dotnet test
```

---

## Test Summary

| Test Class               | Tests | Status |
| :----------------------- | :---- | :----- |
| StatusBarModuleTests     | 7     | ✅     |
| StatusBarRegionViewTests | 2     | ✅     |
| **Total**                | **9** | **✅** |

---

## Dependencies

- **From v0.0.4a:** `IModule`, `ModuleInfo` interfaces
- **From v0.0.4d:** Module loader infrastructure
- **From v0.0.7a:** MediatR for event publishing

## Enables

- **v0.0.8b:** Database heartbeat monitoring (implements IHealthRepository, IHeartbeatService)
- **v0.0.8c:** Vault status monitoring (implements IVaultStatusService)
- **Future Modules:** Shell region pattern for UI contribution
