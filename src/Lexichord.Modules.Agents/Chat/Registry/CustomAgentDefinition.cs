// -----------------------------------------------------------------------
// <copyright file="CustomAgentDefinition.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Serializable record for storing custom agent definitions in user
//   settings. This is the persistence format used by AgentRegistry to
//   save and load custom agents via ISettingsService.
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Chat.Registry;

/// <summary>
/// Serializable definition for a custom agent stored in user settings.
/// </summary>
/// <remarks>
/// <para>
/// Custom agents are persisted as JSON arrays in the user's settings
/// under the <c>Agent:CustomAgents</c> key. This record represents
/// a single entry in that array.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6c as part of the Agent Registry feature.
/// </para>
/// </remarks>
/// <param name="AgentId">Unique identifier for the custom agent.</param>
/// <param name="Name">Human-readable name of the agent.</param>
/// <param name="Description">Description of the agent's purpose.</param>
public record CustomAgentDefinition(
    string AgentId,
    string Name,
    string Description
);
