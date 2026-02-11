// -----------------------------------------------------------------------
// <copyright file="TokenLimitException.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Resilience;

/// <summary>
/// Exception thrown when the conversation exceeds the model's token budget.
/// </summary>
/// <remarks>
/// <para>
/// This exception is raised by <see cref="ITokenBudgetManager"/> when the total
/// tokens in a conversation (including system prompt, history, and user message)
/// exceed the model's context window.
/// </para>
/// <para>
/// The <see cref="ITokenBudgetManager.TruncateToFit"/> method is called to
/// remove the oldest non-system messages until the conversation fits within budget.
/// </para>
/// <para>
/// <b>Recovery Strategy:</b> <see cref="RecoveryStrategy.Truncate"/> — the
/// conversation is automatically truncated and retried.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <seealso cref="ITokenBudgetManager"/>
/// <seealso cref="AgentException"/>
public sealed class TokenLimitException : AgentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenLimitException"/> class.
    /// </summary>
    /// <param name="requested">
    /// The total number of tokens in the original conversation.
    /// </param>
    /// <param name="max">
    /// The maximum number of tokens allowed by the model.
    /// </param>
    /// <param name="truncatedTo">
    /// The number of tokens after truncation (post-recovery).
    /// </param>
    /// <remarks>
    /// LOGIC: The user message includes formatted token counts with thousands separators
    /// to make the numbers readable at a glance (e.g., "12,000 tokens").
    /// </remarks>
    public TokenLimitException(int requested, int max, int truncatedTo)
        : base($"Message too long ({requested:N0} tokens). Truncated to {truncatedTo:N0} tokens.")
    {
        RequestedTokens = requested;
        MaxTokens = max;
        TruncatedTo = truncatedTo;
    }

    /// <summary>
    /// Gets the total token count of the original (untrimmed) conversation.
    /// </summary>
    /// <value>A positive integer representing the original token count.</value>
    public int RequestedTokens { get; }

    /// <summary>
    /// Gets the maximum token count allowed by the model's context window.
    /// </summary>
    /// <value>The model's maximum context length in tokens.</value>
    public int MaxTokens { get; }

    /// <summary>
    /// Gets the token count after truncation.
    /// </summary>
    /// <value>The post-truncation token count, guaranteed to be ≤ <see cref="MaxTokens"/>.</value>
    public int TruncatedTo { get; }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Token limit errors are recovered by truncating the conversation.
    /// The <see cref="ITokenBudgetManager"/> handles the actual truncation logic.
    /// </remarks>
    public override RecoveryStrategy Strategy => RecoveryStrategy.Truncate;
}
