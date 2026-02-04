// -----------------------------------------------------------------------
// <copyright file="LLMProviderInfo.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Metadata about an LLM provider registered with the provider registry.
/// </summary>
/// <remarks>
/// <para>
/// This record contains information about a registered LLM provider including
/// its configuration status, supported models, and capabilities.
/// </para>
/// <para>
/// The <see cref="IsConfigured"/> property indicates whether the provider has
/// a valid API key stored in the secure vault. Providers must be configured
/// before they can be used for chat completions.
/// </para>
/// </remarks>
/// <param name="Name">
/// The unique provider identifier used for service resolution.
/// This should be lowercase and URL-safe (e.g., "openai", "anthropic", "ollama").
/// </param>
/// <param name="DisplayName">
/// The human-readable name shown in the user interface (e.g., "OpenAI", "Anthropic", "Ollama").
/// </param>
/// <param name="SupportedModels">
/// The list of model identifiers supported by this provider
/// (e.g., "gpt-4o", "gpt-4o-mini", "claude-3-opus-20240229").
/// </param>
/// <param name="IsConfigured">
/// Whether the provider has valid API key configuration in the secure vault.
/// Providers without configuration cannot be used for chat completions.
/// </param>
/// <param name="SupportsStreaming">
/// Whether the provider supports streaming responses via Server-Sent Events (SSE).
/// </param>
/// <example>
/// <code>
/// // Creating provider info for a configured OpenAI provider
/// var openAI = new LLMProviderInfo(
///     Name: "openai",
///     DisplayName: "OpenAI",
///     SupportedModels: ["gpt-4o", "gpt-4o-mini", "gpt-4-turbo"],
///     IsConfigured: true,
///     SupportsStreaming: true);
///
/// // Creating info for an unconfigured provider
/// var unconfigured = LLMProviderInfo.Unconfigured("anthropic", "Anthropic");
///
/// // Using with expression to update configuration status
/// var configured = unconfigured with { IsConfigured = true };
/// </code>
/// </example>
public record LLMProviderInfo(
    string Name,
    string DisplayName,
    IReadOnlyList<string> SupportedModels,
    bool IsConfigured,
    bool SupportsStreaming)
{
    /// <summary>
    /// Gets the number of models supported by this provider.
    /// </summary>
    /// <value>The count of supported models.</value>
    public int ModelCount => SupportedModels.Count;

    /// <summary>
    /// Gets a value indicating whether this provider has any supported models.
    /// </summary>
    /// <value><c>true</c> if the provider has at least one supported model; otherwise, <c>false</c>.</value>
    public bool HasModels => SupportedModels.Count > 0;

    /// <summary>
    /// Creates provider information for an unconfigured provider.
    /// </summary>
    /// <param name="name">The unique provider identifier.</param>
    /// <param name="displayName">The human-readable display name.</param>
    /// <returns>
    /// A new <see cref="LLMProviderInfo"/> instance with <see cref="IsConfigured"/> set to <c>false</c>,
    /// an empty model list, and <see cref="SupportsStreaming"/> set to <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Use this factory method when registering a provider that hasn't been configured yet.
    /// The provider info can later be updated using a <c>with</c> expression when
    /// configuration is added.
    /// </remarks>
    /// <example>
    /// <code>
    /// var provider = LLMProviderInfo.Unconfigured("openai", "OpenAI");
    /// // Later, when API key is added:
    /// provider = provider with
    /// {
    ///     IsConfigured = true,
    ///     SupportedModels = ["gpt-4o", "gpt-4o-mini"],
    ///     SupportsStreaming = true
    /// };
    /// </code>
    /// </example>
    public static LLMProviderInfo Unconfigured(string name, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));

        return new LLMProviderInfo(
            Name: name,
            DisplayName: displayName,
            SupportedModels: ImmutableArray<string>.Empty,
            IsConfigured: false,
            SupportsStreaming: false);
    }

    /// <summary>
    /// Creates provider information with the specified models and streaming support.
    /// </summary>
    /// <param name="name">The unique provider identifier.</param>
    /// <param name="displayName">The human-readable display name.</param>
    /// <param name="supportedModels">The list of supported model identifiers.</param>
    /// <param name="supportsStreaming">Whether the provider supports streaming.</param>
    /// <returns>
    /// A new <see cref="LLMProviderInfo"/> instance with <see cref="IsConfigured"/> set to <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Use this factory method to create provider information with known models and capabilities
    /// but without configuration. The <see cref="IsConfigured"/> property will be updated
    /// by the registry based on secure vault status.
    /// </remarks>
    /// <example>
    /// <code>
    /// var provider = LLMProviderInfo.Create(
    ///     "openai",
    ///     "OpenAI",
    ///     ["gpt-4o", "gpt-4o-mini"],
    ///     supportsStreaming: true);
    /// </code>
    /// </example>
    public static LLMProviderInfo Create(
        string name,
        string displayName,
        IReadOnlyList<string> supportedModels,
        bool supportsStreaming = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));
        ArgumentNullException.ThrowIfNull(supportedModels, nameof(supportedModels));

        return new LLMProviderInfo(
            Name: name,
            DisplayName: displayName,
            SupportedModels: supportedModels,
            IsConfigured: false,
            SupportsStreaming: supportsStreaming);
    }

    /// <summary>
    /// Checks whether this provider supports a specific model.
    /// </summary>
    /// <param name="modelId">The model identifier to check.</param>
    /// <returns>
    /// <c>true</c> if the model is in the <see cref="SupportedModels"/> list; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Model comparison is case-sensitive. Model identifiers should match the exact format
    /// used by the provider (e.g., "gpt-4o", "claude-3-opus-20240229").
    /// </remarks>
    public bool SupportsModel(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return false;
        }

        return SupportedModels.Contains(modelId);
    }

    /// <summary>
    /// Creates a copy of this provider info with updated configuration status.
    /// </summary>
    /// <param name="isConfigured">The new configuration status.</param>
    /// <returns>A new <see cref="LLMProviderInfo"/> with the updated status.</returns>
    public LLMProviderInfo WithConfigurationStatus(bool isConfigured)
    {
        return this with { IsConfigured = isConfigured };
    }

    /// <summary>
    /// Creates a copy of this provider info with updated models.
    /// </summary>
    /// <param name="models">The new list of supported models.</param>
    /// <returns>A new <see cref="LLMProviderInfo"/> with the updated models.</returns>
    public LLMProviderInfo WithModels(IReadOnlyList<string> models)
    {
        ArgumentNullException.ThrowIfNull(models, nameof(models));
        return this with { SupportedModels = models };
    }
}
