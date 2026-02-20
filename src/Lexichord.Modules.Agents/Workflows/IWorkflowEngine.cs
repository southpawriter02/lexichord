// -----------------------------------------------------------------------
// <copyright file="IWorkflowEngine.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Engine for executing workflow definitions as sequential agent pipelines.
/// </summary>
/// <remarks>
/// <para>
/// The workflow engine is the core execution runtime for agent workflows. It takes
/// a <see cref="WorkflowDefinition"/> created by the Workflow Designer (v0.7.7a)
/// and executes each step sequentially, handling:
/// </para>
/// <list type="bullet">
///   <item><description>Sequential step execution in <see cref="WorkflowStepDefinition.Order"/> order</description></item>
///   <item><description>Conditional execution via <see cref="WorkflowStepCondition"/> evaluation</description></item>
///   <item><description>Data passing between steps through variable mappings</description></item>
///   <item><description>Cancellation support via <see cref="CancellationToken"/></description></item>
///   <item><description>Aggregated usage metrics across all steps</description></item>
///   <item><description>MediatR event publishing for UI progress updates</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.7b as part of the Workflow Engine.
/// </para>
/// </remarks>
/// <seealso cref="WorkflowDefinition"/>
/// <seealso cref="WorkflowExecutionContext"/>
/// <seealso cref="WorkflowExecutionResult"/>
/// <seealso cref="IExpressionEvaluator"/>
public interface IWorkflowEngine
{
    /// <summary>
    /// Executes a workflow definition to completion.
    /// </summary>
    /// <param name="workflow">The workflow definition to execute.</param>
    /// <param name="context">Execution context with document, variables, and options.</param>
    /// <param name="ct">Cancellation token for aborting the workflow.</param>
    /// <returns>
    /// A <see cref="WorkflowExecutionResult"/> containing per-step results,
    /// aggregated metrics, and the final output.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="workflow"/> or <paramref name="context"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown internally but caught — cancellation is reflected in the result's
    /// <see cref="WorkflowExecutionResult.Status"/> as <see cref="WorkflowExecutionStatus.Cancelled"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// LOGIC: Executes steps sequentially in ascending <see cref="WorkflowStepDefinition.Order"/>.
    /// For each step:
    /// </para>
    /// <list type="number">
    ///   <item><description>Check cancellation token</description></item>
    ///   <item><description>Publish <c>WorkflowStepStartedEvent</c></description></item>
    ///   <item><description>Evaluate step condition (skip if false)</description></item>
    ///   <item><description>Resolve agent from registry (with optional persona)</description></item>
    ///   <item><description>Build <see cref="Lexichord.Abstractions.Agents.AgentRequest"/> from mappings</description></item>
    ///   <item><description>Invoke agent with timeout</description></item>
    ///   <item><description>Apply output mappings to variables</description></item>
    ///   <item><description>Publish <c>WorkflowStepCompletedEvent</c></description></item>
    /// </list>
    /// </remarks>
    Task<WorkflowExecutionResult> ExecuteAsync(
        WorkflowDefinition workflow,
        WorkflowExecutionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a workflow with streaming step results.
    /// Yields results as each step completes for real-time UI updates.
    /// </summary>
    /// <param name="workflow">Workflow definition to execute.</param>
    /// <param name="context">Execution context with document and variables.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Async enumerable of step results, yielded as each step completes.</returns>
    /// <remarks>
    /// LOGIC: Similar to <see cref="ExecuteAsync"/> but yields each
    /// <see cref="WorkflowStepExecutionResult"/> immediately instead of collecting
    /// them into a final result. Useful for real-time progress display in the UI.
    /// </remarks>
    IAsyncEnumerable<WorkflowStepExecutionResult> ExecuteStreamingAsync(
        WorkflowDefinition workflow,
        WorkflowExecutionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Validates that a workflow can be executed in the current context.
    /// Checks agent availability, license requirements, etc.
    /// </summary>
    /// <param name="workflow">Workflow to validate.</param>
    /// <param name="context">Execution context.</param>
    /// <returns>Validation result with any errors or warnings.</returns>
    /// <remarks>
    /// LOGIC: Pre-flight validation that checks:
    /// <list type="bullet">
    ///   <item><description>All referenced agents exist in the registry</description></item>
    ///   <item><description>All referenced personas are valid for their agents</description></item>
    ///   <item><description>Expression conditions are syntactically valid</description></item>
    ///   <item><description>Input mappings reference existing variables or standard variables</description></item>
    /// </list>
    /// </remarks>
    WorkflowExecutionValidation ValidateExecution(
        WorkflowDefinition workflow,
        WorkflowExecutionContext context);

    /// <summary>
    /// Estimates token usage for a workflow without executing it.
    /// </summary>
    /// <param name="workflow">Workflow to estimate.</param>
    /// <param name="context">Execution context.</param>
    /// <returns>Estimated token usage and cost breakdown.</returns>
    /// <remarks>
    /// LOGIC: Uses average token consumption per agent type to estimate total usage.
    /// Estimates are rough approximations for budget planning purposes.
    /// </remarks>
    WorkflowTokenEstimate EstimateTokens(
        WorkflowDefinition workflow,
        WorkflowExecutionContext context);
}
