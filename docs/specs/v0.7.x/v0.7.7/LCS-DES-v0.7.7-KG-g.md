# LCS-DES-077-KG-g: Sync Step Type

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-077-KG-g |
| **System Breakdown** | LCS-SBD-077-KG |
| **Version** | v0.7.7 |
| **Codename** | Sync Step Type (CKVS Phase 4d) |
| **Estimated Hours** | 3 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Sync Step Type** enables document-to-knowledge-graph synchronization within validation workflows. This step triggers bidirectional synchronization, ensuring validated documents update graph entities and vice versa, maintaining consistency across the knowledge management system.

### 1.2 Key Responsibilities

- Trigger document-to-graph or graph-to-document synchronization
- Support bidirectional sync operations
- Handle conflict resolution strategies
- Track sync status and outcomes
- Integrate with ISyncService
- Support async execution in workflows
- Log synchronization details

### 1.3 Module Location

```
src/
  Lexichord.Workflows/
    Validation/
      Steps/
        ISyncWorkflowStep.cs
        SyncWorkflowStep.cs
        SyncDirection.cs
        ConflictStrategy.cs
```

---

## 2. Interface Definitions

### 2.1 Sync Workflow Step Interface

```csharp
namespace Lexichord.Workflows.Validation.Steps;

/// <summary>
/// Sync step for workflow engine.
/// </summary>
public interface ISyncWorkflowStep : IWorkflowStep
{
    /// <summary>
    /// Synchronization direction.
    /// </summary>
    SyncDirection Direction { get; }

    /// <summary>
    /// Conflict resolution strategy.
    /// </summary>
    ConflictStrategy ConflictStrategy { get; }

    /// <summary>
    /// Whether to skip if validation failed.
    /// </summary>
    bool SkipIfValidationFailed { get; }

    /// <summary>
    /// Executes synchronization.
    /// </summary>
    Task<SyncStepResult> ExecuteSyncAsync(
        Document document,
        SyncContext context,
        CancellationToken ct = default);
}
```

---

## 3. Data Types

### 3.1 Sync Direction Enum

```csharp
/// <summary>
/// Direction of synchronization.
/// </summary>
public enum SyncDirection
{
    /// <summary>Sync document changes to knowledge graph.</summary>
    DocumentToGraph = 0,

    /// <summary>Sync graph changes to document.</summary>
    GraphToDocument = 1,

    /// <summary>Bidirectional synchronization.</summary>
    Bidirectional = 2
}
```

### 3.2 Conflict Strategy Enum

```csharp
/// <summary>
/// Strategy for resolving sync conflicts.
/// </summary>
public enum ConflictStrategy
{
    /// <summary>Keep document version.</summary>
    PreferDocument = 0,

    /// <summary>Keep graph version.</summary>
    PreferGraph = 1,

    /// <summary>Keep most recently modified.</summary>
    PreferNewer = 2,

    /// <summary>Merge both versions.</summary>
    Merge = 3,

    /// <summary>Fail on conflict.</summary>
    FailOnConflict = 4,

    /// <summary>Manual resolution required.</summary>
    Manual = 5
}
```

### 3.3 Sync Step Result

```csharp
/// <summary>
/// Result of synchronization step.
/// </summary>
public record SyncStepResult
{
    /// <summary>Step identifier.</summary>
    public required string StepId { get; init; }

    /// <summary>Whether sync succeeded.</summary>
    public required bool Success { get; init; }

    /// <summary>Sync direction used.</summary>
    public SyncDirection Direction { get; init; }

    /// <summary>Number of items synced.</summary>
    public int ItemsSynced { get; init; }

    /// <summary>Number of conflicts detected.</summary>
    public int ConflictsDetected { get; init; }

    /// <summary>Number of conflicts resolved.</summary>
    public int ConflictsResolved { get; init; }

    /// <summary>Conflicts that require manual resolution.</summary>
    public IReadOnlyList<SyncConflict> UnresolvedConflicts { get; init; } = [];

    /// <summary>Changes made during sync.</summary>
    public IReadOnlyList<SyncChange> Changes { get; init; } = [];

    /// <summary>Execution time in milliseconds.</summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>Detailed sync logs.</summary>
    public IReadOnlyList<string> SyncLogs { get; init; } = [];

    /// <summary>Status message.</summary>
    public string? StatusMessage { get; init; }

    /// <summary>Metadata about sync.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
```

### 3.4 Sync Context

```csharp
/// <summary>
/// Context for sync operations.
/// </summary>
public record SyncContext
{
    /// <summary>Workspace ID.</summary>
    public required Guid WorkspaceId { get; init; }

    /// <summary>Document being synced.</summary>
    public required Document Document { get; init; }

    /// <summary>User ID initiating sync.</summary>
    public Guid? UserId { get; init; }

    /// <summary>Workflow ID.</summary>
    public Guid? WorkflowId { get; init; }

    /// <summary>Force sync even if no changes.</summary>
    public bool ForceFull { get; init; } = false;

    /// <summary>Conflict resolution strategy.</summary>
    public ConflictStrategy ConflictStrategy { get; init; }

    /// <summary>Metadata context.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
```

### 3.5 Sync Conflict

```csharp
/// <summary>
/// Conflict detected during synchronization.
/// </summary>
public record SyncConflict
{
    /// <summary>Conflicting entity ID.</summary>
    public required Guid EntityId { get; init; }

    /// <summary>Entity type.</summary>
    public required string EntityType { get; init; }

    /// <summary>Property that conflicts.</summary>
    public required string Property { get; init; }

    /// <summary>Document version of property.</summary>
    public required object DocumentValue { get; init; }

    /// <summary>Graph version of property.</summary>
    public required object GraphValue { get; init; }

    /// <summary>When document was last modified.</summary>
    public DateTime DocumentModifiedTime { get; init; }

    /// <summary>When graph was last modified.</summary>
    public DateTime GraphModifiedTime { get; init; }

    /// <summary>Resolution applied (if any).</summary>
    public SyncConflictResolution? Resolution { get; init; }

    /// <summary>Notes about conflict.</summary>
    public string? Notes { get; init; }
}

public record SyncConflictResolution
{
    /// <summary>Strategy used to resolve.</summary>
    public ConflictStrategy Strategy { get; init; }

    /// <summary>Value that was chosen.</summary>
    public required object ResolvedValue { get; init; }

    /// <summary>Resolution timestamp.</summary>
    public DateTime ResolvedAt { get; init; }
}
```

### 3.6 Sync Change

```csharp
/// <summary>
/// Single change made during sync.
/// </summary>
public record SyncChange
{
    /// <summary>Change type.</summary>
    public SyncChangeType ChangeType { get; init; }

    /// <summary>Entity ID affected.</summary>
    public Guid EntityId { get; init; }

    /// <summary>Entity type.</summary>
    public string? EntityType { get; init; }

    /// <summary>Property changed (if applicable).</summary>
    public string? Property { get; init; }

    /// <summary>Previous value.</summary>
    public object? PreviousValue { get; init; }

    /// <summary>New value.</summary>
    public object? NewValue { get; init; }

    /// <summary>Direction of change.</summary>
    public SyncDirection Direction { get; init; }

    /// <summary>Timestamp of change.</summary>
    public DateTime Timestamp { get; init; }
}

public enum SyncChangeType
{
    Created,
    Updated,
    Deleted,
    Merged,
    Linked
}
```

---

## 4. Implementation

### 4.1 Sync Workflow Step

```csharp
public class SyncWorkflowStep : ISyncWorkflowStep
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncWorkflowStep> _logger;

    public string Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public int Order { get; }
    public bool IsEnabled { get; set; }
    public int? TimeoutMs { get; }

    public SyncDirection Direction { get; }
    public ConflictStrategy ConflictStrategy { get; }
    public bool SkipIfValidationFailed { get; }

    public SyncWorkflowStep(
        string id,
        string name,
        SyncDirection direction,
        ISyncService syncService,
        ILogger<SyncWorkflowStep> logger,
        string? description = null,
        int order = 0,
        int? timeoutMs = null,
        ConflictStrategy conflictStrategy = ConflictStrategy.PreferNewer,
        bool skipIfValidationFailed = true)
    {
        Id = id;
        Name = name;
        Description = description;
        Order = order;
        TimeoutMs = timeoutMs ?? 60000;
        Direction = direction;
        ConflictStrategy = conflictStrategy;
        SkipIfValidationFailed = skipIfValidationFailed;

        _syncService = syncService;
        _logger = logger;
    }

    public IReadOnlyList<ValidationError> ValidateConfiguration()
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(Id))
            errors.Add(new ValidationError { Message = "Step ID cannot be empty" });

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add(new ValidationError { Message = "Step name cannot be empty" });

        if (TimeoutMs.HasValue && TimeoutMs.Value <= 0)
            errors.Add(new ValidationError { Message = "Timeout must be positive" });

        return errors;
    }

    public async Task<SyncStepResult> ExecuteSyncAsync(
        Document document,
        SyncContext context,
        CancellationToken ct = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var logs = new List<string>();

        try
        {
            logs.Add($"[{DateTime.UtcNow:O}] Starting sync step: {Id}");
            logs.Add($"Direction: {Direction}, Strategy: {ConflictStrategy}");
            logs.Add($"Document ID: {document.Id}, Workspace: {context.WorkspaceId}");

            // Execute sync operation
            var syncRequest = new SyncRequest
            {
                DocumentId = document.Id,
                WorkspaceId = context.WorkspaceId,
                Direction = Direction,
                ConflictStrategy = ConflictStrategy,
                ForceFull = context.ForceFull,
                UserId = context.UserId
            };

            var syncResult = await _syncService.SyncAsync(syncRequest, ct);

            stopwatch.Stop();

            logs.Add($"Sync completed: {syncResult.ItemsSynced} items synced");
            logs.Add($"Conflicts: {syncResult.ConflictsDetected} detected, {syncResult.ConflictsResolved} resolved");
            logs.Add($"Changes: {syncResult.Changes.Count}");

            _logger.LogInformation(
                "Sync step {StepId} completed: {ItemsSynced} items, {Conflicts} conflicts",
                Id, syncResult.ItemsSynced, syncResult.ConflictsDetected);

            return new SyncStepResult
            {
                StepId = Id,
                Success = syncResult.Success,
                Direction = Direction,
                ItemsSynced = syncResult.ItemsSynced,
                ConflictsDetected = syncResult.ConflictsDetected,
                ConflictsResolved = syncResult.ConflictsResolved,
                UnresolvedConflicts = syncResult.UnresolvedConflicts,
                Changes = syncResult.Changes,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                SyncLogs = logs,
                StatusMessage = syncResult.StatusMessage,
                Metadata = new Dictionary<string, object>
                {
                    ["direction"] = Direction.ToString(),
                    ["conflictStrategy"] = ConflictStrategy.ToString(),
                    ["changeCount"] = syncResult.Changes.Count
                }
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Sync step {StepId} was cancelled", Id);
            stopwatch.Stop();

            logs.Add($"ERROR: Sync was cancelled");

            return new SyncStepResult
            {
                StepId = Id,
                Success = false,
                Direction = Direction,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                SyncLogs = logs,
                StatusMessage = "Sync step timed out"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync step {StepId} failed", Id);
            stopwatch.Stop();

            logs.Add($"ERROR: {ex.Message}");

            return new SyncStepResult
            {
                StepId = Id,
                Success = false,
                Direction = Direction,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                SyncLogs = logs,
                StatusMessage = $"Sync failed: {ex.Message}"
            };
        }
    }

    public async Task<WorkflowStepResult> ExecuteAsync(
        WorkflowContext context,
        CancellationToken ct = default)
    {
        if (!IsEnabled)
        {
            return new WorkflowStepResult
            {
                StepId = Id,
                Success = true,
                Message = "Sync step is disabled"
            };
        }

        // Check if we should skip due to validation failure
        if (SkipIfValidationFailed && context.HasErrors)
        {
            _logger.LogInformation(
                "Skipping sync step {StepId} due to validation failures",
                Id);

            return new WorkflowStepResult
            {
                StepId = Id,
                Success = true,
                Message = "Skipped due to validation failures"
            };
        }

        try
        {
            using var cts = TimeoutMs.HasValue
                ? new CancellationTokenSource(TimeoutMs.Value)
                : new CancellationTokenSource();

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

            var syncContext = new SyncContext
            {
                WorkspaceId = context.WorkspaceId,
                Document = context.CurrentDocument,
                UserId = context.UserId,
                WorkflowId = context.WorkflowId,
                ConflictStrategy = ConflictStrategy,
                ForceFull = false
            };

            var result = await ExecuteSyncAsync(
                context.CurrentDocument,
                syncContext,
                linked.Token);

            return new WorkflowStepResult
            {
                StepId = Id,
                Success = result.Success,
                Message = result.Success
                    ? $"Sync completed: {result.ItemsSynced} items synced"
                    : result.StatusMessage,
                Data = new Dictionary<string, object>
                {
                    ["syncResult"] = result
                }
            };
        }
        catch (OperationCanceledException)
        {
            return new WorkflowStepResult
            {
                StepId = Id,
                Success = false,
                Message = "Sync step timed out"
            };
        }
    }
}
```

### 4.2 Sync Step Factory

```csharp
public class SyncWorkflowStepFactory
{
    private readonly ISyncService _syncService;
    private readonly ILoggerFactory _loggerFactory;

    public SyncWorkflowStepFactory(
        ISyncService syncService,
        ILoggerFactory loggerFactory)
    {
        _syncService = syncService;
        _loggerFactory = loggerFactory;
    }

    public ISyncWorkflowStep CreateStep(
        string id,
        string name,
        SyncWorkflowStepOptions options)
    {
        return new SyncWorkflowStep(
            id: id,
            name: name,
            direction: options.Direction,
            syncService: _syncService,
            logger: _loggerFactory.CreateLogger<SyncWorkflowStep>(),
            description: options.Description,
            order: options.Order,
            timeoutMs: options.TimeoutMs,
            conflictStrategy: options.ConflictStrategy,
            skipIfValidationFailed: options.SkipIfValidationFailed);
    }
}

/// <summary>
/// Configuration for creating sync workflow steps.
/// </summary>
public record SyncWorkflowStepOptions
{
    public string? Description { get; init; }
    public int Order { get; init; } = 0;
    public int? TimeoutMs { get; init; } = 60000;
    public SyncDirection Direction { get; init; } = SyncDirection.Bidirectional;
    public ConflictStrategy ConflictStrategy { get; init; } = ConflictStrategy.PreferNewer;
    public bool SkipIfValidationFailed { get; init; } = true;
}
```

---

## 5. Sync Execution Flow

```
[Workflow Execution]
        |
        v
[SyncWorkflowStep.ExecuteAsync]
        |
        +---> [Check if Enabled]
        |
        +---> [Check SkipIfValidationFailed]
        |
        +---> [Build SyncRequest]
        |
        +---> [Execute via ISyncService]
        |     (async operation)
        |
        +---> [Collect Changes]
        |
        +---> [Handle Conflicts]
        |     +---> [Apply ConflictStrategy]
        |     +---> [Log Resolutions]
        |
        +---> [Create SyncStepResult]
        |
        +---> [Return WorkflowStepResult]
        |
        +---> [Log Results]
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Sync service unavailable | Return failure with message |
| Document not found | Return failure |
| Conflicts unresolvable | Collect unresolved conflicts |
| Sync timeout | Cancel operation, return timeout error |
| Cancelled operation | Propagate cancellation |
| Invalid conflict strategy | Use PreferNewer default |

---

## 7. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `CreateStep_ValidOptions` | Step created successfully |
| `ValidateConfiguration_Valid` | Config validation passes |
| `ExecuteSync_DocumentToGraph` | D2G sync works |
| `ExecuteSync_GraphToDocument` | G2D sync works |
| `ExecuteSync_Bidirectional` | Bidirectional sync works |
| `ExecuteSync_PreferDocument` | Document preference works |
| `ExecuteSync_PreferGraph` | Graph preference works |
| `ExecuteSync_PreferNewer` | Newer version preference works |
| `ExecuteSync_Merge` | Merge strategy works |
| `ExecuteSync_ConflictDetected` | Conflicts detected |
| `ExecuteSync_SkipOnValidationFailure` | Skip logic works |
| `ExecuteSync_Timeout` | Timeout enforced |

---

## 8. Performance Considerations

| Aspect | Target |
| :------ | :------ |
| Sync request creation | < 100ms |
| Large document sync | < TimeoutMs (default 60s) |
| Conflict detection | < 1s per conflict |
| Result compilation | < 500ms |
| Memory per sync | < 200MB |

---

## 9. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | DocumentToGraph only, basic strategies |
| Teams | All directions + conflict strategies |
| Enterprise | Full + custom sync handlers + conflict resolution |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
