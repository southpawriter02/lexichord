using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Writes configuration overrides to project-level files.
/// </summary>
/// <remarks>
/// LOGIC: Implements <see cref="IProjectConfigurationWriter"/> to enable
/// users to ignore rules and exclude terms for specific projects via
/// the Override UI context menu.
///
/// Key implementation details:
/// - Configuration is stored in {workspace}/.lexichord/style.yaml
/// - All writes are atomic (temp file + rename) to prevent corruption
/// - Thread-safe via internal lock object
/// - YAML serialization uses YamlDotNet with underscore naming convention
///
/// File format example:
/// <code>
/// # .lexichord/style.yaml
/// version: 1
/// ignored_rules:
///   - TERM-001
///   - PASSIVE-003
/// terminology:
///   exclusions:
///     - whitelist
///     - blacklist
/// </code>
///
/// Thread Safety:
/// - _writeLock protects all file write operations
/// - Read operations are safe without locking (immutable data)
///
/// Version: v0.3.6c
/// </remarks>
public sealed class ProjectConfigurationWriter : IProjectConfigurationWriter
{
    private readonly IWorkspaceService _workspaceService;
    private readonly ILogger<ProjectConfigurationWriter>? _logger;
    private readonly object _writeLock = new();

    /// <summary>
    /// Initializes a new instance of <see cref="ProjectConfigurationWriter"/>.
    /// </summary>
    /// <param name="workspaceService">Service for workspace path detection.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <remarks>
    /// LOGIC: Logger is optional to support testing scenarios.
    /// </remarks>
    public ProjectConfigurationWriter(
        IWorkspaceService workspaceService,
        ILogger<ProjectConfigurationWriter>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(workspaceService);

        _workspaceService = workspaceService;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Adds rule to ignored_rules list if not already present.
    /// List is sorted alphabetically for consistency.
    /// </remarks>
    public async Task<bool> IgnoreRuleAsync(string ruleId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            _logger?.LogDebug("IgnoreRuleAsync called with empty ruleId, returning false");
            return false;
        }

        _logger?.LogDebug("Ignoring rule {RuleId} for project", ruleId);

        return await ModifyConfigurationAsync(config =>
        {
            var rules = config.IgnoredRules.ToList();
            if (!rules.Contains(ruleId, StringComparer.OrdinalIgnoreCase))
            {
                rules.Add(ruleId);
                rules.Sort(StringComparer.OrdinalIgnoreCase);
                _logger?.LogInformation("Rule {RuleId} added to project ignore list", ruleId);
                return config with { IgnoredRules = rules };
            }

            _logger?.LogDebug("Rule {RuleId} already in ignore list, no change", ruleId);
            return config;
        }, ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Removes rule from ignored_rules list using case-insensitive matching.
    /// </remarks>
    public async Task<bool> RestoreRuleAsync(string ruleId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            return false;
        }

        _logger?.LogDebug("Restoring rule {RuleId} for project", ruleId);

        return await ModifyConfigurationAsync(config =>
        {
            var rules = config.IgnoredRules
                .Where(r => !r.Equals(ruleId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (rules.Count < config.IgnoredRules.Count)
            {
                _logger?.LogInformation("Rule {RuleId} removed from project ignore list", ruleId);
            }

            return config with { IgnoredRules = rules };
        }, ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Adds term to terminology.exclusions list if not already present.
    /// List is sorted alphabetically for consistency.
    /// </remarks>
    public async Task<bool> ExcludeTermAsync(string term, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            _logger?.LogDebug("ExcludeTermAsync called with empty term, returning false");
            return false;
        }

        _logger?.LogDebug("Excluding term '{Term}' for project", term);

        return await ModifyConfigurationAsync(config =>
        {
            var exclusions = config.TerminologyExclusions.ToList();
            if (!exclusions.Contains(term, StringComparer.OrdinalIgnoreCase))
            {
                exclusions.Add(term);
                exclusions.Sort(StringComparer.OrdinalIgnoreCase);
                _logger?.LogInformation("Term '{Term}' added to project exclusions", term);
                return config with { TerminologyExclusions = exclusions };
            }

            _logger?.LogDebug("Term '{Term}' already in exclusions list, no change", term);
            return config;
        }, ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Removes term from terminology.exclusions list using case-insensitive matching.
    /// </remarks>
    public async Task<bool> RestoreTermAsync(string term, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        _logger?.LogDebug("Restoring term '{Term}' for project", term);

        return await ModifyConfigurationAsync(config =>
        {
            var exclusions = config.TerminologyExclusions
                .Where(t => !t.Equals(term, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (exclusions.Count < config.TerminologyExclusions.Count)
            {
                _logger?.LogInformation("Term '{Term}' removed from project exclusions", term);
            }

            return config with { TerminologyExclusions = exclusions };
        }, ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Reads current project config and checks for exact match in ignored_rules.
    /// </remarks>
    public bool IsRuleIgnored(string ruleId)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            return false;
        }

        var config = LoadProjectConfiguration();
        return config?.IgnoredRules.Contains(ruleId, StringComparer.OrdinalIgnoreCase) ?? false;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Reads current project config and checks for case-insensitive match in exclusions.
    /// </remarks>
    public bool IsTermExcluded(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        var config = LoadProjectConfiguration();
        return config?.TerminologyExclusions.Contains(term, StringComparer.OrdinalIgnoreCase) ?? false;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Creates .lexichord/ directory and style.yaml with defaults if needed.
    /// </remarks>
    public async Task<string> EnsureConfigurationFileAsync(CancellationToken ct = default)
    {
        var configPath = GetConfigurationFilePath();
        if (configPath == null)
        {
            throw new InvalidOperationException("No workspace is open");
        }

        var configDir = Path.GetDirectoryName(configPath)!;

        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
            _logger?.LogDebug("Created project configuration directory: {Path}", configDir);
        }

        if (!File.Exists(configPath))
        {
            var defaultConfig = new StyleConfiguration { Version = 1 };
            await WriteConfigurationAsync(configPath, defaultConfig, ct);
            _logger?.LogDebug("Created project configuration file: {Path}", configPath);
        }

        return configPath;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Returns path based on current workspace root.
    /// Returns null if no workspace is open.
    /// </remarks>
    public string? GetConfigurationFilePath()
    {
        if (!_workspaceService.IsWorkspaceOpen || _workspaceService.CurrentWorkspace == null)
        {
            return null;
        }

        return Path.Combine(_workspaceService.CurrentWorkspace.RootPath, ".lexichord", "style.yaml");
    }

    /// <summary>
    /// Modifies the project configuration using the provided modifier function.
    /// </summary>
    /// <param name="modifier">Function that transforms the current configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if modification succeeded; false otherwise.</returns>
    /// <remarks>
    /// LOGIC: Thread-safe modification with atomic write.
    /// The lock ensures only one write operation at a time.
    /// </remarks>
    private async Task<bool> ModifyConfigurationAsync(
        Func<StyleConfiguration, StyleConfiguration> modifier,
        CancellationToken ct)
    {
        var configPath = await EnsureConfigurationFileAsync(ct);

        lock (_writeLock)
        {
            try
            {
                var config = LoadProjectConfiguration() ?? new StyleConfiguration { Version = 1 };
                var modified = modifier(config);

                WriteConfigurationAsync(configPath, modified, ct).GetAwaiter().GetResult();
                _logger?.LogDebug("Updated project configuration file: {Path}", configPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to write project configuration: {Error}", ex.Message);
                return false;
            }
        }
    }

    /// <summary>
    /// Loads the current project configuration from disk.
    /// </summary>
    /// <returns>The parsed configuration, or null if file doesn't exist.</returns>
    /// <remarks>
    /// LOGIC: Uses YamlDotNet with underscore naming convention.
    /// Ignores unmatched properties for forward compatibility.
    ///
    /// NOTE: We use a mutable DTO because YamlDotNet cannot deserialize
    /// directly into records with IReadOnlyList properties.
    /// </remarks>
    private StyleConfiguration? LoadProjectConfiguration()
    {
        var configPath = GetConfigurationFilePath();
        if (configPath == null || !File.Exists(configPath))
        {
            return null;
        }

        try
        {
            var yaml = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var dto = deserializer.Deserialize<StyleConfigurationDto>(yaml);
            return dto?.ToRecord();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to parse project configuration: {Error}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Writes configuration to disk using atomic write pattern.
    /// </summary>
    /// <param name="path">Target path for the configuration file.</param>
    /// <param name="config">Configuration to write.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// LOGIC: Atomic write sequence:
    /// 1. Serialize to YAML with header comment
    /// 2. Write to .tmp file
    /// 3. Rename .tmp to target (atomic on most filesystems)
    ///
    /// This prevents corruption if the process crashes mid-write.
    /// </remarks>
    private static async Task WriteConfigurationAsync(
        string path,
        StyleConfiguration config,
        CancellationToken ct)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        var yaml = serializer.Serialize(config);

        // Add header comment
        yaml = "# .lexichord/style.yaml\n" +
               "# Project-level style configuration\n" +
               "# This file is auto-generated but can be manually edited\n\n" +
               yaml;

        // Atomic write using temp file + rename
        var tempPath = path + ".tmp";
        await File.WriteAllTextAsync(tempPath, yaml, ct);
        File.Move(tempPath, path, overwrite: true);
    }
}

/// <summary>
/// Mutable DTO for YAML deserialization of StyleConfiguration.
/// </summary>
/// <remarks>
/// LOGIC: YamlDotNet cannot deserialize into IReadOnlyList properties
/// of records. This mutable class serves as an intermediate representation
/// that can be properly deserialized, then converted to the immutable record.
///
/// Version: v0.3.6c
/// </remarks>
internal class StyleConfigurationDto
{
    public int Version { get; set; } = 1;
    public string? DefaultProfile { get; set; }
    public bool AllowProfileSwitching { get; set; } = true;
    public double? TargetGradeLevel { get; set; }
    public int? MaxSentenceLength { get; set; }
    public double GradeLevelTolerance { get; set; } = 2;
    public double PassiveVoiceThreshold { get; set; } = 20;
    public bool FlagAdverbs { get; set; } = true;
    public bool FlagWeaselWords { get; set; } = true;
    public List<string> TerminologyExclusions { get; set; } = [];
    public List<string> IgnoredRules { get; set; } = [];

    /// <summary>
    /// Converts this mutable DTO to an immutable StyleConfiguration record.
    /// </summary>
    public StyleConfiguration ToRecord() => new()
    {
        Version = Version,
        DefaultProfile = DefaultProfile,
        AllowProfileSwitching = AllowProfileSwitching,
        TargetGradeLevel = TargetGradeLevel,
        MaxSentenceLength = MaxSentenceLength,
        GradeLevelTolerance = GradeLevelTolerance,
        PassiveVoiceThreshold = PassiveVoiceThreshold,
        FlagAdverbs = FlagAdverbs,
        FlagWeaselWords = FlagWeaselWords,
        TerminologyExclusions = TerminologyExclusions,
        IgnoredRules = IgnoredRules
    };
}
