# LCS-CL-012c: Tree View UI

**Version:** v0.1.2c  
**Category:** Modules  
**Feature Name:** Tree View UI (Project Explorer)  
**Date:** 2026-01-29

---

## Summary

Implements the Tree View UI for the Project Explorer, providing a hierarchical file browser with Material Design icons, lazy loading, and MediatR-based event handling. The tree view responds to workspace open/close events and external file system changes, enabling real-time updates without manual refresh.

---

## New Features

### FileTreeNode Model

- **Lazy Loading** — Directory contents are loaded on-demand when a node is expanded. Uses a loading placeholder to show an expand arrow before content is loaded.

- **Icon Mapping** — Dynamic icon selection based on file type using Material Design Icons:
    - Folders: `Folder` (collapsed), `FolderOpen` (expanded)
    - Programming: `.cs` → `LanguageCsharp`, `.py` → `LanguagePython`, `.ts` → `LanguageTypescript`, etc.
    - Documents: `.md` → `LanguageMarkdown`, `.json` → `CodeJson`, `.xml` → `Xml`
    - Default: `FileOutline` for unknown extensions

- **Sorting** — Directories always appear before files, with alphabetical sorting within each category. Uses case-insensitive comparison.

- **Edit Mode** — Support for inline renaming with `BeginEdit()`, `CancelEdit()` methods and `IsEditing`, `EditName` observable properties.

- **Tree Navigation** — `FindByPath()` method for locating nodes by absolute path, with `Depth` computed property for current nesting level.

### IFileSystemAccess Abstraction

- **Testable File System** — Interface for file system operations enabling mock-based testing:
    - `GetDirectoryContentsAsync()` — Async directory enumeration
    - `Exists()` — Path existence check
    - `IsDirectory()` — Directory vs file check

- **DirectoryEntry Record** — Immutable record containing `Name`, `FullPath`, and `IsDirectory` for each file system entry.

### FileSystemAccess Service

- **Async Enumeration** — Uses `Task.Run` to avoid blocking UI during directory listing
- **Hidden File Filtering** — Excludes files starting with `.` (Unix) and files with `Hidden` attribute (Windows)
- **Error Handling** — Gracefully handles `UnauthorizedAccessException` and `IOException` during enumeration

### ProjectExplorerViewModel

- **MediatR Event Handlers** — Implements `INotificationHandler<T>` for:
    - `WorkspaceOpenedEvent` — Loads the workspace tree
    - `WorkspaceClosedEvent` — Clears the tree
    - `ExternalFileChangesEvent` — Incrementally updates the tree

- **Incremental Updates** — Processes external file changes without full tree reload:
    - Created files/folders added to their parent node
    - Deleted items removed from the tree
    - Renamed items handled as delete + create

- **Commands** — Async relay commands for UI actions:
    - `LoadWorkspaceCommand` — Load workspace by path
    - `RefreshCommand` — Full tree refresh
    - `ExpandAllCommand` — Recursively expand all directories
    - `CollapseAllCommand` — Collapse all directories
    - `OpenSelectedFileCommand` — Publish `FileOpenRequestedEvent`

### FileOpenRequestedEvent

- **Cross-Module Communication** — MediatR notification published when user double-clicks a file, allowing editor modules to respond.

### ProjectExplorerView

- **Material Design Integration** — Uses `Material.Icons.Avalonia` for file/folder icons

- **Tree Layout** — `TreeView` with `TreeDataTemplate` for recursive structure:
    - Icon with color based on item type
    - Filename text (or TextBox in edit mode)
    - Expand/collapse with lazy loading

- **Toolbar** — Refresh, Expand All, Collapse All buttons

- **Loading Overlay** — Progress bar shown during workspace loading

- **Empty State** — "No folder open" message with Open Folder button

- **Status Bar** — Shows item count or status message

---

## Files Added

### Lexichord.Abstractions

| File                                                          | Description                                 |
| :------------------------------------------------------------ | :------------------------------------------ |
| `src/Lexichord.Abstractions/Contracts/IFileSystemAccess.cs`   | Interface + `DirectoryEntry` record         |
| `src/Lexichord.Abstractions/Events/FileOpenRequestedEvent.cs` | MediatR notification for file open requests |

### Lexichord.Modules.Workspace

| File                                                                     | Description                                   |
| :----------------------------------------------------------------------- | :-------------------------------------------- |
| `src/Lexichord.Modules.Workspace/Models/FileTreeNode.cs`                 | Tree node model with lazy loading             |
| `src/Lexichord.Modules.Workspace/Services/FileSystemAccess.cs`           | `IFileSystemAccess` implementation            |
| `src/Lexichord.Modules.Workspace/ViewModels/ProjectExplorerViewModel.cs` | ViewModel with MediatR handlers               |
| `src/Lexichord.Modules.Workspace/Views/ProjectExplorerView.axaml`        | XAML view with TreeView and Material icons    |
| `src/Lexichord.Modules.Workspace/Views/ProjectExplorerView.axaml.cs`     | Code-behind with event handlers and converter |

### Unit Tests

| File                                                                            | Description                             |
| :------------------------------------------------------------------------------ | :-------------------------------------- |
| `tests/Lexichord.Tests.Unit/Modules/Workspace/FileTreeNodeTests.cs`             | 26 tests for `FileTreeNode` model       |
| `tests/Lexichord.Tests.Unit/Modules/Workspace/ProjectExplorerViewModelTests.cs` | 18 tests for `ProjectExplorerViewModel` |

---

## Files Modified

### Lexichord.Modules.Workspace

| File                                                                 | Modification                                                  |
| :------------------------------------------------------------------- | :------------------------------------------------------------ |
| `src/Lexichord.Modules.Workspace/WorkspaceModule.cs`                 | Added service registrations and view registration             |
| `src/Lexichord.Modules.Workspace/Lexichord.Modules.Workspace.csproj` | Added `Avalonia`, `Material.Icons.Avalonia`, enabled bindings |

---

## Architecture

### Component Diagram

```
┌────────────────────────────────────────────────────────────────┐
│                      WorkspaceModule                           │
├────────────────────────────────────────────────────────────────┤
│  RegisterServices()                                            │
│    ├─ IFileSystemAccess → FileSystemAccess                     │
│    └─ ProjectExplorerViewModel (Transient)                     │
│                                                                │
│  InitializeAsync()                                             │
│    └─ IRegionManager.RegisterToolAsync(Left, ProjectExplorer)  │
└────────────────────────────────────────────────────────────────┘
         │                                    │
         ▼                                    ▼
┌─────────────────────┐        ┌────────────────────────────────┐
│  FileSystemAccess   │        │   ProjectExplorerViewModel     │
├─────────────────────┤        ├────────────────────────────────┤
│  GetContentsAsync() │◄───────│  RootNodes                     │
│  Exists()           │        │  SelectedNode                  │
│  IsDirectory()      │        │  Commands                      │
└─────────────────────┘        │  INotificationHandler<...>     │
                               └────────────────────────────────┘
                                             │
                                             ▼
                               ┌────────────────────────────────┐
                               │      FileTreeNode              │
                               ├────────────────────────────────┤
                               │  Name, FullPath, IsDirectory   │
                               │  Children (ObservableCollection)│
                               │  IconKind, SortKey, Depth      │
                               │  LoadChildrenAsync()           │
                               │  FindByPath()                  │
                               └────────────────────────────────┘
```

### Event Flow

```
User opens workspace
        │
        ▼
WorkspaceService.OpenWorkspaceAsync()
        │
        ▼
WorkspaceOpenedEvent (MediatR)
        │
        ▼
ProjectExplorerViewModel.Handle()
        │
        ▼
LoadWorkspaceAsync()
        │
        ├───► Create root FileTreeNode
        │
        ├───► FileSystemAccess.GetDirectoryContentsAsync()
        │
        └───► Populate RootNodes collection
```

### External Change Handling

```
File system change detected
        │
        ▼
RobustFileSystemWatcher.ChangesDetected
        │
        ▼
WorkspaceService → ExternalFileChangesEvent
        │
        ▼
ProjectExplorerViewModel.Handle()
        │
        ├─ Created → FindByPath(parent) → Add child node
        │
        ├─ Deleted → FindByPath(item) → Remove from parent
        │
        └─ Renamed → Delete old + Create new
```

---

## Test Coverage

### FileTreeNodeTests (26 tests)

- **IconKind Tests (8):** Expanded/collapsed folders, programming files (.cs, .py, .ts, .js, .java, .go, .rs), document files (.md, .txt, .json, .xml), unknown extensions, placeholders
- **SortKey Tests (5):** Directory prefix, file prefix, directory-before-file ordering, alphabetical within category, case insensitivity
- **FindByPath Tests (5):** Self match, child match, grandchild match, non-existent, skip placeholders
- **LoadChildrenAsync Tests (5):** Populate from file system, sort directories first, add placeholders to directories, idempotent loading, skip file nodes
- **Edit Mode Tests (2):** Begin edit, cancel edit
- **Depth Tests (3):** Root node (0), child node (1), grandchild node (2)
- **Placeholder Tests (2):** Correct properties, normal node detection

### ProjectExplorerViewModelTests (18 tests)

- **Initial State (2):** No workspace → empty nodes, workspace open → loads tree
- **LoadWorkspaceAsync (3):** Populates root nodes, sorts directories first, sets status message
- **WorkspaceOpenedEvent (1):** Loads new tree
- **WorkspaceClosedEvent (1):** Clears tree
- **ExternalFileChangesEvent (3):** File created adds node, file deleted removes node, file renamed updates node
- **OpenSelectedFileAsync (3):** Publishes event, ignores directories, ignores no selection
- **CollapseAll (1):** Collapses all nodes
- **RefreshAsync (2):** Reloads tree, does nothing when no workspace

---

## Dependencies

| Package                   | Version            | Purpose                          |
| :------------------------ | :----------------- | :------------------------------- |
| `Avalonia`                | 11.2.3             | UI framework for views           |
| `Material.Icons.Avalonia` | 2.0.0+             | Material Design icon integration |
| `CommunityToolkit.Mvvm`   | (via Abstractions) | ObservableObject, RelayCommand   |

---

## Breaking Changes

None. This is a new feature with no changes to existing public APIs.

---

## Related Documents

| Document       | Description                           |
| :------------- | :------------------------------------ |
| `LCS-DES-012c` | Design specification for Tree View UI |
| `LCS-SBD-012`  | Scope breakdown for Explorer module   |
| `LCS-CL-012a`  | Workspace service and interface       |
| `LCS-CL-012b`  | File system watcher implementation    |

---

## Verification

```bash
# Build solution
dotnet build

# Run Tree View UI tests
dotnet test --filter "FullyQualifiedName~FileTreeNode|FullyQualifiedName~ProjectExplorerViewModel"

# Run all Workspace module tests
dotnet test --filter "FullyQualifiedName~Modules.Workspace"

# Run full test suite
dotnet test
```

---

## Next Steps

- **v0.1.2d:** Project Explorer behaviors — Context menus, actual rename implementation, drag-and-drop
- **v0.1.3:** Integration with editor modules via `FileOpenRequestedEvent`
