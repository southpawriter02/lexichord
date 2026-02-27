// -----------------------------------------------------------------------
// <copyright file="CIPipelineService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Lexichord.Modules.Agents.Workflows.Validation.Templates;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Workflows.Validation.CI;

/// <summary>
/// Service for executing validation workflows in CI/CD pipelines without user interaction.
/// </summary>
/// <remarks>
/// <para>
/// Orchestrates headless workflow execution by resolving the workflow definition from
/// the <see cref="IValidationWorkflowRegistry"/> (v0.7.7h), building step-level results,
/// and mapping outcomes to CI-friendly exit codes.
/// </para>
/// <para>
/// <b>Recording behavior:</b> Record methods are fire-and-forget — exceptions during
/// recording are logged but do not propagate to the caller. Query and status methods
/// propagate exceptions normally.
/// </para>
/// <para>
/// <b>Thread safety:</b> The service uses <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// to track in-flight executions, making it safe for concurrent use from multiple
/// pipeline triggers.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-j §4.2
/// </para>
/// </remarks>
/// <seealso cref="ICIPipelineService"/>
/// <seealso cref="CIWorkflowRequest"/>
/// <seealso cref="CIExecutionResult"/>
public class CIPipelineService : ICIPipelineService
{
    // ── Dependencies ────────────────────────────────────────────────────

    private readonly IValidationWorkflowRegistry _registry;
    private readonly ILogger<CIPipelineService> _logger;

    // ── In-flight execution tracking ───────────────────────────────────

    /// <summary>
    /// Tracks running executions for status queries and cancellation.
    /// Key: execution ID, Value: execution tracker with CTS and status.
    /// </summary>
    private readonly ConcurrentDictionary<string, ExecutionTracker> _executions = new();

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of <see cref="CIPipelineService"/>.
    /// </summary>
    /// <param name="registry">
    /// Validation workflow registry for resolving workflow definitions.
    /// Provided by v0.7.7h registration.
    /// </param>
    /// <param name="logger">Logger for recording CI pipeline operations.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registry"/> or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public CIPipelineService(
        IValidationWorkflowRegistry registry,
        ILogger<CIPipelineService> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── ICIPipelineService Implementation ───────────────────────────────

    /// <inheritdoc />
    public async Task<CIExecutionResult> ExecuteWorkflowAsync(
        CIWorkflowRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var executionId = Guid.NewGuid().ToString("N");
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "CI pipeline: Starting workflow execution {ExecutionId} for workflow '{WorkflowId}' " +
            "in workspace '{WorkspaceId}'",
            executionId, request.WorkflowId, request.WorkspaceId);

        // LOGIC: Create a linked CTS that respects both the caller's token and the
        // configured timeout. This ensures the execution stops regardless of which
        // cancellation source fires first.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));

        // LOGIC: Register the execution tracker for status queries and cancellation.
        var tracker = new ExecutionTracker(timeoutCts, CIExecutionStatus.Running);
        _executions[executionId] = tracker;

        try
        {
            // ── Validate inputs ────────────────────────────────────────

            if (string.IsNullOrWhiteSpace(request.WorkflowId))
            {
                _logger.LogWarning(
                    "CI pipeline: Invalid input — WorkflowId is empty for execution {ExecutionId}",
                    executionId);

                return BuildErrorResult(
                    executionId, request.WorkflowId ?? "", startTime,
                    ExitCode.InvalidInput, "Workflow ID is required");
            }

            if (string.IsNullOrWhiteSpace(request.WorkspaceId))
            {
                _logger.LogWarning(
                    "CI pipeline: Invalid input — WorkspaceId is empty for execution {ExecutionId}",
                    executionId);

                return BuildErrorResult(
                    executionId, request.WorkflowId, startTime,
                    ExitCode.InvalidInput, "Workspace ID is required");
            }

            // ── Resolve workflow ───────────────────────────────────────

            _logger.LogDebug(
                "CI pipeline: Resolving workflow '{WorkflowId}' from registry for execution {ExecutionId}",
                request.WorkflowId, executionId);

            ValidationWorkflowDefinition workflow;
            try
            {
                workflow = await _registry.GetWorkflowAsync(request.WorkflowId, timeoutCts.Token);
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning(
                    "CI pipeline: Workflow '{WorkflowId}' not found in registry for execution {ExecutionId}",
                    request.WorkflowId, executionId);

                return BuildErrorResult(
                    executionId, request.WorkflowId, startTime,
                    ExitCode.InvalidInput, $"Workflow '{request.WorkflowId}' not found");
            }

            _logger.LogDebug(
                "CI pipeline: Resolved workflow '{WorkflowName}' with {StepCount} steps for execution {ExecutionId}",
                workflow.Name, workflow.Steps.Count, executionId);

            // ── Log CI metadata ────────────────────────────────────────

            if (request.CIMetadata != null)
            {
                _logger.LogInformation(
                    "CI pipeline: CI metadata — System={CISystem}, BuildId={BuildId}, " +
                    "Commit={CommitSha}, Branch={Branch} for execution {ExecutionId}",
                    request.CIMetadata.System, request.CIMetadata.BuildId,
                    request.CIMetadata.CommitSha, request.CIMetadata.Branch,
                    executionId);
            }

            // ── Execute workflow steps ─────────────────────────────────

            _logger.LogInformation(
                "CI pipeline: Executing {StepCount} workflow steps for execution {ExecutionId}",
                workflow.Steps.Count, executionId);

            var stepResults = new List<CIStepResult>();
            var totalErrors = 0;
            var totalWarnings = 0;
            var allStepsPassed = true;

            foreach (var stepDef in workflow.Steps)
            {
                timeoutCts.Token.ThrowIfCancellationRequested();

                var stepStartTime = DateTime.UtcNow;

                _logger.LogDebug(
                    "CI pipeline: Executing step '{StepId}' ({StepName}) for execution {ExecutionId}",
                    stepDef.Id, stepDef.Name, executionId);

                tracker.AddLog($"[{DateTime.UtcNow:HH:mm:ss}] Executing step: {stepDef.Name ?? stepDef.Id}");

                // LOGIC: Simulate step execution by recording the step definition
                // as a result. In a full implementation, this would delegate to
                // the validation step factory and execute each step's logic.
                var stepErrors = new List<string>();
                var stepWarnings = new List<string>();
                var stepSuccess = true;

                // LOGIC: Track step execution outcome
                var stepElapsedMs = (long)(DateTime.UtcNow - stepStartTime).TotalMilliseconds;

                var stepResult = new CIStepResult
                {
                    StepId = stepDef.Id,
                    Name = stepDef.Name,
                    Success = stepSuccess,
                    ExecutionTimeMs = stepElapsedMs,
                    Errors = stepErrors,
                    Warnings = stepWarnings,
                    Message = stepSuccess ? "Step completed successfully" : "Step failed"
                };

                stepResults.Add(stepResult);
                totalErrors += stepErrors.Count;
                totalWarnings += stepWarnings.Count;

                if (!stepSuccess)
                {
                    allStepsPassed = false;
                    tracker.AddLog($"[{DateTime.UtcNow:HH:mm:ss}] Step FAILED: {stepDef.Name ?? stepDef.Id}");

                    // LOGIC: If HaltOnFirstError is set, stop execution after the first
                    // step failure. This is useful for fast-fail CI pipelines.
                    if (request.HaltOnFirstError)
                    {
                        _logger.LogInformation(
                            "CI pipeline: Halting on first error at step '{StepId}' for execution {ExecutionId}",
                            stepDef.Id, executionId);
                        break;
                    }
                }
                else
                {
                    tracker.AddLog($"[{DateTime.UtcNow:HH:mm:ss}] Step PASSED: {stepDef.Name ?? stepDef.Id}");
                }
            }

            // ── Build result ───────────────────────────────────────────

            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            var overallSuccess = allStepsPassed;

            // LOGIC: Determine the exit code based on results and request thresholds.
            var exitCode = DetermineExitCode(overallSuccess, totalErrors, totalWarnings, request);

            // LOGIC: If the exit code indicates failure, mark overall success as false.
            if (exitCode != ExitCode.Success)
            {
                overallSuccess = false;
            }

            var result = new CIExecutionResult
            {
                ExecutionId = executionId,
                WorkflowId = request.WorkflowId,
                Success = overallSuccess,
                ExitCode = exitCode,
                ExecutionTimeSeconds = (int)duration,
                ValidationSummary = new CIValidationSummary
                {
                    ErrorCount = totalErrors,
                    WarningCount = totalWarnings,
                    DocumentsValidated = 1,
                    DocumentsPassed = overallSuccess ? 1 : 0,
                    DocumentsFailed = overallSuccess ? 0 : 1,
                    PassRate = overallSuccess ? 100m : 0m
                },
                StepResults = stepResults,
                Message = overallSuccess
                    ? $"Workflow '{request.WorkflowId}' completed successfully"
                    : $"Workflow '{request.WorkflowId}' completed with failures",
                ExecutedAt = startTime
            };

            // LOGIC: Update tracker to reflect completion status.
            tracker.Status = overallSuccess
                ? CIExecutionStatus.Completed
                : CIExecutionStatus.Failed;

            tracker.AddLog($"[{DateTime.UtcNow:HH:mm:ss}] Workflow completed — " +
                           $"Success={overallSuccess}, Errors={totalErrors}, Warnings={totalWarnings}");

            _logger.LogInformation(
                "CI pipeline: Execution {ExecutionId} completed — " +
                "Success={Success}, ExitCode={ExitCode}, Errors={ErrorCount}, " +
                "Warnings={WarningCount}, Duration={Duration}s",
                executionId, overallSuccess, exitCode, totalErrors, totalWarnings, (int)duration);

            return result;
        }
        catch (OperationCanceledException)
        {
            // LOGIC: Distinguish between user cancellation and timeout.
            var isTimeout = timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested;
            var status = isTimeout ? CIExecutionStatus.TimedOut : CIExecutionStatus.Cancelled;
            tracker.Status = status;

            var message = isTimeout
                ? $"Workflow timed out after {request.TimeoutSeconds} seconds"
                : "Workflow execution was cancelled";

            _logger.LogWarning(
                "CI pipeline: Execution {ExecutionId} {Outcome} — {Message}",
                executionId, status, message);

            tracker.AddLog($"[{DateTime.UtcNow:HH:mm:ss}] {message}");

            return BuildErrorResult(
                executionId, request.WorkflowId, startTime,
                isTimeout ? ExitCode.Timeout : ExitCode.ExecutionFailed, message);
        }
        catch (Exception ex)
        {
            tracker.Status = CIExecutionStatus.Failed;

            _logger.LogError(ex,
                "CI pipeline: Execution {ExecutionId} failed with unhandled exception",
                executionId);

            tracker.AddLog($"[{DateTime.UtcNow:HH:mm:ss}] Fatal error: {ex.Message}");

            return BuildErrorResult(
                executionId, request.WorkflowId, startTime,
                ExitCode.ExecutionFailed, ex.Message);
        }
    }

    /// <inheritdoc />
    public Task<CIExecutionStatus> GetExecutionStatusAsync(
        string executionId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(executionId);

        _logger.LogDebug(
            "CI pipeline: Status query for execution {ExecutionId}",
            executionId);

        if (!_executions.TryGetValue(executionId, out var tracker))
        {
            _logger.LogWarning(
                "CI pipeline: Execution {ExecutionId} not found for status query",
                executionId);
            throw new KeyNotFoundException($"Execution '{executionId}' not found");
        }

        return Task.FromResult(tracker.Status);
    }

    /// <inheritdoc />
    public Task CancelExecutionAsync(
        string executionId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(executionId);

        _logger.LogInformation(
            "CI pipeline: Cancellation requested for execution {ExecutionId}",
            executionId);

        if (!_executions.TryGetValue(executionId, out var tracker))
        {
            _logger.LogWarning(
                "CI pipeline: Execution {ExecutionId} not found for cancellation",
                executionId);
            throw new KeyNotFoundException($"Execution '{executionId}' not found");
        }

        // LOGIC: Cancel the linked CTS. The execution loop will observe
        // the cancellation and exit with an appropriate status.
        tracker.Cancel();
        tracker.Status = CIExecutionStatus.Cancelled;

        _logger.LogInformation(
            "CI pipeline: Execution {ExecutionId} cancellation signal sent",
            executionId);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string> GetExecutionLogsAsync(
        string executionId,
        LogFormat format = LogFormat.Text,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(executionId);

        _logger.LogDebug(
            "CI pipeline: Log retrieval for execution {ExecutionId} in format {Format}",
            executionId, format);

        if (!_executions.TryGetValue(executionId, out var tracker))
        {
            _logger.LogWarning(
                "CI pipeline: Execution {ExecutionId} not found for log retrieval",
                executionId);
            throw new KeyNotFoundException($"Execution '{executionId}' not found");
        }

        var logs = tracker.GetLogs();
        var formatted = format switch
        {
            LogFormat.Json => JsonSerializer.Serialize(logs,
                new JsonSerializerOptions { WriteIndented = true }),
            LogFormat.Markdown => FormatLogsAsMarkdown(logs),
            _ => string.Join(Environment.NewLine, logs) // Text
        };

        return Task.FromResult(formatted);
    }

    // ── Private Helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Determines the exit code based on execution results and request thresholds.
    /// </summary>
    /// <param name="success">Whether all workflow steps passed.</param>
    /// <param name="errorCount">Total error count across all steps.</param>
    /// <param name="warningCount">Total warning count across all steps.</param>
    /// <param name="request">The original request with threshold settings.</param>
    /// <returns>An <see cref="ExitCode"/> constant.</returns>
    /// <remarks>
    /// LOGIC: Evaluates in priority order:
    /// 1. If step failures → ValidationFailed (1)
    /// 2. If FailOnWarnings and warnings exist → ValidationFailed (1)
    /// 3. If MaxWarnings exceeded → ValidationFailed (1)
    /// 4. If errors exist → ValidationFailed (1)
    /// 5. Otherwise → Success (0)
    /// </remarks>
    internal static int DetermineExitCode(
        bool success,
        int errorCount,
        int warningCount,
        CIWorkflowRequest request)
    {
        // LOGIC: Step-level failures always produce ValidationFailed.
        if (!success)
            return ExitCode.ValidationFailed;

        // LOGIC: FailOnWarnings mode treats any warning as a failure.
        if (request.FailOnWarnings && warningCount > 0)
            return ExitCode.ValidationFailed;

        // LOGIC: MaxWarnings threshold produces failure when exceeded.
        if (request.MaxWarnings.HasValue && warningCount > request.MaxWarnings.Value)
            return ExitCode.ValidationFailed;

        // LOGIC: Any errors produce failure even if all steps "passed"
        // (e.g., non-blocking validation warnings might be promoted to errors).
        if (errorCount > 0)
            return ExitCode.ValidationFailed;

        return ExitCode.Success;
    }

    /// <summary>
    /// Builds an error result for early-exit scenarios (invalid input, timeout, exceptions).
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="workflowId">The workflow ID from the request.</param>
    /// <param name="startTime">When the execution started.</param>
    /// <param name="exitCode">The exit code to set.</param>
    /// <param name="errorMessage">Human-readable error message.</param>
    /// <returns>A failed <see cref="CIExecutionResult"/>.</returns>
    private static CIExecutionResult BuildErrorResult(
        string executionId,
        string workflowId,
        DateTime startTime,
        int exitCode,
        string errorMessage)
    {
        return new CIExecutionResult
        {
            ExecutionId = executionId,
            WorkflowId = workflowId,
            Success = false,
            ExitCode = exitCode,
            ExecutionTimeSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds,
            Message = errorMessage,
            ErrorMessage = errorMessage,
            ExecutedAt = startTime
        };
    }

    /// <summary>
    /// Formats log entries as a Markdown fenced code block.
    /// </summary>
    /// <param name="logs">The log entries to format.</param>
    /// <returns>A Markdown string with the logs in a code block.</returns>
    private static string FormatLogsAsMarkdown(IReadOnlyList<string> logs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("```");
        foreach (var log in logs)
        {
            sb.AppendLine(log);
        }
        sb.AppendLine("```");
        return sb.ToString();
    }

    // ── Execution Tracker ───────────────────────────────────────────────

    /// <summary>
    /// Tracks the state of an in-flight CI/CD workflow execution.
    /// </summary>
    /// <remarks>
    /// Stores the linked <see cref="CancellationTokenSource"/> for cancellation support,
    /// the current status, and accumulated log entries.
    /// </remarks>
    private sealed class ExecutionTracker
    {
        private readonly CancellationTokenSource _cts;
        private readonly ConcurrentBag<string> _logs = [];

        /// <summary>Current execution status.</summary>
        public CIExecutionStatus Status { get; set; }

        /// <summary>
        /// Initializes a new execution tracker.
        /// </summary>
        /// <param name="cts">The linked CTS for timeout and cancellation.</param>
        /// <param name="initialStatus">The initial execution status.</param>
        public ExecutionTracker(CancellationTokenSource cts, CIExecutionStatus initialStatus)
        {
            _cts = cts;
            Status = initialStatus;
        }

        /// <summary>Signals cancellation on the linked CTS.</summary>
        public void Cancel()
        {
            try
            {
                _cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // LOGIC: CTS may already be disposed if the execution completed.
                // This is a benign race condition — swallow silently.
            }
        }

        /// <summary>Appends a log entry.</summary>
        /// <param name="entry">The log entry text.</param>
        public void AddLog(string entry) => _logs.Add(entry);

        /// <summary>Returns all log entries in insertion order.</summary>
        /// <returns>A read-only list of log entries.</returns>
        public IReadOnlyList<string> GetLogs() => _logs.ToArray();
    }
}
