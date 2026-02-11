// -----------------------------------------------------------------------
// <copyright file="AgentDefinitionAttribute.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Marks a class as an agent implementation for assembly scanning.
/// </summary>
/// <remarks>
/// <para>
/// This attribute enables declarative agent registration via assembly scanning.
/// Classes decorated with this attribute will be discovered by
/// <c>AgentDefinitionScanner</c> during module initialization and automatically
/// registered with the <see cref="IAgentRegistry"/>.
/// </para>
/// <para>
/// <b>Requirements:</b>
/// </para>
/// <list type="bullet">
///   <item>The decorated class must implement <see cref="IAgent"/></item>
///   <item>The class must be public and have a public constructor</item>
///   <item>The <see cref="AgentId"/> must be unique across all agents</item>
/// </list>
/// <para>
/// <b>Priority System:</b> The <see cref="Priority"/> property controls the order
/// of registration when multiple agents are discovered. Lower values register first.
/// This is useful when agents have dependencies on each other or when you want to
/// ensure a specific agent is available early.
/// </para>
/// <list type="bullet">
///   <item><b>Priority 0-50:</b> Critical agents (e.g., default co-pilot)</item>
///   <item><b>Priority 51-100:</b> Core agents (default)</item>
///   <item><b>Priority 101-200:</b> Specialist agents</item>
///   <item><b>Priority 201+:</b> Experimental or custom agents</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.1b as part of declarative agent registration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [AgentDefinition("editor", Priority = 75)]
/// [RequiresLicense(LicenseTier.WriterPro)]
/// public class EditorAgent : BaseAgent
/// {
///     public EditorAgent(
///         IChatCompletionService llm,
///         IPromptRenderer renderer,
///         ILogger&lt;EditorAgent&gt; logger)
///         : base(llm, renderer, logger)
///     {
///     }
///
///     public override string AgentId => "editor";
///     public override string Name => "The Editor";
///     // ... implementation
/// }
/// </code>
/// </example>
/// <seealso cref="IAgent"/>
/// <seealso cref="IAgentRegistry.RegisterAgent"/>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AgentDefinitionAttribute : Attribute
{
    /// <summary>
    /// Gets the unique identifier for the agent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The agent ID must:
    /// </para>
    /// <list type="bullet">
    ///   <item>Be in kebab-case format (lowercase with hyphens)</item>
    ///   <item>Match the value returned by <see cref="IAgent.AgentId"/></item>
    ///   <item>Be unique across all agents in the system</item>
    /// </list>
    /// <para>
    /// <b>Examples:</b> "co-pilot", "editor", "simplifier", "custom-reviewer"
    /// </para>
    /// </remarks>
    public string AgentId { get; }

    /// <summary>
    /// Gets or sets the priority for registration order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lower values register first. Default is 100 (core agents).
    /// </para>
    /// <para>
    /// Use lower priorities (0-50) for critical agents that other agents may depend on.
    /// Use higher priorities (101+) for specialized or experimental agents.
    /// </para>
    /// </remarks>
    /// <value>
    /// The registration priority. Defaults to 100.
    /// </value>
    public int Priority { get; init; } = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentDefinitionAttribute"/> class.
    /// </summary>
    /// <param name="agentId">The unique identifier for the agent.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="agentId"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// The <paramref name="agentId"/> is validated at scan time by the
    /// <c>AgentDefinitionScanner</c>. Invalid agent IDs will be logged and skipped
    /// during registration.
    /// </para>
    /// </remarks>
    public AgentDefinitionAttribute(string agentId)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
    }
}
