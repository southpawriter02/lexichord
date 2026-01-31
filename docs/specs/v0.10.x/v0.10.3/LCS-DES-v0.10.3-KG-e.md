# LCS-DES-v0.10.3-KG-e: Design Specification — Bulk Resolution UI

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-v0.10.3e` | Entity Resolution sub-part e |
| **Feature Name** | `Bulk Resolution UI` | Review and resolve duplicate entities at scale |
| **Target Version** | `v0.10.3e` | Fifth sub-part of v0.10.3-KG |
| **Module Scope** | `Lexichord.Modules.CKVS.Resolution` | Entity Resolution module |
| **Swimlane** | `Entity Resolution` | Knowledge graph vertical |
| **License Tier** | `Teams` | Available in Teams tier and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.EntityResolution` | Entity resolution feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.3-KG](./LCS-SBD-v0.10.3-KG.md) | Entity Resolution scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.3-KG S2.1](./LCS-SBD-v0.10.3-KG.md#21-sub-parts) | e = Bulk Resolution UI |

---

## 2. Executive Summary

### 2.1 The Requirement

Provide a user interface for reviewing and resolving duplicate entity groups at scale:

1. Display all duplicate groups with filtering and sorting
2. Show group details with confidence scores
3. Preview merge operations before execution
4. Execute single or batch merge operations
5. Track resolution progress
6. Export resolution reports

### 2.2 The Proposed Solution

Implement UI components and service endpoints:

1. **DuplicateGroupListComponent:** Display all groups with filtering
2. **DuplicateGroupDetailComponent:** Show group details and merge preview
3. **BulkResolutionController:** API endpoints for group listing and merge operations
4. **ResolutionProgressService:** Track and report resolution progress
5. Integration with Duplicate Detector (Part B) and Entity Merger (Part C)

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IDuplicateDetector` | v0.10.3b | Retrieve duplicate groups for display |
| `IEntityMerger` | v0.10.3c | Execute merge operations |
| `IGraphRepository` | v0.4.5e | Entity metadata and details |
| `IMediator` | v0.0.7a | Event publishing for merge completion |
| `IResolutionAuditService` | v0.10.3f | Log all user actions |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| (Framework default) | | UI components and API endpoints use standard libraries |

### 3.2 Licensing Behavior

- **Core Tier:** Not available
- **WriterPro Tier:** Not available
- **Teams Tier:** Full bulk resolution UI with all features
- **Enterprise Tier:** Full bulk resolution UI with advanced filtering and export

---

## 4. Data Contract (The API)

### 4.1 IBulkResolutionService Interface

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Provides bulk resolution operations for duplicate entity groups.
/// Handles group listing, filtering, and batch merge operations.
/// </summary>
public interface IBulkResolutionService
{
    /// <summary>
    /// Gets paginated list of duplicate groups for UI display.
    /// Supports filtering by type, confidence, and resolution status.
    /// </summary>
    /// <param name="filter">Filter and pagination options</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated group list</returns>
    Task<PagedResult<DuplicateGroupSummary>> GetGroupsAsync(
        DuplicateGroupFilter filter,
        CancellationToken ct = default);

    /// <summary>
    /// Gets detailed information for a specific duplicate group.
    /// Includes all candidate entities with metadata.
    /// </summary>
    /// <param name="groupId">ID of group to retrieve</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Group detail with entity information</returns>
    Task<DuplicateGroupDetail> GetGroupDetailAsync(
        Guid groupId,
        CancellationToken ct = default);

    /// <summary>
    /// Marks a group as reviewed without merging.
    /// </summary>
    /// <param name="groupId">ID of group to mark reviewed</param>
    /// <param name="reason">Optional reason for skipping</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Completion task</returns>
    Task MarkGroupReviewedAsync(
        Guid groupId,
        string? reason = null,
        CancellationToken ct = default);

    /// <summary>
    /// Marks groups as false duplicates (not actually duplicates).
    /// </summary>
    /// <param name="groupId">ID of group to mark as non-duplicate</param>
    /// <param name="reason">Optional reason for rejection</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Completion task</returns>
    Task MarkNotDuplicatesAsync(
        Guid groupId,
        string? reason = null,
        CancellationToken ct = default);

    /// <summary>
    /// Executes merge for a single group.
    /// </summary>
    /// <param name="groupId">ID of group to merge</param>
    /// <param name="primaryEntityId">ID of primary entity in group</param>
    /// <param name="options">Merge options (conflict resolution, aliases)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Merge result</returns>
    Task<MergeResult> MergeGroupAsync(
        Guid groupId,
        Guid primaryEntityId,
        MergeOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Executes batch merge operations for multiple groups.
    /// </summary>
    /// <param name="operations">List of groups to merge with primary entity selection</param>
    /// <param name="options">Batch merge options</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Batch result with progress and outcomes</returns>
    Task<BatchMergeResult> MergeBatchAsync(
        IReadOnlyList<BulkMergeOperation> operations,
        BatchMergeOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets current resolution progress statistics.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Progress summary</returns>
    Task<ResolutionProgress> GetProgressAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Exports resolution report in specified format.
    /// </summary>
    /// <param name="format">Export format (CSV, JSON, PDF)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Export stream with report data</returns>
    Task<Stream> ExportReportAsync(
        ReportFormat format,
        CancellationToken ct = default);
}
```

### 4.2 DuplicateGroupFilter Record

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Filtering and pagination options for duplicate group listing.
/// </summary>
public record DuplicateGroupFilter
{
    /// <summary>
    /// Filter by entity type (optional).
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Filter by minimum confidence score (0-1).
    /// </summary>
    public float? MinConfidence { get; init; }

    /// <summary>
    /// Filter by maximum confidence score (0-1).
    /// </summary>
    public float? MaxConfidence { get; init; }

    /// <summary>
    /// Filter by resolution status.
    /// </summary>
    public DuplicateResolutionStatus? Status { get; init; }

    /// <summary>
    /// Filter by duplicate type.
    /// </summary>
    public DuplicateType? Type { get; init; }

    /// <summary>
    /// Page number for pagination (1-based).
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Items per page.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Sort field (confidence, created, type).
    /// </summary>
    public string? SortBy { get; init; } = "confidence";

    /// <summary>
    /// Sort order (asc, desc).
    /// </summary>
    public SortOrder SortOrder { get; init; } = SortOrder.Descending;

    /// <summary>
    /// Search text for entity name filtering.
    /// </summary>
    public string? SearchText { get; init; }
}
```

### 4.3 DuplicateGroupSummary Record

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Summary of a duplicate group for list view.
/// </summary>
public record DuplicateGroupSummary
{
    /// <summary>
    /// Unique group ID.
    /// </summary>
    public required Guid GroupId { get; init; }

    /// <summary>
    /// Entity type of all members in group.
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Number of entities in this group.
    /// </summary>
    public int EntityCount { get; init; }

    /// <summary>
    /// Confidence score for group (0-1).
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Type of duplication detected.
    /// </summary>
    public DuplicateType DuplicateType { get; init; }

    /// <summary>
    /// Resolution status.
    /// </summary>
    public DuplicateResolutionStatus Status { get; init; }

    /// <summary>
    /// ID of suggested primary entity.
    /// </summary>
    public Guid? SuggestedPrimaryId { get; init; }

    /// <summary>
    /// Name of suggested primary entity.
    /// </summary>
    public string? SuggestedPrimaryName { get; init; }

    /// <summary>
    /// Total relationships that would be transferred.
    /// </summary>
    public int RelationshipCount { get; init; }

    /// <summary>
    /// Total claims that would be transferred.
    /// </summary>
    public int ClaimCount { get; init; }

    /// <summary>
    /// When this group was detected.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When this group was last reviewed/updated.
    /// </summary>
    public DateTimeOffset? LastReviewedAt { get; init; }
}
```

### 4.4 DuplicateGroupDetail Record

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Detailed view of a duplicate group with all candidate entities.
/// </summary>
public record DuplicateGroupDetail
{
    /// <summary>
    /// Group metadata.
    /// </summary>
    public required DuplicateGroupSummary Summary { get; init; }

    /// <summary>
    /// All entities in this group with full metadata.
    /// </summary>
    public IReadOnlyList<DuplicateEntityDetail> Entities { get; init; } = [];

    /// <summary>
    /// Merge preview if primary entity is selected.
    /// </summary>
    public MergePreview? MergePreview { get; init; }

    /// <summary>
    /// Notes or comments on this group.
    /// </summary>
    public string? Notes { get; init; }
}
```

### 4.5 DuplicateEntityDetail Record

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Detailed information about an entity in a duplicate group.
/// </summary>
public record DuplicateEntityDetail
{
    /// <summary>
    /// Entity ID.
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// Entity name.
    /// </summary>
    public required string EntityName { get; init; }

    /// <summary>
    /// Entity type.
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Whether this entity is the suggested primary.
    /// </summary>
    public bool IsSuggestedPrimary { get; init; }

    /// <summary>
    /// Confidence score for this entity as duplicate (0-1).
    /// </summary>
    public float ConfidenceScore { get; init; }

    /// <summary>
    /// Number of relationships connected to this entity.
    /// </summary>
    public int RelationshipCount { get; init; }

    /// <summary>
    /// Number of claims made about this entity.
    /// </summary>
    public int ClaimCount { get; init; }

    /// <summary>
    /// Number of documents referencing this entity.
    /// </summary>
    public int DocumentCount { get; init; }

    /// <summary>
    /// When entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When entity was last modified.
    /// </summary>
    public DateTimeOffset LastModified { get; init; }

    /// <summary>
    /// Summary of properties/attributes.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Properties { get; init; }
}
```

### 4.6 BulkMergeOperation Record

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Configuration for a single merge operation in batch.
/// </summary>
public record BulkMergeOperation
{
    /// <summary>
    /// Group ID to merge.
    /// </summary>
    public required Guid GroupId { get; init; }

    /// <summary>
    /// ID of primary entity to keep.
    /// </summary>
    public required Guid PrimaryEntityId { get; init; }

    /// <summary>
    /// Merge options for this specific operation.
    /// If null, uses batch defaults.
    /// </summary>
    public MergeOptions? Options { get; init; }
}
```

### 4.7 BatchMergeResult Record

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Result of a batch merge operation.
/// </summary>
public record BatchMergeResult
{
    /// <summary>
    /// Total operations requested.
    /// </summary>
    public int TotalOperations { get; init; }

    /// <summary>
    /// Number of successful merges.
    /// </summary>
    public int SuccessfulMerges { get; init; }

    /// <summary>
    /// Number of failed merges.
    /// </summary>
    public int FailedMerges { get; init; }

    /// <summary>
    /// Details of each merge result.
    /// </summary>
    public IReadOnlyList<SingleMergeOutcome> Outcomes { get; init; } = [];

    /// <summary>
    /// Total duration of batch operation.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Message summarizing the batch result.
    /// </summary>
    public string Message { get; init; } = "";
}
```

### 4.8 SingleMergeOutcome Record

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Outcome of a single merge within a batch operation.
/// </summary>
public record SingleMergeOutcome
{
    /// <summary>
    /// Group ID that was processed.
    /// </summary>
    public required Guid GroupId { get; init; }

    /// <summary>
    /// Whether merge was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Result of merge if successful.
    /// </summary>
    public MergeResult? Result { get; init; }

    /// <summary>
    /// Error message if merge failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// When this merge was executed.
    /// </summary>
    public DateTimeOffset ExecutedAt { get; init; }
}
```

### 4.9 ResolutionProgress Record

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Progress summary for bulk resolution operations.
/// </summary>
public record ResolutionProgress
{
    /// <summary>
    /// Total duplicate groups found.
    /// </summary>
    public int TotalGroups { get; init; }

    /// <summary>
    /// Groups that have been resolved (merged or marked non-duplicate).
    /// </summary>
    public int ResolvedGroups { get; init; }

    /// <summary>
    /// Groups pending resolution.
    /// </summary>
    public int PendingGroups { get; init; }

    /// <summary>
    /// Groups marked as reviewed but not merged.
    /// </summary>
    public int ReviewedGroups { get; init; }

    /// <summary>
    /// Total entities that would be merged.
    /// </summary>
    public int TotalEntitiesToMerge { get; init; }

    /// <summary>
    /// Percentage complete (0-100).
    /// </summary>
    public int PercentComplete { get; init; }

    /// <summary>
    /// Timestamp of last update.
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; }

    /// <summary>
    /// Estimated time remaining (if running).
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }
}
```

### 4.10 DuplicateResolutionStatus Enum

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Current resolution status of a duplicate group.
/// </summary>
public enum DuplicateResolutionStatus
{
    /// <summary>Not yet reviewed.</summary>
    Pending = 1,

    /// <summary>User reviewed and determined not duplicates.</summary>
    Rejected = 2,

    /// <summary>User reviewed but deferred action.</summary>
    Reviewed = 3,

    /// <summary>Scheduled for merge.</summary>
    ScheduledForMerge = 4,

    /// <summary>Merge executed successfully.</summary>
    Merged = 5,

    /// <summary>Merge execution failed.</summary>
    MergeFailed = 6
}
```

### 4.11 ReportFormat Enum

```csharp
namespace Lexichord.Modules.CKVS.Resolution.Contracts;

/// <summary>
/// Export format for resolution reports.
/// </summary>
public enum ReportFormat
{
    /// <summary>Comma-separated values.</summary>
    Csv = 1,

    /// <summary>JSON format.</summary>
    Json = 2,

    /// <summary>Portable Document Format.</summary>
    Pdf = 3,

    /// <summary>Excel workbook.</summary>
    Excel = 4
}
```

---

## 5. UI Components

### 5.1 DuplicateGroupListComponent

Displays paginated list of duplicate groups with:
- Filter controls (type, confidence, status)
- Sort options (confidence, created date, entity type)
- Search box for entity name
- Progress summary
- Batch action buttons (Merge All >90%, Mark All Reviewed, Export)

### 5.2 DuplicateGroupDetailComponent

Shows detailed view of selected group:
- Entity cards for each group member
- Confidence score and match type
- Relationship and claim counts
- Suggested primary entity
- Merge preview area
- Action buttons (Merge, Not Duplicates, Mark Reviewed)

### 5.3 MergePreviewComponent

Displays merge preview:
- Relationship transfers visualization
- Claim transfers list
- Property conflict resolution UI
- Aliases to be created
- Reversibility notice
- Merge/Cancel buttons

### 5.4 BatchMergeProgressComponent

Shows batch operation progress:
- Progress bar with percentage
- Current operation status
- List of completed merges
- List of pending merges
- Any errors/failures
- Cancel operation button (if allowed)

---

## 6. Implementation Strategy

### 6.1 API Endpoints

| Method | Endpoint | Purpose |
| :--- | :--- | :--- |
| `GET` | `/api/resolution/groups` | List duplicate groups with filter/sort |
| `GET` | `/api/resolution/groups/{id}` | Get group detail |
| `POST` | `/api/resolution/groups/{id}/reviewed` | Mark group reviewed |
| `POST` | `/api/resolution/groups/{id}/not-duplicates` | Mark as non-duplicate |
| `POST` | `/api/resolution/groups/{id}/merge` | Merge single group |
| `POST` | `/api/resolution/batch-merge` | Execute batch merge |
| `GET` | `/api/resolution/progress` | Get progress stats |
| `GET` | `/api/resolution/report` | Export report |

### 6.2 State Management

UI maintains:
- Selected group for detail view
- Applied filters
- Batch operation selection
- Batch operation progress/status

Backend maintains:
- Duplicate groups cache (updated on scan)
- Resolution status for each group
- User actions/audit trail

---

## 7. Performance Targets

| Metric | Target | Implementation |
| :--- | :--- | :--- |
| List load (100 groups) | <500ms | Pagination, efficient filtering |
| Detail load | <200ms | Cached entity metadata |
| Batch merge (10 groups) | <20s | Parallel where safe, progress tracking |
| Report export (1K groups) | <10s | Streaming output, background processing |

---

## 8. Error Handling

### 8.1 Merge Conflicts During Batch

If a group cannot be merged:
- Log error with details
- Record failure in batch result
- Continue with next group
- Notify user of failures in results

### 8.2 Concurrent Modifications

If group is modified during review:
- Refresh group data from DB
- Show user notification
- Allow user to proceed or cancel

### 8.3 Export Errors

If export fails:
- Log error
- Return error message to user
- Allow retry

---

## 9. Testing

### 9.1 Unit Tests

- Filter logic for group listing
- Pagination calculations
- Batch operation sequencing
- Progress calculation

### 9.2 Integration Tests

- Full group listing with filters
- Group detail loading
- Single merge execution
- Batch merge with mixed success/failure
- Report export in all formats

### 9.3 UI Tests

- Component rendering with test data
- Filter/sort interaction
- Merge preview accuracy
- Batch progress display
- Error message display

---

## 10. License Gating

| Tier | Bulk UI | Batch Operations | Export Report |
|:-----|:-------:|:----------------:|:-------------:|
| **Core** | ✗ | ✗ | ✗ |
| **WriterPro** | ✗ | ✗ | ✗ |
| **Teams** | ✓ | ✓ | CSV, JSON |
| **Enterprise** | ✓ | ✓ | CSV, JSON, PDF, Excel |

---

## 11. Acceptance Criteria

| # | Given | When | Then |
|:---|:------|:------|:------|
| 1 | 12 duplicate groups found | Bulk UI opened | All groups listed with confidence scores |
| 2 | Filters applied (type, confidence) | List rendered | Only matching groups displayed |
| 3 | Group selected in list | Detail view opened | All entities shown with metadata |
| 4 | Primary entity selected | Merge preview calculated | Relationships, claims, conflicts shown |
| 5 | "Merge" clicked on group | Merge executed | Progress shown, group marked resolved |
| 6 | 10 groups selected for batch | Batch merge started | Progress tracked, all merged in <20s |
| 7 | Merge fails for one group | Batch continues | Error recorded, other groups merged |
| 8 | Export report requested | CSV selected | Report downloads with group details |

---

## 12. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design spec - Bulk Resolution UI with list, detail, batch merge, and export |
