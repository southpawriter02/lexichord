// -----------------------------------------------------------------------
// <copyright file="IAgentRegistry.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Registry for discovering and selecting agents.
/// </summary>
/// <remarks>
/// <para>
/// The agent registry provides a centralized way to discover and access
/// available agents. It handles license-based filtering and provides
/// a consistent way to get the default agent for new conversations.
/// </para>
/// <para>
/// The registry is populated on startup by scanning for <see cref="IAgent"/>
/// implementations registered in the DI container. It then filters these
/// based on the user's license tier.
/// </para>
/// <para>
/// Teams users can register custom agents at runtime using
/// <see cref="RegisterCustomAgent"/>. These agents are persisted in
/// user settings and loaded on subsequent startups.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6c as part of the Agent Registry feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get all available agents
/// var agents = registry.AvailableAgents;
///
/// // Get specific agent
/// var coPilot = registry.GetAgent("co-pilot");
///
/// // Get default agent for new conversations
/// var defaultAgent = registry.GetDefaultAgent();
/// </code>
/// </example>
/// <seealso cref="IAgent"/>
public interface IAgentRegistry
{
    /// <summary>
    /// Gets all agents available to the current user based on license tier.
    /// </summary>
    /// <remarks>
    /// LOGIC: This list is filtered based on the
    /// <see cref="Lexichord.Abstractions.Contracts.RequiresLicenseAttribute"/>
    /// decoration on each agent. Agents requiring a higher tier than the user's
    /// current license are excluded.
    /// </remarks>
    IReadOnlyList<IAgent> AvailableAgents { get; }

    /// <summary>
    /// Gets an agent by its unique identifier.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns the requested agent if:
    /// <list type="number">
    ///   <item>The agent exists in the registry</item>
    ///   <item>The user's license tier meets the agent's requirements</item>
    /// </list>
    /// If either condition is not met, throws <see cref="AgentNotFoundException"/>
    /// or <see cref="LicenseTierException"/> respectively.
    /// </remarks>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <returns>The requested agent.</returns>
    /// <exception cref="AgentNotFoundException">Agent does not exist.</exception>
    /// <exception cref="LicenseTierException">User lacks required license.</exception>
    IAgent GetAgent(string agentId);

    /// <summary>
    /// Tries to get an agent by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="agent">The agent if found and accessible, otherwise null.</param>
    /// <returns>True if agent was found and accessible.</returns>
    bool TryGetAgent(string agentId, [MaybeNullWhen(false)] out IAgent agent);

    /// <summary>
    /// Gets the default agent for new conversations.
    /// </summary>
    /// <remarks>
    /// LOGIC: The default agent is determined by:
    /// <list type="number">
    ///   <item>User preference (<c>Agent:DefaultAgentId</c> setting)</item>
    ///   <item>Fallback to "co-pilot" if preference not set or not available</item>
    ///   <item>Fallback to first available agent</item>
    /// </list>
    /// If none is available (e.g., user has no valid license), throws
    /// <see cref="NoAgentAvailableException"/>.
    /// </remarks>
    /// <returns>The default agent.</returns>
    /// <exception cref="NoAgentAvailableException">No agents available.</exception>
    IAgent GetDefaultAgent();

    /// <summary>
    /// Registers a custom agent at runtime (Teams only).
    /// </summary>
    /// <remarks>
    /// LOGIC: Custom agents are stored in user settings and loaded on subsequent
    /// application starts. Only Teams users can register custom agents.
    /// </remarks>
    /// <param name="agent">The agent to register.</param>
    /// <exception cref="LicenseTierException">User lacks Teams license.</exception>
    /// <exception cref="AgentAlreadyRegisteredException">Agent ID already exists.</exception>
    void RegisterCustomAgent(IAgent agent);

    /// <summary>
    /// Unregisters a custom agent.
    /// </summary>
    /// <param name="agentId">The ID of the agent to unregister.</param>
    /// <returns>True if the agent was removed.</returns>
    bool UnregisterCustomAgent(string agentId);

    /// <summary>
    /// Refreshes the agent list based on current license tier.
    /// </summary>
    /// <remarks>
    /// LOGIC: Called automatically when the license context changes.
    /// Can be called manually to force a refresh.
    /// </remarks>
    void Refresh();

    /// <summary>
    /// Event raised when the available agents list changes.
    /// </summary>
    event EventHandler<AgentListChangedEventArgs>? AgentsChanged;
}
