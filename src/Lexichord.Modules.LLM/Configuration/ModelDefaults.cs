// -----------------------------------------------------------------------
// <copyright file="ModelDefaults.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.LLM.Configuration;

/// <summary>
/// Provides static model-specific default configurations for known LLM models.
/// </summary>
/// <remarks>
/// <para>
/// This class provides hardcoded defaults for common models from major providers.
/// These defaults can be overridden by configuration in <c>appsettings.json</c>.
/// </para>
/// <para>
/// For dynamic model discovery, use <see cref="IModelProvider"/> implementations instead.
/// </para>
/// </remarks>
public static class ModelDefaults
{
    /// <summary>
    /// Default context window size for models without explicit configuration.
    /// </summary>
    public const int DefaultContextWindow = 8192;

    /// <summary>
    /// Default maximum output tokens for models without explicit configuration.
    /// </summary>
    public const int DefaultMaxOutputTokens = 4096;

    /// <summary>
    /// Model-specific default configurations keyed by model identifier.
    /// </summary>
    private static readonly Dictionary<string, ModelInfo> _modelInfo = new(StringComparer.OrdinalIgnoreCase)
    {
        // OpenAI Models
        ["gpt-4o"] = new ModelInfo(
            Id: "gpt-4o",
            DisplayName: "GPT-4o",
            ContextWindow: 128000,
            MaxOutputTokens: 16384,
            SupportsVision: true,
            SupportsTools: true),

        ["gpt-4o-mini"] = new ModelInfo(
            Id: "gpt-4o-mini",
            DisplayName: "GPT-4o Mini",
            ContextWindow: 128000,
            MaxOutputTokens: 16384,
            SupportsVision: true,
            SupportsTools: true),

        ["gpt-4-turbo"] = new ModelInfo(
            Id: "gpt-4-turbo",
            DisplayName: "GPT-4 Turbo",
            ContextWindow: 128000,
            MaxOutputTokens: 4096,
            SupportsVision: true,
            SupportsTools: true),

        ["gpt-3.5-turbo"] = new ModelInfo(
            Id: "gpt-3.5-turbo",
            DisplayName: "GPT-3.5 Turbo",
            ContextWindow: 16385,
            MaxOutputTokens: 4096,
            SupportsVision: false,
            SupportsTools: true),

        // Anthropic Models
        ["claude-3-opus-20240229"] = new ModelInfo(
            Id: "claude-3-opus-20240229",
            DisplayName: "Claude 3 Opus",
            ContextWindow: 200000,
            MaxOutputTokens: 4096,
            SupportsVision: true,
            SupportsTools: true),

        ["claude-3-sonnet-20240229"] = new ModelInfo(
            Id: "claude-3-sonnet-20240229",
            DisplayName: "Claude 3 Sonnet",
            ContextWindow: 200000,
            MaxOutputTokens: 4096,
            SupportsVision: true,
            SupportsTools: true),

        ["claude-3-haiku-20240307"] = new ModelInfo(
            Id: "claude-3-haiku-20240307",
            DisplayName: "Claude 3 Haiku",
            ContextWindow: 200000,
            MaxOutputTokens: 4096,
            SupportsVision: true,
            SupportsTools: true),

        ["claude-3-5-sonnet-20241022"] = new ModelInfo(
            Id: "claude-3-5-sonnet-20241022",
            DisplayName: "Claude 3.5 Sonnet",
            ContextWindow: 200000,
            MaxOutputTokens: 8192,
            SupportsVision: true,
            SupportsTools: true),
    };

    /// <summary>
    /// Provider-specific model lists keyed by provider name (lowercase).
    /// </summary>
    private static readonly Dictionary<string, IReadOnlyList<string>> _providerModels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["openai"] = new[] { "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-3.5-turbo" },
        ["anthropic"] = new[] { "claude-3-opus-20240229", "claude-3-sonnet-20240229", "claude-3-haiku-20240307", "claude-3-5-sonnet-20241022" },
    };

    /// <summary>
    /// Gets default <see cref="ChatOptions"/> for a specific model.
    /// </summary>
    /// <param name="model">The model identifier.</param>
    /// <returns>
    /// Default <see cref="ChatOptions"/> for the model. If the model is unknown,
    /// returns default options with the model identifier set.
    /// </returns>
    /// <remarks>
    /// The returned options include model-appropriate MaxTokens based on the model's
    /// maximum output token limit.
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = ModelDefaults.GetDefaults("gpt-4o");
    /// // Returns ChatOptions with MaxTokens = 4096 (sensible default)
    ///
    /// var customOptions = ModelDefaults.GetDefaults("unknown-model");
    /// // Returns new ChatOptions(Model: "unknown-model")
    /// </code>
    /// </example>
    public static ChatOptions GetDefaults(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return new ChatOptions();
        }

        if (_modelInfo.TryGetValue(model, out var info))
        {
            // LOGIC: Use a sensible default MaxTokens (4096) unless the model
            // has a lower maximum output limit.
            var maxTokens = Math.Min(DefaultMaxOutputTokens, info.MaxOutputTokens);
            return new ChatOptions(Model: model, MaxTokens: maxTokens);
        }

        return new ChatOptions(Model: model);
    }

    /// <summary>
    /// Gets the list of known models for a specific provider.
    /// </summary>
    /// <param name="provider">The provider name (e.g., "openai", "anthropic").</param>
    /// <returns>
    /// A read-only list of model identifiers for the provider.
    /// Returns an empty list if the provider is unknown.
    /// </returns>
    /// <remarks>
    /// Provider name matching is case-insensitive.
    /// </remarks>
    /// <example>
    /// <code>
    /// var openaiModels = ModelDefaults.GetModelList("openai");
    /// // Returns ["gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-3.5-turbo"]
    ///
    /// var unknownModels = ModelDefaults.GetModelList("unknown");
    /// // Returns empty list
    /// </code>
    /// </example>
    public static IReadOnlyList<string> GetModelList(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return Array.Empty<string>();
        }

        return _providerModels.TryGetValue(provider, out var models)
            ? models
            : Array.Empty<string>();
    }

    /// <summary>
    /// Gets the <see cref="ModelInfo"/> for a specific model.
    /// </summary>
    /// <param name="model">The model identifier.</param>
    /// <returns>
    /// The <see cref="ModelInfo"/> for the model, or null if the model is unknown.
    /// </returns>
    /// <example>
    /// <code>
    /// var info = ModelDefaults.GetModelInfo("gpt-4o");
    /// if (info != null)
    /// {
    ///     Console.WriteLine($"Context window: {info.ContextWindow}");
    ///     Console.WriteLine($"Supports vision: {info.SupportsVision}");
    /// }
    /// </code>
    /// </example>
    public static ModelInfo? GetModelInfo(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        return _modelInfo.TryGetValue(model, out var info) ? info : null;
    }

    /// <summary>
    /// Gets the context window size for a model.
    /// </summary>
    /// <param name="model">The model identifier.</param>
    /// <returns>
    /// The context window size in tokens. Returns <see cref="DefaultContextWindow"/>
    /// if the model is unknown.
    /// </returns>
    public static int GetContextWindow(string model)
    {
        var info = GetModelInfo(model);
        return info?.ContextWindow ?? DefaultContextWindow;
    }

    /// <summary>
    /// Gets the maximum output tokens for a model.
    /// </summary>
    /// <param name="model">The model identifier.</param>
    /// <returns>
    /// The maximum output tokens. Returns <see cref="DefaultMaxOutputTokens"/>
    /// if the model is unknown.
    /// </returns>
    public static int GetMaxOutputTokens(string model)
    {
        var info = GetModelInfo(model);
        return info?.MaxOutputTokens ?? DefaultMaxOutputTokens;
    }

    /// <summary>
    /// Checks whether a model supports vision (image) inputs.
    /// </summary>
    /// <param name="model">The model identifier.</param>
    /// <returns>True if the model supports vision; otherwise, false.</returns>
    /// <remarks>
    /// Returns false for unknown models.
    /// </remarks>
    public static bool SupportsVision(string model)
    {
        var info = GetModelInfo(model);
        return info?.SupportsVision ?? false;
    }

    /// <summary>
    /// Checks whether a model supports tool/function calling.
    /// </summary>
    /// <param name="model">The model identifier.</param>
    /// <returns>True if the model supports tools; otherwise, false.</returns>
    /// <remarks>
    /// Returns false for unknown models.
    /// </remarks>
    public static bool SupportsTools(string model)
    {
        var info = GetModelInfo(model);
        return info?.SupportsTools ?? false;
    }

    /// <summary>
    /// Gets all known model identifiers.
    /// </summary>
    /// <returns>A read-only list of all registered model identifiers.</returns>
    public static IReadOnlyList<string> GetAllKnownModels()
    {
        return _modelInfo.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets all known provider names.
    /// </summary>
    /// <returns>A read-only list of all registered provider names.</returns>
    public static IReadOnlyList<string> GetAllKnownProviders()
    {
        return _providerModels.Keys.ToList().AsReadOnly();
    }
}
