# Lexichord Scope Breakdown Document (SBD) - v0.18.3-SEC
## File System Security Implementation

---

## 1. DOCUMENT CONTROL

| Property | Value |
|----------|-------|
| **Document ID** | LCS-SBD-v0.18.3-SEC |
| **Version** | 1.0 |
| **Release Target** | v0.18.3 |
| **Theme** | File System Security |
| **Total Estimated Hours** | 62 hours |
| **Creation Date** | 2026-02-01 |
| **Status** | Draft - Pending Review |
| **Owner** | Security Architecture Team |
| **Classification** | Internal - Technical Specification |

---

## 2. EXECUTIVE SUMMARY

### 2.1 Overview

Version 0.18.3 of Lexichord introduces **comprehensive file system security protections** designed to prevent unauthorized access, detect sensitive files, and safeguard critical data from accidental or malicious deletion. This release represents a fundamental shift toward enterprise-grade file system governance within the application ecosystem.

### 2.2 Security Objectives

The File System Security initiative addresses five critical domains:

1. **Path Isolation & Restriction** - Prevent access to sensitive system directories through validation engine
2. **Sensitive Data Detection** - Identify and flag files containing credentials, keys, and PII patterns
3. **Data Protection & Recovery** - Implement trash/recycle functionality with automatic backup creation
4. **Audit & Compliance** - Log all file access and modifications for security investigations
5. **Workspace Segmentation** - Isolate file operations within configurable organizational boundaries

### 2.3 Business Value

- **Risk Reduction**: Prevents accidental exposure of API keys, credentials, and sensitive configuration files
- **Compliance**: Enables audit trails for SOC 2, ISO 27001, and GDPR data protection requirements
- **Data Recovery**: Protects against ransomware and accidental deletion through immutable backup chains
- **Access Control**: Provides fine-grained file system permissions aligned with role-based security models
- **Visibility**: Offers real-time alerts for suspicious file access patterns and unauthorized operations

### 2.4 Impact Scope

- **Core System**: File access layer, storage interface, authentication pipeline
- **User Experience**: New UI for trash management, backup restoration, and access warnings
- **Data Layer**: PostgreSQL schema expansion for audit logs, path restrictions, backup metadata
- **Integration Points**: MediatR event system, dependency injection containers, middleware pipeline

---

## 3. SUB-PARTS BREAKDOWN

### 3.1 v0.18.3a: Path Restriction Engine (12 hours)

**Objective**: Implement a robust path validation and restriction engine that prevents file operations on sensitive system directories and workspace-restricted paths.

**Scope**:
- Multi-level path validation (OS level, application level, workspace level)
- Dynamic path restriction rules loaded from database
- Pattern-based path blacklist/whitelist system
- Real-time path checking with performance optimization
- Integration with authentication context for user-specific restrictions

**Acceptance Criteria**:
- [ ] Path validation engine rejects all system paths (/etc, /sys, /proc, Windows\System32)
- [ ] Workspace paths are restricted per user authorization
- [ ] Path checks execute in < 1ms for cached rules
- [ ] Restriction rules can be updated without application restart
- [ ] All path validation attempts are logged for audit purposes
- [ ] Unit test coverage >= 95% for validation logic
- [ ] Performance under 10,000 simultaneous path checks
- [ ] Support for Windows, Linux, and macOS path formats

**Key Deliverables**:
- IPathRestrictionEngine interface and implementation
- PathValidationContext class with caching
- PathRestrictionRule domain model
- RestrictedPathsRepository for database access
- Integration tests with synthetic file system

---

### 3.2 v0.18.3b: Sensitive File Detection (10 hours)

**Objective**: Build a pattern-matching engine that identifies files containing credentials, API keys, private encryption keys, and other sensitive data.

**Scope**:
- Regex-based pattern matching for sensitive file content
- Filename-based detection (.env, .pem, id_rsa, .aws/credentials)
- File extension scanning (*.key, *.pfx, *.p12)
- Real-time detection on file creation/modification
- Configurable sensitivity levels and custom patterns
- Integration with sensitive file database

**Acceptance Criteria**:
- [ ] Detects 99%+ of common credential patterns (AWS keys, API keys, tokens)
- [ ] Identifies private key files (RSA, DSA, ECDSA formats)
- [ ] Scans environment configuration files (.env, .env.local)
- [ ] Detects database connection strings in files
- [ ] Sensitive file scans complete in < 50ms for small files (< 1MB)
- [ ] Custom pattern rules can be added via admin interface
- [ ] False positive rate < 2%
- [ ] Support for binary and text file detection

**Key Deliverables**:
- ISensitiveFileDetector interface and implementation
- SensitiveFilePattern domain model with regex compilation
- SensitivePatternRepository for pattern management
- FileContentScanner with streaming for large files
- Detection result events and notifications

---

### 3.3 v0.18.3c: Delete Protection & Trash (10 hours)

**Objective**: Implement a soft-delete trash/recycle bin system that prevents permanent data loss through accidental deletion or malicious operations.

**Scope**:
- Soft-delete implementation with logical deletion flags
- Trash/recycle bin with configurable retention policies
- Permanent deletion with confirmation and audit trail
- Restore functionality for trash items
- Automated cleanup based on retention period
- Integration with backup service for data recovery

**Acceptance Criteria**:
- [ ] All file deletions move to trash instead of permanent deletion
- [ ] Trash items retain metadata (deletion time, deleted by, original path)
- [ ] Restore operations accurately reconstruct file system state
- [ ] Permanent deletion requires confirmation with audit log entry
- [ ] Trash cleanup runs automatically after 30-day retention period
- [ ] Trash can be browsed and filtered by date range
- [ ] Restore operations verify destination path availability
- [ ] Bulk operations supported for efficiency
- [ ] Trash size limits enforced with warnings

**Key Deliverables**:
- IDeleteProtectionService interface and implementation
- TrashItem domain model with soft-delete support
- TrashRepository with querying capabilities
- TrashCleanupService for retention policy enforcement
- RestoreOperationValidator for path conflict resolution
- Trash management UI components

---

### 3.4 v0.18.3d: File Access Audit Trail (10 hours)

**Objective**: Create a comprehensive audit logging system that captures all file operations for security investigations and compliance reporting.

**Scope**:
- Structured logging of file access events (read, write, delete, rename, move)
- User and role context preservation in log entries
- IP address and session tracking
- Timestamp and operation duration tracking
- Queryable audit trail with filtering and reporting
- Export capabilities for compliance audits
- Alert triggers for suspicious patterns

**Acceptance Criteria**:
- [ ] All file operations logged with complete context
- [ ] Audit logs include user ID, role, IP address, and timestamp
- [ ] Log entries capture operation duration and result status
- [ ] Audit trail queryable by user, file path, operation type, date range
- [ ] Failed operations logged with error details
- [ ] Bulk operations tracked individually for accountability
- [ ] Audit logs retained for minimum 1 year
- [ ] Export to CSV/JSON for compliance reporting
- [ ] Alert system detects suspicious patterns (mass deletions, unusual hours)
- [ ] Performance impact < 5% for file operations

**Key Deliverables**:
- IFileAccessAuditor interface and implementation
- FileAccessLog domain model and repository
- AuditLogQueryService for compliance reporting
- SuspiciousPatternDetector for anomaly alerts
- AuditLogExporter for compliance exports
- Audit dashboard UI with filtering

---

### 3.5 v0.18.3e: Workspace Boundaries (10 hours)

**Objective**: Enforce logical and physical workspace boundaries to prevent cross-workspace file access and ensure data isolation.

**Scope**:
- Workspace-level file system restrictions
- User role-based access control within workspace
- Workspace-scoped path validation
- Cross-workspace permission denial
- Workspace-specific audit trails
- Multi-tenancy support with strict isolation

**Acceptance Criteria**:
- [ ] Users cannot access files outside their assigned workspace
- [ ] Workspace paths are isolated from other workspaces
- [ ] Shared files require explicit workspace-to-workspace permissions
- [ ] Audit trails are workspace-scoped
- [ ] Admin roles can access workspace management without full file access
- [ ] Workspace switching triggers re-validation of all file permissions
- [ ] API endpoints reject cross-workspace file operations
- [ ] Path traversal attacks fail with invalid path errors
- [ ] Workspace boundaries enforced at all access layers
- [ ] Performance: workspace validation < 1ms per operation

**Key Deliverables**:
- IWorkspaceBoundaryManager interface and implementation
- WorkspaceFileScope domain model
- WorkspacePath validation logic
- WorkspaceScopeValidator middleware
- Cross-workspace permission verification service
- Workspace audit context preservation

---

### 3.6 v0.18.3f: Backup Before Modify (6 hours)

**Objective**: Automatically create immutable backups before any file modification operation, enabling quick recovery from unintended changes.

**Scope**:
- Point-in-time backup creation before modifications
- Backup versioning and chaining
- Incremental backup optimization for large files
- Backup retention policies
- One-click restore to previous versions
- Backup integrity verification
- Storage efficiency through deduplication

**Acceptance Criteria**:
- [ ] Backup created before every file modification
- [ ] Backups are immutable (read-only after creation)
- [ ] Backup metadata includes timestamp, operation, user, content hash
- [ ] File version history accessible through UI
- [ ] Restore to any previous version with single click
- [ ] Backup creation < 10ms for < 10MB files
- [ ] Storage deduplication reduces backup size by 80%+ for similar files
- [ ] Backup integrity verified through hash comparison
- [ ] Older backups automatically deleted after retention period
- [ ] Backup storage usage monitored and reported

**Key Deliverables**:
- IBackupBeforeModifyService interface and implementation
- FileBackup domain model with version chaining
- BackupRepository with versioning support
- BackupIntegrityVerifier using cryptographic hashing
- BackupDeduplicationService for storage optimization
- Version history UI component

---

### 3.7 v0.18.3g: File Type Restrictions (4 hours)

**Objective**: Implement file type validation that prevents upload and execution of dangerous file types within the application.

**Scope**:
- MIME type validation
- File extension restrictions
- Magic byte verification
- Dangerous file type detection (executables, scripts)
- Configurable whitelist/blacklist per workspace
- User guidance for restricted types

**Acceptance Criteria**:
- [ ] Executable files (.exe, .bat, .sh, .ps1) rejected
- [ ] Archive bombs and zip files validated for size
- [ ] MIME type verified against file extension
- [ ] Magic byte validation prevents extension spoofing
- [ ] Workspace-specific file type policies enforced
- [ ] Clear user messages for rejected file types
- [ ] Admin can customize allowed/blocked types
- [ ] Validation < 5ms for file type checking
- [ ] Dangerous files in backups flagged for recovery

**Key Deliverables**:
- IFileTypeRestrictor interface and implementation
- FileTypeValidation domain model
- MimeTypeValidator with magic byte verification
- DangerousFileDetector for executable analysis
- FileTypePolicy repository and management
- User messaging system for rejections

---

## 4. DETAILED INTERFACES & CONTRACTS

### 4.1 IPathRestrictionEngine

```csharp
/// <summary>
/// Provides comprehensive path validation and restriction functionality
/// to prevent unauthorized access to system and protected directories.
/// </summary>
public interface IPathRestrictionEngine
{
    /// <summary>
    /// Validates whether a path is accessible within the current security context.
    /// </summary>
    /// <param name="path">Full file system path to validate</param>
    /// <param name="context">Security context including user and workspace info</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Validation result with detailed reasoning if denied</returns>
    Task<PathValidationResult> ValidatePathAsync(
        string path,
        SecurityContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates multiple paths in batch for efficiency.
    /// </summary>
    /// <param name="paths">Collection of paths to validate</param>
    /// <param name="context">Security context for validation</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Collection of validation results maintaining input order</returns>
    Task<IReadOnlyCollection<PathValidationResult>> ValidatePathsAsync(
        IEnumerable<string> paths,
        SecurityContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new path restriction rule to the engine.
    /// </summary>
    /// <param name="rule">Restriction rule to add</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    Task AddRestrictionRuleAsync(
        PathRestrictionRule rule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a path restriction rule by identifier.
    /// </summary>
    /// <param name="ruleId">Identifier of rule to remove</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    Task RemoveRestrictionRuleAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active restriction rules for a workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace identifier</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Collection of active restriction rules</returns>
    Task<IReadOnlyCollection<PathRestrictionRule>> GetWorkspaceRulesAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached validation results to force re-evaluation.
    /// Used after rule updates or during security incidents.
    /// </summary>
    /// <param name="cancellationToken">Operation cancellation token</param>
    Task ClearCacheAsync(CancellationToken cancellationToken = default);
}

/// <summary>Result of path validation operation.</summary>
public class PathValidationResult
{
    public bool IsAllowed { get; set; }
    public string Path { get; set; }
    public PathViolationType? ViolationType { get; set; }
    public string Reason { get; set; }
    public long ExecutionTimeMs { get; set; }
    public DateTime ValidatedAt { get; set; }
}

/// <summary>Enumeration of path violation types.</summary>
public enum PathViolationType
{
    SystemPathAccess,
    WorkspaceOutOfBounds,
    InsufficientPermission,
    RestrictedPattern,
    UserBlacklisted,
    SymlinkEscape
}
```

### 4.2 ISensitiveFileDetector

```csharp
/// <summary>
/// Detects files containing sensitive data such as credentials,
/// API keys, private encryption keys, and personally identifiable information.
/// </summary>
public interface ISensitiveFileDetector
{
    /// <summary>
    /// Scans a file for sensitive patterns and returns detection results.
    /// </summary>
    /// <param name="filePath">Path to file to scan</param>
    /// <param name="context">Security context for the scan</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Detection result with found patterns and severity</returns>
    Task<SensitiveFileDetectionResult> ScanFileAsync(
        string filePath,
        SecurityContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans file content directly without accessing file system.
    /// </summary>
    /// <param name="fileName">Name of file being scanned</param>
    /// <param name="content">Content to scan as byte array</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Detection result with identified patterns</returns>
    Task<SensitiveFileDetectionResult> ScanContentAsync(
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs filename-based detection without content scanning.
    /// Fast path for obvious sensitive file names.
    /// </summary>
    /// <param name="fileName">File name to check</param>
    /// <returns>Detection results if file name matches sensitive pattern</returns>
    SensitiveFileDetectionResult DetectByFileName(string fileName);

    /// <summary>
    /// Registers a custom sensitive pattern for detection.
    /// </summary>
    /// <param name="pattern">Pattern specification with regex and metadata</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    Task RegisterPatternAsync(
        SensitivePattern pattern,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active sensitive detection patterns.
    /// </summary>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Collection of active sensitive patterns</returns>
    Task<IReadOnlyCollection<SensitivePattern>> GetActivePatternsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables a sensitive pattern by identifier.
    /// </summary>
    /// <param name="patternId">Pattern identifier to disable</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    Task DisablePatternAsync(
        Guid patternId,
        CancellationToken cancellationToken = default);
}

/// <summary>Result of sensitive file detection scan.</summary>
public class SensitiveFileDetectionResult
{
    public string FilePath { get; set; }
    public bool IsSensitive { get; set; }
    public SensitivityLevel SensitivityLevel { get; set; }
    public IReadOnlyCollection<PatternMatch> Matches { get; set; }
    public long ScanTimeMs { get; set; }
    public DateTime ScannedAt { get; set; }
}

/// <summary>Information about a pattern match in scanned content.</summary>
public class PatternMatch
{
    public Guid PatternId { get; set; }
    public string PatternName { get; set; }
    public SensitivityLevel Severity { get; set; }
    public int LineNumber { get; set; }
    public int ColumnStart { get; set; }
    public int MatchLength { get; set; }
    public string ContextPreview { get; set; }
}

/// <summary>Enumeration of sensitivity levels.</summary>
public enum SensitivityLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
```

### 4.3 IDeleteProtectionService

```csharp
/// <summary>
/// Implements soft-delete functionality with trash/recycle bin
/// to protect against accidental or malicious file deletion.
/// </summary>
public interface IDeleteProtectionService
{
    /// <summary>
    /// Performs soft delete by moving file to trash instead of permanent deletion.
    /// </summary>
    /// <param name="filePath">Path of file to delete</param>
    /// <param name="context">Security context of operation</param>
    /// <param name="reason">Optional deletion reason for audit</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Trash item information including trash location</returns>
    Task<TrashItem> SoftDeleteAsync(
        string filePath,
        SecurityContext context,
        string reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a file from trash to its original location.
    /// </summary>
    /// <param name="trashItemId">Identifier of trash item</param>
    /// <param name="context">Security context of restoration</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Information about restored file</returns>
    Task<FileRestoreResult> RestoreFromTrashAsync(
        Guid trashItemId,
        SecurityContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a file from trash (requires confirmation).
    /// </summary>
    /// <param name="trashItemId">Identifier of trash item</param>
    /// <param name="context">Security context of deletion</param>
    /// <param name="confirmationToken">Token proving user confirmation</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    Task PermanentlyDeleteAsync(
        Guid trashItemId,
        SecurityContext context,
        string confirmationToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all trash items for a workspace with optional filtering.
    /// </summary>
    /// <param name="workspaceId">Workspace identifier</param>
    /// <param name="filter">Optional filter criteria</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Paginated collection of trash items</returns>
    Task<PagedResult<TrashItem>> GetTrashItemsAsync(
        Guid workspaceId,
        TrashFilterCriteria filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Empties entire trash for a workspace (requires confirmation).
    /// </summary>
    /// <param name="workspaceId">Workspace identifier</param>
    /// <param name="context">Security context of operation</param>
    /// <param name="confirmationToken">Token proving user confirmation</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Count of permanently deleted items</returns>
    Task<int> EmptyTrashAsync(
        Guid workspaceId,
        SecurityContext context,
        string confirmationToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enforces trash retention policies and removes expired items.
    /// Typically called by background job.
    /// </summary>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Count of items removed</returns>
    Task<int> EnforceRetentionPolicyAsync(CancellationToken cancellationToken = default);
}

/// <summary>Represents an item in the trash/recycle bin.</summary>
public class TrashItem
{
    public Guid Id { get; set; }
    public string OriginalPath { get; set; }
    public string TrashPath { get; set; }
    public long SizeBytes { get; set; }
    public DateTime DeletedAt { get; set; }
    public Guid DeletedByUserId { get; set; }
    public Guid WorkspaceId { get; set; }
    public string DeletionReason { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>Result of file restoration operation.</summary>
public class FileRestoreResult
{
    public Guid TrashItemId { get; set; }
    public string RestoredPath { get; set; }
    public bool WasRestored { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime RestoredAt { get; set; }
}
```

### 4.4 IFileAccessAuditor

```csharp
/// <summary>
/// Records and manages comprehensive audit trail of all file system operations
/// for compliance reporting and security investigations.
/// </summary>
public interface IFileAccessAuditor
{
    /// <summary>
    /// Logs a file access event with complete context.
    /// </summary>
    /// <param name="auditLog">Log entry with all operation details</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    Task LogAccessAsync(
        FileAccessLog auditLog,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk logs multiple access events efficiently.
    /// </summary>
    /// <param name="auditLogs">Collection of log entries</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    Task LogAccessBatchAsync(
        IEnumerable<FileAccessLog> auditLogs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries audit logs with advanced filtering and pagination.
    /// </summary>
    /// <param name="query">Query criteria including filters</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Paginated audit log results</returns>
    Task<PagedResult<FileAccessLog>> QueryAuditLogsAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports audit logs in specified format for compliance reporting.
    /// </summary>
    /// <param name="filter">Filter criteria for export</param>
    /// <param name="format">Export format (CSV, JSON, etc.)</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Stream containing exported audit data</returns>
    Task<Stream> ExportAuditLogsAsync(
        AuditLogQuery filter,
        AuditExportFormat format,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects suspicious access patterns that may indicate security incidents.
    /// </summary>
    /// <param name="workspaceId">Workspace to analyze</param>
    /// <param name="timeWindow">Time window for pattern analysis</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Collection of detected suspicious patterns</returns>
    Task<IReadOnlyCollection<SuspiciousAccessPattern>> DetectAnomaliesAsync(
        Guid workspaceId,
        TimeSpan timeWindow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enforces retention policy for audit logs (typically called by scheduler).
    /// </summary>
    /// <param name="retentionDays">Number of days to retain logs</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Count of logs archived</returns>
    Task<long> ArchiveOldLogsAsync(
        int retentionDays,
        CancellationToken cancellationToken = default);
}

/// <summary>Represents a single file access audit log entry.</summary>
public class FileAccessLog
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public string UserRole { get; set; }
    public string FilePath { get; set; }
    public FileAccessOperation Operation { get; set; }
    public FileAccessResult Result { get; set; }
    public string IpAddress { get; set; }
    public string SessionId { get; set; }
    public long DurationMs { get; set; }
    public string FailureReason { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>Enumeration of file access operations tracked.</summary>
public enum FileAccessOperation
{
    Read,
    Write,
    Create,
    Delete,
    Move,
    Rename,
    Copy,
    GetMetadata,
    SetMetadata,
    ListDirectory,
    Execute,
    ChangePermissions
}

/// <summary>Enumeration of file access operation results.</summary>
public enum FileAccessResult
{
    Success,
    PermissionDenied,
    NotFound,
    InvalidPath,
    SizeLimit,
    QuotaExceeded,
    Timeout,
    Error
}
```

---

## 5. ARCHITECTURE DIAGRAMS

### 5.1 File Access Control Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                        FILE ACCESS REQUEST                           │
│                   (Read/Write/Delete/Execute)                        │
└───────────────────────┬─────────────────────────────────────────────┘
                        │
                        ▼
        ┌───────────────────────────────┐
        │  Authentication Verification  │
        │  - Session validation         │
        │  - JWT token verification     │
        │  - User identification        │
        └───────────────┬───────────────┘
                        │
                        ▼
        ┌────────────────────────────────────────┐
        │   Path Restriction Engine Check        │
        │  ┌──────────────────────────────────┐  │
        │  │ 1. System path violation check   │  │
        │  │ 2. Workspace boundary check      │  │
        │  │ 3. User permission validation    │  │
        │  │ 4. Pattern matching (cache)      │  │
        │  └──────────────────────────────────┘  │
        │  Result: ALLOW / DENY (with reason)    │
        └────────────────────┬───────────────────┘
                             │
        ┌────────────────────┴────────────────┐
        │                                     │
   DENIED                                 ALLOWED
        │                                     │
        ▼                                     ▼
    ┌────────────┐          ┌──────────────────────────────┐
    │ Log Denial │          │ Sensitive File Detection      │
    │ Alert User │          │ - Filename scan               │
    │ Return 403 │          │ - Content pattern matching    │
    └────────────┘          │ - Result: SENSITIVE / SAFE    │
                            └──────────┬───────────────────┘
                                       │
                      ┌────────────────┴────────────────┐
                      │                                 │
                  SENSITIVE                          SAFE
                      │                                 │
                      ▼                                 ▼
                 ┌──────────────┐         ┌──────────────────────────┐
                 │ Warn User    │         │ Create Backup (Pre-Mod)  │
                 │ Log Critical │         │ - Hash existing file     │
                 │ Request Conf │         │ - Store in backup vault  │
                 └──────────────┘         │ - Record metadata        │
                      │                   └──────────┬───────────────┘
                      │                              │
                      └──────────────┬───────────────┘
                                     │
                                     ▼
                      ┌──────────────────────────────┐
                      │ Workspace Boundary Check     │
                      │ - Verify workspace scoping   │
                      │ - Check cross-workspace ops  │
                      └──────────────┬───────────────┘
                                     │
                                     ▼
                      ┌──────────────────────────────┐
                      │ File Type Validation         │
                      │ - MIME type check            │
                      │ - Magic byte verification    │
                      │ - Extension validation       │
                      └──────────────┬───────────────┘
                                     │
                                     ▼
                      ┌──────────────────────────────┐
                      │ Execute File Operation       │
                      │ - Read/Write/Delete file     │
                      │ - Handle system calls        │
                      └──────────────┬───────────────┘
                                     │
                                     ▼
                      ┌──────────────────────────────┐
                      │ Log Successful Access        │
                      │ - Operation details          │
                      │ - Duration and result        │
                      │ - User and context info      │
                      └──────────────┬───────────────┘
                                     │
                                     ▼
                      ┌──────────────────────────────┐
                      │ Publish Events               │
                      │ - SensitiveFileAccessEvent   │
                      │ - FileModifiedEvent          │
                      │ - BackupCreatedEvent         │
                      └──────────────────────────────┘
```

### 5.2 Security Protection Layers

```
┌─────────────────────────────────────────────────────────────────────┐
│                         FILE SYSTEM REQUEST                          │
└─────────────────────────────────────────────────────────────────────┘
                                    │
         ┌──────────────────────────┼──────────────────────────┐
         │                          │                          │
         ▼                          ▼                          ▼
    LAYER 1              LAYER 2                  LAYER 3
  ┌────────────┐    ┌─────────────────┐    ┌──────────────────┐
  │ Path       │    │ Sensitive File  │    │ Delete           │
  │ Restriction│    │ Detection       │    │ Protection       │
  │ Engine     │    │ Engine          │    │ Service          │
  │            │    │                 │    │                  │
  │ - Validates│    │ - Regex patterns│    │ - Soft delete    │
  │   paths    │    │ - Content scan  │    │ - Trash items    │
  │ - Blocks   │    │ - Alerts on     │    │ - Restore ops    │
  │   system   │    │   credentials   │    │ - Retention      │
  │ - Enforces │    │ - Flags .env    │    │                  │
  │   workspace│    │ - Detects keys  │    │                  │
  │   bounds   │    │                 │    │                  │
  └────────────┘    └─────────────────┘    └──────────────────┘
         │                  │                        │
         └──────────────────┼────────────────────────┘
                            │
         ┌──────────────────┼──────────────────┐
         │                  │                  │
         ▼                  ▼                  ▼
     LAYER 4          LAYER 5          LAYER 6
  ┌────────────┐  ┌──────────────┐  ┌──────────────┐
  │ Workspace  │  │ Backup Before│  │ File Type    │
  │ Boundaries │  │ Modify       │  │ Restrictions │
  │            │  │              │  │              │
  │ - Multi-   │  │ - Point-in-  │  │ - MIME type  │
  │   tenant   │  │   time backup│  │   check      │
  │   isolation│  │ - Version    │  │ - Extension  │
  │ - Cross-   │  │   chaining   │  │   validation │
  │   workspace│  │ - Dedup      │  │ - Magic byte │
  │   checks   │  │ - Integrity  │  │   verify     │
  │ - Role-    │  │   verify     │  │ - Executable │
  │   based    │  │ - Storage    │  │   detection  │
  │   control  │  │   optimized  │  │              │
  └────────────┘  └──────────────┘  └──────────────┘
         │                  │                  │
         └──────────────────┼──────────────────┘
                            │
                            ▼
                 ┌──────────────────────┐
                 │ File Access Auditor  │
                 │                      │
                 │ - Logs all access    │
                 │ - Records context    │
                 │ - Detects anomalies  │
                 │ - Generates reports  │
                 │ - Archives on policy │
                 └──────────────────────┘
                            │
                            ▼
                 ┌──────────────────────┐
                 │ MediatR Event System │
                 │                      │
                 │ - Publishes events   │
                 │ - Notifies handlers  │
                 │ - Async processing   │
                 │ - Audit trail        │
                 └──────────────────────┘
```

### 5.3 Backup Versioning Chain

```
Original File (content: "ABC")
│
├─ Modification 1: Change to "ABD"
│  ├─ Backup v0: content "ABC" (before change)
│  │  ├─ hash: 0x123ABC (dedup key)
│  │  ├─ timestamp: 2026-02-01 10:00:00
│  │  ├─ operation: Write
│  │  └─ size: 3 bytes
│  │
│  └─ File now: "ABD"
│
├─ Modification 2: Change to "ABDE"
│  ├─ Backup v1: content "ABD" (before change)
│  │  ├─ hash: 0x123ABD
│  │  ├─ timestamp: 2026-02-01 10:05:00
│  │  ├─ previous_backup: v0 (chain link)
│  │  └─ size: 3 bytes
│  │
│  └─ File now: "ABDE"
│
└─ Modification 3: Change to "ABDEF"
   ├─ Backup v2: content "ABDE" (before change)
   │  ├─ hash: 0x123ABDE
   │  ├─ timestamp: 2026-02-01 10:10:00
   │  ├─ previous_backup: v1 (chain link)
   │  └─ size: 4 bytes
   │
   └─ File now: "ABDEF"

VERSION HISTORY (UI Display):
  [Restore] v2 - 2026-02-01 10:10:00 - "ABDE" - 4 bytes
  [Restore] v1 - 2026-02-01 10:05:00 - "ABD" - 3 bytes
  [Restore] v0 - 2026-02-01 10:00:00 - "ABC" - 3 bytes
  [Current] -- 2026-02-01 10:15:00 - "ABDEF" - 5 bytes

DEDUPLICATION EXAMPLE:
  File A: "SECRET_KEY=..." (content hash: 0xAAA)
  File B: "DATABASE_PASSWORD=..." (content hash: 0xBBB)
  File A modified: "SECRET_KEY=..." → File A backup (hash: 0xAAA) - REUSED (same content)

  Storage saved: 100% for identical content across backups
```

---

## 6. SENSITIVE FILE PATTERNS DATABASE

### 6.1 Credential Patterns

```
PATTERN_ID: SEC-001
Name: AWS Access Key ID
Pattern: AKIA[0-9A-Z]{16}
Files: .env, .env.local, .aws/credentials, config.ini
Severity: CRITICAL
Description: Detects AWS IAM access key IDs
ExampleMatch: AKIAIOSFODNN7EXAMPLE

PATTERN_ID: SEC-002
Name: AWS Secret Access Key
Pattern: aws_secret_access_key\s*=\s*[A-Za-z0-9/+=]{40}
Files: .aws/credentials, .env*, AWS config
Severity: CRITICAL
Description: Detects AWS secret keys in configuration files
ExampleMatch: aws_secret_access_key=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY

PATTERN_ID: SEC-003
Name: GitHub Personal Access Token
Pattern: gh[pousr]{1}_[A-Za-z0-9_]{36,255}
Files: .github/*, .env, config, token files
Severity: CRITICAL
Description: Detects GitHub PAT tokens (2021+ format)
ExampleMatch: ghp_1234567890123456789012345678901234567890

PATTERN_ID: SEC-004
Name: Slack Bot Token
Pattern: xoxb-[0-9]{10,13}-[0-9]{10,13}[a-zA-Z0-9-]*
Files: .env, config.json, slack config
Severity: HIGH
Description: Detects Slack bot authentication tokens
ExampleMatch: xoxb-1234567890123-1234567890123-abc123def456

PATTERN_ID: SEC-005
Name: Private RSA Key
Pattern: -----BEGIN RSA PRIVATE KEY-----
Files: id_rsa, *.pem, *.key, private_key*
Severity: CRITICAL
Description: Detects unencrypted RSA private keys
ExampleMatch: [File header matching RSA key format]

PATTERN_ID: SEC-006
Name: Private EC Key
Pattern: -----BEGIN EC PRIVATE KEY-----
Files: *.pem, *.key, id_ecdsa, private_key*
Severity: CRITICAL
Description: Detects unencrypted EC private keys
ExampleMatch: [File header matching EC key format]

PATTERN_ID: SEC-007
Name: OpenSSH Private Key
Pattern: -----BEGIN OPENSSH PRIVATE KEY-----
Files: id_rsa, id_ecdsa, id_ed25519, id_*
Severity: CRITICAL
Description: Detects OpenSSH private keys
ExampleMatch: [File header matching OpenSSH format]

PATTERN_ID: SEC-008
Name: Database Connection String
Pattern: (mysql|postgresql|mongodb|mssql)://[^\s\n]+
Files: config.*, .env*, connection.*, db.*, database.*
Severity: HIGH
Description: Detects database connection URIs with credentials
ExampleMatch: postgresql://user:password@localhost:5432/dbname

PATTERN_ID: SEC-009
Name: API Key Generic
Pattern: ([a-zA-Z_]{1,20}_?api[_-]?key|api[_-]?key)\s*[:=]\s*[a-zA-Z0-9\-_]{32,}
Files: .env, config, settings, api_keys, secrets
Severity: HIGH
Description: Generic API key pattern detection
ExampleMatch: API_KEY = "sk_test_abc123def456ghi789..."

PATTERN_ID: SEC-010
Name: OAuth Bearer Token
Pattern: Bearer [A-Za-z0-9\-\._~\+\/]+=*
Files: .env, auth config, token files, credentials
Severity: HIGH
Description: Detects OAuth 2.0 bearer tokens
ExampleMatch: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

PATTERN_ID: SEC-011
Name: Private Key File by Extension
Extensions: .pem, .key, .pfx, .p12, .jks, .keystore
Files: All files with these extensions
Severity: CRITICAL
Description: Detects private key files by extension
ExampleMatch: [Any .pem, .key, .pfx file]

PATTERN_ID: SEC-012
Name: Sensitive Filename Pattern
Filenames: id_rsa, id_ecdsa, id_ed25519, .aws/credentials, .env, .env.*, config.local, secrets.*, password.*, credentials.*
Severity: CRITICAL
Description: Files with sensitive naming patterns
ExampleMatch: .env, id_rsa, secrets.json

PATTERN_ID: SEC-013
Name: SSH Config File
Pattern: IdentityFile\s+[^\s\n]+
Files: ssh/config, .ssh/config, sshconfig
Severity: MEDIUM
Description: SSH configuration files with key references
ExampleMatch: IdentityFile ~/.ssh/id_rsa_prod

PATTERN_ID: SEC-014
Name: JWT Token Exposed
Pattern: eyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_\.\-~\+\/]+=*
Files: .env, config, logs, responses, html, js
Severity: HIGH
Description: Detects JSON Web Tokens in code/config
ExampleMatch: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ...
```

### 6.2 File Type Patterns

```
CRITICAL_EXECUTABLE:
  Windows: .exe, .msi, .bat, .cmd, .com, .pif, .scr, .vbs, .js, .ps1
  Linux/macOS: ELF executables, shell scripts (.sh with execute bit)
  Java: .jar (if contains executable class entry)
  .NET: .dll, .exe (if compiled assembly)

DANGEROUS_ARCHIVE:
  - .zip files > 100MB (potential zip bombs)
  - .rar, .7z, .tar.gz with compression ratios > 100:1
  - Archives containing executable files

SCRIPTING_FILES:
  Script: .py, .rb, .php, .asp, .jsp, .pl, .sh, .bat, .cmd, .ps1
  Web: .html, .htm, .js, .jsx, .ts, .tsx (if executable context)
  Template: .erb, .jinja, .hbs, .handlebars

SENSITIVE_CONFIG:
  .env* files (environment variables)
  .config, .ini, .yaml, .yml (config files)
  web.config, app.config (.NET config)
  settings.json, config.json (JS config)
  docker-compose.yml (container config)

BACKUP_ARCHIVES:
  .bak, .backup, .old, .tmp, .temp
  .~* (auto-save files)
  .swp, .swo (editor swap files)
```

---

## 7. DATABASE SCHEMA

### 7.1 PostgreSQL Tables

```sql
-- Table: path_restrictions
-- Purpose: Stores path restriction rules for the validation engine
CREATE TABLE path_restrictions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID NOT NULL,
    pattern VARCHAR(500) NOT NULL,
    restriction_type VARCHAR(50) NOT NULL,
        -- VALUES: 'SYSTEM_PATH', 'USER_BLACKLIST', 'WORKSPACE_BOUNDARY', 'PATTERN'
    rule_severity VARCHAR(20) NOT NULL DEFAULT 'HIGH',
        -- VALUES: 'LOW', 'MEDIUM', 'HIGH', 'CRITICAL'
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_by UUID NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (workspace_id) REFERENCES workspaces(id) ON DELETE CASCADE,
    UNIQUE (workspace_id, pattern, restriction_type)
);

CREATE INDEX idx_path_restrictions_workspace_active
    ON path_restrictions(workspace_id, is_active);
CREATE INDEX idx_path_restrictions_type
    ON path_restrictions(restriction_type);

-- Table: sensitive_patterns
-- Purpose: Stores regex patterns for sensitive file detection
CREATE TABLE sensitive_patterns (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    regex_pattern TEXT NOT NULL,
    severity VARCHAR(20) NOT NULL,
        -- VALUES: 'LOW', 'MEDIUM', 'HIGH', 'CRITICAL'
    pattern_category VARCHAR(50) NOT NULL,
        -- VALUES: 'CREDENTIAL', 'PRIVATE_KEY', 'API_KEY', 'DATABASE', 'PII', 'CONFIG'
    file_extensions TEXT,
        -- JSONB array of extensions: [".env", ".pem", ".key"]
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    match_example VARCHAR(500),
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    workspace_id UUID,
        -- NULL means organization-wide, non-NULL means workspace-specific
    FOREIGN KEY (workspace_id) REFERENCES workspaces(id) ON DELETE CASCADE
);

CREATE INDEX idx_sensitive_patterns_active
    ON sensitive_patterns(is_active);
CREATE INDEX idx_sensitive_patterns_workspace_category
    ON sensitive_patterns(workspace_id, pattern_category);

-- Table: file_access_logs
-- Purpose: Comprehensive audit trail of all file operations
CREATE TABLE file_access_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID NOT NULL,
    user_id UUID NOT NULL,
    user_role VARCHAR(50),
    file_path TEXT NOT NULL,
    operation VARCHAR(30) NOT NULL,
        -- VALUES: 'READ', 'WRITE', 'CREATE', 'DELETE', 'MOVE', 'RENAME', 'COPY', 'EXECUTE'
    operation_result VARCHAR(30) NOT NULL,
        -- VALUES: 'SUCCESS', 'PERMISSION_DENIED', 'NOT_FOUND', 'INVALID_PATH', 'ERROR'
    ip_address INET,
    session_id VARCHAR(255),
    duration_ms BIGINT,
    failure_reason VARCHAR(500),
    request_metadata JSONB,
        -- Stores additional context like User-Agent, content hash, file size, etc.
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (workspace_id) REFERENCES workspaces(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE INDEX idx_file_access_logs_workspace_user_date
    ON file_access_logs(workspace_id, user_id, created_at DESC);
CREATE INDEX idx_file_access_logs_file_path
    ON file_access_logs USING GIN (to_tsvector('english', file_path));
CREATE INDEX idx_file_access_logs_operation
    ON file_access_logs(operation);
CREATE INDEX idx_file_access_logs_result
    ON file_access_logs(operation_result);

-- Partitioning by month for large deployments
-- CREATE TABLE file_access_logs_2026_02 PARTITION OF file_access_logs
--     FOR VALUES FROM ('2026-02-01') TO ('2026-03-01');

-- Table: trash_items
-- Purpose: Implements soft delete with trash/recycle bin functionality
CREATE TABLE trash_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID NOT NULL,
    original_path TEXT NOT NULL,
    trash_path TEXT NOT NULL,
    size_bytes BIGINT NOT NULL,
    deleted_by_user_id UUID NOT NULL,
    deletion_reason VARCHAR(500),
    deleted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NOT NULL,
        -- Default: deleted_at + 30 days (configurable per workspace)
    is_restored BOOLEAN NOT NULL DEFAULT FALSE,
    restored_at TIMESTAMP,
    restored_by_user_id UUID,
    FOREIGN KEY (workspace_id) REFERENCES workspaces(id) ON DELETE CASCADE,
    FOREIGN KEY (deleted_by_user_id) REFERENCES users(id),
    FOREIGN KEY (restored_by_user_id) REFERENCES users(id)
);

CREATE INDEX idx_trash_items_workspace_expires
    ON trash_items(workspace_id, expires_at);
CREATE INDEX idx_trash_items_original_path
    ON trash_items(workspace_id, original_path);
CREATE INDEX idx_trash_items_user_deleted
    ON trash_items(deleted_by_user_id, deleted_at DESC);

-- Table: file_backups
-- Purpose: Stores point-in-time backups before file modifications
CREATE TABLE file_backups (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID NOT NULL,
    file_path TEXT NOT NULL,
    version_number INT NOT NULL,
    content_hash VARCHAR(64) NOT NULL,
        -- SHA-256 hash of file content for integrity verification
    content_location VARCHAR(1000) NOT NULL,
        -- Path in backup vault: s3://lexichord-backups/ws-{id}/file-{hash}
    size_bytes BIGINT NOT NULL,
    backup_reason VARCHAR(50) NOT NULL,
        -- VALUES: 'PRE_MODIFICATION', 'PRE_DELETE', 'POLICY_BACKUP'
    created_before_operation TIMESTAMP NOT NULL,
    created_by_operation VARCHAR(50),
        -- The operation that triggered backup: 'WRITE', 'DELETE', 'MOVE'
    created_by_user_id UUID NOT NULL,
    previous_backup_id UUID,
        -- References earlier version for chaining
    is_immutable BOOLEAN NOT NULL DEFAULT TRUE,
    retention_until TIMESTAMP NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (workspace_id) REFERENCES workspaces(id) ON DELETE CASCADE,
    FOREIGN KEY (created_by_user_id) REFERENCES users(id),
    FOREIGN KEY (previous_backup_id) REFERENCES file_backups(id)
);

CREATE INDEX idx_file_backups_file_workspace_version
    ON file_backups(workspace_id, file_path, version_number DESC);
CREATE INDEX idx_file_backups_content_hash
    ON file_backups(workspace_id, content_hash);
    -- For deduplication lookup
CREATE INDEX idx_file_backups_retention
    ON file_backups(workspace_id, retention_until);

-- Table: sensitive_file_detections
-- Purpose: Records sensitive file detection results
CREATE TABLE sensitive_file_detections (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID NOT NULL,
    file_path TEXT NOT NULL,
    detection_timestamp TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_sensitive BOOLEAN NOT NULL,
    sensitivity_level VARCHAR(20),
        -- VALUES: 'LOW', 'MEDIUM', 'HIGH', 'CRITICAL'
    patterns_matched JSONB,
        -- Array of matched pattern IDs and details
    scan_duration_ms BIGINT,
    scanned_by_user_id UUID,
    last_alert_sent TIMESTAMP,
    alert_count INT DEFAULT 0,
    user_acknowledged BOOLEAN DEFAULT FALSE,
    acknowledged_at TIMESTAMP,
    FOREIGN KEY (workspace_id) REFERENCES workspaces(id) ON DELETE CASCADE,
    FOREIGN KEY (scanned_by_user_id) REFERENCES users(id)
);

CREATE INDEX idx_sensitive_detections_workspace_date
    ON sensitive_file_detections(workspace_id, detection_timestamp DESC);
CREATE INDEX idx_sensitive_detections_file_path
    ON sensitive_file_detections(workspace_id, file_path);
CREATE INDEX idx_sensitive_detections_sensitivity
    ON sensitive_file_detections(workspace_id, sensitivity_level);

-- Table: workspace_file_scopes
-- Purpose: Enforces workspace-level file system boundaries
CREATE TABLE workspace_file_scopes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID NOT NULL UNIQUE,
    root_path TEXT NOT NULL,
        -- Base directory for workspace file operations
    allowed_extensions TEXT,
        -- JSONB array of allowed file extensions
    max_file_size_bytes BIGINT,
    max_workspace_size_bytes BIGINT,
    current_usage_bytes BIGINT DEFAULT 0,
    enable_trash BOOLEAN DEFAULT TRUE,
    trash_retention_days INT DEFAULT 30,
    enable_backup_before_modify BOOLEAN DEFAULT TRUE,
    backup_retention_days INT DEFAULT 90,
    scan_sensitive_files BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (workspace_id) REFERENCES workspaces(id) ON DELETE CASCADE
);

CREATE INDEX idx_workspace_file_scopes_workspace
    ON workspace_file_scopes(workspace_id);

-- Table: suspicious_access_patterns
-- Purpose: Records detected anomalies in file access
CREATE TABLE suspicious_access_patterns (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID NOT NULL,
    detection_type VARCHAR(50) NOT NULL,
        -- VALUES: 'MASS_DELETE', 'UNUSUAL_HOURS', 'RAPID_READS', 'PERMISSION_ESCALATION'
    user_id UUID,
    ip_address INET,
    affected_file_count INT,
    affected_files TEXT[],
    severity_score INT,
        -- 1-100: higher = more suspicious
    description TEXT,
    alert_sent BOOLEAN DEFAULT FALSE,
    alert_sent_at TIMESTAMP,
    investigated BOOLEAN DEFAULT FALSE,
    investigation_notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (workspace_id) REFERENCES workspaces(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE INDEX idx_suspicious_patterns_workspace_date
    ON suspicious_access_patterns(workspace_id, created_at DESC);
CREATE INDEX idx_suspicious_patterns_severity
    ON suspicious_access_patterns(workspace_id, severity_score DESC);
```

### 7.2 Database Relationships Diagram

```
Users
  │
  ├─→ file_access_logs (user_id)
  ├─→ trash_items (deleted_by_user_id, restored_by_user_id)
  ├─→ file_backups (created_by_user_id)
  ├─→ sensitive_file_detections (scanned_by_user_id)
  └─→ suspicious_access_patterns (user_id)

Workspaces
  │
  ├─→ path_restrictions (workspace_id)
  ├─→ trash_items (workspace_id)
  ├─→ file_backups (workspace_id)
  ├─→ sensitive_file_detections (workspace_id)
  ├─→ sensitive_patterns (workspace_id) [NULL = org-wide]
  ├─→ file_access_logs (workspace_id)
  ├─→ workspace_file_scopes (workspace_id)
  └─→ suspicious_access_patterns (workspace_id)

file_backups (chaining)
  └─→ file_backups.previous_backup_id creates version chain
```

---

## 8. ADDITIONAL INTERFACES

### 8.5 IWorkspaceBoundaryManager

```csharp
/// <summary>
/// Enforces logical and physical boundaries between workspaces
/// to ensure data isolation and prevent cross-workspace access.
/// </summary>
public interface IWorkspaceBoundaryManager
{
    /// <summary>
    /// Validates that a file path is within workspace boundaries.
    /// </summary>
    /// <param name="filePath">Path to validate</param>
    /// <param name="workspaceId">Workspace identifier</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Boundary validation result</returns>
    Task<BoundaryValidationResult> ValidateWorkspaceBoundaryAsync(
        string filePath,
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new workspace file scope with root directory and limits.
    /// </summary>
    /// <param name="scope">Workspace file scope configuration</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    Task CreateWorkspaceScopeAsync(
        WorkspaceFileScope scope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a cross-workspace file reference is permitted.
    /// </summary>
    /// <param name="sourceWorkspaceId">Origin workspace</param>
    /// <param name="targetWorkspaceId">Target workspace</param>
    /// <param name="filePath">File path being accessed</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Permission status and reasoning</returns>
    Task<CrossWorkspacePermissionResult> CheckCrossWorkspacePermissionAsync(
        Guid sourceWorkspaceId,
        Guid targetWorkspaceId,
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates workspace storage usage.
    /// </summary>
    /// <param name="workspaceId">Workspace identifier</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    Task RecalculateWorkspaceUsageAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current workspace storage usage and limits.
    /// </summary>
    /// <param name="workspaceId">Workspace identifier</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Storage usage information</returns>
    Task<WorkspaceStorageInfo> GetWorkspaceStorageInfoAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);
}

public class BoundaryValidationResult
{
    public bool IsWithinBoundary { get; set; }
    public string WorkspaceRoot { get; set; }
    public string ReasonIfDenied { get; set; }
}

public class CrossWorkspacePermissionResult
{
    public bool IsPermitted { get; set; }
    public PermissionType RequiredPermission { get; set; }
    public string DenialReason { get; set; }
}

public enum PermissionType
{
    None,
    Read,
    Write,
    Admin
}
```

### 8.6 IBackupBeforeModifyService

```csharp
/// <summary>
/// Automatically creates immutable backups before any file modification,
/// enabling point-in-time recovery and version history.
/// </summary>
public interface IBackupBeforeModifyService
{
    /// <summary>
    /// Creates backup of file before modification operation.
    /// </summary>
    /// <param name="filePath">Path of file to backup</param>
    /// <param name="operation">Operation that triggered backup</param>
    /// <param name="context">Security context</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Created backup information</returns>
    Task<FileBackup> CreateBackupBeforeModifyAsync(
        string filePath,
        FileAccessOperation operation,
        SecurityContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores file to specific backup version.
    /// </summary>
    /// <param name="backupId">Backup identifier to restore</param>
    /// <param name="context">Security context</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Restoration result</returns>
    Task<FileRestoreResult> RestoreFromBackupAsync(
        Guid backupId,
        SecurityContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves version history for a file.
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <param name="workspaceId">Workspace identifier</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Collection of backups in reverse chronological order</returns>
    Task<IReadOnlyCollection<FileBackup>> GetVersionHistoryAsync(
        string filePath,
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enforces backup retention policies.
    /// </summary>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Count of backups deleted</returns>
    Task<int> EnforceRetentionPoliciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets storage statistics for backups.
    /// </summary>
    /// <param name="workspaceId">Workspace identifier</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Backup storage usage information</returns>
    Task<BackupStorageStats> GetBackupStorageStatsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);
}

public class BackupStorageStats
{
    public long TotalBackupSizeBytes { get; set; }
    public long DeduplicatedSizeBytes { get; set; }
    public int TotalBackupCount { get; set; }
    public decimal CompressionRatio { get; set; }
    public DateTime OldestBackup { get; set; }
}
```

### 8.7 IFileTypeRestrictor

```csharp
/// <summary>
/// Validates and restricts file types to prevent upload and execution
/// of dangerous or prohibited files within the application.
/// </summary>
public interface IFileTypeRestrictor
{
    /// <summary>
    /// Validates file type based on extension, MIME type, and magic bytes.
    /// </summary>
    /// <param name="fileName">Name of file</param>
    /// <param name="content">File content as byte array</param>
    /// <param name="mimeType">Provided MIME type (will be verified)</param>
    /// <param name="workspaceId">Workspace for policy lookup</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>File type validation result</returns>
    Task<FileTypeValidationResult> ValidateFileTypeAsync(
        string fileName,
        byte[] content,
        string mimeType,
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects if file is likely executable based on content analysis.
    /// </summary>
    /// <param name="content">File content</param>
    /// <param name="fileName">File name for context</param>
    /// <returns>Executable detection result</returns>
    ExecutableDetectionResult DetectExecutable(byte[] content, string fileName);

    /// <summary>
    /// Validates archive files for dangerous content (zip bombs, etc).
    /// </summary>
    /// <param name="filePath">Path to archive file</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>Archive validation result</returns>
    Task<ArchiveValidationResult> ValidateArchiveAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file type policy for a workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace identifier</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    /// <returns>File type policy configuration</returns>
    Task<FileTypePolicy> GetWorkspacePolicyAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates file type policy for workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace identifier</param>
    /// <param name="policy">Updated policy</param>
    /// <param name="cancellationToken">Operation cancellation token</param>
    Task UpdateWorkspacePolicyAsync(
        Guid workspaceId,
        FileTypePolicy policy,
        CancellationToken cancellationToken = default);
}

public class FileTypeValidationResult
{
    public bool IsAllowed { get; set; }
    public bool IsDangerous { get; set; }
    public string FileType { get; set; }
    public string DetectedMimeType { get; set; }
    public string ExtensionMismatch { get; set; }
    public string DenialReason { get; set; }
    public FileTypeRisk RiskLevel { get; set; }
}

public enum FileTypeRisk
{
    Safe,
    Warning,
    Dangerous,
    Forbidden
}

public class ExecutableDetectionResult
{
    public bool IsLikelyExecutable { get; set; }
    public string ExecutableFormat { get; set; }
    public string Reason { get; set; }
}

public class ArchiveValidationResult
{
    public bool IsValid { get; set; }
    public bool IsZipBomb { get; set; }
    public decimal CompressionRatio { get; set; }
    public int FileCount { get; set; }
    public long UncompressedSize { get; set; }
    public string DenialReason { get; set; }
}

public class FileTypePolicy
{
    public Guid WorkspaceId { get; set; }
    public List<string> AllowedExtensions { get; set; }
    public List<string> BlockedExtensions { get; set; }
    public long MaxFileSizeBytes { get; set; }
    public bool AllowExecutables { get; set; }
    public bool AllowArchives { get; set; }
    public bool AllowScripts { get; set; }
}
```

---

## 9. MEDIATr EVENTS

### 9.1 Event Definitions

```csharp
/// <summary>
/// Published when a sensitive file is accessed or detected.
/// </summary>
public class SensitiveFileAccessEvent : INotification
{
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public string FilePath { get; set; }
    public FileAccessOperation Operation { get; set; }
    public SensitivityLevel SensitivityLevel { get; set; }
    public IReadOnlyCollection<PatternMatch> Matches { get; set; }
    public string IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Published when a file is deleted (moved to trash).
/// </summary>
public class FileDeletedEvent : INotification
{
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public string OriginalPath { get; set; }
    public long SizeBytes { get; set; }
    public DateTime DeletedAt { get; set; }
    public string DeletionReason { get; set; }
    public Guid TrashItemId { get; set; }
}

/// <summary>
/// Published when a file is restored from trash.
/// </summary>
public class FileRestoredEvent : INotification
{
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public string RestoredPath { get; set; }
    public Guid TrashItemId { get; set; }
    public DateTime RestoredAt { get; set; }
    public bool WasSuccessful { get; set; }
}

/// <summary>
/// Published when backup is created before modification.
/// </summary>
public class BackupCreatedEvent : INotification
{
    public Guid WorkspaceId { get; set; }
    public string FilePath { get; set; }
    public Guid BackupId { get; set; }
    public int VersionNumber { get; set; }
    public long SizeBytes { get; set; }
    public string ContentHash { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Published when suspicious file access pattern detected.
/// </summary>
public class SuspiciousAccessDetectedEvent : INotification
{
    public Guid WorkspaceId { get; set; }
    public Guid? UserId { get; set; }
    public string DetectionType { get; set; }
    public int SeverityScore { get; set; }
    public int AffectedFileCount { get; set; }
    public string Description { get; set; }
    public DateTime DetectedAt { get; set; }
}

/// <summary>
/// Published when path restriction is violated.
/// </summary>
public class PathRestrictionViolatedEvent : INotification
{
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public string AttemptedPath { get; set; }
    public PathViolationType ViolationType { get; set; }
    public string IpAddress { get; set; }
    public DateTime ViolationTime { get; set; }
}

/// <summary>
/// Published when workspace storage quota exceeded.
/// </summary>
public class WorkspaceQuotaExceededEvent : INotification
{
    public Guid WorkspaceId { get; set; }
    public long CurrentUsageBytes { get; set; }
    public long LimitBytes { get; set; }
    public decimal UsagePercentage { get; set; }
    public DateTime OccurredAt { get; set; }
}

/// <summary>
/// Handler for sensitive file access events.
/// </summary>
public class SensitiveFileAccessEventHandler : INotificationHandler<SensitiveFileAccessEvent>
{
    private readonly ILogger<SensitiveFileAccessEventHandler> _logger;
    private readonly INotificationService _notificationService;

    public SensitiveFileAccessEventHandler(
        ILogger<SensitiveFileAccessEventHandler> logger,
        INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(SensitiveFileAccessEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Sensitive file accessed: {FilePath} by user {UserId} with sensitivity {Level}",
            notification.FilePath,
            notification.UserId,
            notification.SensitivityLevel);

        if (notification.SensitivityLevel >= SensitivityLevel.High)
        {
            await _notificationService.AlertSecurityTeamAsync(
                $"Critical sensitive file access detected: {notification.FilePath}",
                notification,
                cancellationToken);
        }
    }
}
```

---

## 10. UI MOCKUPS

### 10.1 File Access Warning Dialog

```
┌─────────────────────────────────────────────────────────────────┐
│ ⚠️  SENSITIVE FILE DETECTED                            [X]       │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  This file contains sensitive information and requires           │
│  special handling.                                               │
│                                                                   │
│  File: /project/.env                                             │
│  Sensitivity: ⚠️ CRITICAL                                        │
│                                                                   │
│  Detected Patterns:                                              │
│  ✓ AWS Access Key (AKIA...)                                      │
│  ✓ Database Connection String (postgresql://...)                 │
│  ✓ API Key Pattern                                               │
│                                                                   │
│  Actions:                                                         │
│  ┌──────────────────────────────────────────────────┐            │
│  │ ⓘ This file will be tracked in audit logs       │            │
│  │ ⓘ Access requires confirmation                   │            │
│  │ ⓘ Changes will be automatically backed up        │            │
│  └──────────────────────────────────────────────────┘            │
│                                                                   │
│  Security Notice:                                                 │
│  Do not share this file or its content with anyone unless        │
│  authorized. Report any unauthorized access to security team.    │
│                                                                   │
│         [Cancel]              [Continue Anyway]  [Help]           │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

### 10.2 Trash Management UI

```
┌─────────────────────────────────────────────────────────────────┐
│                          TRASH (Recycle Bin)                      │
│                                                              │ 🔄│
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Showing 24 items | 2.5 GB | Expires: 30 days from deletion     │
│                                                                   │
│  [Search...]  [Filter ▼] [Sort: Newest ▼]  [Empty Trash ✕]     │
│                                                                   │
│ ┌─────────────────────────────────────────────────────────────┐  │
│ │ ☑  File Name              Size    Deleted      Expires      │  │
│ ├─────────────────────────────────────────────────────────────┤  │
│ │ ☐  config.local.json      1.2 MB  2 days ago   28 days left │  │
│ │    ✓ Restore              ✕ Delete              ⓘ Preview  │  │
│ │                                                             │  │
│ │ ☐  credentials_backup.env 2.4 MB  5 days ago   25 days left │  │
│ │    ✓ Restore              ✕ Delete              ⓘ Preview  │  │
│ │                                                             │  │
│ │ ☐  database_dump.sql      856 MB  1 week ago   23 days left │  │
│ │    ✓ Restore              ✕ Delete              ⓘ Preview  │  │
│ │                                                             │  │
│ │ ☐  old_logs/app.log       45 MB   3 weeks ago  7 days left ⚠ │  │
│ │    ✓ Restore              ✕ Delete              ⓘ Preview  │  │
│ │    [Will auto-delete in 7 days]                             │  │
│ │                                                             │  │
│ └─────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  Trash Storage: [████████░░] 50.5 GB / 100 GB                    │
│                                                                   │
│  Footer: Older items auto-delete after 30 days. Admin can        │
│          modify retention period in workspace settings.           │
│                                                                   │
│         [Previous Page]          [Next Page]          [Settings]  │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

### 10.3 Backup/Version History UI

```
┌─────────────────────────────────────────────────────────────────┐
│                    VERSION HISTORY: config.json                   │
│                                                    [← Back] [X]   │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Current Version: 4.2 KB | Modified 2 hours ago by john@...      │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ Version │ Timestamp          │ Size   │ Modified By │ Action│ │
│  ├─────────────────────────────────────────────────────────────┤ │
│  │ [●]     │ 2026-02-01 10:15   │ 4.2 KB │ john@     │ CURRENT│ │
│  │         │ Current / Latest   │        │           │        │ │
│  │         └─ Show diff vs v2   └────────────────────┘        │ │
│  │                                                             │ │
│  │ [ ]     │ 2026-02-01 09:00   │ 4.0 KB │ alice@    │[Restore]│ │
│  │         │ Modified API timeout             [Preview]       │ │
│  │         │ Content: {"timeout": 5000...}               │     │ │
│  │         └─ Show diff vs v1                         │     │ │
│  │                                                       │     │ │
│  │ [ ]     │ 2026-01-31 15:30   │ 3.8 KB │ bob@      │[Restore]│ │
│  │         │ Fixed database config          [Preview]        │ │
│  │         │ Content: {"db": "prod"...}                      │ │
│  │         └─ Show diff vs v0                         │     │ │
│  │                                                       │     │ │
│  │ [ ]     │ 2026-01-30 12:00   │ 3.5 KB │ alice@    │[Restore]│ │
│  │         │ Initial upload                 [Preview]        │ │
│  │         │ Content: {"env": "staging"...}                  │ │
│  │                                                       │     │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                                                                   │
│  Backup Policy:                                                   │
│  • Automatic backup before each modification                      │
│  • Stored with 90-day retention                                   │
│  • Immutable and cryptographically verified                       │
│  • Deduplication: 78% storage saved for identical versions        │
│                                                                   │
│         [Download]        [Show Details]      [Storage Stats]     │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

### 10.4 Audit Log Dashboard

```
┌─────────────────────────────────────────────────────────────────┐
│              FILE ACCESS AUDIT LOG DASHBOARD                     │
│                                                           [⊕ View]│
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Filters:                                                         │
│  User: [All Users ▼]  Operation: [All ▼]  Date: [Last 7 Days ▼] │
│  Status: [All ▼]  [Apply]  [Export CSV]  [Export JSON]          │
│                                                                   │
│  Summary Statistics:                                              │
│  Total Operations: 2,847 | Successful: 2,821 | Failed: 26       │
│  Most Active User: john@ (487 ops) | Most Common: Read (1,245)   │
│                                                                   │
│ ┌─────────────────────────────────────────────────────────────┐  │
│ │ Timestamp          User       File Path         Op   Status │  │
│ ├─────────────────────────────────────────────────────────────┤  │
│ │ 2026-02-01 14:30  john@      /proj/.env        R    ✓      │  │
│ │ 2026-02-01 14:28  alice@     /proj/config.json W    ✓      │  │
│ │ 2026-02-01 14:22  bob@       /sys/admin        R    ✗ 403  │  │
│ │                                    [BLOCK: System path access] │  │
│ │ 2026-02-01 14:15  john@      /proj/app.log     D    ✓      │  │
│ │                                    [Moved to trash]            │  │
│ │ 2026-02-01 14:00  alice@     /proj/data.db     X    ✗ ERR  │  │
│ │                                    [File not found]            │  │
│ │ 2026-02-01 13:45  admin@     /proj/data/      CR   ✓      │  │
│ │                                    [Backup created]            │  │
│ │ 2026-02-01 13:30  john@      /cred.pem         R    ⚠ ALERT │  │
│ │                                    [Sensitive file - logged]    │  │
│ │                                                               │  │
│ │ Page 1 of 87 items                                          │  │
│ │ [< Prev]  [Next >]  [Jump to page: __ ]                   │  │
│ │                                                               │  │
│ └─────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  Anomaly Detection:                                               │
│  ⚠️ Unusual Pattern: bob@ attempted 15 path violations in 5 mins  │
│  ⚠️ Mass Delete: alice@ deleted 45 files in 10 minutes           │
│                                                                   │
│         [Investigate]          [Configure Alerts]                │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 11. IMPLEMENTATION CHECKLIST

### Phase 1: Core Foundations (Weeks 1-2)
- [ ] Create PostgreSQL schema for all tables
- [ ] Implement PathValidationContext and caching layer
- [ ] Build IPathRestrictionEngine basic validation
- [ ] Create security context propagation
- [ ] Set up MediatR event infrastructure
- [ ] Write unit tests for path engine (target: 95% coverage)

### Phase 2: Sensitive Detection (Week 3)
- [ ] Implement ISensitiveFileDetector with regex engine
- [ ] Build pattern loading and compilation
- [ ] Create sensitive_patterns table and repository
- [ ] Implement filename-based detection
- [ ] Write content scanning for small files
- [ ] Integration tests with common credential patterns
- [ ] Performance testing (target: < 50ms for 1MB files)

### Phase 3: Delete Protection (Week 4)
- [ ] Implement soft delete in data layer
- [ ] Create IDeleteProtectionService
- [ ] Build trash_items table and queries
- [ ] Implement restore functionality with conflict resolution
- [ ] Create permanent delete with confirmation
- [ ] Trash retention policy enforcement
- [ ] UI for trash management
- [ ] Test with large file counts (1000+ items)

### Phase 4: Audit Trail (Week 5)
- [ ] Implement IFileAccessAuditor logging
- [ ] Create file_access_logs table with partitioning strategy
- [ ] Build audit query service
- [ ] Implement anomaly detection
- [ ] Create export functionality (CSV/JSON)
- [ ] Dashboard UI for logs
- [ ] Performance test (target: < 5% overhead)

### Phase 5: Workspace Boundaries (Week 6)
- [ ] Implement IWorkspaceBoundaryManager
- [ ] Create workspace_file_scopes table
- [ ] Build boundary validation middleware
- [ ] Implement cross-workspace permission checks
- [ ] Test multi-tenant isolation
- [ ] Performance testing for boundary checks

### Phase 6: Backup System (Week 7)
- [ ] Implement IBackupBeforeModifyService
- [ ] Create file_backups table with versioning
- [ ] Build backup integrity verification
- [ ] Implement deduplication engine
- [ ] Create version history UI
- [ ] Test restore operations
- [ ] Backup storage optimization

### Phase 7: File Type Restrictions (Week 7-8)
- [ ] Implement IFileTypeRestrictor
- [ ] Build MIME type and magic byte validation
- [ ] Create executable detection
- [ ] Implement archive validation
- [ ] Build file type policy management
- [ ] Integration with upload endpoints

### Phase 8: Integration & Testing (Week 8-9)
- [ ] Integration testing across all components
- [ ] End-to-end scenario testing
- [ ] Performance benchmarking
- [ ] Security penetration testing
- [ ] UI testing and polish
- [ ] Documentation writing

### Phase 9: Deployment (Week 10)
- [ ] Database migration strategy
- [ ] Feature flag implementation
- [ ] Rollout plan with canary
- [ ] Monitoring setup
- [ ] Alert configuration
- [ ] Production monitoring

---

## 12. PERFORMANCE TARGETS

| Operation | Target | Notes |
|-----------|--------|-------|
| Path Validation (cached) | < 1ms | In-memory lookup |
| Path Validation (uncached) | < 5ms | Database query + cache |
| Sensitive File Scan (< 1MB) | < 50ms | Content + pattern matching |
| Sensitive File Scan (1-10MB) | < 200ms | Streaming + patterns |
| Delete Operation (soft) | < 10ms | Database insert |
| Trash Query (100 items) | < 100ms | Paginated DB query |
| Backup Creation (< 10MB) | < 10ms | File copy + DB record |
| Backup Integrity Verify | < 50ms | SHA-256 hash comparison |
| Audit Log Entry | < 5ms | Async logging |
| Anomaly Detection (7 days) | < 2s | Background job |
| Workspace Boundary Check | < 1ms | Path comparison |
| Archive Validation (< 100MB) | < 500ms | Decompression scan |
| Overall File Operation Overhead | < 5% | Total request time impact |

---

## 13. TESTING STRATEGY

### Unit Testing
- Path validation engine: boundary cases, symlink attacks, null/empty paths
- Sensitive pattern matching: true positives, false positives, regex edge cases
- Trash operations: soft delete, restore, conflicts, quota enforcement
- Backup versioning: chaining, deduplication, integrity verification
- File type validation: extension spoofing, magic bytes, archives

### Integration Testing
- Path engine + workspace boundaries
- Sensitive detection + audit logging
- Delete + backup + trash + audit trail
- Cross-workspace operations
- Concurrent file operations

### Performance Testing
- 10,000 simultaneous path checks
- 1,000 files in trash
- 100,000 audit log entries
- 500+ file versions for single file
- Workspace with 10GB+ files

### Security Testing
- Path traversal attempts
- Symlink escape attempts
- Permission elevation attempts
- Credential exposure in backups
- Audit log tampering resistance

### Compliance Testing
- GDPR data handling
- SOC 2 audit trail requirements
- ISO 27001 access controls
- Data retention policies
- Export/reporting capabilities

---

## 14. DEPENDENCY CHAIN

```
Lexichord.SecurityCore
├── MediatR (events)
├── Serilog (logging)
└── CryptographyHelper (hashing)

Lexichord.FileSystem.Security
├── Lexichord.SecurityCore
├── Lexichord.Data (repositories)
├── System.IO (file operations)
├── System.Text.RegularExpressions (pattern matching)
└── IdentityHelper (security context)

Lexichord.FileSystem.Audit
├── Lexichord.FileSystem.Security
├── Lexichord.Data
└── MediatR

Lexichord.FileSystem.Backup
├── Lexichord.FileSystem.Security
├── Lexichord.Data
├── CloudStorage (S3/Blob)
└── Lexichord.FileSystem.Audit

Lexichord.Api.FileSystem
├── All above modules
├── AspNetCore
└── Lexichord.Api.Common

Lexichord.Web.Files
├── Lexichord.Api.FileSystem
├── React
└── React-Query
```

---

## 15. LICENSE GATING

| Feature | Community | Pro | Enterprise |
|---------|-----------|-----|------------|
| Path Restriction Engine | ✓ | ✓ | ✓ |
| Sensitive File Detection | - | ✓ | ✓ |
| Delete Protection (Basic) | ✓ | ✓ | ✓ |
| Delete Protection (Advanced) | - | ✓ | ✓ |
| Audit Trail (30 days) | - | ✓ | ✓ |
| Audit Trail (1 year retention) | - | - | ✓ |
| Backup Before Modify | - | ✓ | ✓ |
| Workspace Boundaries | ✓ | ✓ | ✓ |
| File Type Restrictions | ✓ | ✓ | ✓ |
| Custom Pattern Rules | - | - | ✓ |
| Audit Report Export | - | - | ✓ |
| Anomaly Detection | - | - | ✓ |
| Deduplication | - | - | ✓ |
| Real-time Alerts | - | - | ✓ |

---

## 16. RISKS & MITIGATIONS

### Risk 1: Performance Degradation
**Impact**: File operations slow down with 10+ protection layers
**Likelihood**: Medium
**Mitigation**:
- Implement aggressive caching for path rules (in-memory + Redis)
- Async audit logging to prevent blocking
- Batch processing for suspicious pattern detection
- Lazy loading of backup data

### Risk 2: Data Consistency Issues
**Impact**: Backups out of sync with current files, trash inconsistent
**Likelihood**: Medium
**Mitigation**:
- Transaction-based operations for file + backup + log
- Scheduled integrity verification jobs
- Database constraints for referential integrity
- Backup hash verification before restore

### Risk 3: Sensitive Data Exposure in Backups
**Impact**: Backups stored insecurely, sensitive files exposed
**Likelihood**: Low
**Mitigation**:
- Encrypt backups at rest (AES-256)
- Store in separate secure vault (S3 with encryption)
- Restrict backup access to security team
- Audit backup access like regular files
- Automatic redaction of sensitive content

### Risk 4: False Positives in Sensitive Detection
**Impact**: Legitimate files flagged as sensitive, blocking work
**Likelihood**: Medium
**Mitigation**:
- Machine learning tuning to reduce false positives
- Whitelist mechanism for verified safe patterns
- User override with audit trail
- Regular pattern accuracy audits
- Gradual rollout with monitoring

### Risk 5: Workspace Boundary Bypass
**Impact**: Users access files from other workspaces
**Likelihood**: Low
**Mitigation**:
- Multiple validation layers (path + database check + middleware)
- Comprehensive integration tests
- Security review of validation logic
- Canary testing before rollout
- Continuous monitoring of boundary violations

### Risk 6: Audit Log Storage Growth
**Impact**: Database grows uncontrollably, performance degrades
**Likelihood**: Medium
**Mitigation**:
- Partition audit logs by month
- Automatic archival to cold storage
- Index optimization for common queries
- Configurable retention policies
- Compression of old audit logs

### Risk 7: Backup Storage Explosion
**Impact**: Storage costs skyrocket, quota issues
**Likelihood**: Medium
**Mitigation**:
- Aggressive deduplication (target 80%+)
- Incremental backups vs full copies
- Compression for backup files
- Configurable retention periods
- Monitoring and alerts on storage growth

---

## 17. ACCEPTANCE CRITERIA SUMMARY

### v0.18.3a: Path Restriction Engine
- [X] System paths blocked (/etc, /sys, /proc, Windows\System32)
- [X] Workspace boundaries enforced
- [X] Path checks < 1ms (cached)
- [X] Rules updateable without restart
- [X] All violations logged
- [X] 95%+ unit test coverage
- [X] 10K concurrent checks supported

### v0.18.3b: Sensitive File Detection
- [X] 99%+ detection of common credentials
- [X] All major private key formats detected
- [X] File scan < 50ms for < 1MB
- [X] Custom patterns supported
- [X] < 2% false positive rate
- [X] Binary and text file support

### v0.18.3c: Delete Protection & Trash
- [X] All deletes soft (move to trash)
- [X] Trash metadata preserved (deletion time, user, reason)
- [X] Restore functionality working
- [X] Permanent delete requires confirmation
- [X] Auto-cleanup after 30 days
- [X] Browsable trash with filters
- [X] Restore conflict resolution

### v0.18.3d: File Access Audit Trail
- [X] All operations logged with context
- [X] Logs include user, role, IP, timestamp
- [X] Operation duration recorded
- [X] Queryable with filtering
- [X] Failed operations tracked
- [X] Bulk operation tracking
- [X] 1-year retention minimum
- [X] CSV/JSON export
- [X] Anomaly detection
- [X] < 5% performance impact

### v0.18.3e: Workspace Boundaries
- [X] Cross-workspace access prevented
- [X] Workspace-scoped paths enforced
- [X] User roles respected within workspace
- [X] Workspace switching re-validates
- [X] API rejects cross-workspace ops
- [X] Path traversal blocked
- [X] All layers enforce boundaries
- [X] < 1ms validation per op

### v0.18.3f: Backup Before Modify
- [X] Backup created before every modification
- [X] Backups immutable
- [X] Metadata complete
- [X] Version history accessible
- [X] One-click restore
- [X] Creation < 10ms for < 10MB
- [X] 80%+ deduplication
- [X] Integrity verified
- [X] Retention policies enforced

### v0.18.3g: File Type Restrictions
- [X] Executables rejected
- [X] Archives validated for size/bombs
- [X] MIME verified vs extension
- [X] Magic bytes checked
- [X] Workspace policies enforced
- [X] Clear user messages
- [X] Admin customization available
- [X] < 5ms validation

---

## CONCLUSION

Version 0.18.3-SEC (File System Security) represents a comprehensive, enterprise-grade implementation of file system protection mechanisms. With 62 hours of estimated development across 7 sub-components, this release delivers critical security capabilities including path restriction, sensitive data detection, delete protection, comprehensive auditing, workspace isolation, automatic backup creation, and file type validation.

The implementation prioritizes:
1. **Security**: Multiple validation layers, encryption, audit trails
2. **Performance**: Caching, async operations, < 5% overhead
3. **Reliability**: Transactional consistency, integrity verification
4. **Compliance**: Complete audit trails, data retention policies
5. **User Experience**: Clear warnings, one-click recovery, trash management

All sub-components are fully detailed with C# interface specifications, PostgreSQL schema, performance targets, testing strategies, and deployment considerations.

**Revision**: 1.0 | **Status**: Draft
**Next Review**: Upon Security Architecture Team Approval

---

*Document End*
