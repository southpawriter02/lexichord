// -----------------------------------------------------------------------
// <copyright file="WorkflowEngine.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Lexichord.Abstractions.Agents;
using Lexichord.Modules.Agents.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Core workflow execution engine.
/// Orchestrates sequential agent execution with data passing and conditions.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="WorkflowEngine"/> is the runtime that executes workflow definitions
/// created by the Workflow Designer (v0.7.7a). It processes each step sequentially,
/// evaluating conditions, resolving agents from the registry, invoking them with
/// mapped inputs, and collecting outputs for downstream steps.
/// </para>
/// <para>
/// Key behaviors:
/// </para>
/// <list type="bullet">
///   <item><description>Steps execute in ascending <see cref="WorkflowStepDefinition.Order"/></description></item>
///   <item><description>Conditions are evaluated via <see cref="IExpressionEvaluator"/></description></item>
///   <item><description>Agents are resolved from <see cref="IAgentRegistry"/></description></item>
///   <item><description>Data flows between steps via a shared variables dictionary</description></item>
///   <item><description>MediatR events provide real-time progress updates</description></item>
///   <item><description>Cancellation is checked before each step</description></item>
///   <item><description>Per-step timeout via <see cref="CancellationTokenSource.CreateLinkedTokenSource"/></description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <seealso cref="IWorkflowEngine"/>
/// <seealso cref="WorkflowDefinition"/>
/// <seealso cref="IExpressionEvaluator"/>
internal class WorkflowEngine : IWorkflowEngine
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly IMediator _mediator;
    private readonly ILogger<WorkflowEngine> _logger;

    // LOGIC: Default timeout for a single step execution (2 minutes).
    // This prevents a single agent invocation from blocking the entire workflow
    // indefinitely. Can be overridden per-workflow via WorkflowExecutionOptions.StepTimeout.
    private readonly TimeSpan _defaultStepTimeout = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowEngine"/> class.
    /// </summary>
    /// <param name="agentRegistry">Registry for resolving agents by ID.</param>
    /// <param name="expressionEvaluator">Evaluator for conditional expressions.</param>
    /// <param name="mediator">MediatR mediator for publishing workflow events.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public WorkflowEngine(
        IAgentRegistry agentRegistry,
        IExpressionEvaluator expressionEvaluator,
        IMediator mediator,
        ILogger<WorkflowEngine> logger)
    {
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _expressionEvaluator = expressionEvaluator ?? throw new ArgumentNullException(nameof(expressionEvaluator));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> ExecuteAsync(
        WorkflowDefinition workflow,
        WorkflowExecutionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(context);

        var executionId = GenerateExecutionId();
        var stepResults = new List<WorkflowStepExecutionResult>();
        var variables = new Dictionary<string, object>(context.InitialVariables);
        var stopwatch = Stopwatch.StartNew();
        var totalUsage = WorkflowUsageMetrics.Empty;

        // LOGIC: Initialize standard workflow variables from the execution context.
        // These variables are available to all steps and conditions.
        InitializeVariables(variables, context);

        _logger.LogDebug("Workflow {WorkflowId} starting with {StepCount} steps",
            workflow.WorkflowId, workflow.Steps.Count);

        await _mediator.Publish(new WorkflowStartedEvent(
            workflow.WorkflowId, executionId, workflow.Steps.Count), ct);

        string? finalOutput = null;
        string? errorMessage = null;
        var status = WorkflowExecutionStatus.Running;

        try
        {
            // LOGIC: Sort steps by their Order property to ensure correct execution sequence.
            var orderedSteps = workflow.Steps.OrderBy(s => s.Order).ToList();
            var stepNumber = 0;

            foreach (var step in orderedSteps)
            {
                // LOGIC: Check for cancellation before each step to enable responsive
                // cancellation handling without waiting for an agent invocation.
                ct.ThrowIfCancellationRequested();
                stepNumber++;

                // LOGIC: Publish step started event for UI progress updates.
                await _mediator.Publish(new WorkflowStepStartedEvent(
                    workflow.WorkflowId, executionId, step.StepId, step.AgentId,
                    stepNumber, orderedSteps.Count), ct);

                // LOGIC: Evaluate the step's condition to determine if it should execute.
                // Steps without conditions always execute. Condition evaluation failures
                // default to executing the step (fail-open for safety).
                if (!ShouldExecuteStep(step, variables))
                {
                    _logger.LogDebug("Step {StepId} skipped due to condition", step.StepId);
                    var skippedResult = CreateSkippedResult(step);
                    stepResults.Add(skippedResult);
                    totalUsage = totalUsage.WithSkipped();

                    await _mediator.Publish(new WorkflowStepCompletedEvent(
                        workflow.WorkflowId, executionId, step.StepId, true,
                        WorkflowStepStatus.Skipped, TimeSpan.Zero), ct);
                    continue;
                }

                // LOGIC: Execute the step — resolve agent, build request, invoke with timeout.
                var stepResult = await ExecuteStepAsync(
                    step, variables, context, ct);

                stepResults.Add(stepResult);

                // LOGIC: Publish step completed event with the outcome.
                await _mediator.Publish(new WorkflowStepCompletedEvent(
                    workflow.WorkflowId, executionId, step.StepId, stepResult.Success,
                    stepResult.Status, stepResult.Duration), ct);

                if (stepResult.Success)
                {
                    // LOGIC: Update final output to the last successful step's output.
                    // This provides the "pipeline" behavior where each step's output
                    // feeds into the next.
                    finalOutput = stepResult.Output;
                    ApplyOutputMappings(step, stepResult, variables);
                    totalUsage = totalUsage.Add(stepResult.Usage);
                    variables["_previousStepSuccess"] = true;
                }
                else
                {
                    variables["_previousStepSuccess"] = false;
                    errorMessage = stepResult.ErrorMessage;

                    // LOGIC: Optionally stop execution on the first failure.
                    // This is the default behavior (StopOnFirstFailure = true).
                    if (context.Options.StopOnFirstFailure)
                    {
                        _logger.LogWarning("Workflow {WorkflowId} stopping due to step failure",
                            workflow.WorkflowId);

                        // LOGIC: Mark remaining steps as skipped with a reason.
                        foreach (var remaining in orderedSteps.Skip(stepNumber))
                        {
                            stepResults.Add(CreateSkippedResult(remaining, "Previous step failed"));
                        }

                        status = WorkflowExecutionStatus.Failed;
                        break;
                    }
                }
            }

            // LOGIC: Determine final status based on step outcomes.
            // If no explicit status was set (e.g., by StopOnFirstFailure), derive it
            // from the step results.
            if (status == WorkflowExecutionStatus.Running)
            {
                status = stepResults.All(s => s.Success || s.Status == WorkflowStepStatus.Skipped)
                    ? WorkflowExecutionStatus.Completed
                    : WorkflowExecutionStatus.PartialSuccess;
            }
        }
        catch (OperationCanceledException)
        {
            // LOGIC: Handle cancellation gracefully. The workflow result will indicate
            // cancellation status, and completed step results are preserved.
            status = WorkflowExecutionStatus.Cancelled;
            errorMessage = "Workflow cancelled by user";
            _logger.LogWarning("Workflow {WorkflowId} cancelled", workflow.WorkflowId);

            await _mediator.Publish(new WorkflowCancelledEvent(
                workflow.WorkflowId, executionId,
                stepResults.LastOrDefault()?.StepId,
                stepResults.Count(s => s.Status == WorkflowStepStatus.Completed)), CancellationToken.None);
        }
        catch (Exception ex)
        {
            // LOGIC: Catch unexpected exceptions to prevent unhandled failures.
            // The workflow result will indicate failure status with the error message.
            status = WorkflowExecutionStatus.Failed;
            errorMessage = ex.Message;
            _logger.LogError(ex, "Workflow {WorkflowId} failed with exception", workflow.WorkflowId);
        }

        stopwatch.Stop();

        // LOGIC: Build the final execution result with all collected data.
        var result = new WorkflowExecutionResult(
            workflow.WorkflowId,
            executionId,
            status == WorkflowExecutionStatus.Completed,
            status,
            stepResults,
            finalOutput,
            stopwatch.Elapsed,
            totalUsage,
            new Dictionary<string, object>(variables),
            errorMessage);

        _logger.LogInformation(
            "Workflow {WorkflowId} completed in {DurationMs}ms with status {Status}",
            workflow.WorkflowId, stopwatch.ElapsedMilliseconds, status);

        // LOGIC: Publish the final completion event (not published for cancelled workflows
        // since WorkflowCancelledEvent was already published).
        if (status != WorkflowExecutionStatus.Cancelled)
        {
            await _mediator.Publish(new WorkflowCompletedEvent(
                workflow.WorkflowId, executionId, result.Success, status, stopwatch.Elapsed,
                stepResults.Count(s => s.Status == WorkflowStepStatus.Completed),
                stepResults.Count(s => s.Status == WorkflowStepStatus.Skipped)), CancellationToken.None);
        }

        return result;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<WorkflowStepExecutionResult> ExecuteStreamingAsync(
        WorkflowDefinition workflow,
        WorkflowExecutionContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var variables = new Dictionary<string, object>(context.InitialVariables);
        InitializeVariables(variables, context);

        // LOGIC: Stream step results as they complete for real-time UI updates.
        // Unlike ExecuteAsync, this does not collect results or publish
        // workflow-level events — callers handle aggregation.
        foreach (var step in workflow.Steps.OrderBy(s => s.Order))
        {
            ct.ThrowIfCancellationRequested();

            if (!ShouldExecuteStep(step, variables))
            {
                yield return CreateSkippedResult(step);
                continue;
            }

            var result = await ExecuteStepAsync(step, variables, context, ct);
            yield return result;

            if (result.Success)
            {
                ApplyOutputMappings(step, result, variables);
                variables["_previousStepSuccess"] = true;
            }
            else
            {
                variables["_previousStepSuccess"] = false;
                if (context.Options.StopOnFirstFailure)
                    yield break;
            }
        }
    }

    /// <inheritdoc />
    public WorkflowExecutionValidation ValidateExecution(
        WorkflowDefinition workflow,
        WorkflowExecutionContext context)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // LOGIC: Check that all referenced agents exist in the registry.
        foreach (var step in workflow.Steps)
        {
            try
            {
                _agentRegistry.GetAgent(step.AgentId);
            }
            catch
            {
                errors.Add($"Step '{step.StepId}' references unknown agent '{step.AgentId}'");
            }

            // LOGIC: Validate expression conditions for syntax errors.
            if (step.Condition is { Type: ConditionType.Expression } condition
                && !string.IsNullOrWhiteSpace(condition.Expression))
            {
                if (!_expressionEvaluator.IsValid(condition.Expression, out var error))
                {
                    errors.Add($"Step '{step.StepId}' has invalid condition expression: {error}");
                }
            }
        }

        // LOGIC: Warn if no steps are defined.
        if (workflow.Steps.Count == 0)
        {
            warnings.Add("Workflow has no steps");
        }

        return new WorkflowExecutionValidation(
            errors.Count == 0,
            errors,
            warnings);
    }

    /// <inheritdoc />
    public WorkflowTokenEstimate EstimateTokens(
        WorkflowDefinition workflow,
        WorkflowExecutionContext context)
    {
        // LOGIC: Estimate token usage based on rough averages per agent type.
        // These are approximations — actual usage depends on document size,
        // prompt complexity, and model behavior.
        const int defaultPromptTokens = 500;
        const int defaultCompletionTokens = 300;

        var stepEstimates = workflow.Steps
            .Select(s => new StepTokenEstimate(
                s.StepId, s.AgentId, defaultPromptTokens, defaultCompletionTokens))
            .ToList();

        var totalPrompt = stepEstimates.Sum(e => e.EstimatedPromptTokens);
        var totalCompletion = stepEstimates.Sum(e => e.EstimatedCompletionTokens);

        return new WorkflowTokenEstimate(
            totalPrompt,
            totalCompletion,
            totalPrompt + totalCompletion,
            0m, // LOGIC: Cost estimation requires model pricing data (not yet available)
            stepEstimates);
    }

    // ── Private Helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Evaluates whether a step should execute based on its condition.
    /// </summary>
    /// <param name="step">The step definition to evaluate.</param>
    /// <param name="variables">Current workflow variable state.</param>
    /// <returns><c>true</c> if the step should execute; <c>false</c> to skip.</returns>
    /// <remarks>
    /// LOGIC: Implements the condition evaluation logic from spec §5.2:
    /// - No condition → execute (true)
    /// - Always → execute (true)
    /// - PreviousSuccess → check _previousStepSuccess variable (true for first step)
    /// - PreviousFailed → check !_previousStepSuccess (false for first step)
    /// - Expression → evaluate via IExpressionEvaluator (true on error — fail-open)
    /// </remarks>
    private bool ShouldExecuteStep(
        WorkflowStepDefinition step,
        IReadOnlyDictionary<string, object> variables)
    {
        if (step.Condition is null)
            return true;

        try
        {
            return step.Condition.Type switch
            {
                ConditionType.Always => true,

                // LOGIC: PreviousSuccess — execute if previous step succeeded.
                // If no _previousStepSuccess variable exists (first step), default to true.
                ConditionType.PreviousSuccess =>
                    !variables.TryGetValue("_previousStepSuccess", out var s) || (bool)s,

                // LOGIC: PreviousFailed — execute only if previous step failed.
                // If no _previousStepSuccess variable exists (first step), return false.
                ConditionType.PreviousFailed =>
                    variables.TryGetValue("_previousStepSuccess", out var f) && !(bool)f,

                // LOGIC: Expression with empty string — treat as always-execute.
                ConditionType.Expression when string.IsNullOrWhiteSpace(step.Condition.Expression) => true,

                // LOGIC: Expression — evaluate via the sandboxed evaluator.
                ConditionType.Expression =>
                    _expressionEvaluator.Evaluate<bool>(step.Condition.Expression, variables),

                // LOGIC: Unknown condition type — default to execute with a warning.
                _ => true
            };
        }
        catch (Exception ex)
        {
            // LOGIC: Condition evaluation failures default to executing the step (fail-open).
            // This prevents malformed conditions from silently skipping important steps.
            _logger.LogWarning(ex, "Step {StepId} condition evaluation failed, defaulting to execute",
                step.StepId);
            return true;
        }
    }

    /// <summary>
    /// Executes a single workflow step: resolves agent, builds request, invokes with timeout.
    /// </summary>
    /// <param name="step">The step definition to execute.</param>
    /// <param name="variables">Current workflow variable state.</param>
    /// <param name="context">Workflow execution context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The step execution result.</returns>
    /// <remarks>
    /// LOGIC: Handles agent resolution (with optional persona), request building from
    /// variable mappings, timeout enforcement, and error wrapping. Timeout is implemented
    /// via a linked cancellation token source.
    /// </remarks>
    private async Task<WorkflowStepExecutionResult> ExecuteStepAsync(
        WorkflowStepDefinition step,
        Dictionary<string, object> variables,
        WorkflowExecutionContext context,
        CancellationToken ct)
    {
        var stepStopwatch = Stopwatch.StartNew();

        try
        {
            // LOGIC: Get the agent from the registry.
            // If a persona is specified, use GetAgentWithPersona for persona-specific behavior;
            // otherwise use GetAgent for the default persona.
            var agent = step.PersonaId is not null
                ? _agentRegistry.GetAgentWithPersona(step.AgentId, step.PersonaId)
                : _agentRegistry.GetAgent(step.AgentId);

            // LOGIC: Build the agent request from step configuration and variable mappings.
            var request = BuildAgentRequest(step, variables, context);

            // LOGIC: Execute with timeout. Creates a linked cancellation token source
            // that triggers on either user cancellation or step timeout.
            var timeout = context.Options.StepTimeout ?? _defaultStepTimeout;
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);

            var response = await agent.InvokeAsync(request, timeoutCts.Token);

            stepStopwatch.Stop();

            _logger.LogInformation("Step {StepId} completed: {TokensUsed} tokens, {DurationMs}ms",
                step.StepId, response.Usage.TotalTokens, stepStopwatch.ElapsedMilliseconds);

            return new WorkflowStepExecutionResult(
                step.StepId,
                step.AgentId,
                true,
                WorkflowStepStatus.Completed,
                response.Content,
                stepStopwatch.Elapsed,
                new AgentUsageMetrics(
                    response.Usage.PromptTokens,
                    response.Usage.CompletionTokens,
                    response.Usage.TotalTokens,
                    0),
                null,
                null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // LOGIC: Step timeout (not user cancellation). The linked CTS timed out
            // but the original cancellation token is still active.
            stepStopwatch.Stop();
            _logger.LogWarning("Step {StepId} timed out after {Duration}", step.StepId, stepStopwatch.Elapsed);

            return new WorkflowStepExecutionResult(
                step.StepId, step.AgentId, false, WorkflowStepStatus.Failed, null,
                stepStopwatch.Elapsed, AgentUsageMetrics.Empty,
                "Step timed out", null);
        }
        catch (Exception ex)
        {
            // LOGIC: Step execution failed. Capture the error but don't rethrow —
            // the engine decides whether to stop or continue based on StopOnFirstFailure.
            stepStopwatch.Stop();
            _logger.LogError(ex, "Step {StepId} failed: {Error}", step.StepId, ex.Message);

            return new WorkflowStepExecutionResult(
                step.StepId, step.AgentId, false, WorkflowStepStatus.Failed, null,
                stepStopwatch.Elapsed, AgentUsageMetrics.Empty,
                ex.Message, null);
        }
    }

    /// <summary>
    /// Builds an <see cref="AgentRequest"/> from step configuration and variable mappings.
    /// </summary>
    /// <param name="step">The step definition with prompt override and input mappings.</param>
    /// <param name="variables">Current workflow variable state.</param>
    /// <param name="context">Workflow execution context for document path and selection.</param>
    /// <returns>The constructed agent request.</returns>
    /// <remarks>
    /// LOGIC: The user message is the step's PromptOverride (or a default). Input mappings
    /// replace placeholders in the message with variable values. The selection is set to
    /// the previous step's output if available, falling back to the context's selection.
    /// </remarks>
    private static AgentRequest BuildAgentRequest(
        WorkflowStepDefinition step,
        Dictionary<string, object> variables,
        WorkflowExecutionContext context)
    {
        var userMessage = step.PromptOverride ?? "Process the provided content.";

        // LOGIC: Apply input mappings by replacing placeholders in the user message
        // with variable values. Placeholders use {variableName} syntax.
        if (step.InputMappings is { Count: > 0 })
        {
            foreach (var (placeholder, variableName) in step.InputMappings)
            {
                if (variables.TryGetValue(variableName, out var value))
                {
                    userMessage = userMessage.Replace($"{{{placeholder}}}", value?.ToString() ?? "");
                }
            }
        }

        // LOGIC: Use previous step's output as the selection for the next step.
        // This enables the "pipeline" pattern where each step processes the previous
        // step's output. Falls back to the original context selection if no previous
        // output is available.
        var selection = variables.TryGetValue("_previousOutput", out var prev)
            && prev is string s && !string.IsNullOrEmpty(s)
            ? s
            : context.Selection;

        return new AgentRequest(
            UserMessage: userMessage,
            DocumentPath: context.DocumentPath,
            Selection: selection
        );
    }

    /// <summary>
    /// Maps output values from a step result to workflow variables.
    /// </summary>
    /// <param name="step">The step definition with output mappings.</param>
    /// <param name="result">The step execution result.</param>
    /// <param name="variables">Workflow variable dictionary to update.</param>
    /// <remarks>
    /// LOGIC: Always sets the standard _previousOutput variable. Then applies any
    /// explicit output mappings defined on the step. Output mappings support
    /// JSONPath-like expressions ($.output, $.usage.tokens) for extracting
    /// specific values from the agent response.
    /// </remarks>
    private void ApplyOutputMappings(
        WorkflowStepDefinition step,
        WorkflowStepExecutionResult result,
        Dictionary<string, object> variables)
    {
        // LOGIC: Always set the standard previous output variable.
        // This enables downstream steps to reference the previous step's output.
        variables["_previousOutput"] = result.Output ?? "";
        variables["_previousStepSuccess"] = true;

        if (step.OutputMappings is null || step.OutputMappings.Count == 0)
            return;

        // LOGIC: Apply explicit output mappings. Each mapping extracts a value from
        // the step result and assigns it to a named variable.
        foreach (var (variableName, expression) in step.OutputMappings)
        {
            try
            {
                var value = ExtractValue(result, expression);
                variables[variableName] = value;
                _logger.LogDebug("Mapped output {Variable} = {Value}", variableName, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to map output {Variable} with expression {Expression}",
                    variableName, expression);
            }
        }
    }

    /// <summary>
    /// Extracts a value from a step result using a JSONPath-like expression.
    /// </summary>
    /// <param name="result">The step result to extract from.</param>
    /// <param name="expression">
    /// Expression specifying what to extract. Supported expressions:
    /// <list type="bullet">
    ///   <item><description><c>$.output</c> or <c>$</c> — The step's output content</description></item>
    ///   <item><description><c>$.usage.promptTokens</c> — Prompt token count</description></item>
    ///   <item><description><c>$.usage.completionTokens</c> — Completion token count</description></item>
    ///   <item><description><c>$.usage.totalTokens</c> — Total token count</description></item>
    ///   <item><description>Anything else — Treated as a literal value</description></item>
    /// </list>
    /// </param>
    /// <returns>The extracted value.</returns>
    /// <remarks>
    /// LOGIC: Simple path-based value extraction from the step result.
    /// Supports $.output, $.usage.* paths. Unknown paths are treated as literal values.
    /// </remarks>
    private static object ExtractValue(WorkflowStepExecutionResult result, string expression)
    {
        if (expression == "$.output" || expression == "$")
            return result.Output ?? "";

        if (expression.StartsWith("$.usage."))
        {
            var key = expression["$.usage.".Length..];
            return key switch
            {
                "promptTokens" => result.Usage.PromptTokens,
                "completionTokens" => result.Usage.CompletionTokens,
                "totalTokens" => result.Usage.TotalTokens,
                _ => (object)null!
            };
        }

        // LOGIC: Fallback — treat the expression as a literal value.
        return expression;
    }

    /// <summary>
    /// Initializes the standard workflow variables from the execution context.
    /// </summary>
    /// <param name="variables">Variable dictionary to populate.</param>
    /// <param name="context">Execution context providing initial values.</param>
    /// <remarks>
    /// LOGIC: Standard variables available to all workflow steps:
    /// - documentPath: path to the current document
    /// - selection: the user's text selection
    /// - _previousStepSuccess: true (no previous step has failed yet)
    /// </remarks>
    private static void InitializeVariables(Dictionary<string, object> variables, WorkflowExecutionContext context)
    {
        if (context.DocumentPath is not null)
            variables["documentPath"] = context.DocumentPath;
        if (context.Selection is not null)
            variables["selection"] = context.Selection;
        variables["_previousStepSuccess"] = true;
    }

    /// <summary>
    /// Creates a skipped step result for steps bypassed by condition evaluation.
    /// </summary>
    /// <param name="step">The step that was skipped.</param>
    /// <param name="reason">Optional reason for skipping (e.g., "Previous step failed").</param>
    /// <returns>A <see cref="WorkflowStepExecutionResult"/> with Skipped status.</returns>
    private static WorkflowStepExecutionResult CreateSkippedResult(
        WorkflowStepDefinition step, string? reason = null) =>
        new(step.StepId, step.AgentId, true, WorkflowStepStatus.Skipped, null,
            TimeSpan.Zero, AgentUsageMetrics.Empty, reason, null);

    /// <summary>
    /// Generates a unique execution ID for tracking a workflow run.
    /// </summary>
    /// <returns>A string in the format "exec-XXXXXXXX" (12 characters total).</returns>
    private static string GenerateExecutionId() =>
        $"exec-{Guid.NewGuid():N}"[..12];
}
