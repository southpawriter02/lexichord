// -----------------------------------------------------------------------
// <copyright file="ResilienceServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Lexichord.Modules.LLM.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace Lexichord.Modules.LLM.Extensions;

/// <summary>
/// Extension methods for registering LLM resilience services.
/// </summary>
/// <remarks>
/// <para>
/// This class provides methods to register centralized resilience infrastructure:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="ResilienceOptions"/> configuration binding</description></item>
///   <item><description><see cref="IResiliencePipeline"/> for direct pipeline execution</description></item>
///   <item><description><see cref="ResilienceTelemetry"/> for metrics collection</description></item>
///   <item><description><see cref="LLMCircuitBreakerHealthCheck"/> for health monitoring</description></item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// </para>
/// <code>
/// // In module registration
/// services.AddLLMResilience(configuration);
///
/// // For HTTP clients
/// services.AddHttpClient("MyProvider")
///     .AddLLMResiliencePolicies(configuration);
/// </code>
/// </remarks>
public static class ResilienceServiceCollectionExtensions
{
    /// <summary>
    /// HTTP status code for Anthropic's overloaded error.
    /// </summary>
    private const int AnthropicOverloadedStatusCode = 529;

    /// <summary>
    /// Adds LLM resilience services to the service collection.
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
    ///   <item><description><see cref="ResilienceOptions"/> bound from <c>LLM:Resilience</c> configuration section</description></item>
    ///   <item><description><see cref="IResiliencePipeline"/> as singleton</description></item>
    ///   <item><description><see cref="ResilienceTelemetry"/> as singleton</description></item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddLLMResilience(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        // LOGIC: Bind configuration from LLM:Resilience section.
        services.Configure<ResilienceOptions>(
            configuration.GetSection(ResilienceOptions.SectionName));

        // LOGIC: Register the resilience pipeline as a singleton.
        // Singleton ensures circuit breaker state is shared across all requests.
        services.TryAddSingleton<IResiliencePipeline, LLMResiliencePipeline>();

        // LOGIC: Register telemetry as singleton for aggregated metrics.
        services.TryAddSingleton<ResilienceTelemetry>();

        // LOGIC: Wire up telemetry to pipeline events.
        services.AddSingleton(sp =>
        {
            var pipeline = sp.GetRequiredService<IResiliencePipeline>();
            var telemetry = sp.GetRequiredService<ResilienceTelemetry>();

            // Subscribe telemetry to pipeline events
            pipeline.OnPolicyEvent += (_, evt) => telemetry.RecordEvent(evt);

            return telemetry;
        });

        return services;
    }

    /// <summary>
    /// Adds LLM resilience policies to an HTTP client builder.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The HTTP client builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> or <paramref name="configuration"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method adds Polly policies directly to the HTTP client pipeline:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Retry policy with exponential backoff and jitter</description></item>
    ///   <item><description>Circuit breaker policy</description></item>
    /// </list>
    /// <para>
    /// Use this method when you want the policies integrated with <see cref="IHttpClientFactory"/>
    /// rather than using <see cref="IResiliencePipeline"/> directly.
    /// </para>
    /// </remarks>
    public static IHttpClientBuilder AddLLMResiliencePolicies(
        this IHttpClientBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        // LOGIC: Get resilience options from configuration.
        var section = configuration.GetSection(ResilienceOptions.SectionName);
        var options = new ResilienceOptions(
            RetryCount: section.GetValue("RetryCount", 3),
            RetryBaseDelaySeconds: section.GetValue("RetryBaseDelaySeconds", 1.0),
            RetryMaxDelaySeconds: section.GetValue("RetryMaxDelaySeconds", 30.0),
            CircuitBreakerThreshold: section.GetValue("CircuitBreakerThreshold", 5),
            CircuitBreakerDurationSeconds: section.GetValue("CircuitBreakerDurationSeconds", 30),
            TimeoutSeconds: section.GetValue("TimeoutSeconds", 30),
            BulkheadMaxConcurrency: section.GetValue("BulkheadMaxConcurrency", 10),
            BulkheadMaxQueue: section.GetValue("BulkheadMaxQueue", 100));

        return builder
            .AddPolicyHandler(GetRetryPolicy(options))
            .AddPolicyHandler(GetCircuitBreakerPolicy(options));
    }

    /// <summary>
    /// Adds LLM resilience policies to an HTTP client builder with explicit options.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="options">The resilience options to use.</param>
    /// <returns>The HTTP client builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> or <paramref name="options"/> is null.
    /// </exception>
    public static IHttpClientBuilder AddLLMResiliencePolicies(
        this IHttpClientBuilder builder,
        ResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        return builder
            .AddPolicyHandler(GetRetryPolicy(options))
            .AddPolicyHandler(GetCircuitBreakerPolicy(options));
    }

    /// <summary>
    /// Gets the resilience options from configuration with defaults.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The resilience options.</returns>
    public static ResilienceOptions GetResilienceOptions(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        var section = configuration.GetSection(ResilienceOptions.SectionName);
        return new ResilienceOptions(
            RetryCount: section.GetValue("RetryCount", 3),
            RetryBaseDelaySeconds: section.GetValue("RetryBaseDelaySeconds", 1.0),
            RetryMaxDelaySeconds: section.GetValue("RetryMaxDelaySeconds", 30.0),
            CircuitBreakerThreshold: section.GetValue("CircuitBreakerThreshold", 5),
            CircuitBreakerDurationSeconds: section.GetValue("CircuitBreakerDurationSeconds", 30),
            TimeoutSeconds: section.GetValue("TimeoutSeconds", 30),
            BulkheadMaxConcurrency: section.GetValue("BulkheadMaxConcurrency", 10),
            BulkheadMaxQueue: section.GetValue("BulkheadMaxQueue", 100));
    }

    /// <summary>
    /// Creates a retry policy for transient HTTP errors.
    /// </summary>
    /// <param name="options">The resilience options.</param>
    /// <returns>An async retry policy.</returns>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ResilienceOptions options)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .OrResult(msg => (int)msg.StatusCode == AnthropicOverloadedStatusCode)
            .WaitAndRetryAsync(
                retryCount: options.RetryCount,
                sleepDurationProvider: (attempt, response, _) =>
                {
                    // LOGIC: Check for Retry-After header first.
                    var retryAfter = response?.Result?.Headers?.RetryAfter?.Delta;
                    if (retryAfter.HasValue)
                    {
                        return retryAfter.Value;
                    }

                    // LOGIC: Fall back to exponential backoff with jitter.
                    var exponentialDelay = TimeSpan.FromSeconds(
                        Math.Pow(2, attempt) * options.RetryBaseDelaySeconds);
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                    var totalDelay = exponentialDelay + jitter;

                    // LOGIC: Cap at maximum delay.
                    return totalDelay > options.RetryMaxDelay ? options.RetryMaxDelay : totalDelay;
                },
                onRetryAsync: (_, _, _, _) => Task.CompletedTask);
    }

    /// <summary>
    /// Creates a circuit breaker policy to prevent cascade failures.
    /// </summary>
    /// <param name="options">The resilience options.</param>
    /// <returns>A circuit breaker policy.</returns>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ResilienceOptions options)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: options.CircuitBreakerThreshold,
                durationOfBreak: options.CircuitBreakerDuration);
    }
}
