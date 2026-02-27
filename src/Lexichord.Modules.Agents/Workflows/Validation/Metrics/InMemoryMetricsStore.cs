// -----------------------------------------------------------------------
// <copyright file="InMemoryMetricsStore.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Workflows.Validation.Metrics;

/// <summary>
/// In-memory stub implementation of <see cref="IMetricsStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="ConcurrentBag{T}"/> collections for thread-safe storage.
/// Data is not persisted across application restarts. A future version will
/// add SQLite-based time-series storage.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §4.1
/// </para>
/// </remarks>
internal sealed class InMemoryMetricsStore : IMetricsStore
{
    // ── Fields ───────────────────────────────────────────────────────────

    private readonly ConcurrentBag<WorkflowExecutionMetrics> _executions = new();
    private readonly ConcurrentBag<ValidationStepMetrics> _steps = new();
    private readonly ConcurrentBag<GatingDecisionMetrics> _gatingDecisions = new();
    private readonly ConcurrentBag<SyncOperationMetrics> _syncOperations = new();
    private readonly ILogger<InMemoryMetricsStore> _logger;

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of <see cref="InMemoryMetricsStore"/>.
    /// </summary>
    /// <param name="logger">Logger for recording storage operations.</param>
    public InMemoryMetricsStore(ILogger<InMemoryMetricsStore> logger)
    {
        _logger = logger;
        _logger.LogDebug("InMemoryMetricsStore initialized (non-persistent stub)");
    }

    // ── Store Methods ───────────────────────────────────────────────────

    /// <inheritdoc />
    public Task StoreWorkflowExecutionAsync(
        WorkflowExecutionMetrics metrics,
        CancellationToken ct = default)
    {
        _executions.Add(metrics);
        _logger.LogDebug(
            "Stored workflow execution: {ExecutionId} ({WorkflowId})",
            metrics.ExecutionId, metrics.WorkflowId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StoreValidationStepAsync(
        ValidationStepMetrics metrics,
        CancellationToken ct = default)
    {
        _steps.Add(metrics);
        _logger.LogDebug(
            "Stored validation step: {StepId} ({StepType})",
            metrics.StepId, metrics.StepType);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StoreGatingDecisionAsync(
        GatingDecisionMetrics metrics,
        CancellationToken ct = default)
    {
        _gatingDecisions.Add(metrics);
        _logger.LogDebug(
            "Stored gating decision: {GateId} ({Decision})",
            metrics.GateId, metrics.Passed ? "PASS" : "BLOCK");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StoreSyncOperationAsync(
        SyncOperationMetrics metrics,
        CancellationToken ct = default)
    {
        _syncOperations.Add(metrics);
        _logger.LogDebug(
            "Stored sync operation: {Direction} ({Status})",
            metrics.Direction, metrics.Success ? "SUCCESS" : "FAILED");
        return Task.CompletedTask;
    }

    // ── Query Methods ───────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<IReadOnlyList<WorkflowExecutionMetrics>> GetWorkflowExecutionsAsync(
        MetricsQuery query,
        CancellationToken ct = default)
    {
        // LOGIC: Apply filters from the query to the in-memory collection.
        var results = _executions
            .Where(e => e.WorkspaceId == query.WorkspaceId)
            .Where(e => query.StartTime == null || e.ExecutedAt >= query.StartTime)
            .Where(e => query.EndTime == null || e.ExecutedAt <= query.EndTime)
            .Where(e => query.DocumentId == null || e.DocumentId == query.DocumentId)
            .Where(e => query.WorkflowId == null || e.WorkflowId == query.WorkflowId)
            .Where(e => query.Trigger == null || e.Trigger == query.Trigger)
            .OrderByDescending(e => e.ExecutedAt)
            .Take(query.Limit ?? 1000)
            .ToList();

        _logger.LogDebug(
            "GetWorkflowExecutionsAsync: returning {Count} records",
            results.Count);

        return Task.FromResult<IReadOnlyList<WorkflowExecutionMetrics>>(results);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ValidationStepMetrics>> GetValidationStepsAsync(
        MetricsQuery query,
        CancellationToken ct = default)
    {
        // LOGIC: Filter by execution IDs that match the workspace,
        // then apply time range and type filters.
        var executionIds = _executions
            .Where(e => e.WorkspaceId == query.WorkspaceId)
            .Select(e => e.ExecutionId)
            .ToHashSet();

        var results = _steps
            .Where(s => executionIds.Contains(s.ExecutionId))
            .Where(s => query.StartTime == null || s.ExecutedAt >= query.StartTime)
            .Where(s => query.EndTime == null || s.ExecutedAt <= query.EndTime)
            .Where(s => query.ValidationType == null || s.StepType == query.ValidationType)
            .OrderByDescending(s => s.ExecutedAt)
            .Take(query.Limit ?? 1000)
            .ToList();

        _logger.LogDebug(
            "GetValidationStepsAsync: returning {Count} records",
            results.Count);

        return Task.FromResult<IReadOnlyList<ValidationStepMetrics>>(results);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<GatingDecisionMetrics>> GetGatingDecisionsAsync(
        MetricsQuery query,
        CancellationToken ct = default)
    {
        var executionIds = _executions
            .Where(e => e.WorkspaceId == query.WorkspaceId)
            .Select(e => e.ExecutionId)
            .ToHashSet();

        var results = _gatingDecisions
            .Where(g => executionIds.Contains(g.ExecutionId))
            .Where(g => query.StartTime == null || g.ExecutedAt >= query.StartTime)
            .Where(g => query.EndTime == null || g.ExecutedAt <= query.EndTime)
            .OrderByDescending(g => g.ExecutedAt)
            .Take(query.Limit ?? 1000)
            .ToList();

        _logger.LogDebug(
            "GetGatingDecisionsAsync: returning {Count} records",
            results.Count);

        return Task.FromResult<IReadOnlyList<GatingDecisionMetrics>>(results);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SyncOperationMetrics>> GetSyncOperationsAsync(
        MetricsQuery query,
        CancellationToken ct = default)
    {
        var executionIds = _executions
            .Where(e => e.WorkspaceId == query.WorkspaceId)
            .Select(e => e.ExecutionId)
            .ToHashSet();

        var results = _syncOperations
            .Where(s => executionIds.Contains(s.ExecutionId))
            .Where(s => query.StartTime == null || s.ExecutedAt >= query.StartTime)
            .Where(s => query.EndTime == null || s.ExecutedAt <= query.EndTime)
            .OrderByDescending(s => s.ExecutedAt)
            .Take(query.Limit ?? 1000)
            .ToList();

        _logger.LogDebug(
            "GetSyncOperationsAsync: returning {Count} records",
            results.Count);

        return Task.FromResult<IReadOnlyList<SyncOperationMetrics>>(results);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<WorkflowExecutionMetrics>> GetDocumentMetricsAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        var results = _executions
            .Where(e => e.DocumentId == documentId)
            .OrderByDescending(e => e.ExecutedAt)
            .ToList();

        _logger.LogDebug(
            "GetDocumentMetricsAsync: {DocumentId} -> {Count} records",
            documentId, results.Count);

        return Task.FromResult<IReadOnlyList<WorkflowExecutionMetrics>>(results);
    }

    /// <inheritdoc />
    public Task StoreHealthScoresAsync(
        WorkspaceHealthScore workspaceHealth,
        DocumentHealthScore documentHealth,
        CancellationToken ct = default)
    {
        // LOGIC: In-memory stub — health scores are not persisted.
        // The HealthScoreCalculator recalculates on demand.
        _logger.LogDebug(
            "StoreHealthScoresAsync: workspace={WorkspaceId} score={WScore}, document={DocumentId} score={DScore}",
            workspaceHealth.WorkspaceId, workspaceHealth.HealthScore,
            documentHealth.DocumentId, documentHealth.HealthScore);

        return Task.CompletedTask;
    }
}
