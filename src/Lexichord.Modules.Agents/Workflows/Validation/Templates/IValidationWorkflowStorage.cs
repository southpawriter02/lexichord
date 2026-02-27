// -----------------------------------------------------------------------
// <copyright file="IValidationWorkflowStorage.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation.Templates;

/// <summary>
/// Persistence layer for custom (user-defined) validation workflows.
/// </summary>
/// <remarks>
/// <para>
/// This interface abstracts the storage mechanism for custom workflows.
/// The v0.7.7h implementation provides an in-memory stub; future versions
/// will add persistent storage (e.g., SQLite or file-based).
/// </para>
/// <para>
/// Pre-built workflows are <b>not</b> stored via this interface — they are
/// loaded from embedded resources via <see cref="IValidationWorkflowLoader"/>.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-h §4
/// </para>
/// </remarks>
public interface IValidationWorkflowStorage
{
    /// <summary>
    /// Retrieves a custom workflow by ID.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The workflow if found; otherwise <c>null</c>.</returns>
    Task<ValidationWorkflowDefinition?> GetWorkflowAsync(
        string workflowId,
        CancellationToken ct = default);

    /// <summary>
    /// Lists all custom (non-pre-built) workflows.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All stored custom workflows.</returns>
    Task<IReadOnlyList<ValidationWorkflowDefinition>> ListWorkflowsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Persists a new custom workflow.
    /// </summary>
    /// <param name="workflow">The workflow to store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The persisted workflow's ID.</returns>
    Task<string> SaveWorkflowAsync(
        ValidationWorkflowDefinition workflow,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing custom workflow.
    /// </summary>
    /// <param name="workflowId">The workflow ID to update.</param>
    /// <param name="workflow">The updated definition.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateWorkflowAsync(
        string workflowId,
        ValidationWorkflowDefinition workflow,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a custom workflow.
    /// </summary>
    /// <param name="workflowId">The workflow ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteWorkflowAsync(
        string workflowId,
        CancellationToken ct = default);
}
