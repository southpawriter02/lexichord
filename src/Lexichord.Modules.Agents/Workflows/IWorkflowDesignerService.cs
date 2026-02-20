// -----------------------------------------------------------------------
// <copyright file="IWorkflowDesignerService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Service for managing workflow definitions in the designer.
/// </summary>
/// <remarks>
/// <para>
/// Provides CRUD operations for workflow definitions, including creation,
/// validation, persistence, YAML export/import, and duplication. This is
/// the primary service backing the Workflow Designer UI (v0.7.7a).
/// </para>
/// <para>
/// Workflow creation requires <see cref="Lexichord.Abstractions.Contracts.LicenseTier.Teams"/>
/// tier or higher. The service validates workflows against the registered
/// agents in <see cref="Lexichord.Abstractions.Agents.IAgentRegistry"/>.
/// </para>
/// </remarks>
/// <seealso cref="WorkflowDefinition"/>
/// <seealso cref="WorkflowValidationResult"/>
public interface IWorkflowDesignerService
{
    /// <summary>
    /// Creates a new empty workflow definition.
    /// </summary>
    /// <param name="name">Workflow name.</param>
    /// <returns>New workflow with generated ID.</returns>
    WorkflowDefinition CreateNew(string name);

    /// <summary>
    /// Validates a workflow definition and returns any errors.
    /// </summary>
    /// <remarks>
    /// Checks for missing name, empty steps, unknown agents, unknown
    /// personas, duplicate step IDs, and generates warnings for single-step
    /// workflows, same-agent workflows, and workflows with no conditions.
    /// </remarks>
    /// <param name="workflow">Workflow to validate.</param>
    /// <returns>Validation result with errors and warnings.</returns>
    WorkflowValidationResult Validate(WorkflowDefinition workflow);

    /// <summary>
    /// Saves a workflow definition to storage.
    /// </summary>
    /// <param name="workflow">Workflow to save.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveAsync(WorkflowDefinition workflow, CancellationToken ct = default);

    /// <summary>
    /// Loads a workflow definition from storage.
    /// </summary>
    /// <param name="workflowId">Workflow ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Workflow definition or null if not found.</returns>
    Task<WorkflowDefinition?> LoadAsync(string workflowId, CancellationToken ct = default);

    /// <summary>
    /// Lists all user-created workflows.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of workflow summaries.</returns>
    Task<IReadOnlyList<WorkflowSummary>> ListAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes a workflow definition.
    /// </summary>
    /// <param name="workflowId">Workflow ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string workflowId, CancellationToken ct = default);

    /// <summary>
    /// Exports a workflow definition to YAML format.
    /// </summary>
    /// <param name="workflow">Workflow to export.</param>
    /// <returns>YAML string.</returns>
    string ExportToYaml(WorkflowDefinition workflow);

    /// <summary>
    /// Imports a workflow definition from YAML format.
    /// </summary>
    /// <param name="yaml">YAML string.</param>
    /// <returns>Parsed workflow definition.</returns>
    /// <exception cref="WorkflowImportException">If YAML is invalid.</exception>
    WorkflowDefinition ImportFromYaml(string yaml);

    /// <summary>
    /// Duplicates an existing workflow with a new ID and name.
    /// </summary>
    /// <param name="workflowId">Source workflow ID.</param>
    /// <param name="newName">Name for the duplicate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Duplicated workflow definition.</returns>
    Task<WorkflowDefinition> DuplicateAsync(string workflowId, string newName, CancellationToken ct = default);
}
