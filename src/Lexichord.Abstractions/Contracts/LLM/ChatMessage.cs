// -----------------------------------------------------------------------
// <copyright file="ChatMessage.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Represents a single message in a chat conversation.
/// </summary>
/// <remarks>
/// <para>
/// This immutable record encapsulates the role and content of a conversation message.
/// Use the factory methods <see cref="System"/>, <see cref="User"/>, and <see cref="Assistant"/>
/// to create instances with proper validation.
/// </para>
/// <para>
/// Messages are designed to be provider-agnostic and can be serialized to any LLM API format.
/// </para>
/// </remarks>
/// <param name="Role">The role of the message sender (System, User, Assistant, or Tool).</param>
/// <param name="Content">The textual content of the message. Cannot be null.</param>
/// <example>
/// <code>
/// // Using factory methods (recommended)
/// var systemMsg = ChatMessage.System("You are a helpful assistant.");
/// var userMsg = ChatMessage.User("Hello, how are you?");
/// var assistantMsg = ChatMessage.Assistant("I'm doing well, thank you!");
///
/// // Direct construction
/// var message = new ChatMessage(ChatRole.User, "Hello!");
/// </code>
/// </example>
public record ChatMessage(ChatRole Role, string Content)
{
    /// <summary>
    /// Gets the textual content of the message.
    /// </summary>
    /// <value>The message content. Never null.</value>
    public string Content { get; init; } = Content ?? throw new ArgumentNullException(nameof(Content));

    /// <summary>
    /// Creates a system instruction message.
    /// </summary>
    /// <param name="content">The system instruction content.</param>
    /// <returns>A new <see cref="ChatMessage"/> with <see cref="ChatRole.System"/> role.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is empty or whitespace.</exception>
    /// <remarks>
    /// System messages are used to set the behavior, persona, or context for the AI assistant.
    /// They are typically placed at the beginning of the conversation.
    /// </remarks>
    /// <example>
    /// <code>
    /// var systemPrompt = ChatMessage.System("You are a creative writing assistant. Always respond in a professional tone.");
    /// </code>
    /// </example>
    public static ChatMessage System([NotNull] string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));
        return new ChatMessage(ChatRole.System, content);
    }

    /// <summary>
    /// Creates a user message.
    /// </summary>
    /// <param name="content">The user's message content.</param>
    /// <returns>A new <see cref="ChatMessage"/> with <see cref="ChatRole.User"/> role.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is empty or whitespace.</exception>
    /// <remarks>
    /// User messages represent input from the human participant in the conversation.
    /// </remarks>
    /// <example>
    /// <code>
    /// var userMessage = ChatMessage.User("Can you help me write a story about dragons?");
    /// </code>
    /// </example>
    public static ChatMessage User([NotNull] string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));
        return new ChatMessage(ChatRole.User, content);
    }

    /// <summary>
    /// Creates an assistant message.
    /// </summary>
    /// <param name="content">The assistant's response content.</param>
    /// <returns>A new <see cref="ChatMessage"/> with <see cref="ChatRole.Assistant"/> role.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is empty or whitespace.</exception>
    /// <remarks>
    /// Assistant messages represent AI model responses. When constructing conversation history,
    /// use this method to include previous AI outputs.
    /// </remarks>
    /// <example>
    /// <code>
    /// var assistantResponse = ChatMessage.Assistant("Once upon a time, in a land of fire and ice, there lived a magnificent dragon...");
    /// </code>
    /// </example>
    public static ChatMessage Assistant([NotNull] string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));
        return new ChatMessage(ChatRole.Assistant, content);
    }

    /// <summary>
    /// Creates a tool response message.
    /// </summary>
    /// <param name="content">The tool response content, typically JSON.</param>
    /// <returns>A new <see cref="ChatMessage"/> with <see cref="ChatRole.Tool"/> role.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is empty or whitespace.</exception>
    /// <remarks>
    /// Tool messages contain the results of function or tool calls made by the AI.
    /// The content format depends on the specific tool and provider requirements.
    /// </remarks>
    /// <example>
    /// <code>
    /// var toolResult = ChatMessage.Tool("{\"temperature\": 72, \"conditions\": \"sunny\"}");
    /// </code>
    /// </example>
    public static ChatMessage Tool([NotNull] string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));
        return new ChatMessage(ChatRole.Tool, content);
    }
}
