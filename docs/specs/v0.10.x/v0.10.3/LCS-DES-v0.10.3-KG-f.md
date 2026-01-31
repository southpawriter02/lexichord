# LCS-DES-v0.10.3-KG-f: Design Specification — Resolution Audit

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-v0.10.3f` | Entity Resolution sub-part f |
| **Feature Name** | `Resolution Audit` | Track all entity resolution decisions |
| **Target Version** | `v0.10.3f` | Sixth sub-part of v0.10.3-KG |
| **Module Scope** | `Lexichord.Modules.CKVS.Resolution` | Entity Resolution module |
| **Swimlane** | `Entity Resolution` | Knowledge graph vertical |
| **License Tier** | `Teams` | Available in Teams tier and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.EntityResolution` | Entity resolution feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.3-KG](./LCS-SBD-v0.10.3-KG.md) | Entity Resolution scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.3-KG S2.1](./LCS-SBD-v0.10.3-KG.md#21-sub-parts) | f = Resolution Audit |

---

## 2. Executive Summary

### 2.1 The Requirement

Track all entity resolution operations for compliance and audit:

1. Log all disambiguation choices (user selections)
2. Log all merge operations (pre and post-state)
3. Log all duplicate rejections and reviews
4. Record user, timestamp, and context for each action
5. Enable audit queries and export
6. Support undo/rollback of merge operations

### 2.2 The Proposed Solution

Implement `IResolutionAuditService` with:

1. **LogDisambiguationChoiceAsync:** Record disambiguation selections
2. **LogMergeOperationAsync:** Record merge before/after state
3. **LogDuplicateReviewAsync:** Record group reviews and rejections
4. **QueryAuditAsync:** Query audit log with filters
5. **ExportAuditAsync:** Export audit log in various formats
6. Undo chain tracking for merge operations

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IDisambiguationService` | v0.10.3a | Disambiguation choice events |
| `IEntityMerger` | v0.10.3c | Merge operation notifications |
| `IBulkResolutionService` | v0.10.3e | Bulk operation tracking |
| `IGraphVersionService` | v0.10.1-KG | Version/commit tracking |
| `IProfileService` | v0.9.1 | User identification |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| (None required) | | Audit uses standard .NET libraries and database |

### 3.2 Licensing Behavior

- **Core Tier:** Not available
- **WriterPro Tier:** Not available
- **Teams Tier:** Full audit logging for disambiguation and merge operations
- **Enterprise Tier:** Full audit + advanced analytics and compliance reporting

---

## 4. Data Contract (The API)

### 4.1 IResolutionAuditService Interface

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Provides audit logging for all entity resolution operations.
/// Tracks disambiguation choices, merges, and reviews for compliance.
/// </summary>
public interface IResolutionAuditService
{
    /// <summary>
    /// Logs a disambiguation choice made by a user.
    /// Records which candidate was selected for a mention.
    /// </summary>
    /// <param name="entry">Disambiguation audit entry</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Completion task</returns>
    Task LogDisambiguationChoiceAsync(
        DisambiguationAuditEntry entry,
        CancellationToken ct = default);

    /// <summary>
    /// Logs a merge operation with pre and post state.
    /// Records all entities merged and relationships/claims transferred.
    /// </summary>
    /// <param name="entry">Merge operation audit entry</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Unique merge operation ID for undo/rollback</returns>
    Task<Guid> LogMergeOperationAsync(
        MergeOperationAuditEntry entry,
        CancellationToken ct = default);

    /// <summary>
    /// Logs a duplicate group review (marked reviewed or not duplicates).
    /// </summary>
    /// <param name="entry">Duplicate review audit entry</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Completion task</returns>
    Task LogDuplicateReviewAsync(
        DuplicateReviewAuditEntry entry,
        CancellationToken ct = default);

    /// <summary>
    /// Logs an undo/rollback of a previous merge operation.
    /// </summary>
    /// <param name="mergeOperationId">ID of merge to undo</param>
    /// <param name="userId">User performing the undo</param>
    /// <param name="reason">Optional reason for undo</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Completion task</returns>
    Task LogMergeUndoAsync(
        Guid mergeOperationId,
        Guid userId,
        string? reason = null,
        CancellationToken ct = default);

    /// <summary>
    /// Queries audit log with filtering and pagination.
    /// </summary>
    /// <param name="query">Query filter and options</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated audit entries</returns>
    Task<PagedResult<ResolutionAuditEntry>> QueryAuditAsync(
        AuditQueryFilter query,
        CancellationToken ct = default);

    /// <summary>
    /// Exports audit log in specified format.
    /// </summary>
    /// <param name="format">Export format (CSV, JSON, PDF)</param>
    /// <param name="filter">Optional filter to limit export</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Export stream with audit data</returns>
    Task<Stream> ExportAuditAsync(
        AuditExportFormat format,
        AuditQueryFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets undo history for a specific merge operation.
    /// Shows all attempts to undo/redo this merge.
    /// </summary>
    /// <param name="mergeOperationId">ID of merge operation</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Undo history with timestamps and users</returns>
    Task<IReadOnlyList<MergeUndoRecord>> GetUndoHistoryAsync(
        Guid mergeOperationId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets statistics on resolution activities.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Audit statistics</returns>
    Task<AuditStatistics> GetStatisticsAsync(
        CancellationToken ct = default);
}
```

### 4.2 DisambiguationAuditEntry Record

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Audit entry for a disambiguation choice.
/// </summary>
public record DisambiguationAuditEntry
{
    /// <summary>
    /// Unique entry ID.
    /// </summary>
    public required Guid EntryId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// ID of the mention being disambiguated.
    /// </summary>
    public required Guid MentionId { get; init; }

    /// <summary>
    /// Text of the mention.
    /// </summary>
    public required string MentionText { get; init; }

    /// <summary>
    /// ID of document containing mention.
    /// </summary>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// Name of document.
    /// </summary>
    public required string DocumentName { get; init; }

    /// <summary>
    /// Multiple candidates that were presented.
    /// </summary>
    public IReadOnlyList<DisambiguationCandidate> Candidates { get; init; } = [];

    /// <summary>
    /// ID of entity user selected.
    /// </summary>
    public Guid? SelectedEntityId { get; init; }

    /// <summary>
    /// Name of selected entity.
    /// </summary>
    public string? SelectedEntityName { get; init; }

    /// <summary>
    /// Type of selected entity.
    /// </summary>
    public string? SelectedEntityType { get; init; }

    /// <summary>
    /// If true, user created new entity instead of selecting.
    /// </summary>
    public bool CreatedNewEntity { get; init; }

    /// <summary>
    /// Whether user choice should be remembered for similar mentions.
    /// </summary>
    public bool ShouldRemember { get; init; }

    /// <summary>
    /// Confidence score of selected entity.
    /// </summary>
    public float? SelectionConfidenceScore { get; init; }

    /// <summary>
    /// User who made the choice.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Timestamp of choice.
    /// </summary>
    public DateTimeOffset ChoiceTime { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Time user spent reviewing candidates (ms).
    /// </summary>
    public int? ReviewTimeMs { get; init; }

    /// <summary>
    /// Additional context (IP, user agent, etc.).
    /// </summary>
    public IReadOnlyDictionary<string, object>? Context { get; init; }
}
```

### 4.3 MergeOperationAuditEntry Record

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Audit entry for a merge operation.
/// Records pre and post state for rollback capability.
/// </summary>
public record MergeOperationAuditEntry
{
    /// <summary>
    /// Unique entry ID.
    /// </summary>
    public required Guid EntryId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// ID of merge operation.
    /// </summary>
    public required Guid MergeOperationId { get; init; }

    /// <summary>
    /// ID of primary entity (the one kept).
    /// </summary>
    public required Guid PrimaryEntityId { get; init; }

    /// <summary>
    /// Name of primary entity.
    /// </summary>
    public required string PrimaryEntityName { get; init; }

    /// <summary>
    /// IDs of secondary entities (being merged into primary).
    /// </summary>
    public IReadOnlyList<Guid> SecondaryEntityIds { get; init; } = [];

    /// <summary>
    /// Names of secondary entities.
    /// </summary>
    public IReadOnlyList<string> SecondaryEntityNames { get; init; } = [];

    /// <summary>
    /// Number of relationships transferred.
    /// </summary>
    public int RelationshipsTransferred { get; init; }

    /// <summary>
    /// Number of claims transferred.
    /// </summary>
    public int ClaimsTransferred { get; init; }

    /// <summary>
    /// Number of document links updated.
    /// </summary>
    public int DocumentLinksUpdated { get; init; }

    /// <summary>
    /// Aliases created during merge.
    /// </summary>
    public IReadOnlyList<string> AliasesCreated { get; init; } = [];

    /// <summary>
    /// Merge options used.
    /// </summary>
    public required MergeOptions Options { get; init; }

    /// <summary>
    /// Merge conflicts that occurred.
    /// </summary>
    public IReadOnlyList<MergeConflict> ConflictsResolved { get; init; } = [];

    /// <summary>
    /// User who initiated the merge.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Timestamp of merge operation.
    /// </summary>
    public DateTimeOffset OperationTime { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Duration of merge operation (ms).
    /// </summary>
    public long DurationMs { get; init; }

    /// <summary>
    /// Version/commit ID from graph version service.
    /// Enables rollback.
    /// </summary>
    public Guid? VersionCommitId { get; init; }

    /// <summary>
    /// Merge source (Disambiguation, Bulk UI, API, etc.).
    /// </summary>
    public ResolutionSource Source { get; init; }

    /// <summary>
    /// Whether merge can be undone.
    /// </summary>
    public bool CanUndo { get; init; } = true;

    /// <summary>
    /// Additional context (batch ID, group ID, etc.).
    /// </summary>
    public IReadOnlyDictionary<string, object>? Context { get; init; }
}
```

### 4.4 DuplicateReviewAuditEntry Record

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Audit entry for duplicate group review (marked reviewed or not duplicates).
/// </summary>
public record DuplicateReviewAuditEntry
{
    /// <summary>
    /// Unique entry ID.
    /// </summary>
    public required Guid EntryId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// ID of duplicate group.
    /// </summary>
    public required Guid GroupId { get; init; }

    /// <summary>
    /// IDs of entities in the group.
    /// </summary>
    public IReadOnlyList<Guid> EntityIds { get; init; } = [];

    /// <summary>
    /// Names of entities in the group.
    /// </summary>
    public IReadOnlyList<string> EntityNames { get; init; } = [];

    /// <summary>
    /// Type of review action.
    /// </summary>
    public DuplicateReviewAction Action { get; init; }

    /// <summary>
    /// User's reason for review action.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Original confidence score of group.
    /// </summary>
    public float OriginalConfidence { get; init; }

    /// <summary>
    /// User who performed the review.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Timestamp of review.
    /// </summary>
    public DateTimeOffset ReviewTime { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional context.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Context { get; init; }
}
```

### 4.5 ResolutionAuditEntry Record (Base)

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Base record for all audit entries, used in query results.
/// </summary>
public record ResolutionAuditEntry
{
    /// <summary>
    /// Unique entry ID.
    /// </summary>
    public required Guid EntryId { get; init; }

    /// <summary>
    /// Type of resolution action.
    /// </summary>
    public required AuditEntryType EntryType { get; init; }

    /// <summary>
    /// User who performed the action.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// User name or email.
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// When the action occurred.
    /// </summary>
    public required DateTimeOffset ActionTime { get; init; }

    /// <summary>
    /// Summary of the action.
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// Full entry data as JSON.
    /// </summary>
    public string? FullData { get; init; }
}
```

### 4.6 AuditQueryFilter Record

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Filter and pagination options for audit log queries.
/// </summary>
public record AuditQueryFilter
{
    /// <summary>
    /// Filter by entry type.
    /// </summary>
    public AuditEntryType? EntryType { get; init; }

    /// <summary>
    /// Filter by user ID.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Filter by entity ID involved.
    /// </summary>
    public Guid? EntityId { get; init; }

    /// <summary>
    /// Start date for filter (inclusive).
    /// </summary>
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>
    /// End date for filter (inclusive).
    /// </summary>
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>
    /// Filter by merge operation ID.
    /// </summary>
    public Guid? MergeOperationId { get; init; }

    /// <summary>
    /// Filter by duplicate group ID.
    /// </summary>
    public Guid? GroupId { get; init; }

    /// <summary>
    /// Search text in summaries/reasons.
    /// </summary>
    public string? SearchText { get; init; }

    /// <summary>
    /// Page number for pagination (1-based).
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Items per page.
    /// </summary>
    public int PageSize { get; init; } = 50;

    /// <summary>
    /// Sort field (time, user, type, etc.).
    /// </summary>
    public string? SortBy { get; init; } = "time";

    /// <summary>
    /// Sort order (asc, desc).
    /// </summary>
    public SortOrder SortOrder { get; init; } = SortOrder.Descending;
}
```

### 4.7 MergeUndoRecord Record

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Record of an undo/rollback for a merge operation.
/// </summary>
public record MergeUndoRecord
{
    /// <summary>
    /// ID of the merge that was undone.
    /// </summary>
    public required Guid MergeOperationId { get; init; }

    /// <summary>
    /// When the undo was executed.
    /// </summary>
    public DateTimeOffset UndoTime { get; init; }

    /// <summary>
    /// User who performed the undo.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Reason provided for undo.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Whether undo was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if undo failed.
    /// </summary>
    public string? Error { get; init; }
}
```

### 4.8 AuditStatistics Record

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Statistics on resolution audit activities.
/// </summary>
public record AuditStatistics
{
    /// <summary>
    /// Total disambiguation choices recorded.
    /// </summary>
    public int TotalDisambiguations { get; init; }

    /// <summary>
    /// Total merge operations.
    /// </summary>
    public int TotalMerges { get; init; }

    /// <summary>
    /// Total merges that have been undone.
    /// </summary>
    public int TotalMergesUndone { get; init; }

    /// <summary>
    /// Total duplicate groups reviewed.
    /// </summary>
    public int TotalGroupsReviewed { get; init; }

    /// <summary>
    /// Total groups marked as not duplicates.
    /// </summary>
    public int TotalGroupsRejected { get; init; }

    /// <summary>
    /// Number of unique users performing resolutions.
    /// </summary>
    public int UniqueUsers { get; init; }

    /// <summary>
    /// Date range of audit data.
    /// </summary>
    public DateTimeOffset? EarliestEntry { get; init; }

    /// <summary>
    /// Date range of audit data.
    /// </summary>
    public DateTimeOffset? LatestEntry { get; init; }

    /// <summary>
    /// Total entities involved in merges.
    /// </summary>
    public int TotalEntitiesMerged { get; init; }

    /// <summary>
    /// Total relationships transferred.
    /// </summary>
    public int TotalRelationshipsTransferred { get; init; }

    /// <summary>
    /// Total claims transferred.
    /// </summary>
    public int TotalClaimsTransferred { get; init; }
}
```

### 4.9 AuditEntryType Enum

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Type of resolution audit entry.
/// </summary>
public enum AuditEntryType
{
    /// <summary>User selected entity for mention.</summary>
    DisambiguationChoice = 1,

    /// <summary>Entities were merged.</summary>
    MergeOperation = 2,

    /// <summary>Duplicate group marked as reviewed.</summary>
    GroupReviewed = 3,

    /// <summary>Duplicate group marked as not duplicates.</summary>
    GroupRejected = 4,

    /// <summary>Merge operation was undone.</summary>
    MergeUndo = 5
}
```

### 4.10 DuplicateReviewAction Enum

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Type of action taken during duplicate group review.
/// </summary>
public enum DuplicateReviewAction
{
    /// <summary>Group marked reviewed but no action taken.</summary>
    Reviewed = 1,

    /// <summary>Group marked as not actually duplicates.</summary>
    Rejected = 2,

    /// <summary>Group deferred for later review.</summary>
    Deferred = 3
}
```

### 4.11 ResolutionSource Enum

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Source/origin of a resolution operation.
/// </summary>
public enum ResolutionSource
{
    /// <summary>User choice in disambiguation dialog.</summary>
    DisambiguationDialog = 1,

    /// <summary>Single merge via bulk UI.</summary>
    BulkUIManual = 2,

    /// <summary>Batch merge via bulk UI.</summary>
    BulkUIBatch = 3,

    /// <summary>Auto-merge from learning system.</summary>
    AutoMerge = 4,

    /// <summary>API direct call.</summary>
    ApiCall = 5,

    /// <summary>System maintenance or admin action.</summary>
    AdminAction = 6
}
```

### 4.12 AuditExportFormat Enum

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Format for audit log export.
/// </summary>
public enum AuditExportFormat
{
    /// <summary>Comma-separated values.</summary>
    Csv = 1,

    /// <summary>JSON format.</summary>
    Json = 2,

    /// <summary>Portable Document Format.</summary>
    Pdf = 3
}
```

---

## 5. Implementation Strategy

### 5.1 Storage

Audit entries stored in dedicated audit log table:
- Indexed by UserId, ActionTime, EntryType
- Partitioned by date for performance
- Soft delete support (mark deleted, don't remove)

### 5.2 Merge Undo Chain

For merge operations:
- Store version/commit ID from graph version service
- Enable rollback to previous version
- Track undo attempts and results

### 5.3 Event Hooks

Subscribe to events:
- `DisambiguationChoiceRecorded` from Part A
- `MergeOperationCompleted` from Part C
- `DuplicateGroupReviewedEvent` from Part E

---

## 6. Performance Targets

| Metric | Target | Implementation |
| :--- | :--- | :--- |
| Log operation | <100ms | Async queue, background processing |
| Query audit (1K entries) | <500ms | Indexed queries, pagination |
| Export audit (1K entries) | <5s | Streaming output, background job |
| Undo operation | <2s | Version service rollback |

---

## 7. Compliance & Security

### 7.1 Data Retention

- Keep audit logs for minimum 1 year (configurable)
- Archive older logs separately
- Support data subject requests (GDPR)

### 7.2 Access Control

- Only authorized users can query audit logs
- Admin-only export of sensitive data
- Sensitive data redaction in exports

### 7.3 Immutability

- Audit entries cannot be modified
- Only soft delete allowed
- Cryptographic verification optional (enterprise)

---

## 8. Testing

### 8.1 Unit Tests

- Audit entry creation
- Filter logic
- Statistics calculation
- Export formatting

### 8.2 Integration Tests

- Logging from disambiguation service
- Logging from merge operations
- Logging from duplicate review
- Query and filter accuracy
- Undo chain tracking

### 8.3 Compliance Tests

- Data retention policies
- Access control enforcement
- Export redaction
- GDPR subject access requests

---

## 9. License Gating

| Tier | Audit Logging | Query | Export | Compliance |
|:-----|:------:|:------:|:-------:|:-----:|
| **Core** | ✗ | ✗ | ✗ | ✗ |
| **WriterPro** | ✗ | ✗ | ✗ | ✗ |
| **Teams** | ✓ | ✓ | CSV, JSON | Basic |
| **Enterprise** | ✓ | ✓ | CSV, JSON, PDF | Full GDPR |

---

## 10. Acceptance Criteria

| # | Given | When | Then |
|:---|:------|:------|:------|
| 1 | User selects entity in disambiguation | Choice made | Audit entry created with mention/selection/user |
| 2 | Merge executed via bulk UI | Merge completes | Audit entry records entities, relationships, claims |
| 3 | Duplicate group reviewed | Group marked reviewed | Audit entry records group, user, reason |
| 4 | Audit query with date filter | Query executed | Only entries in date range returned |
| 5 | Merge undone | Undo executed | Undo recorded in audit with reason |
| 6 | Export as CSV | Export requested | CSV downloads with all audit fields |
| 7 | Query audit for entity | Query executed | All resolutions involving entity shown |
| 8 | Statistics requested | GetStatisticsAsync called | Counts accurate for all entry types |

---

## 11. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design spec - Audit logging, queries, export, undo tracking |
