// -----------------------------------------------------------------------
// <copyright file="GatingWorkflowStep.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Full implementation of IGatingWorkflowStep. Gates evaluate
//   condition expressions against document state and validation results,
//   blocking workflow progression on unmet conditions. Supports:
//
//   - Compound expressions with AND/OR operators
//   - Configurable failure messages for user display
//   - Alternate branch paths for conditional routing
//   - Timeout enforcement via linked CancellationTokenSource (default 10s)
//   - Timestamped audit trail of evaluation decisions
//   - Configuration validation (empty Id/Name/Expression/Message, invalid timeout)
//
//   Execution flow:
//     1. Check IsEnabled — return success with "disabled" message if false
//     2. Create timeout CTS linked with parent token
//     3. Parse ConditionExpression into individual GatingConditions
//     4. Evaluate each condition via IGatingConditionEvaluator
//     5. Combine results using RequireAll (AND) or !RequireAll (OR) logic
//     6. Build GatingResult with audit trail
//     7. Log gate decision
//     8. Map to WorkflowStepResult for workflow engine
//
// v0.7.7f: Gating Step Type (CKVS Phase 4d)
// Dependencies: IGatingWorkflowStep (v0.7.7f), IGatingConditionEvaluator (v0.7.7f),
//               IWorkflowStep (v0.7.7e), ValidationWorkflowContext (v0.7.7e),
//               ValidationStepResult (v0.7.7e), ILogger<T>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Concrete implementation of <see cref="IGatingWorkflowStep"/>.
/// </summary>
/// <remarks>
/// <para>
/// Gates serve as workflow checkpoints that evaluate condition expressions
/// against document state and validation results. They prevent invalid
/// documents from proceeding to publication, distribution, or downstream
/// workflows.
/// </para>
/// <para>
/// <b>Expression Parsing:</b> The <see cref="ConditionExpression"/> string is
/// split on <c>" AND "</c> and <c>" OR "</c> delimiters to produce individual
/// <see cref="GatingCondition"/> entries. The split is case-sensitive.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe for concurrent reads.
/// The <see cref="IsEnabled"/> property is mutable but the workflow engine
/// serializes step execution.
/// </para>
/// <para>
/// <b>Performance Targets:</b>
/// <list type="bullet">
///   <item><description>Expression parsing: &lt; 50ms</description></item>
///   <item><description>Condition evaluation: &lt; 100ms per condition</description></item>
///   <item><description>Overall gate evaluation: &lt; TimeoutMs (default 10s)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7f as part of Gating Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
internal sealed class GatingWorkflowStep : IGatingWorkflowStep
{
    // ── Dependencies ─────────────────────────────────────────────────────
    private readonly IGatingConditionEvaluator _evaluator;
    private readonly ILogger<GatingWorkflowStep> _logger;

    // ── IWorkflowStep Properties ─────────────────────────────────────────

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string? Description { get; }

    /// <inheritdoc/>
    public int Order { get; }

    /// <inheritdoc/>
    public bool IsEnabled { get; set; }

    /// <inheritdoc/>
    public int? TimeoutMs { get; }

    // ── IGatingWorkflowStep Properties ───────────────────────────────────

    /// <inheritdoc/>
    public string ConditionExpression { get; }

    /// <inheritdoc/>
    public string FailureMessage { get; }

    /// <inheritdoc/>
    public string? BranchPath { get; }

    /// <inheritdoc/>
    public bool RequireAll { get; }

    // ── Constructor ──────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of the <see cref="GatingWorkflowStep"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for this gate.</param>
    /// <param name="name">Human-readable display name.</param>
    /// <param name="conditionExpression">
    /// The condition expression to evaluate (supports AND/OR compound expressions).
    /// </param>
    /// <param name="failureMessage">
    /// User-facing message displayed when the gate fails.
    /// </param>
    /// <param name="evaluator">
    /// The condition evaluator for individual expression evaluation.
    /// </param>
    /// <param name="logger">Logger for gate decision diagnostics.</param>
    /// <param name="description">Optional gate description.</param>
    /// <param name="order">Execution order within the workflow. Default is 0.</param>
    /// <param name="timeoutMs">
    /// Timeout in milliseconds. Default is 10000 (10s). Null disables timeout.
    /// </param>
    /// <param name="branchPath">
    /// Optional alternate workflow path to branch to on failure.
    /// </param>
    /// <param name="requireAll">
    /// Whether all conditions must pass (<c>true</c> = AND, <c>false</c> = OR).
    /// Default is <c>true</c>.
    /// </param>
    public GatingWorkflowStep(
        string id,
        string name,
        string conditionExpression,
        string failureMessage,
        IGatingConditionEvaluator evaluator,
        ILogger<GatingWorkflowStep> logger,
        string? description = null,
        int order = 0,
        int? timeoutMs = 10000,
        string? branchPath = null,
        bool requireAll = true)
    {
        Id = id;
        Name = name;
        Description = description;
        Order = order;
        IsEnabled = true;
        TimeoutMs = timeoutMs;
        ConditionExpression = conditionExpression;
        FailureMessage = failureMessage;
        BranchPath = branchPath;
        RequireAll = requireAll;

        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug(
            "Created gating workflow step '{GateId}' (order: {Order}, " +
            "requireAll: {RequireAll}, timeout: {TimeoutMs}ms, " +
            "branch: '{BranchPath}', condition: '{Expression}')",
            Id, Order, RequireAll, TimeoutMs, BranchPath, ConditionExpression);
    }

    // ── IWorkflowStep.ValidateConfiguration ──────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// Validates:
    /// <list type="bullet">
    ///   <item><description>Id must not be empty</description></item>
    ///   <item><description>Name must not be empty</description></item>
    ///   <item><description>ConditionExpression must not be empty</description></item>
    ///   <item><description>FailureMessage must not be empty</description></item>
    ///   <item><description>TimeoutMs (if set) must be positive</description></item>
    /// </list>
    /// </remarks>
    public IReadOnlyList<ValidationConfigurationError> ValidateConfiguration()
    {
        _logger.LogDebug("Validating configuration for gate '{GateId}'", Id);

        var errors = new List<ValidationConfigurationError>();

        if (string.IsNullOrWhiteSpace(Id))
        {
            errors.Add(new ValidationConfigurationError(
                "Gate ID cannot be empty", nameof(Id)));
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(new ValidationConfigurationError(
                "Gate name cannot be empty", nameof(Name)));
        }

        if (string.IsNullOrWhiteSpace(ConditionExpression))
        {
            errors.Add(new ValidationConfigurationError(
                "Condition expression cannot be empty", nameof(ConditionExpression)));
        }

        if (string.IsNullOrWhiteSpace(FailureMessage))
        {
            errors.Add(new ValidationConfigurationError(
                "Failure message cannot be empty", nameof(FailureMessage)));
        }

        if (TimeoutMs.HasValue && TimeoutMs.Value <= 0)
        {
            errors.Add(new ValidationConfigurationError(
                "Timeout must be a positive value in milliseconds", nameof(TimeoutMs)));
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning(
                "Configuration validation failed for gate '{GateId}': {ErrorCount} errors",
                Id, errors.Count);
        }
        else
        {
            _logger.LogDebug("Configuration validation passed for gate '{GateId}'", Id);
        }

        return errors;
    }

    // ── IGatingWorkflowStep.EvaluateAsync ────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// <para>Evaluation steps:</para>
    /// <list type="number">
    ///   <item>Parse ConditionExpression into individual GatingConditions</item>
    ///   <item>Evaluate each condition via IGatingConditionEvaluator</item>
    ///   <item>Combine results: RequireAll → AND, !RequireAll → OR</item>
    ///   <item>Build GatingResult with audit trail</item>
    /// </list>
    /// </remarks>
    public async Task<GatingResult> EvaluateAsync(
        ValidationWorkflowContext context,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var logs = new List<string>();

        try
        {
            logs.Add($"[{DateTime.UtcNow:O}] Starting gate evaluation: {Id}");
            logs.Add($"Condition: {ConditionExpression}");

            _logger.LogInformation(
                "Evaluating gate '{GateId}': {Expression} (requireAll: {RequireAll})",
                Id, ConditionExpression, RequireAll);

            // LOGIC: Step 1 — Parse expression into individual conditions.
            var conditions = ParseExpression(ConditionExpression);
            logs.Add($"Parsed {conditions.Count} condition(s) from expression");

            _logger.LogDebug(
                "Gate '{GateId}': Parsed {ConditionCount} conditions",
                Id, conditions.Count);

            // LOGIC: Step 2 — Build the evaluation context from the workflow context.
            var evalContext = new GatingEvaluationContext
            {
                DocumentContent = context.DocumentContent,
                DocumentType = context.DocumentType,
                ValidationResults = context.PreviousResults
            };

            // LOGIC: Step 3 — Evaluate each condition independently.
            var conditionResults = new Dictionary<string, bool>();
            foreach (var condition in conditions)
            {
                ct.ThrowIfCancellationRequested();

                logs.Add($"Evaluating condition: {condition.Id} — {condition.Expression}");

                try
                {
                    var result = await _evaluator.EvaluateAsync(condition, evalContext, ct);
                    conditionResults[condition.Id] = result;
                    logs.Add($"  Result: {(result ? "PASS" : "FAIL")}");

                    _logger.LogDebug(
                        "Gate '{GateId}' condition '{ConditionId}': {Result}",
                        Id, condition.Id, result ? "PASS" : "FAIL");
                }
                catch (InvalidOperationException ex)
                {
                    // LOGIC: Invalid expression syntax — treat as failure.
                    conditionResults[condition.Id] = false;
                    logs.Add($"  ERROR: {ex.Message}");

                    _logger.LogWarning(ex,
                        "Gate '{GateId}' condition '{ConditionId}' had invalid syntax",
                        Id, condition.Id);
                }
            }

            stopwatch.Stop();

            // LOGIC: Step 4 — Combine results using AND/OR logic.
            var passed = conditionResults.Count > 0 && (RequireAll
                ? conditionResults.Values.All(v => v)
                : conditionResults.Values.Any(v => v));

            logs.Add($"Overall result: {(passed ? "PASS" : "FAIL")} " +
                     $"(RequireAll={RequireAll}, " +
                     $"{conditionResults.Count(v => v.Value)}/{conditionResults.Count} passed)");

            _logger.LogInformation(
                "Gate '{GateId}' evaluated: {Result} ({PassCount}/{TotalCount} conditions passed, " +
                "{ElapsedMs}ms)",
                Id, passed ? "PASS" : "FAIL",
                conditionResults.Count(v => v.Value), conditionResults.Count,
                stopwatch.ElapsedMilliseconds);

            return new GatingResult
            {
                Passed = passed,
                GateId = Id,
                Condition = ConditionExpression,
                FailureMessage = passed ? null : FailureMessage,
                BranchPath = passed ? null : BranchPath,
                EvaluationLogs = logs,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["conditionCount"] = conditions.Count,
                    ["requireAll"] = RequireAll,
                    ["passedCount"] = conditionResults.Count(v => v.Value)
                }
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Gate '{GateId}' evaluation was cancelled after {ElapsedMs}ms",
                Id, stopwatch.ElapsedMilliseconds);
            stopwatch.Stop();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Gate '{GateId}' evaluation failed with exception after {ElapsedMs}ms",
                Id, stopwatch.ElapsedMilliseconds);
            stopwatch.Stop();

            logs.Add($"ERROR: {ex.Message}");

            return new GatingResult
            {
                Passed = false,
                GateId = Id,
                Condition = ConditionExpression,
                FailureMessage = $"Gate evaluation error: {ex.Message}",
                EvaluationLogs = logs,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    // ── IGatingWorkflowStep.GetConditionDescription ──────────────────────

    /// <inheritdoc/>
    public string GetConditionDescription()
    {
        var conditions = ParseExpression(ConditionExpression);
        var descriptions = conditions.Select(c => c.Expression);
        var joined = string.Join(RequireAll ? " AND " : " OR ", descriptions);
        return joined;
    }

    // ── IWorkflowStep.ExecuteAsync ───────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// <para>Orchestrates the full gate lifecycle:</para>
    /// <list type="number">
    ///   <item>If disabled, return success with auto-pass message</item>
    ///   <item>Create timeout-linked CancellationTokenSource</item>
    ///   <item>Delegate to <see cref="EvaluateAsync"/></item>
    ///   <item>Log gate decision for audit</item>
    ///   <item>Map <see cref="GatingResult"/> to <see cref="WorkflowStepResult"/></item>
    /// </list>
    /// </remarks>
    public async Task<WorkflowStepResult> ExecuteAsync(
        ValidationWorkflowContext context,
        CancellationToken ct = default)
    {
        // LOGIC: Skip disabled gates with an auto-pass result.
        if (!IsEnabled)
        {
            _logger.LogInformation(
                "Skipping disabled gate '{GateId}' (auto-passing)", Id);

            return new WorkflowStepResult
            {
                StepId = Id,
                Success = true,
                Message = "Gate is disabled (auto-passing)"
            };
        }

        try
        {
            // LOGIC: Create a timeout CTS linked with the parent token.
            using var timeoutCts = TimeoutMs.HasValue
                ? new CancellationTokenSource(TimeoutMs.Value)
                : new CancellationTokenSource();

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                ct, timeoutCts.Token);

            // LOGIC: Evaluate the gate.
            var result = await EvaluateAsync(context, linkedCts.Token);

            if (!result.Passed)
            {
                _logger.LogWarning(
                    "Gate '{GateId}' blocked workflow: {Message}",
                    Id, result.FailureMessage);
            }

            // LOGIC: Map to WorkflowStepResult for the workflow engine.
            // Store BranchPath in the Data dictionary for the engine to inspect.
            var data = new Dictionary<string, object>
            {
                ["gatingResult"] = result
            };

            if (result.BranchPath != null)
            {
                data["branchPath"] = result.BranchPath;
            }

            return new WorkflowStepResult
            {
                StepId = Id,
                Success = result.Passed,
                Message = result.Passed
                    ? "Gate passed, workflow continues"
                    : $"Gate blocked: {result.FailureMessage}",
                Data = data
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Gate '{GateId}' timed out or was cancelled", Id);

            return new WorkflowStepResult
            {
                StepId = Id,
                Success = false,
                Message = "Gate evaluation timed out"
            };
        }
    }

    // ── Expression Parsing ───────────────────────────────────────────────

    /// <summary>
    /// Parses a compound condition expression into individual <see cref="GatingCondition"/> entries.
    /// </summary>
    /// <param name="expression">
    /// The expression to parse. Compound expressions are split on <c>" AND "</c>
    /// and <c>" OR "</c> delimiters.
    /// </param>
    /// <returns>A list of individual conditions.</returns>
    /// <remarks>
    /// LOGIC: Split on " AND " or " OR " delimiters (case-sensitive, space-padded
    /// to avoid splitting on substrings). Each part becomes an independent condition.
    /// </remarks>
    internal static List<GatingCondition> ParseExpression(string expression)
    {
        var conditions = new List<GatingCondition>();
        var parts = expression.Split(
            new[] { " AND ", " OR " },
            StringSplitOptions.None);

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i].Trim();
            if (!string.IsNullOrWhiteSpace(part))
            {
                conditions.Add(new GatingCondition
                {
                    Id = $"condition_{i}",
                    Expression = part,
                    ExpectedResult = true
                });
            }
        }

        return conditions;
    }
}
