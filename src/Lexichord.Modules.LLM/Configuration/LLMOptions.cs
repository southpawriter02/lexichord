// -----------------------------------------------------------------------
// <copyright file="LLMOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.LLM.Configuration;

/// <summary>
/// Root configuration options for the LLM module, bindable to <c>appsettings.json</c>.
/// </summary>
/// <remarks>
/// <para>
/// This class is designed for binding to the "LLM" section in <c>appsettings.json</c>.
/// It contains provider configurations and default chat options.
/// </para>
/// <para>
/// Example configuration:
/// </para>
/// <code>
/// {
///   "LLM": {
///     "DefaultProvider": "openai",
///     "Providers": {
///       "OpenAI": {
///         "BaseUrl": "https://api.openai.com/v1",
///         "DefaultModel": "gpt-4o-mini",
///         "MaxRetries": 3,
///         "TimeoutSeconds": 30
///       },
///       "Anthropic": {
///         "BaseUrl": "https://api.anthropic.com/v1",
///         "DefaultModel": "claude-3-haiku-20240307",
///         "MaxRetries": 3,
///         "TimeoutSeconds": 30
///       }
///     },
///     "Defaults": {
///       "Temperature": 0.7,
///       "MaxTokens": 2048,
///       "TopP": 1.0
///     }
///   }
/// }
/// </code>
/// </remarks>
public class LLMOptions
{
    /// <summary>
    /// The configuration section name for binding.
    /// </summary>
    /// <value>The literal string "LLM".</value>
    public const string SectionName = "LLM";

    /// <summary>
    /// Gets or sets the default provider name.
    /// </summary>
    /// <value>The default provider identifier. Defaults to "openai".</value>
    /// <remarks>
    /// This should match a key in the <see cref="Providers"/> dictionary (case-insensitive).
    /// </remarks>
    public string DefaultProvider { get; set; } = "openai";

    /// <summary>
    /// Gets or sets the per-provider configuration dictionary.
    /// </summary>
    /// <value>A dictionary mapping provider names to their configurations.</value>
    /// <remarks>
    /// Keys are provider identifiers (e.g., "OpenAI", "Anthropic").
    /// Each provider has its own <see cref="ProviderOptions"/> configuration.
    /// </remarks>
    public Dictionary<string, ProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the default chat options used when not explicitly specified.
    /// </summary>
    /// <value>The default chat options configuration.</value>
    /// <remarks>
    /// These defaults are applied via <see cref="ChatOptionsResolver"/> when creating
    /// resolved <see cref="Abstractions.Contracts.LLM.ChatOptions"/> instances.
    /// </remarks>
    public ChatOptionsDefaults Defaults { get; set; } = new();

    /// <summary>
    /// Gets a value indicating whether a default provider is configured.
    /// </summary>
    /// <value>True if a default provider is specified and exists in <see cref="Providers"/>; otherwise, false.</value>
    public bool HasDefaultProvider =>
        !string.IsNullOrWhiteSpace(DefaultProvider) &&
        Providers.ContainsKey(DefaultProvider);

    /// <summary>
    /// Gets the configuration for the default provider.
    /// </summary>
    /// <returns>The default provider's <see cref="ProviderOptions"/>, or null if not configured.</returns>
    public ProviderOptions? GetDefaultProviderOptions()
    {
        if (string.IsNullOrWhiteSpace(DefaultProvider))
        {
            return null;
        }

        return Providers.TryGetValue(DefaultProvider, out var options) ? options : null;
    }

    /// <summary>
    /// Gets the configuration for a specific provider by name.
    /// </summary>
    /// <param name="providerName">The provider name to look up.</param>
    /// <returns>The provider's <see cref="ProviderOptions"/>, or null if not found.</returns>
    /// <remarks>
    /// Provider name matching is case-insensitive.
    /// </remarks>
    public ProviderOptions? GetProviderOptions(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return null;
        }

        return Providers.TryGetValue(providerName, out var options) ? options : null;
    }
}
