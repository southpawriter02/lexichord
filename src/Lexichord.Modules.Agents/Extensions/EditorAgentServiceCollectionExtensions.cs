// -----------------------------------------------------------------------
// <copyright file="EditorAgentServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Agents.Editor;
using Lexichord.Modules.Agents.Editor.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Modules.Agents.Extensions;

/// <summary>
/// Extension methods for registering Editor Agent services.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This class provides extension methods for registering all services
/// required by the Editor Agent feature:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="AddEditorAgentContextMenu"/> (v0.7.3a) — Context menu, ViewModel, keyboard shortcuts</description></item>
///   <item><description><see cref="AddEditorAgentPipeline"/> (v0.7.3b) — Agent, command handler, event handler</description></item>
///   <item><description><see cref="AddEditorAgentContextStrategies"/> (v0.7.3c) — Context-aware rewriting strategies</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.3a. Extended in v0.7.3b, v0.7.3c.
/// </para>
/// </remarks>
/// <seealso cref="IEditorAgentContextMenuProvider"/>
/// <seealso cref="EditorAgentContextMenuProvider"/>
/// <seealso cref="IEditorAgent"/>
/// <seealso cref="IRewriteCommandHandler"/>
public static class EditorAgentServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Editor Agent context menu services to the service collection.
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
    ///     <see cref="IEditorAgentContextMenuProvider"/> → <see cref="EditorAgentContextMenuProvider"/> (Singleton)
    ///     <para>Singleton lifetime to maintain selection state subscriptions across the application.</para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="RewriteCommandViewModel"/> (Transient)
    ///     <para>Transient lifetime for per-view isolation of command state.</para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="RewriteKeyboardShortcuts"/> as <see cref="IKeyBindingConfiguration"/> (Singleton)
    ///     <para>Singleton for one-time keyboard binding registration during startup.</para>
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Dependencies:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.Editor.IEditorService"/> (v0.6.7a) — Selection state, context menu registration</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ILicenseContext"/> (v0.0.4c) — License tier checking</description></item>
    ///   <item><description><see cref="MediatR.IMediator"/> (v0.0.7a) — Event publishing</description></item>
    ///   <item><description><see cref="IKeyBindingService"/> — Keyboard shortcut registration</description></item>
    /// </list>
    /// <para>
    /// <b>Introduced in:</b> v0.7.3a as part of the Editor Agent feature.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration in AgentsModule
    /// public override void RegisterServices(IServiceCollection services)
    /// {
    ///     services.AddEditorAgentContextMenu();
    /// }
    ///
    /// // Later, resolve via DI
    /// var provider = serviceProvider.GetRequiredService&lt;IEditorAgentContextMenuProvider&gt;();
    /// var menuItems = provider.GetRewriteMenuItems();
    /// </code>
    /// </example>
    public static IServiceCollection AddEditorAgentContextMenu(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register EditorAgentContextMenuProvider as Singleton.
        // Singleton is required to maintain selection state subscriptions
        // (IEditorService.SelectionChanged, ILicenseContext.LicenseChanged)
        // across the application lifetime.
        services.AddSingleton<IEditorAgentContextMenuProvider, EditorAgentContextMenuProvider>();

        // LOGIC: Register RewriteCommandViewModel as Transient.
        // Transient lifetime ensures per-view isolation of IsExecuting,
        // Progress, and command state. Each view binding gets its own instance.
        services.AddTransient<RewriteCommandViewModel>();

        // LOGIC: Register RewriteKeyboardShortcuts as Singleton implementing
        // IKeyBindingConfiguration. The Configure method is called once during
        // module initialization to register keyboard bindings.
        services.AddSingleton<IKeyBindingConfiguration, RewriteKeyboardShortcuts>();

        return services;
    }

    /// <summary>
    /// Adds the Editor Agent command pipeline services to the service collection.
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
    ///     <see cref="IEditorAgent"/> → <see cref="EditorAgent"/> (Singleton)
    ///     <para>
    ///     Singleton lifetime because the agent is stateless — all per-invocation state
    ///     is stack-local. Registered both as <see cref="IEditorAgent"/> (for the pipeline)
    ///     and as <see cref="IAgent"/> (for the agent registry).
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IRewriteCommandHandler"/> → <see cref="RewriteCommandHandler"/> (Scoped)
    ///     <para>
    ///     Scoped lifetime ensures per-operation isolation of <see cref="IRewriteCommandHandler.IsExecuting"/>
    ///     and the internal <see cref="CancellationTokenSource"/>. Each scope gets its own
    ///     handler instance.
    ///     </para>
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Forward-Declared Dependencies:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="IRewriteApplicator"/> is NOT registered here. It will be provided by v0.7.3d.
    ///     <see cref="RewriteCommandHandler"/> accepts it as nullable (<c>IRewriteApplicator?</c>)
    ///     and skips document application when null.
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>MediatR Auto-Registration:</b>
    /// <see cref="RewriteRequestedEventHandler"/> is automatically discovered by MediatR's
    /// assembly scanning (configured via <c>AddMediatR</c> in the infrastructure module).
    /// No explicit registration is needed.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.7.3b as part of the Agent Command Pipeline.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration in AgentsModule
    /// public override void RegisterServices(IServiceCollection services)
    /// {
    ///     services.AddEditorAgentContextMenu();  // v0.7.3a
    ///     services.AddEditorAgentPipeline();      // v0.7.3b
    /// }
    ///
    /// // Later, resolve via DI
    /// var handler = serviceProvider.GetRequiredService&lt;IRewriteCommandHandler&gt;();
    /// var result = await handler.ExecuteAsync(rewriteRequest);
    /// </code>
    /// </example>
    public static IServiceCollection AddEditorAgentPipeline(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register EditorAgent as Singleton implementing IEditorAgent.
        // The agent is stateless (all state is per-invocation on the stack),
        // making singleton lifetime safe and performant.
        services.AddSingleton<IEditorAgent, EditorAgent>();

        // LOGIC: Also register the EditorAgent as an IAgent for the agent registry.
        // This forward-resolves from the IEditorAgent singleton to avoid duplicate
        // instances. The AgentDefinitionScanner and IAgentRegistry can discover
        // the editor agent through the IAgent service collection.
        services.AddSingleton<IAgent>(sp => sp.GetRequiredService<IEditorAgent>());

        // LOGIC: Register RewriteCommandHandler as Scoped implementing IRewriteCommandHandler.
        // Scoped lifetime ensures per-request isolation of IsExecuting state and the
        // internal CancellationTokenSource. Each DI scope (e.g., each context menu
        // invocation) gets its own handler instance.
        services.AddScoped<IRewriteCommandHandler, RewriteCommandHandler>();

        return services;
    }

    /// <summary>
    /// Adds the Editor Agent context strategy services to the service collection.
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
    ///     <see cref="SurroundingTextContextStrategy"/> (Transient)
    ///     <para>
    ///     Transient lifetime because strategies are stateless — each context-gathering
    ///     cycle creates a fresh instance via the <see cref="Lexichord.Abstractions.Agents.Context.IContextStrategyFactory"/>.
    ///     Gathers surrounding paragraphs from the document for tone consistency.
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="EditorTerminologyContextStrategy"/> (Transient)
    ///     <para>
    ///     Transient lifetime for the same reason. Scans selected text against the
    ///     terminology database and provides matching terms with replacements.
    ///     </para>
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Registration in Factory:</b>
    /// These strategies are also registered in <see cref="Lexichord.Modules.Agents.Context.ContextStrategyFactory"/>
    /// as WriterPro-tier strategies with IDs <c>"surrounding-text"</c> and <c>"terminology"</c>.
    /// The factory resolves them from the DI container when requested.
    /// </para>
    /// <para>
    /// <b>Dependencies:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.Editor.IEditorService"/> (v0.6.7c) — Document content access for surrounding text</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ITerminologyRepository"/> (v0.2.2b) — Active terminology lookup</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ITokenCounter"/> (v0.6.1b) — Token estimation for budget management</description></item>
    /// </list>
    /// <para>
    /// <b>Introduced in:</b> v0.7.3c as part of Context-Aware Rewriting.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration in AgentsModule
    /// public override void RegisterServices(IServiceCollection services)
    /// {
    ///     services.AddEditorAgentContextMenu();       // v0.7.3a
    ///     services.AddEditorAgentPipeline();           // v0.7.3b
    ///     services.AddEditorAgentContextStrategies();  // v0.7.3c
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddEditorAgentContextStrategies(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register SurroundingTextContextStrategy as Transient.
        // Strategies are lightweight and stateless — the ContextStrategyFactory
        // creates them on demand per context-gathering cycle.
        services.AddTransient<SurroundingTextContextStrategy>();

        // LOGIC: Register EditorTerminologyContextStrategy as Transient.
        // Same rationale as above.
        services.AddTransient<EditorTerminologyContextStrategy>();

        return services;
    }
}
