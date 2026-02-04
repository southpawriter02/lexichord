// -----------------------------------------------------------------------
// <copyright file="TokenizerFactory.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Factory for creating model-specific tokenizer instances.
//   - Maps model names to appropriate tokenizer implementations.
//   - Uses lazy initialization for tokenizer instances (thread-safe).
//   - GPT-4o uses o200k_base encoding, GPT-4/3.5 uses cl100k_base.
//   - Claude and unknown models use approximate tokenization.
//   - Logs tokenizer creation for observability.
// -----------------------------------------------------------------------

using Lexichord.Modules.LLM.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.ML.Tokenizers;

namespace Lexichord.Modules.LLM.TokenCounting;

/// <summary>
/// Factory for creating model-specific tokenizer instances.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates appropriate <see cref="ITokenizer"/> implementations based on
/// the target model. It uses lazy initialization for expensive tokenizer instances
/// to optimize memory usage and startup time.
/// </para>
/// <para>
/// <b>Model-to-Tokenizer Mapping:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>GPT-4o, GPT-4o Mini</b>: o200k_base encoding via <see cref="MlTokenizerWrapper"/>.</description></item>
///   <item><description><b>GPT-4, GPT-4 Turbo</b>: cl100k_base encoding via <see cref="MlTokenizerWrapper"/>.</description></item>
///   <item><description><b>GPT-3.5 Turbo</b>: cl100k_base encoding via <see cref="MlTokenizerWrapper"/>.</description></item>
///   <item><description><b>Claude (all versions)</b>: ~4 chars/token via <see cref="ApproximateTokenizer"/>.</description></item>
///   <item><description><b>Unknown models</b>: ~4 chars/token via <see cref="ApproximateTokenizer"/>.</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe. Lazy initialization ensures
/// tokenizers are created exactly once even under concurrent access.
/// </para>
/// </remarks>
internal sealed class TokenizerFactory
{
    private readonly ILogger<TokenizerFactory> _logger;

    /// <summary>
    /// Lazy-loaded tokenizer for GPT-4o models using o200k_base encoding.
    /// </summary>
    /// <remarks>
    /// Thread-safe initialization using <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>.
    /// </remarks>
    private static readonly Lazy<Tokenizer> O200kTokenizer = new(
        () => TiktokenTokenizer.CreateForModel("gpt-4o"),
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Lazy-loaded tokenizer for GPT-4 and GPT-3.5 models using cl100k_base encoding.
    /// </summary>
    /// <remarks>
    /// Thread-safe initialization using <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>.
    /// </remarks>
    private static readonly Lazy<Tokenizer> Cl100kTokenizer = new(
        () => TiktokenTokenizer.CreateForModel("gpt-4"),
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenizerFactory"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger instance for diagnostic output. Must not be <c>null</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public TokenizerFactory(ILogger<TokenizerFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a tokenizer appropriate for the given model.
    /// </summary>
    /// <param name="model">
    /// The model name (e.g., "gpt-4o", "gpt-4", "claude-3-haiku-20240307").
    /// Must not be <c>null</c> or whitespace.
    /// </param>
    /// <returns>
    /// An <see cref="ITokenizer"/> instance configured for the specified model.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="model"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The factory uses model name prefix matching to determine the appropriate
    /// tokenizer. Model names are matched case-insensitively.
    /// </para>
    /// <para>
    /// <b>Logging:</b> Each tokenizer creation is logged at Debug level for
    /// known models, and Warning level for unknown models.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var factory = new TokenizerFactory(logger);
    ///
    /// // Creates MlTokenizerWrapper with o200k_base
    /// var gpt4oTokenizer = factory.CreateForModel("gpt-4o-mini");
    ///
    /// // Creates MlTokenizerWrapper with cl100k_base
    /// var gpt4Tokenizer = factory.CreateForModel("gpt-4-turbo");
    ///
    /// // Creates ApproximateTokenizer
    /// var claudeTokenizer = factory.CreateForModel("claude-3-haiku-20240307");
    /// </code>
    /// </example>
    public ITokenizer CreateForModel(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model, nameof(model));

        var normalizedModel = model.ToLowerInvariant();

        // LOGIC: GPT-4o family uses o200k_base encoding.
        // This includes gpt-4o and gpt-4o-mini.
        if (normalizedModel.StartsWith("gpt-4o"))
        {
            LLMLogEvents.TokenizerCreated(_logger, model, "o200k_base", isExact: true);
            return new MlTokenizerWrapper(O200kTokenizer.Value, "gpt-4o");
        }

        // LOGIC: GPT-4 family (non-4o) uses cl100k_base encoding.
        // This includes gpt-4, gpt-4-turbo, gpt-4-turbo-preview, etc.
        if (normalizedModel.StartsWith("gpt-4"))
        {
            LLMLogEvents.TokenizerCreated(_logger, model, "cl100k_base", isExact: true);
            return new MlTokenizerWrapper(Cl100kTokenizer.Value, "gpt-4");
        }

        // LOGIC: GPT-3.5 family uses cl100k_base encoding.
        // This includes gpt-3.5-turbo and variants.
        if (normalizedModel.StartsWith("gpt-3.5"))
        {
            LLMLogEvents.TokenizerCreated(_logger, model, "cl100k_base", isExact: true);
            return new MlTokenizerWrapper(Cl100kTokenizer.Value, "gpt-3.5");
        }

        // LOGIC: Claude family uses approximation (~4 chars/token).
        // No official tokenizer is available for Claude models.
        if (normalizedModel.StartsWith("claude"))
        {
            LLMLogEvents.TokenizerCreated(_logger, model, "approximation", isExact: false);
            return new ApproximateTokenizer(ApproximateTokenizer.DefaultCharsPerToken, "claude");
        }

        // LOGIC: Unknown models fall back to approximation with a warning.
        LLMLogEvents.TokenizerCreatedUnknownModel(_logger, model);
        return new ApproximateTokenizer(ApproximateTokenizer.DefaultCharsPerToken, "unknown");
    }

    /// <summary>
    /// Determines whether the specified model has an exact tokenizer implementation.
    /// </summary>
    /// <param name="model">
    /// The model name to check. May be <c>null</c> or whitespace.
    /// </param>
    /// <returns>
    /// <c>true</c> if the model has an exact (ML-based) tokenizer;
    /// <c>false</c> if it uses approximation or the model name is invalid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Currently, only GPT models (gpt-4o, gpt-4, gpt-3.5) have exact tokenizers.
    /// All other models, including Claude, use heuristic approximation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var factory = new TokenizerFactory(logger);
    ///
    /// factory.IsExactTokenizer("gpt-4o-mini");  // true
    /// factory.IsExactTokenizer("gpt-4-turbo"); // true
    /// factory.IsExactTokenizer("claude-3-opus"); // false
    /// factory.IsExactTokenizer(null);          // false
    /// </code>
    /// </example>
    public bool IsExactTokenizer(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return false;
        }

        var normalizedModel = model.ToLowerInvariant();

        // LOGIC: Only GPT models have exact tokenizers.
        return normalizedModel.StartsWith("gpt-4") ||
               normalizedModel.StartsWith("gpt-3.5");
    }
}
