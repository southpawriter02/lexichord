// -----------------------------------------------------------------------
// <copyright file="OpenAIServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.LLM.Providers.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;

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
///   <item><description>Retry policy with exponential backoff for transient errors</description></item>
///   <item><description>Circuit breaker to prevent cascade failures</description></item>
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
    ///   <item><description>Named HTTP client "OpenAI" with resilience policies</description></item>
    ///   <item><description><see cref="OpenAIChatCompletionService"/> as keyed singleton</description></item>
    ///   <item><description>Provider metadata with the provider registry</description></item>
    /// </list>
    /// <para>
    /// The retry policy uses exponential backoff with jitter, starting at 2 seconds
    /// and doubling with each retry up to the configured <see cref="OpenAIOptions.MaxRetries"/>.
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
        var maxRetries = optionsSection.GetValue("MaxRetries", 3);

        // LOGIC: Register named HTTP client with resilience policies.
        services.AddHttpClient(OpenAIOptions.HttpClientName)
            .ConfigureHttpClient(client =>
            {
                // LOGIC: Set timeout from configuration.
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            })
            .AddPolicyHandler(GetRetryPolicy(maxRetries))
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // LOGIC: Register OpenAI provider using the existing extension method.
        services.AddChatCompletionProvider<OpenAIChatCompletionService>(
            providerName: "openai",
            displayName: "OpenAI",
            supportedModels: OpenAIOptions.SupportedModels,
            supportsStreaming: true);

        return services;
    }

    /// <summary>
    /// Creates a retry policy for transient HTTP errors.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <returns>An async retry policy.</returns>
    /// <remarks>
    /// <para>
    /// The policy handles:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>HTTP 5xx server errors</description></item>
    ///   <item><description>HTTP 408 request timeout</description></item>
    ///   <item><description>HTTP 429 rate limit (with potential Retry-After header)</description></item>
    ///   <item><description>Network-level failures (HttpRequestException)</description></item>
    /// </list>
    /// <para>
    /// Uses exponential backoff: 2^attempt seconds + random jitter (0-1000ms).
    /// </para>
    /// </remarks>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int maxRetries)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: maxRetries,
                sleepDurationProvider: (attempt, response, _) =>
                {
                    // LOGIC: Check for Retry-After header first.
                    var retryAfter = response?.Result?.Headers?.RetryAfter?.Delta;
                    if (retryAfter.HasValue)
                    {
                        return retryAfter.Value;
                    }

                    // LOGIC: Fall back to exponential backoff with jitter.
                    var exponentialDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                    return exponentialDelay + jitter;
                },
                onRetryAsync: (_, _, _, _) => Task.CompletedTask);
    }

    /// <summary>
    /// Creates a circuit breaker policy to prevent cascade failures.
    /// </summary>
    /// <returns>A circuit breaker policy.</returns>
    /// <remarks>
    /// <para>
    /// The circuit breaker opens after 5 consecutive failures and remains open
    /// for 30 seconds before attempting a test request.
    /// </para>
    /// <para>
    /// This protects the application from continuously hitting a failing API,
    /// allowing time for the service to recover.
    /// </para>
    /// </remarks>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}
