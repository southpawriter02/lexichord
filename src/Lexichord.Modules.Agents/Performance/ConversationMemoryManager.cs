// -----------------------------------------------------------------------
// <copyright file="ConversationMemoryManager.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.Agents.Performance;

/// <summary>
/// Manages conversation history to prevent unbounded memory growth.
/// Preserves system messages while trimming oldest user/assistant turns.
/// </summary>
/// <remarks>
/// <para>
/// This manager tracks conversation state (message count and estimated bytes)
/// and provides two enforcement mechanisms:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <see cref="TrimToLimit"/>: Removes oldest non-system messages to stay within
///     a message count limit.
///   </description></item>
///   <item><description>
///     <see cref="EnforceMemoryLimit"/>: Logs a warning when estimated memory usage
///     exceeds the configured byte limit.
///   </description></item>
/// </list>
/// <para>
/// Memory estimation uses a simple heuristic: 2 bytes per character (UTF-16)
/// plus ~100 bytes of per-message object overhead. This provides a reasonable
/// approximation without the cost of exact measurement.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8c as part of Performance Optimization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var manager = serviceProvider.GetRequiredService&lt;IConversationMemoryManager&gt;();
///
/// // Trim to configured limit
/// var messages = conversation.Messages.ToList();
/// manager.TrimToLimit(messages, 50);
///
/// // Check memory
/// if (manager.EstimatedMemoryBytes > 5 * 1024 * 1024)
/// {
///     manager.EnforceMemoryLimit(5 * 1024 * 1024);
/// }
/// </code>
/// </example>
/// <seealso cref="IConversationMemoryManager"/>
/// <seealso cref="PerformanceOptions"/>
public sealed class ConversationMemoryManager : IConversationMemoryManager
{
    // LOGIC: Logger for tracking trimming operations and memory warnings.
    private readonly ILogger<ConversationMemoryManager> _logger;

    // LOGIC: Token counter for future token-based trimming (currently used for
    // byte estimation only).
    private readonly ITokenCounter _tokenCounter;

    // LOGIC: Resolved performance options controlling max messages and memory limits.
    private readonly PerformanceOptions _options;

    // LOGIC: Tracks the current message count after the last trim operation.
    private int _currentMessageCount;

    // LOGIC: Tracks the estimated memory usage in bytes after the last trim.
    private long _estimatedBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationMemoryManager"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="tokenCounter">Token counter for memory estimation.</param>
    /// <param name="options">Performance configuration options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/>, <paramref name="tokenCounter"/>,
    /// or <paramref name="options"/> is null.
    /// </exception>
    public ConversationMemoryManager(
        ILogger<ConversationMemoryManager> logger,
        ITokenCounter tokenCounter,
        IOptions<PerformanceOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;

        _logger.LogDebug(
            "ConversationMemoryManager initialized with MaxMessages={MaxMessages}, MaxMemoryBytes={MaxMemoryBytes}",
            _options.MaxConversationMessages,
            _options.MaxConversationMemoryBytes);
    }

    /// <inheritdoc />
    public int CurrentMessageCount => _currentMessageCount;

    /// <inheritdoc />
    public long EstimatedMemoryBytes => _estimatedBytes;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The trimming algorithm works as follows:
    /// </para>
    /// <list type="number">
    ///   <item><description>If message count is at or below the limit, no trimming occurs.</description></item>
    ///   <item><description>System messages are separated from other messages.</description></item>
    ///   <item><description>The allowed count for non-system messages is calculated.</description></item>
    ///   <item><description>Oldest non-system messages are removed until the limit is met.</description></item>
    ///   <item><description>State is updated with new count and estimated bytes.</description></item>
    /// </list>
    /// </remarks>
    public void TrimToLimit(IList<ChatMessage> messages, int maxMessages)
    {
        ArgumentNullException.ThrowIfNull(messages, nameof(messages));

        // LOGIC: Short-circuit if already within limits.
        if (messages.Count <= maxMessages)
        {
            _currentMessageCount = messages.Count;
            _estimatedBytes = EstimateBytes(messages);

            _logger.LogDebug(
                "Current memory: {Bytes} bytes, {Count} messages",
                _estimatedBytes,
                _currentMessageCount);

            return;
        }

        // LOGIC: Separate system messages from non-system messages.
        // System messages are always preserved to maintain conversation context.
        var systemMessages = messages.Where(m => m.Role == ChatRole.System).ToList();
        var otherMessages = messages.Where(m => m.Role != ChatRole.System).ToList();

        // LOGIC: Calculate how many non-system messages are allowed.
        var allowedOther = maxMessages - systemMessages.Count;
        var toRemove = otherMessages.Count - allowedOther;

        if (toRemove > 0)
        {
            _logger.LogInformation(
                "Trimmed {Removed} messages to limit {Max}",
                toRemove,
                maxMessages);

            // LOGIC: Remove oldest non-system messages first (they appear earliest
            // in the otherMessages list since the original list is chronological).
            for (int i = 0; i < toRemove; i++)
            {
                messages.Remove(otherMessages[i]);
            }
        }

        // LOGIC: Update tracked state after trimming.
        _currentMessageCount = messages.Count;
        _estimatedBytes = EstimateBytes(messages);

        _logger.LogDebug(
            "Current memory: {Bytes} bytes, {Count} messages",
            _estimatedBytes,
            _currentMessageCount);
    }

    /// <inheritdoc />
    public void EnforceMemoryLimit(long maxBytes)
    {
        // LOGIC: Only log a warning if memory exceeds the limit.
        // Actual trimming must be performed by the caller via TrimToLimit.
        if (_estimatedBytes <= maxBytes)
        {
            return;
        }

        _logger.LogWarning(
            "Memory limit exceeded: {Current}MB > {Max}MB. Aggressive trim required.",
            _estimatedBytes / (1024 * 1024),
            maxBytes / (1024 * 1024));
    }

    /// <summary>
    /// Estimates the memory usage of a collection of messages.
    /// </summary>
    /// <param name="messages">The messages to estimate.</param>
    /// <returns>The estimated memory usage in bytes.</returns>
    /// <remarks>
    /// Uses a simple heuristic: 2 bytes per character (UTF-16 encoding) plus
    /// approximately 100 bytes of per-message object overhead (reference types,
    /// string headers, record fields, etc.).
    /// </remarks>
    private static long EstimateBytes(IEnumerable<ChatMessage> messages)
    {
        // LOGIC: Approximate memory: 2 bytes per character (UTF-16) + object overhead.
        return messages.Sum(m => (long)(m.Content?.Length ?? 0) * 2 + 100);
    }
}
