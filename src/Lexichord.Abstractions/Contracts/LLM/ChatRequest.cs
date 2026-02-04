// -----------------------------------------------------------------------
// <copyright file="ChatRequest.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Represents a chat completion request containing messages and configuration options.
/// </summary>
/// <remarks>
/// <para>
/// This immutable record encapsulates all the data needed to make a chat completion request
/// to any LLM provider. Use the factory methods for common scenarios or the
/// <see cref="ChatRequestBuilder"/> for complex request construction.
/// </para>
/// <para>
/// The request is provider-agnostic and will be adapted to each provider's API format
/// by the respective <see cref="IChatCompletionService"/> implementation.
/// </para>
/// </remarks>
/// <param name="Messages">The conversation messages. Must contain at least one message.</param>
/// <param name="Options">Configuration options for the request. When null, defaults are used.</param>
/// <example>
/// <code>
/// // Simple user message
/// var request = ChatRequest.FromUserMessage("Hello!");
///
/// // With system prompt
/// var request = ChatRequest.WithSystemPrompt(
///     "You are a helpful assistant.",
///     "What is the capital of France?"
/// );
///
/// // With custom options
/// var request = ChatRequest.FromUserMessage(
///     "Write a creative story.",
///     ChatOptions.Creative.WithMaxTokens(1000)
/// );
///
/// // Using builder for complex requests
/// var request = ChatRequestBuilder.Create()
///     .WithSystemPrompt("You are a code reviewer.")
///     .AddUserMessage("Review this code:")
///     .AddAssistantMessage("I'll analyze the code...")
///     .AddUserMessage("What about performance?")
///     .WithOptions(ChatOptions.Precise)
///     .Build();
/// </code>
/// </example>
public record ChatRequest(ImmutableArray<ChatMessage> Messages, ChatOptions? Options = null)
{
    /// <summary>
    /// Gets the conversation messages.
    /// </summary>
    /// <value>An immutable array of chat messages. Never empty.</value>
    /// <exception cref="ArgumentException">Thrown during construction if messages is empty.</exception>
    public ImmutableArray<ChatMessage> Messages { get; init; } = Messages.IsDefaultOrEmpty
        ? throw new ArgumentException("Messages cannot be empty.", nameof(Messages))
        : Messages;

    /// <summary>
    /// Creates a simple request with a single user message.
    /// </summary>
    /// <param name="content">The user message content.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>A new <see cref="ChatRequest"/> with the specified user message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is empty or whitespace.</exception>
    /// <remarks>
    /// This is the simplest way to create a chat request for single-turn conversations.
    /// For multi-turn conversations or complex scenarios, use <see cref="ChatRequestBuilder"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = ChatRequest.FromUserMessage("What is 2 + 2?");
    /// var response = await service.CompleteAsync(request);
    /// </code>
    /// </example>
    public static ChatRequest FromUserMessage(
        [NotNull] string content,
        ChatOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        return new ChatRequest(
            ImmutableArray.Create(ChatMessage.User(content)),
            options);
    }

    /// <summary>
    /// Creates a request with a system prompt and user message.
    /// </summary>
    /// <param name="systemPrompt">The system instruction content.</param>
    /// <param name="userMessage">The user message content.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>A new <see cref="ChatRequest"/> with system prompt and user message.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="systemPrompt"/> or <paramref name="userMessage"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="systemPrompt"/> or <paramref name="userMessage"/> is empty or whitespace.
    /// </exception>
    /// <remarks>
    /// This method creates a two-message conversation with a system instruction
    /// followed by a user message. This is the most common pattern for guided AI interactions.
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = ChatRequest.WithSystemPrompt(
    ///     "You are a professional translator. Translate text to French.",
    ///     "Hello, how are you?"
    /// );
    /// </code>
    /// </example>
    public static ChatRequest WithSystemPrompt(
        [NotNull] string systemPrompt,
        [NotNull] string userMessage,
        ChatOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt, nameof(systemPrompt));
        ArgumentException.ThrowIfNullOrWhiteSpace(userMessage, nameof(userMessage));

        return new ChatRequest(
            ImmutableArray.Create(
                ChatMessage.System(systemPrompt),
                ChatMessage.User(userMessage)),
            options);
    }

    /// <summary>
    /// Creates a new request with additional messages appended.
    /// </summary>
    /// <param name="messages">The messages to append.</param>
    /// <returns>A new <see cref="ChatRequest"/> with the additional messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="messages"/> is null.</exception>
    /// <remarks>
    /// This method is useful for continuing conversations by adding new messages
    /// to an existing request.
    /// </remarks>
    public ChatRequest WithMessages(params ChatMessage[] messages)
    {
        ArgumentNullException.ThrowIfNull(messages, nameof(messages));
        return this with { Messages = Messages.AddRange(messages) };
    }

    /// <summary>
    /// Creates a new request with the specified options.
    /// </summary>
    /// <param name="options">The options to use.</param>
    /// <returns>A new <see cref="ChatRequest"/> with the specified options.</returns>
    public ChatRequest WithOptions(ChatOptions options) => this with { Options = options };

    /// <summary>
    /// Gets the system message from this request, if present.
    /// </summary>
    /// <returns>The system message content, or null if no system message exists.</returns>
    public string? GetSystemPrompt()
        => Messages.FirstOrDefault(m => m.Role == ChatRole.System)?.Content;

    /// <summary>
    /// Gets the last user message from this request.
    /// </summary>
    /// <returns>The last user message content, or null if no user messages exist.</returns>
    public string? GetLastUserMessage()
        => Messages.LastOrDefault(m => m.Role == ChatRole.User)?.Content;
}
