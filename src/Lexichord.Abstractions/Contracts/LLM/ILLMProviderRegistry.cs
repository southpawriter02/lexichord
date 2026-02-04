// -----------------------------------------------------------------------
// <copyright file="ILLMProviderRegistry.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Registry for managing LLM provider instances and selection.
/// </summary>
/// <remarks>
/// <para>
/// The provider registry serves as the central management point for all LLM providers
/// in the application. It provides:
/// </para>
/// <list type="bullet">
///   <item><description>Discovery of available providers and their capabilities</description></item>
///   <item><description>Resolution of provider instances by name</description></item>
///   <item><description>Default provider selection and persistence</description></item>
///   <item><description>Configuration status checking for API key presence</description></item>
/// </list>
/// <para>
/// Providers are registered during application startup using the
/// <c>AddChatCompletionProvider&lt;T&gt;</c> extension method. The registry
/// automatically checks the secure vault for API key configuration.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe as the registry
/// is typically registered as a singleton and accessed from multiple threads.
/// </para>
/// <para>
/// <b>License Gating:</b> Access to multiple configured providers may be gated
/// by license tier. Core users may be limited to a single configured provider.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get the default provider and make a chat completion request
/// var provider = registry.GetDefaultProvider();
/// var response = await provider.CompleteAsync(request);
///
/// // List all available providers
/// foreach (var info in registry.AvailableProviders)
/// {
///     Console.WriteLine($"{info.DisplayName}: {(info.IsConfigured ? "Configured" : "Not configured")}");
/// }
///
/// // Switch to a specific provider
/// if (registry.IsProviderConfigured("anthropic"))
/// {
///     registry.SetDefaultProvider("anthropic");
/// }
/// </code>
/// </example>
public interface ILLMProviderRegistry
{
    /// <summary>
    /// Gets information about all registered providers.
    /// </summary>
    /// <value>
    /// A read-only list of <see cref="LLMProviderInfo"/> for all registered providers,
    /// including both configured and unconfigured providers.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property returns metadata about all providers that have been registered
    /// with the registry, regardless of their configuration status. Use the
    /// <see cref="LLMProviderInfo.IsConfigured"/> property to filter for
    /// providers that are ready for use.
    /// </para>
    /// <para>
    /// The list is ordered by registration time (first registered first).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Get only configured providers
    /// var configuredProviders = registry.AvailableProviders
    ///     .Where(p => p.IsConfigured)
    ///     .ToList();
    ///
    /// // Get providers that support streaming
    /// var streamingProviders = registry.AvailableProviders
    ///     .Where(p => p.IsConfigured &amp;&amp; p.SupportsStreaming)
    ///     .ToList();
    /// </code>
    /// </example>
    IReadOnlyList<LLMProviderInfo> AvailableProviders { get; }

    /// <summary>
    /// Gets a provider service instance by its unique name.
    /// </summary>
    /// <param name="providerName">
    /// The unique provider identifier (e.g., "openai", "anthropic").
    /// Comparison is case-insensitive.
    /// </param>
    /// <returns>
    /// The <see cref="IChatCompletionService"/> instance for the specified provider.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="providerName"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> is empty or whitespace.
    /// </exception>
    /// <exception cref="ProviderNotFoundException">
    /// Thrown when no provider with the specified name is registered.
    /// </exception>
    /// <exception cref="ProviderNotConfiguredException">
    /// Thrown when the provider is registered but lacks API key configuration.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method resolves the provider service from the DI container using
    /// keyed service registration. The provider must be both registered and
    /// configured (have a valid API key) to be returned.
    /// </para>
    /// <para>
    /// Provider names are case-insensitive. "OpenAI", "openai", and "OPENAI"
    /// all resolve to the same provider.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// try
    /// {
    ///     var openAI = registry.GetProvider("openai");
    ///     var response = await openAI.CompleteAsync(request);
    /// }
    /// catch (ProviderNotFoundException)
    /// {
    ///     Console.WriteLine("OpenAI provider is not registered.");
    /// }
    /// catch (ProviderNotConfiguredException)
    /// {
    ///     Console.WriteLine("OpenAI API key is not configured.");
    /// }
    /// </code>
    /// </example>
    IChatCompletionService GetProvider(string providerName);

    /// <summary>
    /// Gets the user's default provider service instance.
    /// </summary>
    /// <returns>
    /// The <see cref="IChatCompletionService"/> instance for the default provider.
    /// </returns>
    /// <exception cref="ProviderNotConfiguredException">
    /// Thrown when no default provider is set and no configured providers are available,
    /// or when the default provider lacks API key configuration.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method returns the provider that the user has designated as their default,
    /// as persisted via <see cref="SetDefaultProvider"/>. If no default has been
    /// explicitly set, the first configured provider (by registration order) is returned.
    /// </para>
    /// <para>
    /// The default provider preference is persisted across application restarts.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Use the default provider for chat completions
    /// var provider = registry.GetDefaultProvider();
    /// var response = await provider.CompleteAsync(request);
    /// </code>
    /// </example>
    IChatCompletionService GetDefaultProvider();

    /// <summary>
    /// Sets the default provider for chat operations.
    /// </summary>
    /// <param name="providerName">
    /// The unique provider identifier to set as default.
    /// Comparison is case-insensitive.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="providerName"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> is empty or whitespace.
    /// </exception>
    /// <exception cref="ProviderNotFoundException">
    /// Thrown when no provider with the specified name is registered.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method persists the user's default provider preference. The preference
    /// is stored and will be maintained across application restarts.
    /// </para>
    /// <para>
    /// Note: Setting a default provider does not require the provider to be
    /// configured. This allows users to set a preference before configuring
    /// API keys. However, <see cref="GetDefaultProvider"/> will throw
    /// <see cref="ProviderNotConfiguredException"/> if the default provider
    /// is not configured when attempting to use it.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Set Anthropic as the default provider
    /// registry.SetDefaultProvider("anthropic");
    ///
    /// // Now GetDefaultProvider() returns the Anthropic provider
    /// var provider = registry.GetDefaultProvider();
    /// Console.WriteLine(provider.ProviderName); // "anthropic"
    /// </code>
    /// </example>
    void SetDefaultProvider(string providerName);

    /// <summary>
    /// Checks if a provider has valid API key configuration.
    /// </summary>
    /// <param name="providerName">
    /// The unique provider identifier to check.
    /// Comparison is case-insensitive.
    /// </param>
    /// <returns>
    /// <c>true</c> if the provider is registered and has a valid API key
    /// in the secure vault; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method checks both registration status and API key presence.
    /// It returns <c>false</c> for unregistered providers rather than
    /// throwing an exception, making it suitable for conditional logic.
    /// </para>
    /// <para>
    /// Use this method before calling <see cref="GetProvider"/> to avoid
    /// exception handling for expected cases where a provider may not be
    /// configured.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Conditionally use a provider only if configured
    /// if (registry.IsProviderConfigured("openai"))
    /// {
    ///     var provider = registry.GetProvider("openai");
    ///     var response = await provider.CompleteAsync(request);
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Please configure OpenAI API key in Settings.");
    /// }
    /// </code>
    /// </example>
    bool IsProviderConfigured(string providerName);
}
