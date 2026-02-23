// -----------------------------------------------------------------------
// <copyright file="IWorkflowStep.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Base interface for validation workflow steps. Defines the contract
//   for self-describing, configurable, executable steps within a validation
//   workflow pipeline. Steps declare their identity, ordering, timeout,
//   and provide configuration validation.
//
// v0.7.7e: Validation Step Types (CKVS Phase 4d)
// Dependencies: WorkflowStepResult (v0.7.7e), ValidationWorkflowContext (v0.7.7e)
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Base interface for steps in a validation workflow pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Each workflow step is a self-contained execution unit within a
/// validation workflow. Steps declare their identity, ordering, timeout,
/// and enable/disable state. The workflow engine invokes steps in
/// ascending <see cref="Order"/> and respects <see cref="IsEnabled"/>
/// and <see cref="TimeoutMs"/> settings.
/// </para>
/// <para>
/// This is a validation-pipeline-specific base step type introduced in
/// CKVS Phase 4d. It is distinct from the existing
/// <see cref="WorkflowStepDefinition"/> record used by the agent
/// workflow designer (v0.7.7a).
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe. The workflow
/// engine may invoke multiple steps concurrently in future versions.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7e as part of Validation Step Types (CKVS Phase 4d).
/// </para>
/// </remarks>
public interface IWorkflowStep
{
    /// <summary>
    /// Gets the unique identifier for this step.
    /// </summary>
    /// <remarks>
    /// Must be stable and unique within a workflow definition. Used for
    /// result attribution, logging, and configuration-based enable/disable.
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// Gets the human-readable display name for this step.
    /// </summary>
    /// <remarks>
    /// Shown in the workflow execution UI and logs.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets the optional description of the step's purpose.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the execution order within the workflow.
    /// </summary>
    /// <remarks>
    /// Steps execute in ascending order. Lower values run first.
    /// </remarks>
    int Order { get; }

    /// <summary>
    /// Gets whether this step is enabled for execution.
    /// </summary>
    /// <remarks>
    /// Disabled steps are skipped during execution but remain in the
    /// workflow definition for configuration purposes. When skipped,
    /// the step returns a success result with a "disabled" message.
    /// </remarks>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the execution timeout in milliseconds.
    /// </summary>
    /// <remarks>
    /// If the step exceeds this timeout, it is cancelled via
    /// <see cref="CancellationToken"/> and returns a timeout error.
    /// Null means no timeout enforcement.
    /// </remarks>
    int? TimeoutMs { get; }

    /// <summary>
    /// Executes the step within the given workflow context.
    /// </summary>
    /// <param name="context">
    /// The workflow context containing document, workspace, and trigger information.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for timeout enforcement and user cancellation.
    /// Implementations must check this token periodically.
    /// </param>
    /// <returns>
    /// A <see cref="WorkflowStepResult"/> indicating success or failure
    /// with an optional message and data payload.
    /// </returns>
    Task<WorkflowStepResult> ExecuteAsync(
        ValidationWorkflowContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Validates the step's configuration before execution.
    /// </summary>
    /// <returns>
    /// A read-only list of configuration errors. An empty list indicates
    /// the configuration is valid.
    /// </returns>
    IReadOnlyList<ValidationConfigurationError> ValidateConfiguration();
}

/// <summary>
/// Represents a configuration error detected during step validation.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IWorkflowStep.ValidateConfiguration"/> to indicate
/// invalid step configuration. Configuration errors prevent step execution.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7e as part of Validation Step Types (CKVS Phase 4d).
/// </para>
/// </remarks>
/// <param name="Message">Human-readable description of the configuration error.</param>
/// <param name="PropertyName">Optional name of the misconfigured property.</param>
public record ValidationConfigurationError(
    string Message,
    string? PropertyName = null
);
