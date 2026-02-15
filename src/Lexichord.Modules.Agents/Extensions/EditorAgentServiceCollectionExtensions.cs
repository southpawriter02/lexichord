// -----------------------------------------------------------------------
// <copyright file="EditorAgentServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Agents.Editor;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Modules.Agents.Extensions;

/// <summary>
/// Extension methods for registering Editor Agent context menu services.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This class provides the <see cref="AddEditorAgentContextMenu"/> extension
/// method that registers all services required for the Editor Agent's context menu integration.
/// </para>
/// <para>
/// <b>Services Registered:</b>
/// <list type="bullet">
///   <item><description><see cref="IEditorAgentContextMenuProvider"/> → <see cref="EditorAgentContextMenuProvider"/> (Singleton)</description></item>
///   <item><description><see cref="RewriteCommandViewModel"/> (Transient)</description></item>
///   <item><description><see cref="RewriteKeyboardShortcuts"/> as <see cref="IKeyBindingConfiguration"/> (Singleton)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.3a as part of the Editor Agent feature.
/// </para>
/// </remarks>
/// <seealso cref="IEditorAgentContextMenuProvider"/>
/// <seealso cref="EditorAgentContextMenuProvider"/>
/// <seealso cref="RewriteCommandViewModel"/>
/// <seealso cref="RewriteKeyboardShortcuts"/>
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
}
