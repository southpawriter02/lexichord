// -----------------------------------------------------------------------
// <copyright file="ICIPipelineService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation.CI;

/// <summary>
/// Service for executing validation workflows in CI/CD pipelines without user interaction.
/// </summary>
/// <remarks>
/// <para>
/// The CI pipeline service is the primary entry point for headless workflow execution.
/// It accepts a <see cref="CIWorkflowRequest"/> describing the workflow, document,
/// timeout, and output preferences, then orchestrates execution via the underlying
/// <see cref="IWorkflowEngine"/> (v0.7.7b) and validation workflow infrastructure.
/// </para>
/// <para>
/// <b>Exit code semantics:</b> Results include an <see cref="CIExecutionResult.ExitCode"/>
/// that CI/CD systems can use to determine pipeline pass/fail status:
/// </para>
/// <list type="bullet">
///   <item><description>0 — All validations passed</description></item>
///   <item><description>1 — Validation failures detected</description></item>
///   <item><description>2 — Invalid input (bad workspace/workflow/document)</description></item>
///   <item><description>3 — Execution error (unexpected failure)</description></item>
///   <item><description>124 — Timeout exceeded</description></item>
///   <item><description>127 — Fatal/unrecoverable error</description></item>
/// </list>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-j §2.1
/// </para>
/// </remarks>
/// <seealso cref="CIWorkflowRequest"/>
/// <seealso cref="CIExecutionResult"/>
/// <seealso cref="CIPipelineService"/>
public interface ICIPipelineService
{
    /// <summary>
    /// Executes a validation workflow in headless mode for CI/CD integration.
    /// </summary>
    /// <param name="request">
    /// The CI workflow request containing workspace, workflow ID, document target,
    /// timeout, output format, and optional CI system metadata.
    /// </param>
    /// <param name="ct">Cancellation token for aborting the execution.</param>
    /// <returns>
    /// A <see cref="CIExecutionResult"/> containing the execution outcome, exit code,
    /// validation summary, per-step results, and timing information.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Resolves the workflow from the registry, builds an execution context,
    /// runs the workflow via the engine, and maps results into a CI-friendly format
    /// with appropriate exit codes. Timeout is enforced via a linked
    /// <see cref="CancellationTokenSource"/>.
    /// </para>
    /// <para>
    /// <b>Specification:</b> LCS-DES-077-KG-j §2.1, §4.2
    /// </para>
    /// </remarks>
    Task<CIExecutionResult> ExecuteWorkflowAsync(
        CIWorkflowRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current status of a running or completed workflow execution.
    /// </summary>
    /// <param name="executionId">The execution ID returned from <see cref="ExecuteWorkflowAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The current <see cref="CIExecutionStatus"/> of the execution.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no execution with the specified ID is found.
    /// </exception>
    /// <remarks>
    /// <b>Specification:</b> LCS-DES-077-KG-j §2.1
    /// </remarks>
    Task<CIExecutionStatus> GetExecutionStatusAsync(
        string executionId,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels a running workflow execution.
    /// </summary>
    /// <param name="executionId">The execution ID to cancel.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no execution with the specified ID is found.
    /// </exception>
    /// <remarks>
    /// <para>
    /// LOGIC: Signals the linked <see cref="CancellationTokenSource"/> for the
    /// specified execution. The execution will complete with exit code 124 (Timeout)
    /// or the cancellation will be reflected in the result status.
    /// </para>
    /// <para>
    /// <b>Specification:</b> LCS-DES-077-KG-j §2.1
    /// </para>
    /// </remarks>
    Task CancelExecutionAsync(
        string executionId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the execution logs for a completed or running workflow execution.
    /// </summary>
    /// <param name="executionId">The execution ID to retrieve logs for.</param>
    /// <param name="format">
    /// The desired log output format. Defaults to <see cref="LogFormat.Text"/>.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A formatted string containing the execution logs in the requested format.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no execution with the specified ID is found.
    /// </exception>
    /// <remarks>
    /// <para>
    /// LOGIC: Retrieves stored log entries for the execution and formats them
    /// according to the requested <see cref="LogFormat"/>:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="LogFormat.Text"/> — Plain text, one entry per line</description></item>
    ///   <item><description><see cref="LogFormat.Json"/> — JSON array of log entries</description></item>
    ///   <item><description><see cref="LogFormat.Markdown"/> — Fenced code block</description></item>
    /// </list>
    /// <para>
    /// <b>Specification:</b> LCS-DES-077-KG-j §2.1
    /// </para>
    /// </remarks>
    Task<string> GetExecutionLogsAsync(
        string executionId,
        LogFormat format = LogFormat.Text,
        CancellationToken ct = default);
}
