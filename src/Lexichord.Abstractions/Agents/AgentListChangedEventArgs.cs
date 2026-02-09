// -----------------------------------------------------------------------
// <copyright file="AgentListChangedEventArgs.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Event arguments for agent list changes.
/// </summary>
/// <remarks>
/// <para>
/// Raised by <see cref="IAgentRegistry"/> when the available agents list
/// changes due to license tier changes, agent registration/unregistration,
/// or manual refresh.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6c as part of the Agent Registry feature.
/// </para>
/// </remarks>
public sealed class AgentListChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="AgentListChangedEventArgs"/>.
    /// </summary>
    /// <param name="reason">The reason the agent list changed.</param>
    /// <param name="availableAgents">The updated list of available agents.</param>
    public AgentListChangedEventArgs(
        AgentListChangeReason reason,
        IReadOnlyList<IAgent> availableAgents)
    {
        Reason = reason;
        AvailableAgents = availableAgents;
    }

    /// <summary>
    /// Gets the reason the agent list changed.
    /// </summary>
    public AgentListChangeReason Reason { get; }

    /// <summary>
    /// Gets the updated list of available agents after the change.
    /// </summary>
    public IReadOnlyList<IAgent> AvailableAgents { get; }
}
