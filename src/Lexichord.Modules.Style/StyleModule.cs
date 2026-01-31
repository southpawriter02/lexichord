using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Abstractions.Contracts.Threading;
using Lexichord.Abstractions.Layout;
using Lexichord.Modules.Style.Data;
using Lexichord.Modules.Style.Filters;
using Lexichord.Modules.Style.Services;
using Lexichord.Modules.Style.Services.Linting;
using Lexichord.Modules.Style.Threading;
using Lexichord.Modules.Style.ViewModels;
using Lexichord.Modules.Style.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style;

/// <summary>
/// Module registration for the Style module ("The Rulebook").
/// </summary>
/// <remarks>
/// LOGIC: Registers all style-related services and initializes the style engine.
/// The Style module provides:
/// - Style sheet loading and validation (v0.2.1c)
/// - Content analysis for style violations (v0.2.1b)
/// - Live file watching for hot reload (v0.2.1d)
///
/// Philosophy: Concordance - "Rules over improvisation"
/// The Rulebook provides governed writing environments where style
/// guides are applied consistently across all content.
/// </remarks>
public sealed class StyleModule : IModule
{
    private ILogger<StyleModule>? _logger;

    /// <inheritdoc/>
    public ModuleInfo Info => new(
        Id: "style",
        Name: "The Rulebook",
        Version: new Version(0, 2, 7),
        Author: "Lexichord Team",
        Description: "Style and writing rules engine providing governed writing environments"
    );

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: All services are singletons because:
    /// - IStyleEngine: Maintains active sheet state, thread-safe analysis
    /// - IStyleSheetLoader: Caches compiled patterns, expensive to create
    /// - IStyleConfigurationWatcher: Holds OS file watcher resources
    /// - IScannerService: Shared pattern cache, expensive to recreate (v0.2.3c)
    /// </remarks>
    public void RegisterServices(IServiceCollection services)
    {
        // LOGIC: Core style analysis engine
        services.AddSingleton<IStyleEngine, StyleEngine>();

        // LOGIC: YAML deserialization and schema validation
        services.AddSingleton<IStyleSheetLoader, YamlStyleSheetLoader>();

        // LOGIC: File system watcher for live reload
        services.AddSingleton<IStyleConfigurationWatcher, FileSystemStyleWatcher>();

        // LOGIC: v0.3.1a - Fuzzy matching service (stateless, thread-safe)
        services.AddSingleton<IFuzzyMatchService, FuzzyMatchService>();

        // LOGIC: v0.3.1c - Document tokenizer for text processing
        services.AddSingleton<IDocumentTokenizer, DocumentTokenizer>();

        // LOGIC: v0.3.3a - Sentence tokenizer for readability analysis
        services.AddSingleton<ISentenceTokenizer, SentenceTokenizer>();

        // LOGIC: v0.3.3b - Syllable counter for readability metrics
        services.AddSingleton<ISyllableCounter, SyllableCounter>();

        // LOGIC: v0.3.3c - Readability calculator service (stateless, thread-safe)
        services.AddSingleton<IReadabilityService, ReadabilityService>();

        // LOGIC: v0.3.1c - Fuzzy scanner for terminology matching
        services.AddSingleton<IFuzzyScanner, FuzzyScanner>();

        // LOGIC: v0.2.2b - Memory cache for terminology lookups
        services.AddMemoryCache();

        // LOGIC: v0.2.2b - Configure terminology cache options
        services.Configure<TerminologyCacheOptions>(options => { });

        // LOGIC: v0.2.2b - Terminology repository with caching
        services.AddSingleton<ITerminologyRepository, TerminologyRepository>();

        // LOGIC: v0.2.2c - Terminology seeder with embedded defaults
        services.AddSingleton<ITerminologySeeder, TerminologySeeder>();

        // LOGIC: v0.2.2d - Terminology CRUD service with validation and events
        services.AddSingleton<ITerminologyService, TerminologyService>();

        // LOGIC: v0.2.3a - Configure linting options
        services.Configure<LintingOptions>(options => { });

        // LOGIC: v0.2.3b - Linting configuration as service
        services.AddSingleton<ILintingConfiguration>(sp =>
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LintingOptions>>().Value);

        // LOGIC: v0.2.3c - Pattern matching engine with caching
        services.AddSingleton<IScannerService, ScannerService>();

        // LOGIC: v0.2.3d - Violation aggregator with caching
        services.AddSingleton<IViolationAggregator, ViolationAggregator>();

        // LOGIC: v0.2.7a - Thread marshaller for UI/background thread coordination
        services.AddSingleton<IThreadMarshaller, AvaloniaThreadMarshaller>();

        // LOGIC: v0.2.3a - Reactive linting orchestrator
        services.AddSingleton<ILintingOrchestrator, LintingOrchestrator>();

        // LOGIC: v0.2.4a - Violation color provider for editor integration
        services.AddSingleton<IViolationColorProvider, ViolationColorProvider>();

        // LOGIC: v0.2.5b - Configure filter options
        services.Configure<FilterOptions>(options => { });

        // LOGIC: v0.2.5b - Term filter service for grid filtering
        services.AddSingleton<ITermFilterService, TermFilterService>();

        // LOGIC: v0.3.4a - Voice Profile repository and service
        services.AddSingleton<IVoiceProfileRepository, VoiceProfileRepository>();
        services.AddSingleton<IVoiceProfileService, VoiceProfileService>();

        // LOGIC: v0.3.4b - Passive voice detector with pattern matching and confidence scoring
        services.AddSingleton<IPassiveVoiceDetector, PassiveVoiceDetector>();

        // LOGIC: v0.3.4c - Weak word scanner (adverbs, weasel words, fillers)
        services.AddSingleton<IWeakWordScanner, WeakWordScanner>();

        // LOGIC: v0.3.5a - Chart data aggregation service
        services.AddSingleton<IChartDataService, ChartDataService>();
        services.AddSingleton<IResonanceAxisProvider, DefaultAxisProvider>();

        // LOGIC: v0.3.5b - Spider chart series builder (stateless, thread-safe)
        services.AddSingleton<ISpiderChartSeriesBuilder, SpiderChartSeriesBuilder>();

        // LOGIC: v0.3.5b - Target overlay computation service with caching
        services.AddSingleton<ITargetOverlayService, TargetOverlayService>();

        // LOGIC: v0.3.5c - Real-time update service with debouncing
        services.AddSingleton<IResonanceUpdateService, ResonanceUpdateService>();

        // LOGIC: v0.2.5b - Filter ViewModel for filter bar UI
        services.AddTransient<FilterViewModel>();

        // LOGIC: v0.2.5a - Lexicon grid view components
        services.AddTransient<LexiconViewModel>();
        services.AddTransient<LexiconView>();

        // LOGIC: v0.2.5c - Term editor dialog components
        services.AddTransient<TermEditorViewModel>();
        services.AddSingleton<ITermEditorDialogService, TermEditorDialogService>();

        // LOGIC: v0.2.5d - Import/Export services
        services.AddSingleton<ITermImportService, TermImportService>();
        services.AddSingleton<ITermExportService, TermExportService>();

        // LOGIC: v0.2.6a - Problems Panel components
        services.AddTransient<ProblemsPanelViewModel>();
        services.AddTransient<ProblemsPanelView>();

        // LOGIC: v0.2.6c - Scorecard Widget components
        services.AddTransient<IScorecardViewModel, ScorecardViewModel>();
        services.AddTransient<ScorecardWidget>();

        // LOGIC: v0.3.3d - Readability HUD Widget components
        services.AddSingleton<IReadabilityHudViewModel, ReadabilityHudViewModel>();
        services.AddTransient<ReadabilityHudWidget>();

        // LOGIC: v0.3.5b - Resonance Dashboard components
        services.AddTransient<IResonanceDashboardViewModel, ResonanceDashboardViewModel>();
        services.AddTransient<ResonanceDashboardView>();

        // LOGIC: v0.2.6d - Project linting service for scope filtering
        services.AddSingleton<IProjectLintingService, ProjectLintingService>();

        // LOGIC: v0.2.7c - Content filter for frontmatter detection (runs first, priority 100)
        services.AddSingleton<IContentFilter, YamlFrontmatterFilter>();
        services.AddSingleton<YamlFrontmatterFilter>();

        // LOGIC: v0.2.7b - Content filter for code block detection (priority 200)
        services.AddSingleton<IContentFilter, MarkdownCodeBlockFilter>();
        services.AddSingleton<MarkdownCodeBlockFilter>();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Initialization sequence:
    /// 1. Load embedded default rules (lexichord.yaml)
    /// 2. Set as active style sheet
    /// 3. (Future: Check license tier for custom rules)
    /// 4. (Future: Start file watcher if licensed)
    ///
    /// Errors during initialization are logged but don't crash the module.
    /// Writing with no style checking is better than not writing at all.
    /// </remarks>
    public async Task InitializeAsync(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<StyleModule>>();
        _logger.LogInformation("Initializing Style module v{Version} (The Rulebook)", Info.Version);

        // LOGIC: v0.3.1d - Initialize license-gated UI controls with license context
        var licenseContext = provider.GetRequiredService<ILicenseContext>();
        FeatureGate.Initialize(licenseContext);
        UpgradePromptDialog.Initialize(licenseContext);
        _logger.LogDebug("Initialized license-gated UI controls");


        try
        {
            var engine = provider.GetRequiredService<IStyleEngine>();
            var loader = provider.GetRequiredService<IStyleSheetLoader>();

            // LOGIC: Load embedded default rules
            _logger.LogDebug("Loading embedded default style sheet...");
            var defaultSheet = await loader.LoadEmbeddedDefaultAsync();

            // LOGIC: Activate the default sheet
            engine.SetActiveStyleSheet(defaultSheet);
            _logger.LogInformation(
                "Style module initialized with {RuleCount} rules from '{SheetName}'",
                defaultSheet.Rules.Count,
                defaultSheet.Name);

            // LOGIC: v0.2.2c - Seed terminology database with default terms
            var seeder = provider.GetRequiredService<ITerminologySeeder>();
            var seedResult = await seeder.SeedIfEmptyAsync();
            if (seedResult.WasEmpty)
            {
                _logger.LogInformation(
                    "Seeded {Count} style terms in {Duration}ms",
                    seedResult.TermsSeeded,
                    seedResult.Duration.TotalMilliseconds);
            }

            // LOGIC: v0.2.1d - Start file watcher for live reload
            // The watcher internally checks for WriterPro license tier
            var watcher = provider.GetRequiredService<IStyleConfigurationWatcher>();
            var workspaceService = provider.GetService<IWorkspaceService>();
            
            if (workspaceService?.CurrentWorkspace?.RootPath is { } projectRoot)
            {
                _logger.LogDebug("Starting style configuration watcher for '{Path}'", projectRoot);
                watcher.StartWatching(projectRoot);
                
                // LOGIC: Try to load custom rules if they exist
                var customStylePath = Path.Combine(projectRoot, ".lexichord", "style.yaml");
                if (File.Exists(customStylePath))
                {
                    try
                    {
                        _logger.LogDebug("Loading custom style rules from '{Path}'", customStylePath);
                        var customSheet = await loader.LoadFromFileAsync(customStylePath);
                        engine.SetActiveStyleSheet(customSheet);
                        _logger.LogInformation(
                            "Loaded custom style sheet with {RuleCount} rules from '{SheetName}'",
                            customSheet.Rules.Count, customSheet.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load custom style rules, using defaults");
                    }
                }
            }
            else
            {
                _logger.LogDebug("No workspace open, skipping style watcher initialization");
            }

            // LOGIC: v0.2.5a - Register LexiconView in Right dock region
            var regionManager = provider.GetService<IRegionManager>();
            if (regionManager is not null)
            {
                await regionManager.RegisterToolAsync(
                    ShellRegion.Right,
                    "lexichord.lexicon",
                    "Lexicon",
                    sp => sp.GetRequiredService<LexiconView>());
                _logger.LogDebug("Registered LexiconView in Right dock region");

                // LOGIC: v0.2.6a - Register ProblemsPanel in Right dock region
                await regionManager.RegisterToolAsync(
                    ShellRegion.Right,
                    "lexichord.problems",
                    "Problems",
                    sp => sp.GetRequiredService<ProblemsPanelView>());
                _logger.LogDebug("Registered ProblemsPanelView in Right dock region");
            }
        }
        catch (Exception ex)
        {
            // LOGIC: Log but don't throw - style checking is optional
            _logger.LogError(ex, "Failed to initialize Style module. Style checking will be disabled.");
        }
    }
}
