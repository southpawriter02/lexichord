// -----------------------------------------------------------------------
// <copyright file="MlTokenizerWrapper.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Wrapper for Microsoft.ML.Tokenizers.Tokenizer implementing ITokenizer.
//   - Provides exact token counting using ML-based tokenization.
//   - Supports cl100k_base (GPT-4, GPT-3.5) and o200k_base (GPT-4o) encodings.
//   - Thread-safe through the underlying Tokenizer implementation.
//   - Handles null/empty input gracefully.
// -----------------------------------------------------------------------

using Microsoft.ML.Tokenizers;

namespace Lexichord.Modules.LLM.TokenCounting;

/// <summary>
/// Wrapper for <see cref="Tokenizer"/> from Microsoft.ML.Tokenizers implementing <see cref="ITokenizer"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class provides exact token counting using ML-based tokenization.
/// It wraps the Microsoft.ML.Tokenizers library to provide Tiktoken-compatible
/// tokenization for OpenAI models.
/// </para>
/// <para>
/// <b>Supported Encodings:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>cl100k_base</b>: Used by GPT-4, GPT-4 Turbo, and GPT-3.5 Turbo.</description></item>
///   <item><description><b>o200k_base</b>: Used by GPT-4o and GPT-4o Mini.</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe. The underlying <see cref="Tokenizer"/>
/// from Microsoft.ML.Tokenizers is designed for concurrent access.
/// </para>
/// </remarks>
internal sealed class MlTokenizerWrapper : ITokenizer
{
    private readonly Tokenizer _tokenizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MlTokenizerWrapper"/> class.
    /// </summary>
    /// <param name="tokenizer">
    /// The Microsoft.ML.Tokenizers tokenizer instance. Must not be <c>null</c>.
    /// </param>
    /// <param name="modelFamily">
    /// The model family identifier (e.g., "gpt-4o", "gpt-4"). Must not be <c>null</c> or whitespace.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tokenizer"/> or <paramref name="modelFamily"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="modelFamily"/> is empty or whitespace.
    /// </exception>
    /// <example>
    /// <code>
    /// // Create a wrapper for GPT-4 tokenization
    /// var tiktoken = TiktokenTokenizer.CreateForModel("gpt-4");
    /// var wrapper = new MlTokenizerWrapper(tiktoken, "gpt-4");
    ///
    /// var tokenCount = wrapper.CountTokens("Hello, world!");
    /// Console.WriteLine($"Token count: {tokenCount}");
    /// </code>
    /// </example>
    public MlTokenizerWrapper(Tokenizer tokenizer, string modelFamily)
    {
        ArgumentNullException.ThrowIfNull(tokenizer, nameof(tokenizer));
        ArgumentException.ThrowIfNullOrWhiteSpace(modelFamily, nameof(modelFamily));

        _tokenizer = tokenizer;
        ModelFamily = modelFamily;
    }

    /// <inheritdoc />
    /// <value>
    /// The model family identifier passed to the constructor (e.g., "gpt-4o", "gpt-4").
    /// </value>
    public string ModelFamily { get; }

    /// <inheritdoc />
    /// <value>
    /// Always returns <c>true</c> since ML-based tokenization provides exact counts.
    /// </value>
    public bool IsExact => true;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Uses the underlying <see cref="Tokenizer.EncodeToIds"/> method to tokenize
    /// the text and returns the count of resulting token IDs.
    /// </para>
    /// <para>
    /// <b>Null/Empty Handling:</b> Returns 0 for <c>null</c> or empty input strings
    /// without invoking the tokenizer.
    /// </para>
    /// </remarks>
    public int CountTokens(string text)
    {
        // LOGIC: Return 0 for null or empty input per interface contract.
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // LOGIC: Use EncodeToIds for efficient token counting.
        // This avoids creating string representations of tokens.
        var encoded = _tokenizer.EncodeToIds(text);
        return encoded.Count;
    }
}
