// -----------------------------------------------------------------------
// <copyright file="ValidationWorkflowContext.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Context record passed to validation workflow steps during execution.
//   Contains workspace identity, document reference, workflow metadata, and
//   accumulated results from previous steps for conditional logic.
//
// v0.7.7e: Validation Step Types (CKVS Phase 4d)
// Dependencies: ValidationTrigger (v0.7.7e), ValidationStepResult (v0.7.7e)
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Context for validation workflow step execution.
/// </summary>
/// <remarks>
/// <para>
/// Provides the execution environment for <see cref="IValidationWorkflowStep"/>
/// instances. Contains the document being validated, workspace identity,
/// trigger information, and results from any previously executed steps.
/// </para>
/// <para>
/// The <see cref="PreviousResults"/> list enables conditional step execution
/// based on outcomes of earlier validation passes.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7e as part of Validation Step Types (CKVS Phase 4d).
/// </para>
/// </remarks>
public record ValidationWorkflowContext
{
    /// <summary>
    /// Gets the workspace identifier.
    /// </summary>
    public required Guid WorkspaceId { get; init; }

    /// <summary>
    /// Gets the identifier of the document being validated.
    /// </summary>
    /// <remarks>
    /// References the document in the workspace's document store.
    /// </remarks>
    public required string DocumentId { get; init; }

    /// <summary>
    /// Gets the content of the document being validated.
    /// </summary>
    /// <remarks>
    /// The raw text content that validation rules operate on.
    /// </remarks>
    public required string DocumentContent { get; init; }

    /// <summary>
    /// Gets the document type (e.g., "markdown", "yaml", "json").
    /// </summary>
    public string? DocumentType { get; init; }

    /// <summary>
    /// Gets the optional workflow identifier executing this validation.
    /// </summary>
    /// <remarks>
    /// Null when validation is triggered outside of a workflow context.
    /// </remarks>
    public Guid? WorkflowId { get; init; }

    /// <summary>
    /// Gets the optional user identifier who initiated the validation.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Gets the trigger that initiated this validation.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="ValidationTrigger.Manual"/>.
    /// </remarks>
    public ValidationTrigger Trigger { get; init; } = ValidationTrigger.Manual;

    /// <summary>
    /// Gets optional configuration overrides for this execution.
    /// </summary>
    /// <remarks>
    /// Allows callers to override default rule configurations on a
    /// per-execution basis.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? ConfigOverrides { get; init; }

    /// <summary>
    /// Gets the results from previously executed validation steps.
    /// </summary>
    /// <remarks>
    /// Populated incrementally as steps execute. Enables conditional logic
    /// based on prior step outcomes.
    /// </remarks>
    public IReadOnlyList<ValidationStepResult>? PreviousResults { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the validation started.
    /// </summary>
    public DateTime StartTime { get; init; } = DateTime.UtcNow;
}
