// -----------------------------------------------------------------------
// <copyright file="ModelTokenLimits.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Static pricing and token limit data for LLM models.
//   - Provides context window sizes and max output tokens for known models.
//   - Includes pricing per 1M tokens (input and output) for cost estimation.
//   - Uses prefix matching for model family lookups (e.g., "gpt-4o-" variants).
//   - Returns sensible defaults for unknown models.
//   - Pricing data is current as of early 2025 and may require updates.
// -----------------------------------------------------------------------

namespace Lexichord.Modules.LLM.TokenCounting;

/// <summary>
/// Provides static token limit and pricing data for LLM models.
/// </summary>
/// <remarks>
/// <para>
/// This class provides hardcoded pricing and limit data for common LLM models.
/// It is used by <see cref="LLMTokenCounter"/> for cost estimation and context
/// window management.
/// </para>
/// <para>
/// <b>Pricing Data:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Prices are in USD per 1 million tokens.</description></item>
///   <item><description>Input and output tokens are priced separately.</description></item>
///   <item><description>Pricing data is current as of early 2025.</description></item>
///   <item><description>Unknown models return zero pricing (cost cannot be estimated).</description></item>
/// </list>
/// <para>
/// <b>Model Lookup:</b>
/// </para>
/// <para>
/// Models are looked up first by exact match, then by prefix matching to handle
/// model variants (e.g., "gpt-4o-2024-05-13" matches "gpt-4o" pricing).
/// </para>
/// </remarks>
public static class ModelTokenLimits
{
    /// <summary>
    /// Default context window size for unknown models (8,192 tokens).
    /// </summary>
    /// <remarks>
    /// This conservative default ensures safe operation with unknown models.
    /// </remarks>
    public const int DefaultContextWindow = 8192;

    /// <summary>
    /// Default maximum output tokens for unknown models (4,096 tokens).
    /// </summary>
    public const int DefaultMaxOutputTokens = 4096;

    /// <summary>
    /// Model pricing and limit information.
    /// </summary>
    /// <param name="ContextWindow">Maximum context window size in tokens.</param>
    /// <param name="MaxOutputTokens">Maximum output tokens per request.</param>
    /// <param name="InputPricePerMillion">Price in USD per 1M input tokens.</param>
    /// <param name="OutputPricePerMillion">Price in USD per 1M output tokens.</param>
    public readonly record struct ModelPricing(
        int ContextWindow,
        int MaxOutputTokens,
        decimal InputPricePerMillion,
        decimal OutputPricePerMillion);

    /// <summary>
    /// Known model pricing and limits keyed by model identifier.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Exact model IDs are checked first, followed by prefix matching.
    /// Order matters for prefix matching: more specific prefixes should come first.
    /// </para>
    /// </remarks>
    private static readonly Dictionary<string, ModelPricing> _modelPricing = new(StringComparer.OrdinalIgnoreCase)
    {
        // -----------------------------------------------------------------
        // OpenAI Models
        // -----------------------------------------------------------------

        // LOGIC: GPT-4o family (o200k_base encoding, 128K context).
        ["gpt-4o"] = new ModelPricing(
            ContextWindow: 128000,
            MaxOutputTokens: 16384,
            InputPricePerMillion: 2.50m,
            OutputPricePerMillion: 10.00m),

        ["gpt-4o-mini"] = new ModelPricing(
            ContextWindow: 128000,
            MaxOutputTokens: 16384,
            InputPricePerMillion: 0.15m,
            OutputPricePerMillion: 0.60m),

        // LOGIC: GPT-4 Turbo (cl100k_base encoding, 128K context).
        ["gpt-4-turbo"] = new ModelPricing(
            ContextWindow: 128000,
            MaxOutputTokens: 4096,
            InputPricePerMillion: 10.00m,
            OutputPricePerMillion: 30.00m),

        ["gpt-4-turbo-preview"] = new ModelPricing(
            ContextWindow: 128000,
            MaxOutputTokens: 4096,
            InputPricePerMillion: 10.00m,
            OutputPricePerMillion: 30.00m),

        // LOGIC: GPT-4 base (cl100k_base encoding, 8K context).
        ["gpt-4"] = new ModelPricing(
            ContextWindow: 8192,
            MaxOutputTokens: 4096,
            InputPricePerMillion: 30.00m,
            OutputPricePerMillion: 60.00m),

        // LOGIC: GPT-3.5 Turbo (cl100k_base encoding, 16K context).
        ["gpt-3.5-turbo"] = new ModelPricing(
            ContextWindow: 16385,
            MaxOutputTokens: 4096,
            InputPricePerMillion: 0.50m,
            OutputPricePerMillion: 1.50m),

        // -----------------------------------------------------------------
        // Anthropic Claude Models
        // -----------------------------------------------------------------

        // LOGIC: Claude 3.5 Sonnet (latest, highest capability per cost).
        ["claude-3-5-sonnet-20241022"] = new ModelPricing(
            ContextWindow: 200000,
            MaxOutputTokens: 8192,
            InputPricePerMillion: 3.00m,
            OutputPricePerMillion: 15.00m),

        ["claude-3-5-sonnet-20240620"] = new ModelPricing(
            ContextWindow: 200000,
            MaxOutputTokens: 8192,
            InputPricePerMillion: 3.00m,
            OutputPricePerMillion: 15.00m),

        // LOGIC: Claude 3 Opus (highest capability).
        ["claude-3-opus-20240229"] = new ModelPricing(
            ContextWindow: 200000,
            MaxOutputTokens: 4096,
            InputPricePerMillion: 15.00m,
            OutputPricePerMillion: 75.00m),

        // LOGIC: Claude 3 Sonnet (balanced capability/cost).
        ["claude-3-sonnet-20240229"] = new ModelPricing(
            ContextWindow: 200000,
            MaxOutputTokens: 4096,
            InputPricePerMillion: 3.00m,
            OutputPricePerMillion: 15.00m),

        // LOGIC: Claude 3 Haiku (fastest, lowest cost).
        ["claude-3-haiku-20240307"] = new ModelPricing(
            ContextWindow: 200000,
            MaxOutputTokens: 4096,
            InputPricePerMillion: 0.25m,
            OutputPricePerMillion: 1.25m),
    };

    /// <summary>
    /// Model family prefixes for prefix-based lookups.
    /// </summary>
    /// <remarks>
    /// Order matters: more specific prefixes should come before more general ones.
    /// </remarks>
    private static readonly (string Prefix, ModelPricing Pricing)[] _modelPrefixes =
    [
        // OpenAI prefixes (order: most specific first).
        ("gpt-4o-mini", _modelPricing["gpt-4o-mini"]),
        ("gpt-4o", _modelPricing["gpt-4o"]),
        ("gpt-4-turbo", _modelPricing["gpt-4-turbo"]),
        ("gpt-4", _modelPricing["gpt-4"]),
        ("gpt-3.5-turbo", _modelPricing["gpt-3.5-turbo"]),
        ("gpt-3.5", _modelPricing["gpt-3.5-turbo"]),

        // Claude prefixes (order: most specific first).
        ("claude-3-5-sonnet", _modelPricing["claude-3-5-sonnet-20241022"]),
        ("claude-3-opus", _modelPricing["claude-3-opus-20240229"]),
        ("claude-3-sonnet", _modelPricing["claude-3-sonnet-20240229"]),
        ("claude-3-haiku", _modelPricing["claude-3-haiku-20240307"]),

        // Generic Claude fallback (use Haiku pricing as conservative estimate).
        ("claude", _modelPricing["claude-3-haiku-20240307"]),
    ];

    /// <summary>
    /// Gets the context window size for the specified model.
    /// </summary>
    /// <param name="model">
    /// The model identifier (e.g., "gpt-4o", "claude-3-5-sonnet-20241022").
    /// May be <c>null</c> or whitespace.
    /// </param>
    /// <returns>
    /// The context window size in tokens. Returns <see cref="DefaultContextWindow"/>
    /// for unknown or invalid model identifiers.
    /// </returns>
    /// <example>
    /// <code>
    /// var contextWindow = ModelTokenLimits.GetContextWindow("gpt-4o");
    /// // Returns 128000
    ///
    /// var unknown = ModelTokenLimits.GetContextWindow("unknown-model");
    /// // Returns 8192 (default)
    /// </code>
    /// </example>
    public static int GetContextWindow(string model)
    {
        var pricing = GetPricing(model);
        return pricing?.ContextWindow ?? DefaultContextWindow;
    }

    /// <summary>
    /// Gets the maximum output tokens for the specified model.
    /// </summary>
    /// <param name="model">
    /// The model identifier. May be <c>null</c> or whitespace.
    /// </param>
    /// <returns>
    /// The maximum output tokens. Returns <see cref="DefaultMaxOutputTokens"/>
    /// for unknown or invalid model identifiers.
    /// </returns>
    /// <example>
    /// <code>
    /// var maxOutput = ModelTokenLimits.GetMaxOutputTokens("gpt-4o");
    /// // Returns 16384
    ///
    /// var unknown = ModelTokenLimits.GetMaxOutputTokens("unknown-model");
    /// // Returns 4096 (default)
    /// </code>
    /// </example>
    public static int GetMaxOutputTokens(string model)
    {
        var pricing = GetPricing(model);
        return pricing?.MaxOutputTokens ?? DefaultMaxOutputTokens;
    }

    /// <summary>
    /// Gets the pricing information for the specified model.
    /// </summary>
    /// <param name="model">
    /// The model identifier. May be <c>null</c> or whitespace.
    /// </param>
    /// <returns>
    /// The <see cref="ModelPricing"/> for the model, or <c>null</c> if the model
    /// is unknown or the identifier is invalid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Lookup order:
    /// </para>
    /// <list type="number">
    ///   <item><description>Exact match against known model IDs.</description></item>
    ///   <item><description>Prefix match against model family prefixes.</description></item>
    ///   <item><description>Returns <c>null</c> if no match is found.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Exact match
    /// var pricing = ModelTokenLimits.GetPricing("gpt-4o");
    /// // pricing.InputPricePerMillion == 2.50m
    ///
    /// // Prefix match (variant of gpt-4o)
    /// var variantPricing = ModelTokenLimits.GetPricing("gpt-4o-2024-05-13");
    /// // Uses gpt-4o pricing
    ///
    /// // Unknown model
    /// var unknown = ModelTokenLimits.GetPricing("llama-3");
    /// // Returns null
    /// </code>
    /// </example>
    public static ModelPricing? GetPricing(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        // LOGIC: Try exact match first.
        if (_modelPricing.TryGetValue(model, out var exactPricing))
        {
            return exactPricing;
        }

        // LOGIC: Try prefix matching for model variants.
        var lowerModel = model.ToLowerInvariant();
        foreach (var (prefix, pricing) in _modelPrefixes)
        {
            if (lowerModel.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return pricing;
            }
        }

        // LOGIC: Unknown model.
        return null;
    }

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
    /// if token counts are negative.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Cost calculation: <c>(inputTokens × inputPrice + outputTokens × outputPrice) / 1,000,000</c>
    /// </para>
    /// <para>
    /// For unknown models, returns 0 since pricing cannot be determined.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // GPT-4o: $2.50/1M input, $10.00/1M output
    /// var cost = ModelTokenLimits.CalculateCost("gpt-4o", inputTokens: 1000, outputTokens: 500);
    /// // cost = (1000 × 2.50 + 500 × 10.00) / 1,000,000 = 0.0075 USD
    ///
    /// // Unknown model
    /// var unknownCost = ModelTokenLimits.CalculateCost("llama-3", 1000, 500);
    /// // Returns 0
    /// </code>
    /// </example>
    public static decimal CalculateCost(string model, int inputTokens, int outputTokens)
    {
        // LOGIC: Return 0 for invalid inputs.
        if (inputTokens < 0 || outputTokens < 0)
        {
            return 0m;
        }

        var pricing = GetPricing(model);
        if (pricing is null)
        {
            return 0m;
        }

        // LOGIC: Calculate cost based on per-million pricing.
        var inputCost = inputTokens * pricing.Value.InputPricePerMillion / 1_000_000m;
        var outputCost = outputTokens * pricing.Value.OutputPricePerMillion / 1_000_000m;

        return inputCost + outputCost;
    }

    /// <summary>
    /// Checks whether pricing information is available for the specified model.
    /// </summary>
    /// <param name="model">
    /// The model identifier. May be <c>null</c> or whitespace.
    /// </param>
    /// <returns>
    /// <c>true</c> if pricing is available; <c>false</c> otherwise.
    /// </returns>
    /// <example>
    /// <code>
    /// ModelTokenLimits.HasPricing("gpt-4o");       // true
    /// ModelTokenLimits.HasPricing("claude-3-opus-20240229"); // true
    /// ModelTokenLimits.HasPricing("llama-3");     // false
    /// </code>
    /// </example>
    public static bool HasPricing(string model)
    {
        return GetPricing(model) is not null;
    }

    /// <summary>
    /// Gets all known model identifiers with pricing information.
    /// </summary>
    /// <returns>
    /// A read-only list of model identifiers that have pricing data.
    /// </returns>
    /// <remarks>
    /// This returns only exact model IDs, not prefix patterns.
    /// </remarks>
    public static IReadOnlyList<string> GetAllKnownModels()
    {
        return _modelPricing.Keys.ToList().AsReadOnly();
    }
}
