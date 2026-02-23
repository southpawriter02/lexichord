// -----------------------------------------------------------------------
// <copyright file="IGatingWorkflowStep.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Extended workflow step interface for gating steps that block
//   workflow progression based on condition expressions. Adds condition
//   expression, failure message, branch path, and evaluation methods.
//
// v0.7.7f: Gating Step Type (CKVS Phase 4d)
// Dependencies: IWorkflowStep (v0.7.7e), GatingResult (v0.7.7f),
//               ValidationWorkflowContext (v0.7.7e)
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Extended workflow step interface for gating steps that block
/// workflow progression based on condition expressions.
/// </summary>
/// <remarks>
/// <para>
/// Gates evaluate condition expressions against document state and prior
/// validation results. They serve as checkpoints that prevent invalid
/// documents from proceeding to publication, distribution, or downstream
/// workflows. Gates support:
/// </para>
/// <list type="bullet">
///   <item><description>Boolean condition expressions with AND/OR logic</description></item>
///   <item><description>Configurable failure messages for user display</description></item>
///   <item><description>Alternate branch paths for conditional routing</description></item>
///   <item><description>Audit trail logging for compliance</description></item>
/// </list>
/// <para>
/// <b>License Gating:</b>
/// <list type="bullet">
///   <item><description>WriterPro: Basic gates (validation count only)</description></item>
///   <item><description>Teams: All expressions + branching</description></item>
///   <item><description>Enterprise: Full + custom expression functions</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7f as part of Gating Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var gate = new GatingWorkflowStep(
///     id: "pre-publish-gate",
///     name: "Pre-Publish Quality Gate",
///     conditionExpression: "validation_count(error) == 0 AND content_length > 100",
///     failureMessage: "Document must have zero errors and at least 100 characters",
///     evaluator: evaluator,
///     logger: logger);
///
/// var result = await gate.EvaluateAsync(context);
/// if (!result.Passed)
/// {
///     // Gate blocked workflow
///     Console.WriteLine(result.FailureMessage);
/// }
/// </code>
/// </example>
public interface IGatingWorkflowStep : IWorkflowStep
{
    /// <summary>
    /// Gets the condition expression to evaluate.
    /// </summary>
    /// <remarks>
    /// Supports compound expressions separated by <c>AND</c> / <c>OR</c>.
    /// Examples:
    /// <list type="bullet">
    ///   <item><description><c>validation_count(error) == 0</c></description></item>
    ///   <item><description><c>validation_count(error) == 0 AND validation_count(warning) &lt;= 5</c></description></item>
    ///   <item><description><c>content_length > 100 OR metadata('draft') == false</c></description></item>
    /// </list>
    /// </remarks>
    string ConditionExpression { get; }

    /// <summary>
    /// Gets the message displayed to the user when the gate fails.
    /// </summary>
    /// <remarks>
    /// Should be actionable and describe what needs to be fixed for the gate to pass.
    /// </remarks>
    string FailureMessage { get; }

    /// <summary>
    /// Gets the optional alternate workflow path to branch to on gate failure.
    /// </summary>
    /// <remarks>
    /// When set, the workflow engine redirects to this path instead of halting.
    /// Null means the gate halts the workflow on failure.
    /// </remarks>
    string? BranchPath { get; }

    /// <summary>
    /// Gets whether all conditions must pass (<c>true</c> = AND, <c>false</c> = OR).
    /// </summary>
    /// <remarks>
    /// Default is <c>true</c> (all conditions required). When <c>false</c>,
    /// at least one condition must pass.
    /// </remarks>
    bool RequireAll { get; }

    /// <summary>
    /// Evaluates the gate condition against the current workflow context.
    /// </summary>
    /// <param name="context">
    /// The validation workflow context containing document and trigger information.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for timeout enforcement and user cancellation.
    /// </param>
    /// <returns>
    /// A <see cref="GatingResult"/> containing the pass/fail status,
    /// evaluation logs, and optional branch path.
    /// </returns>
    Task<GatingResult> EvaluateAsync(
        ValidationWorkflowContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a human-readable description of the condition expression.
    /// </summary>
    /// <returns>
    /// A formatted string describing all conditions joined by AND/OR.
    /// </returns>
    string GetConditionDescription();
}
