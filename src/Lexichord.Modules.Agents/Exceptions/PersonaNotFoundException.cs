// -----------------------------------------------------------------------
// <copyright file="PersonaNotFoundException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Exceptions;

/// <summary>
/// Exception thrown when a persona ID is not found for a specific agent.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown when attempting to switch to or apply a persona
/// that does not exist in the agent's <see cref="Abstractions.Agents.AgentConfiguration"/>.
/// </para>
/// <para>
/// <b>Common Causes:</b>
/// </para>
/// <list type="bullet">
///   <item>Typo in persona ID when calling <c>SwitchPersona</c></item>
///   <item>Persona removed from configuration after hot-reload</item>
///   <item>Attempting to use persona from a different agent</item>
///   <item>Configuration updated but UI not refreshed</item>
/// </list>
/// <para>
/// <b>Resolution:</b> Check available personas via
/// <c>AgentConfiguration.Personas</c> before attempting to switch.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.1b as part of persona management.
/// </para>
/// </remarks>
/// <seealso cref="Abstractions.Agents.IAgentRegistry.SwitchPersona"/>
/// <seealso cref="Abstractions.Agents.IAgentRegistry.GetAgentWithPersona"/>
public sealed class PersonaNotFoundException : Exception
{
    /// <summary>
    /// Gets the agent ID for which the persona was not found.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Gets the persona ID that was not found.
    /// </summary>
    public string PersonaId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonaNotFoundException"/> class.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="personaId">The persona ID that was not found.</param>
    public PersonaNotFoundException(string agentId, string personaId)
        : base($"Persona '{personaId}' not found for agent '{agentId}'")
    {
        AgentId = agentId;
        PersonaId = personaId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonaNotFoundException"/> class
    /// with a custom message.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="personaId">The persona ID that was not found.</param>
    /// <param name="message">The custom error message.</param>
    public PersonaNotFoundException(string agentId, string personaId, string message)
        : base(message)
    {
        AgentId = agentId;
        PersonaId = personaId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonaNotFoundException"/> class
    /// with a custom message and inner exception.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="personaId">The persona ID that was not found.</param>
    /// <param name="message">The custom error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PersonaNotFoundException(string agentId, string personaId, string message, Exception innerException)
        : base(message, innerException)
    {
        AgentId = agentId;
        PersonaId = personaId;
    }
}
