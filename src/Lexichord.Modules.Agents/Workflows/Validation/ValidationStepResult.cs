// -----------------------------------------------------------------------
// <copyright file="ValidationStepResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Contains the result of a validation step execution, including
//   pass/fail status, errors, warnings, execution metrics, and the
//   declared failure action/severity for workflow engine decision-making.
//
// v0.7.7e: Validation Step Types (CKVS Phase 4d)
// Dependencies: ValidationFailureAction (v0.7.7e), ValidationFailureSeverity (v0.7.7e)
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Result of a validation step execution.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IValidationWorkflowStep.ExecuteValidationAsync"/> after
/// running all applicable validation rules. Contains the aggregated pass/fail
/// status, individual errors and warnings, execution metrics, and the step's
/// declared failure handling configuration.
/// </para>
/// <para>
/// The workflow engine inspects <see cref="IsValid"/> and <see cref="FailureAction"/>
/// to determine whether to continue, halt, or branch the workflow.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7e as part of Validation Step Types (CKVS Phase 4d).
/// </para>
/// </remarks>
public record ValidationStepResult
{
    /// <summary>
    /// Gets the identifier of the step that produced this result.
    /// </summary>
    public required string StepId { get; init; }

    /// <summary>
    /// Gets whether the validation passed (no errors at or above the minimum severity).
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets the validation errors found during execution.
    /// </summary>
    /// <remarks>
    /// Empty if <see cref="IsValid"/> is <c>true</c>. Each error describes a specific
    /// validation rule violation.
    /// </remarks>
    public IReadOnlyList<ValidationStepError> Errors { get; init; } = [];

    /// <summary>
    /// Gets the validation warnings found during execution.
    /// </summary>
    /// <remarks>
    /// Warnings do not affect <see cref="IsValid"/> and are informational.
    /// May be non-empty even when the step passes.
    /// </remarks>
    public IReadOnlyList<ValidationStepWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Gets the IDs of validation rules that were executed.
    /// </summary>
    public IReadOnlyList<string> ExecutedRuleIds { get; init; } = [];

    /// <summary>
    /// Gets the total number of items (entities, fields, references) checked.
    /// </summary>
    public int ItemsChecked { get; init; }

    /// <summary>
    /// Gets the number of items that had at least one issue.
    /// </summary>
    public int ItemsWithIssues { get; init; }

    /// <summary>
    /// Gets the execution duration in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// Gets the failure action declared by the step.
    /// </summary>
    /// <remarks>
    /// The workflow engine uses this to decide how to proceed when
    /// <see cref="IsValid"/> is <c>false</c>.
    /// </remarks>
    public ValidationFailureAction FailureAction { get; init; }

    /// <summary>
    /// Gets the failure severity level declared by the step.
    /// </summary>
    public ValidationFailureSeverity FailureSeverity { get; init; }

    /// <summary>
    /// Gets optional metadata about the execution.
    /// </summary>
    /// <remarks>
    /// May contain step-type-specific information such as rule counts,
    /// step type labels, or diagnostic data.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// A validation error produced by a validation step.
/// </summary>
/// <remarks>
/// <para>
/// Represents a specific validation rule violation found during step execution.
/// Errors contribute to the step's <see cref="ValidationStepResult.IsValid"/>
/// determination.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7e as part of Validation Step Types (CKVS Phase 4d).
/// </para>
/// </remarks>
/// <param name="RuleId">ID of the rule that produced this error.</param>
/// <param name="Code">Machine-readable error code for programmatic handling.</param>
/// <param name="Message">Human-readable error description.</param>
public record ValidationStepError(
    string RuleId,
    string Code,
    string Message
);

/// <summary>
/// A validation warning produced by a validation step.
/// </summary>
/// <remarks>
/// <para>
/// Represents an informational finding that does not affect the step's
/// pass/fail status. Warnings indicate potential issues or suggestions.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7e as part of Validation Step Types (CKVS Phase 4d).
/// </para>
/// </remarks>
/// <param name="RuleId">ID of the rule that produced this warning.</param>
/// <param name="Code">Machine-readable warning code for programmatic handling.</param>
/// <param name="Message">Human-readable warning description.</param>
public record ValidationStepWarning(
    string RuleId,
    string Code,
    string Message
);
