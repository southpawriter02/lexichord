// -----------------------------------------------------------------------
// <copyright file="AgentListChangeReason.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Reasons for agent list changes.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="AgentListChangedEventArgs"/> to indicate why the
/// available agents list was updated. Consumers can use this to decide
/// how to respond (e.g., show a notification for license changes).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6c as part of the Agent Registry feature.
/// </para>
/// </remarks>
public enum AgentListChangeReason
{
    /// <summary>
    /// The user's license tier changed, causing agent availability to be recalculated.
    /// </summary>
    LicenseChanged,

    /// <summary>
    /// A custom agent was registered at runtime.
    /// </summary>
    AgentRegistered,

    /// <summary>
    /// A custom agent was unregistered.
    /// </summary>
    AgentUnregistered,

    /// <summary>
    /// The agent list was manually refreshed.
    /// </summary>
    Refreshed
}
