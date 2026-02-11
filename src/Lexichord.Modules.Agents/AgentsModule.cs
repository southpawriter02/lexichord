// -----------------------------------------------------------------------
// <copyright file="AgentsModule.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Chat.Abstractions;
using Lexichord.Modules.Agents.Chat.Services;
using Lexichord.Modules.Agents.Commands;
using Lexichord.Modules.Agents.Configuration;
using Lexichord.Modules.Agents.Extensions;
using Lexichord.Modules.Agents.Performance;
using Lexichord.Modules.Agents.Services;
using Lexichord.Modules.Agents.Templates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System.Text;

namespace Lexichord.Modules.Agents;

/// <summary>
/// The Agents module provides AI agent orchestration and prompt templating capabilities.
/// </summary>
/// <remarks>
/// <para>
/// This module serves as the foundation for AI agent interactions in Lexichord,
/// providing the following capabilities:
/// </para>
/// <list type="bullet">
///   <item><description><strong>v0.6.3b:</strong> Mustache-based prompt template rendering via <see cref="MustachePromptRenderer"/></description></item>
///   <item><description><strong>v0.6.3c:</strong> Template repository for built-in and custom prompts via <see cref="PromptTemplateRepository"/></description></item>
///   <item><description><strong>v0.6.3d:</strong> Context injection from style rules and RAG via <see cref="ContextInjector"/></description></item>
/// </list>
/// <para>
/// The module is part of the v0.6.x "Conductors" release series, which transforms
/// Lexichord from a smart search tool into an intelligent writing assistant with
/// LLM-powered features.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // The module is automatically loaded by the ModuleLoader from the Modules directory.
/// // Services can then be resolved via DI:
///
/// var renderer = serviceProvider.GetRequiredService&lt;IPromptRenderer&gt;();
///
/// var template = PromptTemplate.Create(
///     templateId: "assistant",
///     name: "Assistant",
///     systemPrompt: "You are a helpful writing assistant.",
///     userPrompt: "{{user_input}}",
///     requiredVariables: ["user_input"]
/// );
///
/// var messages = renderer.RenderMessages(template, new Dictionary&lt;string, object&gt;
/// {
///     ["user_input"] = "Help me improve this paragraph."
/// });
/// </code>
/// </example>
/// <seealso cref="IModule"/>
/// <seealso cref="MustachePromptRenderer"/>
/// <seealso cref="IPromptRenderer"/>
public class AgentsModule : IModule
{
    /// <inheritdoc />
    /// <remarks>
    /// Module metadata for the Agents module:
    /// <list type="bullet">
    ///   <item><description><strong>Id:</strong> "agents"</description></item>
    ///   <item><description><strong>Name:</strong> "Agents"</description></item>
    ///   <item><description><strong>Version:</strong> 0.6.5 (The Stream)</description></item>
    ///   <item><description><strong>Author:</strong> Lexichord Team</description></item>
    /// </list>
    /// </remarks>
    public ModuleInfo Info => new(
        Id: "agents",
        Name: "Agents",
        Version: new Version(0, 6, 8),
        Author: "Lexichord Team",
        Description: "AI agent orchestration with streaming, prompt templating, conversation management, agent registry, selection context, and performance optimization");

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="IPromptRenderer"/> → <see cref="MustachePromptRenderer"/> (Singleton) - v0.6.3b
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="MustacheRendererOptions"/> via IOptions pattern - v0.6.3b
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IPromptTemplateRepository"/> → <see cref="PromptTemplateRepository"/> (Singleton) - v0.6.3c
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="PromptTemplateOptions"/> via IOptions pattern - v0.6.3c
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IContextInjector"/> → <see cref="ContextInjector"/> (Scoped) - v0.6.3d
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="ContextInjectorOptions"/> via IOptions pattern - v0.6.3d
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="ISSEParser"/> → <see cref="SSEParser"/> (Singleton) - v0.6.5b
    ///   </description></item>
    /// </list>
    /// </remarks>
    public void RegisterServices(IServiceCollection services)
    {
        // LOGIC: Register Mustache prompt renderer services (v0.6.3b).
        // The renderer is registered as a singleton for thread-safe reuse.
        services.AddMustacheRenderer();

        // LOGIC: Register template repository services (v0.6.3c).
        // The repository manages built-in and custom templates with hot-reload support.
        services.AddTemplateRepository();

        // LOGIC: Register context injection services (v0.6.3d).
        // The injector orchestrates context assembly from multiple providers.
        services.AddContextInjection();

        // LOGIC: Register conversation management services (v0.6.4c).
        // The conversation manager handles chat history and lifecycle.
        services.AddConversationManagement();

        // LOGIC: Register context panel services (v0.6.4d).
        // The context panel ViewModel manages UI state for context source display.
        services.AddContextPanel();

        // LOGIC: Register SSE parser service (v0.6.5b).
        // The parser is stateless and thread-safe, suitable for singleton registration.
        services.AddSingleton<ISSEParser, SSEParser>();

        // LOGIC: Register CoPilot Agent (v0.6.6b).
        // Scoped lifetime ensures per-request isolation for agent state.
        services.AddCoPilotAgent();

        // LOGIC: Register Agent Registry (v0.6.6c).
        // Singleton lifetime ensures shared caching and event handling.
        services.AddAgentRegistry();

        // LOGIC: Register Usage Tracking (v0.6.6d).
        // Tracks per-conversation and session-level agent usage metrics.
        services.AddUsageTracking();

        // LOGIC: Register Selection Context services (v0.6.7a).
        // Coordinates sending editor selection to Co-pilot chat.
        services.AddSingleton<ISelectionContextService, SelectionContextService>();
        services.AddTransient<SelectionContextCommand>();
        services.AddSingleton<IKeyBindingConfiguration, SelectionContextKeyBindings>();

        // LOGIC: Register Inline Suggestions services (v0.6.7b).
        // EditorInsertionService is singleton to maintain preview state across the application.
        // Commands and ViewModel are transient as they are created per-usage.
        services.AddSingleton<IEditorInsertionService, EditorInsertionService>();
        services.AddTransient<InsertAtCursorCommand>();
        services.AddTransient<ReplaceSelectionCommand>();
        services.AddTransient<ViewModels.PreviewOverlayViewModel>();

        // LOGIC: Register Document-Aware Prompting services (v0.6.7c).
        // ASTCacheProvider is singleton to maintain a shared Markdown AST cache across
        // the application, avoiding redundant re-parsing of unchanged documents.
        // DocumentContextAnalyzer is singleton to maintain its DocumentChanged subscription.
        // ContextAwarePromptSelector is singleton as it is stateless and thread-safe.
        services.AddSingleton<ASTCacheProvider>();
        services.AddSingleton<IDocumentContextAnalyzer, DocumentContextAnalyzer>();
        services.AddSingleton<ContextAwarePromptSelector>();

        // LOGIC: Register Quick Actions services (v0.6.7d).
        // QuickActionsService is singleton to maintain the action registry across
        // the application lifetime. Built-in actions are loaded at construction.
        // ViewModel is transient as it is created per-usage by the panel host.
        services.AddSingleton<IQuickActionsService, QuickActionsService>();
        services.AddTransient<ViewModels.QuickActionsPanelViewModel>();

        // LOGIC: Register Performance Optimization services (v0.6.8c).
        // PerformanceOptions are configured via IOptions pattern for appsettings binding.
        services.Configure<PerformanceOptions>(_ => { });

        // LOGIC: ConversationMemoryManager is singleton to maintain memory tracking state
        // across the application lifetime.
        services.AddSingleton<IConversationMemoryManager, ConversationMemoryManager>();

        // LOGIC: RequestCoalescer is singleton since it manages a background processing
        // loop and pending batch state that must persist across requests.
        services.AddSingleton<IRequestCoalescer, RequestCoalescer>();

        // LOGIC: CachedContextAssembler wraps IContextInjector with caching. Uses a factory
        // to resolve the inner IContextInjector from the scoped service provider.
        services.AddSingleton<ICachedContextAssembler>(sp =>
        {
            // NOTE: IContextInjector is scoped, but we only need it as a delegate target.
            // We create a scope to resolve it for the singleton wrapper.
            var scope = sp.CreateScope();
            var inner = scope.ServiceProvider.GetRequiredService<IContextInjector>();
            var logger = sp.GetRequiredService<ILogger<CachedContextAssembler>>();
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PerformanceOptions>>();
            return new CachedContextAssembler(inner, logger, options);
        });

        // LOGIC: Object pooling for StringBuilder reuse across template rendering
        // and context assembly operations, reducing GC pressure.
        services.AddSingleton<ObjectPool<StringBuilder>>(sp =>
            new DefaultObjectPoolProvider().CreateStringBuilderPool());
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Performs post-DI-build initialization:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Verifies the prompt renderer is available</description></item>
    ///   <item><description>Initializes the template repository and loads templates</description></item>
    ///   <item><description>Logs module initialization status</description></item>
    /// </list>
    /// </remarks>
    public async Task InitializeAsync(IServiceProvider provider)
    {
        var logger = provider.GetRequiredService<ILogger<AgentsModule>>();

        logger.LogInformation(
            "Initializing {ModuleName} module v{Version}",
            Info.Name,
            Info.Version);

        // LOGIC: Verify the prompt renderer is available (v0.6.3b).
        var renderer = provider.GetService<IPromptRenderer>();
        if (renderer is not null)
        {
            logger.LogDebug(
                "Prompt renderer registered successfully: {RendererType}",
                renderer.GetType().Name);
        }
        else
        {
            logger.LogWarning("Prompt renderer is not registered. Template rendering will not be available.");
        }

        // LOGIC: Initialize the template repository (v0.6.3c).
        // This loads embedded templates and sets up hot-reload if licensed.
        var repository = provider.GetService<IPromptTemplateRepository>();
        if (repository is PromptTemplateRepository repoImpl)
        {
            await repoImpl.InitializeAsync();

            var templates = repository.GetAllTemplates();
            logger.LogDebug(
                "Template repository initialized with {Count} templates",
                templates.Count);
        }
        else if (repository is not null)
        {
            logger.LogDebug(
                "Template repository registered: {RepositoryType}",
                repository.GetType().Name);
        }
        else
        {
            logger.LogWarning("Template repository is not registered. Template management will not be available.");
        }

        // LOGIC: Verify context injector is available (v0.6.3d).
        // Note: IContextInjector is scoped, so we create a scope to verify registration.
        using (var scope = provider.CreateScope())
        {
            var injector = scope.ServiceProvider.GetService<IContextInjector>();
            if (injector is not null)
            {
                logger.LogDebug(
                    "Context injector registered successfully: {InjectorType}",
                    injector.GetType().Name);
            }
            else
            {
                logger.LogWarning("Context injector is not registered. Context injection will not be available.");
            }
        }

        // LOGIC: Verify SSE parser is available (v0.6.5b).
        var sseParser = provider.GetService<ISSEParser>();
        if (sseParser is not null)
        {
            logger.LogDebug(
                "SSE parser registered successfully: {ParserType}",
                sseParser.GetType().Name);
        }
        else
        {
            logger.LogWarning("SSE parser is not registered. Streaming response parsing will not be available.");
        }

        logger.LogInformation(
            "{ModuleName} module initialized successfully",
            Info.Name);

        // LOGIC: Verify agent registry is available (v0.6.6c).
        var registry = provider.GetService<IAgentRegistry>();
        if (registry is not null)
        {
            logger.LogDebug(
                "Agent registry available with {AgentCount} agents",
                registry.AvailableAgents.Count);
        }
        else
        {
            logger.LogWarning("Agent registry is not registered. Agent discovery will not be available.");
        }

        // LOGIC: Verify performance optimization services are available (v0.6.8c).
        var memoryManager = provider.GetService<IConversationMemoryManager>();
        if (memoryManager is not null)
        {
            logger.LogDebug(
                "Conversation memory manager available: {ManagerType}",
                memoryManager.GetType().Name);
        }
        else
        {
            logger.LogWarning("Conversation memory manager is not registered. Memory limits will not be enforced.");
        }

        var cachedAssembler = provider.GetService<ICachedContextAssembler>();
        if (cachedAssembler is not null)
        {
            logger.LogDebug(
                "Cached context assembler available: {AssemblerType}",
                cachedAssembler.GetType().Name);
        }
        else
        {
            logger.LogWarning("Cached context assembler is not registered. Context caching will not be available.");
        }
    }
}
