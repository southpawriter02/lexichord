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
    /// <para>
    /// <b>OBSOLETE:</b> This method is deprecated in favor of
    /// <see cref="RegisterAgent(AgentConfiguration, Func{IServiceProvider, IAgent})"/>
    /// which provides better support for personas and configuration-driven registration.
    /// </para>
    /// </remarks>
    /// <param name="agent">The agent to register.</param>
    /// <exception cref="LicenseTierException">User lacks Teams license.</exception>
    /// <exception cref="AgentAlreadyRegisteredException">Agent ID already exists.</exception>
    [Obsolete("Use RegisterAgent(AgentConfiguration, Func<IServiceProvider, IAgent>) instead. This method will be removed in v0.8.0")]
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

    // -----------------------------------------------------------------------
    // v0.7.1b Extensions: Persona Management & Factory Registration
    // -----------------------------------------------------------------------

    /// <summary>
    /// Gets all personas across all registered agents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: This property aggregates personas from all agents registered via
    /// <see cref="RegisterAgent"/> (factory-based registrations). It does not
    /// include personas from legacy <see cref="RegisterCustomAgent"/> registrations.
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item>Populate persona picker UI</item>
    ///   <item>Discover available personality variants</item>
    ///   <item>Analytics on persona diversity</item>
    /// </list>
    /// <para>
    /// <b>Introduced in:</b> v0.7.1b.
    /// </para>
    /// </remarks>
    IReadOnlyList<AgentPersona> AvailablePersonas { get; }

    /// <summary>
    /// Registers an agent with configuration and factory (v0.7.1b).
    /// </summary>
    /// <param name="config">The agent configuration including personas and capabilities.</param>
    /// <param name="factory">Factory function to create agent instances from DI.</param>
    /// <remarks>
    /// <para>
    /// LOGIC: This method stores the agent configuration and factory for on-demand
    /// instantiation. The factory is invoked lazily when the agent is first accessed.
    /// </para>
    /// <list type="number">
    ///   <item>Validates configuration via <see cref="AgentConfiguration.Validate"/></item>
    ///   <item>Stores configuration and factory for later instantiation</item>
    ///   <item>Invalidates available agents cache</item>
    ///   <item>Publishes <see cref="Events.AgentRegisteredEvent"/> via MediatR</item>
    /// </list>
    /// <para>
    /// <b>License Validation:</b> The registry does NOT validate license tier at
    /// registration time. License checks occur when the agent is first accessed
    /// via <see cref="GetAgent"/> or <see cref="GetAgentWithPersona"/>.
    /// </para>
    /// <para>
    /// <b>Factory Requirements:</b> The factory function should:
    /// </para>
    /// <list type="bullet">
    ///   <item>Resolve dependencies from <see cref="IServiceProvider"/></item>
    ///   <item>Return a new or cached instance of <see cref="IAgent"/></item>
    ///   <item>Be thread-safe (may be called concurrently)</item>
    /// </list>
    /// <para>
    /// <b>Introduced in:</b> v0.7.1b as replacement for <see cref="RegisterCustomAgent"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="config"/> or <paramref name="factory"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if configuration validation fails (e.g., invalid agent ID format).
    /// </exception>
    /// <seealso cref="AgentConfiguration"/>
    /// <seealso cref="Events.AgentRegisteredEvent"/>
    void RegisterAgent(AgentConfiguration config, Func<IServiceProvider, IAgent> factory);

    /// <summary>
    /// Gets an agent with a specific persona applied.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="personaId">The unique identifier of the persona to apply.</param>
    /// <returns>The agent instance with the persona applied.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: This method retrieves (or creates) an agent instance and applies the
    /// specified persona:
    /// </para>
    /// <list type="number">
    ///   <item>Validates agent exists and persona ID is valid</item>
    ///   <item>Checks license tier requirements for the agent</item>
    ///   <item>Gets or creates agent instance from factory</item>
    ///   <item>
    ///     If agent implements <see cref="IPersonaAwareAgent"/>, calls
    ///     <see cref="IPersonaAwareAgent.ApplyPersona"/>
    ///   </item>
    ///   <item>Records active persona preference</item>
    ///   <item>Returns agent instance</item>
    /// </list>
    /// <para>
    /// <b>Non-Persona-Aware Agents:</b> If the agent does not implement
    /// <see cref="IPersonaAwareAgent"/>, the persona preference is recorded but
    /// not applied to the current instance. It will be used for future instantiations.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.7.1b.
    /// </para>
    /// </remarks>
    /// <exception cref="AgentNotFoundException">Agent ID not found.</exception>
    /// <exception cref="PersonaNotFoundException">Persona ID not found for this agent.</exception>
    /// <exception cref="Exceptions.AgentAccessDeniedException">User lacks required license tier.</exception>
    /// <seealso cref="SwitchPersona"/>
    /// <seealso cref="IPersonaAwareAgent"/>
    IAgent GetAgentWithPersona(string agentId, string personaId);

    /// <summary>
    /// Updates an existing agent configuration (hot-reload).
    /// </summary>
    /// <param name="config">The updated configuration.</param>
    /// <remarks>
    /// <para>
    /// LOGIC: This method hot-reloads an agent's configuration without requiring
    /// application restart:
    /// </para>
    /// <list type="number">
    ///   <item>Validates the agent exists in registry</item>
    ///   <item>Validates new configuration via <see cref="AgentConfiguration.Validate"/></item>
    ///   <item>Updates stored configuration, preserving factory function</item>
    ///   <item>Invalidates cached agent instance (forces recreation on next access)</item>
    ///   <item>Preserves active persona preference</item>
    ///   <item>Publishes <see cref="Events.AgentConfigReloadedEvent"/> via MediatR</item>
    /// </list>
    /// <para>
    /// <b>Use Cases:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item>Update personas without restart</item>
    ///   <item>Adjust temperature or model settings</item>
    ///   <item>Add/remove capabilities</item>
    ///   <item>Modify license tier requirements</item>
    /// </list>
    /// <para>
    /// <b>Active Instances:</b> Cached agent instances are invalidated. New instances
    /// will use the updated configuration, but existing conversation contexts remain intact.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.7.1b for workspace agent hot-reload.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
    /// <exception cref="AgentNotFoundException">Agent ID not found in registry.</exception>
    /// <seealso cref="Events.AgentConfigReloadedEvent"/>
    void UpdateAgent(AgentConfiguration config);

    /// <summary>
    /// Switches the active persona for a cached agent.
    /// </summary>
    /// <param name="agentId">The agent whose persona should change.</param>
    /// <param name="personaId">The ID of the persona to activate.</param>
    /// <remarks>
    /// <para>
    /// LOGIC: This method changes the active persona for an agent:
    /// </para>
    /// <list type="number">
    ///   <item>Validates agent and persona IDs are valid</item>
    ///   <item>Checks if agent is cached in memory</item>
    ///   <item>
    ///     <b>If cached:</b> Applies persona via <see cref="IPersonaAwareAgent.ApplyPersona"/>
    ///     (if supported)
    ///   </item>
    ///   <item>
    ///     <b>If not cached:</b> Records preference for next instantiation
    ///   </item>
    ///   <item>Updates active persona tracking</item>
    ///   <item>Publishes <see cref="Events.PersonaSwitchedEvent"/> via MediatR</item>
    /// </list>
    /// <para>
    /// <b>Difference from GetAgentWithPersona:</b> This method only switches the
    /// persona without returning the agent instance. Use this when you want to change
    /// persona preference without immediately needing the agent.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.7.1b.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if agent ID or persona ID is null/empty.</exception>
    /// <exception cref="AgentNotFoundException">Agent ID not found.</exception>
    /// <exception cref="PersonaNotFoundException">Persona ID not found for this agent.</exception>
    /// <seealso cref="GetAgentWithPersona"/>
    /// <seealso cref="GetActivePersona"/>
    void SwitchPersona(string agentId, string personaId);

    /// <summary>
    /// Gets the currently active persona for an agent.
    /// </summary>
    /// <param name="agentId">The agent to query.</param>
    /// <returns>
    /// The active persona, or null if no persona has been explicitly set
    /// (agent is using default behavior).
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: This method returns the persona that was most recently applied via
    /// <see cref="SwitchPersona"/> or <see cref="GetAgentWithPersona"/>. If no
    /// persona has been explicitly set, returns the configuration's
    /// <see cref="AgentConfiguration.DefaultPersona"/>.
    /// </para>
    /// <para>
    /// <b>Return Values:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><b>Non-null:</b> Explicit persona active or default persona defined</item>
    ///   <item>
    ///     <b>Null:</b> No explicit switch and configuration has no default persona,
    ///     or agent not found
    ///   </item>
    /// </list>
    /// <para>
    /// <b>Introduced in:</b> v0.7.1b.
    /// </para>
    /// </remarks>
    /// <seealso cref="SwitchPersona"/>
    AgentPersona? GetActivePersona(string agentId);

    /// <summary>
    /// Checks if an agent is accessible without throwing exceptions.
    /// </summary>
    /// <param name="agentId">The agent to check.</param>
    /// <returns>True if agent exists and user has required license; otherwise false.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: This is a non-throwing alternative to <see cref="GetAgent"/>. It checks:
    /// </para>
    /// <list type="bullet">
    ///   <item>Agent exists in registry</item>
    ///   <item>User's current license tier meets agent's requirements</item>
    /// </list>
    /// <para>
    /// <b>Use Cases:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item>UI visibility checks (show/hide agent in picker)</item>
    ///   <item>Pre-validation before attempting access</item>
    ///   <item>Conditional feature availability</item>
    /// </list>
    /// <para>
    /// <b>Introduced in:</b> v0.7.1b.
    /// </para>
    /// </remarks>
    /// <seealso cref="GetAgent"/>
    bool CanAccess(string agentId);

    /// <summary>
    /// Gets the configuration for a specific agent.
    /// </summary>
    /// <param name="agentId">The agent to query.</param>
    /// <returns>The agent configuration, or null if not found.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: This method retrieves the stored <see cref="AgentConfiguration"/> for
    /// agents registered via <see cref="RegisterAgent"/>. It does not return
    /// configurations for legacy <see cref="RegisterCustomAgent"/> registrations.
    /// </para>
    /// <para>
    /// <b>Use Cases:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item>Inspect available personas before switching</item>
    ///   <item>Display agent metadata in UI</item>
    ///   <item>Validate capabilities before invoking</item>
    ///   <item>Audit license tier requirements</item>
    /// </list>
    /// <para>
    /// <b>Introduced in:</b> v0.7.1b.
    /// </para>
    /// </remarks>
    /// <seealso cref="AgentConfiguration"/>
    AgentConfiguration? GetConfiguration(string agentId);
}
