// -----------------------------------------------------------------------
// <copyright file="AgentPaletteItemViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Agents.ViewModels;

/// <summary>
/// ViewModel for an agent in the workflow designer palette.
/// </summary>
/// <remarks>
/// <para>
/// Represents a draggable agent card in the designer's palette sidebar.
/// Users drag these items onto the canvas to create workflow steps.
/// </para>
/// <para>
/// The <see cref="IsAvailable"/> property indicates whether the user's
/// current license tier meets the <see cref="RequiredTier"/> for this agent.
/// Unavailable agents are shown dimmed with an upgrade indicator.
/// </para>
/// </remarks>
/// <param name="AgentId">Unique agent identifier matching the registry.</param>
/// <param name="Name">Display name for the agent card.</param>
/// <param name="Description">Short description of the agent's capabilities.</param>
/// <param name="Icon">Lucide icon name for the agent card.</param>
/// <param name="IsAvailable">Whether the user's license tier allows using this agent.</param>
/// <param name="RequiredTier">Minimum license tier required to use this agent.</param>
public record AgentPaletteItemViewModel(
    string AgentId,
    string Name,
    string Description,
    string Icon,
    bool IsAvailable,
    LicenseTier RequiredTier
);
