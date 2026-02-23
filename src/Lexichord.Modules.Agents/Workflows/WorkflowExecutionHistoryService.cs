// -----------------------------------------------------------------------
// <copyright file="WorkflowExecutionHistoryService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// In-memory implementation of <see cref="IWorkflowExecutionHistoryService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tracks workflow execution history using a thread-safe <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// keyed by execution ID. History is bounded to <see cref="MaxHistoryEntries"/> (100) entries,
/// with the oldest entries trimmed on each new recording.
/// </para>
/// <para>
/// <b>Threading:</b> All operations are thread-safe. The internal collection uses
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> for lock-free reads and writes.
/// Trimming uses a lock to prevent race conditions during eviction.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7d as part of the Workflow Execution UI.
/// </para>
/// </remarks>
/// <seealso cref="IWorkflowExecutionHistoryService"/>
/// <seealso cref="WorkflowExecutionSummary"/>
/// <seealso cref="WorkflowExecutionStatistics"/>
public sealed class WorkflowExecutionHistoryService : IWorkflowExecutionHistoryService
{
    // ── Dependencies ────────────────────────────────────────────────────

    /// <summary>Logger for recording diagnostic information.</summary>
    private readonly ILogger<WorkflowExecutionHistoryService> _logger;

    // ── Internal State ──────────────────────────────────────────────────

    /// <summary>
    /// Thread-safe dictionary of execution summaries keyed by execution ID.
    /// </summary>
    /// <remarks>
    /// Using ConcurrentDictionary for lock-free reads. The dictionary is trimmed
    /// after each insert to enforce the maximum entry limit.
    /// </remarks>
    private readonly ConcurrentDictionary<string, WorkflowExecutionSummary> _history = new();

    /// <summary>Lock object for trimming operations to prevent concurrent eviction races.</summary>
    private readonly object _trimLock = new();

    // ── Constants ────────────────────────────────────────────────────────

    /// <summary>
    /// Maximum number of history entries to retain. Oldest entries are evicted
    /// when this limit is exceeded.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #24: History > 100 → New entry → Oldest entries trimmed.
    /// </remarks>
    internal const int MaxHistoryEntries = 100;

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowExecutionHistoryService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is <c>null</c>.</exception>
    public WorkflowExecutionHistoryService(ILogger<WorkflowExecutionHistoryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("WorkflowExecutionHistoryService initialized (v0.7.7d)");
    }

    // ── IWorkflowExecutionHistoryService Implementation ─────────────────

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC:
    /// <list type="number">
    ///   <item><description>
    ///     Creates a <see cref="WorkflowExecutionSummary"/> from the <paramref name="result"/>,
    ///     truncating <see cref="WorkflowExecutionResult.FinalOutput"/> to 100 characters for
    ///     compact display.
    ///   </description></item>
    ///   <item><description>
    ///     Inserts the summary into the thread-safe dictionary keyed by execution ID.
    ///   </description></item>
    ///   <item><description>
    ///     If the dictionary exceeds <see cref="MaxHistoryEntries"/>, trims the oldest entries
    ///     under a lock to prevent concurrent eviction races.
    ///   </description></item>
    /// </list>
    /// </remarks>
    public Task RecordAsync(
        WorkflowExecutionResult result,
        string workflowName,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // STEP 1: Create summary with truncated output preview
        var summary = new WorkflowExecutionSummary(
            result.ExecutionId,
            result.WorkflowId,
            workflowName,
            DateTime.UtcNow,
            result.TotalDuration,
            result.Status,
            result.StepResults.Count(s => s.Status == WorkflowStepStatus.Completed),
            result.StepResults.Count,
            result.TotalUsage.TotalTokens,
            result.FinalOutput?.Length > 100 ? result.FinalOutput[..100] + "..." : result.FinalOutput
        );

        // STEP 2: Insert into history
        _history[summary.ExecutionId] = summary;

        _logger.LogDebug(
            "Recorded workflow execution: {ExecutionId} for workflow {WorkflowId} ({Status})",
            summary.ExecutionId, summary.WorkflowId, summary.Status);

        // STEP 3: Trim if over limit
        TrimHistory();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Snapshots the dictionary values, optionally filters by workflow ID,
    /// sorts by <see cref="WorkflowExecutionSummary.ExecutedAt"/> descending (most recent first),
    /// and takes up to <paramref name="limit"/> entries.
    /// </remarks>
    public Task<IReadOnlyList<WorkflowExecutionSummary>> GetHistoryAsync(
        string? workflowId = null,
        int limit = 10,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // STEP 1: Snapshot and filter
        var query = _history.Values.AsEnumerable();

        if (workflowId is not null)
        {
            query = query.Where(h => h.WorkflowId == workflowId);
        }

        // STEP 2: Sort by most recent first, take limit
        var result = query
            .OrderByDescending(h => h.ExecutedAt)
            .Take(limit)
            .ToList();

        _logger.LogDebug(
            "Retrieved {Count} history entries (workflowId={WorkflowId}, limit={Limit})",
            result.Count, workflowId ?? "all", limit);

        return Task.FromResult<IReadOnlyList<WorkflowExecutionSummary>>(result);
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Filters history entries by <paramref name="workflowId"/>, then computes
    /// aggregated statistics. Returns zero-valued statistics when no entries exist.
    /// </remarks>
    public Task<WorkflowExecutionStatistics> GetStatisticsAsync(
        string workflowId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var entries = _history.Values
            .Where(h => h.WorkflowId == workflowId)
            .ToList();

        // CASE: No entries → return zero stats
        if (entries.Count == 0)
        {
            _logger.LogDebug("No execution history found for workflow {WorkflowId}", workflowId);

            return Task.FromResult(new WorkflowExecutionStatistics(
                0, 0, 0, 0, 0,
                TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero,
                0, 0, null, null));
        }

        // STEP 1: Count by status
        var successful = entries.Count(e => e.Status == WorkflowExecutionStatus.Completed);
        var failed = entries.Count(e => e.Status == WorkflowExecutionStatus.Failed);
        var cancelled = entries.Count(e => e.Status == WorkflowExecutionStatus.Cancelled);

        // STEP 2: Compute duration statistics
        var avgDuration = TimeSpan.FromSeconds(entries.Average(e => e.Duration.TotalSeconds));
        var minDuration = entries.Min(e => e.Duration);
        var maxDuration = entries.Max(e => e.Duration);

        // STEP 3: Compute token statistics
        var avgTokens = (int)entries.Average(e => e.TotalTokens);
        var totalTokens = entries.Sum(e => e.TotalTokens);

        var stats = new WorkflowExecutionStatistics(
            TotalExecutions: entries.Count,
            SuccessfulExecutions: successful,
            FailedExecutions: failed,
            CancelledExecutions: cancelled,
            SuccessRate: (double)successful / entries.Count,
            AverageDuration: avgDuration,
            MinDuration: minDuration,
            MaxDuration: maxDuration,
            AverageTokensPerExecution: avgTokens,
            TotalTokensUsed: totalTokens,
            FirstExecution: entries.Min(e => e.ExecutedAt),
            LastExecution: entries.Max(e => e.ExecutedAt));

        _logger.LogDebug(
            "Computed statistics for workflow {WorkflowId}: {Total} executions, {SuccessRate:P0} success rate",
            workflowId, stats.TotalExecutions, stats.SuccessRate);

        return Task.FromResult(stats);
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Identifies all entries with <see cref="WorkflowExecutionSummary.ExecutedAt"/>
    /// before <paramref name="olderThan"/> and removes them from the dictionary.
    /// </remarks>
    public Task ClearHistoryAsync(DateTime olderThan, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var keysToRemove = _history
            .Where(kvp => kvp.Value.ExecutedAt < olderThan)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _history.TryRemove(key, out _);
        }

        _logger.LogDebug(
            "Cleared {Count} history entries older than {CutoffDate:u}",
            keysToRemove.Count, olderThan);

        return Task.CompletedTask;
    }

    // ── Private Helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Trims the history to <see cref="MaxHistoryEntries"/> by removing the oldest entries.
    /// </summary>
    /// <remarks>
    /// LOGIC: Uses a lock to prevent concurrent trim operations from racing.
    /// Sorts entries by date ascending and removes the excess from the head.
    /// </remarks>
    private void TrimHistory()
    {
        if (_history.Count <= MaxHistoryEntries) return;

        lock (_trimLock)
        {
            // Re-check inside lock (double-check pattern)
            if (_history.Count <= MaxHistoryEntries) return;

            var toRemove = _history
                .OrderBy(kvp => kvp.Value.ExecutedAt)
                .Take(_history.Count - MaxHistoryEntries)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                _history.TryRemove(key, out _);
            }

            _logger.LogDebug("Trimmed {Count} oldest history entries", toRemove.Count);
        }
    }
}
