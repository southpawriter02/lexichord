# LCS-CL-012a: Workspace Service

**Version:** v0.1.2a  
**Category:** Modules  
**Feature Name:** Workspace Service  
**Date:** 2026-01-28

---

## Summary

Implements the foundational Workspace Service that manages the current open folder state. This service tracks which project is open, persists recent workspaces, and publishes events for cross-module coordination.

---

## New Features

### Workspace Contracts (Lexichord.Abstractions)

- **IWorkspaceService** — Core service interface for workspace management
    - `CurrentWorkspace` — Property returning the active `WorkspaceInfo` or null
    - `IsWorkspaceOpen` — Boolean indicating if a workspace is currently open
    - `OpenWorkspaceAsync(folderPath)` — Opens a folder as workspace
    - `CloseWorkspaceAsync()` — Closes the current workspace
    - `GetRecentWorkspaces()` — Returns list of recent workspace paths
    - `ClearRecentWorkspacesAsync()` — Clears recent workspace history
    - `WorkspaceChanged` — Local event for UI binding

- **WorkspaceInfo** — Immutable record for workspace information
    - `RootPath` — Absolute path to workspace root
    - `Name` — Display name (derived from folder)
    - `OpenedAt` — Timestamp when workspace was opened
    - `Directory` — Computed `DirectoryInfo` accessor
    - `ContainsPath(path)` — Validates if path is within workspace (prevents traversal)

- **WorkspaceChangedEventArgs** — Event arguments for local notifications
    - `ChangeType` — Opened or Closed
    - `PreviousWorkspace` — Workspace before change (if any)
    - `NewWorkspace` — Workspace after change (if any)

- **IFileSystemWatcher** — Interface for file system change detection (v0.1.2b stub)
    - `WatchPath`, `IsWatching` — Current state
    - `StartWatching()`, `StopWatching()` — Lifecycle control
    - `ChangesDetected`, `Error` — Events for change batches and errors

### Workspace Events (MediatR Notifications)

- **WorkspaceOpenedEvent** — Published when a workspace is successfully opened
    - `WorkspaceRootPath` — The opened folder path
    - `WorkspaceName` — Display name for the workspace

- **WorkspaceClosedEvent** — Published when a workspace is closed
    - `WorkspaceRootPath` — The closed folder path

### Workspace Module

- **WorkspaceService** — Full implementation of `IWorkspaceService`
    - Thread-safe state management with lock synchronization
    - Recent workspaces persisted via `ISystemSettingsRepository`
    - Maximum 10 recent workspaces maintained
    - File watcher lifecycle management (starts on open, stops on close)
    - MediatR event publishing for cross-module communication
    - Local event raising for UI binding
    - Comprehensive logging via `ILogger<T>`

- **StubFileSystemWatcher** — Temporary no-op implementation
    - Placeholder until v0.1.2b "Robust File System Watcher" is implemented
    - All methods are no-ops; events are never raised
    - Logs initialization for debugging

- **WorkspaceModule** — Module registration implementing `IModule`
    - Registers `IWorkspaceService` as singleton
    - Registers `IFileSystemWatcher` (stub) as singleton
    - Logs module initialization messages

---

## Files Added

### Lexichord.Abstractions

| File                                                                | Description                 |
| :------------------------------------------------------------------ | :-------------------------- |
| `src/Lexichord.Abstractions/Contracts/IWorkspaceService.cs`         | Workspace service interface |
| `src/Lexichord.Abstractions/Contracts/WorkspaceInfo.cs`             | Immutable workspace record  |
| `src/Lexichord.Abstractions/Contracts/WorkspaceChangedEventArgs.cs` | Event args + enum           |
| `src/Lexichord.Abstractions/Contracts/IFileSystemWatcher.cs`        | File watcher interface      |
| `src/Lexichord.Abstractions/Events/WorkspaceEvents.cs`              | MediatR notifications       |

### Lexichord.Modules.Workspace (New Project)

| File                                                                 | Description            |
| :------------------------------------------------------------------- | :--------------------- |
| `src/Lexichord.Modules.Workspace/Lexichord.Modules.Workspace.csproj` | Module project file    |
| `src/Lexichord.Modules.Workspace/Services/WorkspaceService.cs`       | Service implementation |
| `src/Lexichord.Modules.Workspace/Services/StubFileSystemWatcher.cs`  | Stub watcher           |
| `src/Lexichord.Modules.Workspace/WorkspaceModule.cs`                 | Module registration    |

### Unit Tests

| File                                                                    | Description         |
| :---------------------------------------------------------------------- | :------------------ |
| `tests/Lexichord.Tests.Unit/Modules/Workspace/WorkspaceServiceTests.cs` | Service tests (20+) |
| `tests/Lexichord.Tests.Unit/Modules/Workspace/WorkspaceInfoTests.cs`    | Record tests (10+)  |

## Files Modified

| File                                                     | Description                       |
| :------------------------------------------------------- | :-------------------------------- |
| `Lexichord.sln`                                          | Added Workspace module project    |
| `tests/Lexichord.Tests.Unit/Lexichord.Tests.Unit.csproj` | Added Workspace project reference |

---

## Architecture

### State Management

```
┌──────────────────────────────────────────────────────────────┐
│                      WorkspaceService                        │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ State (thread-safe)                                     │ │
│  │  • _currentWorkspace: WorkspaceInfo?                    │ │
│  │  • _recentWorkspaces: List<string> (max 10)             │ │
│  │  • _lock: object                                        │ │
│  └─────────────────────────────────────────────────────────┘ │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ Dependencies                                            │ │
│  │  • IFileSystemWatcher (v0.1.2b stub for now)            │ │
│  │  • ISystemSettingsRepository (persistence)              │ │
│  │  • IMediator (cross-module events)                      │ │
│  │  • ILogger<WorkspaceService>                            │ │
│  └─────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

### Event Flow

```
┌─────────────────┐    OpenWorkspaceAsync()    ┌──────────────────────┐
│   UI Command    │ ─────────────────────────▶ │   WorkspaceService   │
└─────────────────┘                            └───────────┬──────────┘
                                                           │
           ┌───────────────────────────────────────────────┼───────────────┐
           ▼                                               ▼               ▼
┌──────────────────────┐           ┌────────────────────────────┐  ┌─────────────────┐
│ IFileSystemWatcher   │           │ ISystemSettingsRepository  │  │    IMediator    │
│   .StartWatching()   │           │ .SetValueAsync() (recent)  │  │   .Publish()    │
└──────────────────────┘           └────────────────────────────┘  └────────┬────────┘
                                                                            ▼
                                                              ┌──────────────────────────┐
                                                              │  WorkspaceOpenedEvent    │
                                                              │  (cross-module handlers) │
                                                              └──────────────────────────┘
           ┌──────────────────────────────────────────────────────────────────────────────┐
           │                         Local Event                                          │
           ▼                                                                              │
┌──────────────────────┐                                                                  │
│  WorkspaceChanged    │ ◀────────────────────────────────────────────────────────────────┘
│  (ViewModel binding) │
└──────────────────────┘
```

---

## Verification Commands

```bash
# 1. Build solution
dotnet build

# 2. Run workspace-related tests
dotnet test --filter "FullyQualifiedName~Workspace"

# 3. Verify module output location
ls -la Modules/Lexichord.Modules.Workspace.dll

# 4. Run all unit tests
dotnet test --filter "Category!=Integration"
```

---

## Test Summary

| Test Class            | Tests  | Status |
| :-------------------- | :----- | :----- |
| WorkspaceServiceTests | 20     | ✅     |
| WorkspaceInfoTests    | 13     | ✅     |
| **Total**             | **33** | **✅** |

---

## Dependencies

- **From v0.0.3a:** Dependency Injection (`IServiceCollection`)
- **From v0.0.5d:** `ISystemSettingsRepository` for persistence
- **From v0.0.7a:** MediatR for cross-module events
- **From v0.0.4a:** `IModule` interface

---

## Implementation Notes

### Dependency Adaptation

The original design specification referenced `IConfigurationService` for recent workspace persistence. However, the actual codebase uses `ISystemSettingsRepository` which provides `GetValueAsync<T>` and `SetValueAsync<T>` methods. The implementation uses JSON serialization for the recent workspaces list.

### File System Watcher Strategy

Since `v0.1.2b: Robust File System Watcher` is not yet implemented, this version includes:

1. **`IFileSystemWatcher` interface** — Defined in abstractions with all required properties and events
2. **`StubFileSystemWatcher`** — A no-op implementation that allows `WorkspaceService` to compile and run

The stub will be replaced by `RobustFileSystemWatcher` in v0.1.2b.

### Thread Safety

`WorkspaceService` uses a simple lock object for thread-safe state updates. This is sufficient for the current use case where workspace changes are infrequent user-initiated actions.

---

## Enables

- **v0.1.2b:** Robust File System Watcher (replace stub)
- **v0.1.2c:** Project Explorer Tree View
- **v0.1.2d:** File Operation Service
