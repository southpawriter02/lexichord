// -----------------------------------------------------------------------
// <copyright file="Conversation.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.Agents.Chat.Models;

/// <summary>
/// Represents a conversation with its messages and metadata.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Conversation"/> is an immutable record that represents a complete
/// conversation state at a point in time. When messages are added or metadata
/// changes, a new <see cref="Conversation"/> instance is created using the
/// <c>with</c> expression.
/// </para>
/// <para>
/// The immutability ensures thread-safety and simplifies state management
/// in the reactive UI layer.
/// </para>
/// </remarks>
/// <param name="ConversationId">Unique identifier for this conversation.</param>
/// <param name="Title">Display title, auto-generated or user-set.</param>
/// <param name="CreatedAt">When the conversation was created.</param>
/// <param name="LastMessageAt">When the last message was added.</param>
/// <param name="Messages">The ordered list of messages in this conversation.</param>
/// <param name="Metadata">Additional context and tracking data.</param>
public record Conversation(
    Guid ConversationId,
    string Title,
    DateTime CreatedAt,
    DateTime LastMessageAt,
    IReadOnlyList<ChatMessage> Messages,
    ConversationMetadata Metadata)
{
    /// <summary>
    /// Creates an empty conversation with default values.
    /// </summary>
    /// <returns>A new empty conversation.</returns>
    public static Conversation Empty() => new(
        Guid.NewGuid(),
        "New Conversation",
        DateTime.Now,
        DateTime.Now,
        Array.Empty<ChatMessage>(),
        ConversationMetadata.Default);

    /// <summary>
    /// Creates an empty conversation with specific metadata.
    /// </summary>
    /// <param name="metadata">The metadata to attach.</param>
    /// <returns>A new empty conversation with metadata.</returns>
    public static Conversation WithMetadata(ConversationMetadata metadata) => new(
        Guid.NewGuid(),
        "New Conversation",
        DateTime.Now,
        DateTime.Now,
        Array.Empty<ChatMessage>(),
        metadata);

    /// <summary>
    /// Gets the message count.
    /// </summary>
    public int MessageCount => Messages.Count;

    /// <summary>
    /// Checks if conversation has any messages.
    /// </summary>
    public bool HasMessages => Messages.Count > 0;

    /// <summary>
    /// Gets the duration since the conversation was created.
    /// </summary>
    public TimeSpan Age => DateTime.Now - CreatedAt;

    /// <summary>
    /// Gets the duration since the last message was added.
    /// </summary>
    public TimeSpan TimeSinceLastMessage => DateTime.Now - LastMessageAt;

    /// <summary>
    /// Gets the count of user messages.
    /// </summary>
    public int UserMessageCount => Messages.Count(m => m.Role == ChatRole.User);

    /// <summary>
    /// Gets the count of assistant messages.
    /// </summary>
    public int AssistantMessageCount => Messages.Count(m => m.Role == ChatRole.Assistant);

    /// <summary>
    /// Gets the total character count across all messages.
    /// </summary>
    public int TotalCharacterCount => Messages.Sum(m => m.Content.Length);

    /// <summary>
    /// Gets the first user message, if any.
    /// </summary>
    public ChatMessage? FirstUserMessage =>
        Messages.FirstOrDefault(m => m.Role == ChatRole.User);

    /// <summary>
    /// Gets the last message, if any.
    /// </summary>
    public ChatMessage? LastMessage => Messages.LastOrDefault();

    /// <summary>
    /// Checks if the conversation matches a search query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <returns>True if any message contains the query.</returns>
    public bool MatchesSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        return Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               Messages.Any(m => m.Content.Contains(query, StringComparison.OrdinalIgnoreCase));
    }
}
