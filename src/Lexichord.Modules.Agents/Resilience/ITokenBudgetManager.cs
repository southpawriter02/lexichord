// -----------------------------------------------------------------------
// <copyright file="ITokenBudgetManager.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.Agents.Resilience;

/// <summary>
/// Manages token budgets for conversations to prevent context window overflow.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ITokenBudgetManager"/> enforces token limits by checking conversation
/// size against model budgets and truncating older messages when necessary. It works
/// in concert with <see cref="IErrorRecoveryService"/> for the
/// <see cref="RecoveryStrategy.Truncate"/> recovery path.
/// </para>
/// <para>
/// <b>Budget Layout:</b>
/// </para>
/// <code>
/// ┌──────────────────────────────────────────────────┐
/// │ Model Max Tokens                                 │
/// │ ┌────────────┐ ┌───────────────┐ ┌────────────┐ │
/// │ │   System    │ │  Conversation │ │  Response   │ │
/// │ │   Buffer    │ │   Messages    │ │  Reserve    │ │
/// │ │   (500)     │ │  (variable)   │ │  (1024)     │ │
/// │ └────────────┘ └───────────────┘ └────────────┘ │
/// └──────────────────────────────────────────────────┘
/// </code>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check if messages fit within budget
/// if (!_tokenBudget.CheckBudget(messages, maxTokens: 8192))
/// {
///     // Truncate to fit
///     var truncated = _tokenBudget.TruncateToFit(messages, maxTokens: 8192);
///     // Use truncated messages for the request
/// }
/// </code>
/// </example>
/// <seealso cref="TokenBudgetManager"/>
/// <seealso cref="TokenLimitException"/>
public interface ITokenBudgetManager
{
    /// <summary>
    /// Checks whether the given messages fit within the specified token budget.
    /// </summary>
    /// <param name="messages">The conversation messages to check.</param>
    /// <param name="maxTokens">
    /// The maximum token budget (model's context window size).
    /// </param>
    /// <returns>
    /// <c>true</c> if the messages fit within the budget (after reserving space
    /// for the response); <c>false</c> if truncation is needed.
    /// </returns>
    /// <remarks>
    /// LOGIC: The available budget is calculated as <paramref name="maxTokens"/> minus
    /// the response reserve (1024 tokens). The method uses
    /// <see cref="EstimateTokens"/> to sum the token count of all messages.
    /// </remarks>
    bool CheckBudget(IEnumerable<ChatMessage> messages, int maxTokens);

    /// <summary>
    /// Truncates messages to fit within the specified token budget.
    /// </summary>
    /// <param name="messages">The conversation messages to truncate.</param>
    /// <param name="maxTokens">
    /// The maximum token budget (model's context window size).
    /// </param>
    /// <returns>
    /// A new read-only list of messages that fit within the budget. System messages
    /// are always preserved. Oldest non-system messages are removed first.
    /// </returns>
    /// <remarks>
    /// LOGIC: The truncation algorithm:
    /// <list type="number">
    ///   <item><description>Calculate available tokens: maxTokens - ResponseReserve - SystemBuffer</description></item>
    ///   <item><description>If already within budget, return messages unchanged</description></item>
    ///   <item><description>Always include all system messages first</description></item>
    ///   <item><description>Add remaining messages from most recent to oldest until budget exhausted</description></item>
    ///   <item><description>Log the number of removed messages</description></item>
    /// </list>
    /// </remarks>
    IReadOnlyList<ChatMessage> TruncateToFit(IReadOnlyList<ChatMessage> messages, int maxTokens);

    /// <summary>
    /// Estimates the total token count for the given messages.
    /// </summary>
    /// <param name="messages">The messages to estimate tokens for.</param>
    /// <returns>
    /// The estimated total token count, including per-message overhead (4 tokens per message
    /// for role, separator, etc.).
    /// </returns>
    /// <remarks>
    /// LOGIC: Each message's content is counted via <see cref="Lexichord.Abstractions.Contracts.ITokenCounter.CountTokens"/>
    /// plus 4 overhead tokens per message (consistent with OpenAI's token counting guidance).
    /// </remarks>
    int EstimateTokens(IEnumerable<ChatMessage> messages);
}
