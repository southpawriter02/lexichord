// -----------------------------------------------------------------------
// <copyright file="ChatRequestBuilder.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Fluent builder for constructing <see cref="ChatRequest"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// This builder provides a fluent API for constructing complex chat requests
/// with multiple messages and custom configuration options.
/// </para>
/// <para>
/// The builder is mutable and not thread-safe. Create a new builder instance
/// for each request construction.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple request
/// var request = ChatRequestBuilder.Create()
///     .AddUserMessage("Hello!")
///     .Build();
///
/// // Multi-turn conversation
/// var request = ChatRequestBuilder.Create()
///     .WithSystemPrompt("You are a helpful assistant.")
///     .AddUserMessage("What is Python?")
///     .AddAssistantMessage("Python is a programming language...")
///     .AddUserMessage("Can you show me an example?")
///     .WithOptions(ChatOptions.Precise)
///     .Build();
/// </code>
/// </example>
public sealed class ChatRequestBuilder
{
    private readonly List<ChatMessage> _messages = [];
    private ChatOptions? _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatRequestBuilder"/> class.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="Create"/> factory method for a more fluent API.
    /// </remarks>
    private ChatRequestBuilder()
    {
    }

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    /// <returns>A new <see cref="ChatRequestBuilder"/> instance.</returns>
    /// <example>
    /// <code>
    /// var builder = ChatRequestBuilder.Create();
    /// </code>
    /// </example>
    public static ChatRequestBuilder Create() => new();

    /// <summary>
    /// Sets the system prompt for the conversation.
    /// </summary>
    /// <param name="content">The system instruction content.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a system message already exists.</exception>
    /// <remarks>
    /// The system message will be placed at the beginning of the message list.
    /// Only one system message is allowed per request.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.WithSystemPrompt("You are a creative writing assistant.");
    /// </code>
    /// </example>
    public ChatRequestBuilder WithSystemPrompt([NotNull] string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        if (_messages.Exists(m => m.Role == ChatRole.System))
        {
            throw new InvalidOperationException("A system message has already been added.");
        }

        _messages.Insert(0, ChatMessage.System(content));
        return this;
    }

    /// <summary>
    /// Adds a user message to the conversation.
    /// </summary>
    /// <param name="content">The user message content.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is empty or whitespace.</exception>
    /// <example>
    /// <code>
    /// builder.AddUserMessage("What is the capital of France?");
    /// </code>
    /// </example>
    public ChatRequestBuilder AddUserMessage([NotNull] string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));
        _messages.Add(ChatMessage.User(content));
        return this;
    }

    /// <summary>
    /// Adds an assistant message to the conversation.
    /// </summary>
    /// <param name="content">The assistant's response content.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is empty or whitespace.</exception>
    /// <remarks>
    /// Use this method to include previous AI responses when constructing
    /// multi-turn conversation history.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.AddAssistantMessage("The capital of France is Paris.");
    /// </code>
    /// </example>
    public ChatRequestBuilder AddAssistantMessage([NotNull] string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));
        _messages.Add(ChatMessage.Assistant(content));
        return this;
    }

    /// <summary>
    /// Adds a tool response message to the conversation.
    /// </summary>
    /// <param name="content">The tool response content, typically JSON.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is empty or whitespace.</exception>
    /// <example>
    /// <code>
    /// builder.AddToolMessage("{\"temperature\": 72}");
    /// </code>
    /// </example>
    public ChatRequestBuilder AddToolMessage([NotNull] string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));
        _messages.Add(ChatMessage.Tool(content));
        return this;
    }

    /// <summary>
    /// Adds an arbitrary message to the conversation.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    /// <example>
    /// <code>
    /// var customMessage = new ChatMessage(ChatRole.User, "Hello!");
    /// builder.AddMessage(customMessage);
    /// </code>
    /// </example>
    public ChatRequestBuilder AddMessage([NotNull] ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));
        _messages.Add(message);
        return this;
    }

    /// <summary>
    /// Adds multiple messages to the conversation.
    /// </summary>
    /// <param name="messages">The messages to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="messages"/> is null.</exception>
    /// <example>
    /// <code>
    /// var history = new[]
    /// {
    ///     ChatMessage.User("Hello!"),
    ///     ChatMessage.Assistant("Hi there!")
    /// };
    /// builder.AddMessages(history);
    /// </code>
    /// </example>
    public ChatRequestBuilder AddMessages([NotNull] IEnumerable<ChatMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages, nameof(messages));
        _messages.AddRange(messages);
        return this;
    }

    /// <summary>
    /// Sets the options for the request.
    /// </summary>
    /// <param name="options">The chat options to use.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    /// Calling this method multiple times will replace any previously set options.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.WithOptions(ChatOptions.Creative.WithMaxTokens(500));
    /// </code>
    /// </example>
    public ChatRequestBuilder WithOptions(ChatOptions? options)
    {
        _options = options;
        return this;
    }

    /// <summary>
    /// Sets the model for the request.
    /// </summary>
    /// <param name="model">The model identifier to use.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    /// This is a convenience method that modifies the options. If options haven't been
    /// set, creates new default options with the specified model.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.WithModel("gpt-4-turbo");
    /// </code>
    /// </example>
    public ChatRequestBuilder WithModel(string model)
    {
        _options = (_options ?? ChatOptions.Default).WithModel(model);
        return this;
    }

    /// <summary>
    /// Sets the temperature for the request.
    /// </summary>
    /// <param name="temperature">The temperature value (0.0 to 2.0).</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="temperature"/> is out of valid range.
    /// </exception>
    /// <example>
    /// <code>
    /// builder.WithTemperature(0.7);
    /// </code>
    /// </example>
    public ChatRequestBuilder WithTemperature(double temperature)
    {
        _options = (_options ?? ChatOptions.Default).WithTemperature(temperature);
        return this;
    }

    /// <summary>
    /// Sets the maximum tokens for the request.
    /// </summary>
    /// <param name="maxTokens">The maximum number of tokens to generate.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxTokens"/> is less than 1.
    /// </exception>
    /// <example>
    /// <code>
    /// builder.WithMaxTokens(1000);
    /// </code>
    /// </example>
    public ChatRequestBuilder WithMaxTokens(int maxTokens)
    {
        _options = (_options ?? ChatOptions.Default).WithMaxTokens(maxTokens);
        return this;
    }

    /// <summary>
    /// Clears all messages from the builder.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    /// This method removes all messages but preserves the current options.
    /// </remarks>
    public ChatRequestBuilder ClearMessages()
    {
        _messages.Clear();
        return this;
    }

    /// <summary>
    /// Resets the builder to its initial state.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    /// This method removes all messages and clears the options.
    /// </remarks>
    public ChatRequestBuilder Reset()
    {
        _messages.Clear();
        _options = null;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="ChatRequest"/> from the current builder state.
    /// </summary>
    /// <returns>A new <see cref="ChatRequest"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no messages have been added to the builder.
    /// </exception>
    /// <example>
    /// <code>
    /// var request = ChatRequestBuilder.Create()
    ///     .WithSystemPrompt("You are helpful.")
    ///     .AddUserMessage("Hello!")
    ///     .Build();
    /// </code>
    /// </example>
    public ChatRequest Build()
    {
        if (_messages.Count == 0)
        {
            throw new InvalidOperationException("At least one message must be added before building.");
        }

        return new ChatRequest(_messages.ToImmutableArray(), _options);
    }
}
