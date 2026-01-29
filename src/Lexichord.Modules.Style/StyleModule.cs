using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Data;
using Lexichord.Modules.Style.Services;
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
        Version: new Version(0, 2, 1),
        Author: "Lexichord Team",
        Description: "Style and writing rules engine providing governed writing environments"
    );

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: All services are singletons because:
    /// - IStyleEngine: Maintains active sheet state, thread-safe analysis
    /// - IStyleSheetLoader: Caches compiled patterns, expensive to create
    /// - IStyleConfigurationWatcher: Holds OS file watcher resources
    /// </remarks>
    public void RegisterServices(IServiceCollection services)
    {
        // LOGIC: Core style analysis engine
        services.AddSingleton<IStyleEngine, StyleEngine>();

        // LOGIC: YAML deserialization and schema validation
        services.AddSingleton<IStyleSheetLoader, YamlStyleSheetLoader>();

        // LOGIC: File system watcher for live reload
        services.AddSingleton<IStyleConfigurationWatcher, FileSystemStyleWatcher>();

        // LOGIC: v0.2.2b - Memory cache for terminology lookups
        services.AddMemoryCache();

        // LOGIC: v0.2.2b - Configure terminology cache options
        services.Configure<TerminologyCacheOptions>(options => { });

        // LOGIC: v0.2.2b - Terminology repository with caching
        services.AddSingleton<ITerminologyRepository, TerminologyRepository>();

        // LOGIC: v0.2.2c - Terminology seeder with embedded defaults
        services.AddSingleton<ITerminologySeeder, TerminologySeeder>();
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
        }
        catch (Exception ex)
        {
            // LOGIC: Log but don't throw - style checking is optional
            _logger.LogError(ex, "Failed to initialize Style module. Style checking will be disabled.");
        }
    }
}
