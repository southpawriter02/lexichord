# LCS-SBD-012: Scope Breakdown — Explorer (Project Management)

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-SBD-012                              |
| **Version**      | v0.1.2                                   |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-26                               |
| **Depends On**   | v0.1.1 (Layout Engine), v0.0.7 (Event Bus), v0.0.4 (IModule) |

---

## 1. Executive Summary

### 1.1 The Vision

The Explorer (Project Management) module transforms Lexichord from a single-file application into a **workspace-aware writing environment**. Writers can open folders containing their manuscripts, research, and resources, then navigate them through a familiar tree view interface—just like VS Code, Obsidian, or Scrivener.

### 1.2 Business Value

- **Organization:** Writers can manage multi-file projects (novels with chapters, series with books).
- **Context Awareness:** The RAG system (future) will use workspace context for smarter suggestions.
- **Familiar UX:** Tree-based file navigation is the industry standard for IDEs and writing tools.
- **External Integration:** File watcher enables seamless collaboration with Git, cloud sync, and other tools.

### 1.3 Dependencies on Previous Versions

| Component | Source | Usage |
|:----------|:-------|:------|
| IModule | v0.0.4 | WorkspaceModule implements IModule interface |
| Event Bus | v0.0.7 | Publish FileCreatedEvent, FileDeletedEvent, WorkspaceOpenedEvent |
| IRegionManager | v0.1.1 | Register ProjectExplorerView in Left dock region |
| ILayoutService | v0.1.1 | Explorer panel width persisted in layout |
| Serilog | v0.0.3b | Log workspace and file operations |
| IConfigurationService | v0.0.3d | Store recent workspaces list |

---

## 2. Sub-Part Specifications

### 2.1 v0.1.2a: Workspace Service

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-012a |
| **Title** | Workspace Service |
| **Module** | `Lexichord.Modules.Workspace` |
| **License Tier** | Core |

**Goal:** Create `Lexichord.Modules.Workspace` module and implement `IWorkspaceService` with "Current Open Folder" state management.

**Key Deliverables:**
- Create `Lexichord.Modules.Workspace` project with IModule implementation
- Define `IWorkspaceService` interface for workspace state management
- Implement folder opening with recent workspaces tracking
- Publish `WorkspaceOpenedEvent` and `WorkspaceClosedEvent` via Event Bus

**Key Interfaces:**
```csharp
public interface IWorkspaceService
{
    WorkspaceInfo? CurrentWorkspace { get; }
    bool IsWorkspaceOpen { get; }
    Task<bool> OpenWorkspaceAsync(string folderPath);
    Task CloseWorkspaceAsync();
    IReadOnlyList<string> GetRecentWorkspaces();
    event EventHandler<WorkspaceChangedEventArgs>? WorkspaceChanged;
}
```

**Dependencies:**
- v0.0.4: IModule interface for module registration
- v0.0.7: Event Bus for publishing workspace events
- v0.0.3d: IConfigurationService for storing recent workspaces

---

### 2.2 v0.1.2b: File System Watcher

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-012b |
| **Title** | File System Watcher |
| **Module** | `Lexichord.Modules.Workspace` |
| **License Tier** | Core |

**Goal:** Implement a robust `IFileSystemWatcher` wrapper that handles external file changes (git operations, cloud sync, other applications) with debouncing and batching.

**Key Deliverables:**
- Define `IFileSystemWatcher` interface in Abstractions
- Implement `RobustFileSystemWatcher` wrapping `System.IO.FileSystemWatcher`
- Handle common issues: buffer overflow, duplicate events, high-frequency changes
- Debounce rapid changes (git operations produce many events)
- Publish `FileCreatedEvent`, `FileChangedEvent`, `FileDeletedEvent`, `FileRenamedEvent`

**Key Interfaces:**
```csharp
public interface IFileSystemWatcher : IDisposable
{
    string WatchPath { get; }
    bool IsWatching { get; }
    void StartWatching(string path, string filter = "*.*");
    void StopWatching();
    event EventHandler<FileSystemChangeEventArgs>? FileChanged;
    event EventHandler<FileSystemErrorEventArgs>? Error;
}
```

**Dependencies:**
- v0.1.2a: IWorkspaceService (watches current workspace folder)
- v0.0.7: Event Bus for publishing file change events

---

### 2.3 v0.1.2c: Tree View UI

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-012c |
| **Title** | Tree View UI |
| **Module** | `Lexichord.Modules.Workspace` |
| **License Tier** | Core |

**Goal:** Build `ProjectExplorerView` UserControl with recursive folder tree, file icons using Material.Icons.Avalonia, and lazy loading for large directories.

**Key Deliverables:**
- Create `ProjectExplorerView` Avalonia UserControl
- Create `ProjectExplorerViewModel` with hierarchical data binding
- Implement `FileTreeNode` model for recursive tree structure
- Add file type icons via Material.Icons.Avalonia
- Implement lazy loading of directory contents
- Support single-click to select, double-click to open file
- Register view in Left dock region via IRegionManager

**Key Interfaces:**
```csharp
public class ProjectExplorerViewModel : ObservableObject
{
    public ObservableCollection<FileTreeNode> RootNodes { get; }
    public FileTreeNode? SelectedNode { get; set; }
    public ICommand OpenFileCommand { get; }
    public ICommand RefreshCommand { get; }
    Task LoadWorkspaceAsync(string rootPath);
}
```

**Dependencies:**
- v0.1.2a: IWorkspaceService (provides current workspace root)
- v0.1.1b: IRegionManager (register in Left region)
- Material.Icons.Avalonia NuGet package

---

### 2.4 v0.1.2d: Context Menu Actions

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-012d |
| **Title** | Context Menu Actions |
| **Module** | `Lexichord.Modules.Workspace` |
| **License Tier** | Core |

**Goal:** Implement context menu with New File, New Folder, Rename (F2), Delete, and Reveal in Explorer actions, publishing appropriate events.

**Key Deliverables:**
- Create context menu for ProjectExplorerView
- Implement "New File" with template support
- Implement "New Folder" with inline editing
- Implement "Rename" with F2 keyboard shortcut
- Implement "Delete" with confirmation dialog
- Implement "Reveal in Explorer/Finder/File Manager"
- Publish `FileCreatedEvent`, `FileDeletedEvent`, `FileRenamedEvent`

**Key Interfaces:**
```csharp
public interface IFileOperationService
{
    Task<string?> CreateFileAsync(string parentPath, string fileName);
    Task<string?> CreateFolderAsync(string parentPath, string folderName);
    Task<bool> RenameAsync(string path, string newName);
    Task<bool> DeleteAsync(string path, bool recursive = false);
    Task RevealInExplorerAsync(string path);
}
```

**Dependencies:**
- v0.1.2c: ProjectExplorerView (provides context menu host)
- v0.0.7: Event Bus for publishing file operation events

---

## 3. Implementation Checklist

| # | Sub-Part | Task | Est. Hours |
|:--|:---------|:-----|:-----------|
| 1 | v0.1.2a | Create Lexichord.Modules.Workspace project | 1 |
| 2 | v0.1.2a | Define IWorkspaceService interface in Abstractions | 1 |
| 3 | v0.1.2a | Implement WorkspaceService with folder state | 3 |
| 4 | v0.1.2a | Implement WorkspaceModule : IModule | 2 |
| 5 | v0.1.2a | Define workspace events (Opened, Closed) | 1 |
| 6 | v0.1.2a | Implement recent workspaces persistence | 2 |
| 7 | v0.1.2a | Unit tests for WorkspaceService | 2 |
| 8 | v0.1.2b | Define IFileSystemWatcher interface | 1 |
| 9 | v0.1.2b | Implement RobustFileSystemWatcher | 4 |
| 10 | v0.1.2b | Add debouncing logic for rapid changes | 2 |
| 11 | v0.1.2b | Add buffer overflow handling | 1 |
| 12 | v0.1.2b | Define file change events | 1 |
| 13 | v0.1.2b | Unit tests for file watcher | 3 |
| 14 | v0.1.2c | Create FileTreeNode model | 2 |
| 15 | v0.1.2c | Create ProjectExplorerViewModel | 3 |
| 16 | v0.1.2c | Create ProjectExplorerView AXAML | 4 |
| 17 | v0.1.2c | Add file type icons mapping | 2 |
| 18 | v0.1.2c | Implement lazy directory loading | 2 |
| 19 | v0.1.2c | Register view with IRegionManager | 1 |
| 20 | v0.1.2c | Unit tests for ViewModel | 2 |
| 21 | v0.1.2d | Define IFileOperationService interface | 1 |
| 22 | v0.1.2d | Implement FileOperationService | 3 |
| 23 | v0.1.2d | Create context menu in AXAML | 2 |
| 24 | v0.1.2d | Implement New File action | 2 |
| 25 | v0.1.2d | Implement New Folder action | 1 |
| 26 | v0.1.2d | Implement Rename action with F2 | 2 |
| 27 | v0.1.2d | Implement Delete with confirmation | 2 |
| 28 | v0.1.2d | Implement Reveal in Explorer | 1 |
| 29 | v0.1.2d | Unit tests for FileOperationService | 2 |
| **Total** | | | **55 hours** |

---

## 4. Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|:-----|:-------|:------------|:-----------|
| FileSystemWatcher buffer overflow | High | Medium | Increase buffer size; implement recovery with manual refresh |
| Large directory performance | Medium | Medium | Lazy loading; virtualization in tree view |
| Cross-platform path handling | High | Medium | Use Path.Combine; normalize separators |
| Concurrent file operations | Medium | Low | Lock mechanisms; operation queue |
| Icon package updates | Low | Low | Pin Material.Icons.Avalonia version |
| File permission errors | Medium | Medium | Try-catch with user-friendly messages |

---

## 5. Success Metrics

| Metric | Target | Measurement |
|:-------|:-------|:------------|
| Workspace open time | < 500ms | Time from click to tree visible |
| Directory expansion time | < 100ms | Time to load directory contents |
| File watcher latency | < 500ms | Time from external change to UI update |
| Memory per 1000 files | < 10MB | Profiler measurement |
| Tree scroll performance | 60fps | No jank during scroll |

---

## 6. What This Enables

After v0.1.2, Lexichord will support:
- Opening folders as workspaces for multi-file projects
- Tree-based navigation of manuscripts and resources
- Automatic detection of external file changes (git, cloud sync)
- File management (create, rename, delete) within the app
- Foundation for v0.1.3's Editor (double-click opens file)
- Foundation for future RAG module (workspace context)
