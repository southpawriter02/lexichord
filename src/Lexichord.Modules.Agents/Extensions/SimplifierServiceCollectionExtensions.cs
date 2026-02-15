// -----------------------------------------------------------------------
// <copyright file="SimplifierServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Modules.Agents.Simplifier;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Modules.Agents.Extensions;

/// <summary>
/// Extension methods for registering Simplifier Agent services.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This class provides extension methods for registering all services
/// required by the Simplifier Agent feature:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="AddReadabilityTargetService"/> (v0.7.4a) — Readability target resolution and preset management</description></item>
///   <item><description><see cref="AddSimplifierAgentPipeline"/> (v0.7.4b) — Simplifier Agent, response parser, pipeline interface</description></item>
///   <item><description><see cref="AddSimplifierPreviewUI"/> (v0.7.4c) — Preview/Diff UI ViewModel</description></item>
///   <item><description><see cref="AddBatchSimplificationService"/> (v0.7.4d) — Batch simplification service and ViewModels</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.4a as part of the Simplifier Agent feature.
/// </para>
/// </remarks>
/// <seealso cref="IReadabilityTargetService"/>
/// <seealso cref="ReadabilityTargetService"/>
/// <seealso cref="ISimplificationPipeline"/>
/// <seealso cref="SimplifierAgent"/>
/// <seealso cref="SimplificationPreviewViewModel"/>
public static class SimplifierServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Readability Target Service to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="IReadabilityTargetService"/> → <see cref="ReadabilityTargetService"/> (Singleton)
    ///     <para>
    ///     Singleton lifetime is appropriate because the service is stateless after initialization.
    ///     Custom presets are loaded once and cached. Thread-safe locking protects preset operations.
    ///     </para>
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Dependencies:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.IVoiceProfileService"/> (v0.3.4a) — Voice Profile settings</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.IReadabilityService"/> (v0.3.3c) — Text readability analysis</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ISettingsService"/> (v0.1.6a) — Custom preset persistence</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ILicenseContext"/> (v0.0.4c) — License tier checking</description></item>
    ///   <item><description><see cref="Microsoft.Extensions.Logging.ILogger{T}"/> — Diagnostic logging</description></item>
    /// </list>
    /// <para>
    /// <b>Introduced in:</b> v0.7.4a as part of the Simplifier Agent Readability Target Service.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration in AgentsModule
    /// public override void RegisterServices(IServiceCollection services)
    /// {
    ///     services.AddEditorAgentContextMenu();         // v0.7.3a
    ///     services.AddEditorAgentPipeline();             // v0.7.3b
    ///     services.AddEditorAgentContextStrategies();    // v0.7.3c
    ///     services.AddEditorAgentUndoIntegration();      // v0.7.3d
    ///     services.AddReadabilityTargetService();        // v0.7.4a
    /// }
    ///
    /// // Later, resolve via DI
    /// var targetService = serviceProvider.GetRequiredService&lt;IReadabilityTargetService&gt;();
    /// var target = await targetService.GetTargetAsync(presetId: "general-public");
    /// </code>
    /// </example>
    public static IServiceCollection AddReadabilityTargetService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register ReadabilityTargetService as Singleton implementing IReadabilityTargetService.
        // Singleton lifetime is appropriate because:
        // 1. The service is stateless after initialization (custom presets are cached)
        // 2. Thread-safe locking protects preset modification operations
        // 3. Dependencies (IVoiceProfileService, IReadabilityService, etc.) are also singletons or safe for concurrent use
        services.AddSingleton<IReadabilityTargetService, ReadabilityTargetService>();

        return services;
    }

    /// <summary>
    /// Adds the Simplifier Agent Pipeline to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="ISimplificationResponseParser"/> → <see cref="SimplificationResponseParser"/> (Singleton)
    ///     <para>
    ///     Singleton lifetime is appropriate because the parser is stateless—it uses
    ///     compiled regex patterns and has no instance state.
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="ISimplificationPipeline"/> → <see cref="SimplifierAgent"/> (Singleton)
    ///     <para>
    ///     Singleton lifetime is appropriate because the agent is stateless—all state
    ///     is passed via <see cref="SimplificationRequest"/> and returned via
    ///     <see cref="SimplificationResult"/>.
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IAgent"/> → <see cref="SimplifierAgent"/> (Singleton, forwarding registration)
    ///     <para>
    ///     Registered as a forwarding service so the agent is discoverable via the
    ///     <see cref="IAgentRegistry"/> while sharing the same instance as
    ///     <see cref="ISimplificationPipeline"/>.
    ///     </para>
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Dependencies:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.LLM.IChatCompletionService"/> — LLM chat completion</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.LLM.IPromptRenderer"/> — Prompt template rendering</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.LLM.IPromptTemplateRepository"/> — Template lookup</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Agents.Context.IContextOrchestrator"/> — Context assembly</description></item>
    ///   <item><description><see cref="IReadabilityTargetService"/> (v0.7.4a) — Target resolution</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.IReadabilityService"/> (v0.3.3c) — Text analysis</description></item>
    ///   <item><description><see cref="Microsoft.Extensions.Logging.ILogger{T}"/> — Diagnostic logging</description></item>
    /// </list>
    /// <para>
    /// <b>Introduced in:</b> v0.7.4b as part of the Simplifier Agent Simplification Pipeline.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration in AgentsModule
    /// public override void RegisterServices(IServiceCollection services)
    /// {
    ///     services.AddReadabilityTargetService();        // v0.7.4a
    ///     services.AddSimplifierAgentPipeline();         // v0.7.4b
    /// }
    ///
    /// // Resolve and use via DI
    /// var pipeline = serviceProvider.GetRequiredService&lt;ISimplificationPipeline&gt;();
    ///
    /// var request = new SimplificationRequest
    /// {
    ///     OriginalText = complexText,
    ///     Target = await targetService.GetTargetAsync(presetId: "general-public")
    /// };
    ///
    /// var result = await pipeline.SimplifyAsync(request, cancellationToken);
    /// </code>
    /// </example>
    public static IServiceCollection AddSimplifierAgentPipeline(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register SimplificationResponseParser as Singleton.
        // The parser is stateless (uses compiled regex patterns) and thread-safe.
        services.AddSingleton<ISimplificationResponseParser, SimplificationResponseParser>();

        // LOGIC: Register SimplifierAgent as Singleton implementing ISimplificationPipeline.
        // Singleton lifetime is appropriate because:
        // 1. The agent is stateless—all state is passed via request/result
        // 2. Injected services (IChatCompletionService, etc.) are singletons or thread-safe
        // 3. No per-request state is maintained between invocations
        services.AddSingleton<ISimplificationPipeline, SimplifierAgent>();

        // LOGIC: Forward IAgent to the same SimplifierAgent instance.
        // This enables the agent to be discovered via IAgentRegistry while sharing
        // the same instance used by ISimplificationPipeline consumers.
        services.AddSingleton<IAgent>(sp =>
            (IAgent)sp.GetRequiredService<ISimplificationPipeline>());

        return services;
    }

    /// <summary>
    /// Adds the Simplifier Agent Preview/Diff UI services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="SimplificationPreviewViewModel"/> (Transient)
    ///     <para>
    ///     Transient lifetime is used because each preview instance requires its own
    ///     isolated state. Multiple previews can be open simultaneously, each with
    ///     different original text and simplification results.
    ///     </para>
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Dependencies:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="ISimplificationPipeline"/> (v0.7.4b) — Re-simplification support</description></item>
    ///   <item><description><see cref="IReadabilityTargetService"/> (v0.7.4a) — Preset retrieval</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.Editor.IEditorService"/> — Apply changes to document</description></item>
    ///   <item><description><see cref="MediatR.IMediator"/> — Event publishing</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ILicenseContext"/> — License validation</description></item>
    ///   <item><description><see cref="Microsoft.Extensions.Logging.ILogger{T}"/> — Diagnostic logging</description></item>
    /// </list>
    /// <para>
    /// <b>Introduced in:</b> v0.7.4c as part of the Simplifier Agent Preview/Diff UI.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration in AgentsModule
    /// public override void RegisterServices(IServiceCollection services)
    /// {
    ///     services.AddReadabilityTargetService();        // v0.7.4a
    ///     services.AddSimplifierAgentPipeline();         // v0.7.4b
    ///     services.AddSimplifierPreviewUI();             // v0.7.4c
    /// }
    ///
    /// // Creating a preview ViewModel
    /// var viewModel = serviceProvider.GetRequiredService&lt;SimplificationPreviewViewModel&gt;();
    /// await viewModel.InitializeAsync(documentPath);
    /// viewModel.SetResult(simplificationResult, originalText);
    /// </code>
    /// </example>
    public static IServiceCollection AddSimplifierPreviewUI(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register SimplificationPreviewViewModel as Transient.
        // Transient lifetime is appropriate because:
        // 1. Each preview instance needs isolated state (original text, changes, selection)
        // 2. Multiple previews may be open simultaneously (different documents)
        // 3. The ViewModel is disposable and should be cleaned up when the preview closes
        services.AddTransient<SimplificationPreviewViewModel>();

        return services;
    }

    /// <summary>
    /// Adds the Batch Simplification services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="IBatchSimplificationService"/> → <see cref="BatchSimplificationService"/> (Singleton)
    ///     <para>
    ///     Singleton lifetime is appropriate because the service is stateless—all
    ///     operation state is passed via parameters and returned via results.
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="BatchProgressViewModel"/> (Transient)
    ///     <para>
    ///     Transient lifetime is used because each batch operation needs its own
    ///     progress tracking state. Each dialog instance is independent.
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="BatchCompletionViewModel"/> (Transient)
    ///     <para>
    ///     Transient lifetime is used because each completion dialog needs its own
    ///     result state. Multiple completion dialogs may exist for different operations.
    ///     </para>
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Dependencies:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="ISimplificationPipeline"/> (v0.7.4b) — Per-paragraph simplification</description></item>
    ///   <item><description><see cref="IReadabilityTargetService"/> (v0.7.4a) — Target resolution</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.IReadabilityService"/> (v0.3.3c) — Metrics calculation</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.Editor.IEditorService"/> — Document operations</description></item>
    ///   <item><description><see cref="MediatR.IMediator"/> — Event publishing</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ILicenseContext"/> — License validation</description></item>
    ///   <item><description><see cref="Microsoft.Extensions.Logging.ILogger{T}"/> — Diagnostic logging</description></item>
    /// </list>
    /// <para>
    /// <b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration in AgentsModule
    /// public override void RegisterServices(IServiceCollection services)
    /// {
    ///     services.AddReadabilityTargetService();        // v0.7.4a
    ///     services.AddSimplifierAgentPipeline();         // v0.7.4b
    ///     services.AddSimplifierPreviewUI();             // v0.7.4c
    ///     services.AddBatchSimplificationService();      // v0.7.4d
    /// }
    ///
    /// // Using the batch service
    /// var batchService = serviceProvider.GetRequiredService&lt;IBatchSimplificationService&gt;();
    /// var progress = new Progress&lt;BatchSimplificationProgress&gt;(p =&gt;
    ///     Console.WriteLine($"Processing paragraph {p.CurrentParagraph}/{p.TotalParagraphs}"));
    ///
    /// var result = await batchService.SimplifyDocumentAsync(
    ///     documentPath,
    ///     target,
    ///     options: null,
    ///     progress,
    ///     cancellationToken);
    /// </code>
    /// </example>
    /// <seealso cref="IBatchSimplificationService"/>
    /// <seealso cref="BatchSimplificationService"/>
    /// <seealso cref="BatchProgressViewModel"/>
    /// <seealso cref="BatchCompletionViewModel"/>
    public static IServiceCollection AddBatchSimplificationService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register BatchSimplificationService as Singleton.
        // Singleton lifetime is appropriate because:
        // 1. The service is stateless—all operation state is passed via parameters
        // 2. Injected services (ISimplificationPipeline, etc.) are singletons or thread-safe
        // 3. No per-operation state is maintained between invocations
        services.AddSingleton<IBatchSimplificationService, BatchSimplificationService>();

        // LOGIC: Register BatchProgressViewModel as Transient.
        // Transient lifetime is appropriate because:
        // 1. Each batch operation needs isolated progress tracking state
        // 2. The ViewModel holds CancellationTokenSource for the specific operation
        // 3. Multiple batch operations should not share state
        services.AddTransient<BatchProgressViewModel>();

        // LOGIC: Register BatchCompletionViewModel as Transient.
        // Transient lifetime is appropriate because:
        // 1. Each completion dialog displays results for a specific operation
        // 2. Multiple completion dialogs may be shown for different documents
        // 3. The ViewModel holds BatchSimplificationResult for the specific operation
        services.AddTransient<BatchCompletionViewModel>();

        return services;
    }
}
