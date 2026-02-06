// -----------------------------------------------------------------------
// <copyright file="ConversationManager.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Chat.Contracts;
using Lexichord.Modules.Agents.Chat.Models;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Chat.Services;

/// <summary>
/// Implements conversation lifecycle management, history tracking, and export functionality.
/// </summary>
public class ConversationManager : IConversationManager
{
    private const int DefaultMaxHistory = 50;

    private readonly ILogger<ConversationManager> _logger;

    private Conversation _currentConversation;
    private readonly List<ChatMessage> _messages = [];
    private readonly List<Conversation> _recentConversations = [];

    private int MaxHistoryLength { get; } = DefaultMaxHistory;

    /// <inheritdoc/>
    public Conversation CurrentConversation => _currentConversation;

    /// <inheritdoc/>
    public IReadOnlyList<Conversation> RecentConversations => _recentConversations.AsReadOnly();

    /// <inheritdoc/>
    public bool HasActiveConversation => _currentConversation.HasMessages;

    /// <inheritdoc/>
    public int TotalMessageCount =>
        _currentConversation.MessageCount +
        _recentConversations.Sum(c => c.MessageCount);

    /// <inheritdoc/>
    public event EventHandler<ConversationChangedEventArgs>? ConversationChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationManager"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown if logger is null.</exception>
    public ConversationManager(ILogger<ConversationManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _currentConversation = Conversation.Empty();

        _logger.LogDebug(
            "ConversationManager initialized with max history: {MaxHistory}",
            MaxHistoryLength);
    }

    /// <inheritdoc/>
    public Task<Conversation> CreateConversationAsync(CancellationToken ct = default)
    {
        // Archive current if it has messages
        ArchiveCurrentConversation();

        _messages.Clear();
        _currentConversation = Conversation.Empty();

        _logger.LogInformation(
            "New conversation created: {ConversationId}",
            _currentConversation.ConversationId);

        RaiseConversationChanged(ConversationChangeType.Created);

        return Task.FromResult(_currentConversation);
    }

    /// <inheritdoc/>
    public Task<Conversation> CreateConversationAsync(
        ConversationMetadata metadata,
        CancellationToken ct = default)
    {
        // Archive current if it has messages
        ArchiveCurrentConversation();

        _messages.Clear();
        _currentConversation = Conversation.WithMetadata(metadata);

        _logger.LogInformation(
            "New conversation created with metadata: {ConversationId}, Model: {Model}, Doc: {Doc}",
            _currentConversation.ConversationId,
            metadata.SelectedModel,
            metadata.DocumentName);

        RaiseConversationChanged(ConversationChangeType.Created);

        return Task.FromResult(_currentConversation);
    }

    /// <inheritdoc/>
    public Task AddMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        _messages.Add(message);

        // Auto-title from first user message
        if (_messages.Count == 1 && message.Role == ChatRole.User)
        {
            var title = GenerateTitle(message.Content);
            _currentConversation = _currentConversation with
            {
                Title = title,
                Messages = _messages.AsReadOnly(),
                LastMessageAt = DateTime.Now,
            };
            RaiseConversationChanged(ConversationChangeType.TitleChanged);
        }

        // Truncate if exceeds max
        var truncateCount = TruncateHistoryIfNeeded();

        if (truncateCount > 0)
        {
            _logger.LogWarning(
                "Conversation truncated: {Count} messages removed",
                truncateCount);
            RaiseConversationChanged(ConversationChangeType.Truncated, truncateCount);
        }

        // Update conversation
        _currentConversation = _currentConversation with
        {
            Messages = _messages.AsReadOnly(),
            LastMessageAt = DateTime.Now,
        };

        _logger.LogDebug(
            "Message added: {Role}, total: {Count}",
            message.Role,
            _messages.Count);

        RaiseConversationChanged(ConversationChangeType.MessageAdded);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task AddMessagesAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var messageList = messages.ToList();

        if (messageList.Count == 0)
            return Task.CompletedTask;

        foreach (var message in messageList)
        {
            _messages.Add(message);
        }

        // Auto-title from first user message
        var firstUserMessage = messageList.FirstOrDefault(m => m.Role == ChatRole.User);

        if (_currentConversation.Title == "New Conversation" && firstUserMessage is not null)
        {
            var title = GenerateTitle(firstUserMessage.Content);
            _currentConversation = _currentConversation with { Title = title };
        }

        // Truncate if exceeds max
        var truncateCount = TruncateHistoryIfNeeded();

        if (truncateCount > 0)
        {
            _logger.LogWarning(
                "Conversation truncated: {Count} messages removed",
                truncateCount);
            RaiseConversationChanged(ConversationChangeType.Truncated, truncateCount);
        }

        // Update conversation
        _currentConversation = _currentConversation with
        {
            Messages = _messages.AsReadOnly(),
            LastMessageAt = DateTime.Now,
        };

        _logger.LogDebug(
            "Added {Count} messages to conversation",
            messageList.Count);

        RaiseConversationChanged(ConversationChangeType.MessagesAdded);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ClearCurrentConversationAsync(CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Clearing current conversation: {ConversationId}",
            _currentConversation.ConversationId);

        return CreateConversationAsync(ct);
    }

    /// <inheritdoc/>
    public Task<Conversation?> SwitchToConversationAsync(
        Guid conversationId,
        CancellationToken ct = default)
    {
        var targetConversation = _recentConversations
            .FirstOrDefault(c => c.ConversationId == conversationId);

        if (targetConversation is null)
        {
            _logger.LogWarning(
                "Conversation not found: {ConversationId}",
                conversationId);

            return Task.FromResult<Conversation?>(null);
        }

        // Archive current if it has messages
        ArchiveCurrentConversation();

        // Remove from recent and make current
        _recentConversations.Remove(targetConversation);
        _currentConversation = targetConversation;
        _messages.Clear();
        _messages.AddRange(targetConversation.Messages);

        _logger.LogInformation(
            "Switched to conversation: {ConversationId}",
            conversationId);

        RaiseConversationChanged(ConversationChangeType.Switched);

        return Task.FromResult<Conversation?>(targetConversation);
    }

    /// <inheritdoc/>
    public Task<string> ExportToMarkdownAsync(
        Conversation conversation,
        CancellationToken ct = default)
    {
        return ExportToMarkdownAsync(conversation, ConversationExportOptions.Default, ct);
    }

    /// <inheritdoc/>
    public Task<string> ExportToMarkdownAsync(
        Conversation conversation,
        ConversationExportOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        _logger.LogInformation(
            "Exporting conversation {ConversationId} to Markdown",
            conversation.ConversationId);

        var sb = new StringBuilder();

        // Title
        sb.AppendLine($"# {conversation.Title}");
        sb.AppendLine();

        // Metadata (if enabled)
        if (options.IncludeMetadata)
        {
            sb.AppendLine($"**Date:** {conversation.CreatedAt:yyyy-MM-dd HH:mm}");

            if (conversation.Metadata.SelectedModel is not null)
            {
                sb.AppendLine($"**Model:** {conversation.Metadata.SelectedModel}");
            }

            if (conversation.Metadata.DocumentPath is not null)
            {
                sb.AppendLine($"**Document:** {conversation.Metadata.DocumentPath}");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        // Messages
        foreach (var message in conversation.Messages)
        {
            // Filter system messages if disabled
            if (!options.IncludeSystemMessages && message.Role == ChatRole.System)
                continue;

            // Role header
            var roleHeader = FormatRoleHeader(message.Role, options.UseEmoji);
            sb.AppendLine($"## {roleHeader}");
            sb.AppendLine();

            // Timestamp (if enabled)
            if (options.IncludeTimestamps)
            {
                sb.AppendLine($"*{DateTime.Now:HH:mm:ss}*");
                sb.AppendLine();
            }

            // Content
            sb.AppendLine(message.Content);
            sb.AppendLine();
        }

        return Task.FromResult(sb.ToString());
    }

    /// <inheritdoc/>
    public async Task ExportToFileAsync(
        Conversation conversation,
        string filePath,
        CancellationToken ct = default)
    {
        var markdown = await ExportToMarkdownAsync(conversation, ct);

        try
        {
            await File.WriteAllTextAsync(filePath, markdown, ct);

            _logger.LogInformation(
                "Conversation exported to file: {FilePath}",
                filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to write export file: {FilePath}, Error: {ErrorMessage}",
                filePath,
                ex.Message);

            throw;
        }
    }

    /// <inheritdoc/>
    public Task SetTitleAsync(string title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be null or whitespace.", nameof(title));
        }

        _currentConversation = _currentConversation with { Title = title };

        _logger.LogDebug("Title set to: {Title}", title);

        RaiseConversationChanged(ConversationChangeType.TitleChanged);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ConversationSearchResult>> SearchAsync(
        string query,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<IReadOnlyList<ConversationSearchResult>>(
                Array.Empty<ConversationSearchResult>());
        }

        _logger.LogDebug("Searching conversations for: {Query}", query);

        var results = new List<ConversationSearchResult>();

        // Search current conversation
        if (_currentConversation.MatchesSearch(query))
        {
            results.Add(CreateSearchResult(_currentConversation, query));
        }

        // Search recent conversations
        foreach (var conversation in _recentConversations)
        {
            if (conversation.MatchesSearch(query))
            {
                results.Add(CreateSearchResult(conversation, query));
            }
        }

        _logger.LogInformation(
            "Found {Count} matching conversations",
            results.Count);

        return Task.FromResult<IReadOnlyList<ConversationSearchResult>>(results.AsReadOnly());
    }

    /// <inheritdoc/>
    public Task<bool> DeleteConversationAsync(
        Guid conversationId,
        CancellationToken ct = default)
    {
        var conversation = _recentConversations
            .FirstOrDefault(c => c.ConversationId == conversationId);

        if (conversation is null)
        {
            return Task.FromResult(false);
        }

        _recentConversations.Remove(conversation);

        _logger.LogInformation(
            "Conversation deleted: {ConversationId}",
            conversationId);

        RaiseConversationChanged(ConversationChangeType.Deleted);

        return Task.FromResult(true);
    }

    #region Private Helper Methods

    /// <summary>
    /// Archives the current conversation to recent list if it has messages.
    /// </summary>
    private void ArchiveCurrentConversation()
    {
        if (!_currentConversation.HasMessages)
            return;

        // LOGIC (v0.6.4c): Create a snapshot of the conversation with a copy of messages.
        // This prevents the archived conversation from being affected when _messages is cleared.
        var snapshotConversation = _currentConversation with
        {
            Messages = _messages.ToList().AsReadOnly(),
        };

        _recentConversations.Insert(0, snapshotConversation);

        // Keep maximum of 10 recent conversations
        if (_recentConversations.Count > 10)
        {
            _recentConversations.RemoveAt(_recentConversations.Count - 1);
        }
    }

    /// <summary>
    /// Truncates message history if it exceeds the configured maximum.
    /// </summary>
    /// <returns>The number of messages removed.</returns>
    private int TruncateHistoryIfNeeded()
    {
        if (_messages.Count <= MaxHistoryLength)
            return 0;

        var truncateCount = _messages.Count - MaxHistoryLength;
        _messages.RemoveRange(0, truncateCount);

        return truncateCount;
    }

    /// <summary>
    /// Generates a conversation title from message content.
    /// </summary>
    /// <param name="content">The message content.</param>
    /// <returns>A truncated title (max 50 chars).</returns>
    private string GenerateTitle(string content)
    {
        const int maxLength = 50;

        if (content.Length <= maxLength)
            return content;

        // Truncate at word boundary
        var truncated = content[..maxLength];
        var lastSpace = truncated.LastIndexOf(' ');

        if (lastSpace > 0)
            truncated = truncated[..lastSpace];

        var title = truncated + "...";

        _logger.LogDebug("Auto-generated title: {Title}", title);

        return title;
    }

    /// <summary>
    /// Formats a role header for Markdown export.
    /// </summary>
    /// <param name="role">The message role.</param>
    /// <param name="useEmoji">Whether to use emoji indicators.</param>
    /// <returns>Formatted role header string.</returns>
    private static string FormatRoleHeader(ChatRole role, bool useEmoji)
    {
        return role switch
        {
            ChatRole.System => useEmoji ? "⚙️ System" : "System",
            ChatRole.User => useEmoji ? "👤 User" : "User",
            ChatRole.Assistant => useEmoji ? "🤖 Assistant" : "Assistant",
            _ => useEmoji ? "❓ Unknown" : "Unknown",
        };
    }

    /// <summary>
    /// Creates a search result from a conversation and query.
    /// </summary>
    /// <param name="conversation">The matching conversation.</param>
    /// <param name="query">The search query.</param>
    /// <returns>A search result with matched messages and snippets.</returns>
    private static ConversationSearchResult CreateSearchResult(
        Conversation conversation,
        string query)
    {
        var matchingMessages = conversation.Messages
            .Where(m => m.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var snippets = matchingMessages
            .Select(m => CreateSnippet(m.Content, query))
            .ToList();

        return new ConversationSearchResult(
            conversation,
            matchingMessages.AsReadOnly(),
            snippets.AsReadOnly());
    }

    /// <summary>
    /// Creates a text snippet with context around the query match.
    /// </summary>
    /// <param name="content">The full message content.</param>
    /// <param name="query">The search query.</param>
    /// <returns>A snippet with surrounding context.</returns>
    private static string CreateSnippet(string content, string query)
    {
        const int contextLength = 40;

        var index = content.IndexOf(query, StringComparison.OrdinalIgnoreCase);

        if (index < 0)
            return content.Length > contextLength ? content[..contextLength] + "..." : content;

        var start = Math.Max(0, index - contextLength);
        var end = Math.Min(content.Length, index + query.Length + contextLength);

        var snippet = content[start..end];

        if (start > 0)
            snippet = "..." + snippet;

        if (end < content.Length)
            snippet += "...";

        // Highlight the match
        snippet = snippet.Replace(
            query,
            $"**{query}**",
            StringComparison.OrdinalIgnoreCase);

        return snippet;
    }

    /// <summary>
    /// Raises the ConversationChanged event.
    /// </summary>
    /// <param name="changeType">The type of change.</param>
    /// <param name="affectedMessageCount">Number of messages affected.</param>
    private void RaiseConversationChanged(
        ConversationChangeType changeType,
        int affectedMessageCount = 0)
    {
        var args = new ConversationChangedEventArgs(
            changeType,
            _currentConversation,
            affectedMessageCount);

        ConversationChanged?.Invoke(this, args);
    }

    #endregion
}
