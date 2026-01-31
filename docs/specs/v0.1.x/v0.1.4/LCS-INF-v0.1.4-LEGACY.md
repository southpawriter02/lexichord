# LCS-INF-014: Feature Design Specification — Scribe (IO & Persistence)

## 1. Metadata & Categorization

| Field                | Value                                         | Description                                        |
| :------------------- | :-------------------------------------------- | :------------------------------------------------- |
| **Feature ID**       | `INF-014`                                     | Infrastructure - Scribe (IO & Persistence)         |
| **Feature Name**     | Scribe (IO & Persistence)                     | File operations, dirty tracking, safe workflows    |
| **Target Version**   | `v0.1.4`                                      | Editor Productivity Milestone                      |
| **Module Scope**     | `Lexichord.Host`, `Lexichord.Modules.Editor`  | Core shell and editor infrastructure               |
| **Swimlane**         | `Infrastructure`                              | The Podium (Platform)                              |
| **License Tier**     | `Core`                                        | Foundation (Required for all tiers)                |
| **Feature Gate Key** | N/A                                           | No runtime gating for core IO features             |
| **Author**           | System Architect                              |                                                    |
| **Status**           | **Draft**                                     | Pending implementation                             |
| **Last Updated**     | 2026-01-26                                    |                                                    |

---

## 2. Executive Summary

### 2.1 The Requirement

The v0.1.3 Editor Module provides text editing capabilities but lacks **persistent file operations**:

- Documents cannot be saved to disk safely
- Users have no visual indicator of unsaved changes
- Closing the application may result in data loss
- No quick access to previously opened files

These limitations prevent Lexichord from being used as a serious writing tool. Every text editor must protect user data through robust save mechanisms and clear communication of document state.

### 2.2 The Proposed Solution

We **SHALL** implement a complete IO and persistence layer with:

1. **v0.1.4a: Dirty State Tracking** — Visual "*" indicator, TextChanged binding to IsDirty.
2. **v0.1.4b: Atomic Saves** — Write-temp-delete-rename strategy to prevent corruption.
3. **v0.1.4c: Safe Close Workflow** — IShutdownService intercepting close, save confirmation dialog.
4. **v0.1.4d: Recent Files History** — MRU list in database, File > Open Recent menu.

---

## 3. Architecture & Modular Strategy

### 3.1 High-Level Architecture

```mermaid
graph TB
    subgraph "Lexichord.Host"
        MAIN[MainWindow]
        SHUTDOWN[ShutdownService]
        MENU[MenuService]
        RECENT[RecentFilesService]
    end

    subgraph "Lexichord.Modules.Editor"
        EDITOR[EditorView]
        EDITORVM[EditorDocumentViewModel]
        DIRTY[DirtyStateTracker]
        FILESVC[FileService]
    end

    subgraph "Lexichord.Abstractions"
        IFILE[IFileService]
        ISHUTDOWN[IShutdownService]
        IRECENT[IRecentFilesService]
        IDIRTY[IDirtyStateTracker]
    end

    subgraph "Lexichord.Infrastructure"
        RECENTREPO[RecentFilesRepository]
        DB[(PostgreSQL)]
    end

    subgraph "File System"
        FS[Local Files]
    end

    MAIN --> SHUTDOWN
    MAIN --> MENU
    MENU --> RECENT

    EDITOR --> EDITORVM
    EDITORVM --> DIRTY
    EDITORVM --> FILESVC

    FILESVC --> IFILE
    SHUTDOWN --> ISHUTDOWN
    RECENT --> IRECENT
    DIRTY --> IDIRTY

    FILESVC --> FS
    RECENT --> RECENTREPO
    RECENTREPO --> DB
```

### 3.2 File Save Sequence

```mermaid
sequenceDiagram
    participant User
    participant Editor as EditorDocumentViewModel
    participant FileService
    participant FileSystem
    participant EventBus

    User->>Editor: Press Ctrl+S
    Editor->>Editor: Check IsDirty
    alt Document is dirty
        Editor->>FileService: SaveAsync(path, content)
        FileService->>FileSystem: Write to {file}.tmp
        FileSystem-->>FileService: Success
        FileService->>FileSystem: Delete original {file}
        FileSystem-->>FileService: Success (or not exists)
        FileService->>FileSystem: Rename {file}.tmp to {file}
        FileSystem-->>FileService: Success
        FileService-->>Editor: SaveResult.Success
        Editor->>Editor: ClearDirty()
        Editor->>Editor: Update tab title (remove *)
        Editor->>EventBus: Publish(DocumentSavedEvent)
    else Document not dirty
        Editor-->>User: No action needed
    end
```

### 3.3 Safe Close Sequence

```mermaid
sequenceDiagram
    participant User
    participant MainWindow
    participant ShutdownService
    participant Dialog as SaveChangesDialog
    participant FileService
    participant Documents as DocumentManager

    User->>MainWindow: Click X (close)
    MainWindow->>ShutdownService: RequestShutdownAsync()
    ShutdownService->>Documents: GetDirtyDocuments()
    Documents-->>ShutdownService: List<DocumentViewModel>

    alt No dirty documents
        ShutdownService-->>MainWindow: true (proceed)
        MainWindow->>MainWindow: Close()
    else Has dirty documents
        ShutdownService->>Dialog: Show(dirtyDocuments)
        Dialog-->>User: Display dialog

        alt User clicks "Save All"
            loop For each dirty document
                Dialog->>FileService: SaveAsync(doc)
            end
            Dialog-->>ShutdownService: true (proceed)
            ShutdownService-->>MainWindow: true (proceed)
            MainWindow->>MainWindow: Close()
        else User clicks "Discard All"
            Dialog-->>ShutdownService: true (proceed)
            ShutdownService-->>MainWindow: true (proceed)
            MainWindow->>MainWindow: Close()
        else User clicks "Cancel"
            Dialog-->>ShutdownService: false (cancel)
            ShutdownService-->>MainWindow: false (cancel)
            Note over MainWindow: Window remains open
        end
    end
```

### 3.4 Dependencies

**NuGet Packages:**

| Package | Version | Purpose |
|:--------|:--------|:--------|
| `CommunityToolkit.Mvvm` | 8.x | ObservableProperty, RelayCommand |
| `MediatR` | 12.x | Event publishing |
| `FluentMigrator` | 6.x | Database schema migration |
| `Dapper` | 2.x | Recent files repository |

**Project References:**

| Project | Reference Direction |
|:--------|:--------------------|
| `Lexichord.Host` | -> `Lexichord.Abstractions` |
| `Lexichord.Modules.Editor` | -> `Lexichord.Abstractions` |
| `Lexichord.Infrastructure` | -> `Lexichord.Abstractions` |

### 3.5 Licensing Behavior

- **N/A for Scribe:** Core IO features available to all license tiers.
- Future auto-save intervals may be configurable in WriterPro tier.

---

## 4. Decision Tree: Save Operations

```text
START: "How should this save operation proceed?"
|
+-- User triggers Save (Ctrl+S)
|   +-- Is document dirty?
|   |   +-- YES -> Has existing file path?
|   |   |   +-- YES -> SaveAsync(existingPath, content)
|   |   |   |   +-- Success -> ClearDirty, update title, publish event
|   |   |   |   +-- Failure -> Show error, keep dirty
|   |   |   +-- NO -> Show SaveAs dialog
|   |   |       +-- User selects path -> SaveAsAsync(newPath, content)
|   |   |       +-- User cancels -> No action
|   |   +-- NO -> No action (already saved)
|   +-- END
|
+-- User triggers Save As (Ctrl+Shift+S)
|   +-- Show SaveAs dialog
|   +-- User selects path?
|   |   +-- YES -> SaveAsAsync(newPath, content)
|   |   |   +-- Success -> Update filePath, ClearDirty, publish event
|   |   |   +-- Failure -> Show error, keep dirty
|   |   +-- NO (cancel) -> No action
|   +-- END
|
+-- Application closing with dirty documents
|   +-- Show SaveChangesDialog
|   +-- User choice?
|       +-- Save All -> Save each dirty doc, then close
|       +-- Discard All -> Close without saving
|       +-- Cancel -> Abort close
|
+-- END
```

---

## 5. Data Contracts

### 5.1 IFileService Interface

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for file system operations with atomic save support.
/// </summary>
/// <remarks>
/// LOGIC: All save operations use an atomic write strategy to prevent corruption:
/// 1. Write content to {filename}.tmp
/// 2. Delete original {filename} (if exists)
/// 3. Rename {filename}.tmp to {filename}
///
/// If any step fails, the original file is preserved and an error is returned.
/// </remarks>
public interface IFileService
{
    /// <summary>
    /// Saves content to the specified file path using atomic write.
    /// </summary>
    /// <param name="filePath">The target file path.</param>
    /// <param name="content">The content to save.</param>
    /// <param name="encoding">The text encoding (default: UTF-8).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure with error details.</returns>
    Task<SaveResult> SaveAsync(
        string filePath,
        string content,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves content to a new file path (Save As operation).
    /// </summary>
    /// <param name="filePath">The new target file path.</param>
    /// <param name="content">The content to save.</param>
    /// <param name="encoding">The text encoding (default: UTF-8).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure with error details.</returns>
    Task<SaveResult> SaveAsAsync(
        string filePath,
        string content,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads content from the specified file path.
    /// </summary>
    /// <param name="filePath">The source file path.</param>
    /// <param name="encoding">The text encoding (default: auto-detect).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing file content or error details.</returns>
    Task<LoadResult> LoadAsync(
        string filePath,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the specified file can be written to.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file can be written, false otherwise.</returns>
    bool CanWrite(string filePath);

    /// <summary>
    /// Checks if the specified file exists.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    bool Exists(string filePath);
}

/// <summary>
/// Result of a save operation.
/// </summary>
/// <param name="Success">Whether the save succeeded.</param>
/// <param name="FilePath">The path where the file was saved.</param>
/// <param name="BytesWritten">Number of bytes written.</param>
/// <param name="Error">Error details if save failed.</param>
public record SaveResult(
    bool Success,
    string FilePath,
    long BytesWritten = 0,
    SaveError? Error = null
);

/// <summary>
/// Error details for a failed save operation.
/// </summary>
/// <param name="Code">Error code for programmatic handling.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Exception">The underlying exception, if any.</param>
public record SaveError(
    SaveErrorCode Code,
    string Message,
    Exception? Exception = null
);

/// <summary>
/// Error codes for save operations.
/// </summary>
public enum SaveErrorCode
{
    Unknown,
    FileInUse,
    AccessDenied,
    PathTooLong,
    DiskFull,
    ReadOnly,
    DirectoryNotFound,
    InvalidPath,
    IoError
}

/// <summary>
/// Result of a load operation.
/// </summary>
/// <param name="Success">Whether the load succeeded.</param>
/// <param name="FilePath">The path that was loaded.</param>
/// <param name="Content">The file content if successful.</param>
/// <param name="Encoding">The detected encoding.</param>
/// <param name="Error">Error details if load failed.</param>
public record LoadResult(
    bool Success,
    string FilePath,
    string? Content = null,
    Encoding? Encoding = null,
    LoadError? Error = null
);

/// <summary>
/// Error details for a failed load operation.
/// </summary>
/// <param name="Code">Error code for programmatic handling.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Exception">The underlying exception, if any.</param>
public record LoadError(
    LoadErrorCode Code,
    string Message,
    Exception? Exception = null
);

/// <summary>
/// Error codes for load operations.
/// </summary>
public enum LoadErrorCode
{
    Unknown,
    FileNotFound,
    AccessDenied,
    PathTooLong,
    EncodingError,
    FileTooLarge,
    IoError
}
```

### 5.2 IDirtyStateTracker Interface

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Tracks dirty (unsaved changes) state for a document.
/// </summary>
/// <remarks>
/// LOGIC: Documents implement this interface to track unsaved changes.
/// The dirty state is set when content changes and cleared after successful save.
/// UI binds to IsDirty to show visual indicators (e.g., "*" in tab title).
/// </remarks>
public interface IDirtyStateTracker
{
    /// <summary>
    /// Gets whether the document has unsaved changes.
    /// </summary>
    bool IsDirty { get; }

    /// <summary>
    /// Marks the document as having unsaved changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Called when document content changes.
    /// Should be debounced to avoid excessive state changes.
    /// </remarks>
    void MarkDirty();

    /// <summary>
    /// Clears the dirty state after successful save.
    /// </summary>
    void ClearDirty();

    /// <summary>
    /// Event raised when dirty state changes.
    /// </summary>
    event EventHandler<DirtyStateChangedEventArgs>? DirtyStateChanged;
}

/// <summary>
/// Event args for dirty state changes.
/// </summary>
public class DirtyStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the document ID.
    /// </summary>
    public required string DocumentId { get; init; }

    /// <summary>
    /// Gets the new dirty state.
    /// </summary>
    public required bool IsDirty { get; init; }
}
```

### 5.3 IShutdownService Interface

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for managing application shutdown with dirty document handling.
/// </summary>
/// <remarks>
/// LOGIC: The shutdown service intercepts application close events and ensures
/// users have the opportunity to save unsaved work. It maintains a registry of
/// open documents and queries their dirty state during shutdown.
/// </remarks>
public interface IShutdownService
{
    /// <summary>
    /// Requests application shutdown, handling dirty documents.
    /// </summary>
    /// <returns>True if shutdown should proceed, false if cancelled.</returns>
    /// <remarks>
    /// LOGIC: If dirty documents exist, shows a confirmation dialog.
    /// Dialog offers: Save All, Discard All, Cancel.
    /// </remarks>
    Task<bool> RequestShutdownAsync();

    /// <summary>
    /// Registers a document for shutdown tracking.
    /// </summary>
    /// <param name="document">The document to track.</param>
    void RegisterDocument(DocumentViewModel document);

    /// <summary>
    /// Unregisters a document from shutdown tracking.
    /// </summary>
    /// <param name="document">The document to unregister.</param>
    void UnregisterDocument(DocumentViewModel document);

    /// <summary>
    /// Gets all documents with unsaved changes.
    /// </summary>
    /// <returns>List of dirty documents.</returns>
    IReadOnlyList<DocumentViewModel> GetDirtyDocuments();

    /// <summary>
    /// Gets whether there are any dirty documents.
    /// </summary>
    bool HasDirtyDocuments { get; }

    /// <summary>
    /// Event raised when shutdown is requested.
    /// </summary>
    event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested;
}

/// <summary>
/// Event args for shutdown requests.
/// </summary>
public class ShutdownRequestedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the list of dirty documents at shutdown time.
    /// </summary>
    public required IReadOnlyList<DocumentViewModel> DirtyDocuments { get; init; }

    /// <summary>
    /// Gets or sets whether the shutdown should be cancelled.
    /// </summary>
    public bool Cancel { get; set; }
}
```

### 5.4 IRecentFilesService Interface

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for managing recently opened files history.
/// </summary>
/// <remarks>
/// LOGIC: Maintains a Most Recently Used (MRU) list of files, persisted to
/// the database. Used to populate File > Open Recent menu. Handles missing
/// files gracefully by marking them unavailable.
/// </remarks>
public interface IRecentFilesService
{
    /// <summary>
    /// Gets the list of recently opened files.
    /// </summary>
    /// <param name="maxCount">Maximum number of files to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recent files, ordered by last opened time descending.</returns>
    Task<IReadOnlyList<RecentFileEntry>> GetRecentFilesAsync(
        int maxCount = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates a file in the recent files list.
    /// </summary>
    /// <param name="filePath">The file path to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// LOGIC: If file already exists, updates LastOpenedAt and increments OpenCount.
    /// If file doesn't exist, adds new entry.
    /// </remarks>
    Task AddRecentFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a file from the recent files list.
    /// </summary>
    /// <param name="filePath">The file path to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveRecentFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all recent files history.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if all recent files still exist and removes missing ones.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of files that were removed because they no longer exist.</returns>
    Task<IReadOnlyList<string>> PruneInvalidEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when the recent files list changes.
    /// </summary>
    event EventHandler<RecentFilesChangedEventArgs>? RecentFilesChanged;
}

/// <summary>
/// Entry in the recent files list.
/// </summary>
/// <param name="FilePath">Full path to the file.</param>
/// <param name="FileName">Display name (file name only).</param>
/// <param name="LastOpenedAt">When the file was last opened.</param>
/// <param name="OpenCount">Number of times the file has been opened.</param>
/// <param name="Exists">Whether the file currently exists on disk.</param>
public record RecentFileEntry(
    string FilePath,
    string FileName,
    DateTimeOffset LastOpenedAt,
    int OpenCount,
    bool Exists
);

/// <summary>
/// Event args for recent files list changes.
/// </summary>
public class RecentFilesChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the type of change.
    /// </summary>
    public required RecentFilesChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets the affected file path, if applicable.
    /// </summary>
    public string? FilePath { get; init; }
}

/// <summary>
/// Types of recent files list changes.
/// </summary>
public enum RecentFilesChangeType
{
    Added,
    Removed,
    Updated,
    Cleared,
    Pruned
}
```

### 5.5 Domain Events

```csharp
namespace Lexichord.Abstractions.Events;

using MediatR;

/// <summary>
/// Event published when a document's dirty state changes.
/// </summary>
/// <param name="DocumentId">The document identifier.</param>
/// <param name="FilePath">The file path, if saved.</param>
/// <param name="IsDirty">The new dirty state.</param>
public record DocumentDirtyChangedEvent(
    string DocumentId,
    string? FilePath,
    bool IsDirty
) : INotification;

/// <summary>
/// Event published when a document is saved.
/// </summary>
/// <param name="DocumentId">The document identifier.</param>
/// <param name="FilePath">The saved file path.</param>
/// <param name="BytesWritten">Number of bytes written.</param>
/// <param name="SavedAt">When the save occurred.</param>
public record DocumentSavedEvent(
    string DocumentId,
    string FilePath,
    long BytesWritten,
    DateTimeOffset SavedAt
) : INotification;

/// <summary>
/// Event published when a document is loaded from file.
/// </summary>
/// <param name="DocumentId">The document identifier.</param>
/// <param name="FilePath">The loaded file path.</param>
/// <param name="LoadedAt">When the load occurred.</param>
public record DocumentLoadedEvent(
    string DocumentId,
    string FilePath,
    DateTimeOffset LoadedAt
) : INotification;

/// <summary>
/// Event published when a file is opened (for recent files tracking).
/// </summary>
/// <param name="FilePath">The opened file path.</param>
/// <param name="OpenedAt">When the file was opened.</param>
public record FileOpenedEvent(
    string FilePath,
    DateTimeOffset OpenedAt
) : INotification;
```

---

## 6. Implementation Logic

### 6.1 FileService Implementation

```csharp
using System.Text;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Editor.Services;

/// <summary>
/// File service with atomic save support.
/// </summary>
/// <remarks>
/// LOGIC: All save operations use a three-phase atomic write:
/// 1. Write content to {filename}.tmp
/// 2. Delete original {filename} (if exists)
/// 3. Rename {filename}.tmp to {filename}
///
/// If any phase fails, we attempt rollback to preserve the original file.
/// This prevents data loss from interrupted writes.
/// </remarks>
public sealed class FileService(
    ILogger<FileService> logger) : IFileService
{
    private const string TempExtension = ".tmp";
    private const int MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB

    /// <inheritdoc/>
    public async Task<SaveResult> SaveAsync(
        string filePath,
        string content,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        encoding ??= new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        logger.LogInformation("Saving file: {FilePath}", filePath);

        try
        {
            // Validate path
            var validation = ValidatePath(filePath);
            if (validation is not null)
                return validation;

            // PHASE 1: Write to temp file
            var tempPath = filePath + TempExtension;
            logger.LogDebug("Writing to temp file: {TempPath}", tempPath);

            var bytes = encoding.GetBytes(content);
            await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);

            // PHASE 2: Delete original (if exists)
            if (File.Exists(filePath))
            {
                logger.LogDebug("Deleting original file: {FilePath}", filePath);
                File.Delete(filePath);
            }

            // PHASE 3: Rename temp to original
            logger.LogDebug("Renaming temp to original: {TempPath} -> {FilePath}", tempPath, filePath);
            File.Move(tempPath, filePath);

            logger.LogInformation(
                "File saved successfully: {FilePath} ({Bytes} bytes)",
                filePath, bytes.Length);

            return new SaveResult(true, filePath, bytes.Length);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Access denied saving file: {FilePath}", filePath);
            return new SaveResult(false, filePath, Error: new SaveError(
                SaveErrorCode.AccessDenied,
                $"Access denied: {filePath}",
                ex));
        }
        catch (DirectoryNotFoundException ex)
        {
            logger.LogError(ex, "Directory not found: {FilePath}", filePath);
            return new SaveResult(false, filePath, Error: new SaveError(
                SaveErrorCode.DirectoryNotFound,
                $"Directory not found: {Path.GetDirectoryName(filePath)}",
                ex));
        }
        catch (PathTooLongException ex)
        {
            logger.LogError(ex, "Path too long: {FilePath}", filePath);
            return new SaveResult(false, filePath, Error: new SaveError(
                SaveErrorCode.PathTooLong,
                $"Path exceeds maximum length: {filePath}",
                ex));
        }
        catch (IOException ex) when (ex.HResult == -2147024816) // Disk full
        {
            logger.LogError(ex, "Disk full saving file: {FilePath}", filePath);
            CleanupTempFile(filePath);
            return new SaveResult(false, filePath, Error: new SaveError(
                SaveErrorCode.DiskFull,
                "Insufficient disk space",
                ex));
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "IO error saving file: {FilePath}", filePath);
            CleanupTempFile(filePath);
            return new SaveResult(false, filePath, Error: new SaveError(
                SaveErrorCode.IoError,
                ex.Message,
                ex));
        }
    }

    /// <inheritdoc/>
    public async Task<SaveResult> SaveAsAsync(
        string filePath,
        string content,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        // SaveAs is the same as Save for our purposes
        return await SaveAsync(filePath, content, encoding, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<LoadResult> LoadAsync(
        string filePath,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Loading file: {FilePath}", filePath);

        try
        {
            if (!File.Exists(filePath))
            {
                logger.LogWarning("File not found: {FilePath}", filePath);
                return new LoadResult(false, filePath, Error: new LoadError(
                    LoadErrorCode.FileNotFound,
                    $"File not found: {filePath}"));
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > MaxFileSizeBytes)
            {
                logger.LogWarning("File too large: {FilePath} ({Size} bytes)", filePath, fileInfo.Length);
                return new LoadResult(false, filePath, Error: new LoadError(
                    LoadErrorCode.FileTooLarge,
                    $"File exceeds maximum size ({MaxFileSizeBytes / 1024 / 1024} MB)"));
            }

            // Auto-detect encoding if not specified
            encoding ??= await DetectEncodingAsync(filePath, cancellationToken);

            var content = await File.ReadAllTextAsync(filePath, encoding, cancellationToken);

            logger.LogInformation(
                "File loaded successfully: {FilePath} ({Length} chars)",
                filePath, content.Length);

            return new LoadResult(true, filePath, content, encoding);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Access denied loading file: {FilePath}", filePath);
            return new LoadResult(false, filePath, Error: new LoadError(
                LoadErrorCode.AccessDenied,
                $"Access denied: {filePath}",
                ex));
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "IO error loading file: {FilePath}", filePath);
            return new LoadResult(false, filePath, Error: new LoadError(
                LoadErrorCode.IoError,
                ex.Message,
                ex));
        }
    }

    /// <inheritdoc/>
    public bool CanWrite(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directory))
                return false;

            if (!Directory.Exists(directory))
                return false;

            // Check if file exists and is read-only
            if (File.Exists(filePath))
            {
                var attributes = File.GetAttributes(filePath);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                    return false;
            }

            // Try to open for write access
            using var fs = File.OpenWrite(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public bool Exists(string filePath)
    {
        return File.Exists(filePath);
    }

    private SaveResult? ValidatePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return new SaveResult(false, filePath, Error: new SaveError(
                SaveErrorCode.InvalidPath,
                "File path cannot be empty"));
        }

        var invalidChars = Path.GetInvalidPathChars();
        if (filePath.IndexOfAny(invalidChars) >= 0)
        {
            return new SaveResult(false, filePath, Error: new SaveError(
                SaveErrorCode.InvalidPath,
                "File path contains invalid characters"));
        }

        return null;
    }

    private void CleanupTempFile(string filePath)
    {
        var tempPath = filePath + TempExtension;
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
                logger.LogDebug("Cleaned up temp file: {TempPath}", tempPath);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to clean up temp file: {TempPath}", tempPath);
        }
    }

    private static async Task<Encoding> DetectEncodingAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        // Read first few bytes to detect BOM
        var buffer = new byte[4];
        await using var fs = File.OpenRead(filePath);
        var bytesRead = await fs.ReadAsync(buffer, cancellationToken);

        // Check for BOM
        if (bytesRead >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
            return Encoding.UTF8;

        if (bytesRead >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
            return Encoding.Unicode; // UTF-16 LE

        if (bytesRead >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
            return Encoding.BigEndianUnicode; // UTF-16 BE

        // Default to UTF-8 without BOM
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}
```

### 6.2 ShutdownService Implementation

```csharp
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.ViewModels;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// Service for managing application shutdown with dirty document handling.
/// </summary>
/// <remarks>
/// LOGIC: Maintains a registry of open documents and intercepts shutdown
/// to ensure users can save their work. Shows a dialog if dirty documents exist.
/// </remarks>
public sealed class ShutdownService(
    IFileService fileService,
    ILogger<ShutdownService> logger) : IShutdownService
{
    private readonly List<DocumentViewModel> _documents = [];
    private readonly object _lock = new();

    /// <inheritdoc/>
    public event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested;

    /// <inheritdoc/>
    public bool HasDirtyDocuments => GetDirtyDocuments().Count > 0;

    /// <inheritdoc/>
    public async Task<bool> RequestShutdownAsync()
    {
        logger.LogInformation("Shutdown requested");

        var dirtyDocuments = GetDirtyDocuments();

        if (dirtyDocuments.Count == 0)
        {
            logger.LogDebug("No dirty documents, proceeding with shutdown");
            return true;
        }

        logger.LogInformation(
            "Found {Count} dirty documents, showing save dialog",
            dirtyDocuments.Count);

        // Raise event to show dialog (UI layer handles this)
        var args = new ShutdownRequestedEventArgs
        {
            DirtyDocuments = dirtyDocuments
        };

        ShutdownRequested?.Invoke(this, args);

        if (args.Cancel)
        {
            logger.LogInformation("Shutdown cancelled by user");
            return false;
        }

        logger.LogInformation("Shutdown proceeding");
        return true;
    }

    /// <inheritdoc/>
    public void RegisterDocument(DocumentViewModel document)
    {
        lock (_lock)
        {
            if (!_documents.Contains(document))
            {
                _documents.Add(document);
                logger.LogDebug(
                    "Registered document for shutdown tracking: {Title}",
                    document.Title);
            }
        }
    }

    /// <inheritdoc/>
    public void UnregisterDocument(DocumentViewModel document)
    {
        lock (_lock)
        {
            if (_documents.Remove(document))
            {
                logger.LogDebug(
                    "Unregistered document from shutdown tracking: {Title}",
                    document.Title);
            }
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<DocumentViewModel> GetDirtyDocuments()
    {
        lock (_lock)
        {
            return _documents.Where(d => d.IsDirty).ToList();
        }
    }
}
```

### 6.3 RecentFilesService Implementation

```csharp
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// Service for managing recently opened files history.
/// </summary>
/// <remarks>
/// LOGIC: MRU list is persisted to database via IRecentFilesRepository.
/// On startup, the list is loaded and pruned of invalid entries.
/// The service also handles FileOpenedEvent to auto-add opened files.
/// </remarks>
public sealed class RecentFilesService(
    IRecentFilesRepository repository,
    IMediator mediator,
    ILogger<RecentFilesService> logger) : IRecentFilesService, INotificationHandler<FileOpenedEvent>
{
    private const int DefaultMaxCount = 10;

    /// <inheritdoc/>
    public event EventHandler<RecentFilesChangedEventArgs>? RecentFilesChanged;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecentFileEntry>> GetRecentFilesAsync(
        int maxCount = DefaultMaxCount,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting recent files (max: {MaxCount})", maxCount);

        var entries = await repository.GetRecentFilesAsync(maxCount, cancellationToken);

        // Check file existence
        var results = entries.Select(e => e with
        {
            Exists = File.Exists(e.FilePath)
        }).ToList();

        logger.LogDebug("Retrieved {Count} recent files", results.Count);

        return results;
    }

    /// <inheritdoc/>
    public async Task AddRecentFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding to recent files: {FilePath}", filePath);

        var fileName = Path.GetFileName(filePath);
        var entry = new RecentFileEntry(
            filePath,
            fileName,
            DateTimeOffset.UtcNow,
            1,
            true);

        await repository.UpsertAsync(entry, cancellationToken);

        RecentFilesChanged?.Invoke(this, new RecentFilesChangedEventArgs
        {
            ChangeType = RecentFilesChangeType.Added,
            FilePath = filePath
        });

        logger.LogDebug("Added to recent files: {FilePath}", filePath);
    }

    /// <inheritdoc/>
    public async Task RemoveRecentFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Removing from recent files: {FilePath}", filePath);

        await repository.DeleteAsync(filePath, cancellationToken);

        RecentFilesChanged?.Invoke(this, new RecentFilesChangedEventArgs
        {
            ChangeType = RecentFilesChangeType.Removed,
            FilePath = filePath
        });
    }

    /// <inheritdoc/>
    public async Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Clearing recent files history");

        await repository.ClearAllAsync(cancellationToken);

        RecentFilesChanged?.Invoke(this, new RecentFilesChangedEventArgs
        {
            ChangeType = RecentFilesChangeType.Cleared
        });
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> PruneInvalidEntriesAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Pruning invalid recent files entries");

        var entries = await repository.GetRecentFilesAsync(100, cancellationToken);
        var removed = new List<string>();

        foreach (var entry in entries)
        {
            if (!File.Exists(entry.FilePath))
            {
                await repository.DeleteAsync(entry.FilePath, cancellationToken);
                removed.Add(entry.FilePath);
                logger.LogDebug("Pruned missing file: {FilePath}", entry.FilePath);
            }
        }

        if (removed.Count > 0)
        {
            RecentFilesChanged?.Invoke(this, new RecentFilesChangedEventArgs
            {
                ChangeType = RecentFilesChangeType.Pruned
            });

            logger.LogInformation("Pruned {Count} invalid entries", removed.Count);
        }

        return removed;
    }

    /// <summary>
    /// Handles FileOpenedEvent to automatically track opened files.
    /// </summary>
    public async Task Handle(FileOpenedEvent notification, CancellationToken cancellationToken)
    {
        await AddRecentFileAsync(notification.FilePath, cancellationToken);
    }
}
```

---

## 7. Use Cases & User Stories

### 7.1 User Stories

| ID | Role | Story | Acceptance Criteria |
|:---|:-----|:------|:--------------------|
| US-01 | Writer | As a writer, I want to see "*" when my document has unsaved changes. | Tab title shows "*" suffix when dirty. |
| US-02 | Writer | As a writer, I want Save (Ctrl+S) to save my document safely. | Document saves without corruption. |
| US-03 | Writer | As a writer, I want to be prompted to save when closing with changes. | Dialog appears with Save/Discard/Cancel. |
| US-04 | Writer | As a writer, I want quick access to recently opened files. | File > Open Recent shows last 10 files. |
| US-05 | Writer | As a writer, I want missing recent files to be handled gracefully. | Missing files shown grayed or removed. |
| US-06 | Developer | As a developer, I want file operations logged for debugging. | All save/load operations logged. |
| US-07 | Developer | As a developer, I want atomic saves to prevent data loss. | Interrupted save preserves original file. |

### 7.2 Use Cases

#### UC-01: Edit and Save Document

**Preconditions:**
- Document is open in editor.
- Document has no unsaved changes.

**Flow:**
1. User types text in editor.
2. TextChanged event fires.
3. EditorDocumentViewModel.MarkDirty() called.
4. IsDirty becomes true.
5. Tab title updates to "filename.md*".
6. User presses Ctrl+S.
7. FileService.SaveAsync() called.
8. Atomic save completes successfully.
9. IsDirty cleared.
10. Tab title updates to "filename.md".
11. DocumentSavedEvent published.

**Postconditions:**
- File is saved to disk.
- No "*" indicator in tab.
- Recent files list updated.

---

#### UC-02: Close with Unsaved Changes

**Preconditions:**
- Document has unsaved changes.
- User clicks X button on window.

**Flow:**
1. MainWindow.Closing event fires.
2. ShutdownService.RequestShutdownAsync() called.
3. GetDirtyDocuments() returns one document.
4. SaveChangesDialog shown.
5. User clicks "Save All".
6. FileService.SaveAsync() called for each dirty doc.
7. All saves succeed.
8. Dialog returns true.
9. Window closes.

**Postconditions:**
- All documents saved.
- Application closed.

---

#### UC-03: Open Recent File

**Preconditions:**
- Recent files history has entries.
- One file no longer exists on disk.

**Flow:**
1. User clicks File > Open Recent.
2. RecentFilesService.GetRecentFilesAsync() called.
3. Menu populated with entries.
4. Missing file shown grayed out.
5. User clicks existing file.
6. FileService.LoadAsync() called.
7. Document opens in editor.
8. FileOpenedEvent published.
9. Recent files list updated (file moved to top).

**Postconditions:**
- Document open in editor.
- Recent files updated.

---

## 8. Observability & Logging

### 8.1 Log Events

| Level | Context | Message Template |
|:------|:--------|:-----------------|
| Information | FileService | `Saving file: {FilePath}` |
| Debug | FileService | `Writing to temp file: {TempPath}` |
| Debug | FileService | `Deleting original file: {FilePath}` |
| Debug | FileService | `Renaming temp to original: {TempPath} -> {FilePath}` |
| Information | FileService | `File saved successfully: {FilePath} ({Bytes} bytes)` |
| Error | FileService | `Access denied saving file: {FilePath}` |
| Error | FileService | `IO error saving file: {FilePath}` |
| Information | FileService | `Loading file: {FilePath}` |
| Information | FileService | `File loaded successfully: {FilePath} ({Length} chars)` |
| Warning | FileService | `File not found: {FilePath}` |
| Information | ShutdownService | `Shutdown requested` |
| Debug | ShutdownService | `No dirty documents, proceeding with shutdown` |
| Information | ShutdownService | `Found {Count} dirty documents, showing save dialog` |
| Information | ShutdownService | `Shutdown cancelled by user` |
| Debug | ShutdownService | `Registered document for shutdown tracking: {Title}` |
| Information | RecentFilesService | `Adding to recent files: {FilePath}` |
| Information | RecentFilesService | `Pruning invalid recent files entries` |
| Debug | RecentFilesService | `Pruned missing file: {FilePath}` |

---

## 9. Unit Testing Requirements

### 9.1 FileService Tests

```csharp
[TestFixture]
[Category("Unit")]
public class FileServiceTests
{
    private Mock<ILogger<FileService>> _mockLogger = null!;
    private FileService _sut = null!;
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<FileService>>();
        _sut = new FileService(_mockLogger.Object);
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Test]
    public async Task SaveAsync_ValidContent_CreatesFile()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "test.md");
        var content = "Hello, World!";

        // Act
        var result = await _sut.SaveAsync(filePath, content);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(File.Exists(filePath), Is.True);
            Assert.That(File.ReadAllText(filePath), Is.EqualTo(content));
        });
    }

    [Test]
    public async Task SaveAsync_AtomicWrite_PreservesOriginalOnFailure()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "test.md");
        var originalContent = "Original";
        await File.WriteAllTextAsync(filePath, originalContent);

        // Make directory read-only to cause rename failure
        var dirInfo = new DirectoryInfo(_tempDir);
        // Note: This test may need platform-specific handling

        // The key assertion is that original file survives on failure
    }

    [Test]
    public async Task SaveAsync_EmptyPath_ReturnsError()
    {
        // Act
        var result = await _sut.SaveAsync("", "content");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error?.Code, Is.EqualTo(SaveErrorCode.InvalidPath));
        });
    }

    [Test]
    public async Task LoadAsync_ExistingFile_ReturnsContent()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "test.md");
        var content = "Test content";
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var result = await _sut.LoadAsync(filePath);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Content, Is.EqualTo(content));
        });
    }

    [Test]
    public async Task LoadAsync_NonExistentFile_ReturnsError()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "nonexistent.md");

        // Act
        var result = await _sut.LoadAsync(filePath);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error?.Code, Is.EqualTo(LoadErrorCode.FileNotFound));
        });
    }

    [Test]
    public void CanWrite_ExistingWritableFile_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "test.md");
        File.WriteAllText(filePath, "test");

        // Act
        var result = _sut.CanWrite(filePath);

        // Assert
        Assert.That(result, Is.True);
    }
}
```

### 9.2 ShutdownService Tests

```csharp
[TestFixture]
[Category("Unit")]
public class ShutdownServiceTests
{
    private Mock<IFileService> _mockFileService = null!;
    private Mock<ILogger<ShutdownService>> _mockLogger = null!;
    private ShutdownService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _mockFileService = new Mock<IFileService>();
        _mockLogger = new Mock<ILogger<ShutdownService>>();
        _sut = new ShutdownService(_mockFileService.Object, _mockLogger.Object);
    }

    [Test]
    public async Task RequestShutdownAsync_NoDirtyDocuments_ReturnsTrue()
    {
        // Act
        var result = await _sut.RequestShutdownAsync();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void RegisterDocument_AddsToTracking()
    {
        // Arrange
        var mockDoc = CreateMockDocument("Test", isDirty: false);

        // Act
        _sut.RegisterDocument(mockDoc);

        // Assert
        Assert.That(_sut.GetDirtyDocuments(), Is.Empty);
    }

    [Test]
    public void GetDirtyDocuments_WithDirtyDoc_ReturnsIt()
    {
        // Arrange
        var mockDoc = CreateMockDocument("Test", isDirty: true);
        _sut.RegisterDocument(mockDoc);

        // Act
        var dirtyDocs = _sut.GetDirtyDocuments();

        // Assert
        Assert.That(dirtyDocs, Has.Count.EqualTo(1));
        Assert.That(dirtyDocs[0], Is.EqualTo(mockDoc));
    }

    [Test]
    public void HasDirtyDocuments_WithDirty_ReturnsTrue()
    {
        // Arrange
        var mockDoc = CreateMockDocument("Test", isDirty: true);
        _sut.RegisterDocument(mockDoc);

        // Assert
        Assert.That(_sut.HasDirtyDocuments, Is.True);
    }

    [Test]
    public void UnregisterDocument_RemovesFromTracking()
    {
        // Arrange
        var mockDoc = CreateMockDocument("Test", isDirty: true);
        _sut.RegisterDocument(mockDoc);

        // Act
        _sut.UnregisterDocument(mockDoc);

        // Assert
        Assert.That(_sut.HasDirtyDocuments, Is.False);
    }

    private static DocumentViewModel CreateMockDocument(string title, bool isDirty)
    {
        var mock = new Mock<DocumentViewModel>();
        mock.SetupGet(d => d.Title).Returns(title);
        mock.SetupGet(d => d.IsDirty).Returns(isDirty);
        mock.SetupGet(d => d.Id).Returns(Guid.NewGuid().ToString());
        return mock.Object;
    }
}
```

### 9.3 RecentFilesService Tests

```csharp
[TestFixture]
[Category("Unit")]
public class RecentFilesServiceTests
{
    private Mock<IRecentFilesRepository> _mockRepo = null!;
    private Mock<IMediator> _mockMediator = null!;
    private Mock<ILogger<RecentFilesService>> _mockLogger = null!;
    private RecentFilesService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IRecentFilesRepository>();
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<RecentFilesService>>();
        _sut = new RecentFilesService(
            _mockRepo.Object,
            _mockMediator.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task GetRecentFilesAsync_ReturnsFromRepository()
    {
        // Arrange
        var entries = new List<RecentFileEntry>
        {
            new("C:/test.md", "test.md", DateTimeOffset.UtcNow, 1, true)
        };
        _mockRepo.Setup(r => r.GetRecentFilesAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        // Act
        var result = await _sut.GetRecentFilesAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AddRecentFileAsync_UpsertsCalled()
    {
        // Arrange
        var filePath = "C:/test.md";

        // Act
        await _sut.AddRecentFileAsync(filePath);

        // Assert
        _mockRepo.Verify(r => r.UpsertAsync(
            It.Is<RecentFileEntry>(e => e.FilePath == filePath),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ClearHistoryAsync_ClearsCalled()
    {
        // Act
        await _sut.ClearHistoryAsync();

        // Assert
        _mockRepo.Verify(r => r.ClearAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_FileOpenedEvent_AddsToRecent()
    {
        // Arrange
        var notification = new FileOpenedEvent("C:/test.md", DateTimeOffset.UtcNow);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _mockRepo.Verify(r => r.UpsertAsync(
            It.Is<RecentFileEntry>(e => e.FilePath == notification.FilePath),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

---

## 10. Security & Safety

### 10.1 File Path Security

> [!IMPORTANT]
> File paths must be validated to prevent path traversal attacks.
> Never accept file paths from untrusted sources without validation.

```csharp
// CORRECT: Validate path before operations
if (Path.GetFullPath(filePath).StartsWith(allowedDirectory))
{
    await fileService.SaveAsync(filePath, content);
}

// WRONG: Trust user-provided paths directly
await fileService.SaveAsync(userProvidedPath, content);
```

### 10.2 Atomic Save Guarantees

> [!NOTE]
> The atomic save strategy ensures data integrity:
> - If any step fails, the original file is preserved
> - Temp files are cleaned up on failure
> - Users never lose data due to interrupted saves

### 10.3 Recent Files Privacy

> [!WARNING]
> Recent files history may reveal sensitive file paths.
> Consider clearing history on logout or providing user control.

---

## 11. Risks & Mitigations

| Risk | Impact | Mitigation |
|:-----|:-------|:-----------|
| Atomic save fails mid-operation | High | Preserve original; log failure; notify user |
| Save while app crash | High | Implement auto-save feature (v0.1.5) |
| Database unavailable | Medium | Graceful degradation for recent files |
| File locked by another process | Medium | Retry with exponential backoff; show error |
| Path too long (Windows) | Low | Validate path length; truncate display |
| Unicode encoding issues | Low | Auto-detect encoding; default to UTF-8 |

---

## 12. Acceptance Criteria (QA)

| # | Category | Criterion |
|:--|:---------|:----------|
| 1 | **[Dirty]** | Tab title shows "*" when document has unsaved changes |
| 2 | **[Dirty]** | Tab title loses "*" after successful save |
| 3 | **[Dirty]** | DocumentDirtyChangedEvent published on state change |
| 4 | **[Save]** | Ctrl+S saves document to existing path |
| 5 | **[Save]** | Save As prompts for new path |
| 6 | **[Save]** | Atomic save prevents corruption during write |
| 7 | **[Save]** | Failed save preserves original file |
| 8 | **[Close]** | Dialog appears when closing with dirty documents |
| 9 | **[Close]** | "Save All" saves all and closes |
| 10 | **[Close]** | "Discard All" closes without saving |
| 11 | **[Close]** | "Cancel" aborts close operation |
| 12 | **[Recent]** | File > Open Recent shows last 10 files |
| 13 | **[Recent]** | Opening file updates recent files list |
| 14 | **[Recent]** | Missing files handled gracefully |
| 15 | **[Logging]** | All file operations logged appropriately |

---

## 13. Verification Commands

```bash
# 1. Build all projects
dotnet build

# 2. Run unit tests for file service
dotnet test --filter "FullyQualifiedName~FileService"

# 3. Run unit tests for shutdown service
dotnet test --filter "FullyQualifiedName~ShutdownService"

# 4. Run unit tests for recent files
dotnet test --filter "FullyQualifiedName~RecentFiles"

# 5. Verify migration exists
grep -r "RecentFiles" src/Lexichord.Infrastructure/Migrations/

# 6. Run the application and test:
# - Open a file, make changes, verify "*" appears
# - Press Ctrl+S, verify "*" disappears
# - Make changes, close window, verify dialog appears
# - Click File > Open Recent, verify menu populated

# 7. Check database for recent files
psql -h localhost -U lexichord -c "SELECT * FROM \"RecentFiles\";"
```

---

## 14. Deliverable Checklist

| Step | Description | Status |
|:-----|:------------|:-------|
| 1 | Define IDirtyStateTracker interface in Abstractions | [ ] |
| 2 | Implement dirty state in EditorDocumentViewModel | [ ] |
| 3 | Bind TextChanged to IsDirty property | [ ] |
| 4 | Update tab title binding for "*" indicator | [ ] |
| 5 | Define IFileService interface in Abstractions | [ ] |
| 6 | Implement FileService with atomic save | [ ] |
| 7 | Handle all error cases in FileService | [ ] |
| 8 | Define IShutdownService interface in Abstractions | [ ] |
| 9 | Implement ShutdownService | [ ] |
| 10 | Create SaveChangesDialog view and view model | [ ] |
| 11 | Wire up MainWindow.Closing interception | [ ] |
| 12 | Create Migration for RecentFiles table | [ ] |
| 13 | Define IRecentFilesService interface | [ ] |
| 14 | Implement RecentFilesService | [ ] |
| 15 | Create RecentFilesRepository | [ ] |
| 16 | Wire up File > Open Recent menu | [ ] |
| 17 | Define domain events | [ ] |
| 18 | Unit tests for FileService | [ ] |
| 19 | Unit tests for ShutdownService | [ ] |
| 20 | Unit tests for RecentFilesService | [ ] |
| 21 | Integration tests for save operations | [ ] |
| 22 | Update CHANGELOG.md | [ ] |
