// -----------------------------------------------------------------------
// <copyright file="PresetWorkflowSummary.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Lightweight summary of a preset workflow for display in the Workflow Gallery UI.
/// </summary>
/// <remarks>
/// <para>
/// Contains only the information needed to render preset workflow cards
/// in the gallery view, avoiding the overhead of loading full step definitions
/// and prompt overrides for display purposes.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-v0.7.7c §4.1
/// </para>
/// </remarks>
/// <param name="WorkflowId">
/// Unique identifier for the preset (e.g., "preset-technical-review").
/// Always prefixed with "preset-".
/// </param>
/// <param name="Name">
/// Human-readable display name (e.g., "Technical Review").
/// </param>
/// <param name="Description">
/// Brief description of what the workflow does, suitable for card subtitles.
/// </param>
/// <param name="Icon">
/// Lucide icon name for the workflow card (e.g., "file-code", "megaphone", "zap").
/// Falls back to "workflow" if the preset has no icon specified.
/// </param>
/// <param name="Category">
/// Organizational category for grouping in the gallery (Technical, Marketing, etc.).
/// </param>
/// <param name="StepCount">
/// Total number of steps in the workflow (1–4 for v0.7.7c presets).
/// </param>
/// <param name="AgentIds">
/// Distinct agent IDs used across all steps, for displaying the agent pipeline
/// (e.g., ["editor", "simplifier", "tuning", "summarizer"]).
/// </param>
/// <param name="RequiredTier">
/// Minimum <see cref="LicenseTier"/> required to execute this preset.
/// WriterPro for basic presets, Teams for advanced presets.
/// </param>
/// <param name="Tags">
/// Searchable tags for filtering and discovery (e.g., ["technical", "documentation"]).
/// </param>
public record PresetWorkflowSummary(
    string WorkflowId,
    string Name,
    string Description,
    string Icon,
    WorkflowCategory Category,
    int StepCount,
    IReadOnlyList<string> AgentIds,
    LicenseTier RequiredTier,
    IReadOnlyList<string> Tags
);
