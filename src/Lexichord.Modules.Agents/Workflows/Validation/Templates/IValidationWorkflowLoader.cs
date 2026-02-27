// -----------------------------------------------------------------------
// <copyright file="IValidationWorkflowLoader.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation.Templates;

/// <summary>
/// Loads validation workflow definitions from a backing store (e.g., embedded resources).
/// </summary>
/// <remarks>
/// <para>
/// The default implementation (<see cref="EmbeddedResourceValidationWorkflowLoader"/>)
/// loads pre-built workflow YAML templates from assembly embedded resources.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-h §4
/// </para>
/// </remarks>
public interface IValidationWorkflowLoader
{
    /// <summary>
    /// Loads a workflow definition by its unique identifier.
    /// </summary>
    /// <param name="workflowId">
    /// The workflow identifier (e.g., "on-save-validation").
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The loaded <see cref="ValidationWorkflowDefinition"/> if found;
    /// otherwise <c>null</c>.
    /// </returns>
    Task<ValidationWorkflowDefinition?> LoadWorkflowAsync(
        string workflowId,
        CancellationToken ct = default);
}
