# LCS-SBD-015: Scope Breakdown — Command Palette (Conductor's Baton)

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-SBD-015                              |
| **Version**      | v0.1.5                                   |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-26                               |
| **Depends On**   | v0.1.2 (Workspace), v0.1.3 (Editor), v0.0.4 (IModule) |

---

## 1. Executive Summary

### 1.1 The Vision

The Command Palette (Conductor's Baton) transforms Lexichord into a **keyboard-centric powerhouse**. Like VS Code's Ctrl+P, Sublime Text's Command Palette, or JetBrains' Search Everywhere, this feature provides instant access to every action in the application through a single, unified interface. Writers can execute commands, navigate to files, and remap keyboard shortcuts without touching the mouse.

### 1.2 Business Value

- **Efficiency:** Expert users can perform any action in 2-3 keystrokes.
- **Discoverability:** Commands are searchable, exposing features users didn't know existed.
- **Customization:** Users can remap shortcuts to match their muscle memory from other applications.
- **Extensibility:** Modules register their own commands, enabling plugin-like capabilities.
- **Accessibility:** Keyboard-first design benefits users who cannot use a mouse.

### 1.3 Dependencies on Previous Versions

| Component | Source | Usage |
|:----------|:-------|:------|
| IModule | v0.0.4 | Modules register commands via ICommandRegistry |
| IWorkspaceService | v0.1.2 | File Jumper indexes files from current workspace |
| IRegionManager | v0.1.1 | Palette overlays the main window |
| IConfigurationService | v0.0.3d | Store keybindings.json path and user preferences |
| IMediator | v0.0.7 | Publish CommandExecutedEvent for observability |
| Serilog | v0.0.3b | Log command execution and search operations |

---

## 2. Sub-Part Specifications

### 2.1 v0.1.5a: Command Registry

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-015a |
| **Title** | Command Registry |
| **Module** | `Lexichord.Host` / `Lexichord.Abstractions` |
| **License Tier** | Core |

**Goal:** Create `ICommandRegistry` in Abstractions allowing modules to register "Actions" with Id, Title, Category, Icon, and DefaultShortcut. Registry is a Singleton in Host.

**Key Deliverables:**
- Define `ICommandRegistry` interface in Lexichord.Abstractions
- Define `CommandDefinition` record with all metadata fields
- Implement `CommandRegistry` in Lexichord.Host as Singleton
- Modules call `ICommandRegistry.Register()` during initialization
- Support command categories for organized display
- Publish `CommandRegisteredEvent` via Event Bus

**Key Interfaces:**
```csharp
public interface ICommandRegistry
{
    void Register(CommandDefinition command);
    void Unregister(string commandId);
    IReadOnlyList<CommandDefinition> GetAllCommands();
    IReadOnlyList<CommandDefinition> GetCommandsByCategory(string category);
    CommandDefinition? GetCommand(string commandId);
    bool TryExecute(string commandId, object? parameter = null);
    event EventHandler<CommandRegisteredEventArgs>? CommandRegistered;
}
```

**Dependencies:**
- v0.0.4: IModule interface for module registration timing
- v0.0.7: Event Bus for command events

---

### 2.2 v0.1.5b: Palette UI

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-015b |
| **Title** | Palette UI |
| **Module** | `Lexichord.Host` |
| **License Tier** | Core |

**Goal:** Implement modal overlay centered in window with FuzzySharp fuzzy search filtering commands as user types. Activated by Ctrl+Shift+P (commands) or Ctrl+P (files).

**Key Deliverables:**
- Create `CommandPaletteView` Avalonia UserControl as modal overlay
- Create `CommandPaletteViewModel` with search and selection logic
- Integrate FuzzySharp NuGet package for fuzzy matching
- Implement keyboard navigation (Up/Down/Enter/Escape)
- Display command icon, title, category, and shortcut
- Show match highlighting in search results
- Auto-focus search input on activation
- Close on Escape or click outside

**Key Interfaces:**
```csharp
public class CommandPaletteViewModel : ObservableObject
{
    public string SearchQuery { get; set; }
    public ObservableCollection<CommandSearchResult> FilteredCommands { get; }
    public CommandSearchResult? SelectedCommand { get; set; }
    public ICommand ExecuteSelectedCommand { get; }
    public ICommand CloseCommand { get; }
    Task ShowAsync(PaletteMode mode);
}
```

**Dependencies:**
- v0.1.5a: ICommandRegistry (source of commands)
- FuzzySharp NuGet package for fuzzy search
- v0.1.1b: IRegionManager (overlay positioning)

---

### 2.3 v0.1.5c: File Jumper

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-015c |
| **Title** | File Jumper |
| **Module** | `Lexichord.Host` |
| **License Tier** | Core |

**Goal:** Extend the palette to index files from `IWorkspaceService`, allowing instant navigation by typing filename. Activated with Ctrl+P.

**Key Deliverables:**
- Create `IFileIndexService` interface in Abstractions
- Implement `FileIndexService` that walks workspace directory
- Index file names, paths, and modification times
- Support incremental indexing on file watcher events
- Integrate file results into Command Palette (mode switching)
- Ctrl+P opens palette in "file mode" (no `>` prefix)
- Ctrl+Shift+P opens palette in "command mode" (`>` prefix)
- Display file type icons from Material.Icons.Avalonia

**Key Interfaces:**
```csharp
public interface IFileIndexService
{
    Task RebuildIndexAsync(string workspaceRoot, CancellationToken ct = default);
    void UpdateFile(string filePath, FileIndexAction action);
    IReadOnlyList<FileIndexEntry> Search(string query, int maxResults = 50);
    int IndexedFileCount { get; }
    event EventHandler<FileIndexChangedEventArgs>? IndexChanged;
}
```

**Dependencies:**
- v0.1.2a: IWorkspaceService (provides workspace root)
- v0.1.2b: IFileSystemWatcher (triggers index updates)
- v0.1.5b: Command Palette UI (renders file results)

---

### 2.4 v0.1.5d: Keybinding Service

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-015d |
| **Title** | Keybinding Service |
| **Module** | `Lexichord.Host` |
| **License Tier** | Core |

**Goal:** Implement `KeyBindingManager` that allows users to remap keyboard shortcuts via `keybindings.json` configuration file.

**Key Deliverables:**
- Define `IKeyBindingService` interface in Abstractions
- Implement `KeyBindingManager` in Lexichord.Host
- Create `keybindings.json` schema with command ID to key gesture mapping
- Support key gesture parsing (Ctrl+Shift+P, Cmd+K, etc.)
- Handle conflicts detection (warn when two commands share a shortcut)
- Support "when" context conditions (e.g., "editorFocus")
- Provide UI for viewing/editing keybindings in Settings
- Hot-reload keybindings when file changes

**Key Interfaces:**
```csharp
public interface IKeyBindingService
{
    KeyGesture? GetBinding(string commandId);
    IReadOnlyList<KeyBinding> GetAllBindings();
    void SetBinding(string commandId, KeyGesture? gesture);
    void ResetToDefaults();
    Task LoadBindingsAsync();
    Task SaveBindingsAsync();
    bool TryHandleKeyEvent(KeyEventArgs e, string? context = null);
    event EventHandler<KeyBindingChangedEventArgs>? BindingChanged;
}
```

**Dependencies:**
- v0.1.5a: ICommandRegistry (provides command IDs and defaults)
- v0.0.3d: IConfigurationService (keybindings.json file path)
- System.Text.Json for serialization

---

## 3. Implementation Checklist

| # | Sub-Part | Task | Est. Hours |
|:--|:---------|:-----|:-----------|
| 1 | v0.1.5a | Define CommandDefinition record in Abstractions | 1 |
| 2 | v0.1.5a | Define ICommandRegistry interface in Abstractions | 1 |
| 3 | v0.1.5a | Implement CommandRegistry in Host | 3 |
| 4 | v0.1.5a | Define command events (Registered, Executed) | 1 |
| 5 | v0.1.5a | Register built-in commands (New, Open, Save, etc.) | 2 |
| 6 | v0.1.5a | Unit tests for CommandRegistry | 2 |
| 7 | v0.1.5b | Install FuzzySharp NuGet package | 0.5 |
| 8 | v0.1.5b | Create CommandPaletteView.axaml | 4 |
| 9 | v0.1.5b | Create CommandPaletteViewModel | 4 |
| 10 | v0.1.5b | Implement fuzzy search filtering | 2 |
| 11 | v0.1.5b | Implement keyboard navigation | 2 |
| 12 | v0.1.5b | Implement match highlighting | 2 |
| 13 | v0.1.5b | Add Ctrl+Shift+P activation | 1 |
| 14 | v0.1.5b | Unit tests for ViewModel | 2 |
| 15 | v0.1.5c | Define IFileIndexService interface | 1 |
| 16 | v0.1.5c | Implement FileIndexService | 4 |
| 17 | v0.1.5c | Implement incremental index updates | 2 |
| 18 | v0.1.5c | Integrate with Command Palette | 2 |
| 19 | v0.1.5c | Add Ctrl+P activation (file mode) | 1 |
| 20 | v0.1.5c | Unit tests for FileIndexService | 2 |
| 21 | v0.1.5d | Define IKeyBindingService interface | 1 |
| 22 | v0.1.5d | Implement KeyBindingManager | 4 |
| 23 | v0.1.5d | Define keybindings.json schema | 1 |
| 24 | v0.1.5d | Implement JSON serialization | 2 |
| 25 | v0.1.5d | Implement conflict detection | 2 |
| 26 | v0.1.5d | Implement "when" context conditions | 2 |
| 27 | v0.1.5d | Add keybindings UI in Settings | 3 |
| 28 | v0.1.5d | Implement hot-reload | 1 |
| 29 | v0.1.5d | Unit tests for KeyBindingManager | 2 |
| **Total** | | | **55 hours** |

---

## 4. Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|:-----|:-------|:------------|:-----------|
| FuzzySharp performance with many commands | Medium | Low | Limit results; cache command list |
| Large workspace indexing slow | Medium | Medium | Background indexing; incremental updates |
| Keybinding conflicts confuse users | Medium | Medium | Clear conflict warnings; visual indicators |
| Platform-specific key differences | High | Medium | Abstract KeyGesture; platform detection |
| Modal overlay accessibility issues | Medium | Low | Proper focus management; screen reader support |
| Hot-reload file watching issues | Low | Low | Debounce; fallback to manual reload |

---

## 5. Success Metrics

| Metric | Target | Measurement |
|:-------|:-------|:------------|
| Palette open time | < 50ms | Time from keypress to visible |
| Search responsiveness | < 16ms | Time to filter results per keystroke |
| File indexing time (1000 files) | < 2s | Time from workspace open to index ready |
| Command execution latency | < 10ms | Time from Enter to action start |
| Memory overhead per command | < 1KB | Profiler measurement |

---

## 6. What This Enables

After v0.1.5, Lexichord will support:
- Keyboard-centric workflow for all actions
- Instant file navigation with Ctrl+P
- Unified command access with Ctrl+Shift+P
- User-customizable keybindings
- Module-contributed commands appearing in palette
- Foundation for future vim-mode plugin
- Foundation for command history and frecency sorting
