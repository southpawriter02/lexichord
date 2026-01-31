# LCS-CL-015c — File Jumper Service

**Lexichord Version**: v0.1.5c (File Jumper)  
**Spec Ref**: [LCS-DES-015c](../../specs/v0.1.x/v0.1.5/LCS-DES-015c.md)  
**Date**: 2026-01-29

---

## Summary

Implements the File Indexing Service for workspace file discovery and fuzzy search. Provides the backend infrastructure for the Command Palette's file navigation functionality.

---

## Added

### Abstractions Layer (`Lexichord.Abstractions`)

| File                              | Description                                                               |
| --------------------------------- | ------------------------------------------------------------------------- |
| `Contracts/IFileIndexService.cs`  | Core interface for file indexing, search, and recent files tracking       |
| `Contracts/FileIndexEntry.cs`     | Immutable record for indexed file metadata with computed properties       |
| `Contracts/FileIndexSettings.cs`  | Configuration record with ignore patterns, size limits, binary extensions |
| `Contracts/FileIndexAction.cs`    | Enum for file change types (Created, Modified, Deleted)                   |
| `Contracts/FileIndexEventArgs.cs` | Event args for index changes and progress reporting                       |
| `Events/FileIndexRebuiltEvent.cs` | MediatR notification published on index rebuild completion                |

### Host Implementation (`Lexichord.Host`)

| File                                 | Description                                                |
| ------------------------------------ | ---------------------------------------------------------- |
| `Services/IgnorePatternMatcher.cs`   | Glob-to-regex converter for file path filtering            |
| `Services/FileIndexService.cs`       | Singleton implementation with ConcurrentDictionary storage |
| `Services/FileIndexEventHandlers.cs` | MediatR handlers for workspace and file system events      |

### Features

- **Fuzzy Search**: FuzzySharp-powered search with 40+ score threshold
- **Recent Files**: LRU tracking via LinkedList + HashSet for O(1) access
- **Incremental Updates**: Real-time index updates from file watcher events
- **Ignore Patterns**: Configurable glob patterns (`.git/**`, `node_modules/**`, etc.)
- **Binary Filtering**: Excludes binary extensions (.exe, .dll, .pdf, etc.)
- **Icon Mapping**: Material Design icons for 20+ file extensions

---

## Modified

| File              | Change                                                                         |
| ----------------- | ------------------------------------------------------------------------------ |
| `HostServices.cs` | Registered `IFileIndexService` singleton and `FileIndexSettings` configuration |

---

## Configuration

New configuration section in `appsettings.json`:

```json
{
    "FileIndex": {
        "IgnorePatterns": [".git/**", "node_modules/**", "bin/**", "obj/**"],
        "IncludeHiddenFiles": false,
        "MaxFileSizeBytes": 52428800,
        "MaxRecentFiles": 50,
        "FileWatcherDebounceMs": 300
    }
}
```

---

## Testing

### Unit Tests Added

| Test Class                  | Tests | Coverage                                            |
| --------------------------- | ----- | --------------------------------------------------- |
| `FileIndexEntryTests`       | 15    | Extension, IconKind, DirectoryName, FileSizeDisplay |
| `FileIndexSettingsTests`    | 10    | Default values, SectionName, overrides              |
| `IgnorePatternMatcherTests` | 15    | Glob patterns, wildcards, edge cases                |
| `FileIndexServiceTests`     | 28    | Indexing, search, updates, recent files, events     |

### Verification Commands

```bash
# Build solution
dotnet build

# Run FileIndex tests
dotnet test --filter "FullyQualifiedName~FileIndex"
```

---

## Dependencies

- **FuzzySharp v2.0.2** — Fuzzy string matching (already in Host.csproj)
- **MediatR** — Event publishing for FileIndexRebuiltEvent

---

## Technical Notes

### Thread Safety

- `ConcurrentDictionary<string, FileIndexEntry>` for index storage
- `lock` object for recent files list manipulation

### Performance

- Background indexing via `Task.Run` with cancellation support
- Progress reporting every 100 files
- Enumeration with `IgnoreInaccessible = true`

### Event Flow

```
WorkspaceOpenedEvent → FileIndexWorkspaceOpenedHandler → RebuildIndexAsync
WorkspaceClosedEvent → FileIndexWorkspaceClosedHandler → Clear()
ExternalFileChangesEvent → FileIndexExternalChangesHandler → UpdateFile/UpdateFileRenamed
```
