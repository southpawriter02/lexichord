# LCS-DS-v0.18.3c-SEC: Design Specification — Delete Protection & Trash

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.3c-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.3-SEC                          |
| **Release Version**   | v0.18.3c                                     |
| **Component Name**    | Delete Protection & Trash                    |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-04                                   |
| **Last Updated**      | 2026-02-04                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for the **Delete Protection & Trash** system (v0.18.3c). This feature is a critical data safety net, designed to prevent permanent data loss from accidental user deletions or malicious actions. Instead of immediate permanent deletion, file operations will trigger a "soft delete," moving the item to a managed trash/recycle bin. This system provides functionality for listing, restoring, and permanently deleting items from the trash, all governed by configurable retention policies.

---

## 3. Detailed Design

### 3.1. Objective

To implement a robust soft-delete and trash management system that intercepts delete operations, moves files to a secure trash location, and provides users and administrators with tools to manage the lifecycle of deleted items. The system will ensure data recoverability while managing storage costs through automated retention policies.

### 3.2. Scope

-   **Soft Delete**: Intercept file deletion requests and move the file to a designated trash area instead of deleting it from the file system.
-   **Trash Management**: Provide functionalities to list, search, and filter items in the trash.
-   **Restore Functionality**: Allow users to restore deleted items to their original location, with conflict resolution if the original path is no longer available.
-   **Permanent Deletion**: Implement a secure, audited process for permanently deleting items from the trash, which will require user confirmation.
-   **Automated Retention**: A background service will automatically enforce retention policies (e.g., permanently delete items older than 30 days).
-   **Auditing**: All trash-related operations (soft delete, restore, permanent delete) will be fully audited.

### 3.3. Detailed Architecture

The Delete Protection system will be implemented as a service (`IDeleteProtectionService`) that orchestrates the soft-delete process. A dedicated database table, `trash_items`, will store metadata about deleted files. The actual deleted files will be moved to a hidden directory within the workspace's storage area (e.g., `/.trash/{file_guid}`).

```mermaid
graph TD
    subgraph File Delete Request
        A[Delete Request for 'file.txt'] --> B{IDeleteProtectionService.SoftDeleteAsync};
    end

    subgraph DeleteProtectionService
        B --> C{1. Validate Permissions};
        C --> D{2. Move 'file.txt' to '/.trash/{guid}'};
        D --> E{3. Create Record in 'trash_items' Table};
        E -- Metadata: original_path, user_id, etc. --> F{4. Publish 'FileDeletedEvent'};
        F --> G[Return TrashItem to Caller];
    end

    subgraph User Action: Restore
        H[Restore Request for TrashItem] --> I{RestoreFromTrashAsync};
        I --> J{Check if Original Path is Free};
        J -- Path Free --> K{Move File from '/.trash' to Original Path};
        K --> L{Update 'trash_items' Record (is_restored=true)};
        L --> M{Publish 'FileRestoredEvent'};
        J -- Path Occupied --> N[Return Error to User];
    end

    subgraph Background Cleanup Job
        O(Timer: Daily) --> P{EnforceRetentionPolicyAsync};
        P --> Q{Query 'trash_items' for Expired Items};
        Q -- Expired Items --> R{Permanently Delete Files from '/.trash'};
        R --> S{Delete Records from 'trash_items' Table};
    end
```

#### 3.3.1. Trash Storage

Deleted files will not be stored in the database. Only their metadata will. The files themselves will be moved to a dedicated, isolated directory (the "trash folder"). To prevent filename collisions, each deleted file will be renamed to a unique identifier (e.g., a GUID) in the trash folder, with the original path stored in the `trash_items` database table.

### 3.4. Data Flow

1.  **Soft Deletion**:
    a. A user action triggers a file deletion. The application calls `IDeleteProtectionService.SoftDeleteAsync`.
    b. The service validates that the user has permission to delete the file.
    c. The file is moved from its original location to the workspace's trash folder and renamed to a unique ID.
    d. A new record is inserted into the `trash_items` table with the original path, the new path in the trash, user information, and the expiration date.
    e. A `FileDeletedEvent` is published.

2.  **Restoration**:
    a. A user requests to restore an item from the trash UI. This calls `RestoreFromTrashAsync`.
    b. The service checks if the original path is currently occupied. If it is, an error is returned.
    c. If the path is clear, the file is moved from the trash folder back to its original location and renamed.
    d. The corresponding record in `trash_items` is marked as restored (`is_restored = true`).
    e. A `FileRestoredEvent` is published.

3.  **Permanent Deletion**:
    a. A user requests to permanently delete an item. This calls `PermanentlyDeleteAsync`, which requires a confirmation token.
    b. After validation, the service deletes the physical file from the trash folder.
    c. The record is deleted from the `trash_items` table.
    d. The action is logged in the audit trail.

### 3.5. Interfaces & Records

The primary interface and its related models are defined in the parent SBD. Here is the C# implementation based on that definition:

```csharp
/// <summary>
/// Implements soft-delete functionality with a trash/recycle bin
/// to protect against accidental or malicious file deletion.
/// </summary>
public interface IDeleteProtectionService
{
    /// <summary>
    /// Performs a soft delete by moving the file to the trash.
    /// </summary>
    /// <param name="filePath">Path of the file to delete.</param>
    /// <param name="context">Security context of the operation.</param>
    /// <param name="cancellationToken">Operation cancellation token.</param>
    /// <returns>A TrashItem record with details of the soft-deleted file.</returns>
    Task<TrashItem> SoftDeleteAsync(
        string filePath,
        SecurityContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a file from the trash to its original location.
    /// </summary>
    /// <param name="trashItemId">Identifier of the trash item to restore.</param>
    /// <param name="context">Security context of the restoration.</param>
    /// <param name="cancellationToken">Operation cancellation token.</param>
    /// <returns>A result indicating the success of the restore operation.</returns>
    Task<FileRestoreResult> RestoreFromTrashAsync(
        Guid trashItemId,
        SecurityContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a file from the trash. Requires user confirmation.
    /// </summary>
    /// <param name="trashItemId">Identifier of the trash item.</param>
    /// <param name="context">Security context of the deletion.</param>
    /// <param name="confirmationToken">A token proving user confirmation.</param>
    /// <param name="cancellationToken">Operation cancellation token.</param>
    Task PermanentlyDeleteAsync(
        Guid trashItemId,
        SecurityContext context,
        string confirmationToken,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an item in the trash/recycle bin.
/// </summary>
public record TrashItem(
    Guid Id,
    Guid WorkspaceId,
    string OriginalPath,
    string TrashPath,
    long SizeBytes,
    Guid DeletedByUserId,
    DateTime DeletedAt,
    DateTime ExpiresAt);

/// <summary>
/// Represents the result of a file restoration operation.
/// </summary>
public record FileRestoreResult(
    Guid TrashItemId,
    string RestoredPath,
    bool WasRestored,
    string? ErrorMessage,
    DateTime RestoredAt);
```

### 3.6. Error Handling

-   **Restore Path Conflict**: If `RestoreFromTrashAsync` is called and a file or directory already exists at the original path, the operation will fail with a clear error message. It will not overwrite existing files.
-   **File Not Found**: If the physical file in the trash folder is missing when a restore or permanent delete is attempted, the system will log a critical error and clean up the orphaned database record.
-   **Race Conditions**: All file system moves and database operations will be performed within a transaction where possible to prevent inconsistent states (e.g., file moved but database record creation fails).

### 3.7. Security Considerations

-   **Trash Access**: The trash folder (`/.trash`) should be inaccessible for direct browsing or modification by users. All interactions must go through the `IDeleteProtectionService`. The `PathRestrictionEngine` should be configured to block direct access to this directory.
-   **Permanent Deletion Confirmation**: To prevent malicious bulk deletions, permanent deletion of single items and emptying the trash will require a confirmation step in the UI, which generates a short-lived token passed to the API.
-   **Audit Trail**: Every action (soft delete, restore, permanent delete) must be logged in the `file_access_logs` table with the responsible user's context.

### 3.8. Performance Considerations

-   **File Move Operation**: The soft-delete operation involves a file move/rename, which is typically an atomic and fast operation on most modern file systems if it's within the same volume.
-   **Database Queries**: The `trash_items` table will be indexed by `workspace_id` and `expires_at` to ensure efficient querying for both the UI and the cleanup service.
-   **Cleanup Job**: The background job for enforcing retention policies will process items in batches to avoid long-running transactions and to handle a large number of expired items gracefully.

### 3.9. Testing Strategy

-   **Unit Tests**:
    -   Test the `SoftDeleteAsync` logic, ensuring it correctly generates the trash path and database record.
    -   Test `RestoreFromTrashAsync`, including the path conflict scenario.
    -   Test the `PermanentlyDeleteAsync` logic.
    -   Test the logic of the retention policy cleanup service.
-   **Integration Tests**:
    -   Test the end-to-end flow: delete a file, see it in the trash, restore it, and verify it's back in the original location.
    -   Test the background cleanup job, verifying that expired items are correctly removed from both the file system and the database.
-   **Stress Tests**:
    -   Test the system's behavior with a large number of items in the trash (e.g., >100,000) to ensure UI and cleanup performance remains acceptable.

---

## 4. Key Artifacts & Deliverables

| Artifact                  | Description                                                              |
| :------------------------ | :----------------------------------------------------------------------- |
| `IDeleteProtectionService`| The main service interface for trash management.                         |
| `DeleteProtectionService` | The default implementation of the service.                               |
| `TrashItem`               | Record representing an item in the trash.                                |
| `FileRestoreResult`       | Record representing the outcome of a restore operation.                  |
| `TrashRepository`         | A repository for querying the `trash_items` table.                       |
| `TrashCleanupService`     | A background service to enforce retention policies.                      |
| Database Migration        | SQL script to create the `trash_items` table.                            |
| MediatR Events            | `FileDeletedEvent`, `FileRestoredEvent`.                                 |
| UI Components             | A UI for viewing, searching, restoring, and deleting trash items.        |

---

## 5. Acceptance Criteria

- [ ] All file deletions in the application are intercepted and result in a soft delete.
- [ ] Deleted items correctly appear in the trash UI with accurate metadata (deletion time, user, original path).
- [ ] The restore operation successfully moves a file back to its original location if the path is available.
- [ ] The restore operation fails with a clear error message if the original path is occupied.
- [ ] Permanent deletion requires a UI confirmation step and is logged in the audit trail.
- [ ] The background cleanup job automatically and permanently deletes trash items that have passed their retention period (default 30 days).
- [ ] The trash can be browsed and filtered by date range and filename.

---

## 6. Dependencies & Integration Points

-   **`IFileAccessAuditor`**: To log all soft delete, restore, and permanent delete actions.
-   **`PathRestrictionEngine (v0.18.3a)`**: To prevent direct user access to the `/.trash` directory.
-   **`File System Abstraction`**: The service will interact with the file system via an abstraction layer to perform file move and delete operations.
-   **MediatR**: To publish events related to the lifecycle of trash items.
-   **PostgreSQL Database**: For storing the `trash_items` table.
-   **UI Framework**: To build the trash management interface.
