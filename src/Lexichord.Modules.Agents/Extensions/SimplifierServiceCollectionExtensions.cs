// -----------------------------------------------------------------------
// <copyright file="SimplifierServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

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
/// </list>
/// <para>
/// <b>Future Sub-Parts:</b>
/// <list type="bullet">
///   <item><description>v0.7.4b — Simplifier Agent core</description></item>
///   <item><description>v0.7.4c — Simplification strategies</description></item>
///   <item><description>v0.7.4d — UI integration</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4a as part of the Simplifier Agent feature.
/// </para>
/// </remarks>
/// <seealso cref="IReadabilityTargetService"/>
/// <seealso cref="ReadabilityTargetService"/>
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
}
