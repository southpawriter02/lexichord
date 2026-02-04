// -----------------------------------------------------------------------
// <copyright file="AgentsServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lexichord.Modules.Agents.Extensions;

/// <summary>
/// Extension methods for registering Agents module services with the dependency injection container.
/// </summary>
/// <remarks>
/// <para>
/// This class provides extension methods for <see cref="IServiceCollection"/> to register
/// the prompt rendering services introduced in v0.6.3b.
/// </para>
/// <para>
/// Available registration methods:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="AddMustacheRenderer(IServiceCollection)"/> - Default options</description></item>
///   <item><description><see cref="AddMustacheRenderer(IServiceCollection, Action{MustacheRendererOptions})"/> - Custom options</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Using default options in AgentsModule
/// public override void RegisterServices(IServiceCollection services)
/// {
///     services.AddMustacheRenderer();
/// }
///
/// // Using custom options
/// services.AddMustacheRenderer(options =>
/// {
///     options.IgnoreCaseOnKeyLookup = false;
///     options.ThrowOnMissingVariables = true;
/// });
/// </code>
/// </example>
public static class AgentsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Mustache prompt renderer to the service collection with default options.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="MustacheRendererOptions"/> - Configuration options (via <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>)
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="MustachePromptRenderer"/> as <see cref="IPromptRenderer"/> - Singleton for thread-safe reuse
    ///   </description></item>
    /// </list>
    /// <para>
    /// The renderer is registered as a singleton because:
    /// </para>
    /// <list type="number">
    ///   <item><description>The underlying Stubble renderer is thread-safe</description></item>
    ///   <item><description>Configuration is immutable after construction</description></item>
    ///   <item><description>No per-request state is maintained</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic registration with default options
    /// services.AddMustacheRenderer();
    ///
    /// // Later, resolve via DI
    /// var renderer = serviceProvider.GetRequiredService&lt;IPromptRenderer&gt;();
    /// var result = renderer.Render("Hello, {{name}}!", variables);
    /// </code>
    /// </example>
    public static IServiceCollection AddMustacheRenderer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register default options via Options pattern.
        services.AddOptions<MustacheRendererOptions>();

        // LOGIC: Register renderer as singleton for thread-safe reuse.
        // The Stubble renderer is thread-safe and should be reused across requests.
        services.AddSingleton<IPromptRenderer, MustachePromptRenderer>();

        return services;
    }

    /// <summary>
    /// Adds the Mustache prompt renderer to the service collection with custom options.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">An action to configure the renderer options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This overload allows customization of the renderer behavior via
    /// <see cref="MustacheRendererOptions"/>.
    /// </para>
    /// <para>
    /// Available options:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="MustacheRendererOptions.IgnoreCaseOnKeyLookup"/> - Case sensitivity for variable names</description></item>
    ///   <item><description><see cref="MustacheRendererOptions.ThrowOnMissingVariables"/> - Validation behavior</description></item>
    ///   <item><description><see cref="MustacheRendererOptions.FastRenderThresholdMs"/> - Performance threshold</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register with strict mode (case-sensitive, throws on missing)
    /// services.AddMustacheRenderer(options =>
    /// {
    ///     options.IgnoreCaseOnKeyLookup = false;
    ///     options.ThrowOnMissingVariables = true;
    /// });
    ///
    /// // Register with lenient mode (case-insensitive, no throw)
    /// services.AddMustacheRenderer(options =>
    /// {
    ///     options.IgnoreCaseOnKeyLookup = true;
    ///     options.ThrowOnMissingVariables = false;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMustacheRenderer(
        this IServiceCollection services,
        Action<MustacheRendererOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // LOGIC: Create a mutable options instance and apply configuration.
        var options = MustacheRendererOptions.Default;
        configureOptions(options);

        // LOGIC: Register the configured options via Options pattern.
        // Note: We need to use Configure with a lambda that creates a new instance
        // since MustacheRendererOptions is a record (immutable).
        services.Configure<MustacheRendererOptions>(opt =>
        {
            // The configure delegate receives the configured options
            // Since records are immutable, we configure via the registration
        });

        // LOGIC: Register with a factory that uses the configured options.
        services.AddSingleton<IPromptRenderer>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MustachePromptRenderer>>();
            return new MustachePromptRenderer(
                logger,
                Microsoft.Extensions.Options.Options.Create(options));
        });

        return services;
    }

    /// <summary>
    /// Adds the prompt template repository to the service collection with default options.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="PromptTemplateOptions"/> - Configuration options (via <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>)
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="PromptTemplateRepository"/> as <see cref="IPromptTemplateRepository"/> - Singleton for application-wide template management
    ///   </description></item>
    /// </list>
    /// <para>
    /// The repository is registered as a singleton because:
    /// </para>
    /// <list type="number">
    ///   <item><description>Templates should be cached application-wide</description></item>
    ///   <item><description>Hot-reload requires a single watcher instance</description></item>
    ///   <item><description>The repository is thread-safe</description></item>
    /// </list>
    /// <para>
    /// <strong>Dependencies:</strong>
    /// The repository requires <see cref="Lexichord.Abstractions.Contracts.ILicenseContext"/> to be registered.
    /// Optionally uses <see cref="Lexichord.Abstractions.Contracts.IFileSystemWatcher"/> for hot-reload.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic registration with default options
    /// services.AddTemplateRepository();
    ///
    /// // Later, resolve via DI
    /// var repository = serviceProvider.GetRequiredService&lt;IPromptTemplateRepository&gt;();
    /// var template = repository.GetTemplate("co-pilot-editor");
    /// </code>
    /// </example>
    public static IServiceCollection AddTemplateRepository(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register default options via Options pattern.
        services.AddOptions<PromptTemplateOptions>();

        // LOGIC: Register repository as singleton for application-wide template management.
        // TryAdd to avoid duplicate registrations if called multiple times.
        services.TryAddSingleton<IPromptTemplateRepository, PromptTemplateRepository>();

        return services;
    }

    /// <summary>
    /// Adds the prompt template repository to the service collection with custom options.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">An action to configure the repository options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This overload allows customization of the repository behavior via
    /// <see cref="PromptTemplateOptions"/>.
    /// </para>
    /// <para>
    /// Available options:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="PromptTemplateOptions.EnableBuiltInTemplates"/> - Whether to load embedded templates</description></item>
    ///   <item><description><see cref="PromptTemplateOptions.EnableHotReload"/> - Whether to enable hot-reload</description></item>
    ///   <item><description><see cref="PromptTemplateOptions.GlobalTemplatesPath"/> - Path to global templates</description></item>
    ///   <item><description><see cref="PromptTemplateOptions.UserTemplatesPath"/> - Path to user templates</description></item>
    ///   <item><description><see cref="PromptTemplateOptions.FileWatcherDebounceMs"/> - Debounce delay for file changes</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register with custom paths
    /// services.AddTemplateRepository(options =>
    /// {
    ///     options.UserTemplatesPath = Path.Combine(
    ///         Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    ///         "LexichordTemplates");
    ///     options.EnableHotReload = true;
    ///     options.FileWatcherDebounceMs = 500;
    /// });
    ///
    /// // Register without built-in templates (testing)
    /// services.AddTemplateRepository(options =>
    /// {
    ///     options.EnableBuiltInTemplates = false;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddTemplateRepository(
        this IServiceCollection services,
        Action<PromptTemplateOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // LOGIC: Register options with configuration action.
        services.Configure(configureOptions);

        // LOGIC: Register repository as singleton.
        services.TryAddSingleton<IPromptTemplateRepository, PromptTemplateRepository>();

        return services;
    }
}
