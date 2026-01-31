# LCS-SBD-013: Scope Breakdown — The Manuscript (Editor Module)

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-SBD-013                              |
| **Version**      | v0.1.3                                   |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-26                               |
| **Depends On**   | v0.1.1 (Layout Engine), v0.0.7 (Event Bus) |

---

## 1. Executive Summary

### 1.1 The Vision

The Manuscript (Editor Module) is the **heart of Lexichord** — the core product that makes the Free tier valuable. By integrating AvalonEdit, a high-performance text editor component, Lexichord becomes a legitimate writing tool capable of handling large documents, syntax highlighting, and professional editing features.

This module transforms Lexichord from an application framework into a **functional writing environment** that can compete with established text editors while laying the groundwork for premium AI-powered features.

### 1.2 Business Value

- **Core Product:** The Editor is the "product" for Free tier users — a fast, reliable text editor.
- **Performance:** AvalonEdit handles million-line files without lag.
- **Developer Experience:** Syntax highlighting for Markdown, JSON, YAML, XML.
- **User Retention:** Professional editing features (search/replace, line numbers, zoom) create stickiness.
- **Upgrade Path:** Editor provides the canvas for premium AI features (v0.2.x+).

### 1.3 Dependencies on Previous Versions

| Component | Source | Usage |
|:----------|:-------|:------|
| IRegionManager | v0.1.1b | Register EditorView in Center document region |
| DocumentViewModel | v0.1.1d | Base class for ManuscriptViewModel |
| ILayoutService | v0.1.1c | Editor tabs persist across sessions |
| IMediator / Event Bus | v0.0.7 | Publish DocumentChangedEvent, SearchExecutedEvent |
| IConfigurationService | v0.0.3d | Load/save editor settings |
| ISettingsService | v0.1.6a | Persist editor preferences (font, tab size) |
| Serilog | v0.0.3b | Log editor operations |

---

## 2. Sub-Part Specifications

### 2.1 v0.1.3a: AvalonEdit Integration

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-013a |
| **Title** | AvalonEdit Integration |
| **Module** | `Lexichord.Modules.Editor` |
| **License Tier** | Core |

**Goal:** Create the `Lexichord.Modules.Editor` module and embed the AvalonEdit TextEditor control with essential features: line numbers, word wrap toggle, and large file handling.

**Key Deliverables:**
- Create `Lexichord.Modules.Editor` project
- Install `AvaloniaEdit` NuGet package
- Implement `ManuscriptView` with TextEditor control
- Implement `ManuscriptViewModel` extending `DocumentViewModel`
- Wire line numbers and word wrap from settings

**Key Interfaces:**
```csharp
public interface IEditorService
{
    Task<ManuscriptViewModel> OpenDocumentAsync(string filePath);
    Task<ManuscriptViewModel> CreateDocumentAsync(string title);
    Task<bool> SaveDocumentAsync(ManuscriptViewModel document);
    IReadOnlyList<ManuscriptViewModel> GetOpenDocuments();
}
```

**Dependencies:**
- v0.1.1b: IRegionManager (Center region injection)
- v0.1.1d: DocumentViewModel (base class)

---

### 2.2 v0.1.3b: Syntax Highlighting Service

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-013b |
| **Title** | Syntax Highlighting Service |
| **Module** | `Lexichord.Modules.Editor` |
| **License Tier** | Core |

**Goal:** Implement an `.xshd` definition loader that applies syntax highlighting based on file extension and respects the active App Theme (light/dark).

**Key Deliverables:**
- Create `ISyntaxHighlightingService` interface
- Implement `XshdHighlightingService` with definition loader
- Bundle `.xshd` files for Markdown, JSON, YAML, XML
- Theme-aware color palette switching (light/dark variants)
- Auto-detect highlighting mode from file extension

**Key Interfaces:**
```csharp
public interface ISyntaxHighlightingService
{
    IHighlightingDefinition? GetHighlighting(string fileExtension);
    IHighlightingDefinition? GetHighlightingByName(string name);
    void RegisterHighlighting(string name, string[] extensions, IHighlightingDefinition definition);
    void SetTheme(EditorTheme theme);
    IReadOnlyList<string> GetAvailableHighlightings();
}
```

**Dependencies:**
- v0.1.3a: ManuscriptView (applies highlighting)
- v0.0.5: IThemeManager (theme change notifications)

---

### 2.3 v0.1.3c: Search & Replace Overlay

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-013c |
| **Title** | Search & Replace Overlay |
| **Module** | `Lexichord.Modules.Editor` |
| **License Tier** | Core |

**Goal:** Create a custom UI overlay for search (not a popup window) that integrates seamlessly with the editor and uses AvalonEdit's search APIs.

**Key Deliverables:**
- Implement `SearchOverlayView` as inline overlay control
- Wire `Ctrl+F` to open search overlay
- Implement Find Next (`F3` / `Enter`), Find Previous (`Shift+F3`)
- Implement Replace and Replace All functionality
- Support regex search mode toggle
- Match case and whole word options
- Highlight all matches in document

**Key Interfaces:**
```csharp
public interface ISearchService
{
    void ShowSearch();
    void HideSearch();
    SearchResult? FindNext(string searchText, SearchOptions options);
    SearchResult? FindPrevious(string searchText, SearchOptions options);
    int ReplaceAll(string searchText, string replaceText, SearchOptions options);
    void HighlightAllMatches(string searchText, SearchOptions options);
    void ClearHighlights();
}
```

**Dependencies:**
- v0.1.3a: ManuscriptView (overlay host)
- v0.0.7: Event Bus (SearchExecutedEvent)

---

### 2.4 v0.1.3d: Editor Configuration

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-013d |
| **Title** | Editor Configuration |
| **Module** | `Lexichord.Modules.Editor` |
| **License Tier** | Core |

**Goal:** Implement editor settings with Cascadia Code as default font, `Ctrl+Scroll` zoom, Tab/Space configuration, and persistence via `ISettingsService`.

**Key Deliverables:**
- Define `EditorSettings` record with all configurable options
- Implement Cascadia Code as default font (with fallback)
- Wire `Ctrl+Scroll` for font size zoom (10pt - 72pt range)
- Implement Tab/Space toggle with configurable tab width
- Persist settings via `ISettingsService`
- Create EditorSettingsView for Settings module integration

**Key Interfaces:**
```csharp
public interface IEditorConfigurationService
{
    EditorSettings GetSettings();
    Task SaveSettingsAsync(EditorSettings settings);
    void ApplySettings(TextEditor editor);
    event EventHandler<EditorSettingsChangedEventArgs> SettingsChanged;
}
```

**Dependencies:**
- v0.0.6: ISettingsService (persistence)
- v0.1.3a: ManuscriptView (applies settings)

---

## 3. Implementation Checklist

| # | Sub-Part | Task | Est. Hours |
|:--|:---------|:-----|:-----------|
| 1 | v0.1.3a | Create `Lexichord.Modules.Editor` project | 0.5 |
| 2 | v0.1.3a | Install AvaloniaEdit NuGet package | 0.5 |
| 3 | v0.1.3a | Define IEditorService interface in Abstractions | 1 |
| 4 | v0.1.3a | Implement ManuscriptView.axaml with TextEditor | 4 |
| 5 | v0.1.3a | Implement ManuscriptViewModel : DocumentViewModel | 4 |
| 6 | v0.1.3a | Implement EditorService for document management | 4 |
| 7 | v0.1.3a | Wire line numbers toggle | 1 |
| 8 | v0.1.3a | Wire word wrap toggle | 1 |
| 9 | v0.1.3a | Unit tests for ManuscriptViewModel | 2 |
| 10 | v0.1.3b | Define ISyntaxHighlightingService interface | 1 |
| 11 | v0.1.3b | Implement XshdHighlightingService | 4 |
| 12 | v0.1.3b | Create Markdown.xshd definition | 2 |
| 13 | v0.1.3b | Create JSON.xshd definition | 1 |
| 14 | v0.1.3b | Create YAML.xshd definition | 1 |
| 15 | v0.1.3b | Create XML.xshd definition | 1 |
| 16 | v0.1.3b | Implement theme-aware color switching | 2 |
| 17 | v0.1.3b | Unit tests for highlighting service | 2 |
| 18 | v0.1.3c | Design SearchOverlayView.axaml | 2 |
| 19 | v0.1.3c | Implement SearchOverlayViewModel | 4 |
| 20 | v0.1.3c | Wire Ctrl+F to show overlay | 1 |
| 21 | v0.1.3c | Implement Find Next/Previous | 2 |
| 22 | v0.1.3c | Implement Replace/Replace All | 2 |
| 23 | v0.1.3c | Implement match highlighting | 2 |
| 24 | v0.1.3c | Unit tests for search service | 2 |
| 25 | v0.1.3d | Define EditorSettings record | 1 |
| 26 | v0.1.3d | Implement IEditorConfigurationService | 3 |
| 27 | v0.1.3d | Wire Ctrl+Scroll zoom | 2 |
| 28 | v0.1.3d | Implement Tab/Space configuration | 1 |
| 29 | v0.1.3d | Font family selection with fallback | 2 |
| 30 | v0.1.3d | EditorSettingsView for settings panel | 3 |
| 31 | v0.1.3d | Unit tests for configuration service | 2 |
| 32 | All | Integration tests for Editor module | 4 |
| **Total** | | | **64 hours** |

---

## 4. Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|:-----|:-------|:------------|:-----------|
| AvaloniaEdit version incompatibility | High | Medium | Pin specific version; test before upgrade |
| Cascadia Code not installed on user system | Medium | High | Bundle font or use system monospace fallback |
| Large file performance (>100MB) | Medium | Low | Lazy loading, virtualization, progress indicators |
| XSHD parsing errors | Low | Medium | Validate definitions at startup; log warnings |
| Theme colors clash with custom highlighting | Medium | Medium | Test all highlighting modes in both themes |
| Search regex causes ReDoS | Medium | Low | Timeout regex execution; limit pattern complexity |

---

## 5. Success Metrics

| Metric | Target | Measurement |
|:-------|:-------|:------------|
| Document open time (<1MB) | < 100ms | Stopwatch in OpenDocumentAsync |
| Document open time (10MB) | < 500ms | Stopwatch in OpenDocumentAsync |
| Search latency (100K lines) | < 50ms | Stopwatch in FindNext |
| Replace All (1000 matches) | < 200ms | Stopwatch in ReplaceAll |
| Memory per document | < 10MB + file size | Memory profiler |
| Ctrl+Scroll zoom response | < 16ms | 60fps during zoom |

---

## 6. What This Enables

After v0.1.3, Lexichord will support:

- **Core Product:** A fully functional text editor for Free tier users
- **Developer Workflow:** Edit Markdown, JSON, YAML, XML with syntax highlighting
- **Large Documents:** Handle manuscripts, logs, and data files efficiently
- **Professional UX:** Search, replace, zoom, customizable appearance
- **Foundation for AI:** Editor canvas ready for v0.2.x AI writing assistant features
- **Module Pattern:** Template for additional editor-based modules (Code, Notes)
