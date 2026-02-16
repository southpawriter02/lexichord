// -----------------------------------------------------------------------
// <copyright file="TuningServiceCollectionExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.Agents.Events;
using Lexichord.Modules.Agents.Tuning;
using Lexichord.Modules.Agents.Tuning.Configuration;
using Lexichord.Modules.Agents.Tuning.Storage;
using MediatR;
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
///   <item><description><see cref="AddFixSuggestionGenerator"/> (v0.7.5b) — AI-powered fix suggestions for style deviations</description></item>
///   <item><description><see cref="AddTuningReviewUI"/> (v0.7.5c) — Accept/Reject UI ViewModels for suggestion review</description></item>
///   <item><description><see cref="AddLearningLoop"/> (v0.7.5d) — Learning Loop feedback persistence and pattern analysis</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.5a as part of the Tuning Agent feature.
/// </para>
/// <para>
/// <b>Updated in:</b> v0.7.5b with fix suggestion generation support.
/// </para>
/// <para>
/// <b>Updated in:</b> v0.7.5c with Accept/Reject review UI.
/// </para>
/// <para>
/// <b>Updated in:</b> v0.7.5d with Learning Loop feedback system.
/// </para>
/// </remarks>
/// <seealso cref="IStyleDeviationScanner"/>
/// <seealso cref="StyleDeviationScanner"/>
/// <seealso cref="ScannerOptions"/>
/// <seealso cref="IFixSuggestionGenerator"/>
/// <seealso cref="FixSuggestionGenerator"/>
/// <seealso cref="TuningPanelViewModel"/>
/// <seealso cref="SuggestionCardViewModel"/>
/// <seealso cref="ILearningLoopService"/>
/// <seealso cref="LearningLoopService"/>
/// <seealso cref="LearningStorageOptions"/>
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

    /// <summary>
    /// Adds the Fix Suggestion Generator service to the service collection.
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
    ///     <see cref="DiffGenerator"/> (Singleton)
    ///     <para>
    ///     Internal helper for generating text diffs using DiffPlex.
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="FixValidator"/> (Singleton)
    ///     <para>
    ///     Internal helper for validating fix suggestions against the linter.
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IFixSuggestionGenerator"/> → <see cref="FixSuggestionGenerator"/> (Singleton)
    ///     <para>
    ///     Singleton lifetime is appropriate because the generator is stateless.
    ///     </para>
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Dependencies:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.LLM.IChatCompletionService"/> (v0.6.1a) — LLM communication</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.LLM.IPromptRenderer"/> (v0.6.3b) — Template rendering</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.LLM.IPromptTemplateRepository"/> (v0.6.3c) — Template storage</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.IStyleEngine"/> (v0.2.1a) — Fix validation via re-analysis</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ILicenseContext"/> (v0.0.4c) — License tier validation</description></item>
    ///   <item><description><see cref="Microsoft.Extensions.Logging.ILogger{T}"/> — Diagnostic logging</description></item>
    /// </list>
    /// <para>
    /// <b>License Requirement:</b> Requires WriterPro tier or higher.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.7.5b as part of the Automatic Fix Suggestions feature.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration in AgentsModule
    /// public override void RegisterServices(IServiceCollection services)
    /// {
    ///     services.AddStyleDeviationScanner();
    ///     services.AddFixSuggestionGenerator();
    /// }
    ///
    /// // Resolve and use via DI
    /// var generator = serviceProvider.GetRequiredService&lt;IFixSuggestionGenerator&gt;();
    /// var suggestion = await generator.GenerateFixAsync(deviation);
    ///
    /// if (suggestion.Success &amp;&amp; suggestion.IsHighConfidence)
    /// {
    ///     Console.WriteLine($"Fix: {suggestion.SuggestedText}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IFixSuggestionGenerator"/>
    /// <seealso cref="FixSuggestionGenerator"/>
    /// <seealso cref="FixSuggestion"/>
    /// <seealso cref="FixGenerationOptions"/>
    public static IServiceCollection AddFixSuggestionGenerator(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register internal helper services
        // These are internal implementation details not exposed via interfaces
        services.AddSingleton<DiffGenerator>();
        services.AddSingleton<FixValidator>();

        // LOGIC: Register FixSuggestionGenerator as Singleton implementing IFixSuggestionGenerator.
        // Singleton lifetime is appropriate because:
        // 1. The generator is stateless
        // 2. Thread-safe via SemaphoreSlim for batch operations
        // 3. Injected services are singletons or thread-safe
        services.AddSingleton<IFixSuggestionGenerator, FixSuggestionGenerator>();

        return services;
    }

    /// <summary>
    /// Adds the Tuning Review UI ViewModels to the service collection.
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
    ///     <see cref="TuningPanelViewModel"/> (Transient)
    ///     <para>
    ///     Transient lifetime is appropriate because each panel instance manages its own
    ///     scan state, suggestion collection, and UI state. Multiple panels should not
    ///     share state.
    ///     </para>
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Note:</b> <see cref="SuggestionCardViewModel"/> is NOT DI-registered.
    /// Instances are created manually by <see cref="TuningPanelViewModel"/> during
    /// the scan/generation flow.
    /// </para>
    /// <para>
    /// <b>Dependencies:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="IStyleDeviationScanner"/> (v0.7.5a) — Deviation detection</description></item>
    ///   <item><description><see cref="IFixSuggestionGenerator"/> (v0.7.5b) — Fix generation</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.Editor.IEditorService"/> (v0.6.7b) — Document editing</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.Undo.IUndoRedoService"/> (v0.7.3d) — Undo support (nullable)</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ILicenseContext"/> (v0.0.4c) — License validation</description></item>
    ///   <item><description><see cref="MediatR.IMediator"/> (v0.0.7a) — Event publishing</description></item>
    ///   <item><description><see cref="Microsoft.Extensions.Logging.ILogger{T}"/> — Diagnostic logging</description></item>
    /// </list>
    /// <para>
    /// <b>License Requirement:</b> Requires WriterPro tier or higher.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.7.5c as part of the Accept/Reject UI feature.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration in AgentsModule
    /// public override void RegisterServices(IServiceCollection services)
    /// {
    ///     services.AddStyleDeviationScanner();
    ///     services.AddFixSuggestionGenerator();
    ///     services.AddTuningReviewUI();
    /// }
    ///
    /// // Resolve and use via DI
    /// var viewModel = serviceProvider.GetRequiredService&lt;TuningPanelViewModel&gt;();
    /// viewModel.InitializeAsync();
    /// await viewModel.ScanDocumentCommand.ExecuteAsync(null);
    /// </code>
    /// </example>
    /// <seealso cref="TuningPanelViewModel"/>
    /// <seealso cref="SuggestionCardViewModel"/>
    public static IServiceCollection AddTuningReviewUI(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Register TuningPanelViewModel as Transient.
        // Transient lifetime is appropriate because:
        // 1. Each panel instance manages its own scan/review state
        // 2. Multiple panels should have isolated suggestion collections
        // 3. Matches the SimplificationPreviewViewModel registration pattern
        services.AddTransient<TuningPanelViewModel>();

        return services;
    }

    /// <summary>
    /// Adds the Learning Loop services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">Optional configuration action for storage options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="LearningStorageOptions"/> (via <c>IOptions&lt;LearningStorageOptions&gt;</c>)
    ///     <para>
    ///     Configures database path, retention days, pattern cache limits, and
    ///     minimum pattern frequency.
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IFeedbackStore"/> → <see cref="SqliteFeedbackStore"/> (Singleton)
    ///     <para>
    ///     Singleton lifetime is appropriate because the SQLite connection pool is shared
    ///     across the application. Thread-safe via per-method connection pooling.
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="PatternAnalyzer"/> (Singleton)
    ///     <para>
    ///     Singleton lifetime is appropriate because the analyzer is stateless.
    ///     </para>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="ILearningLoopService"/> → <see cref="LearningLoopService"/> (Singleton)
    ///     <para>
    ///     Singleton lifetime ensures consistent state and a single database connection pool.
    ///     Also registered as MediatR notification handlers for <see cref="SuggestionAcceptedEvent"/>
    ///     and <see cref="SuggestionRejectedEvent"/> via factory forwarding.
    ///     </para>
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>MediatR Handler Forwarding:</b> The <see cref="LearningLoopService"/> singleton
    /// is forwarded as both <c>INotificationHandler&lt;SuggestionAcceptedEvent&gt;</c> and
    /// <c>INotificationHandler&lt;SuggestionRejectedEvent&gt;</c> to ensure the same instance
    /// handles events as serves the public API.
    /// </para>
    /// <para>
    /// <b>Dependencies:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ISettingsService"/> (v0.1.6a) — Privacy settings persistence</description></item>
    ///   <item><description><see cref="Lexichord.Abstractions.Contracts.ILicenseContext"/> (v0.0.4c) — License tier validation</description></item>
    ///   <item><description><see cref="Microsoft.Extensions.Logging.ILogger{T}"/> — Diagnostic logging</description></item>
    /// </list>
    /// <para>
    /// <b>License Requirement:</b> Requires Teams tier or higher.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration in AgentsModule with default options
    /// public override void RegisterServices(IServiceCollection services)
    /// {
    ///     services.AddStyleDeviationScanner();
    ///     services.AddFixSuggestionGenerator();
    ///     services.AddTuningReviewUI();
    ///     services.AddLearningLoop();
    /// }
    ///
    /// // Registration with custom options
    /// services.AddLearningLoop(options =>
    /// {
    ///     options.DatabasePath = "/custom/path/learning.db";
    ///     options.RetentionDays = 180;
    /// });
    ///
    /// // Resolve and use via DI
    /// var learningLoop = serviceProvider.GetRequiredService&lt;ILearningLoopService&gt;();
    /// var context = await learningLoop.GetLearningContextAsync("TERM-001");
    /// </code>
    /// </example>
    /// <seealso cref="ILearningLoopService"/>
    /// <seealso cref="LearningLoopService"/>
    /// <seealso cref="LearningStorageOptions"/>
    /// <seealso cref="SqliteFeedbackStore"/>
    /// <seealso cref="PatternAnalyzer"/>
    public static IServiceCollection AddLearningLoop(
        this IServiceCollection services,
        Action<LearningStorageOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // LOGIC: Configure LearningStorageOptions via IOptions pattern.
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            // LOGIC: Ensure options are registered even without custom configuration.
            services.Configure<LearningStorageOptions>(_ => { });
        }

        // LOGIC: Register SqliteFeedbackStore as Singleton implementing IFeedbackStore.
        // Singleton lifetime is appropriate because:
        // 1. Connection pooling is managed at the connection string level
        // 2. Thread-safe via per-method connection creation from the pool
        // 3. Schema initialization is called once during module InitializeAsync
        services.AddSingleton<IFeedbackStore, SqliteFeedbackStore>();

        // LOGIC: Register PatternAnalyzer as Singleton.
        // The analyzer is stateless — all state is passed as method parameters.
        services.AddSingleton<PatternAnalyzer>();

        // LOGIC: Register LearningLoopService as Singleton implementing ILearningLoopService.
        // Singleton ensures the same instance handles both public API calls and MediatR events.
        services.AddSingleton<LearningLoopService>();
        services.AddSingleton<ILearningLoopService>(sp =>
            sp.GetRequiredService<LearningLoopService>());

        // LOGIC: Forward MediatR handler registrations to the same singleton instance.
        // This is critical — without forwarding, MediatR would create separate instances
        // for each handler registration, breaking the singleton pattern.
        services.AddSingleton<INotificationHandler<SuggestionAcceptedEvent>>(sp =>
            sp.GetRequiredService<LearningLoopService>());
        services.AddSingleton<INotificationHandler<SuggestionRejectedEvent>>(sp =>
            sp.GetRequiredService<LearningLoopService>());

        return services;
    }
}
