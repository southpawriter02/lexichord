// -----------------------------------------------------------------------
// <copyright file="SyncWorkflowStep.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Concrete implementation of ISyncWorkflowStep. Synchronizes document
//   state with the knowledge graph via ISyncService. Execution flow:
//     1. Check if step is enabled (return auto-pass if disabled)
//     2. Check SkipIfValidationFailed against prior step results
//     3. Build SyncWorkflowContext from ValidationWorkflowContext
//     4. Delegate to ExecuteSyncAsync for the actual sync operation
//     5. ExecuteSyncAsync builds infrastructure SyncContext and calls ISyncService
//     6. Map SyncResult to SyncStepResult with audit logs
//     7. Map SyncStepResult to WorkflowStepResult for workflow engine
//
// v0.7.7g: Sync Step Type (CKVS Phase 4d)
// Dependencies: ISyncWorkflowStep (v0.7.7g), IWorkflowStep (v0.7.7e),
//               ISyncService (v0.7.6e), ValidationWorkflowContext (v0.7.7e),
//               WorkflowStepResult (v0.7.7e), SyncStepResult (v0.7.7g),
//               SyncWorkflowContext (v0.7.7g), ConflictStrategy (v0.7.7g),
//               SyncDirection (v0.7.7g), SyncChange (v0.7.7g),
//               SyncStepConflict (v0.7.7g), Document (v0.4.1c)
// -----------------------------------------------------------------------

using System.Diagnostics;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Concrete implementation of <see cref="ISyncWorkflowStep"/>.
/// </summary>
/// <remarks>
/// <para>
/// Sync steps enable document-to-knowledge-graph synchronization within
/// validation workflows. The step delegates to <see cref="ISyncService"/>
/// from the sync infrastructure (v0.7.6e) and maps the results to
/// workflow-compatible result types.
/// </para>
/// <para>
/// <b>Execution Flow:</b>
/// <list type="number">
///   <item>If disabled, return success with auto-pass message.</item>
///   <item>If <see cref="SkipIfValidationFailed"/> and prior steps failed, skip.</item>
///   <item>Build <see cref="SyncWorkflowContext"/> from <see cref="ValidationWorkflowContext"/>.</item>
///   <item>Delegate to <see cref="ExecuteSyncAsync"/> for the actual sync.</item>
///   <item>Map <see cref="SyncStepResult"/> to <see cref="WorkflowStepResult"/>.</item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe. The mutable
/// <see cref="IsEnabled"/> property is serialized by the workflow engine.
/// </para>
/// <para>
/// <b>Performance Targets:</b>
/// <list type="bullet">
///   <item><description>Sync request creation: &lt; 100ms</description></item>
///   <item><description>Large document sync: &lt; TimeoutMs (default 60s)</description></item>
///   <item><description>Conflict detection: &lt; 1s per conflict</description></item>
///   <item><description>Result compilation: &lt; 500ms</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7g as part of Sync Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
public class SyncWorkflowStep : ISyncWorkflowStep
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncWorkflowStep> _logger;

    // ── IWorkflowStep Properties ────────────────────────────────────────

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string? Description { get; }

    /// <inheritdoc/>
    public int Order { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// Mutable to allow runtime enable/disable. The workflow engine
    /// serializes step execution, so concurrent mutation is not a concern.
    /// </remarks>
    public bool IsEnabled { get; set; }

    /// <inheritdoc/>
    public int? TimeoutMs { get; }

    // ── ISyncWorkflowStep Properties ────────────────────────────────────

    /// <inheritdoc/>
    public SyncDirection Direction { get; }

    /// <inheritdoc/>
    public ConflictStrategy ConflictStrategy { get; }

    /// <inheritdoc/>
    public bool SkipIfValidationFailed { get; }

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncWorkflowStep"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for this sync step.</param>
    /// <param name="name">Human-readable display name.</param>
    /// <param name="direction">Synchronization direction.</param>
    /// <param name="syncService">
    /// The sync service for delegating document-graph sync operations.
    /// </param>
    /// <param name="logger">Logger for sync step diagnostics.</param>
    /// <param name="description">Optional description of the step's purpose.</param>
    /// <param name="order">Execution order within the workflow (default: 0).</param>
    /// <param name="timeoutMs">
    /// Execution timeout in milliseconds (default: 60000ms = 1 minute).
    /// </param>
    /// <param name="conflictStrategy">
    /// Conflict resolution strategy (default: <see cref="ConflictStrategy.PreferNewer"/>).
    /// </param>
    /// <param name="skipIfValidationFailed">
    /// Whether to skip sync when prior validation steps have failed (default: true).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="syncService"/> or <paramref name="logger"/> is null.
    /// </exception>
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
        IsEnabled = true;
        TimeoutMs = timeoutMs ?? 60000;
        Direction = direction;
        ConflictStrategy = conflictStrategy;
        SkipIfValidationFailed = skipIfValidationFailed;

        _syncService = syncService ??
            throw new ArgumentNullException(nameof(syncService));
        _logger = logger ??
            throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug(
            "SyncWorkflowStep created: Id={StepId}, Name={StepName}, " +
            "Direction={Direction}, ConflictStrategy={ConflictStrategy}, " +
            "SkipIfValidationFailed={SkipIfValidationFailed}, " +
            "TimeoutMs={TimeoutMs}, Order={Order}",
            Id, Name, Direction, ConflictStrategy,
            SkipIfValidationFailed, TimeoutMs, Order);
    }

    // ── ValidateConfiguration ───────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// Validates:
    /// <list type="bullet">
    ///   <item><description>Id must not be empty</description></item>
    ///   <item><description>Name must not be empty</description></item>
    ///   <item><description>TimeoutMs (if set) must be positive</description></item>
    /// </list>
    /// </remarks>
    public IReadOnlyList<ValidationConfigurationError> ValidateConfiguration()
    {
        var errors = new List<ValidationConfigurationError>();

        // LOGIC: Validate step identity
        if (string.IsNullOrWhiteSpace(Id))
        {
            errors.Add(new ValidationConfigurationError(
                "Sync step ID cannot be empty.",
                nameof(Id)));
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(new ValidationConfigurationError(
                "Sync step name cannot be empty.",
                nameof(Name)));
        }

        // LOGIC: Validate timeout
        if (TimeoutMs.HasValue && TimeoutMs.Value <= 0)
        {
            errors.Add(new ValidationConfigurationError(
                "Timeout must be a positive value in milliseconds.",
                nameof(TimeoutMs)));
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning(
                "Sync step '{StepId}' configuration validation failed with {ErrorCount} errors",
                Id, errors.Count);
        }
        else
        {
            _logger.LogDebug(
                "Sync step '{StepId}' configuration validation passed", Id);
        }

        return errors;
    }

    // ── ExecuteSyncAsync ────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// <para>Sync execution steps:</para>
    /// <list type="number">
    ///   <item>Build timestamped audit log.</item>
    ///   <item>Create infrastructure <see cref="SyncContext"/> from workflow context.</item>
    ///   <item>Create a <see cref="Document"/> reference for the sync service.</item>
    ///   <item>Call <see cref="ISyncService.SyncDocumentToGraphAsync"/>.</item>
    ///   <item>Map <see cref="SyncResult"/> to <see cref="SyncStepResult"/>.</item>
    ///   <item>Log completion metrics.</item>
    /// </list>
    /// </remarks>
    public async Task<SyncStepResult> ExecuteSyncAsync(
        SyncWorkflowContext context,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var logs = new List<string>();

        try
        {
            // LOGIC: Start audit trail
            logs.Add($"[{DateTime.UtcNow:O}] Starting sync step: {Id}");
            logs.Add($"Direction: {Direction}, Strategy: {ConflictStrategy}");
            logs.Add($"Document ID: {context.DocumentId}, Workspace: {context.WorkspaceId}");

            _logger.LogInformation(
                "Starting sync step '{StepId}' (direction: {Direction}, " +
                "strategy: {ConflictStrategy}, document: {DocumentId})",
                Id, Direction, ConflictStrategy, context.DocumentId);

            // LOGIC: Build infrastructure SyncContext for ISyncService
            var syncContext = new Abstractions.Contracts.Knowledge.Sync.SyncContext
            {
                UserId = context.UserId ?? Guid.Empty,
                Document = new Document(
                    Id: Guid.TryParse(context.DocumentId, out var docGuid) ? docGuid : Guid.NewGuid(),
                    ProjectId: context.WorkspaceId,
                    FilePath: context.DocumentId,
                    Title: context.DocumentId,
                    Hash: string.Empty,
                    Status: DocumentStatus.Indexed,
                    IndexedAt: DateTime.UtcNow,
                    FailureReason: null),
                WorkspaceId = context.WorkspaceId,
                AutoResolveConflicts = ConflictStrategy != ConflictStrategy.Manual
                    && ConflictStrategy != ConflictStrategy.FailOnConflict,
                DefaultConflictStrategy = ConflictStrategy.ToResolutionStrategy(),
                PublishEvents = true,
                Timeout = TimeoutMs.HasValue
                    ? TimeSpan.FromMilliseconds(TimeoutMs.Value)
                    : TimeSpan.FromMinutes(5)
            };

            logs.Add($"[{DateTime.UtcNow:O}] Built sync context, delegating to ISyncService");

            // LOGIC: Delegate to ISyncService
            var syncResult = await _syncService.SyncDocumentToGraphAsync(
                syncContext.Document,
                syncContext,
                ct);

            stopwatch.Stop();

            // LOGIC: Map SyncResult to SyncStepResult
            var success = syncResult.Status == SyncOperationStatus.Success
                || syncResult.Status == SyncOperationStatus.SuccessWithConflicts
                || syncResult.Status == SyncOperationStatus.NoChanges;

            var itemsSynced = syncResult.EntitiesAffected.Count
                + syncResult.ClaimsAffected.Count
                + syncResult.RelationshipsAffected.Count;

            var conflictsDetected = syncResult.Conflicts.Count;

            // LOGIC: Map infrastructure SyncConflicts to workflow SyncStepConflicts
            var unresolvedConflicts = syncResult.Conflicts.Select(c => new SyncStepConflict
            {
                EntityId = Guid.NewGuid(),
                EntityType = "KnowledgeEntity",
                Property = c.ConflictTarget,
                DocumentValue = c.DocumentValue,
                GraphValue = c.GraphValue,
                DocumentModifiedTime = DateTime.UtcNow,
                GraphModifiedTime = c.DetectedAt.UtcDateTime,
                Notes = c.Description
            }).ToList();

            // LOGIC: Map affected items to SyncChange records
            var changes = new List<SyncChange>();
            foreach (var entity in syncResult.EntitiesAffected)
            {
                changes.Add(new SyncChange
                {
                    ChangeType = SyncChangeType.Updated,
                    EntityId = entity.Id,
                    EntityType = "KnowledgeEntity",
                    Direction = Direction,
                    Timestamp = DateTime.UtcNow
                });
            }

            logs.Add($"[{DateTime.UtcNow:O}] Sync completed: {itemsSynced} items synced");
            logs.Add($"Conflicts: {conflictsDetected} detected, " +
                     $"{conflictsDetected - unresolvedConflicts.Count} resolved");
            logs.Add($"Changes: {changes.Count}");
            logs.Add($"Status: {syncResult.Status}");

            _logger.LogInformation(
                "Sync step '{StepId}' completed: {ItemsSynced} items, " +
                "{ConflictsDetected} conflicts, status={Status}, " +
                "duration={DurationMs}ms",
                Id, itemsSynced, conflictsDetected,
                syncResult.Status, stopwatch.ElapsedMilliseconds);

            return new SyncStepResult
            {
                StepId = Id,
                Success = success,
                Direction = Direction,
                ItemsSynced = itemsSynced,
                ConflictsDetected = conflictsDetected,
                ConflictsResolved = conflictsDetected - unresolvedConflicts.Count,
                UnresolvedConflicts = unresolvedConflicts,
                Changes = changes,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                SyncLogs = logs,
                StatusMessage = success
                    ? $"Sync completed: {itemsSynced} items synced"
                    : syncResult.ErrorMessage ?? "Sync failed",
                Metadata = new Dictionary<string, object>
                {
                    ["direction"] = Direction.ToString(),
                    ["conflictStrategy"] = ConflictStrategy.ToString(),
                    ["changeCount"] = changes.Count,
                    ["syncStatus"] = syncResult.Status.ToString()
                }
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Sync step '{StepId}' was cancelled (timeout or user cancellation)",
                Id);
            stopwatch.Stop();

            logs.Add($"[{DateTime.UtcNow:O}] ERROR: Sync was cancelled");

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
            _logger.LogError(ex,
                "Sync step '{StepId}' failed with exception: {ErrorMessage}",
                Id, ex.Message);
            stopwatch.Stop();

            logs.Add($"[{DateTime.UtcNow:O}] ERROR: {ex.Message}");

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

    // ── ExecuteAsync (IWorkflowStep) ────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// <para>Orchestrates the full sync lifecycle:</para>
    /// <list type="number">
    ///   <item>If disabled, return success with auto-pass message.</item>
    ///   <item>If <see cref="SkipIfValidationFailed"/> and prior steps have errors, skip.</item>
    ///   <item>Create timeout-linked <see cref="CancellationTokenSource"/>.</item>
    ///   <item>Build <see cref="SyncWorkflowContext"/> from <see cref="ValidationWorkflowContext"/>.</item>
    ///   <item>Delegate to <see cref="ExecuteSyncAsync"/>.</item>
    ///   <item>Map <see cref="SyncStepResult"/> to <see cref="WorkflowStepResult"/>.</item>
    /// </list>
    /// </remarks>
    public async Task<WorkflowStepResult> ExecuteAsync(
        ValidationWorkflowContext context,
        CancellationToken ct = default)
    {
        // LOGIC: Disabled steps return success without execution
        if (!IsEnabled)
        {
            _logger.LogInformation(
                "Sync step '{StepId}' is disabled, returning auto-pass", Id);

            return new WorkflowStepResult
            {
                StepId = Id,
                Success = true,
                Message = "Sync step is disabled"
            };
        }

        // LOGIC: Check if we should skip due to prior validation failures
        if (SkipIfValidationFailed && HasValidationErrors(context))
        {
            _logger.LogInformation(
                "Skipping sync step '{StepId}' due to validation failures " +
                "in prior steps", Id);

            return new WorkflowStepResult
            {
                StepId = Id,
                Success = true,
                Message = "Skipped due to validation failures"
            };
        }

        try
        {
            // LOGIC: Create linked cancellation with timeout
            using var cts = TimeoutMs.HasValue
                ? new CancellationTokenSource(TimeoutMs.Value)
                : new CancellationTokenSource();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

            // LOGIC: Build SyncWorkflowContext from ValidationWorkflowContext
            var syncContext = new SyncWorkflowContext
            {
                WorkspaceId = context.WorkspaceId,
                DocumentId = context.DocumentId,
                DocumentContent = context.DocumentContent,
                UserId = context.UserId,
                WorkflowId = context.WorkflowId,
                ConflictStrategy = ConflictStrategy,
                ForceFull = false
            };

            // LOGIC: Delegate to sync-specific execution
            var result = await ExecuteSyncAsync(syncContext, linked.Token);

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
            _logger.LogWarning(
                "Sync step '{StepId}' timed out during workflow execution", Id);

            return new WorkflowStepResult
            {
                StepId = Id,
                Success = false,
                Message = "Sync step timed out"
            };
        }
    }

    // ── Private Helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Checks whether any prior validation step reported errors.
    /// </summary>
    /// <param name="context">The validation workflow context.</param>
    /// <returns>
    /// <c>true</c> if any previous result has <see cref="ValidationStepResult.IsValid"/>
    /// set to <c>false</c>; <c>false</c> otherwise.
    /// </returns>
    private bool HasValidationErrors(ValidationWorkflowContext context)
    {
        if (context.PreviousResults is null || context.PreviousResults.Count == 0)
        {
            return false;
        }

        // LOGIC: Check if any prior step reported invalid results
        return context.PreviousResults.Any(r => !r.IsValid);
    }
}
