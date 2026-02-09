// -----------------------------------------------------------------------
// <copyright file="AgentRequest.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Abstractions.Agents;

/// <summary>
/// Immutable record capturing all parameters for an agent invocation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="AgentRequest"/> encapsulates everything the agent needs to
/// generate a response: the user's message, conversation history, and
/// context references (document path, selection).
/// </para>
/// <para>
/// This record is immutable by design, ensuring thread-safety when passed
/// between components. Use <c>with</c> expressions to create modified copies.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6a as part of the Agent Abstractions layer.
/// </para>
/// </remarks>
/// <param name="UserMessage">
/// The user's input message. This is the primary content the agent will respond to.
/// Should not be empty or whitespace-only; use <see cref="Validate"/> to enforce.
/// </param>
/// <param name="History">
/// Optional conversation history for multi-turn dialogues. Messages are ordered
/// chronologically (oldest first). Set to <c>null</c> for single-turn interactions.
/// </param>
/// <param name="DocumentPath">
/// Optional path to the current document for context injection. When provided,
/// agents with <see cref="AgentCapabilities.DocumentContext"/> will load and
/// inject the document content. Set to <c>null</c> if no document context is needed.
/// </param>
/// <param name="Selection">
/// Optional selected text from the document. When provided, the agent focuses
/// on this specific content rather than the entire document. Useful for
/// targeted questions about specific passages.
/// </param>
/// <example>
/// <code>
/// // Simple chat request
/// var request1 = new AgentRequest("What is the hero's journey?");
///
/// // Request with document context
/// var request2 = new AgentRequest(
///     "Summarize this chapter",
///     DocumentPath: "/path/to/chapter1.md"
/// );
///
/// // Request with selection and history
/// var request3 = new AgentRequest(
///     "How can I improve this paragraph?",
///     History: previousMessages,
///     DocumentPath: "/path/to/novel.md",
///     Selection: "The sun set slowly..."
/// );
/// </code>
/// </example>
public record AgentRequest(
    string UserMessage,
    IReadOnlyList<ChatMessage>? History = null,
    string? DocumentPath = null,
    string? Selection = null)
{
    /// <summary>
    /// Validates the request and throws if invalid.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if <see cref="UserMessage"/> is <c>null</c>, empty, or whitespace-only.
    /// </exception>
    /// <remarks>
    /// LOGIC: Validates that the user message contains meaningful content.
    /// This should be called before passing the request to an agent to
    /// fail fast with a clear error message rather than sending an empty
    /// prompt to the LLM provider.
    /// </remarks>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(UserMessage))
            throw new ArgumentException("User message cannot be empty", nameof(UserMessage));
    }

    /// <summary>
    /// Indicates whether this request includes document context.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="DocumentPath"/> is non-null and non-empty;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Used by agents with <see cref="AgentCapabilities.DocumentContext"/>
    /// to determine whether to load document content for context injection.
    /// </remarks>
    public bool HasDocumentContext => !string.IsNullOrEmpty(DocumentPath);

    /// <summary>
    /// Indicates whether this request includes a text selection.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Selection"/> is non-null and non-empty;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: When true, agents should focus on the selected text rather than
    /// the full document. This enables targeted queries about specific passages.
    /// </remarks>
    public bool HasSelection => !string.IsNullOrEmpty(Selection);

    /// <summary>
    /// Indicates whether this request includes conversation history.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="History"/> is non-null and contains at least
    /// one message; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Used by agents with <see cref="AgentCapabilities.Chat"/> to determine
    /// whether to include prior messages in the prompt. Multi-turn conversations
    /// provide better context but consume more tokens.
    /// </remarks>
    public bool HasHistory => History is { Count: > 0 };

    /// <summary>
    /// Gets the number of messages in the history.
    /// </summary>
    /// <value>
    /// The count of messages in <see cref="History"/>, or 0 if history is <c>null</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: Null-safe count accessor used for logging and context window estimation.
    /// </remarks>
    public int HistoryCount => History?.Count ?? 0;
}
