// =============================================================================
// File: TiktokenTokenCounter.cs
// Project: Lexichord.Modules.RAG
// Description: Token counter implementation using Microsoft.ML.Tokenizers
//              for OpenAI Tiktoken-compatible tokenization (cl100k_base).
// =============================================================================
// LOGIC: Production-ready tokenizer implementation for GPT-4 and compatible models.
//   - Uses TiktokenTokenizer from Microsoft.ML.Tokenizers for cl100k_base encoding.
//   - Provides efficient token counting, text truncation, and encoding/decoding.
//   - Handles edge cases: null/empty strings, negative maxTokens, invalid tokens.
//   - Logs truncation events for observability and debugging.
//   - Stateless and thread-safe for concurrent usage.
// =============================================================================

using System.Collections.ObjectModel;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.ML.Tokenizers;

namespace Lexichord.Modules.RAG.Embedding;

/// <summary>
/// Token counter implementation using OpenAI's Tiktoken tokenizer.
/// Supports cl100k_base encoding used by GPT-4 and GPT-3.5-turbo models.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="TiktokenTokenCounter"/> is a production-ready implementation
/// of <see cref="ITokenCounter"/> that uses Microsoft.ML.Tokenizers' Tiktoken
/// support to provide accurate token counting and text truncation for OpenAI models.
/// </para>
/// <para>
/// <b>Model Support:</b>
/// </para>
/// <list type="bullet">
///   <item><description>cl100k_base (default): Used by GPT-4, GPT-3.5-turbo, and later models.</description></item>
/// </list>
/// <para>
/// <b>Features:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Efficient token counting without creating full token lists.</description></item>
///   <item><description>Text truncation respecting token boundaries.</description></item>
///   <item><description>Bidirectional encoding/decoding between text and token sequences.</description></item>
///   <item><description>Graceful handling of null/empty strings and edge cases.</description></item>
///   <item><description>Comprehensive logging for truncation events.</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class is stateless and fully thread-safe.
/// The underlying Tiktoken tokenizer from Microsoft.ML.Tokenizers is also thread-safe.
/// </para>
/// <para>
/// <b>Performance:</b> Token counting is cached internally by the tokenizer,
/// making repeated operations on the same text efficient.
/// </para>
/// </remarks>
public sealed class TiktokenTokenCounter : ITokenCounter
{
    private readonly string _model;
    private readonly Tokenizer _tokenizer;
    private readonly ILogger<TiktokenTokenCounter>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TiktokenTokenCounter"/> class.
    /// </summary>
    /// <param name="model">
    /// The tokenizer model name. Currently only "cl100k_base" is supported.
    /// Defaults to "cl100k_base" if not specified or null.
    /// </param>
    /// <param name="logger">
    /// Optional logger instance for diagnostic output. If null, logging is disabled.
    /// </param>
    /// <remarks>
    /// <para>
    /// The constructor validates that the specified model is supported and creates
    /// a Tiktoken tokenizer instance via <see cref="TiktokenTokenizer.CreateForModel"/>.
    /// </para>
    /// <para>
    /// <b>Model Selection:</b> Currently, "cl100k_base" is the only supported model.
    /// Other model names will result in an <see cref="ArgumentException"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when the specified model is not supported or if tokenizer creation fails.
    /// </exception>
    /// <example>
    /// <code>
    /// // Using default model (cl100k_base)
    /// var counter1 = new TiktokenTokenCounter(logger: _logger);
    ///
    /// // Using explicit model
    /// var counter2 = new TiktokenTokenCounter("cl100k_base", _logger);
    ///
    /// // Without logging
    /// var counter3 = new TiktokenTokenCounter("cl100k_base", null);
    /// </code>
    /// </example>
    /// <summary>
    /// Maps encoding names to representative model names that use them.
    /// TiktokenTokenizer.CreateForModel expects model names, not encoding names.
    /// </summary>
    private static readonly Dictionary<string, string> EncodingToModelMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cl100k_base"] = "gpt-4",
        ["p50k_base"] = "text-davinci-003",
        ["p50k_edit"] = "text-davinci-edit-001",
        ["r50k_base"] = "text-davinci-001",
        ["o200k_base"] = "gpt-4o"
    };

    public TiktokenTokenCounter(string? model = null, ILogger<TiktokenTokenCounter>? logger = null)
    {
        // LOGIC: Normalize model name to default if not provided.
        _model = string.IsNullOrWhiteSpace(model) ? "cl100k_base" : model.Trim();
        _logger = logger;

        try
        {
            // LOGIC: If the user passed an encoding name (e.g., "cl100k_base"),
            // map it to a representative model name for TiktokenTokenizer.CreateForModel.
            var modelForApi = EncodingToModelMap.TryGetValue(_model, out var mapped) ? mapped : _model;

            // LOGIC: Create Tiktoken tokenizer for the specified model.
            // TiktokenTokenizer.CreateForModel internally maps model names to encodings.
            _tokenizer = TiktokenTokenizer.CreateForModel(modelForApi);
            _logger?.LogDebug("Initialized Tiktoken token counter for model '{Model}'", _model);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create Tiktoken tokenizer for model '{Model}'", _model);
            throw new ArgumentException(
                $"Failed to create tokenizer for model '{_model}'. " +
                $"Ensure the model name is valid and supported.",
                nameof(model), ex);
        }
    }

    /// <inheritdoc/>
    public string Model => _model;

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// This method efficiently counts tokens by encoding the text and measuring
    /// the resulting token list length. It handles null/empty strings gracefully
    /// by returning 0 without encoding.
    /// </para>
    /// </remarks>
    public int CountTokens(string? text)
    {
        // LOGIC: Return 0 for null or empty input per interface contract.
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        try
        {
            var tokens = _tokenizer.EncodeToIds(text);
            return tokens.Count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error counting tokens for text of length {TextLength}", text.Length);
            throw;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// This method efficiently truncates text by encoding to tokens, taking the
    /// first maxTokens, and decoding back to text. It respects token boundaries
    /// and never splits tokens.
    /// </para>
    /// </remarks>
    public (string Text, bool WasTruncated) TruncateToTokenLimit(string? text, int maxTokens)
    {
        // LOGIC: Validate maxTokens parameter.
        if (maxTokens <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxTokens),
                maxTokens,
                "Maximum tokens must be greater than 0.");
        }

        // LOGIC: Return empty text for null/empty input with WasTruncated=false
        // per interface contract.
        if (string.IsNullOrEmpty(text))
        {
            return (string.Empty, false);
        }

        try
        {
            // LOGIC: Encode text to tokens.
            var tokens = _tokenizer.EncodeToIds(text);

            // LOGIC: If text already fits within token limit, return unchanged.
            if (tokens.Count <= maxTokens)
            {
                return (text, false);
            }

            // LOGIC: Text exceeds limit - truncate to maxTokens and decode.
            var truncatedTokens = tokens.Take(maxTokens).ToList();
            var truncatedText = _tokenizer.Decode(truncatedTokens) ?? string.Empty;

            _logger?.LogWarning(
                "Text truncated from {OriginalTokenCount} tokens to {MaxTokens} tokens " +
                "(text length {OriginalLength} → {TruncatedLength} chars)",
                tokens.Count, maxTokens, text.Length, truncatedText.Length);

            return (truncatedText, true);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "Error truncating text to {MaxTokens} tokens (text length {TextLength})",
                maxTokens, text.Length);
            throw;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// This method provides low-level access to the token sequence. The returned
    /// list is read-only to prevent external modifications.
    /// </para>
    /// </remarks>
    public IReadOnlyList<int> Encode(string? text)
    {
        // LOGIC: Return empty list for null or empty input per interface contract.
        if (string.IsNullOrEmpty(text))
        {
            return new ReadOnlyCollection<int>(Array.Empty<int>());
        }

        try
        {
            var tokens = _tokenizer.EncodeToIds(text);
            _logger?.LogDebug("Encoded {TextLength} chars to {TokenCount} tokens", text.Length, tokens.Count);
            return new ReadOnlyCollection<int>(tokens.ToArray());
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error encoding text of length {TextLength}", text.Length);
            throw;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// This method is the inverse of <see cref="Encode"/>. Note that decoding
    /// is not always a perfect inverse of encoding due to the nature of byte-pair
    /// encoding and whitespace handling in tokenization schemes.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tokens"/> is null.
    /// </exception>
    public string Decode(IReadOnlyList<int>? tokens)
    {
        // LOGIC: Validate tokens parameter.
        ArgumentNullException.ThrowIfNull(tokens);

        // LOGIC: Return empty string for empty token list per interface contract.
        if (tokens.Count == 0)
        {
            return string.Empty;
        }

        try
        {
            var text = _tokenizer.Decode(tokens.ToList()) ?? string.Empty;
            _logger?.LogDebug("Decoded {TokenCount} tokens to {TextLength} chars", tokens.Count, text.Length);
            return text;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error decoding {TokenCount} tokens", tokens.Count);
            throw;
        }
    }
}
