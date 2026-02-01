// =============================================================================
// File: ITokenCounter.cs
// Project: Lexichord.Abstractions
// Description: Contract interface for tokenization operations, enabling
//              token counting, text truncation, and encoding/decoding
//              for language models.
// =============================================================================
// LOGIC: Strategy pattern interface for tokenization across different models.
//   - Abstracts away model-specific tokenizer implementations.
//   - Provides methods for counting tokens, truncating text to token limits,
//     and bidirectional encoding/decoding between text and token sequences.
//   - Enables flexible integration with different tokenizer backends
//     (Tiktoken, Byte Pair Encoding, etc.).
//   - Model property allows runtime strategy selection and factory patterns.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Contract interface for tokenization operations used in RAG and LLM pipelines.
/// Provides token counting, text truncation, and bidirectional encoding/decoding.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ITokenCounter"/> interface abstracts tokenization logic,
/// enabling pluggable implementations for different tokenizer backends
/// (e.g., Tiktoken, OpenAI tokenizers, custom BPE implementations).
/// </para>
/// <para>
/// <b>Key Responsibilities:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Tokenization: Converting text to token sequences and vice versa.</description></item>
///   <item><description>Counting: Determining the token count of a given text.</description></item>
///   <item><description>Truncation: Limiting text to a maximum token budget.</description></item>
///   <item><description>Model Awareness: Supporting multiple tokenizer models via the Model property.</description></item>
/// </list>
/// <para>
/// <b>Common Implementations:</b>
/// </para>
/// <list type="bullet">
///   <item><description><c>TiktokenTokenCounter</c> (v0.4.4c): OpenAI Tiktoken tokenizer using Microsoft.ML.Tokenizers.</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> Implementations should be stateless and thread-safe,
/// allowing concurrent calls from multiple threads.
/// </para>
/// <para>
/// <b>Usage Patterns:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Token budget management: Use <see cref="CountTokens"/> to check if content fits within LLM token limits.</description></item>
///   <item><description>Content truncation: Use <see cref="TruncateToTokenLimit"/> to ensure content stays within budget.</description></item>
///   <item><description>Token inspection: Use <see cref="Encode"/> and <see cref="Decode"/> for low-level token analysis.</description></item>
/// </list>
/// </remarks>
public interface ITokenCounter
{
    /// <summary>
    /// Gets the tokenizer model name this counter is configured for.
    /// </summary>
    /// <value>
    /// The model identifier (e.g., "gpt-4", "gpt-3.5-turbo", "cl100k_base").
    /// </value>
    /// <remarks>
    /// This property identifies the tokenization model used by the implementation.
    /// Different models use different tokenization schemes, resulting in different
    /// token counts for the same text. The model name enables strategy selection
    /// and factory patterns for runtime tokenizer selection.
    /// </remarks>
    string Model { get; }

    /// <summary>
    /// Counts the number of tokens in the provided text for the configured model.
    /// </summary>
    /// <param name="text">
    /// The text to count tokens for. May be null or empty.
    /// </param>
    /// <returns>
    /// The number of tokens in the text. Returns 0 for null or empty strings.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method efficiently counts tokens without creating the full token list,
    /// useful for checking token budgets before encoding large texts.
    /// </para>
    /// <para>
    /// <b>Null/Empty Handling:</b> Returns 0 for null or empty input strings.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var counter = tokenCounterFactory.Create("gpt-4");
    /// int tokenCount = counter.CountTokens("Hello, world!");
    /// if (tokenCount > 2048)
    /// {
    ///     // Text is too large for the token budget
    ///     var truncated = counter.TruncateToTokenLimit("...", 2048);
    /// }
    /// </code>
    /// </example>
    int CountTokens(string text);

    /// <summary>
    /// Truncates text to fit within a maximum token limit.
    /// </summary>
    /// <param name="text">
    /// The text to truncate. May be null or empty.
    /// </param>
    /// <param name="maxTokens">
    /// The maximum number of tokens allowed. Must be greater than 0.
    /// </param>
    /// <returns>
    /// A tuple containing the truncated text and a boolean indicating whether
    /// truncation occurred. If no truncation was needed, returns the original
    /// text with <c>WasTruncated = false</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxTokens"/> is less than or equal to 0.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is essential for ensuring text fits within LLM context windows.
    /// If the input text is already within the token limit, the original text is
    /// returned unchanged with <c>WasTruncated = false</c>.
    /// </para>
    /// <para>
    /// <b>Null/Empty Handling:</b> Returns empty string with <c>WasTruncated = false</c>
    /// for null or empty input.
    /// </para>
    /// <para>
    /// <b>Edge Cases:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>If maxTokens is 0 or negative, throws ArgumentOutOfRangeException.</description></item>
    ///   <item><description>If the first token exceeds maxTokens, returns empty string with WasTruncated = true.</description></item>
    ///   <item><description>Truncation respects token boundaries (never mid-token).</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var counter = tokenCounterFactory.Create("gpt-4");
    /// var (truncated, wasTruncated) = counter.TruncateToTokenLimit(longText, 1024);
    /// if (wasTruncated)
    /// {
    ///     _logger.LogWarning("Text truncated from {OriginalTokens} to 1024 tokens",
    ///         counter.CountTokens(longText));
    /// }
    /// </code>
    /// </example>
    (string Text, bool WasTruncated) TruncateToTokenLimit(string text, int maxTokens);

    /// <summary>
    /// Encodes text into its token sequence for the configured model.
    /// </summary>
    /// <param name="text">
    /// The text to encode. May be null or empty.
    /// </param>
    /// <returns>
    /// An immutable list of token IDs representing the input text.
    /// Returns an empty list for null or empty input.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method provides low-level access to the token sequence, useful for
    /// detailed token analysis, debugging, and token-level manipulation.
    /// </para>
    /// <para>
    /// <b>Null/Empty Handling:</b> Returns an empty immutable list for null
    /// or empty input.
    /// </para>
    /// <para>
    /// <b>Token IDs:</b> Token IDs are specific to the model and tokenizer.
    /// The same ID from different models may represent different concepts.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var counter = tokenCounterFactory.Create("gpt-4");
    /// var tokens = counter.Encode("Hello");
    /// _logger.LogDebug("Token sequence: {Tokens}", string.Join(", ", tokens));
    /// </code>
    /// </example>
    IReadOnlyList<int> Encode(string text);

    /// <summary>
    /// Decodes a token sequence back into text.
    /// </summary>
    /// <param name="tokens">
    /// The token IDs to decode. Must not be null.
    /// </param>
    /// <returns>
    /// The reconstructed text from the token sequence.
    /// Returns empty string for empty token list.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tokens"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is the inverse of <see cref="Encode"/>.
    /// Note that decoding is not always a perfect inverse of encoding due to
    /// the nature of some tokenization schemes and potential whitespace handling.
    /// </para>
    /// <para>
    /// <b>Token Validity:</b> Token IDs must be valid for the configured model.
    /// Invalid token IDs may result in undefined behavior or exceptions depending
    /// on the implementation.
    /// </para>
    /// <para>
    /// <b>Null/Empty Handling:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Throws ArgumentNullException if tokens is null.</description></item>
    ///   <item><description>Returns empty string for empty token list.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var counter = tokenCounterFactory.Create("gpt-4");
    /// var tokens = counter.Encode("Hello, world!");
    /// var reconstructed = counter.Decode(tokens);
    /// // reconstructed might be "Hello, world!" or similar
    /// </code>
    /// </example>
    string Decode(IReadOnlyList<int> tokens);
}
