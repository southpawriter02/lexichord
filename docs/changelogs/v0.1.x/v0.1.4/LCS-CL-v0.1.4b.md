# LCS-CL-014b: Atomic Saves

**Version**: v0.1.4b  
**Released**: 2026-01-29  
**Status**: ✅ Complete

---

## Overview

Implements atomic file saves using Write-Temp-Delete-Rename strategy to prevent data corruption. File operations are crash-safe through three-phase execution.

---

## Changes

### New Files

| File                                            | Purpose                                                   |
| :---------------------------------------------- | :-------------------------------------------------------- |
| `Abstractions/Contracts/Editor/IFileService.cs` | Interface and supporting types for atomic file operations |
| `Abstractions/Events/DocumentSaveEvents.cs`     | MediatR events for save success/failure notifications     |
| `Modules.Editor/Services/FileService.cs`        | Implementation with three-phase atomic write strategy     |
| `Tests.Unit/Modules/Editor/FileServiceTests.cs` | 26 unit tests covering atomic save and load operations    |

### Modified Files

| File                             | Changes                                          |
| :------------------------------- | :----------------------------------------------- |
| `Modules.Editor/EditorModule.cs` | Registers `IFileService`, bumps version to 0.1.4 |

---

## Technical Details

### Atomic Write Strategy

Three-phase save operation:

1. **Phase 1 (Temp Write)**: Write content to `{file}.tmp` with `FileOptions.WriteThrough`
2. **Phase 2 (Delete Original)**: Delete existing file (if any) after read-only check
3. **Phase 3 (Rename)**: Move temp file to original path

### Error Recovery

If Phase 3 fails (rename):

1. Retry up to 3 times with 100/200/300ms delays
2. Fall back to Copy+Delete as alternative

Original file is preserved on any Phase 1 or Phase 2 failure.

### Data Contracts

```csharp
// Result types
record SaveResult(bool Success, string FilePath, long BytesWritten, TimeSpan Duration, SaveError? Error);
record LoadResult(bool Success, string FilePath, string? Content, Encoding? Encoding, LoadError? Error);

// Error types with recovery hints
record SaveError(SaveErrorCode Code, string Message, Exception? Exception, string? RecoveryHint);
enum SaveErrorCode { Unknown, FileInUse, ReadOnly, DiskFull, TempWriteFailed, RenameFailed, ... }
```

### Domain Events

| Event                     | Published When              | Payload                              |
| :------------------------ | :-------------------------- | :----------------------------------- |
| `DocumentSavedEvent`      | Save completes successfully | Path, bytes, duration, timestamp     |
| `DocumentSaveFailedEvent` | Save fails                  | Path, error code, message, timestamp |

---

## Test Coverage

| Test                                               | Description                        |
| :------------------------------------------------- | :--------------------------------- |
| `SaveAsync_NewFile_CreatesFileAndReturnsSuccess`   | Creates new file with atomic write |
| `SaveAsync_ExistingFile_OverwritesFileAtomically`  | Overwrites without corruption risk |
| `SaveAsync_CorrectBytesWritten_ReturnsProperCount` | Accurate byte count tracking       |
| `SaveAsync_NoTempFileRemains_CleansUpAfterSuccess` | Temp file cleanup verification     |
| `SaveAsync_EmptyPath_ReturnsSaveError`             | Empty path validation              |
| `SaveAsync_DirectoryNotFound_ReturnsSaveError`     | Missing directory handling         |
| `SaveAsync_ReadOnlyFile_ReturnsSaveError`          | Read-only file protection          |
| `SaveAsync_Cancelled_ReturnsSaveError`             | Cancellation token support         |
| `SaveAsync_Success_PublishesDocumentSavedEvent`    | Event publishing on success        |
| `SaveAsync_Utf8Content_PreservesSpecialCharacters` | Unicode character preservation     |
| `LoadAsync_ExistingFile_ReturnsContent`            | Content loading                    |
| `LoadAsync_NonexistentFile_ReturnsError`           | Missing file error handling        |
| `CanWrite_ReadOnlyFile_ReturnsFalse`               | Write permission check             |
| `GetMetadata_ExistingFile_ReturnsMetadata`         | File metadata retrieval            |

---

## Dependencies

- v0.1.3 (Editor Module) — Provides editor infrastructure
- v0.0.7 (Event Bus) — Provides MediatR for event publishing
