// -----------------------------------------------------------------------
// <copyright file="AnthropicServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.LLM.Providers.Anthropic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

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
///   <item><description>Retry policy with exponential backoff for transient errors</description></item>
///   <item><description>Circuit breaker to prevent cascade failures</description></item>
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
    ///   <item><description>Named HTTP client "Anthropic" with resilience policies</description></item>
    ///   <item><description><see cref="AnthropicChatCompletionService"/> as keyed singleton</description></item>
    ///   <item><description>Provider metadata with the provider registry</description></item>
    /// </list>
    /// <para>
    /// The retry policy uses exponential backoff with jitter, starting at 2 seconds
    /// and doubling with each retry up to the configured <see cref="AnthropicOptions.MaxRetries"/>.
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
        var maxRetries = optionsSection.GetValue("MaxRetries", 3);

        // LOGIC: Register named HTTP client with resilience policies.
        services.AddHttpClient(AnthropicOptions.HttpClientName)
            .ConfigureHttpClient(client =>
            {
                // LOGIC: Set timeout from configuration.
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            })
            .AddPolicyHandler(GetRetryPolicy(maxRetries))
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // LOGIC: Register Anthropic provider using the existing extension method.
        services.AddChatCompletionProvider<AnthropicChatCompletionService>(
            providerName: "anthropic",
            displayName: "Anthropic",
            supportedModels: AnthropicOptions.SupportedModels,
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
    ///   <item><description>HTTP 429 rate limit</description></item>
    ///   <item><description>HTTP 529 overloaded (Anthropic-specific)</description></item>
    ///   <item><description>Network-level failures (HttpRequestException)</description></item>
    /// </list>
    /// <para>
    /// Uses exponential backoff: 2^attempt seconds + random jitter (0-1000ms).
    /// </para>
    /// <para>
    /// Note: Anthropic may return 529 (overloaded_error) which is handled as a transient error.
    /// </para>
    /// </remarks>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int maxRetries)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .OrResult(msg => (int)msg.StatusCode == 529) // Anthropic overloaded
            .WaitAndRetryAsync(
                retryCount: maxRetries,
                sleepDurationProvider: (attempt, response, _) =>
                {
                    // LOGIC: Check for Retry-After header first.
                    // Note: Anthropic doesn't consistently provide this header.
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
    /// allowing time for the service to recover. This is particularly important
    /// for Anthropic's overloaded_error scenarios.
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
