// -----------------------------------------------------------------------
// <copyright file="AgentsModule.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
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
        Version: new Version(0, 7, 5),
        Author: "Lexichord Team",
        Description: "AI agent orchestration with streaming, prompt templating, conversation management, agent registry, selection context, performance optimization, editor agent context menu, undo/redo integration, readability target service, and style deviation scanning");

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

        // ── v0.6.8d: Error Handling & Recovery ──────────────────────────
        // LOGIC: Register error recovery coordination service as singleton.
        // ErrorRecoveryService is stateless — it maps exception types to strategies
        // using static lookup tables and delegates truncation to TokenBudgetManager.
        services.AddSingleton<Resilience.IErrorRecoveryService, Resilience.ErrorRecoveryService>();

        // LOGIC: Register token budget manager as singleton.
        // TokenBudgetManager is stateless and thread-safe — all state (messages,
        // budget) is passed as parameters to its methods.
        services.AddSingleton<Resilience.ITokenBudgetManager, Resilience.TokenBudgetManager>();

        // LOGIC: Register rate limit queue as singleton.
        // The queue maintains a long-running background processing loop and
        // must persist across the application lifetime to track rate limit windows.
        services.AddSingleton<Resilience.IRateLimitQueue, Resilience.RateLimitQueue>();

        // LOGIC: Register ResilientChatService as the primary non-keyed IChatCompletionService.
        // This decorator wraps the default provider (resolved from the LLMProviderRegistry)
        // with Polly resilience policies (retry, circuit breaker, timeout).
        // CoPilotAgent and CoPilotViewModel inject IChatCompletionService and will
        // receive this resilient wrapper transparently.
        services.AddSingleton<IChatCompletionService>(sp =>
        {
            // Resolve the default provider via the LLM provider registry
            var registry = sp.GetRequiredService<ILLMProviderRegistry>();
            var inner = registry.GetDefaultProvider();
            var recovery = sp.GetRequiredService<Resilience.IErrorRecoveryService>();
            var rateLimitQueue = sp.GetRequiredService<Resilience.IRateLimitQueue>();
            var logger = sp.GetRequiredService<ILogger<Resilience.ResilientChatService>>();
            var mediator = sp.GetRequiredService<MediatR.IMediator>();
            return new Resilience.ResilientChatService(inner, recovery, rateLimitQueue, logger, mediator);
        });

        // ── v0.7.2: Context Assembler ───────────────────────────────────────
        // LOGIC: Register the full Context Assembler pipeline:
        //   v0.7.2a — Strategy abstraction layer (IContextStrategyFactory)
        //   v0.7.2b — Concrete strategy implementations (Document, Selection, etc.)
        //   v0.7.2c — Context Orchestrator (parallel execution, dedup, budget)
        //   v0.7.2d — Context Preview Panel (bridge, ViewModels)
        services.AddContextStrategies();

        // ── v0.7.3: Editor Agent ────────────────────────────────────────────
        // LOGIC: Register the Editor Agent context menu services:
        //   v0.7.3a — Context menu provider, ViewModel, keyboard shortcuts
        //   - IEditorAgentContextMenuProvider: Singleton for selection state tracking
        //   - RewriteCommandViewModel: Transient for per-view command state
        //   - RewriteKeyboardShortcuts: Singleton IKeyBindingConfiguration
        services.AddEditorAgentContextMenu();

        // LOGIC: Register the Editor Agent command pipeline services:
        //   v0.7.3b — EditorAgent, RewriteCommandHandler
        //   - IEditorAgent (+ IAgent): Singleton for stateless rewrite operations
        //   - IRewriteCommandHandler: Scoped for per-request execution isolation
        //   - RewriteRequestedEventHandler: auto-registered via MediatR assembly scanning
        //   NOTE: IRewriteApplicator is NOT registered — provided by v0.7.3d
        services.AddEditorAgentPipeline();

        // LOGIC: Register the Editor Agent context strategy services:
        //   v0.7.3c — Context-Aware Rewriting strategies
        //   - SurroundingTextContextStrategy: Transient, gathers surrounding paragraphs
        //   - EditorTerminologyContextStrategy: Transient, scans terminology matches
        //   Both are registered in ContextStrategyFactory as WriterPro-tier strategies.
        services.AddEditorAgentContextStrategies();

        // LOGIC: Register the Editor Agent undo/redo integration services:
        //   v0.7.3d — Undo/Redo Integration
        //   - IRewriteApplicator → RewriteApplicator: Scoped for per-operation preview state
        //   - IUndoRedoService is NOT registered (nullable in RewriteApplicator)
        //   NOTE: This enables RewriteCommandHandler to delegate document application
        //   to the applicator (was previously skipped when applicator was null).
        services.AddEditorAgentUndoIntegration();

        // ── v0.7.4: Simplifier Agent ───────────────────────────────────────
        // LOGIC: Register the Simplifier Agent readability target service:
        //   v0.7.4a — Readability Target Service
        //   - IReadabilityTargetService → ReadabilityTargetService: Singleton for preset management
        //   - Built-in presets: General Public, Technical, Executive, International/ESL
        //   - Custom preset CRUD (WriterPro/Teams tier) with ISettingsService persistence
        //   - Target resolution from Voice Profile, presets, or explicit parameters
        //   - Target validation against source text readability metrics
        services.AddReadabilityTargetService();

        // LOGIC: Register the Simplifier Agent pipeline services:
        //   v0.7.4b — Simplification Pipeline
        //   - ISimplificationResponseParser → SimplificationResponseParser: Singleton for response parsing
        //   - ISimplificationPipeline → SimplifierAgent: Singleton for stateless simplification
        //   - IAgent (forwarded): Enables agent discovery via IAgentRegistry
        //   - Prompt template: specialist-simplifier.yaml with structured output format
        services.AddSimplifierAgentPipeline();

        // LOGIC: Register the Simplifier Agent Preview/Diff UI services:
        //   v0.7.4c — Preview/Diff UI
        //   - SimplificationPreviewViewModel: Transient for isolated preview instances
        //   - Enables before/after comparison, selective acceptance, re-simplification
        services.AddSimplifierPreviewUI();

        // ── v0.7.4d: Batch Simplification ────────────────────────────────────
        // LOGIC: Register batch simplification services:
        //   - IBatchSimplificationService → BatchSimplificationService: Singleton
        //     Orchestrates paragraph-by-paragraph simplification with skip detection
        //   - BatchProgressViewModel: Transient for per-operation progress tracking
        //   - BatchCompletionViewModel: Transient for per-operation completion summary
        services.AddBatchSimplificationService();

        // ── v0.7.5: Tuning Agent ─────────────────────────────────────────────
        // LOGIC: Register the Style Deviation Scanner services:
        //   v0.7.5a — Style Deviation Scanner
        //   - IStyleDeviationScanner → StyleDeviationScanner: Singleton
        //     Bridges linting infrastructure with AI fix generation
        //   - Subscribes to LintingCompletedEvent and StyleSheetReloadedEvent
        //   - Result caching via IMemoryCache with content hash validation
        //   - Real-time deviation detection via MediatR event handlers
        services.AddStyleDeviationScanner();

        // LOGIC: Register the Fix Suggestion Generator services:
        //   v0.7.5b — Automatic Fix Suggestions
        //   - IFixSuggestionGenerator → FixSuggestionGenerator: Singleton
        //     Generates AI-powered fix suggestions for style deviations
        //   - DiffGenerator: Internal helper for text diff generation
        //   - FixValidator: Internal helper for fix validation via re-linting
        //   - Uses tuning-agent-fix.yaml prompt template
        //   - Batch processing with SemaphoreSlim parallelism control
        services.AddFixSuggestionGenerator();

        // LOGIC: Register the Tuning Review UI services:
        //   v0.7.5c — Accept/Reject UI
        //   - TuningPanelViewModel: Transient for per-instance isolation
        //     Orchestrates suggestion review with accept/reject/modify/skip actions
        //   - SuggestionCardViewModel: Not DI-registered (created by TuningPanelViewModel)
        //   - TuningUndoableOperation: Not DI-registered (created per-accept)
        services.AddTuningReviewUI();

        // LOGIC: Register the Learning Loop services:
        //   v0.7.5d — Learning Loop
        //   - IFeedbackStore → SqliteFeedbackStore: Singleton for SQLite persistence
        //   - PatternAnalyzer: Singleton for pattern extraction and prompt enhancement
        //   - ILearningLoopService → LearningLoopService: Singleton
        //     Also registered as INotificationHandler<SuggestionAcceptedEvent> and
        //     INotificationHandler<SuggestionRejectedEvent> via factory forwarding
        //   - Captures user accept/reject/modify decisions for continuous improvement
        //   - Generates learning context to enhance fix generation prompts
        services.AddLearningLoop();
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

        // LOGIC: Verify Editor Agent context menu provider is available (v0.7.3a).
        var editorAgentProvider = provider.GetService<Editor.IEditorAgentContextMenuProvider>();
        if (editorAgentProvider is not null)
        {
            var menuItems = editorAgentProvider.GetRewriteMenuItems();
            logger.LogDebug(
                "Editor Agent context menu provider available with {MenuItemCount} rewrite options",
                menuItems.Count);
        }
        else
        {
            logger.LogWarning("Editor Agent context menu provider is not registered. AI rewrite features will not be available.");
        }

        // LOGIC: Verify Editor Agent pipeline is available (v0.7.3b).
        var editorAgent = provider.GetService<Editor.IEditorAgent>();
        if (editorAgent is not null)
        {
            logger.LogDebug(
                "Editor Agent pipeline available: AgentId={AgentId}, Capabilities={Capabilities}",
                editorAgent.AgentId,
                editorAgent.Capabilities);
        }
        else
        {
            logger.LogWarning("Editor Agent is not registered. AI rewrite pipeline will not be available.");
        }

        // LOGIC: Verify Editor Agent context strategies are available (v0.7.3c).
        var strategyFactory = provider.GetService<IContextStrategyFactory>();
        if (strategyFactory is not null)
        {
            var availableIds = strategyFactory.AvailableStrategyIds;
            var hasSurrounding = availableIds.Contains("surrounding-text");
            var hasTerminology = availableIds.Contains("terminology");

            if (hasSurrounding && hasTerminology)
            {
                logger.LogDebug(
                    "Editor Agent context strategies available: surrounding-text, terminology");
            }
            else
            {
                logger.LogWarning(
                    "Editor Agent context strategies partially available: surrounding-text={HasSurrounding}, terminology={HasTerminology}",
                    hasSurrounding, hasTerminology);
            }
        }

        // LOGIC: Verify Editor Agent undo/redo integration is available (v0.7.3d).
        // IRewriteApplicator is scoped, so we create a scope to verify registration.
        using (var scope = provider.CreateScope())
        {
            var applicator = scope.ServiceProvider.GetService<Editor.IRewriteApplicator>();
            if (applicator is not null)
            {
                logger.LogDebug(
                    "Editor Agent rewrite applicator available: {ApplicatorType}",
                    applicator.GetType().Name);
            }
            else
            {
                logger.LogWarning("Editor Agent rewrite applicator is not registered. Rewrite undo/redo integration will not be available.");
            }
        }

        // LOGIC: Verify Readability Target Service is available (v0.7.4a).
        var readabilityTargetService = provider.GetService<IReadabilityTargetService>();
        if (readabilityTargetService is not null)
        {
            var presets = await readabilityTargetService.GetAllPresetsAsync();
            var builtInCount = presets.Count(p => p.IsBuiltIn);
            var customCount = presets.Count(p => !p.IsBuiltIn);

            logger.LogDebug(
                "Readability Target Service available with {BuiltInCount} built-in presets and {CustomCount} custom presets",
                builtInCount,
                customCount);
        }
        else
        {
            logger.LogWarning("Readability Target Service is not registered. Simplifier Agent features will not be available.");
        }

        // LOGIC: Verify Simplifier Agent pipeline is available (v0.7.4b).
        var simplifierPipeline = provider.GetService<ISimplificationPipeline>();
        if (simplifierPipeline is not null)
        {
            // LOGIC: Verify the agent is also registered as IAgent for discovery
            var simplifierAsAgent = simplifierPipeline as IAgent;
            if (simplifierAsAgent is not null)
            {
                logger.LogDebug(
                    "Simplifier Agent pipeline available: AgentId={AgentId}, Capabilities={Capabilities}",
                    simplifierAsAgent.AgentId,
                    simplifierAsAgent.Capabilities);
            }
            else
            {
                logger.LogDebug(
                    "Simplifier Agent pipeline available: {PipelineType}",
                    simplifierPipeline.GetType().Name);
            }
        }
        else
        {
            logger.LogWarning("Simplifier Agent pipeline is not registered. Text simplification features will not be available.");
        }

        // LOGIC: Verify Style Deviation Scanner is available (v0.7.5a).
        var styleDeviationScanner = provider.GetService<IStyleDeviationScanner>();
        if (styleDeviationScanner is not null)
        {
            logger.LogDebug(
                "Style Deviation Scanner available: {ScannerType}",
                styleDeviationScanner.GetType().Name);
        }
        else
        {
            logger.LogWarning("Style Deviation Scanner is not registered. Tuning Agent features will not be available.");
        }

        // LOGIC: Verify Fix Suggestion Generator is available (v0.7.5b).
        var fixSuggestionGenerator = provider.GetService<IFixSuggestionGenerator>();
        if (fixSuggestionGenerator is not null)
        {
            logger.LogDebug(
                "Fix Suggestion Generator available: {GeneratorType}",
                fixSuggestionGenerator.GetType().Name);
        }
        else
        {
            logger.LogWarning("Fix Suggestion Generator is not registered. AI fix suggestion features will not be available.");
        }

        // LOGIC: Verify Tuning Review UI is available (v0.7.5c).
        // TuningPanelViewModel is transient, so we resolve it to verify registration
        // and immediately dispose it.
        var tuningPanelViewModel = provider.GetService<Tuning.TuningPanelViewModel>();
        if (tuningPanelViewModel is not null)
        {
            logger.LogDebug(
                "Tuning Panel ViewModel available: {ViewModelType}",
                tuningPanelViewModel.GetType().Name);
            tuningPanelViewModel.Dispose();
        }
        else
        {
            logger.LogWarning("Tuning Panel ViewModel is not registered. Accept/Reject UI will not be available.");
        }

        // LOGIC: Verify Learning Loop is available and initialize storage (v0.7.5d).
        var learningLoopService = provider.GetService<ILearningLoopService>();
        if (learningLoopService is not null)
        {
            logger.LogDebug(
                "Learning Loop Service available: {ServiceType}",
                learningLoopService.GetType().Name);

            // LOGIC: Initialize the feedback store's database schema.
            // This creates tables if they don't exist.
            var feedbackStore = provider.GetService<Tuning.Storage.IFeedbackStore>();
            if (feedbackStore is not null)
            {
                await feedbackStore.InitializeAsync();
                logger.LogDebug("Learning Loop feedback store initialized");
            }
        }
        else
        {
            logger.LogWarning("Learning Loop Service is not registered. Feedback-driven improvement will not be available.");
        }
    }
}
