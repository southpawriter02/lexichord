// -----------------------------------------------------------------------
// <copyright file="AgentConfigReloadedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Abstractions.Agents.Events;

/// <summary>
/// Published when an agent configuration is updated via hot-reload.
/// </summary>
/// <param name="AgentId">The agent whose configuration was reloaded.</param>
/// <param name="OldConfiguration">The previous configuration.</param>
/// <param name="NewConfiguration">The updated configuration.</param>
/// <param name="Timestamp">The UTC timestamp when reload occurred.</param>
/// <remarks>
/// <para>
/// This event is published by <see cref="IAgentRegistry.UpdateAgent"/> after
/// successfully hot-reloading an agent configuration. The old configuration
/// is included to allow subscribers to detect what changed (e.g., personas added,
/// temperature adjusted, capabilities modified).
/// </para>
/// <para>
/// <b>Hot-Reload Behavior:</b>
/// </para>
/// <list type="bullet">
///   <item>Cached agent instances are invalidated and recreated on next access</item>
///   <item>Active persona preferences are preserved</item>
///   <item>Factory function remains unchanged</item>
/// </list>
/// <para>
/// <b>Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item>Notify UI that agent definitions changed</item>
///   <item>Log configuration changes for audit</item>
///   <item>Trigger re-evaluation of agent selection</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.1b as part of configuration hot-reload support.
/// </para>
/// </remarks>
/// <seealso cref="IAgentRegistry.UpdateAgent"/>
public sealed record AgentConfigReloadedEvent(
    string AgentId,
    AgentConfiguration OldConfiguration,
    AgentConfiguration NewConfiguration,
    DateTimeOffset Timestamp
) : INotification;
