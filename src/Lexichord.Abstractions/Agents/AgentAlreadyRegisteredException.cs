// -----------------------------------------------------------------------
// <copyright file="AgentAlreadyRegisteredException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Exception thrown when attempting to register an agent with an ID
/// that already exists in the registry.
/// </summary>
/// <remarks>
/// <para>
/// Thrown by <see cref="IAgentRegistry.RegisterCustomAgent"/> when the agent's
/// <see cref="IAgent.AgentId"/> conflicts with a registered or custom agent.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6c as part of the Agent Registry feature.
/// </para>
/// </remarks>
public sealed class AgentAlreadyRegisteredException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="AgentAlreadyRegisteredException"/>.
    /// </summary>
    /// <param name="agentId">The agent ID that is already registered.</param>
    public AgentAlreadyRegisteredException(string agentId)
        : base($"Agent already registered: {agentId}")
    {
        AgentId = agentId;
    }

    /// <summary>
    /// Gets the agent ID that was already registered.
    /// </summary>
    public string AgentId { get; }
}
