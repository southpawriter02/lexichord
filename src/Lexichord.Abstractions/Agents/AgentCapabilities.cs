// -----------------------------------------------------------------------
// <copyright file="AgentCapabilities.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Flags indicating the capabilities of an agent.
/// </summary>
/// <remarks>
/// <para>
/// Agent capabilities are used for feature discovery and UI adaptation.
/// The UI reads these flags to determine which features to enable or show.
/// </para>
/// <para>
/// Multiple capabilities can be combined using bitwise OR. For example,
/// a fully-featured agent might have:
/// <c>Chat | DocumentContext | RAGContext | StyleEnforcement | Streaming</c>
/// </para>
/// <para>
/// When implementing new agents, carefully consider which capabilities apply.
/// Claiming a capability that isn't fully implemented will lead to user confusion.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6a as part of the Agent Abstractions layer.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Minimal chat-only agent
/// AgentCapabilities.Chat
///
/// // Full-featured writing assistant
/// AgentCapabilities.Chat | AgentCapabilities.DocumentContext |
/// AgentCapabilities.RAGContext | AgentCapabilities.StyleEnforcement |
/// AgentCapabilities.Streaming
///
/// // Research agent without document context
/// AgentCapabilities.Chat | AgentCapabilities.RAGContext
/// </code>
/// </example>
[Flags]
public enum AgentCapabilities
{
    /// <summary>
    /// No special capabilities. Base agent with no context awareness.
    /// </summary>
    None = 0,

    /// <summary>
    /// Supports conversational chat with message history.
    /// </summary>
    /// <remarks>
    /// When enabled, the agent can maintain context across multiple turns
    /// within a conversation. The UI will show conversation controls.
    /// </remarks>
    Chat = 1,

    /// <summary>
    /// Can access and reason about document content.
    /// </summary>
    /// <remarks>
    /// When enabled, the agent receives the current document content
    /// (or selection) as part of the context. The UI will show the
    /// document context indicator in the context panel.
    /// </remarks>
    DocumentContext = 2,

    /// <summary>
    /// Uses semantic search for retrieval-augmented generation.
    /// </summary>
    /// <remarks>
    /// When enabled, the agent performs semantic search against the
    /// project's knowledge base to find relevant content. Retrieved
    /// chunks are injected into the prompt context and cited in responses.
    /// </remarks>
    RAGContext = 4,

    /// <summary>
    /// Applies and enforces style rules.
    /// </summary>
    /// <remarks>
    /// When enabled, the agent loads applicable style rules and instructs
    /// the LLM to follow them. The UI will show style context in the
    /// context panel.
    /// </remarks>
    StyleEnforcement = 8,

    /// <summary>
    /// Supports streaming responses.
    /// </summary>
    /// <remarks>
    /// When enabled (and user has Teams license), responses are streamed
    /// token-by-token via <see cref="Lexichord.Abstractions.Contracts.LLM.StreamingChatToken"/>.
    /// When disabled or user lacks Teams license, responses are returned in batch.
    /// </remarks>
    Streaming = 16,

    /// <summary>
    /// Can generate or analyze code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the agent can generate code snippets, analyze code structure,
    /// explain code behavior, and suggest code improvements.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.7.1a as part of specialist agent capabilities.
    /// </para>
    /// </remarks>
    CodeGeneration = 32,

    /// <summary>
    /// Can perform research and cite sources.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the agent can search for information, synthesize findings,
    /// and provide citations for claims. Typically combined with RAGContext.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.7.1a as part of specialist agent capabilities.
    /// </para>
    /// </remarks>
    ResearchAssistance = 64,

    /// <summary>
    /// Can summarize long-form content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the agent can condense large documents, extract key points,
    /// and create executive summaries while preserving essential information.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.7.1a as part of specialist agent capabilities.
    /// </para>
    /// </remarks>
    Summarization = 128,

    /// <summary>
    /// Can analyze and suggest improvements to structure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the agent can evaluate document organization, identify
    /// structural weaknesses, and recommend reorganization strategies.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.7.1a as part of specialist agent capabilities.
    /// </para>
    /// </remarks>
    StructureAnalysis = 256,

    /// <summary>
    /// Can help with brainstorming and ideation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the agent can generate creative ideas, explore alternatives,
    /// facilitate ideation sessions, and help overcome creative blocks.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.7.1a as part of specialist agent capabilities.
    /// </para>
    /// </remarks>
    Brainstorming = 512,

    /// <summary>
    /// Can translate between languages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the agent can translate text between languages while
    /// preserving tone, style, and cultural nuances.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.7.1a as part of specialist agent capabilities.
    /// </para>
    /// </remarks>
    Translation = 1024,

    /// <summary>
    /// All standard writing assistant capabilities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Convenience combination for fully-featured writing assistants.
    /// Equivalent to <c>Chat | DocumentContext | RAGContext | StyleEnforcement | Streaming |
    /// CodeGeneration | ResearchAssistance | Summarization | StructureAnalysis |
    /// Brainstorming | Translation</c>.
    /// </para>
    /// <para>
    /// <b>Extended in:</b> v0.7.1a to include specialist agent capabilities.
    /// </para>
    /// </remarks>
    All = Chat | DocumentContext | RAGContext | StyleEnforcement | Streaming |
          CodeGeneration | ResearchAssistance | Summarization |
          StructureAnalysis | Brainstorming | Translation
}
