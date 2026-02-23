// -----------------------------------------------------------------------
// <copyright file="ValidationWorkflowStepOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Configuration record for creating validation workflow steps via
//   the ValidationWorkflowStepFactory. Provides sensible defaults for all
//   optional parameters (30s timeout, Halt on failure, Error severity, async).
//
// v0.7.7e: Validation Step Types (CKVS Phase 4d)
// Dependencies: ValidationFailureAction (v0.7.7e), ValidationFailureSeverity (v0.7.7e)
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Configuration options for creating a <see cref="ValidationWorkflowStep"/>
/// via the <see cref="ValidationWorkflowStepFactory"/>.
/// </summary>
/// <remarks>
/// <para>
/// All properties have sensible defaults. At minimum, the factory requires
/// the step's <c>id</c>, <c>name</c>, and <see cref="ValidationStepType"/>
/// to be passed separately; this options record covers the remaining
/// configuration parameters.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7e as part of Validation Step Types (CKVS Phase 4d).
/// </para>
/// </remarks>
public record ValidationWorkflowStepOptions
{
    /// <summary>
    /// Gets the optional description of the step's purpose.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the execution order within the workflow (0-based).
    /// </summary>
    /// <remarks>
    /// Steps execute in ascending order. Default is 0.
    /// </remarks>
    public int Order { get; init; } = 0;

    /// <summary>
    /// Gets the execution timeout in milliseconds.
    /// </summary>
    /// <remarks>
    /// If the step exceeds this timeout, it is cancelled and returns a
    /// timeout error. Default is 30000 (30 seconds). Null disables timeout.
    /// </remarks>
    public int? TimeoutMs { get; init; } = 30000;

    /// <summary>
    /// Gets the action to take when validation fails.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="ValidationFailureAction.Halt"/> (stop workflow).
    /// </remarks>
    public ValidationFailureAction FailureAction { get; init; } = ValidationFailureAction.Halt;

    /// <summary>
    /// Gets the severity level of validation failures.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="ValidationFailureSeverity.Error"/>.
    /// </remarks>
    public ValidationFailureSeverity FailureSeverity { get; init; } = ValidationFailureSeverity.Error;

    /// <summary>
    /// Gets whether to execute validation asynchronously.
    /// </summary>
    /// <remarks>
    /// Default is <c>true</c>. When <c>false</c>, the step uses the
    /// synchronous validation path.
    /// </remarks>
    public bool ExecuteAsync { get; init; } = true;

    /// <summary>
    /// Gets optional step-specific configuration parameters.
    /// </summary>
    /// <remarks>
    /// Passed through to the <see cref="IValidationWorkflowStep.Options"/> property.
    /// Examples: custom rule patterns, threshold values, target schemas.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? StepOptions { get; init; }
}
