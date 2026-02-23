// -----------------------------------------------------------------------
// <copyright file="IValidationWorkflowStep.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Extended workflow step interface for validation-specific steps.
//   Adds step type classification, failure handling configuration, severity
//   tracking, async mode selection, and validation-specific execution
//   methods (rule retrieval and validation execution).
//
// v0.7.7e: Validation Step Types (CKVS Phase 4d)
// Dependencies: IWorkflowStep (v0.7.7e), ValidationStepType (v0.7.7e),
//               ValidationFailureAction (v0.7.7e), ValidationFailureSeverity (v0.7.7e),
//               ValidationRule (v0.7.7e), ValidationStepResult (v0.7.7e)
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Extended workflow step interface for validation-specific operations.
/// </summary>
/// <remarks>
/// <para>
/// Extends <see cref="IWorkflowStep"/> with validation-specific capabilities:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="StepType"/>: Categorizes the validation (schema, consistency, etc.)</description></item>
///   <item><description><see cref="FailureAction"/>: Determines workflow behavior on failure (halt, continue, etc.)</description></item>
///   <item><description><see cref="FailureSeverity"/>: Classifies the severity of any failures found</description></item>
///   <item><description><see cref="GetValidationRulesAsync"/>: Retrieves applicable rules for this step</description></item>
///   <item><description><see cref="ExecuteValidationAsync"/>: Runs the validation and returns detailed results</description></item>
/// </list>
/// <para>
/// The workflow engine calls <see cref="IWorkflowStep.ExecuteAsync"/> which internally
/// delegates to <see cref="ExecuteValidationAsync"/> and maps the result to a
/// <see cref="WorkflowStepResult"/>. Callers that need detailed validation data
/// should use <see cref="ExecuteValidationAsync"/> directly.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7e as part of Validation Step Types (CKVS Phase 4d).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var step = factory.CreateStep(
///     "schema-check",
///     "Schema Compliance",
///     ValidationStepType.Schema,
///     new ValidationWorkflowStepOptions
///     {
///         FailureAction = ValidationFailureAction.Halt,
///         FailureSeverity = ValidationFailureSeverity.Error,
///         TimeoutMs = 10000
///     });
///
/// var context = new ValidationWorkflowContext
/// {
///     WorkspaceId = Guid.NewGuid(),
///     DocumentId = "doc-123",
///     DocumentContent = "# Hello World"
/// };
///
/// var result = await step.ExecuteValidationAsync(context);
/// if (!result.IsValid)
/// {
///     // Handle validation failures
/// }
/// </code>
/// </example>
public interface IValidationWorkflowStep : IWorkflowStep
{
    /// <summary>
    /// Gets the type of validation this step performs.
    /// </summary>
    /// <remarks>
    /// Used for routing, filtering, and license gating.
    /// </remarks>
    ValidationStepType StepType { get; }

    /// <summary>
    /// Gets the step-specific configuration options.
    /// </summary>
    /// <remarks>
    /// Contains rule-specific parameters such as thresholds, patterns,
    /// or target schemas. These are passed through to the validation service.
    /// </remarks>
    IReadOnlyDictionary<string, object> Options { get; }

    /// <summary>
    /// Gets the action to take when validation fails.
    /// </summary>
    /// <remarks>
    /// Determines workflow behavior on failure. The workflow engine inspects
    /// this value after step execution to decide whether to halt, continue,
    /// branch, or notify.
    /// </remarks>
    ValidationFailureAction FailureAction { get; }

    /// <summary>
    /// Gets the severity level of validation failures from this step.
    /// </summary>
    /// <remarks>
    /// Categorizes the urgency of any failures found. Used for prioritization
    /// in the validation results panel.
    /// </remarks>
    ValidationFailureSeverity FailureSeverity { get; }

    /// <summary>
    /// Gets whether this step executes asynchronously.
    /// </summary>
    /// <remarks>
    /// When <c>true</c> (default), the step uses the async validation path.
    /// When <c>false</c>, the step uses the synchronous validation path.
    /// </remarks>
    bool IsAsync { get; }

    /// <summary>
    /// Gets the validation rules applicable to this step.
    /// </summary>
    /// <param name="ct">Cancellation token for aborting rule retrieval.</param>
    /// <returns>
    /// A read-only list of <see cref="ValidationRule"/> instances configured
    /// for this step type. Only enabled rules are included.
    /// </returns>
    Task<IReadOnlyList<ValidationRule>> GetValidationRulesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Executes the validation step and returns detailed results.
    /// </summary>
    /// <param name="context">
    /// The validation workflow context containing document and execution metadata.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for timeout enforcement and user cancellation.
    /// </param>
    /// <returns>
    /// A <see cref="ValidationStepResult"/> containing pass/fail status,
    /// errors, warnings, execution metrics, and the step's failure configuration.
    /// </returns>
    Task<ValidationStepResult> ExecuteValidationAsync(
        ValidationWorkflowContext context,
        CancellationToken ct = default);
}
