// -----------------------------------------------------------------------
// <copyright file="TuningServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Modules.Agents.Tuning;
using Lexichord.Modules.Agents.Tuning.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Modules.Agents.Extensions;

/// <summary>
/// Extension methods for registering Tuning Agent services.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This class provides extension methods for registering all services
/// required by the Tuning Agent feature:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="AddStyleDeviationScanner"/> (v0.7.5a) — Style deviation scanning with caching and real-time updates</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.5a as part of the Tuning Agent feature.
/// </para>
/// </remarks>
/// <seealso cref="IStyleDeviationScanner"/>
/// <seealso cref="StyleDeviationScanner"/>
/// <seealso cref="ScannerOptions"/>
public static class TuningServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Style Deviation Scanner service to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">Optional configuration action for scanner options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="ScannerOptions"/> (via <c>IOptions&lt;ScannerOptions&gt;</c>)
    ///     <para>
    ///     Configures context window size, cache TTL, severity filtering, and real-time
    ///     update behavior for the scanner.
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IStyleDeviationScanner"/> → <see cref="StyleDeviationScanner"/> (Singleton)
    ///     <para>
    ///     Singleton lifetime is appropriate because the scanner is stateless after
    ///     initialization. It uses <see cref="IMemoryCache"/> for result caching and
    ///     subscribes to MediatR events for real-time updates.
    ///     </para>
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Dependencies:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.Linting.ILintingOrchestrator"/> (v0.2.3a) — Raw violation detection</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.Editor.IEditorService"/> (v0.6.7c) — Document content access</description></item>
    ///   <item><description><see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> — Result caching</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ILicenseContext"/> (v0.0.4c) — License tier validation</description></item>
    ///   <item><description><see cref="Microsoft.Extensions.Logging.ILogger{T}"/> — Diagnostic logging</description></item>
    /// </list>
    /// <para>
    /// <b>MediatR Event Handlers:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="Lexichord.Abstractions.Events.LintingCompletedEvent"/> — Re-scans open documents when linting completes
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="Lexichord.Abstractions.Events.StyleSheetReloadedEvent"/> — Invalidates all caches when style rules change
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>License Requirement:</b> Requires WriterPro tier or higher.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.7.5a as part of the Style Deviation Scanner feature.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration in AgentsModule with default options
    /// public override void RegisterServices(IServiceCollection services)
    /// {
    ///     services.AddStyleDeviationScanner();
    /// }
    ///
    /// // Registration with custom options
    /// services.AddStyleDeviationScanner(options =>
    /// {
    ///     options.ContextWindowSize = 750;
    ///     options.CacheTtlMinutes = 10;
    ///     options.MinimumSeverity = ViolationSeverity.Warning;
    /// });
    ///
    /// // Resolve and use via DI
    /// var scanner = serviceProvider.GetRequiredService&lt;IStyleDeviationScanner&gt;();
    /// var result = await scanner.ScanDocumentAsync(documentPath, cancellationToken);
    ///
    /// foreach (var deviation in result.Deviations)
    /// {
    ///     Console.WriteLine($"[{deviation.Priority}] {deviation.Message}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IStyleDeviationScanner"/>
    /// <seealso cref="StyleDeviationScanner"/>
    /// <seealso cref="ScannerOptions"/>
    /// <seealso cref="StyleDeviation"/>
    /// <seealso cref="DeviationScanResult"/>
    public static IServiceCollection AddStyleDeviationScanner(
        this IServiceCollection services,
        Action<ScannerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Configure ScannerOptions via IOptions pattern.
        // Options can be modified via the configure delegate or later via IConfigureOptions.
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            // LOGIC: Ensure options are registered even without custom configuration.
            // This prevents IOptions<ScannerOptions> from being null in the constructor.
            services.Configure<ScannerOptions>(_ => { });
        }

        // LOGIC: Register StyleDeviationScanner as Singleton implementing IStyleDeviationScanner.
        // Singleton lifetime is appropriate because:
        // 1. The scanner is effectively stateless—cache is managed via IMemoryCache
        // 2. Thread-safe via SemaphoreSlim for scan operations
        // 3. Injected services (ILintingOrchestrator, IEditorService, etc.) are singletons or thread-safe
        // 4. MediatR event handlers are auto-registered and use the same instance
        services.AddSingleton<IStyleDeviationScanner, StyleDeviationScanner>();

        return services;
    }
}
