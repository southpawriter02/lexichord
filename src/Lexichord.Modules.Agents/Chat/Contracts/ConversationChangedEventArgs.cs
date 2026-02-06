// -----------------------------------------------------------------------
// <copyright file="ConversationChangedEventArgs.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Chat.Models;

namespace Lexichord.Modules.Agents.Chat.Contracts;

/// <summary>
/// Event arguments for conversation change notifications.
/// </summary>
/// <param name="ChangeType">The type of change that occurred.</param>
/// <param name="Conversation">The conversation after the change.</param>
/// <param name="AffectedMessageCount">Number of messages affected (for truncation).</param>
public record ConversationChangedEventArgs(
    ConversationChangeType ChangeType,
    Conversation Conversation,
    int AffectedMessageCount = 0)
{
    /// <summary>
    /// Gets whether this change added new content.
    /// </summary>
    public bool IsAddition =>
        ChangeType is ConversationChangeType.MessageAdded or ConversationChangeType.Created;

    /// <summary>
    /// Gets whether this change removed content.
    /// </summary>
    public bool IsRemoval =>
        ChangeType is ConversationChangeType.Cleared or ConversationChangeType.Truncated;
}

/// <summary>
/// Types of conversation changes that can occur.
/// </summary>
public enum ConversationChangeType
{
    /// <summary>A new conversation was created.</summary>
    Created,

    /// <summary>A message was added to the conversation.</summary>
    MessageAdded,

    /// <summary>Multiple messages were added to the conversation.</summary>
    MessagesAdded,

    /// <summary>The conversation was cleared.</summary>
    Cleared,

    /// <summary>The conversation title was changed.</summary>
    TitleChanged,

    /// <summary>Old messages were truncated due to history limits.</summary>
    Truncated,

    /// <summary>The active conversation was switched.</summary>
    Switched,

    /// <summary>A conversation was deleted from history.</summary>
    Deleted,
}
