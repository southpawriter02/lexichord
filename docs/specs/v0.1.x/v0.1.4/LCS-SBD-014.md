# LCS-SBD-014: Scope Breakdown — Scribe (IO & Persistence)

## Document Control

| Field            | Value                                                      |
| :--------------- | :--------------------------------------------------------- |
| **Document ID**  | LCS-SBD-014                                                |
| **Version**      | v0.1.4                                                     |
| **Status**       | Draft                                                      |
| **Last Updated** | 2026-01-26                                                 |
| **Depends On**   | v0.1.3 (Editor Module), v0.1.1d (DocumentViewModel), v0.0.5 (Database), v0.0.7 (Event Bus) |

---

## 1. Executive Summary

### 1.1 The Vision

The **Scribe** module transforms Lexichord from a read-only editor into a **full-featured authoring environment** with robust file I/O and persistence capabilities. By implementing dirty state tracking, atomic saves, safe close workflows, and recent files history, users gain confidence that their work is protected against data loss—the most critical feature for any writing application.

### 1.2 Business Value

- **Data Safety:** Atomic saves prevent file corruption during write operations.
- **User Awareness:** Visual dirty state indicators ("*") keep users informed of unsaved changes.
- **Graceful Exit:** Safe close workflow prompts for save confirmation, preventing accidental data loss.
- **Productivity:** Recent files history enables rapid access to frequently edited documents.
- **Professional Experience:** These features are expected in every modern text editor and IDE.

### 1.3 Dependencies on Previous Versions

| Component | Source | Usage |
|:----------|:-------|:------|
| DocumentViewModel | v0.1.1d | Base class for editor documents with IsDirty property |
| Editor Module | v0.1.3 | Text editing surface to track changes |
| Database | v0.0.5 | Persist recent files history via repository |
| Event Bus | v0.0.7 | Publish DocumentSavedEvent, DocumentClosedEvent |
| IConfigurationService | v0.0.3d | Retrieve application data paths |
| Serilog | v0.0.3b | Log all file operations |

---

## 2. Sub-Part Specifications

### 2.1 v0.1.4a: Dirty State Tracking

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-014a |
| **Title** | Dirty State Tracking |
| **Module** | `Lexichord.Modules.Editor` |
| **License Tier** | Core |

**Goal:** Implement visual "star" indicator (filename.md*) in tab titles when document has unsaved changes, binding TextChanged events to DocumentViewModel.IsDirty property.

**Key Deliverables:**
- Bind editor TextChanged event to DocumentViewModel.IsDirty
- Update tab title to show "*" suffix when dirty
- Clear dirty state after successful save
- Publish DocumentDirtyChangedEvent via event bus

**Key Interfaces:**
```csharp
public interface IDirtyStateTracker
{
    bool IsDirty { get; }
    void MarkDirty();
    void ClearDirty();
    event EventHandler<DirtyStateChangedEventArgs> DirtyStateChanged;
}
```

**Dependencies:**
- v0.1.1d: DocumentViewModel (provides IsDirty property)
- v0.1.3: Editor Module (provides TextChanged event source)

---

### 2.2 v0.1.4b: Atomic Saves

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-014b |
| **Title** | Atomic Saves |
| **Module** | `Lexichord.Modules.Editor` |
| **License Tier** | Core |

**Goal:** Implement FileService.SaveAsync() with atomic write strategy (Write Temp -> Delete Original -> Rename) to prevent file corruption during save operations.

**Key Deliverables:**
- Define IFileService interface in Abstractions
- Implement atomic save with temp file strategy
- Handle edge cases (read-only, permissions, disk full)
- Publish DocumentSavedEvent on successful save

**Key Interfaces:**
```csharp
public interface IFileService
{
    Task<SaveResult> SaveAsync(string filePath, string content, CancellationToken ct = default);
    Task<SaveResult> SaveAsAsync(string filePath, string content, CancellationToken ct = default);
    Task<LoadResult> LoadAsync(string filePath, CancellationToken ct = default);
    bool CanWrite(string filePath);
}
```

**Atomic Save Strategy:**
1. Write content to `{filename}.tmp`
2. Delete original `{filename}` (if exists)
3. Rename `{filename}.tmp` to `{filename}`
4. If any step fails, preserve original file

**Dependencies:**
- v0.1.4a: Dirty State Tracking (clear dirty flag on save)
- v0.0.7: Event Bus (publish save events)

---

### 2.3 v0.1.4c: Safe Close Workflow

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-014c |
| **Title** | Safe Close Workflow |
| **Module** | `Lexichord.Host` |
| **License Tier** | Core |

**Goal:** Implement IShutdownService that intercepts window close (X button) and shows "Save Changes?" confirmation dialog when dirty documents exist.

**Key Deliverables:**
- Define IShutdownService interface
- Intercept MainWindow.Closing event
- Enumerate all dirty documents via IDocumentManager
- Display confirmation dialog with Save/Discard/Cancel options
- Handle batch save operations for multiple dirty documents

**Key Interfaces:**
```csharp
public interface IShutdownService
{
    Task<bool> RequestShutdownAsync();
    void RegisterDocument(DocumentViewModel document);
    void UnregisterDocument(DocumentViewModel document);
    IReadOnlyList<DocumentViewModel> GetDirtyDocuments();
}
```

**Dialog Options:**
- **Save All:** Save all dirty documents, then close
- **Discard All:** Close without saving
- **Cancel:** Abort close operation

**Dependencies:**
- v0.1.4a: Dirty State Tracking (identify dirty documents)
- v0.1.4b: Atomic Saves (save dirty documents)
- v0.1.1d: DocumentViewModel (dirty state property)

---

### 2.4 v0.1.4d: Recent Files History

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-014d |
| **Title** | Recent Files History |
| **Module** | `Lexichord.Host`, `Lexichord.Modules.Editor` |
| **License Tier** | Core |

**Goal:** Implement MRU (Most Recently Used) list persisted to database, populate File > Open Recent submenu on application startup.

**Key Deliverables:**
- Define RecentFile entity and repository
- Create Migration_XXX_RecentFiles for database schema
- Implement IRecentFilesService for MRU management
- Populate "File > Open Recent" menu dynamically
- Limit history to configurable maximum (default: 10)
- Handle missing files gracefully (gray out or remove)

**Key Interfaces:**
```csharp
public interface IRecentFilesService
{
    Task<IReadOnlyList<RecentFileEntry>> GetRecentFilesAsync(int maxCount = 10);
    Task AddRecentFileAsync(string filePath);
    Task RemoveRecentFileAsync(string filePath);
    Task ClearHistoryAsync();
    event EventHandler<RecentFilesChangedEventArgs> RecentFilesChanged;
}
```

**Storage Schema:**
```sql
CREATE TABLE "RecentFiles" (
    "Id" UUID PRIMARY KEY,
    "FilePath" VARCHAR(1024) NOT NULL,
    "FileName" VARCHAR(255) NOT NULL,
    "LastOpenedAt" TIMESTAMPTZ NOT NULL,
    "OpenCount" INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT "UQ_RecentFiles_FilePath" UNIQUE ("FilePath")
);

CREATE INDEX "IX_RecentFiles_LastOpenedAt" ON "RecentFiles" ("LastOpenedAt" DESC);
```

**Dependencies:**
- v0.0.5: Database (repository pattern)
- v0.0.5c: FluentMigrator (schema creation)
- v0.0.7: Event Bus (publish FileOpenedEvent)

---

## 3. Implementation Checklist

| # | Sub-Part | Task | Est. Hours |
|:--|:---------|:-----|:-----------|
| 1 | v0.1.4a | Define IDirtyStateTracker interface | 0.5 |
| 2 | v0.1.4a | Implement dirty state in EditorDocumentViewModel | 2 |
| 3 | v0.1.4a | Bind TextChanged to IsDirty property | 1 |
| 4 | v0.1.4a | Update tab title with "*" indicator | 1 |
| 5 | v0.1.4a | Unit tests for dirty state tracking | 2 |
| 6 | v0.1.4b | Define IFileService interface | 1 |
| 7 | v0.1.4b | Implement atomic save strategy | 4 |
| 8 | v0.1.4b | Handle edge cases (permissions, disk full) | 2 |
| 9 | v0.1.4b | Implement SaveAs with file dialog | 2 |
| 10 | v0.1.4b | Unit tests for file service | 3 |
| 11 | v0.1.4c | Define IShutdownService interface | 0.5 |
| 12 | v0.1.4c | Implement ShutdownService | 3 |
| 13 | v0.1.4c | Create SaveChangesDialog view | 2 |
| 14 | v0.1.4c | Wire up MainWindow.Closing interception | 1 |
| 15 | v0.1.4c | Unit tests for shutdown service | 2 |
| 16 | v0.1.4d | Create Migration for RecentFiles table | 1 |
| 17 | v0.1.4d | Define IRecentFilesService interface | 0.5 |
| 18 | v0.1.4d | Implement RecentFilesService | 3 |
| 19 | v0.1.4d | Create RecentFilesRepository | 2 |
| 20 | v0.1.4d | Wire up File > Open Recent menu | 2 |
| 21 | v0.1.4d | Handle missing files in menu | 1 |
| 22 | v0.1.4d | Unit tests for recent files service | 2 |
| **Total** | | | **38 hours** |

---

## 4. Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|:-----|:-------|:------------|:-----------|
| Atomic save fails mid-operation | High | Low | Implement rollback; preserve .tmp file on failure |
| User closes app while save in progress | High | Low | Block close until save completes; show progress |
| Database unavailable for recent files | Medium | Low | Graceful degradation; empty menu; log warning |
| File path too long for database | Medium | Low | Validate path length; truncate display name |
| Concurrent access to same file | Medium | Medium | File locking; detect external changes |
| TextChanged fires too frequently | Low | High | Debounce dirty state updates |

---

## 5. Success Metrics

| Metric | Target | Measurement |
|:-------|:-------|:------------|
| Save operation reliability | 99.99% | No data loss in save operations |
| Atomic save completion | < 100ms | Time from save initiation to completion |
| Dirty state response | < 16ms | Time from keystroke to "*" appearing |
| Recent files load | < 50ms | Time to populate menu on startup |
| Close dialog response | Immediate | No perceptible delay showing dialog |

---

## 6. What This Enables

After v0.1.4, Lexichord will support:
- Complete document editing workflow with save/load operations
- Visual feedback for unsaved changes
- Protection against accidental data loss on close
- Quick access to recently opened files
- Foundation for v0.1.5's Auto-Save functionality
- Foundation for v0.1.6's File Watching (external changes)
