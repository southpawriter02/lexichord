// -----------------------------------------------------------------------
// <copyright file="IConversationManager.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Chat.Models;

namespace Lexichord.Modules.Agents.Chat.Contracts;

/// <summary>
/// Manages conversation lifecycle, history, and export.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IConversationManager"/> provides a unified API for managing
/// co-pilot chat conversations. It handles:
/// </para>
/// <list type="bullet">
///   <item>Conversation creation and lifecycle management</item>
///   <item>Message addition with automatic history truncation</item>
///   <item>Conversation export to Markdown format</item>
///   <item>Recent conversation tracking (for Teams+ license tier)</item>
///   <item>Auto-title generation from first user message</item>
/// </list>
/// <para>
/// This interface is registered as a scoped service in the DI container,
/// ensuring each user session has its own conversation state.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Inject and use in a ViewModel
/// public class CoPilotViewModel
/// {
///     private readonly IConversationManager _conversations;
///
///     public CoPilotViewModel(IConversationManager conversations)
///     {
///         _conversations = conversations;
///         _conversations.ConversationChanged += OnConversationChanged;
///     }
///
///     private async Task StartNewConversationAsync()
///     {
///         var conversation = await _conversations.CreateConversationAsync();
///         // conversation.ConversationId is a new unique identifier
///     }
/// }
/// </code>
/// </example>
public interface IConversationManager
{
    /// <summary>
    /// Gets the currently active conversation.
    /// </summary>
    /// <value>
    /// The current <see cref="Conversation"/>. Never null; returns an empty
    /// conversation if none has been started.
    /// </value>
    Conversation CurrentConversation { get; }

    /// <summary>
    /// Gets recently accessed conversations (Teams+ feature).
    /// </summary>
    /// <value>
    /// A read-only list of recent conversations, ordered by last access time
    /// (most recent first). Maximum of 10 conversations retained.
    /// </value>
    /// <remarks>
    /// This feature requires the Teams+ license tier. Users on lower tiers
    /// will see an empty list.
    /// </remarks>
    IReadOnlyList<Conversation> RecentConversations { get; }

    /// <summary>
    /// Gets whether there is an active conversation with messages.
    /// </summary>
    bool HasActiveConversation { get; }

    /// <summary>
    /// Gets the total message count across all recent conversations.
    /// </summary>
    int TotalMessageCount { get; }

    /// <summary>
    /// Creates a new conversation and sets it as current.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created conversation.</returns>
    /// <remarks>
    /// If the current conversation has messages, it is archived to
    /// <see cref="RecentConversations"/> before creating the new one.
    /// The <see cref="ConversationChanged"/> event fires with
    /// <see cref="ConversationChangeType.Created"/>.
    /// </remarks>
    Task<Conversation> CreateConversationAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new conversation with initial metadata.
    /// </summary>
    /// <param name="metadata">Initial metadata for the conversation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created conversation with metadata.</returns>
    Task<Conversation> CreateConversationAsync(
        ConversationMetadata metadata,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a message to the current conversation.
    /// Triggers history truncation if max length exceeded.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if message is null.</exception>
    /// <remarks>
    /// <para>
    /// If this is the first user message, an auto-generated title is created
    /// from the message content.
    /// </para>
    /// <para>
    /// If adding this message exceeds the configured max history length,
    /// older messages are truncated from the beginning of the conversation.
    /// </para>
    /// </remarks>
    Task AddMessageAsync(ChatMessage message, CancellationToken ct = default);

    /// <summary>
    /// Adds multiple messages to the current conversation.
    /// </summary>
    /// <param name="messages">The messages to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Useful for restoring a conversation from persistence or
    /// adding a user-assistant message pair atomically.
    /// </remarks>
    Task AddMessagesAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken ct = default);

    /// <summary>
    /// Clears the current conversation and creates a new empty one.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Equivalent to calling <see cref="CreateConversationAsync()"/>.
    /// The cleared conversation is archived to recent conversations.
    /// </remarks>
    Task ClearCurrentConversationAsync(CancellationToken ct = default);

    /// <summary>
    /// Switches to a specific conversation from the recent list.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation to switch to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The conversation that was switched to, or null if not found.</returns>
    Task<Conversation?> SwitchToConversationAsync(
        Guid conversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Exports a conversation to Markdown format.
    /// </summary>
    /// <param name="conversation">The conversation to export.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The conversation formatted as Markdown.</returns>
    /// <exception cref="ArgumentNullException">Thrown if conversation is null.</exception>
    Task<string> ExportToMarkdownAsync(
        Conversation conversation,
        CancellationToken ct = default);

    /// <summary>
    /// Exports a conversation to Markdown format with custom options.
    /// </summary>
    /// <param name="conversation">The conversation to export.</param>
    /// <param name="options">Export formatting options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The conversation formatted as Markdown.</returns>
    Task<string> ExportToMarkdownAsync(
        Conversation conversation,
        ConversationExportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Exports a conversation to Markdown and saves to a file.
    /// </summary>
    /// <param name="conversation">The conversation to export.</param>
    /// <param name="filePath">The file path to save to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExportToFileAsync(
        Conversation conversation,
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the conversation title.
    /// </summary>
    /// <param name="title">The new title.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown if title is null or whitespace.</exception>
    Task SetTitleAsync(string title, CancellationToken ct = default);

    /// <summary>
    /// Searches recent conversations for messages containing the query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Conversations containing matching messages.</returns>
    Task<IReadOnlyList<ConversationSearchResult>> SearchAsync(
        string query,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a conversation from the recent list.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the conversation was found and deleted.</returns>
    Task<bool> DeleteConversationAsync(
        Guid conversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Raised when conversation changes occur.
    /// </summary>
    /// <remarks>
    /// Subscribe to this event to update UI when:
    /// <list type="bullet">
    ///   <item>A new conversation is created</item>
    ///   <item>A message is added</item>
    ///   <item>The conversation is cleared</item>
    ///   <item>The title changes</item>
    ///   <item>History is truncated</item>
    /// </list>
    /// </remarks>
    event EventHandler<ConversationChangedEventArgs>? ConversationChanged;
}
