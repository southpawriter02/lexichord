// -----------------------------------------------------------------------
// <copyright file="TokenBudgetManager.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Resilience;

/// <summary>
/// Manages token budgets for conversations to prevent context window overflow.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="TokenBudgetManager"/> uses <see cref="ITokenCounter"/> to estimate
/// message token counts and enforces budgets by truncating the oldest non-system
/// messages when the conversation exceeds the model's context window.
/// </para>
/// <para>
/// <b>Budget Allocation:</b>
/// </para>
/// <code>
/// Available = MaxTokens - ResponseReserve(1024) - SystemBuffer(500)
/// </code>
/// <para>
/// <b>Truncation Strategy:</b>
/// </para>
/// <list type="number">
///   <item><description>System messages are always preserved (they set agent behavior)</description></item>
///   <item><description>The most recent user/assistant messages are kept first</description></item>
///   <item><description>Oldest non-system messages are dropped until budget is met</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class is stateless and thread-safe. Multiple calls
/// can be made concurrently without synchronization.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <seealso cref="ITokenBudgetManager"/>
/// <seealso cref="TokenLimitException"/>
public sealed class TokenBudgetManager : ITokenBudgetManager
{
    private readonly ITokenCounter _tokenCounter;
    private readonly ILogger<TokenBudgetManager> _logger;

    /// <summary>
    /// Tokens reserved for the model's response output.
    /// </summary>
    /// <remarks>
    /// LOGIC: 1024 tokens provides approximately 750 words of response space,
    /// which is sufficient for most writing assistant interactions. This reserve
    /// prevents the prompt from consuming the entire context window.
    /// </remarks>
    private const int ResponseReserve = 1024;

    /// <summary>
    /// Tokens reserved as a buffer for system message overhead.
    /// </summary>
    /// <remarks>
    /// LOGIC: System messages include role markers, formatting, and potential
    /// injection content. A 500-token buffer accounts for this overhead without
    /// being overly conservative.
    /// </remarks>
    private const int SystemBuffer = 500;

    /// <summary>
    /// Per-message overhead for role markers, separators, and metadata.
    /// </summary>
    /// <remarks>
    /// LOGIC: Consistent with OpenAI's token counting guidance (4 tokens per message
    /// for &lt;|start|&gt;role&lt;|end|&gt; markers and separator tokens).
    /// </remarks>
    private const int MessageOverhead = 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenBudgetManager"/> class.
    /// </summary>
    /// <param name="tokenCounter">
    /// The token counter used to estimate message token counts.
    /// </param>
    /// <param name="logger">Logger for budget check and truncation diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tokenCounter"/> or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public TokenBudgetManager(
        ITokenCounter tokenCounter,
        ILogger<TokenBudgetManager> logger)
    {
        _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug(
            "TokenBudgetManager initialized with model '{Model}', ResponseReserve={Reserve}, SystemBuffer={Buffer}",
            _tokenCounter.Model,
            ResponseReserve,
            SystemBuffer);
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Computes total message tokens via <see cref="EstimateTokens"/> and
    /// checks against maxTokens minus the response reserve. The response reserve
    /// is subtracted to leave room for the model's output.
    /// </remarks>
    public bool CheckBudget(IEnumerable<ChatMessage> messages, int maxTokens)
    {
        ArgumentNullException.ThrowIfNull(messages, nameof(messages));

        var total = EstimateTokens(messages);
        var available = maxTokens - ResponseReserve;

        _logger.LogDebug(
            "Token budget check: {Total} / {Available} ({Max} - {Reserve} reserve)",
            total,
            available,
            maxTokens,
            ResponseReserve);

        return total <= available;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Each message is counted as its content tokens plus <see cref="MessageOverhead"/>
    /// (4 tokens for role markers and separators). This matches the OpenAI token
    /// counting guidance for chat completions.
    /// </remarks>
    public int EstimateTokens(IEnumerable<ChatMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages, nameof(messages));

        return messages.Sum(m => _tokenCounter.CountTokens(m.Content ?? "") + MessageOverhead);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: The truncation algorithm preserves the conversation's most relevant
    /// context by keeping system messages (which set behavior) and the most recent
    /// user/assistant exchanges (which provide immediate context).
    /// </para>
    /// <para>
    /// <b>Algorithm:</b>
    /// </para>
    /// <list type="number">
    ///   <item><description>Calculate available token budget (maxTokens - ResponseReserve - SystemBuffer)</description></item>
    ///   <item><description>If messages already fit, return them unchanged</description></item>
    ///   <item><description>Extract and add all system messages first (they define agent behavior)</description></item>
    ///   <item><description>Iterate non-system messages from newest to oldest</description></item>
    ///   <item><description>Add each message if it fits within remaining budget</description></item>
    ///   <item><description>Stop when budget exhausted; log count of removed messages</description></item>
    /// </list>
    /// </remarks>
    public IReadOnlyList<ChatMessage> TruncateToFit(
        IReadOnlyList<ChatMessage> messages,
        int maxTokens)
    {
        ArgumentNullException.ThrowIfNull(messages, nameof(messages));

        var available = maxTokens - ResponseReserve - SystemBuffer;

        // LOGIC: Check if truncation is needed at all. This avoids unnecessary
        // list construction and token counting for conversations within budget.
        if (CheckBudget(messages, maxTokens))
        {
            _logger.LogDebug(
                "Messages fit within budget. No truncation needed ({Count} messages)",
                messages.Count);

            return messages;
        }

        var result = new List<ChatMessage>();
        var currentTokens = 0;

        // LOGIC: Always keep system messages — they define the agent's persona
        // and behavior, and removing them would fundamentally change the agent's output.
        var systemMessages = messages.Where(m => m.Role == ChatRole.System).ToList();
        foreach (var sys in systemMessages)
        {
            result.Add(sys);
            currentTokens += _tokenCounter.CountTokens(sys.Content ?? "") + MessageOverhead;
        }

        // LOGIC: Add non-system messages from most recent to oldest. This preserves
        // the immediate conversation context while dropping the oldest history.
        var otherMessages = messages.Where(m => m.Role != ChatRole.System).Reverse().ToList();

        foreach (var msg in otherMessages)
        {
            var msgTokens = _tokenCounter.CountTokens(msg.Content ?? "") + MessageOverhead;

            if (currentTokens + msgTokens <= available)
            {
                // LOGIC: Insert after system messages to maintain chronological order
                // within the kept messages. System messages come first, then the
                // most recent non-system messages in their original order.
                result.Insert(systemMessages.Count, msg);
                currentTokens += msgTokens;
            }
            else
            {
                var removedCount = otherMessages.Count - (result.Count - systemMessages.Count);

                _logger.LogInformation(
                    "Truncating: {Removed} messages removed to fit budget. " +
                    "Kept {Kept} of {Total} messages ({CurrentTokens} / {Available} tokens)",
                    removedCount,
                    result.Count,
                    messages.Count,
                    currentTokens,
                    available);

                break;
            }
        }

        return result;
    }
}
