// -----------------------------------------------------------------------
// <copyright file="LLMTokenCounter.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Main implementation of ILLMTokenCounter.
//   - Uses TokenizerCache for efficient tokenizer instance reuse.
//   - Uses TokenizerFactory to create model-specific tokenizers.
//   - Includes message overhead (~4 tokens per message for structure).
//   - Delegates to ModelTokenLimits for context windows and pricing.
//   - Provides comprehensive logging for all operations.
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.LLM.Logging;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.LLM.TokenCounting;

/// <summary>
/// Provides token counting, limit queries, and cost estimation for LLM operations.
/// </summary>
/// <remarks>
/// <para>
/// This class implements <see cref="ILLMTokenCounter"/> with model-specific tokenization
/// support. It uses caching to efficiently reuse tokenizer instances across requests.
/// </para>
/// <para>
/// <b>Token Counting Accuracy:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>GPT models</b>: Uses Tiktoken for exact counts.</description></item>
///   <item><description><b>Claude models</b>: Uses approximation (~4 chars/token).</description></item>
///   <item><description><b>Unknown models</b>: Uses approximation with default settings.</description></item>
/// </list>
/// <para>
/// <b>Message Overhead:</b>
/// </para>
/// <para>
/// When counting tokens for chat messages, this implementation adds overhead
/// to account for message structure (role tokens, delimiters). The default
/// overhead is 4 tokens per message.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe for concurrent usage.
/// </para>
/// </remarks>
internal sealed class LLMTokenCounter : ILLMTokenCounter
{
    /// <summary>
    /// The default number of overhead tokens per message.
    /// </summary>
    /// <remarks>
    /// This accounts for role tokens, message delimiters, and special tokens
    /// used by chat models to structure conversations.
    /// </remarks>
    public const int DefaultMessageOverheadTokens = 4;

    /// <summary>
    /// The default factor for estimating response tokens (60% of max).
    /// </summary>
    /// <remarks>
    /// Based on empirical observation that most responses don't reach
    /// the maximum token limit.
    /// </remarks>
    public const double DefaultResponseEstimateFactor = 0.6;

    private readonly TokenizerCache _cache;
    private readonly TokenizerFactory _factory;
    private readonly ILogger<LLMTokenCounter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMTokenCounter"/> class.
    /// </summary>
    /// <param name="cache">
    /// The tokenizer cache for efficient tokenizer reuse. Must not be <c>null</c>.
    /// </param>
    /// <param name="factory">
    /// The tokenizer factory for creating model-specific tokenizers. Must not be <c>null</c>.
    /// </param>
    /// <param name="logger">
    /// The logger instance for diagnostic output. Must not be <c>null</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cache"/>, <paramref name="factory"/>, or
    /// <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public LLMTokenCounter(
        TokenizerCache cache,
        TokenizerFactory factory,
        ILogger<LLMTokenCounter> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Uses model-specific tokenization: exact for GPT models, approximate
    /// for Claude and unknown models.
    /// </para>
    /// </remarks>
    public int CountTokens(string? text, string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model, nameof(model));

        // LOGIC: Return 0 for null or empty text.
        if (string.IsNullOrEmpty(text))
        {
            LLMLogEvents.TokenCounterTextCounted(_logger, model, 0, 0);
            return 0;
        }

        var tokenizer = GetTokenizer(model);
        var count = tokenizer.CountTokens(text);

        LLMLogEvents.TokenCounterTextCounted(_logger, model, text.Length, count);
        return count;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Includes message overhead (default 4 tokens per message) in addition
    /// to content tokens. Overhead accounts for role tokens and delimiters.
    /// </para>
    /// </remarks>
    public int CountTokens(IEnumerable<ChatMessage>? messages, string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model, nameof(model));

        // LOGIC: Return 0 for null or empty message sequences.
        if (messages is null)
        {
            LLMLogEvents.TokenCounterMessagesCounted(_logger, model, 0, 0);
            return 0;
        }

        var tokenizer = GetTokenizer(model);
        var totalTokens = 0;
        var messageCount = 0;

        foreach (var message in messages)
        {
            // LOGIC: Count content tokens.
            var contentTokens = tokenizer.CountTokens(message.Content);

            // LOGIC: Add message overhead for role and structure.
            totalTokens += contentTokens + DefaultMessageOverheadTokens;
            messageCount++;

            LLMLogEvents.TokenCounterMessageDetail(
                _logger, messageCount, message.Role.ToString(), contentTokens, DefaultMessageOverheadTokens);
        }

        LLMLogEvents.TokenCounterMessagesCounted(_logger, model, messageCount, totalTokens);
        return totalTokens;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Uses a factor of 60% of <paramref name="maxTokens"/> as a reasonable
    /// estimate for typical responses.
    /// </para>
    /// </remarks>
    public int EstimateResponseTokens(int promptTokens, int maxTokens)
    {
        // LOGIC: Return 0 for invalid inputs.
        if (promptTokens < 0 || maxTokens <= 0)
        {
            return 0;
        }

        // LOGIC: Estimate response as 60% of max tokens.
        var estimated = (int)(maxTokens * DefaultResponseEstimateFactor);

        LLMLogEvents.TokenCounterResponseEstimated(_logger, promptTokens, maxTokens, estimated);
        return estimated;
    }

    /// <inheritdoc />
    public int GetModelLimit(string model)
    {
        var limit = ModelTokenLimits.GetContextWindow(model);
        LLMLogEvents.TokenCounterModelLimitQueried(_logger, model ?? "null", limit);
        return limit;
    }

    /// <inheritdoc />
    public int GetMaxOutputTokens(string model)
    {
        var maxOutput = ModelTokenLimits.GetMaxOutputTokens(model);
        LLMLogEvents.TokenCounterMaxOutputQueried(_logger, model ?? "null", maxOutput);
        return maxOutput;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Returns 0 for unknown models where pricing data is not available.
    /// </para>
    /// </remarks>
    public decimal CalculateCost(string model, int inputTokens, int outputTokens)
    {
        var cost = ModelTokenLimits.CalculateCost(model, inputTokens, outputTokens);

        if (cost == 0 && inputTokens > 0 && outputTokens >= 0 && !string.IsNullOrWhiteSpace(model))
        {
            // LOGIC: Log warning if model pricing is not found.
            LLMLogEvents.TokenCounterPricingNotFound(_logger, model);
        }
        else if (cost > 0)
        {
            LLMLogEvents.TokenCounterCostCalculated(_logger, model ?? "null", inputTokens, outputTokens, cost);
        }

        return cost;
    }

    /// <inheritdoc />
    public bool IsExactTokenizer(string model)
    {
        return _factory.IsExactTokenizer(model);
    }

    /// <summary>
    /// Gets or creates a tokenizer for the specified model.
    /// </summary>
    /// <param name="model">The model identifier.</param>
    /// <returns>The tokenizer for the model.</returns>
    private ITokenizer GetTokenizer(string model)
    {
        return _cache.GetOrCreate(model, () => _factory.CreateForModel(model));
    }
}
