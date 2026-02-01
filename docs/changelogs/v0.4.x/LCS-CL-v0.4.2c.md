# Changelog: v0.4.2c - File Watcher Integration

**Release Date:** 2026-02-01
**Status:** Implemented
**Specification:** [LCS-DES-v0.4.2c](../../specs/v0.4.x/v0.4.2/LCS-DES-v0.4.2c.md)

---

## Summary

Integrates the file watcher subsystem with the RAG ingestion pipeline. When files matching configured extensions are created, modified, or renamed in the workspace, the `FileWatcherIngestionHandler` publishes `FileIndexingRequestedEvent` notifications for downstream processing. Features configurable extension filtering, directory exclusion, and per-file debouncing.

---

## Changes

### Added

#### Lexichord.Abstractions/Contracts/Ingestion/

| File                            | Description                                                         |
| :------------------------------ | :------------------------------------------------------------------ |
| `FileIndexingRequestedEvent.cs` | MediatR notification record for indexing requests                   |
| `FileIndexingChangeType.cs`     | Enum defining Created, Changed, Renamed change types (in same file) |
| `FileWatcherOptions.cs`         | Configuration record with extension/directory filtering             |

#### Lexichord.Modules.RAG/Services/

| File                             | Description                                                    |
| :------------------------------- | :------------------------------------------------------------- |
| `FileWatcherIngestionHandler.cs` | INotificationHandler that bridges file watcher to RAG pipeline |

### Modified

#### Lexichord.Modules.RAG/

| File                           | Description                                                       |
| :----------------------------- | :---------------------------------------------------------------- |
| `RAGModule.cs`                 | Added DI registration for handler and options                     |
| `Lexichord.Modules.RAG.csproj` | Added MediatR and Microsoft.Extensions.Options package references |

### Unit Tests

#### Lexichord.Tests.Unit/Abstractions/Ingestion/

| File                                 | Tests                                         |
| :----------------------------------- | :-------------------------------------------- |
| `FileIndexingRequestedEventTests.cs` | Factory methods, enum values, record equality |
| `FileWatcherOptionsTests.cs`         | Defaults, filtering methods, path handling    |

#### Lexichord.Tests.Unit/Modules/RAG/

| File                                  | Tests                                             |
| :------------------------------------ | :------------------------------------------------ |
| `FileWatcherIngestionHandlerTests.cs` | Filtering, debouncing, event publishing, disposal |

---

## Technical Details

### Event Architecture

```
WorkspaceService                 FileWatcherIngestionHandler            Ingestion Pipeline
     │                                     │                                  │
     │ IFileSystemWatcher.ChangesDetected  │                                  │
     ├────────────────────────────────────>│                                  │
     │                                     │ Filter & Debounce                │
     │                                     ├──────────────────────>           │
     │                                     │ FileIndexingRequestedEvent       │
     │                                     ├─────────────────────────────────>│
```

### Filtering Logic

1. **Extension check**: File must have supported extension (`.md`, `.txt`, `.json`, `.yaml`)
2. **Directory check**: Path must not contain excluded directories (`.git`, `node_modules`, etc.)
3. **Change type check**: Deleted files are skipped (handled by removal flow)
4. **Directory check**: Directory changes are skipped (only files indexed)

### Debouncing

- Uses `ConcurrentDictionary` for thread-safe pending change tracking
- `System.Threading.Timer` per file with configurable delay (default 300ms)
- Rapid successive changes coalesce to single event with latest change type
- Timers properly disposed on service disposal

### FileWatcherOptions Defaults

| Property              | Default Value                                        |
| :-------------------- | :--------------------------------------------------- |
| `Enabled`             | `true`                                               |
| `SupportedExtensions` | `.md`, `.txt`, `.json`, `.yaml`                      |
| `ExcludedDirectories` | `.git`, `node_modules`, `bin`, `obj`, `.vs`, `.idea` |
| `DebounceDelayMs`     | `300`                                                |

---

## Verification

```bash
# Build RAG module
dotnet build src/Lexichord.Modules.RAG

# Run v0.4.2c tests
dotnet test tests/Lexichord.Tests.Unit --filter "Feature=v0.4.2c"
# Result: 28 tests passed

# Run all new test files
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~FileIndexingRequestedEventTests|FullyQualifiedName~FileWatcherOptionsTests|FullyQualifiedName~FileWatcherIngestionHandlerTests"
# Result: 61 tests passed
```

---

## Dependencies

| Dependency                     | Version | Purpose                                   |
| :----------------------------- | :------ | :---------------------------------------- |
| `MediatR`                      | 12.4.0  | Event publishing and handler registration |
| `Microsoft.Extensions.Options` | 9.0.0   | IOptions pattern for configuration        |
| `IFileSystemWatcher`           | v0.1.2b | Source of file change events              |
| `ExternalFileChangesEvent`     | v0.1.2b | Input event from WorkspaceService         |

---

## Related Documents

- [LCS-DES-v0.4.2c](../../specs/v0.4.x/v0.4.2/LCS-DES-v0.4.2c.md) - Design specification
- [LCS-SBD-v0.4.2 §3.3](../../specs/v0.4.x/v0.4.2/LCS-SBD-v0.4.2.md#33-v042c-file-watcher-integration) - Scope breakdown
- [LCS-CL-v0.4.2b](./LCS-CL-v0.4.2b.md) - Previous sub-part (Hash-Based Change Detection)
