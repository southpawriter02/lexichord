# LCS-CL-011a: Dock Library Integration

**Version:** v0.1.1a  
**Category:** Host Infrastructure  
**Feature Name:** Dock Library Integration  
**Date:** 2026-01-28

---

## Summary

Integrates `Dock.Avalonia` to provide resizable, draggable panels in the main application window. Establishes abstraction interfaces that decouple modules from the docking library implementation.

---

## New Features

### Layout Interfaces (Lexichord.Abstractions)

- **IDockFactory** — Factory interface for creating and managing dock layouts
    - `CreateDefaultLayout()` — Builds 4-region layout (Left, Center, Right, Bottom)
    - `CreateDocument()` / `CreateTool()` — Factory methods for dockables
    - `FindDockable(id)` — Recursive ID-based lookup
    - `RootDock`, `DocumentDock`, `GetToolDock(region)` — Accessors

- **IDocument** — Document interface extending `IDockable`
    - `IsDirty` — Unsaved changes indicator
    - `IsPinned` — Pin state for tab behavior
    - `DocumentId` — Stable identifier for serialization
    - `CanCloseAsync()` — Close confirmation hook

- **ITool** — Tool pane interface extending `IDockable`
    - `PreferredRegion` — Default `ShellRegion` placement
    - `MinWidth`, `MinHeight` — Size constraints
    - `CanClose` — Closability control

- **DockRegionConfig** — Record for region dimensions and behavior
    - Static factory instances for Left, Right, Bottom, Center

### Layout Implementation (Lexichord.Host)

- **LexichordDockFactory** — Concrete `IDockFactory` implementation
    - Creates default 4-region layout with stable IDs
    - Caches tool docks by `ShellRegion` for quick lookup
    - Serilog logging for layout operations

- **LexichordDocument** — Document implementation with MVVM
    - Observable `IsDirty`, `IsPinned` properties
    - `DisplayTitle` with dirty indicator (\*)
    - Default close confirmation blocking dirty documents

- **LexichordTool** — Tool pane implementation with MVVM
    - Observable `PreferredRegion`, `MinWidth`, `MinHeight`
    - Bridged `CanClose` property

- **MainWindowViewModel** — ViewModel managing dock layout
    - `Layout` property bound to `DockControl`
    - `InitializeLayout()` wiring factory to UI

### UI Integration

- **MainWindow.axaml** — Updated with `DockControl`
    - Replaced static `ContentHostPanel` with dock system
    - Styling for dock chrome consistency

- **HostServices.cs** — New `AddDockServices()` extension
    - Registers `IDockFactory` and `MainWindowViewModel` as singletons

---

## Files Added

### Lexichord.Abstractions

| File                                                    | Description        |
| :------------------------------------------------------ | :----------------- |
| `src/Lexichord.Abstractions/Layout/IDockFactory.cs`     | Factory interface  |
| `src/Lexichord.Abstractions/Layout/IDocument.cs`        | Document interface |
| `src/Lexichord.Abstractions/Layout/ITool.cs`            | Tool interface     |
| `src/Lexichord.Abstractions/Layout/DockRegionConfig.cs` | Region config      |

### Lexichord.Host

| File                                                   | Description             |
| :----------------------------------------------------- | :---------------------- |
| `src/Lexichord.Host/Layout/LexichordDockFactory.cs`    | Factory implementation  |
| `src/Lexichord.Host/Layout/LexichordDocument.cs`       | Document implementation |
| `src/Lexichord.Host/Layout/LexichordTool.cs`           | Tool implementation     |
| `src/Lexichord.Host/ViewModels/MainWindowViewModel.cs` | Window ViewModel        |

### Unit Tests

| File                                                                  | Description        |
| :-------------------------------------------------------------------- | :----------------- |
| `tests/Lexichord.Tests.Unit/Host/Layout/LexichordDockFactoryTests.cs` | Factory tests (9)  |
| `tests/Lexichord.Tests.Unit/Host/Layout/LexichordDocumentTests.cs`    | Document tests (6) |

## Files Modified

| File                                                     | Description                          |
| :------------------------------------------------------- | :----------------------------------- |
| `Directory.Build.props`                                  | Added DockAvaloniaVersion property   |
| `src/Lexichord.Host/Lexichord.Host.csproj`               | Added Dock.Avalonia packages         |
| `src/Lexichord.Host/Views/MainWindow.axaml`              | Added dock namespace and DockControl |
| `src/Lexichord.Host/Views/MainWindow.axaml.cs`           | Added ViewModel property             |
| `src/Lexichord.Host/HostServices.cs`                     | Added AddDockServices()              |
| `tests/Lexichord.Tests.Unit/Lexichord.Tests.Unit.csproj` | Added Dock.Model.Avalonia package    |

---

## Architecture

### Default Layout Structure

```
┌─────────────────────────────────────────────────────────┐
│                      RootDock                           │
│  ┌─────────────────────────────────────────────────────┐│
│  │                  ProportionalDock                   ││
│  │ ┌────────┬────────────────────────┬───────────────┐ ││
│  │ │  Left  │        Center          │     Right     │ ││
│  │ │ToolDock│     DocumentDock       │   ToolDock    │ ││
│  │ │ (200px)│                        │   (250px)     │ ││
│  │ ├────────┴────────────────────────┴───────────────┤ ││
│  │ │                    Bottom                       │ ││
│  │ │                   ToolDock                      │ ││
│  │ │                   (200px)                       │ ││
│  │ └─────────────────────────────────────────────────┘ ││
│  └─────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────┘
```

### Stable IDs

| ID                    | Purpose              |
| :-------------------- | :------------------- |
| `Lexichord.Root`      | Root dock container  |
| `Lexichord.Main`      | Main vertical layout |
| `Lexichord.Documents` | Center document dock |
| `Lexichord.Left`      | Left tool dock       |
| `Lexichord.Right`     | Right tool dock      |
| `Lexichord.Bottom`    | Bottom tool dock     |

---

## Verification Commands

```bash
# 1. Build solution
dotnet build

# 2. Run all dock-related tests
dotnet test --filter "FullyQualifiedName~LexichordDockFactory"
dotnet test --filter "FullyQualifiedName~LexichordDocument"

# 3. Run all unit tests
dotnet test --filter "Category!=Integration"

# 4. Launch application to verify layout
dotnet run --project src/Lexichord.Host
```

---

## Test Summary

| Test Class                | Tests  | Status |
| :------------------------ | :----- | :----- |
| LexichordDockFactoryTests | 9      | ✅     |
| LexichordDocumentTests    | 6      | ✅     |
| **Total**                 | **15** | **✅** |

---

## Dependencies

- **NuGet Packages (v0.1.1a)**
    - `Dock.Avalonia` 11.1.0.1
    - `Dock.Model` 11.1.0.1
    - `Dock.Model.Avalonia` 11.1.0.1
    - `Dock.Model.Mvvm` 11.1.0.1

- **From v0.0.3a:** Dependency Injection, `IServiceCollection`
- **From v0.0.3b:** Serilog logging infrastructure
- **From v0.0.8a:** `ShellRegion` enum

## Enables

- **v0.1.1b:** Layout serialization and persistence
- **v0.1.1c:** Module region registration
- **v0.1.1d:** Multi-window/detachable panels
