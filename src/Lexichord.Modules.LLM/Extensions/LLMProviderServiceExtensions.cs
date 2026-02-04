// -----------------------------------------------------------------------
// <copyright file="LLMProviderServiceExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.LLM.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lexichord.Modules.LLM.Extensions;

/// <summary>
/// Extension methods for registering LLM providers with the DI container.
/// </summary>
/// <remarks>
/// <para>
/// This class provides methods to register the provider registry and individual
/// LLM providers using the .NET 8+ keyed services feature.
/// </para>
/// <para>
/// <b>Usage:</b>
/// </para>
/// <code>
/// // In Program.cs or module registration
/// services.AddLLMProviderRegistry();
/// services.AddChatCompletionProvider&lt;OpenAIChatService&gt;(
///     "openai",
///     "OpenAI",
///     ["gpt-4o", "gpt-4o-mini", "gpt-4-turbo"],
///     supportsStreaming: true);
/// </code>
/// </remarks>
public static class LLMProviderServiceExtensions
{
    /// <summary>
    /// Registers the LLM provider registry and core services.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="LLMProviderRegistry"/> as a singleton</description></item>
    ///   <item><description><see cref="ILLMProviderRegistry"/> pointing to the same instance</description></item>
    /// </list>
    /// <para>
    /// Call this method once during application startup before registering
    /// individual providers with <see cref="AddChatCompletionProvider{TProvider}"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddLLMProviderRegistry();
    /// </code>
    /// </example>
    public static IServiceCollection AddLLMProviderRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        // LOGIC: Register LLMProviderRegistry as singleton for shared state.
        services.TryAddSingleton<LLMProviderRegistry>();

        // LOGIC: Register ILLMProviderRegistry as a facade pointing to the same instance.
        services.TryAddSingleton<ILLMProviderRegistry>(sp =>
            sp.GetRequiredService<LLMProviderRegistry>());

        return services;
    }

    /// <summary>
    /// Registers a chat completion provider with keyed service registration.
    /// </summary>
    /// <typeparam name="TProvider">
    /// The provider implementation type that implements <see cref="IChatCompletionService"/>.
    /// </typeparam>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="providerName">
    /// The unique provider identifier used for service resolution (e.g., "openai").
    /// This should be lowercase and URL-safe.
    /// </param>
    /// <param name="displayName">
    /// The human-readable name shown in the UI (e.g., "OpenAI").
    /// </param>
    /// <param name="supportedModels">
    /// The list of model identifiers supported by this provider.
    /// </param>
    /// <param name="supportsStreaming">
    /// Whether the provider supports streaming responses. Defaults to <c>true</c>.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any required parameter is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> or <paramref name="displayName"/>
    /// is empty or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Registers the provider as a keyed singleton service</description></item>
    ///   <item><description>Registers provider metadata with the registry during startup</description></item>
    /// </list>
    /// <para>
    /// The provider's <see cref="LLMProviderInfo.IsConfigured"/> status will be
    /// determined at runtime based on secure vault API key presence.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddLLMProviderRegistry();
    /// services.AddChatCompletionProvider&lt;OpenAIChatCompletionService&gt;(
    ///     "openai",
    ///     "OpenAI",
    ///     ["gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-3.5-turbo"],
    ///     supportsStreaming: true);
    ///
    /// services.AddChatCompletionProvider&lt;AnthropicChatCompletionService&gt;(
    ///     "anthropic",
    ///     "Anthropic",
    ///     ["claude-3-opus-20240229", "claude-3-sonnet-20240229", "claude-3-haiku-20240307"],
    ///     supportsStreaming: true);
    /// </code>
    /// </example>
    public static IServiceCollection AddChatCompletionProvider<TProvider>(
        this IServiceCollection services,
        string providerName,
        string displayName,
        IReadOnlyList<string> supportedModels,
        bool supportsStreaming = true)
        where TProvider : class, IChatCompletionService
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));
        ArgumentNullException.ThrowIfNull(supportedModels, nameof(supportedModels));

        var normalizedName = providerName.ToLowerInvariant();

        // LOGIC: Register the provider as a keyed singleton service.
        // This allows resolution via IServiceProvider.GetKeyedService<IChatCompletionService>(providerName).
        services.AddKeyedSingleton<IChatCompletionService, TProvider>(normalizedName);

        // LOGIC: Create provider info for registration.
        // IsConfigured will be false initially; RefreshConfigurationStatusAsync updates it at runtime.
        var providerInfo = new LLMProviderInfo(
            Name: normalizedName,
            DisplayName: displayName,
            SupportedModels: supportedModels,
            IsConfigured: false,
            SupportsStreaming: supportsStreaming);

        // LOGIC: Register provider info with the registry during initialization.
        // We use a hosted service style initialization via IServiceProvider.
        services.AddSingleton<IProviderRegistration>(new ProviderRegistration(providerInfo));

        return services;
    }

    /// <summary>
    /// Registers a chat completion provider with a custom factory function.
    /// </summary>
    /// <typeparam name="TProvider">
    /// The provider implementation type that implements <see cref="IChatCompletionService"/>.
    /// </typeparam>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="providerName">The unique provider identifier.</param>
    /// <param name="displayName">The human-readable display name.</param>
    /// <param name="supportedModels">The list of supported model identifiers.</param>
    /// <param name="factory">The factory function to create provider instances.</param>
    /// <param name="supportsStreaming">Whether the provider supports streaming.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any required parameter is null.
    /// </exception>
    /// <remarks>
    /// Use this overload when the provider requires custom construction logic
    /// that cannot be satisfied by standard DI resolution.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddChatCompletionProvider&lt;CustomProvider&gt;(
    ///     "custom",
    ///     "Custom LLM",
    ///     ["model-1", "model-2"],
    ///     sp => new CustomProvider(
    ///         sp.GetRequiredService&lt;IHttpClientFactory&gt;(),
    ///         sp.GetRequiredService&lt;ISecureVault&gt;(),
    ///         customConfig),
    ///     supportsStreaming: false);
    /// </code>
    /// </example>
    public static IServiceCollection AddChatCompletionProvider<TProvider>(
        this IServiceCollection services,
        string providerName,
        string displayName,
        IReadOnlyList<string> supportedModels,
        Func<IServiceProvider, TProvider> factory,
        bool supportsStreaming = true)
        where TProvider : class, IChatCompletionService
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));
        ArgumentNullException.ThrowIfNull(supportedModels, nameof(supportedModels));
        ArgumentNullException.ThrowIfNull(factory, nameof(factory));

        var normalizedName = providerName.ToLowerInvariant();

        // LOGIC: Register with custom factory function.
        services.AddKeyedSingleton<IChatCompletionService>(normalizedName, (sp, _) => factory(sp));

        var providerInfo = new LLMProviderInfo(
            Name: normalizedName,
            DisplayName: displayName,
            SupportedModels: supportedModels,
            IsConfigured: false,
            SupportsStreaming: supportsStreaming);

        services.AddSingleton<IProviderRegistration>(new ProviderRegistration(providerInfo));

        return services;
    }

    /// <summary>
    /// Initializes the provider registry with all registered provider information.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method should be called during application startup after all services
    /// are registered. It:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Registers all provider metadata with the registry</description></item>
    ///   <item><description>Checks the secure vault for API key configuration</description></item>
    /// </list>
    /// </remarks>
    public static async Task InitializeProviderRegistryAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));

        var registry = serviceProvider.GetRequiredService<LLMProviderRegistry>();
        var registrations = serviceProvider.GetServices<IProviderRegistration>();

        // LOGIC: Register all provider info with the registry.
        foreach (var registration in registrations)
        {
            registry.RegisterProvider(registration.ProviderInfo);
        }

        // LOGIC: Refresh configuration status from secure vault.
        await registry.RefreshConfigurationStatusAsync(cancellationToken);
    }
}

/// <summary>
/// Internal interface for provider registration metadata.
/// </summary>
internal interface IProviderRegistration
{
    /// <summary>
    /// Gets the provider information to register.
    /// </summary>
    LLMProviderInfo ProviderInfo { get; }
}

/// <summary>
/// Internal record for storing provider registration metadata.
/// </summary>
/// <param name="ProviderInfo">The provider information to register.</param>
internal sealed record ProviderRegistration(LLMProviderInfo ProviderInfo) : IProviderRegistration;
