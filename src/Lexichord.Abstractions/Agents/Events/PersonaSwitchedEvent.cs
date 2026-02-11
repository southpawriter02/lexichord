// -----------------------------------------------------------------------
// <copyright file="PersonaSwitchedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Abstractions.Agents.Events;

/// <summary>
/// Published when a user switches the active persona for an agent.
/// </summary>
/// <param name="AgentId">The agent whose persona was changed.</param>
/// <param name="PreviousPersonaId">The ID of the previous persona, or null if using default.</param>
/// <param name="NewPersonaId">The ID of the newly activated persona.</param>
/// <param name="Timestamp">The UTC timestamp when the switch occurred.</param>
/// <remarks>
/// <para>
/// This event is published by <see cref="IAgentRegistry.SwitchPersona"/> when
/// a persona switch is successfully applied. This occurs both for cached agents
/// (immediate application) and uncached agents (preference recording).
/// </para>
/// <para>
/// <b>Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item>Update UI to reflect active persona</item>
///   <item>Log persona usage analytics</item>
///   <item>Track persona switching patterns</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.1b as part of runtime persona switching.
/// </para>
/// </remarks>
/// <seealso cref="IAgentRegistry.SwitchPersona"/>
/// <seealso cref="IPersonaAwareAgent.ApplyPersona"/>
public sealed record PersonaSwitchedEvent(
    string AgentId,
    string? PreviousPersonaId,
    string NewPersonaId,
    DateTimeOffset Timestamp
) : INotification;
