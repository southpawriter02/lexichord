// -----------------------------------------------------------------------
// <copyright file="LLMServiceExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.LLM.Configuration;
using Lexichord.Modules.LLM.Services;
using Lexichord.Modules.LLM.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.LLM.Extensions;

/// <summary>
/// Extension methods for registering LLM module services with the DI container.
/// </summary>
/// <remarks>
/// <para>
/// This class provides extension methods to configure and register all LLM module
/// services in a single call, following the standard ASP.NET Core service registration pattern.
/// </para>
/// </remarks>
public static class LLMServiceExtensions
{
    /// <summary>
    /// Adds LLM module services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="LLMOptions"/> configuration binding</description></item>
    ///   <item><description><see cref="ChatOptionsValidator"/> as the default <see cref="IValidator{ChatOptions}"/></description></item>
    ///   <item><description><see cref="ModelRegistry"/> for model discovery and caching</description></item>
    ///   <item><description><see cref="TokenEstimator"/> for token estimation</description></item>
    ///   <item><description><see cref="ChatOptionsResolver"/> for options resolution</description></item>
    ///   <item><description><see cref="ProviderAwareChatOptionsValidatorFactory"/> for provider-specific validation</description></item>
    /// </list>
    /// <para>
    /// <b>Configuration:</b>
    /// </para>
    /// <para>
    /// The LLM options are bound from the "LLM" section of the configuration.
    /// See <see cref="LLMOptions"/> for the expected configuration schema.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Program.cs or Startup.cs
    /// services.AddLLMServices(configuration);
    ///
    /// // Or with inline configuration
    /// services.AddLLMServices(configuration, options =>
    /// {
    ///     options.DefaultProvider = "anthropic";
    ///     options.Defaults.Temperature = 0.8;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddLLMServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        // LOGIC: Bind LLMOptions from configuration.
        services.Configure<LLMOptions>(configuration.GetSection(LLMOptions.SectionName));

        // LOGIC: Register the base ChatOptions validator.
        services.AddSingleton<IValidator<ChatOptions>, ChatOptionsValidator>();

        // LOGIC: Register ModelRegistry as singleton for caching across requests.
        services.AddSingleton<ModelRegistry>();

        // LOGIC: Register TokenEstimator as singleton (stateless service).
        services.AddSingleton<TokenEstimator>();

        // LOGIC: Register ChatOptionsResolver as scoped for per-request resolution.
        services.AddScoped<ChatOptionsResolver>();

        // LOGIC: Register the provider-aware validator factory.
        services.AddSingleton<ProviderAwareChatOptionsValidatorFactory>();

        return services;
    }

    /// <summary>
    /// Adds LLM module services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="configureOptions">Action to configure additional options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    /// <remarks>
    /// This overload allows customizing the <see cref="LLMOptions"/> after
    /// configuration binding, useful for overriding values in tests or
    /// applying environment-specific settings.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddLLMServices(configuration, options =>
    /// {
    ///     // Override the default provider for testing
    ///     options.DefaultProvider = "openai";
    ///     options.Providers["OpenAI"] = new ProviderOptions
    ///     {
    ///         BaseUrl = "https://api.openai.com/v1",
    ///         DefaultModel = "gpt-4o-mini"
    ///     };
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddLLMServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<LLMOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        ArgumentNullException.ThrowIfNull(configureOptions, nameof(configureOptions));

        // LOGIC: First bind from configuration, then apply custom configuration.
        services.Configure<LLMOptions>(configuration.GetSection(LLMOptions.SectionName));
        services.PostConfigure(configureOptions);

        // Register all other services.
        services.AddSingleton<IValidator<ChatOptions>, ChatOptionsValidator>();
        services.AddSingleton<ModelRegistry>();
        services.AddSingleton<TokenEstimator>();
        services.AddScoped<ChatOptionsResolver>();
        services.AddSingleton<ProviderAwareChatOptionsValidatorFactory>();

        return services;
    }

    /// <summary>
    /// Adds LLM options only (without services) for scenarios where only configuration is needed.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Use this method when you only need access to <see cref="LLMOptions"/> via
    /// <see cref="IOptions{TOptions}"/> without the full service registration.
    /// </remarks>
    public static IServiceCollection AddLLMOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        services.Configure<LLMOptions>(configuration.GetSection(LLMOptions.SectionName));
        services.AddSingleton<IValidator<ChatOptions>, ChatOptionsValidator>();

        return services;
    }
}
