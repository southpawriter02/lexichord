// -----------------------------------------------------------------------
// <copyright file="LicenseTierException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Exception thrown when a user's license tier is insufficient for the requested operation.
/// </summary>
/// <remarks>
/// <para>
/// Thrown by <see cref="IAgentRegistry.GetAgent"/> when the user's license tier
/// does not meet the agent's <see cref="RequiresLicenseAttribute"/> requirement,
/// or by <see cref="IAgentRegistry.RegisterCustomAgent"/> when the user lacks
/// a Teams license.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6c as part of the Agent Registry feature.
/// </para>
/// </remarks>
public sealed class LicenseTierException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="LicenseTierException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="requiredTier">The minimum license tier required.</param>
    public LicenseTierException(string message, LicenseTier requiredTier)
        : base(message)
    {
        RequiredTier = requiredTier;
    }

    /// <summary>
    /// Gets the minimum license tier required for the operation.
    /// </summary>
    public LicenseTier RequiredTier { get; }
}
