// -----------------------------------------------------------------------
// <copyright file="AgentMetadata.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Positional record caching agent metadata extracted during discovery.
//   Avoids repeated reflection calls by storing all information needed for
//   filtering and display without resolving the full agent instance.
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Agents.Chat.Registry;

/// <summary>
/// Cached metadata about a registered agent.
/// </summary>
/// <remarks>
/// <para>
/// Metadata is extracted once during discovery and cached to avoid
/// repeated reflection calls. It contains all information needed for
/// filtering and display without resolving the full agent instance.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6c as part of the Agent Registry feature.
/// </para>
/// </remarks>
/// <param name="AgentId">Unique identifier for the agent.</param>
/// <param name="Name">Human-readable name of the agent.</param>
/// <param name="Description">Description of the agent's purpose.</param>
/// <param name="Capabilities">Agent capability flags.</param>
/// <param name="RequiredLicense">Minimum license tier required.</param>
/// <param name="AgentType">The concrete agent implementation type.</param>
public record AgentMetadata(
    string AgentId,
    string Name,
    string Description,
    AgentCapabilities Capabilities,
    LicenseTier RequiredLicense,
    Type AgentType
);
