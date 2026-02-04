// -----------------------------------------------------------------------
// <copyright file="AnthropicServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.LLM.Providers.Anthropic;
using Lexichord.Modules.LLM.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Modules.LLM.Extensions;

/// <summary>
/// Extension methods for registering Anthropic services with the DI container.
/// </summary>
/// <remarks>
/// <para>
/// This class provides methods to register the Anthropic chat completion service and its
/// dependencies, including HTTP client configuration with resilience policies.
/// </para>
/// <para>
/// <b>Configuration:</b> The provider reads configuration from the
/// <c>LLM:Providers:Anthropic</c> section in <see cref="IConfiguration"/>.
/// </para>
/// <para>
/// <b>HTTP Client:</b> A named HTTP client "Anthropic" is registered with:
/// </para>
/// <list type="bullet">
///   <item><description>Configurable timeout from <see cref="AnthropicOptions.TimeoutSeconds"/></description></item>
///   <item><description>Retry policy with exponential backoff for transient errors (via <see cref="ResilienceServiceCollectionExtensions"/>)</description></item>
///   <item><description>Circuit breaker to prevent cascade failures (via <see cref="ResilienceServiceCollectionExtensions"/>)</description></item>
/// </list>
/// <para>
/// <b>Anthropic API Requirements:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Authentication via <c>x-api-key</c> header (not Bearer token)</description></item>
///   <item><description>Requires <c>anthropic-version</c> header</description></item>
///   <item><description>API key stored in secure vault as <c>anthropic:api-key</c></description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs or module registration
/// services.AddAnthropicProvider(configuration);
///
/// // Configuration in appsettings.json
/// {
///   "LLM": {
///     "Providers": {
///       "Anthropic": {
///         "BaseUrl": "https://api.anthropic.com/v1",
///         "DefaultModel": "claude-3-haiku-20240307",
///         "ApiVersion": "2024-01-01",
///         "MaxRetries": 3,
///         "TimeoutSeconds": 30
///       }
///     }
///   }
/// }
/// </code>
/// </example>
public static class AnthropicServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Anthropic chat completion provider to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="AnthropicOptions"/> from configuration</description></item>
    ///   <item><description>Named HTTP client "Anthropic" with centralized resilience policies</description></item>
    ///   <item><description><see cref="AnthropicChatCompletionService"/> as keyed singleton</description></item>
    ///   <item><description>Provider metadata with the provider registry</description></item>
    /// </list>
    /// <para>
    /// The retry policy uses exponential backoff with jitter, starting at 2 seconds
    /// and doubling with each retry up to the configured <see cref="ResilienceOptions.RetryCount"/>.
    /// Policy configuration is read from the centralized <c>LLM:Resilience</c> section.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddAnthropicProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        // LOGIC: Bind Anthropic configuration from the LLM:Providers:Anthropic section.
        services.Configure<AnthropicOptions>(
            configuration.GetSection("LLM:Providers:Anthropic"));

        // LOGIC: Get options for HTTP client configuration.
        // Use default values if configuration is not present.
        var optionsSection = configuration.GetSection("LLM:Providers:Anthropic");
        var timeoutSeconds = optionsSection.GetValue("TimeoutSeconds", 30);

        // LOGIC: Get resilience options from centralized configuration (v0.6.2c).
        var resilienceOptions = ResilienceServiceCollectionExtensions.GetResilienceOptions(configuration);

        // LOGIC: Register named HTTP client with centralized resilience policies.
        services.AddHttpClient(AnthropicOptions.HttpClientName)
            .ConfigureHttpClient(client =>
            {
                // LOGIC: Set timeout from configuration.
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            })
            .AddLLMResiliencePolicies(resilienceOptions);

        // LOGIC: Register Anthropic provider using the existing extension method.
        services.AddChatCompletionProvider<AnthropicChatCompletionService>(
            providerName: "anthropic",
            displayName: "Anthropic",
            supportedModels: AnthropicOptions.SupportedModels,
            supportsStreaming: true);

        return services;
    }
}
