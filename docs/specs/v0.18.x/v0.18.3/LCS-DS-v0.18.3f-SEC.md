# LCS-DS-v0.18.3f-SEC: Design Specification — Backup Before Modify

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.3f-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.3-SEC                          |
| **Release Version**   | v0.18.3f                                     |
| **Component Name**    | Backup Before Modify                         |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-04                                   |
| **Last Updated**      | 2026-02-04                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for the **Backup Before Modify** system (v0.18.3f). This feature provides an essential data protection layer by automatically creating a point-in-time backup of a file immediately before any modification or deletion operation. These backups are immutable and versioned, creating a file history that allows users to easily view, compare, and restore previous versions of a file, thus safeguarding against unintended changes and providing a detailed change history.

---

## 3. Detailed Design

### 3.1. Objective

To automatically create immutable, versioned backups before any file modification or (soft) deletion operation. The system will enable quick recovery of previous file states, provide a clear version history for each file, and optimize storage usage through deduplication.

### 3.2. Scope

-   **Automatic Backup Creation**: The system will be integrated into the file operation pipeline to trigger a backup automatically before any `write` or `delete` operation.
-   **Point-in-Time Snapshot**: The backup will be a complete, immutable snapshot of the file's content at that moment.
-   **Versioning**: Backups for the same file will be versioned, creating a linear history (chain) that can be traversed.
-   **Restore to Version**: Users will be able to restore a file to any of its previous backed-up versions.
-   **Storage Efficiency**: The system will implement content-based deduplication to avoid storing identical file content multiple times, significantly reducing storage overhead.
-   **Backup Integrity**: Each backup's integrity will be verifiable through a stored cryptographic hash (SHA-256).

### 3.3. Detailed Architecture

The `IBackupBeforeModifyService` will be the core component. It will be called from the file operation pipeline just before the file is modified. The service will calculate the file's hash, check if a backup with that hash already exists (for deduplication), and then copy the file to a secure backup vault.

```mermaid
graph TD
    subgraph File Write/Delete Pipeline
        A[Request to Modify 'file.txt'] --> B{Call IBackupBeforeModifyService.CreateBackupAsync};
    end

    subgraph BackupBeforeModifyService
        B --> C{1. Read 'file.txt' Content};
        C --> D{2. Calculate SHA-256 Hash of Content};
        D -- Hash --> E{3. Check 'file_backups' Table for Existing Hash};
        E -- Hash Found (Deduplication) --> F[Link New Backup Record to Existing Content];
        E -- Hash Not Found --> G[4. Copy File to Backup Vault (e.g., S3/Blob)];
        G --> H{5. Create New Record in 'file_backups' Table};
        F --> H;
        H -- Backup Metadata --> I{6. Publish 'BackupCreatedEvent'};
        I --> J[Return Backup Record];
    end
    
    subgraph File Write/Delete Pipeline
        J --> K[Proceed with Actual File Modification];
    end
```

#### 3.3.1. Storage and Deduplication

The backup vault will be a separate storage location (e.g., an S3 bucket, a specific directory on disk). Backed-up files will be stored by their content hash. For example, a file with hash `abc...` will be stored as `s3://lexichord-backups/content/abc...`.

The `file_backups` database table does not store the content itself. It stores metadata, including the `content_hash`. When creating a backup:
1.  The service calculates the hash of the current file content.
2.  It queries the database to see if any other backup already points to this `content_hash`.
3.  **If yes (deduplication)**: It creates a new `file_backups` record but points it to the existing content file in the vault. No new file is uploaded.
4.  **If no**: It uploads the new content to the vault (named by its hash) and creates a new `file_backups` record pointing to it.

This ensures that if a file is saved 10 times with no changes, only one copy of its content is stored, but 10 version records are created in the database.

### 3.4. Data Flow

1.  **Backup Trigger**: A request to write to a file is initiated. Before the write occurs, the pipeline calls `IBackupBeforeModifyService.CreateBackupAsync`.
2.  **Hashing**: The service reads the current content of the file and computes its SHA-256 hash.
3.  **Deduplication Check**: It checks the `file_backups` table for an existing entry with the same `content_hash` and `workspace_id`.
4.  **Storage**: If the hash is new, the file content is copied to the backup vault.
5.  **Metadata Record**: A new record is inserted into the `file_backups` table. This record includes the file's original path, a new version number, the content hash, the user ID, and a reference to the `previous_backup_id` for that file, forming a chain.
6.  **Event**: A `BackupCreatedEvent` is published.
7.  **Original Operation Proceeds**: The service returns, and the pipeline proceeds with the original file modification.

### 3.5. Interfaces & Records

The primary interface and its related models are defined in the parent SBD. Here is the C# implementation based on that definition:

```csharp
/// <summary>
/// Automatically creates immutable backups before any file modification,
/// enabling point-in-time recovery and version history.
/// </summary>
public interface IBackupBeforeModifyService
{
    /// <summary>
    /// Creates a backup of a file before a modification or deletion operation.
    /// </summary>
    /// <param name="filePath">The path of the file to back up.</param>
    /// <param name="operation">The operation that triggered the backup.</param>
    /// <param name="context">The security context of the user.</param>
    /// <param name="cancellationToken">Operation cancellation token.</param>
    /// <returns>A record representing the created backup.</returns>
    Task<FileBackup> CreateBackupBeforeModifyAsync(
        string filePath,
        FileAccessOperation operation,
        SecurityContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the complete version history for a specific file.
    /// </summary>
    /// <param name="filePath">The path of the file.</param>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="cancellationToken">Operation cancellation token.</param>
    /// <returns>A collection of backup records in reverse chronological order.</returns>
    Task<IReadOnlyCollection<FileBackup>> GetVersionHistoryAsync(
        string filePath,
        Guid workspaceId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a single point-in-time backup of a file.
/// </summary>
public record FileBackup(
    Guid Id,
    Guid WorkspaceId,
    string FilePath,
    int VersionNumber,
    string ContentHash,
    string ContentLocation,
    long SizeBytes,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    Guid? PreviousBackupId);
```

### 3.6. Error Handling

-   **Backup Failure**: If the backup process fails for any reason (e.g., cannot read the original file, cannot write to the backup vault), the entire operation should be aborted. The original file will not be modified, and an error will be returned to the user, preventing data modification without a successful backup.
-   **Hash Mismatch**: During a restore operation, the system will verify that the hash of the content in the backup vault matches the `content_hash` stored in the database record. If there's a mismatch, it indicates backup corruption, and the restore will be blocked.

### 3.7. Security Considerations

-   **Backup Vault Security**: The backup vault must be secured with strict access controls. Users should not have direct read or write access to it. All access must be brokered through the `IBackupBeforeModifyService`.
-   **Immutability**: Once a backup is written to the vault, it must be treated as immutable. The application should use permissions (e.g., S3 object lock) to enforce this where possible.
-   **Sensitive Data**: Backups will contain the same sensitive data as the original files. The backup vault must be encrypted at rest, and all security measures that apply to the primary storage must also apply to the backup storage.

### 3.8. Performance Considerations

-   **Hashing Overhead**: Hashing a file adds I/O and CPU overhead. For large files, this can be time-consuming. This operation will be performed asynchronously. The backup creation step will add latency to all write/delete operations. The target is to complete the backup for a 10MB file in < 100ms.
-   **Storage Costs**: While deduplication helps, the backup system will increase storage consumption. A clear retention policy (e.g., keep versions for 90 days) and a cleanup job are essential to manage costs.
-   **Database Queries**: The `file_backups` table must be indexed on `(workspace_id, file_path, version_number)` to make retrieving a file's version history fast. The `content_hash` column must also be indexed for the deduplication check.

### 3.9. Testing Strategy

-   **Unit Tests**:
    -   Test the hashing and deduplication logic.
    -   Test the version chaining logic (linking `previous_backup_id`).
    -   Test the restore logic, including the integrity check.
-   **Integration Tests**:
    -   Test the end-to-end flow: modify a file, verify that a backup record is created in the database, and (if it's new content) that a file is created in the backup vault.
    -   Modify a file twice with the same content and verify that only one content file is stored in the vault, but two version records are created in the database.
    -   Test the `GetVersionHistoryAsync` method to ensure it returns the correct chain of versions.
-   **Performance Tests**:
    -   Benchmark the overhead of the backup process on file write operations for various file sizes.

---

## 4. Key Artifacts & Deliverables

| Artifact                  | Description                                                              |
| :------------------------ | :----------------------------------------------------------------------- |
| `IBackupBeforeModifyService`| The main service interface for creating backups and retrieving history.  |
| `BackupBeforeModifyService`| The default implementation with deduplication logic.                     |
| `FileBackup`              | The record representing a single backup version.                         |
| `BackupRepository`        | A repository for interacting with the `file_backups` table.              |
| Database Migration        | SQL script to create the `file_backups` table.                           |
| MediatR Event             | `BackupCreatedEvent`.                                                    |
| UI Component              | A UI for displaying a file's version history and allowing restores.      |

---

## 5. Acceptance Criteria

- [ ] A backup is automatically and successfully created before every file modification and soft deletion.
- [ ] Backups are immutable after creation.
- [ ] The system correctly restores a file to any of its previous versions.
- [ ] The storage deduplication mechanism correctly identifies and handles identical content, reducing storage growth.
- [ ] The backup integrity can be verified via the stored content hash.
- [ ] A file's version history is accessible through the UI, showing a clear chain of changes.
- [ ] The backup creation process for a file < 10MB completes in under 100ms.

---

## 6. Dependencies & Integration Points

-   **File Operation Pipeline**: This service must be called from the pipeline before any file content is changed or a file is moved to the trash.
-   **`IDeleteProtectionService (v0.18.3c)`**: This service will also trigger the backup service before it moves a file to the trash.
-   **Storage Abstraction**: The service will need a way to write to the designated backup vault (e.g., an S3 client, a file system service).
-   **PostgreSQL Database**: For storing the `file_backups` table.
-   **UI Framework**: To build the version history viewer.
