// -----------------------------------------------------------------------
// <copyright file="OpenAIServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.LLM.Providers.OpenAI;
using Lexichord.Modules.LLM.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Modules.LLM.Extensions;

/// <summary>
/// Extension methods for registering OpenAI services with the DI container.
/// </summary>
/// <remarks>
/// <para>
/// This class provides methods to register the OpenAI chat completion service and its
/// dependencies, including HTTP client configuration with resilience policies.
/// </para>
/// <para>
/// <b>Configuration:</b> The provider reads configuration from the
/// <c>LLM:Providers:OpenAI</c> section in <see cref="IConfiguration"/>.
/// </para>
/// <para>
/// <b>HTTP Client:</b> A named HTTP client "OpenAI" is registered with:
/// </para>
/// <list type="bullet">
///   <item><description>Configurable timeout from <see cref="OpenAIOptions.TimeoutSeconds"/></description></item>
///   <item><description>Retry policy with exponential backoff for transient errors (via <see cref="ResilienceServiceCollectionExtensions"/>)</description></item>
///   <item><description>Circuit breaker to prevent cascade failures (via <see cref="ResilienceServiceCollectionExtensions"/>)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs or module registration
/// services.AddOpenAIProvider(configuration);
///
/// // Configuration in appsettings.json
/// {
///   "LLM": {
///     "Providers": {
///       "OpenAI": {
///         "BaseUrl": "https://api.openai.com/v1",
///         "DefaultModel": "gpt-4o-mini",
///         "MaxRetries": 3,
///         "TimeoutSeconds": 30
///       }
///     },
///     "Resilience": {
///       "RetryCount": 3,
///       "CircuitBreakerThreshold": 5
///     }
///   }
/// }
/// </code>
/// </example>
public static class OpenAIServiceCollectionExtensions
{
    /// <summary>
    /// Adds the OpenAI chat completion provider to the service collection.
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
    ///   <item><description><see cref="OpenAIOptions"/> from configuration</description></item>
    ///   <item><description>Named HTTP client "OpenAI" with centralized resilience policies</description></item>
    ///   <item><description><see cref="OpenAIChatCompletionService"/> as keyed singleton</description></item>
    ///   <item><description>Provider metadata with the provider registry</description></item>
    /// </list>
    /// <para>
    /// The retry policy uses exponential backoff with jitter, starting at 2 seconds
    /// and doubling with each retry up to the configured <see cref="OpenAIOptions.MaxRetries"/>.
    /// Policy configuration is read from the centralized <c>LLM:Resilience</c> section.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddOpenAIProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        // LOGIC: Bind OpenAI configuration from the LLM:Providers:OpenAI section.
        services.Configure<OpenAIOptions>(
            configuration.GetSection("LLM:Providers:OpenAI"));

        // LOGIC: Get options for HTTP client configuration.
        // Use default values if configuration is not present.
        var optionsSection = configuration.GetSection("LLM:Providers:OpenAI");
        var timeoutSeconds = optionsSection.GetValue("TimeoutSeconds", 30);

        // LOGIC: Get resilience options from centralized configuration (v0.6.2c).
        var resilienceOptions = ResilienceServiceCollectionExtensions.GetResilienceOptions(configuration);

        // LOGIC: Register named HTTP client with centralized resilience policies.
        services.AddHttpClient(OpenAIOptions.HttpClientName)
            .ConfigureHttpClient(client =>
            {
                // LOGIC: Set timeout from configuration.
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            })
            .AddLLMResiliencePolicies(resilienceOptions);

        // LOGIC: Register OpenAI provider using the existing extension method.
        services.AddChatCompletionProvider<OpenAIChatCompletionService>(
            providerName: "openai",
            displayName: "OpenAI",
            supportedModels: OpenAIOptions.SupportedModels,
            supportsStreaming: true);

        return services;
    }
}
