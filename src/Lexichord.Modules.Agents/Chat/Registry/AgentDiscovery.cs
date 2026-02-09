// -----------------------------------------------------------------------
// <copyright file="AgentDiscovery.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Encapsulates the logic for discovering IAgent implementations
//   from the DI container and extracting their metadata via reflection.
//   Separated from AgentRegistry for clarity and testability.
// -----------------------------------------------------------------------

using System.Reflection;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Chat.Registry;

/// <summary>
/// Provides agent discovery and metadata extraction logic.
/// </summary>
/// <remarks>
/// <para>
/// This static class encapsulates the reflection-based agent discovery
/// logic used by <see cref="AgentRegistry"/>. It scans the DI container
/// for <see cref="IAgent"/> implementations and extracts their metadata.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6c as part of the Agent Registry feature.
/// </para>
/// </remarks>
internal static class AgentDiscovery
{
    /// <summary>
    /// Discovers all <see cref="IAgent"/> implementations from the DI container.
    /// </summary>
    /// <param name="serviceProvider">The DI service provider.</param>
    /// <param name="logger">Logger for discovery diagnostics.</param>
    /// <returns>
    /// A dictionary mapping agent IDs to their extracted metadata.
    /// </returns>
    /// <remarks>
    /// LOGIC: Creates a scope to resolve all IAgent registrations,
    /// then extracts metadata from each via <see cref="ExtractMetadata"/>.
    /// Agents with duplicate IDs will have the last-registered instance win.
    /// </remarks>
    internal static Dictionary<string, AgentMetadata> DiscoverAgents(
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        logger.LogDebug("Discovering agents from DI container");

        var result = new Dictionary<string, AgentMetadata>();

        using var scope = serviceProvider.CreateScope();
        var agents = scope.ServiceProvider.GetServices<IAgent>();

        foreach (var agent in agents)
        {
            var metadata = ExtractMetadata(agent);
            result[agent.AgentId] = metadata;

            logger.LogDebug(
                "Discovered agent: {AgentId} ({Name}), License: {License}",
                agent.AgentId, agent.Name, metadata.RequiredLicense);
        }

        return result;
    }

    /// <summary>
    /// Extracts metadata from an agent instance using reflection.
    /// </summary>
    /// <param name="agent">The agent to extract metadata from.</param>
    /// <returns>A new <see cref="AgentMetadata"/> record.</returns>
    /// <remarks>
    /// LOGIC: Reads the <see cref="RequiresLicenseAttribute"/> from the agent's
    /// concrete type to determine the required license tier. If no attribute
    /// is present, defaults to <see cref="LicenseTier.Core"/>.
    /// </remarks>
    internal static AgentMetadata ExtractMetadata(IAgent agent)
    {
        var agentType = agent.GetType();
        var licenseAttribute = agentType.GetCustomAttribute<RequiresLicenseAttribute>();
        var requiredLicense = licenseAttribute?.Tier ?? LicenseTier.Core;

        return new AgentMetadata(
            AgentId: agent.AgentId,
            Name: agent.Name,
            Description: agent.Description,
            Capabilities: agent.Capabilities,
            RequiredLicense: requiredLicense,
            AgentType: agentType);
    }
}
