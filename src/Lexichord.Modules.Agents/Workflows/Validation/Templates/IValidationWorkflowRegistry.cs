// -----------------------------------------------------------------------
// <copyright file="IValidationWorkflowRegistry.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation.Templates;

/// <summary>
/// Central registry for managing and accessing validation workflow definitions.
/// </summary>
/// <remarks>
/// <para>
/// The registry provides a unified access point for both pre-built (system)
/// and custom (user-defined) validation workflows. Pre-built workflows are
/// loaded from embedded YAML resources via <see cref="IValidationWorkflowLoader"/>;
/// custom workflows are persisted via <see cref="IValidationWorkflowStorage"/>.
/// </para>
/// <para>
/// <b>Pre-built workflow IDs:</b>
/// <list type="bullet">
///   <item><description><c>on-save-validation</c> — triggers on document save (WriterPro+)</description></item>
///   <item><description><c>pre-publish-gate</c> — triggers before publication (Teams+)</description></item>
///   <item><description><c>nightly-health-check</c> — scheduled daily at 2 AM UTC (Teams+)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-h §3
/// </para>
/// </remarks>
public interface IValidationWorkflowRegistry
{
    /// <summary>
    /// Retrieves a workflow by its unique identifier.
    /// </summary>
    /// <param name="workflowId">
    /// The workflow identifier (e.g., "on-save-validation").
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="ValidationWorkflowDefinition"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no workflow with the given ID exists in the registry.
    /// </exception>
    Task<ValidationWorkflowDefinition> GetWorkflowAsync(
        string workflowId,
        CancellationToken ct = default);

    /// <summary>
    /// Lists all available workflows (pre-built and custom).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Combined list of all workflow definitions.</returns>
    Task<IReadOnlyList<ValidationWorkflowDefinition>> ListWorkflowsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Lists only the pre-built (system-provided) workflows.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of pre-built workflow definitions.</returns>
    Task<IReadOnlyList<ValidationWorkflowDefinition>> ListPrebuiltAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Registers a new custom workflow.
    /// </summary>
    /// <param name="workflow">The workflow definition to register.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The registered workflow's ID.</returns>
    Task<string> RegisterWorkflowAsync(
        ValidationWorkflowDefinition workflow,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing custom workflow.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to update.</param>
    /// <param name="workflow">The updated definition.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to update a pre-built workflow.
    /// </exception>
    Task UpdateWorkflowAsync(
        string workflowId,
        ValidationWorkflowDefinition workflow,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a custom workflow.
    /// </summary>
    /// <param name="workflowId">The ID of the workflow to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to delete a pre-built workflow.
    /// </exception>
    Task DeleteWorkflowAsync(
        string workflowId,
        CancellationToken ct = default);
}
