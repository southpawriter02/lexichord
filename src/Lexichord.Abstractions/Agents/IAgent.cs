// -----------------------------------------------------------------------
// <copyright file="IAgent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Contract for all agent implementations in Lexichord.
/// </summary>
/// <remarks>
/// <para>
/// Agents are the primary abstraction for AI-powered assistants in Lexichord.
/// Each agent encapsulates a specific set of capabilities (chat, document analysis,
/// style enforcement, etc.) and exposes them through a unified invocation pattern.
/// </para>
/// <para>
/// Implementations should be stateless between invocations. Conversation state is
/// managed externally and passed via <see cref="AgentRequest.History"/>.
/// </para>
/// <para>
/// Agents may be decorated with <see cref="Lexichord.Abstractions.Contracts.RequiresLicenseAttribute"/>
/// to indicate minimum license tier requirements. The agent registry enforces
/// these requirements during agent discovery and selection.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6a as part of the Agent Abstractions layer.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [RequiresLicense(LicenseTier.WriterPro)]
/// public class CoPilotAgent : IAgent
/// {
///     public string AgentId => "co-pilot";
///     public string Name => "Co-pilot";
///     public string Description => "General writing assistant";
///
///     public IPromptTemplate Template => _template;
///
///     public AgentCapabilities Capabilities =>
///         AgentCapabilities.Chat | AgentCapabilities.DocumentContext;
///
///     public async Task&lt;AgentResponse&gt; InvokeAsync(AgentRequest request, CancellationToken ct)
///     {
///         // 1. Assemble context from document, selection, and RAG sources
///         // 2. Render the prompt template with assembled context
///         // 3. Add conversation history to the message stream
///         // 4. Invoke the LLM completion service
///         // 5. Extract citations from RAG-sourced content
///         // 6. Calculate usage metrics
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="AgentRequest"/>
/// <seealso cref="AgentResponse"/>
/// <seealso cref="AgentCapabilities"/>
/// <seealso cref="UsageMetrics"/>
public interface IAgent
{
    /// <summary>
    /// Unique identifier for this agent.
    /// </summary>
    /// <value>
    /// A string identifying the agent (e.g., "co-pilot", "research-assistant", "style-editor").
    /// Should be lowercase, kebab-case, and stable across versions.
    /// </value>
    /// <remarks>
    /// LOGIC: The agent ID is used for:
    /// <list type="bullet">
    ///   <item><description>Configuration lookup (per-agent settings)</description></item>
    ///   <item><description>Telemetry tagging (usage attribution)</description></item>
    ///   <item><description>User preferences (default agent selection)</description></item>
    ///   <item><description>Conversation metadata (which agent generated a response)</description></item>
    /// </list>
    /// This ID should remain stable across application restarts and version upgrades.
    /// </remarks>
    string AgentId { get; }

    /// <summary>
    /// Human-readable name of the agent.
    /// </summary>
    /// <value>
    /// A concise display name (2-3 words max), e.g., "Co-pilot", "Research Assistant".
    /// </value>
    /// <remarks>
    /// LOGIC: Displayed in the agent selector UI dropdown and usage reports.
    /// Should be descriptive enough for users to understand the agent's purpose at a glance.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Description of the agent's purpose and capabilities.
    /// </summary>
    /// <value>
    /// A 1-2 sentence description explaining what the agent does and when to use it.
    /// </value>
    /// <remarks>
    /// LOGIC: Displayed in the agent selector tooltip and help documentation.
    /// Should help users choose the right agent for their task.
    /// </remarks>
    string Description { get; }

    /// <summary>
    /// The prompt template used by this agent.
    /// </summary>
    /// <value>
    /// An <see cref="IPromptTemplate"/> loaded from the template repository.
    /// </value>
    /// <remarks>
    /// LOGIC: Templates are loaded from <see cref="IPromptTemplateRepository"/> and define
    /// the system prompt, context injection points, and output formatting.
    /// The template ID should match a registered template in the repository.
    /// </remarks>
    IPromptTemplate Template { get; }

    /// <summary>
    /// Flags indicating the capabilities of this agent.
    /// </summary>
    /// <value>
    /// One or more <see cref="AgentCapabilities"/> flags combined with bitwise OR.
    /// </value>
    /// <remarks>
    /// LOGIC: Used by the UI to adapt available features (show/hide context panel,
    /// enable/disable style enforcement, etc.) and by the registry to filter
    /// agents based on required capabilities.
    /// </remarks>
    AgentCapabilities Capabilities { get; }

    /// <summary>
    /// Invokes the agent with the given request.
    /// </summary>
    /// <param name="request">The invocation parameters including message and context.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the
    /// <see cref="AgentResponse"/> with content, citations, and usage metrics.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="request"/> fails validation.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested via <paramref name="ct"/>.</exception>
    /// <remarks>
    /// <para>
    /// This is the primary entry point for agent execution. Implementations should:
    /// </para>
    /// <list type="number">
    ///   <item><description>Assemble context from document, selection, and RAG sources</description></item>
    ///   <item><description>Render the prompt template with assembled context</description></item>
    ///   <item><description>Add conversation history to the message stream</description></item>
    ///   <item><description>Invoke the LLM completion service</description></item>
    ///   <item><description>Extract citations from RAG-sourced content</description></item>
    ///   <item><description>Calculate usage metrics</description></item>
    /// </list>
    /// <para>
    /// Implementations should respect the <paramref name="ct"/> cancellation token
    /// and abort promptly when cancellation is requested.
    /// </para>
    /// </remarks>
    Task<AgentResponse> InvokeAsync(AgentRequest request, CancellationToken ct = default);
}
