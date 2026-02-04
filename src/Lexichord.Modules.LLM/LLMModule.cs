// -----------------------------------------------------------------------
// <copyright file="LLMModule.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.LLM.Configuration;
using Lexichord.Modules.LLM.Extensions;
using Lexichord.Modules.LLM.Services;
using Lexichord.Modules.LLM.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.LLM;

/// <summary>
/// The LLM module provides chat options configuration, validation, and provider abstractions.
/// </summary>
/// <remarks>
/// <para>
/// This module serves as the "Gateway" for LLM provider interactions, providing:
/// </para>
/// <list type="bullet">
///   <item><description>Chat options configuration and validation</description></item>
///   <item><description>Model discovery and registry</description></item>
///   <item><description>Token estimation for context window management</description></item>
///   <item><description>Options resolution pipeline</description></item>
///   <item><description>Provider-specific parameter mapping</description></item>
/// </list>
/// <para>
/// <b>Module Dependencies:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Lexichord.Abstractions: Core contracts and LLM abstractions</description></item>
/// </list>
/// <para>
/// <b>Services Provided:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="ModelRegistry"/>: Model caching and discovery</description></item>
///   <item><description><see cref="TokenEstimator"/>: Token counting and estimation</description></item>
///   <item><description><see cref="ChatOptionsResolver"/>: Options resolution pipeline</description></item>
///   <item><description><see cref="ProviderAwareChatOptionsValidatorFactory"/>: Provider-specific validation</description></item>
/// </list>
/// </remarks>
public class LLMModule : IModule
{
    /// <inheritdoc />
    public ModuleInfo Info => new(
        Id: "llm",
        Name: "LLM Gateway",
        Version: new Version(0, 6, 1),
        Author: "Lexichord Team",
        Description: "LLM provider abstraction layer with chat options configuration, validation, and model discovery");

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection services)
    {
        // LOGIC: Get configuration from the ServiceCollection's IConfiguration.
        // This follows the pattern used by other modules like RAGModule.
        var configuration = GetConfiguration(services);

        // LOGIC: Register LLM options and bind from configuration.
        services.Configure<LLMOptions>(configuration.GetSection(LLMOptions.SectionName));

        // Register default LLMOptions if no configuration is present.
        services.AddSingleton(sp =>
        {
            var options = sp.GetService<IOptions<LLMOptions>>();
            return options?.Value ?? new LLMOptions();
        });

        // LOGIC: Register the base ChatOptions validator.
        services.AddSingleton<IValidator<ChatOptions>, ChatOptionsValidator>();

        // LOGIC: Register ModelRegistry as singleton for caching across requests.
        // It will use any registered IModelProvider implementations.
        services.AddSingleton<ModelRegistry>();

        // LOGIC: Register TokenEstimator as singleton.
        // Depends on ITokenCounter which should be registered by another module.
        services.AddSingleton<TokenEstimator>();

        // LOGIC: Register ChatOptionsResolver as scoped for per-request resolution.
        services.AddScoped<ChatOptionsResolver>();

        // LOGIC: Register the provider-aware validator factory.
        services.AddSingleton<ProviderAwareChatOptionsValidatorFactory>();
    }

    /// <inheritdoc />
    public async Task InitializeAsync(IServiceProvider provider)
    {
        var logger = provider.GetRequiredService<ILogger<LLMModule>>();

        logger.LogInformation(
            "Initializing {ModuleName} v{Version}",
            Info.Name,
            Info.Version);

        // LOGIC: Log the configured providers.
        var options = provider.GetService<IOptions<LLMOptions>>()?.Value;
        if (options is not null)
        {
            logger.LogDebug(
                "LLM configuration: DefaultProvider={DefaultProvider}, ProviderCount={ProviderCount}",
                options.DefaultProvider,
                options.Providers.Count);

            foreach (var (name, config) in options.Providers)
            {
                if (config.IsConfigured)
                {
                    logger.LogDebug(
                        "Provider '{ProviderName}' configured: BaseUrl={BaseUrl}, DefaultModel={DefaultModel}",
                        name,
                        config.BaseUrl,
                        config.DefaultModel);
                }
            }

            logger.LogDebug(
                "Default chat options: Temperature={Temperature}, MaxTokens={MaxTokens}, TopP={TopP}",
                options.Defaults.Temperature,
                options.Defaults.MaxTokens,
                options.Defaults.TopP);
        }

        // LOGIC: Pre-warm the model registry cache for known providers.
        var modelRegistry = provider.GetService<ModelRegistry>();
        if (modelRegistry is not null)
        {
            try
            {
                // Pre-load known provider model lists
                foreach (var providerName in ModelDefaults.GetAllKnownProviders())
                {
                    _ = await modelRegistry.GetModelsAsync(providerName);
                }

                logger.LogInformation(
                    "Model registry warmed up with {ProviderCount} providers",
                    ModelDefaults.GetAllKnownProviders().Count);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to warm up model registry; will load models on demand");
            }
        }

        logger.LogInformation("{ModuleName} initialized successfully", Info.Name);
    }

    /// <summary>
    /// Gets the IConfiguration from the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The configuration instance.</returns>
    private static IConfiguration GetConfiguration(IServiceCollection services)
    {
        // LOGIC: Look for an existing IConfiguration registration.
        var configDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfiguration));

        if (configDescriptor?.ImplementationInstance is IConfiguration config)
        {
            return config;
        }

        // LOGIC: Build a temporary service provider to resolve IConfiguration.
        using var tempProvider = services.BuildServiceProvider();
        return tempProvider.GetService<IConfiguration>() ?? new ConfigurationBuilder().Build();
    }
}
