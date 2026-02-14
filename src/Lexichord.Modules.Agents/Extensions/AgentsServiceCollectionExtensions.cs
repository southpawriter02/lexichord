// -----------------------------------------------------------------------
// <copyright file="AgentsServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts.LLM;
using MediatR;
using Lexichord.Modules.Agents.Context;
using Lexichord.Modules.Agents.Context.Strategies;
using Lexichord.Modules.Agents.Templates;
using Lexichord.Modules.Agents.Templates.Formatters;
using Lexichord.Modules.Agents.Templates.Providers;
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

    /// <summary>
    /// Adds the context injection services to the service collection with default options.
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
    ///     <see cref="ContextInjectorOptions"/> - Configuration options (via <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>)
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IContextFormatter"/> - Singleton formatter for context output
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IContextProvider"/> implementations - Singleton providers (Document, StyleRules, RAG)
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IContextInjector"/> - Scoped orchestrator for context assembly
    ///   </description></item>
    /// </list>
    /// <para>
    /// <strong>Provider Registration:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="DocumentContextProvider"/> - Priority 50 (foundational)</description></item>
    ///   <item><description><see cref="StyleRulesContextProvider"/> - Priority 100 (enhancing)</description></item>
    ///   <item><description><see cref="RAGContextProvider"/> - Priority 200 (overriding, requires license)</description></item>
    /// </list>
    /// <para>
    /// <strong>Dependencies:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ILicenseContext"/> - For feature gating</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.IStyleEngine"/> - For StyleRulesContextProvider</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ISemanticSearchService"/> - For RAGContextProvider</description></item>
    /// </list>
    /// <para>
    /// <strong>Introduced in:</strong> v0.6.3d as part of the Context Injection Service.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic registration with default options
    /// services.AddContextInjection();
    ///
    /// // Later, resolve via DI
    /// var injector = serviceProvider.GetRequiredService&lt;IContextInjector&gt;();
    /// var context = await injector.AssembleContextAsync(request);
    /// </code>
    /// </example>
    public static IServiceCollection AddContextInjection(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register default options via Options pattern.
        services.AddOptions<ContextInjectorOptions>();

        // LOGIC: Register formatter as singleton.
        // TryAdd to avoid duplicate registrations if called multiple times.
        services.TryAddSingleton<IContextFormatter, DefaultContextFormatter>();

        // LOGIC: Register context providers as singletons.
        // All providers are stateless and thread-safe.
        services.AddSingleton<IContextProvider, DocumentContextProvider>();
        services.AddSingleton<IContextProvider, StyleRulesContextProvider>();
        services.AddSingleton<IContextProvider, RAGContextProvider>();

        // LOGIC: Register the context injector as scoped.
        // Scoped allows per-request isolation while sharing provider instances.
        services.AddScoped<IContextInjector, ContextInjector>();

        return services;
    }

    /// <summary>
    /// Adds the context injection services to the service collection with custom options.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">An action to configure the injector options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This overload allows customization of the injector behavior via
    /// <see cref="ContextInjectorOptions"/>.
    /// </para>
    /// <para>
    /// Available options:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="ContextInjectorOptions.RAGTimeoutMs"/> - Timeout for RAG provider</description></item>
    ///   <item><description><see cref="ContextInjectorOptions.ProviderTimeoutMs"/> - Timeout for other providers</description></item>
    ///   <item><description><see cref="ContextInjectorOptions.MaxStyleRules"/> - Maximum style rules to include</description></item>
    ///   <item><description><see cref="ContextInjectorOptions.MaxChunkLength"/> - Maximum RAG chunk length</description></item>
    ///   <item><description><see cref="ContextInjectorOptions.MinRAGRelevanceScore"/> - Minimum RAG relevance score</description></item>
    /// </list>
    /// <para>
    /// <strong>Introduced in:</strong> v0.6.3d as part of the Context Injection Service.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register with fast timeouts for responsive UI
    /// services.AddContextInjection(options =>
    /// {
    ///     options.RAGTimeoutMs = 2000;
    ///     options.ProviderTimeoutMs = 1000;
    /// });
    ///
    /// // Register with higher relevance threshold
    /// services.AddContextInjection(options =>
    /// {
    ///     options.MinRAGRelevanceScore = 0.7f;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddContextInjection(
        this IServiceCollection services,
        Action<ContextInjectorOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // LOGIC: Register options with configuration action.
        // Note: ContextInjectorOptions is a record, so we use Configure<T>.
        services.Configure(configureOptions);

        // LOGIC: Register formatter as singleton.
        services.TryAddSingleton<IContextFormatter, DefaultContextFormatter>();

        // LOGIC: Register context providers as singletons.
        services.AddSingleton<IContextProvider, DocumentContextProvider>();
        services.AddSingleton<IContextProvider, StyleRulesContextProvider>();
        services.AddSingleton<IContextProvider, RAGContextProvider>();

        // LOGIC: Register the context injector as scoped.
        services.AddScoped<IContextInjector, ContextInjector>();

        return services;
    }

    /// <summary>
    /// Adds the Context Panel ViewModel and related services to the service collection.
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
    ///     <see cref="Chat.ViewModels.ContextPanelViewModel"/> - Transient for UI isolation
    ///   </description></item>
    /// </list>
    /// <para>
    /// The ViewModel is registered as transient because:
    /// </para>
    /// <list type="number">
    ///   <item><description>Each Context Panel instance needs its own ViewModel</description></item>
    ///   <item><description>ViewModels contain per-view state (toggle states, expansion)</description></item>
    ///   <item><description>Disposable pattern requires individual instance management</description></item>
    /// </list>
    /// <para>
    /// <strong>Dependencies:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="IContextInjector"/> - For context assembly</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.IStyleEngine"/> - For style rules</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ISemanticSearchService"/> - For RAG chunks</description></item>
    /// </list>
    /// <para>
    /// <strong>Introduced in:</strong> v0.6.4d as part of the Context Panel feature.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration in AgentsModule
    /// public override void RegisterServices(IServiceCollection services)
    /// {
    ///     services.AddContextPanel();
    /// }
    ///
    /// // Later, resolve via DI
    /// var viewModel = serviceProvider.GetRequiredService&lt;ContextPanelViewModel&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddContextPanel(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register ViewModel as transient for per-instance isolation.
        // Each Context Panel view needs its own ViewModel with independent state.
        services.AddTransient<Chat.ViewModels.ContextPanelViewModel>();

        return services;
    }

    /// <summary>
    /// Registers the Co-pilot Agent services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Lexichord.Abstractions.Agents.IAgent"/> → <see cref="Chat.Agents.CoPilotAgent"/> (Scoped)</description></item>
    /// </list>
    /// <para>
    /// <b>Introduced in:</b> v0.6.6b as the first concrete agent implementation.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddCoPilotAgent(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register CoPilotAgent as scoped so each request scope
        // gets its own agent instance with independent state.
        services.AddScoped<Lexichord.Abstractions.Agents.IAgent, Chat.Agents.CoPilotAgent>();

        return services;
    }

    /// <summary>
    /// Registers the Agent Registry services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Chat.Registry.AgentDefinitionScanner"/> (Singleton) — v0.7.1b</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Agents.IAgentRegistry"/> → <see cref="Chat.Registry.AgentRegistry"/> (Singleton)</description></item>
    /// </list>
    /// <para>
    /// The registry is registered as a singleton to maintain shared caching
    /// and event subscriptions across the application lifetime.
    /// </para>
    /// <para>
    /// <b>Dependencies:</b> Requires <see cref="MediatR.IMediator"/> to be registered
    /// (typically via MediatR package registration).
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.6.6c as part of the Agent Registry feature.
    /// <br/>
    /// <b>Updated in:</b> v0.7.1b to include <see cref="Chat.Registry.AgentDefinitionScanner"/>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddAgentRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: v0.7.1c — Register agent configuration loading services
        // (validator and YAML loader) for built-in and workspace agents.
        services.AddAgentConfigLoading();

        // LOGIC: v0.7.1b — Register AgentDefinitionScanner as singleton for
        // declarative agent registration via [AgentDefinition] attribute.
        services.AddSingleton<Chat.Registry.AgentDefinitionScanner>();

        // LOGIC: Register AgentRegistry as singleton for shared caching
        // and license event subscription across the application lifetime.
        services.AddSingleton<Lexichord.Abstractions.Agents.IAgentRegistry, Chat.Registry.AgentRegistry>();

        return services;
    }

    /// <summary>
    /// Registers the Usage Tracking services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Chat.Services.SessionUsageCoordinator"/> (Singleton)</description></item>
    ///   <item><description><see cref="Chat.Services.UsageTracker"/> (Scoped)</description></item>
    ///   <item><description><see cref="Chat.Persistence.UsageRepository"/> (Scoped)</description></item>
    ///   <item><description><see cref="Chat.ViewModels.UsageDisplayViewModel"/> (Transient)</description></item>
    ///   <item><description><see cref="Chat.Events.Handlers.AgentInvocationHandler"/> (Transient)</description></item>
    /// </list>
    /// <para>
    /// <b>Introduced in:</b> v0.6.6d as part of the Usage Tracking feature.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddUsageTracking(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Singleton — shared session state across all conversations.
        services.AddSingleton<Chat.Services.SessionUsageCoordinator>();

        // LOGIC: Scoped — per-conversation usage accumulation.
        services.AddScoped<Chat.Services.UsageTracker>();

        // LOGIC: Scoped — DbContext-aligned lifetime.
        services.AddScoped<Chat.Persistence.UsageRepository>();

        // LOGIC: Transient — fresh instance per UI binding.
        services.AddTransient<Chat.ViewModels.UsageDisplayViewModel>();

        // LOGIC: Transient — stateless event handler.
        services.AddTransient<MediatR.INotificationHandler<Lexichord.Abstractions.Agents.Events.AgentInvocationEvent>, Chat.Events.Handlers.AgentInvocationHandler>();

        return services;
    }

    /// <summary>
    /// Registers the Agent Configuration Loading services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Configuration.IAgentConfigValidator"/> (Singleton)</description></item>
    ///   <item><description><see cref="Configuration.AgentConfigValidator"/> (Singleton)</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Agents.IAgentConfigLoader"/> (Singleton)</description></item>
    ///   <item><description><see cref="Configuration.YamlAgentConfigLoader"/> (Singleton)</description></item>
    /// </list>
    /// <para>
    /// <strong>Introduced in:</strong> v0.7.1c as part of the Agent Configuration Files feature.
    /// </para>
    /// <para>
    /// <strong>Dependencies:</strong>
    /// <list type="bullet">
    ///   <item><description><c>IFileSystemWatcher</c> (from Workspace module)</description></item>
    ///   <item><description><c>ILicenseContext</c> (from Licensing module)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddAgentConfigLoading(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: v0.7.1c — Register AgentConfigValidator as singleton for reuse
        // across all configuration loading operations.
        services.AddSingleton<Configuration.IAgentConfigValidator, Configuration.AgentConfigValidator>();

        // LOGIC: v0.7.1c — Register YamlAgentConfigLoader as singleton for
        // shared file watching and configuration caching.
        services.AddSingleton<Lexichord.Abstractions.Agents.IAgentConfigLoader, Configuration.YamlAgentConfigLoader>();

        return services;
    }

    /// <summary>
    /// Registers the Agent Selector UI ViewModels.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="ViewModels.AgentSelectorViewModel"/> (Transient) — v0.7.1d</description></item>
    ///   <item><description><see cref="ViewModels.AgentItemViewModel"/> (Transient) — v0.7.1d</description></item>
    ///   <item><description><see cref="ViewModels.PersonaItemViewModel"/> (Transient) — v0.7.1d</description></item>
    /// </list>
    /// <para>
    /// The ViewModels are registered as transient for per-instance isolation,
    /// allowing independent state management for each UI component instance.
    /// </para>
    /// <para>
    /// <strong>Dependencies:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Lexichord.Abstractions.Agents.IAgentRegistry"/> (v0.7.1b) — Agent and persona retrieval</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ISettingsService"/> (v0.1.6a) — Favorites and recents persistence</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ILicenseContext"/> (v0.0.4c) — Tier display and access control</description></item>
    ///   <item><description><c>IMediator</c> (v0.0.7a) — Event subscription and upgrade prompts</description></item>
    /// </list>
    /// <para>
    /// <strong>Introduced in:</strong> v0.7.1d as part of the Agent Selector UI feature.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddAgentSelectorUI(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: v0.7.1d — Register AgentSelectorViewModel as transient for per-instance
        // isolation, allowing independent favorites, recents, and selection state.
        services.AddTransient<ViewModels.AgentSelectorViewModel>();

        // LOGIC: v0.7.1d — Register AgentItemViewModel as transient. While typically
        // instantiated by AgentSelectorViewModel, registration enables DI testing.
        services.AddTransient<ViewModels.AgentItemViewModel>();

        // LOGIC: v0.7.1d — Register PersonaItemViewModel as transient. While typically
        // instantiated by AgentSelectorViewModel, registration enables DI testing.
        services.AddTransient<ViewModels.PersonaItemViewModel>();

        return services;
    }

    /// <summary>
    /// Registers the Context Strategy services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Lexichord.Abstractions.Agents.Context.IContextStrategyFactory"/> → <see cref="Context.ContextStrategyFactory"/> (Singleton)</description></item>
    /// </list>
    /// <para>
    /// The factory is registered as a singleton because it is stateless and manages
    /// strategy instantiation via the DI container. Strategies themselves are created
    /// as transient instances by the factory on-demand.
    /// </para>
    /// <para>
    /// <strong>Dependencies:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ITokenCounter"/> (v0.6.1b) — Required for token estimation in strategies</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ILicenseContext"/> (v0.0.6a) — Required for license tier filtering</description></item>
    /// </list>
    /// <para>
    /// <strong>Note:</strong> In v0.7.2a, no concrete strategies are registered yet.
    /// The factory registration enables the interface layer for testing and integration.
    /// Concrete strategies will be added in v0.7.2b.
    /// </para>
    /// <para>
    /// <strong>Introduced in:</strong> v0.7.2a as part of the Context Strategy Interface.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration in AgentsModule
    /// public override void RegisterServices(IServiceCollection services)
    /// {
    ///     services.AddContextStrategies();
    /// }
    ///
    /// // Later, resolve via DI
    /// var factory = serviceProvider.GetRequiredService&lt;IContextStrategyFactory&gt;();
    /// var strategies = factory.CreateAllStrategies();
    /// </code>
    /// </example>
    public static IServiceCollection AddContextStrategies(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: v0.7.2a — Register ContextStrategyFactory as singleton for
        // application-wide strategy management. The factory is stateless and
        // uses the DI container to create strategy instances on demand.
        services.AddSingleton<Lexichord.Abstractions.Agents.Context.IContextStrategyFactory, Context.ContextStrategyFactory>();

        // NOTE: ITokenCounter is already registered by other modules (v0.6.1b)
        // NOTE: ILicenseContext is already registered by Licensing module (v0.0.6a)

        // LOGIC: v0.7.2b — Register concrete context strategy implementations.
        // Strategies are registered as Transient because:
        //   1. They are lightweight and stateless (no shared mutable state)
        //   2. Each resolution gets fresh logger/service references
        //   3. Factory creates them on demand per context-gathering cycle

        // WriterPro-tier strategies (core writing context)
        services.AddTransient<DocumentContextStrategy>();
        services.AddTransient<SelectionContextStrategy>();
        services.AddTransient<CursorContextStrategy>();
        services.AddTransient<HeadingContextStrategy>();

        // Teams-tier strategies (advanced collaboration features)
        services.AddTransient<RAGContextStrategy>();
        services.AddTransient<StyleContextStrategy>();

        // LOGIC: v0.7.2e — Register knowledge graph context strategy.
        // Bridges the v0.6.6e IKnowledgeContextProvider pipeline into
        // the v0.7.2 Context Assembler strategy interface.
        services.AddTransient<KnowledgeContextStrategy>();

        // LOGIC: v0.7.2c — Register the Context Orchestrator that coordinates
        // all strategies during context assembly. See AddContextOrchestrator().
        services.AddContextOrchestrator();

        // LOGIC: v0.7.2d — Register the Context Preview Panel bridge and ViewModel
        // for real-time display of assembly results. See AddContextPreviewPanel().
        services.AddContextPreviewPanel();

        return services;
    }

    /// <summary>
    /// Registers the Context Orchestrator and its configuration options.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="ContextOptions"/> via <c>AddOptions</c> with sensible defaults</description></item>
    ///   <item><description><see cref="IContextOrchestrator"/> → <see cref="ContextOrchestrator"/> as Singleton</description></item>
    /// </list>
    /// <para>
    /// <strong>Singleton Lifetime:</strong>
    /// The orchestrator is registered as a singleton because it maintains per-strategy
    /// enabled/disabled state in a <c>ConcurrentDictionary</c>. This state persists
    /// for the application lifetime and is shared across all context assembly requests.
    /// </para>
    /// <para>
    /// <strong>Configuration:</strong>
    /// <see cref="ContextOptions"/> uses <c>AddOptions</c> with defaults rather than
    /// configuration binding, because <c>AgentsModule.RegisterServices()</c> does not
    /// receive <c>IConfiguration</c>. Consumers can override defaults by binding
    /// <c>IConfiguration.GetSection("Context")</c> to <c>ContextOptions</c> in the host.
    /// </para>
    /// <para>
    /// <strong>Introduced in:</strong> v0.7.2c as part of the Context Orchestrator.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Called automatically from AddContextStrategies()
    /// services.AddContextStrategies(); // includes AddContextOrchestrator()
    ///
    /// // Or called directly for testing
    /// services.AddContextOrchestrator();
    /// </code>
    /// </example>
    public static IServiceCollection AddContextOrchestrator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: v0.7.2c — Register ContextOptions with defaults.
        // Uses AddOptions<T>() pattern (matching PerformanceOptions) since
        // AgentsModule.RegisterServices() does not receive IConfiguration.
        services.AddOptions<ContextOptions>();

        // LOGIC: v0.7.2c — Register ContextOrchestrator as Singleton.
        // Singleton is required to maintain strategy enabled/disabled state
        // in the ConcurrentDictionary across requests.
        services.AddSingleton<IContextOrchestrator, ContextOrchestrator>();

        return services;
    }

    /// <summary>
    /// Registers the Context Preview Panel bridge and ViewModel for
    /// real-time display of assembled context fragments.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="ContextPreviewBridge"/> as Singleton — MediatR notification handler bridge</description></item>
    ///   <item><description><see cref="INotificationHandler{T}"/> for <see cref="ContextAssembledEvent"/> — forwards to bridge</description></item>
    ///   <item><description><see cref="INotificationHandler{T}"/> for <see cref="StrategyToggleEvent"/> — forwards to bridge</description></item>
    ///   <item><description><see cref="Chat.ViewModels.ContextPreviewViewModel"/> as Transient — per-panel instance</description></item>
    /// </list>
    /// <para>
    /// <strong>Singleton Bridge Pattern:</strong>
    /// The bridge is a Singleton that implements <see cref="INotificationHandler{T}"/>
    /// for both event types. It re-publishes notifications as C# events that transient
    /// ViewModels subscribe to in their constructors. This solves the MediatR-to-ViewModel
    /// lifetime mismatch where MediatR would create new handler instances per notification.
    /// </para>
    /// <para>
    /// <strong>Introduced in:</strong> v0.7.2d as part of the Context Preview Panel.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Called automatically from AddContextStrategies()
    /// services.AddContextStrategies(); // includes AddContextPreviewPanel()
    ///
    /// // Or called directly for testing
    /// services.AddContextPreviewPanel();
    /// </code>
    /// </example>
    public static IServiceCollection AddContextPreviewPanel(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: v0.7.2d — Register ContextPreviewBridge as Singleton.
        // The bridge must be a singleton so the same instance receives MediatR
        // notifications and forwards them to subscribed ViewModels.
        services.AddSingleton<ContextPreviewBridge>();

        // LOGIC: v0.7.2d — Register the bridge as INotificationHandler for both events.
        // MediatR resolves handlers by interface, so we forward to the same singleton instance.
        services.AddSingleton<INotificationHandler<ContextAssembledEvent>>(
            sp => sp.GetRequiredService<ContextPreviewBridge>());
        services.AddSingleton<INotificationHandler<StrategyToggleEvent>>(
            sp => sp.GetRequiredService<ContextPreviewBridge>());

        // LOGIC: v0.7.2d — Register ContextPreviewViewModel as Transient.
        // Each Context Preview panel view needs its own ViewModel with independent state.
        services.AddTransient<Chat.ViewModels.ContextPreviewViewModel>();

        return services;
    }
}


