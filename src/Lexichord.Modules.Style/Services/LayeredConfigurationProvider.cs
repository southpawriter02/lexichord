using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Events;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Provides merged configuration from system, user, and project sources.
/// </summary>
/// <remarks>
/// LOGIC: This service implements hierarchical configuration loading with caching.
///
/// Layer Priority (highest wins):
/// 1. Project: {workspace}/.lexichord/style.yaml (requires Writer Pro)
/// 2. User: Settings from IConfiguration/appsettings
/// 3. System: Embedded StyleConfiguration.Defaults
///
/// Merge Strategy:
/// - Scalar values: Higher source overrides lower source
/// - Nullable scalars: Non-null higher values override lower
/// - Lists (Exclusions, IgnoredRules): Concatenate and deduplicate
///
/// Security:
/// - File size limit: 100KB max for configuration files
/// - YAML parse errors are logged and gracefully handled
///
/// Performance:
/// - Results cached with 5-second TTL
/// - Cache invalidated on workspace change
///
/// Thread Safety:
/// - All public methods are thread-safe via lock
///
/// Version: v0.3.6a
/// </remarks>
public sealed class LayeredConfigurationProvider : ILayeredConfigurationProvider
{
    private readonly IWorkspaceService _workspaceService;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<LayeredConfigurationProvider> _logger;

    private readonly object _cacheLock = new();
    private StyleConfiguration? _cachedConfig;
    private DateTime _cacheTime;

    private const int MaxConfigFileSizeBytes = 100 * 1024; // 100KB
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <summary>
    /// Initializes a new instance of <see cref="LayeredConfigurationProvider"/>.
    /// </summary>
    /// <param name="workspaceService">Service for workspace state.</param>
    /// <param name="licenseContext">License context for tier checks.</param>
    /// <param name="logger">Logger instance.</param>
    /// <remarks>
    /// LOGIC: Dependencies injected via DI. All are required for proper operation.
    /// </remarks>
    public LayeredConfigurationProvider(
        IWorkspaceService workspaceService,
        ILicenseContext licenseContext,
        ILogger<LayeredConfigurationProvider> logger)
    {
        _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("LayeredConfigurationProvider initialized");
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Returns merged configuration with caching.
    /// Cache is checked first; if valid, returns cached result.
    /// Otherwise, loads and merges all layers.
    /// </remarks>
    public StyleConfiguration GetEffectiveConfiguration()
    {
        lock (_cacheLock)
        {
            if (_cachedConfig != null && DateTime.UtcNow - _cacheTime < CacheDuration)
            {
                _logger.LogDebug("Returning cached configuration (age: {Age}ms)",
                    (DateTime.UtcNow - _cacheTime).TotalMilliseconds);
                return _cachedConfig;
            }
        }

        var stopwatch = Stopwatch.StartNew();

        // 1. Start with system defaults
        var merged = StyleConfiguration.Defaults;
        _logger.LogDebug("Loading configuration from {Source}", ConfigurationSource.System);

        // 2. Merge user configuration
        var userConfig = LoadUserConfiguration();
        if (userConfig != null)
        {
            merged = MergeConfigurations(merged, userConfig);
            _logger.LogDebug("Configuration merged from {Source}: {KeyCount} potential overrides",
                ConfigurationSource.User, CountNonDefaultValues(userConfig));
        }

        // 3. Merge project configuration (if licensed and workspace open)
        if (_licenseContext.IsFeatureEnabled(FeatureCodes.GlobalDictionary))
        {
            if (_workspaceService.IsWorkspaceOpen)
            {
                var projectConfig = LoadProjectConfiguration();
                if (projectConfig != null)
                {
                    merged = MergeConfigurations(merged, projectConfig);
                    _logger.LogDebug("Configuration merged from {Source}: {KeyCount} potential overrides",
                        ConfigurationSource.Project, CountNonDefaultValues(projectConfig));
                }
            }
            else
            {
                _logger.LogDebug("Project configuration skipped: no workspace open");
            }
        }
        else
        {
            _logger.LogDebug("Project configuration skipped: GlobalDictionary feature not enabled");
        }

        stopwatch.Stop();
        _logger.LogInformation("Effective configuration loaded in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

        // Cache the result
        lock (_cacheLock)
        {
            _cachedConfig = merged;
            _cacheTime = DateTime.UtcNow;
        }

        return merged;
    }

    /// <inheritdoc />
    public StyleConfiguration? GetConfiguration(ConfigurationSource source)
    {
        return source switch
        {
            ConfigurationSource.System => StyleConfiguration.Defaults,
            ConfigurationSource.User => LoadUserConfiguration(),
            ConfigurationSource.Project => LoadProjectConfiguration(),
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown configuration source")
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Clears cache and notifies subscribers.
    /// Called when:
    /// - Workspace opened/closed
    /// - Configuration file changed (file watcher)
    /// - License state changed
    /// </remarks>
    public void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedConfig = null;
            _logger.LogDebug("Configuration cache invalidated");
        }

        ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
        {
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Loads user-level configuration from settings.
    /// </summary>
    /// <returns>User configuration or null if not available.</returns>
    /// <remarks>
    /// LOGIC: Currently returns null as user config is not yet implemented.
    /// Future: Load from IConfiguration "Style" section or user config file.
    /// </remarks>
    private StyleConfiguration? LoadUserConfiguration()
    {
        // TODO: v0.3.6 - Load from IConfiguration or user config file
        // For now, return null (no user-level overrides)
        _logger.LogDebug("User configuration: not yet implemented, returning null");
        return null;
    }

    /// <summary>
    /// Loads project-level configuration from workspace.
    /// </summary>
    /// <returns>Project configuration or null if not available.</returns>
    /// <remarks>
    /// LOGIC: Loads from {workspace}/.lexichord/style.yaml if:
    /// - Workspace is open
    /// - File exists
    /// - File is under size limit
    /// - YAML parses successfully
    /// </remarks>
    private StyleConfiguration? LoadProjectConfiguration()
    {
        if (!_workspaceService.IsWorkspaceOpen ||
            _workspaceService.CurrentWorkspace?.RootPath == null)
        {
            return null;
        }

        var projectPath = Path.Combine(
            _workspaceService.CurrentWorkspace.RootPath,
            ".lexichord",
            "style.yaml");

        if (!File.Exists(projectPath))
        {
            _logger.LogDebug("Project configuration not found: {Path}", projectPath);
            return null;
        }

        try
        {
            // Security: Check file size
            var fileInfo = new FileInfo(projectPath);
            if (fileInfo.Length > MaxConfigFileSizeBytes)
            {
                _logger.LogWarning("Configuration file too large: {Path} ({Size} bytes, max: {Max})",
                    projectPath, fileInfo.Length, MaxConfigFileSizeBytes);
                return null;
            }

            _logger.LogDebug("Loading configuration from {Source}: {Path}",
                ConfigurationSource.Project, projectPath);

            var yaml = File.ReadAllText(projectPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<StyleConfiguration>(yaml);
        }
        catch (YamlException ex)
        {
            _logger.LogWarning("Failed to parse project configuration from {Path}: {Error} at line {Line}",
                projectPath, ex.Message, ex.Start.Line);
            return null;
        }
        catch (IOException ex)
        {
            _logger.LogWarning("Failed to read project configuration from {Path}: {Error}",
                projectPath, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Unexpected error loading project configuration from {Path}: {Error}",
                projectPath, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Merges two configurations with higher-priority values taking precedence.
    /// </summary>
    /// <param name="lower">Lower-priority configuration (e.g., System).</param>
    /// <param name="higher">Higher-priority configuration (e.g., User or Project).</param>
    /// <returns>Merged configuration.</returns>
    /// <remarks>
    /// LOGIC: Merge strategy:
    /// - For nullable scalars: non-null higher value wins
    /// - For non-nullable scalars (with defaults): higher value always wins
    /// - For lists: concatenate and deduplicate
    /// </remarks>
    private static StyleConfiguration MergeConfigurations(
        StyleConfiguration lower,
        StyleConfiguration higher)
    {
        return lower with
        {
            // Version: always use higher
            Version = higher.Version,

            // Profile settings: nullable, so non-null higher wins
            DefaultProfile = higher.DefaultProfile ?? lower.DefaultProfile,
            AllowProfileSwitching = higher.AllowProfileSwitching,

            // Readability constraints: nullable scalars
            TargetGradeLevel = higher.TargetGradeLevel ?? lower.TargetGradeLevel,
            MaxSentenceLength = higher.MaxSentenceLength ?? lower.MaxSentenceLength,
            GradeLevelTolerance = higher.GradeLevelTolerance,

            // Voice analysis: non-nullable, higher wins
            PassiveVoiceThreshold = higher.PassiveVoiceThreshold,
            FlagAdverbs = higher.FlagAdverbs,
            FlagWeaselWords = higher.FlagWeaselWords,

            // Lists: concatenate and deduplicate
            TerminologyAdditions = higher.TerminologyAdditions.Count > 0
                ? higher.TerminologyAdditions
                    .Concat(lower.TerminologyAdditions)
                    .DistinctBy(t => t.Pattern)
                    .ToList()
                : lower.TerminologyAdditions,

            TerminologyExclusions = higher.TerminologyExclusions
                .Concat(lower.TerminologyExclusions)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),

            IgnoredRules = higher.IgnoredRules
                .Concat(lower.IgnoredRules)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    /// <summary>
    /// Counts non-default values in a configuration for logging purposes.
    /// </summary>
    private static int CountNonDefaultValues(StyleConfiguration config)
    {
        var count = 0;
        var defaults = StyleConfiguration.Defaults;

        if (config.DefaultProfile != defaults.DefaultProfile) count++;
        if (config.AllowProfileSwitching != defaults.AllowProfileSwitching) count++;
        if (config.TargetGradeLevel != defaults.TargetGradeLevel) count++;
        if (config.MaxSentenceLength != defaults.MaxSentenceLength) count++;
        if (config.GradeLevelTolerance != defaults.GradeLevelTolerance) count++;
        if (config.PassiveVoiceThreshold != defaults.PassiveVoiceThreshold) count++;
        if (config.FlagAdverbs != defaults.FlagAdverbs) count++;
        if (config.FlagWeaselWords != defaults.FlagWeaselWords) count++;
        if (config.TerminologyAdditions.Count > 0) count++;
        if (config.TerminologyExclusions.Count > 0) count++;
        if (config.IgnoredRules.Count > 0) count++;

        return count;
    }
}
