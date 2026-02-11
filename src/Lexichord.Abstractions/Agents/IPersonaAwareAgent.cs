// -----------------------------------------------------------------------
// <copyright file="IPersonaAwareAgent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Marker interface for agents that support runtime persona switching.
/// </summary>
/// <remarks>
/// <para>
/// Agents implementing this interface can dynamically change their personality
/// and behavior by applying different <see cref="AgentPersona"/> configurations
/// without requiring recreation of the agent instance.
/// </para>
/// <para>
/// <b>Persona Application Flow:</b>
/// </para>
/// <list type="number">
///   <item>
///     <see cref="IAgentRegistry.SwitchPersona"/> is called with target persona ID
///   </item>
///   <item>
///     Registry validates persona exists in agent's configuration
///   </item>
///   <item>
///     If agent implements <see cref="IPersonaAwareAgent"/>, calls <see cref="ApplyPersona"/>
///   </item>
///   <item>
///     Agent updates internal state (e.g., temperature, system prompt overrides)
///   </item>
///   <item>
///     <see cref="ActivePersona"/> property reflects the new persona
///   </item>
/// </list>
/// <para>
/// <b>Implementation Requirements:</b>
/// </para>
/// <list type="bullet">
///   <item>Thread-safe persona application (may be called from multiple threads)</item>
///   <item>Preserve conversation history when switching personas</item>
///   <item>Apply persona-specific overrides (temperature, system prompt)</item>
///   <item>Update <see cref="ActivePersona"/> property atomically</item>
/// </list>
/// <para>
/// <b>Non-Implementing Agents:</b> Agents that don't implement this interface can
/// still be registered with multiple personas, but persona switches will not affect
/// the runtime behavior. The registry will record the persona preference for future
/// agent instantiations.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.1b as part of runtime persona management.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class EditorAgent : BaseAgent, IPersonaAwareAgent
/// {
///     private AgentPersona? _activePersona;
///
///     public AgentPersona? ActivePersona => _activePersona;
///
///     public void ApplyPersona(AgentPersona persona)
///     {
///         _activePersona = persona;
///
///         // Update internal state based on persona
///         if (persona.Temperature is not null)
///         {
///             _chatOptions = _chatOptions with { Temperature = persona.Temperature };
///         }
///
///         if (persona.SystemPromptOverride is not null)
///         {
///             _systemPrompt = persona.SystemPromptOverride;
///         }
///     }
///
///     public void ResetToDefaultPersona()
///     {
///         _activePersona = Configuration.DefaultPersona;
///         // Reset to base configuration values
///         _chatOptions = Configuration.DefaultOptions;
///         _systemPrompt = Template.SystemPrompt;
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="AgentPersona"/>
/// <seealso cref="IAgentRegistry.SwitchPersona"/>
/// <seealso cref="AgentConfiguration"/>
public interface IPersonaAwareAgent : IAgent
{
    /// <summary>
    /// Gets the currently active persona, or null if using the agent's default behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property reflects the most recently applied persona via <see cref="ApplyPersona"/>.
    /// If no persona has been explicitly applied, this may return the configuration's
    /// default persona or null.
    /// </para>
    /// <para>
    /// <b>Thread Safety:</b> This property should be thread-safe for concurrent reads.
    /// </para>
    /// </remarks>
    AgentPersona? ActivePersona { get; }

    /// <summary>
    /// Applies a new persona to this agent instance.
    /// </summary>
    /// <param name="persona">The persona to apply. Must not be null.</param>
    /// <remarks>
    /// <para>
    /// LOGIC: This method updates the agent's internal state to reflect the persona's
    /// settings:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <b>Temperature:</b> Override the agent's temperature setting if persona
    ///     specifies a value
    ///   </item>
    ///   <item>
    ///     <b>System Prompt:</b> Apply <see cref="AgentPersona.SystemPromptOverride"/>
    ///     if specified
    ///   </item>
    ///   <item>
    ///     <b>Active Persona:</b> Update <see cref="ActivePersona"/> property
    ///   </item>
    /// </list>
    /// <para>
    /// <b>Conversation Preservation:</b> Implementations should preserve the conversation
    /// history when switching personas. The persona affects future interactions, not
    /// past ones.
    /// </para>
    /// <para>
    /// <b>Thread Safety:</b> This method may be called from multiple threads and should
    /// be implemented with appropriate synchronization.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="persona"/> is null.</exception>
    void ApplyPersona(AgentPersona persona);

    /// <summary>
    /// Resets the agent to its configuration's default persona.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: This method restores the agent to its baseline configuration state:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     If <see cref="AgentConfiguration.DefaultPersona"/> is defined, applies that persona
    ///   </item>
    ///   <item>
    ///     Otherwise, clears all persona overrides and uses base configuration values
    ///   </item>
    ///   <item>
    ///     Updates <see cref="ActivePersona"/> to reflect the default (may be null)
    ///   </item>
    /// </list>
    /// <para>
    /// <b>Use Case:</b> Useful when user wants to return to "stock" agent behavior
    /// after experimenting with different personas.
    /// </para>
    /// </remarks>
    void ResetToDefaultPersona();
}
