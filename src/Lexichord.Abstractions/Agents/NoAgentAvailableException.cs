// -----------------------------------------------------------------------
// <copyright file="NoAgentAvailableException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Exception thrown when no agents are available for the current license tier.
/// </summary>
/// <remarks>
/// <para>
/// Thrown by <see cref="IAgentRegistry.GetDefaultAgent"/> when no agents
/// are accessible after exhausting all fallback options (user preference,
/// "co-pilot", first available).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6c as part of the Agent Registry feature.
/// </para>
/// </remarks>
public sealed class NoAgentAvailableException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="NoAgentAvailableException"/>.
    /// </summary>
    public NoAgentAvailableException()
        : base("No agents are available for the current license tier") { }
}
