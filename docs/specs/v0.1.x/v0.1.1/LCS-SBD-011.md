# LCS-SBD-011: Scope Breakdown — The Layout Engine (Docking System)

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-SBD-011                              |
| **Version**      | v0.1.1                                   |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-26                               |
| **Depends On**   | v0.0.8 (Golden Skeleton)                 |

---

## 1. Executive Summary

### 1.1 The Vision

The Layout Engine transforms Lexichord from a static application shell into a **dynamic, IDE-like workspace**. By replacing the fixed grid layout from v0.0.2 with a professional docking system, users gain the flexibility to organize their workspace exactly as they prefer—just like Visual Studio, VS Code, or JetBrains IDEs.

### 1.2 Business Value

- **User Experience:** Writers can customize their workspace to match their workflow.
- **Module Integration:** Future modules (RAG, Agents, Style) can inject views without knowing layout details.
- **Persistence:** Layout state survives restarts—users never lose their arrangement.
- **Professional Feel:** Docking systems are expected in modern IDEs and writing tools.

### 1.3 Dependencies on v0.0.x

| Component | Source | Usage |
|:----------|:-------|:------|
| IModule | v0.0.4 | Modules register views via docking |
| IShellRegionView | v0.0.8 | Abstraction over dock regions |
| Event Bus | v0.0.7 | Publish LayoutChangedEvent |
| IConfigurationService | v0.0.3d | Persist layout settings |
| Serilog | v0.0.3b | Log layout operations |

---

## 2. Sub-Part Specifications

### 2.1 v0.1.1a: Dock Library Integration

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-011a |
| **Title** | Dock Library Integration |
| **Module** | `Lexichord.Host` |
| **License Tier** | Core |

**Goal:** Integrate `Dock.Avalonia` into the Host and establish the default docking layout with a central Document Region and collapsible Tool Regions.

**Key Deliverables:**
- Install and configure `Dock.Avalonia` NuGet package
- Implement `IDockFactory` for creating default layout
- Define five dock regions: Top, Left, Center, Right, Bottom
- Create `DockableHostView` wrapper for MainWindow content

**Key Interfaces:**
```csharp
public interface IDockFactory
{
    IRootDock CreateDefaultLayout();
    IDockable CreateDocument(string title, object content);
    IToolDock CreateToolPane(ShellRegion region);
}
```

**Dependencies:**
- v0.0.2c: MainWindow Shell (provides container for dock)
- v0.0.8: IShellRegionView (abstraction for region views)

---

### 2.2 v0.1.1b: Region Injection Service

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-011b |
| **Title** | Region Injection Service |
| **Module** | `Lexichord.Host` |
| **License Tier** | Core |

**Goal:** Implement `IRegionManager` to allow modules to inject views into specific dock regions without coupling to the docking library.

**Key Deliverables:**
- Define `IRegionManager` interface in Abstractions
- Implement `DockRegionManager` in Host
- Support lazy view materialization
- Handle region availability checks

**Key Interfaces:**
```csharp
public interface IRegionManager
{
    void RegisterView(ShellRegion region, Type viewType, int order = 0);
    void RegisterView<TView>(ShellRegion region, int order = 0) where TView : class;
    Task<bool> ActivateViewAsync(ShellRegion region, Type viewType);
    IReadOnlyList<IRegionView> GetViews(ShellRegion region);
    event EventHandler<RegionViewChangedEventArgs> ViewChanged;
}
```

**Dependencies:**
- v0.1.1a: Dock Library Integration (provides dock infrastructure)
- v0.0.4: IModule (modules call RegisterView during InitializeAsync)

---

### 2.3 v0.1.1c: Layout Serialization

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-011c |
| **Title** | Layout Serialization |
| **Module** | `Lexichord.Host` |
| **License Tier** | Core |

**Goal:** Implement `ILayoutService` to serialize dock layout state on exit and restore it on startup, preserving panel widths, positions, and open tabs.

**Key Deliverables:**
- Define `ILayoutService` interface
- Implement JSON serialization of dock state
- Handle version migration (layout format changes)
- Implement layout reset to defaults option

**Key Interfaces:**
```csharp
public interface ILayoutService
{
    Task<bool> SaveLayoutAsync(string? profileName = null);
    Task<bool> LoadLayoutAsync(string? profileName = null);
    Task ResetToDefaultAsync();
    IReadOnlyList<string> GetLayoutProfiles();
    event EventHandler<LayoutChangedEventArgs> LayoutChanged;
}
```

**Storage Location:**
- Windows: `%APPDATA%/Lexichord/layouts/default.json`
- macOS: `~/Library/Application Support/Lexichord/layouts/default.json`
- Linux: `~/.config/Lexichord/layouts/default.json`

**Dependencies:**
- v0.1.1a: Dock Library Integration (provides dock state to serialize)
- v0.0.3d: IConfigurationService (storage path configuration)

---

### 2.4 v0.1.1d: Tab Infrastructure

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-011d |
| **Title** | Tab Infrastructure |
| **Module** | `Lexichord.Host` |
| **License Tier** | Core |

**Goal:** Create the `DocumentViewModel` base class and implement tab behaviors: drag-and-drop, tear-out to floating windows, and pinning.

**Key Deliverables:**
- Abstract `DocumentViewModel` base class
- Tab drag-and-drop within dock
- Tab tear-out to floating windows
- Tab pinning functionality
- Tab close with dirty state confirmation

**Key Interfaces:**
```csharp
public abstract class DocumentViewModel : ObservableObject, IDocument
{
    public abstract string Title { get; }
    public abstract string Id { get; }
    public virtual bool CanClose => true;
    public virtual bool IsDirty { get; protected set; }
    public virtual bool IsPinned { get; set; }

    public virtual Task<bool> CanCloseAsync() => Task.FromResult(!IsDirty);
    public abstract Task OnClosingAsync();
}
```

**Dependencies:**
- v0.1.1a: Dock Library Integration (dock provides tab container)
- v0.1.1b: Region Injection Service (documents register as Center region)

---

## 3. Implementation Checklist

| # | Sub-Part | Task | Est. Hours |
|:--|:---------|:-----|:-----------|
| 1 | v0.1.1a | Install Dock.Avalonia NuGet package | 0.5 |
| 2 | v0.1.1a | Create IDockFactory interface in Abstractions | 1 |
| 3 | v0.1.1a | Implement LexichordDockFactory | 4 |
| 4 | v0.1.1a | Replace MainWindow grid with DockControl | 2 |
| 5 | v0.1.1a | Unit tests for dock factory | 2 |
| 6 | v0.1.1b | Define IRegionManager interface | 1 |
| 7 | v0.1.1b | Implement DockRegionManager | 4 |
| 8 | v0.1.1b | Add view materialization logic | 2 |
| 9 | v0.1.1b | Unit tests for region manager | 2 |
| 10 | v0.1.1c | Define ILayoutService interface | 1 |
| 11 | v0.1.1c | Implement DockLayoutSerializer | 4 |
| 12 | v0.1.1c | Add layout profile support | 2 |
| 13 | v0.1.1c | Implement version migration | 2 |
| 14 | v0.1.1c | Unit tests for serialization | 2 |
| 15 | v0.1.1d | Create DocumentViewModel base class | 2 |
| 16 | v0.1.1d | Implement IDocument interface | 1 |
| 17 | v0.1.1d | Wire up drag-and-drop | 2 |
| 18 | v0.1.1d | Implement tear-out to floating | 2 |
| 19 | v0.1.1d | Implement tab pinning | 1 |
| 20 | v0.1.1d | Unit tests for DocumentViewModel | 2 |
| **Total** | | | **39 hours** |

---

## 4. Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|:-----|:-------|:------------|:-----------|
| Dock.Avalonia version compatibility | High | Medium | Pin specific version; test before upgrade |
| Complex layout state serialization | Medium | Medium | Use well-tested JSON serialization; schema versioning |
| Performance with many tabs | Medium | Low | Implement virtualization for tab headers |
| Floating windows on multiple monitors | Low | Medium | Test multi-monitor scenarios; graceful fallback |
| Tab close loses unsaved data | High | Low | Always confirm dirty state before close |

---

## 5. Success Metrics

| Metric | Target | Measurement |
|:-------|:-------|:------------|
| Layout restore accuracy | 100% | All panel sizes/positions match saved state |
| Cold start with layout | < 500ms | Time to render restored layout |
| Memory per docked panel | < 5MB | Profiler measurement |
| Tab drag-drop latency | < 16ms | 60fps during drag operation |

---

## 6. What This Enables

After v0.1.1, Lexichord will support:
- Flexible workspace layouts like professional IDEs
- Module UI injection without tight coupling
- User preference persistence across sessions
- Foundation for v0.1.2's Project Explorer (Left region)
- Foundation for v0.1.3's Editor (Center document region)
