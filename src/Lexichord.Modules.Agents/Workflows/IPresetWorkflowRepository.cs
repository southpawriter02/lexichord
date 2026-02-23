// -----------------------------------------------------------------------
// <copyright file="IPresetWorkflowRepository.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Repository for accessing preset (built-in) workflows.
/// Presets are loaded from embedded YAML resources at startup and cached immutably.
/// </summary>
/// <remarks>
/// <para>
/// Preset workflows provide production-ready workflow templates that deliver
/// immediate value without requiring users to design workflows from scratch.
/// All presets are immutable — Teams users can duplicate and edit the copy
/// via <see cref="IWorkflowDesignerService.DuplicateAsync"/>.
/// </para>
/// <para>
/// <b>License gating:</b>
/// <list type="bullet">
///   <item><description>Core: View only</description></item>
///   <item><description>WriterPro: View + Execute (3/day)</description></item>
///   <item><description>Teams: View + Execute + Duplicate</description></item>
///   <item><description>Enterprise: View + Execute + Duplicate</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-v0.7.7c §4.1
/// </para>
/// </remarks>
public interface IPresetWorkflowRepository
{
    /// <summary>
    /// Gets all available preset workflows.
    /// </summary>
    /// <returns>
    /// An immutable list of all preset workflow definitions, ordered alphabetically by name.
    /// The list is guaranteed to contain exactly 5 presets in v0.7.7c.
    /// </returns>
    IReadOnlyList<WorkflowDefinition> GetAll();

    /// <summary>
    /// Gets a preset workflow by its unique identifier.
    /// </summary>
    /// <param name="workflowId">
    /// Preset workflow ID (e.g., "preset-technical-review").
    /// Must start with the "preset-" prefix.
    /// </param>
    /// <returns>
    /// The matching <see cref="WorkflowDefinition"/> if found; otherwise <c>null</c>.
    /// </returns>
    WorkflowDefinition? GetById(string workflowId);

    /// <summary>
    /// Gets preset workflows filtered by organizational category.
    /// </summary>
    /// <param name="category">
    /// The <see cref="WorkflowCategory"/> to filter by (e.g., Technical, Marketing, Academic).
    /// </param>
    /// <returns>
    /// An immutable list of preset workflows in the specified category.
    /// May be empty if no presets match the category.
    /// </returns>
    IReadOnlyList<WorkflowDefinition> GetByCategory(WorkflowCategory category);

    /// <summary>
    /// Checks whether a workflow ID refers to a built-in preset.
    /// </summary>
    /// <param name="workflowId">
    /// Workflow ID to check. Must start with "preset-" and exist in the repository.
    /// </param>
    /// <returns>
    /// <c>true</c> if the ID refers to a preset workflow; otherwise <c>false</c>.
    /// </returns>
    bool IsPreset(string workflowId);

    /// <summary>
    /// Gets lightweight summaries of all preset workflows for display in the UI.
    /// </summary>
    /// <returns>
    /// An immutable list of <see cref="PresetWorkflowSummary"/> records,
    /// one per preset, containing display-ready information (name, icon, step count, etc.).
    /// </returns>
    IReadOnlyList<PresetWorkflowSummary> GetSummaries();
}
