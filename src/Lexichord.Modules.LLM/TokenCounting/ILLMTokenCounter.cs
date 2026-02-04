// -----------------------------------------------------------------------
// <copyright file="ILLMTokenCounter.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Public interface for LLM token counting operations.
//   - Provides token counting for text and chat messages.
//   - Supports model limit queries and cost estimation.
//   - Model-specific tokenization (exact for GPT, approximate for Claude).
//   - Separate from ITokenCounter in Abstractions (used by RAG module).
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.LLM.TokenCounting;

/// <summary>
/// Provides token counting, limit queries, and cost estimation for LLM operations.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides comprehensive token management capabilities for LLM
/// interactions. It supports multiple LLM providers with model-specific tokenization.
/// </para>
/// <para>
/// <b>Token Counting Accuracy:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>GPT models</b>: Exact counts using ML-based tokenization (Tiktoken).</description></item>
///   <item><description><b>Claude models</b>: Approximate counts (~4 characters per token).</description></item>
///   <item><description><b>Unknown models</b>: Approximate counts with default heuristics.</description></item>
/// </list>
/// <para>
/// <b>Cost Estimation:</b>
/// </para>
/// <para>
/// Cost estimation uses pricing data current as of early 2025. For accurate
/// billing information, refer to the provider's official pricing pages.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// </para>
/// <para>
/// Implementations must be thread-safe for concurrent usage across multiple
/// requests.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic token counting
/// var count = tokenCounter.CountTokens("Hello, world!", "gpt-4o");
///
/// // Message-aware counting (includes message overhead)
/// var messages = new[] { ChatMessage.System("Be helpful."), ChatMessage.User("Hi!") };
/// var messageCount = tokenCounter.CountTokens(messages, "gpt-4o");
///
/// // Cost estimation
/// var cost = tokenCounter.CalculateCost("gpt-4o", inputTokens: 1000, outputTokens: 500);
///
/// // Context window management
/// var limit = tokenCounter.GetModelLimit("gpt-4o");
/// var maxOutput = tokenCounter.GetMaxOutputTokens("gpt-4o");
/// var available = limit - messageCount - maxOutput;
/// </code>
/// </example>
public interface ILLMTokenCounter
{
    /// <summary>
    /// Counts the number of tokens in the specified text for a given model.
    /// </summary>
    /// <param name="text">
    /// The text to tokenize. May be <c>null</c> or empty.
    /// </param>
    /// <param name="model">
    /// The model identifier (e.g., "gpt-4o", "claude-3-5-sonnet-20241022").
    /// Must not be <c>null</c> or whitespace.
    /// </param>
    /// <returns>
    /// The number of tokens in the text. Returns 0 for <c>null</c> or empty text.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="model"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method counts tokens in raw text without considering message formatting
    /// overhead. For chat messages, use <see cref="CountTokens(IEnumerable{ChatMessage}, string)"/>
    /// which includes message structure overhead.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var tokenCounter = serviceProvider.GetRequiredService&lt;ILLMTokenCounter&gt;();
    ///
    /// var count = tokenCounter.CountTokens("Hello, world!", "gpt-4o");
    /// Console.WriteLine($"Token count: {count}");
    ///
    /// // Returns 0 for null/empty
    /// var emptyCount = tokenCounter.CountTokens(null, "gpt-4o"); // 0
    /// var zeroCount = tokenCounter.CountTokens("", "gpt-4o");    // 0
    /// </code>
    /// </example>
    int CountTokens(string? text, string model);

    /// <summary>
    /// Counts the total number of tokens in a sequence of chat messages for a given model.
    /// </summary>
    /// <param name="messages">
    /// The chat messages to tokenize. May be <c>null</c> or empty.
    /// </param>
    /// <param name="model">
    /// The model identifier. Must not be <c>null</c> or whitespace.
    /// </param>
    /// <returns>
    /// The total token count including message formatting overhead.
    /// Returns 0 for <c>null</c> or empty message sequences.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="model"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method includes message structure overhead (role tokens, message delimiters)
    /// in addition to the content tokens. The overhead varies by model:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>GPT models: ~4 tokens per message for role/delimiters.</description></item>
    ///   <item><description>Claude models: ~4 tokens per message (approximation).</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var messages = new[]
    /// {
    ///     ChatMessage.System("You are a helpful assistant."),
    ///     ChatMessage.User("What is 2+2?"),
    ///     ChatMessage.Assistant("2+2 equals 4."),
    /// };
    ///
    /// var count = tokenCounter.CountTokens(messages, "gpt-4o");
    /// // Includes content tokens + ~12 tokens overhead (4 per message)
    /// </code>
    /// </example>
    int CountTokens(IEnumerable<ChatMessage>? messages, string model);

    /// <summary>
    /// Estimates the number of response tokens based on prompt tokens and max tokens setting.
    /// </summary>
    /// <param name="promptTokens">
    /// The number of tokens in the prompt. Must be non-negative.
    /// </param>
    /// <param name="maxTokens">
    /// The maximum tokens requested for the response. Must be positive.
    /// </param>
    /// <returns>
    /// An estimated response token count, typically a fraction of <paramref name="maxTokens"/>
    /// for cost estimation purposes. Returns 0 if inputs are invalid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method provides a heuristic estimate useful for cost projections.
    /// Actual response length varies based on prompt complexity and model behavior.
    /// </para>
    /// <para>
    /// The estimate uses 60% of <paramref name="maxTokens"/> as a reasonable average,
    /// since most responses don't reach the maximum limit.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Estimate response for a typical request
    /// var estimated = tokenCounter.EstimateResponseTokens(promptTokens: 500, maxTokens: 4096);
    /// // Returns approximately 2458 (60% of maxTokens)
    ///
    /// // Use for cost projection
    /// var estimatedCost = tokenCounter.CalculateCost("gpt-4o", 500, estimated);
    /// </code>
    /// </example>
    int EstimateResponseTokens(int promptTokens, int maxTokens);

    /// <summary>
    /// Gets the context window size (maximum total tokens) for the specified model.
    /// </summary>
    /// <param name="model">
    /// The model identifier. May be <c>null</c> or whitespace.
    /// </param>
    /// <returns>
    /// The context window size in tokens. Returns a default value (8192)
    /// for unknown or invalid model identifiers.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The context window is the maximum number of tokens (input + output) that
    /// can be processed in a single request.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var gpt4oLimit = tokenCounter.GetModelLimit("gpt-4o");
    /// // Returns 128000
    ///
    /// var claudeLimit = tokenCounter.GetModelLimit("claude-3-5-sonnet-20241022");
    /// // Returns 200000
    ///
    /// var unknownLimit = tokenCounter.GetModelLimit("unknown-model");
    /// // Returns 8192 (default)
    /// </code>
    /// </example>
    int GetModelLimit(string model);

    /// <summary>
    /// Gets the maximum output tokens for the specified model.
    /// </summary>
    /// <param name="model">
    /// The model identifier. May be <c>null</c> or whitespace.
    /// </param>
    /// <returns>
    /// The maximum output tokens. Returns a default value (4096)
    /// for unknown or invalid model identifiers.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the maximum number of tokens the model can generate in a single response.
    /// The actual response may be shorter based on the prompt and model behavior.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var gpt4oMax = tokenCounter.GetMaxOutputTokens("gpt-4o");
    /// // Returns 16384
    ///
    /// var claude35Max = tokenCounter.GetMaxOutputTokens("claude-3-5-sonnet-20241022");
    /// // Returns 8192
    /// </code>
    /// </example>
    int GetMaxOutputTokens(string model);

    /// <summary>
    /// Calculates the estimated cost for a request with the specified token counts.
    /// </summary>
    /// <param name="model">
    /// The model identifier. May be <c>null</c> or whitespace.
    /// </param>
    /// <param name="inputTokens">
    /// The number of input (prompt) tokens. Must be non-negative.
    /// </param>
    /// <param name="outputTokens">
    /// The number of output (completion) tokens. Must be non-negative.
    /// </param>
    /// <returns>
    /// The estimated cost in USD. Returns 0 if the model is unknown or
    /// token counts are invalid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Cost estimation uses pricing data current as of early 2025.
    /// For accurate billing, refer to the provider's official pricing pages.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // GPT-4o cost: $2.50/1M input, $10.00/1M output
    /// var cost = tokenCounter.CalculateCost("gpt-4o", inputTokens: 1000, outputTokens: 500);
    /// // cost = (1000 × 2.50 + 500 × 10.00) / 1,000,000 = $0.0075
    ///
    /// // Unknown model returns 0
    /// var unknownCost = tokenCounter.CalculateCost("llama-3", 1000, 500);
    /// // Returns 0
    /// </code>
    /// </example>
    decimal CalculateCost(string model, int inputTokens, int outputTokens);

    /// <summary>
    /// Determines whether the specified model has an exact tokenizer implementation.
    /// </summary>
    /// <param name="model">
    /// The model identifier. May be <c>null</c> or whitespace.
    /// </param>
    /// <returns>
    /// <c>true</c> if the model has an exact (ML-based) tokenizer;
    /// <c>false</c> if it uses approximation or the model is unknown.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Exact tokenizers provide precise counts matching the target model's
    /// actual tokenization. Approximate tokenizers use heuristics and may
    /// vary from actual usage by 10-20%.
    /// </para>
    /// <list type="bullet">
    ///   <item><description>GPT models (gpt-4o, gpt-4, gpt-3.5): Exact tokenizers.</description></item>
    ///   <item><description>Claude models: Approximate tokenizers.</description></item>
    ///   <item><description>Unknown models: Approximate tokenizers.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// tokenCounter.IsExactTokenizer("gpt-4o");              // true
    /// tokenCounter.IsExactTokenizer("gpt-4-turbo");         // true
    /// tokenCounter.IsExactTokenizer("claude-3-5-sonnet-20241022"); // false
    /// tokenCounter.IsExactTokenizer("unknown-model");       // false
    /// </code>
    /// </example>
    bool IsExactTokenizer(string model);
}
