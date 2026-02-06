// -----------------------------------------------------------------------
// <copyright file="ConversationSearchResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.Agents.Chat.Models;

/// <summary>
/// Represents a search result from conversation history.
/// </summary>
/// <param name="Conversation">The matching conversation.</param>
/// <param name="MatchingMessages">Messages that matched the query.</param>
/// <param name="HighlightedSnippets">Text snippets with highlighted matches.</param>
public record ConversationSearchResult(
    Conversation Conversation,
    IReadOnlyList<ChatMessage> MatchingMessages,
    IReadOnlyList<string> HighlightedSnippets)
{
    /// <summary>
    /// Gets the number of matches in this conversation.
    /// </summary>
    public int MatchCount => MatchingMessages.Count;

    /// <summary>
    /// Gets the first matching snippet for preview.
    /// </summary>
    public string? PreviewSnippet => HighlightedSnippets.FirstOrDefault();
}
