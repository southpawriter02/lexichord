// -----------------------------------------------------------------------
// <copyright file="SummaryExportServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.SummaryExport;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Agents.SummaryExport;
using Lexichord.Modules.Agents.SummaryExport.Services;
using Lexichord.Modules.Agents.SummaryExport.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Modules.Agents.Extensions;

/// <summary>
/// Extension methods for registering Summary Export services.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This class provides extension methods for registering all services
/// required by the Summary Export feature:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="AddSummaryExportPipeline"/> (v0.7.6c) — Export services, caching, and ViewModels</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6c as part of the Summarizer Agent Export Formats.
/// </para>
/// </remarks>
/// <seealso cref="ISummaryExporter"/>
/// <seealso cref="SummaryExporter"/>
/// <seealso cref="IClipboardService"/>
public static class SummaryExportServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Summary Export Pipeline services to the service collection.
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
    ///     <see cref="IClipboardService"/> → <see cref="ClipboardService"/> (Singleton)
    ///     <para>
    ///     Singleton lifetime is appropriate because clipboard operations are stateless
    ///     and the service wraps Avalonia's application-level clipboard access.
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="ISummaryCacheService"/> → <see cref="SummaryCacheService"/> (Singleton)
    ///     <para>
    ///     Singleton lifetime ensures consistent caching across the application.
    ///     Uses IMemoryCache internally which is also a singleton.
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="ISummaryExporter"/> → <see cref="SummaryExporter"/> (Singleton)
    ///     <para>
    ///     Singleton lifetime is appropriate because the exporter is stateless—all state
    ///     is passed via request/result parameters.
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="SummaryPanelViewModel"/> (Transient)
    ///     <para>
    ///     Transient lifetime because each panel instance should have its own ViewModel
    ///     with independent state for the displayed summary.
    ///     </para>
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Dependencies:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.Editor.IFileService"/> (v0.1.4a) — File operations</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.Editor.IEditorService"/> (v0.1.3a) — Cursor operations</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ILicenseContext"/> (v0.0.4c) — License verification</description></item>
    ///   <item><description><see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> — Memory caching</description></item>
    ///   <item><description><see cref="MediatR.IMediator"/> — Event publishing</description></item>
    ///   <item><description><see cref="Microsoft.Extensions.Logging.ILogger{T}"/> — Diagnostic logging</description></item>
    /// </list>
    /// <para>
    /// <b>Introduced in:</b> v0.7.6c as part of the Summarizer Agent Export Formats.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration in AgentsModule
    /// public override void RegisterServices(IServiceCollection services)
    /// {
    ///     services.AddSummarizerAgentPipeline();         // v0.7.6a
    ///     services.AddMetadataExtractionPipeline();      // v0.7.6b
    ///     services.AddSummaryExportPipeline();           // v0.7.6c
    /// }
    ///
    /// // Resolve and use via DI
    /// var exporter = serviceProvider.GetRequiredService&lt;ISummaryExporter&gt;();
    /// var result = await exporter.ExportAsync(summary, documentPath, options, ct);
    /// </code>
    /// </example>
    public static IServiceCollection AddSummaryExportPipeline(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register ClipboardService as Singleton implementing IClipboardService.
        // Singleton lifetime is appropriate because:
        // 1. Clipboard operations are stateless
        // 2. Wraps Avalonia's application-level clipboard access
        // 3. Thread-safe implementation with UI thread marshalling
        services.AddSingleton<IClipboardService, ClipboardService>();

        // LOGIC: Register SummaryCacheService as Singleton implementing ISummaryCacheService.
        // Singleton lifetime ensures:
        // 1. Consistent caching across the application
        // 2. Proper sharing of IMemoryCache entries
        // 3. No duplication of file cache operations
        services.AddSingleton<ISummaryCacheService, SummaryCacheService>();

        // LOGIC: Register SummaryExporter as Singleton implementing ISummaryExporter.
        // Singleton lifetime is appropriate because:
        // 1. The exporter is stateless—all state is passed via request/result
        // 2. Injected services are singletons or thread-safe
        // 3. No per-request state is maintained between invocations
        services.AddSingleton<ISummaryExporter, SummaryExporter>();

        // LOGIC: Register SummaryPanelViewModel as Transient.
        // Transient lifetime because:
        // 1. Each panel instance should have its own ViewModel
        // 2. ViewModels maintain state for the displayed summary
        // 3. Multiple panels could display different summaries
        services.AddTransient<SummaryPanelViewModel>();

        return services;
    }
}
