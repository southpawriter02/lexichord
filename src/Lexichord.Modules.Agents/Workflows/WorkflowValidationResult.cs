// -----------------------------------------------------------------------
// <copyright file="WorkflowValidationResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Result of workflow validation.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IWorkflowDesignerService.Validate"/> to indicate
/// whether a workflow definition is valid and can be saved or executed.
/// </para>
/// <para>
/// A workflow with only warnings (no errors) is considered valid.
/// Errors prevent saving/execution; warnings are informational.
/// </para>
/// </remarks>
/// <param name="IsValid">Whether the workflow passed validation (no errors).</param>
/// <param name="Errors">List of validation errors found. Empty if valid.</param>
/// <param name="Warnings">List of validation warnings found. May be non-empty even if valid.</param>
public record WorkflowValidationResult(
    bool IsValid,
    IReadOnlyList<WorkflowValidationError> Errors,
    IReadOnlyList<WorkflowValidationWarning> Warnings
);

/// <summary>
/// A validation error in a workflow definition.
/// </summary>
/// <remarks>
/// Errors prevent the workflow from being saved or executed. Each error
/// includes a code for programmatic handling and an optional step ID
/// indicating which step caused the error.
/// </remarks>
/// <param name="StepId">
/// ID of the step that caused the error, or null for workflow-level errors
/// (e.g., MISSING_NAME, EMPTY_WORKFLOW, DUPLICATE_STEP_ID).
/// </param>
/// <param name="Code">
/// Machine-readable error code (e.g., "MISSING_NAME", "EMPTY_WORKFLOW",
/// "UNKNOWN_AGENT", "UNKNOWN_PERSONA", "INVALID_CONDITION",
/// "CIRCULAR_MAPPING", "DUPLICATE_STEP_ID").
/// </param>
/// <param name="Message">Human-readable error description.</param>
public record WorkflowValidationError(
    string? StepId,
    string Code,
    string Message
);

/// <summary>
/// A validation warning in a workflow definition.
/// </summary>
/// <remarks>
/// Warnings are informational and do not prevent saving or execution.
/// They indicate potential issues or suboptimal configurations.
/// </remarks>
/// <param name="StepId">
/// ID of the step that caused the warning, or null for workflow-level warnings
/// (e.g., SINGLE_STEP, SAME_AGENT, NO_CONDITIONS).
/// </param>
/// <param name="Code">
/// Machine-readable warning code (e.g., "SINGLE_STEP", "SAME_AGENT", "NO_CONDITIONS").
/// </param>
/// <param name="Message">Human-readable warning description.</param>
public record WorkflowValidationWarning(
    string? StepId,
    string Code,
    string Message
);

/// <summary>
/// Summary of a workflow for listing.
/// </summary>
/// <remarks>
/// Lightweight projection of <see cref="WorkflowDefinition"/> used by
/// <see cref="IWorkflowDesignerService.ListAsync"/> to return workflow
/// summaries without loading the full definition including all steps.
/// </remarks>
/// <param name="WorkflowId">Unique workflow identifier.</param>
/// <param name="Name">Workflow display name.</param>
/// <param name="Description">Optional workflow description.</param>
/// <param name="StepCount">Number of steps in the workflow.</param>
/// <param name="ModifiedAt">UTC timestamp of last modification.</param>
/// <param name="Category">Workflow category for grouping.</param>
/// <param name="IsBuiltIn">Whether this is a system-provided workflow.</param>
public record WorkflowSummary(
    string WorkflowId,
    string Name,
    string? Description,
    int StepCount,
    DateTime ModifiedAt,
    WorkflowCategory Category,
    bool IsBuiltIn
);
