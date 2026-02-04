// -----------------------------------------------------------------------
// <copyright file="ITokenizer.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Internal abstraction for tokenizer implementations.
//   - Provides a common interface for different tokenization strategies.
//   - Supports both exact (ML-based) and approximate (heuristic) tokenizers.
//   - The ModelFamily property enables cache key grouping.
//   - The IsExact property indicates whether counts are precise or estimated.
// -----------------------------------------------------------------------

namespace Lexichord.Modules.LLM.TokenCounting;

/// <summary>
/// Internal abstraction for tokenizer implementations.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a unified API for different tokenization strategies:
/// </para>
/// <list type="bullet">
///   <item><description><b>Exact tokenizers</b>: Use ML-based tokenization (e.g., Tiktoken for GPT models).</description></item>
///   <item><description><b>Approximate tokenizers</b>: Use heuristic estimation (e.g., character ratio for Claude).</description></item>
/// </list>
/// <para>
/// Implementations must be thread-safe and stateless for concurrent usage.
/// </para>
/// </remarks>
internal interface ITokenizer
{
    /// <summary>
    /// Gets the model family this tokenizer is configured for.
    /// </summary>
    /// <value>
    /// A string identifying the model family (e.g., "gpt-4o", "gpt-4", "claude", "unknown").
    /// Used for cache key grouping in <see cref="TokenizerCache"/>.
    /// </value>
    string ModelFamily { get; }

    /// <summary>
    /// Gets a value indicating whether this tokenizer provides exact token counts.
    /// </summary>
    /// <value>
    /// <c>true</c> if the tokenizer uses ML-based tokenization with exact counts;
    /// <c>false</c> if it uses heuristic approximation.
    /// </value>
    /// <remarks>
    /// Exact tokenizers (like Tiktoken) provide precise counts matching the target model's
    /// tokenization. Approximate tokenizers provide estimates that may vary from actual
    /// usage by up to 20% depending on text characteristics.
    /// </remarks>
    bool IsExact { get; }

    /// <summary>
    /// Counts the number of tokens in the provided text.
    /// </summary>
    /// <param name="text">
    /// The text to tokenize. May be <c>null</c> or empty.
    /// </param>
    /// <returns>
    /// The number of tokens in the text. Returns 0 for <c>null</c> or empty strings.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method must be thread-safe and support concurrent calls.
    /// </para>
    /// <para>
    /// <b>Performance:</b> Implementations should be efficient for repeated calls.
    /// Consider caching or lazy initialization of expensive resources.
    /// </para>
    /// </remarks>
    int CountTokens(string text);
}
