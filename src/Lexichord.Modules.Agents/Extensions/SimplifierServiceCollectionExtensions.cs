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
/// </list>
/// <para>
/// <b>Future Sub-Parts:</b>
/// <list type="bullet">
///   <item><description>v0.7.4c — Simplification result integration (acceptance, preview)</description></item>
///   <item><description>v0.7.4d — UI integration (toolbar, status bar, keyboard shortcuts)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4a as part of the Simplifier Agent feature.
/// </para>
/// </remarks>
/// <seealso cref="IReadabilityTargetService"/>
/// <seealso cref="ReadabilityTargetService"/>
/// <seealso cref="ISimplificationPipeline"/>
/// <seealso cref="SimplifierAgent"/>
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
}
