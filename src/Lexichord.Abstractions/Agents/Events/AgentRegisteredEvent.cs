// -----------------------------------------------------------------------
// <copyright file="AgentRegisteredEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Abstractions.Agents.Events;

/// <summary>
/// Published when a new agent is registered with the registry.
/// </summary>
/// <param name="Configuration">The agent configuration that was registered.</param>
/// <param name="Timestamp">The UTC timestamp when registration occurred.</param>
/// <remarks>
/// <para>
/// This event is published by <see cref="IAgentRegistry.RegisterAgent"/> after
/// successfully registering an agent configuration. Subscribers can use this
/// to react to new agent availability (e.g., update UI, log telemetry).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.1b as part of agent persona management.
/// </para>
/// </remarks>
/// <seealso cref="IAgentRegistry.RegisterAgent"/>
public sealed record AgentRegisteredEvent(
    AgentConfiguration Configuration,
    DateTimeOffset Timestamp
) : INotification;
