// -----------------------------------------------------------------------
// <copyright file="AgentAccessDeniedException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Agents.Exceptions;

/// <summary>
/// Exception thrown when the current license tier is insufficient for accessing an agent.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown when attempting to access an agent that requires a higher
/// license tier than the user's current subscription. It provides detailed context
/// about the requirement for user-facing error messages and upgrade prompts.
/// </para>
/// <para>
/// <b>Common Scenarios:</b>
/// </para>
/// <list type="bullet">
///   <item>Core user attempting to access WriterPro specialist agents</item>
///   <item>WriterPro user attempting to access Teams-only agents</item>
///   <item>Trial user after license expiration</item>
///   <item>Workspace agent requiring higher tier than individual license</item>
/// </list>
/// <para>
/// <b>Recommended UX:</b> Display an upgrade prompt with:
/// </para>
/// <list type="bullet">
///   <item>Clear message about required tier</item>
///   <item>Benefits of upgrading</item>
///   <item>Direct link to subscription management</item>
/// </list>
/// <para>
/// <b>Difference from LicenseTierException:</b> This exception is specific to
/// agent access and includes agent ID context, while <c>LicenseTierException</c>
/// is a general-purpose license exception.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.1b for factory-based agent registration.
/// </para>
/// </remarks>
/// <seealso cref="Abstractions.Agents.IAgentRegistry.GetAgent"/>
/// <seealso cref="Abstractions.Agents.IAgentRegistry.CanAccess"/>
public sealed class AgentAccessDeniedException : Exception
{
    /// <summary>
    /// Gets the agent ID that was denied access.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Gets the license tier required to access the agent.
    /// </summary>
    public LicenseTier RequiredTier { get; }

    /// <summary>
    /// Gets the user's current license tier.
    /// </summary>
    public LicenseTier CurrentTier { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentAccessDeniedException"/> class.
    /// </summary>
    /// <param name="agentId">The agent ID that was denied.</param>
    /// <param name="required">The license tier required.</param>
    /// <param name="current">The user's current license tier.</param>
    public AgentAccessDeniedException(string agentId, LicenseTier required, LicenseTier current)
        : base($"Agent '{agentId}' requires {required} tier (current: {current})")
    {
        AgentId = agentId;
        RequiredTier = required;
        CurrentTier = current;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentAccessDeniedException"/> class
    /// with a custom message.
    /// </summary>
    /// <param name="agentId">The agent ID that was denied.</param>
    /// <param name="required">The license tier required.</param>
    /// <param name="current">The user's current license tier.</param>
    /// <param name="message">The custom error message.</param>
    public AgentAccessDeniedException(string agentId, LicenseTier required, LicenseTier current, string message)
        : base(message)
    {
        AgentId = agentId;
        RequiredTier = required;
        CurrentTier = current;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentAccessDeniedException"/> class
    /// with a custom message and inner exception.
    /// </summary>
    /// <param name="agentId">The agent ID that was denied.</param>
    /// <param name="required">The license tier required.</param>
    /// <param name="current">The user's current license tier.</param>
    /// <param name="message">The custom error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AgentAccessDeniedException(
        string agentId,
        LicenseTier required,
        LicenseTier current,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        AgentId = agentId;
        RequiredTier = required;
        CurrentTier = current;
    }
}
