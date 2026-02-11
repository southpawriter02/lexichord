// -----------------------------------------------------------------------
// <copyright file="IConversationMemoryManager.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.Agents.Performance;

/// <summary>
/// Manages conversation history to prevent unbounded memory growth.
/// </summary>
/// <remarks>
/// <para>
/// The conversation memory manager enforces configurable limits on:
/// </para>
/// <list type="bullet">
///   <item><description>Maximum message count via <see cref="TrimToLimit"/></description></item>
///   <item><description>Maximum memory usage via <see cref="EnforceMemoryLimit"/></description></item>
/// </list>
/// <para>
/// System messages are always preserved during trimming operations.
/// Oldest non-system messages are removed first when limits are exceeded.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8c as part of Performance Optimization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Trim conversation to 50 messages
/// var messages = conversation.Messages.ToList();
/// memoryManager.TrimToLimit(messages, 50);
///
/// // Check current memory usage
/// Console.WriteLine($"Messages: {memoryManager.CurrentMessageCount}");
/// Console.WriteLine($"Memory: {memoryManager.EstimatedMemoryBytes} bytes");
///
/// // Enforce a 5MB memory limit
/// memoryManager.EnforceMemoryLimit(5 * 1024 * 1024);
/// </code>
/// </example>
/// <seealso cref="ConversationMemoryManager"/>
/// <seealso cref="PerformanceOptions"/>
public interface IConversationMemoryManager
{
    /// <summary>
    /// Trims messages to the specified limit, preserving system messages.
    /// </summary>
    /// <param name="messages">The mutable list of messages to trim in-place.</param>
    /// <param name="maxMessages">The maximum allowed message count.</param>
    /// <remarks>
    /// <para>
    /// When the message count exceeds <paramref name="maxMessages"/>, the oldest
    /// non-system messages are removed. System messages (those with
    /// <see cref="ChatRole.System"/> role) are always preserved regardless of age.
    /// </para>
    /// <para>
    /// After trimming, <see cref="CurrentMessageCount"/> and <see cref="EstimatedMemoryBytes"/>
    /// are updated to reflect the new conversation state.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="messages"/> is null.
    /// </exception>
    void TrimToLimit(IList<ChatMessage> messages, int maxMessages);

    /// <summary>
    /// Gets the current count of messages tracked by this manager.
    /// </summary>
    /// <value>A non-negative integer representing the number of messages in the active conversation.</value>
    int CurrentMessageCount { get; }

    /// <summary>
    /// Estimates the current memory usage of conversation history in bytes.
    /// </summary>
    /// <value>The estimated memory in bytes using UTF-16 encoding calculation plus object overhead.</value>
    /// <remarks>
    /// Estimation uses approximately 2 bytes per character (UTF-16) plus ~100 bytes
    /// of per-message object overhead.
    /// </remarks>
    long EstimatedMemoryBytes { get; }

    /// <summary>
    /// Clears oldest messages when memory threshold is exceeded.
    /// </summary>
    /// <param name="maxBytes">The maximum allowed memory in bytes.</param>
    /// <remarks>
    /// When <see cref="EstimatedMemoryBytes"/> exceeds <paramref name="maxBytes"/>,
    /// a warning is logged indicating aggressive trimming is required. The actual
    /// trimming should be performed by calling <see cref="TrimToLimit"/> with an
    /// appropriately reduced limit.
    /// </remarks>
    void EnforceMemoryLimit(long maxBytes);
}
