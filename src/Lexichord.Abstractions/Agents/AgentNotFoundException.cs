// -----------------------------------------------------------------------
// <copyright file="AgentNotFoundException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Exception thrown when an agent is not found in the registry.
/// </summary>
/// <remarks>
/// <para>
/// Thrown by <see cref="IAgentRegistry.GetAgent"/> when the requested agent ID
/// does not exist in the registry (neither as a registered nor custom agent).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6c as part of the Agent Registry feature.
/// </para>
/// </remarks>
public sealed class AgentNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="AgentNotFoundException"/>.
    /// </summary>
    /// <param name="agentId">The agent ID that was not found.</param>
    public AgentNotFoundException(string agentId)
        : base($"Agent not found: {agentId}")
    {
        AgentId = agentId;
    }

    /// <summary>
    /// Gets the agent ID that was not found.
    /// </summary>
    public string AgentId { get; }
}
