# LCS-DS-v0.18.3d-SEC: Design Specification — File Access Audit Trail

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.3d-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.3-SEC                          |
| **Release Version**   | v0.18.3d                                     |
| **Component Name**    | File Access Audit Trail                      |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-04                                   |
| **Last Updated**      | 2026-02-04                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for the **File Access Audit Trail** system (v0.18.3d). This component is the definitive source of truth for all file-related activities within Lexichord. It is designed to create a comprehensive, immutable, and queryable log of every file operation (read, write, delete, etc.). This audit trail is essential for security investigations, compliance with regulations (e.g., SOC 2, GDPR), and for detecting suspicious activity patterns.

---

## 3. Detailed Design

### 3.1. Objective

To create a comprehensive and performant audit logging system that captures all file operations with detailed context. The system will provide structured logging, long-term storage, advanced querying capabilities, and alerting for suspicious activities, all with minimal performance impact on the core file operations.

### 3.2. Scope

-   **Structured Logging**: Log all critical file access events (`Read`, `Write`, `Delete`, `Rename`, `Move`) in a structured format.
-   **Contextual Data**: Each log entry will include rich contextual information: user ID, user role, IP address, session ID, timestamp, operation duration, and the result of the operation.
-   **Queryable Audit Trail**: The audit trail will be stored in a dedicated database table (`file_access_logs`) and will be queryable through an API and a dedicated UI.
-   **Export Functionality**: Provide the ability to export audit logs in standard formats (CSV, JSON) for compliance and external analysis.
-   **Suspicious Pattern Detection**: A service will analyze the audit logs to detect and alert on suspicious patterns, such as mass deletions or unusual access times.
-   **Log Retention**: Implement a configurable retention policy to archive or delete old logs.

### 3.3. Detailed Architecture

The `IFileAccessAuditor` service will provide the central interface for logging. To minimize performance impact on the file operation pipeline, the auditor will use a non-blocking, asynchronous "fire-and-forget" pattern for logging. Log entries will be added to an in-memory queue and processed by a background service that performs batch writes to the database.

```mermaid
graph TD
    subgraph File Operation Pipeline
        A[File Operation] --> B(Call IFileAccessAuditor.LogAccessAsync);
    end

    subgraph FileAccessAuditor Service
        B --> C{Add Log to In-Memory Queue};
        C --> D[Return Task.CompletedTask Immediately];
    end

    subgraph Background Logging Service
        E(Timer: 2 seconds or Queue > 100) --> F{Dequeue Logs};
        F --> G{Write Batch to 'file_access_logs' Table};
        G -- DB Error --> H{Retry / Log to Fallback File};
    end

    subgraph Audit Query & Reporting
        I[API Request for Logs] --> J{AuditLogQueryService};
        J --> K{Query 'file_access_logs' Table};
        K --> I;
        L[API Request for Export] --> J;
        J --> M{Generate CSV/JSON Stream};
        M --> L;
    end

    subgraph Anomaly Detection
        N(Timer: Hourly) --> O{SuspiciousPatternDetector};
        O --> P{Query Recent Logs from DB};
        P --> Q{Analyze for Patterns (Mass Delete, etc.)};
        Q -- Anomaly Found --> R{Publish 'SuspiciousAccessDetectedEvent'};
    end
```

#### 3.3.1. Asynchronous Logging

The key to performance is decoupling the logging from the file operation. The `LogAccessAsync` method will simply add the log entry to a `ConcurrentQueue<FileAccessLog>`. A background `IHostedService` will continuously monitor this queue and write batches of logs to the database, ensuring that the file operation itself is not delayed by database latency.

### 3.4. Data Flow

1.  **Log Generation**: A file operation completes (successfully or not). The service responsible for the operation constructs a `FileAccessLog` object with all relevant context.
2.  **Logging**: It calls `IFileAccessAuditor.LogAccessAsync`, passing the log object. The auditor adds the object to the internal queue and returns immediately.
3.  **Batch Processing**: The background logging service dequeues available logs in batches.
4.  **Database Write**: The service performs a bulk insert of the log batch into the `file_access_logs` table.
5.  **Querying**: A user or administrator uses the Audit Dashboard UI. The UI calls an API endpoint, which uses the `AuditLogQueryService` to query the `file_access_logs` table with the specified filters (user, date range, etc.).
6.  **Anomaly Detection**: On a schedule, the `SuspiciousPatternDetector` service queries the logs for the last N hours and analyzes them for predefined patterns. If a pattern is matched, it publishes an event.

### 3.5. Interfaces & Records

The primary interface and its related models are defined in the parent SBD. Here is the C# implementation based on that definition:

```csharp
/// <summary>
/// Records and manages a comprehensive audit trail of all file system operations
/// for compliance reporting and security investigations.
/// </summary>
public interface IFileAccessAuditor
{
    /// <summary>
    /// Asynchronously logs a file access event with complete context. This method is non-blocking.
    /// </summary>
    /// <param name="auditLog">The log entry with all operation details.</param>
    Task LogAccessAsync(FileAccessLog auditLog);
}

/// <summary>
/// Represents a single file access audit log entry.
/// </summary>
public record FileAccessLog(
    Guid Id,
    Guid WorkspaceId,
    Guid UserId,
    string UserRole,
    string FilePath,
    FileAccessOperation Operation,
    FileAccessResult Result,
    string IpAddress,
    string SessionId,
    long DurationMs,
    string? FailureReason,
    DateTime Timestamp);

/// <summary>
/// The types of file access operations that are tracked.
/// </summary>
public enum FileAccessOperation
{
    Read, Write, Create, Delete, Move, Rename, Copy, Execute
}

/// <summary>
/// The result status of a file access operation.
/// </summary>
public enum FileAccessResult
{
    Success,
    PermissionDenied,
    NotFound,
    InvalidPath,
    Error
}
```

### 3.6. Error Handling

-   **Logging Failures**: If the background service fails to write a batch of logs to the database, it will retry several times with exponential backoff. If failures persist, the batch of logs will be written to a local fallback file on disk for later reprocessing.
-   **Queue Overflow**: The in-memory queue will be capped at a large size (e.g., 1 million entries). If this limit is reached (indicating a prolonged database outage), new logs will be dropped, and a critical system-wide alert will be raised. This is an extreme fallback to prevent the application from running out of memory.

### 3.7. Security Considerations

-   **Log Integrity**: The `file_access_logs` table should have restrictive database permissions. The application's runtime user should only have `INSERT` and `SELECT` permissions, not `UPDATE` or `DELETE`, to prevent log tampering.
-   **Sensitive Data in Logs**: Care must be taken to not log sensitive data directly. For example, the content of a write operation should not be logged, only its metadata (size, hash, etc.). File paths, however, are essential and will be logged.
-   **Denial of Service**: An attacker could try to generate a massive volume of file operations to flood the logging system. The asynchronous, batching nature of the design helps absorb spikes, but rate limiting on file operations may be necessary at a higher level.

### 3.8. Performance Considerations

-   **Minimal Impact on Operations**: The "fire-and-forget" logging approach is designed to have a near-zero performance impact on the file operations themselves. The only synchronous work is adding an item to a concurrent queue.
-   **Database Performance**: The `file_access_logs` table is expected to grow very large. It must be properly indexed (e.g., on `workspace_id`, `user_id`, and `timestamp`). For large-scale deployments, table partitioning by date (e.g., monthly) is a critical strategy for maintaining query performance.
-   **Query Performance**: Queries against the audit log must be optimized. The UI should enforce providing a time range for all queries to avoid full table scans.

### 3.9. Testing Strategy

-   **Unit Tests**:
    -   Test the `LogAccessAsync` method to ensure it correctly adds items to the queue.
    -   Test the logic of the background logging service, including its batching and error handling (retry/fallback).
    -   Test the `AuditLogQueryService` with various filter combinations.
    -   Test the suspicious pattern detection logic with sample log data.
-   **Integration Tests**:
    -   Test the full flow: a file operation occurs, a log is queued, and the background service writes it to the database. Verify the record in the database.
-   **Load Tests**:
    -   Simulate a high volume of file operations and measure the performance of the logging pipeline.
    -   Verify that the background service can keep up with the load and that the queue does not grow indefinitely.
    -   Benchmark query performance on a `file_access_logs` table populated with millions of records.

---

## 4. Key Artifacts & Deliverables

| Artifact                  | Description                                                              |
| :------------------------ | :----------------------------------------------------------------------- |
| `IFileAccessAuditor`      | The main service interface for non-blocking logging.                     |
| `FileAccessAuditor`       | The implementation using a background service for batch DB writes.       |
| `FileAccessLog`           | The structured record for a single audit log entry.                      |
| `AuditLogQueryService`    | A service for querying the audit logs with filtering and pagination.     |
| `SuspiciousPatternDetector`| A service to analyze logs for anomalies.                                 |
| Database Migration        | SQL script to create and index the `file_access_logs` table.             |
| MediatR Event             | `SuspiciousAccessDetectedEvent` published when anomalies are found.      |
| UI Dashboard              | A UI for viewing, filtering, and exporting audit logs.                   |

---

## 5. Acceptance Criteria

- [ ] All file operations (`Read`, `Write`, `Create`, `Delete`, `Move`, `Rename`) are logged with complete and accurate context.
- [ ] The performance overhead of the logging call on a file operation is less than 5% under benchmark conditions.
- [ ] The audit trail is queryable by user, file path, operation type, and date range via the UI.
- [ ] Failed file operations are logged with the reason for the failure.
- [ ] The system can export audit logs to CSV and JSON formats.
- [ ] The anomaly detection service correctly identifies and alerts on predefined suspicious patterns (e.g., mass deletions).
- [ ] The database schema for `file_access_logs` is partitioned by month to ensure query performance on large datasets.

---

## 6. Dependencies & Integration Points

-   **All File Operation Services**: Any service that interacts with the file system must call the `IFileAccessAuditor`. This can be enforced using middleware or decorators.
-   **`IHostedService`**: The background logging service will be implemented as a hosted service that starts with the application.
-   **PostgreSQL Database**: For storing the `file_access_logs` table. Partitioning will be used for scalability.
-   **MediatR**: For publishing `SuspiciousAccessDetectedEvent`s.
-   **UI Framework**: To build the audit log dashboard.
