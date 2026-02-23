// -----------------------------------------------------------------------
// <copyright file="WorkflowStepResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Result record returned by IWorkflowStep.ExecuteAsync(). Provides
//   a simple success/failure indication with an optional message and
//   arbitrary data payload for downstream step consumption.
//
// v0.7.7e: Validation Step Types (CKVS Phase 4d)
// Dependencies: None
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Result of a base workflow step execution.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IWorkflowStep.ExecuteAsync"/> to communicate the
/// outcome of step execution to the workflow engine. The <see cref="Data"/>
/// dictionary can carry arbitrary payloads for downstream step consumption
/// (e.g., a detailed <see cref="ValidationStepResult"/> under the
/// "validationResult" key).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7e as part of Validation Step Types (CKVS Phase 4d).
/// </para>
/// </remarks>
public record WorkflowStepResult
{
    /// <summary>
    /// Gets the identifier of the step that produced this result.
    /// </summary>
    public required string StepId { get; init; }

    /// <summary>
    /// Gets whether the step executed successfully.
    /// </summary>
    /// <remarks>
    /// For validation steps, success is determined by the combination of
    /// <see cref="ValidationStepResult.IsValid"/> and the step's
    /// <see cref="ValidationFailureAction"/>. A step with
    /// <see cref="ValidationFailureAction.Continue"/> may report success
    /// even when validation findings exist.
    /// </remarks>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the optional human-readable message describing the outcome.
    /// </summary>
    /// <remarks>
    /// Examples: "Validation passed (42 items checked)",
    /// "Step is disabled", "Validation step timed out".
    /// </remarks>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the optional data payload for downstream consumption.
    /// </summary>
    /// <remarks>
    /// Validation steps store the detailed <see cref="ValidationStepResult"/>
    /// under the "validationResult" key. Other step types may store
    /// different payloads as needed.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Data { get; init; }
}
