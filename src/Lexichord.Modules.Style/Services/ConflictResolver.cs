using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Resolves conflicts between configuration layers using project-wins semantics.
/// </summary>
/// <remarks>
/// LOGIC: Implements hierarchical conflict resolution with the following priorities:
///
/// 1. Project configuration (highest priority, requires Writer Pro)
/// 2. User configuration
/// 3. System defaults (lowest priority)
///
/// Key features:
/// - Generic value resolution with null-coalescing
/// - Conflict detection and logging for visibility
/// - Term override logic (exclusions take precedence over additions)
/// - Rule ignore patterns with wildcard support (* prefix/suffix)
///
/// Thread Safety:
/// - All methods are stateless and thread-safe
/// - No internal mutable state
///
/// Version: v0.3.6b
/// </remarks>
public sealed class ConflictResolver : IConflictResolver
{
    private readonly ITerminologyRepository? _terminologyRepository;
    private readonly ILogger<ConflictResolver>? _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ConflictResolver"/>.
    /// </summary>
    /// <param name="terminologyRepository">Optional terminology repository for global term lookups.</param>
    /// <param name="logger">Optional logger for conflict reporting.</param>
    /// <remarks>
    /// LOGIC: Dependencies are optional to support testing and scenarios
    /// where the full dependency chain is not available.
    /// </remarks>
    public ConflictResolver(
        ITerminologyRepository? terminologyRepository = null,
        ILogger<ConflictResolver>? logger = null)
    {
        _terminologyRepository = terminologyRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Simple null-coalescing: higher wins when non-null.
    /// </remarks>
    public T? Resolve<T>(T? higher, T? lower)
    {
        return higher ?? lower;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Extends Resolve with guaranteed non-null return via default.
    /// </remarks>
    public T ResolveWithDefault<T>(T? higher, T? lower, T defaultValue)
    {
        return higher ?? lower ?? defaultValue;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Compares configuration layers pairwise and collects conflicts.
    /// Comparison order:
    /// 1. Project vs User (if both present)
    /// 2. Effective higher (Project or User) vs System
    /// </remarks>
    public IReadOnlyList<ConfigurationConflict> DetectConflicts(
        StyleConfiguration? project,
        StyleConfiguration? user,
        StyleConfiguration system)
    {
        var conflicts = new List<ConfigurationConflict>();

        // Compare Project vs User
        if (project != null && user != null)
        {
            DetectLayerConflicts(conflicts, project, user,
                ConfigurationSource.Project, ConfigurationSource.User);
        }

        // Compare effective higher vs System
        var effectiveHigher = project ?? user;
        if (effectiveHigher != null)
        {
            var higherSource = project != null
                ? ConfigurationSource.Project
                : ConfigurationSource.User;

            DetectLayerConflicts(conflicts, effectiveHigher, system,
                higherSource, ConfigurationSource.System);
        }

        // Log significant conflicts
        foreach (var conflict in conflicts.Where(c => c.IsSignificant))
        {
            _logger?.LogDebug("Configuration conflict detected: {Description}", conflict.Description);
        }

        return conflicts.AsReadOnly();
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Term override resolution priority:
    /// 1. Exclusions → term is ALLOWED (not flagged)
    /// 2. Additions → term is FORBIDDEN (flagged)
    /// 3. Global repository → check if term exists
    ///
    /// Note: Since effectiveConfig contains merged lists, we check exclusions
    /// first (they always win), then additions, then fall back to repository.
    /// </remarks>
    public bool ShouldFlagTerm(string term, StyleConfiguration effectiveConfig)
    {
        if (string.IsNullOrEmpty(term))
        {
            return false;
        }

        // Check exclusions first - exclusions always allow the term
        if (effectiveConfig.TerminologyExclusions.Any(e =>
            string.Equals(e, term, StringComparison.OrdinalIgnoreCase)))
        {
            _logger?.LogDebug("Term '{Term}' excluded by configuration", term);
            return false;
        }

        // Check additions - additions forbid the term
        if (effectiveConfig.TerminologyAdditions.Any(a =>
            string.Equals(a.Pattern, term, StringComparison.OrdinalIgnoreCase)))
        {
            _logger?.LogDebug("Term '{Term}' flagged by configuration addition", term);
            return true;
        }

        // Fall back to global terminology repository
        if (_terminologyRepository != null)
        {
            var activeTerms = _terminologyRepository
                .GetAllActiveTermsAsync()
                .GetAwaiter()
                .GetResult();

            var inGlobal = activeTerms.Any(t =>
                string.Equals(t.Term, term, StringComparison.OrdinalIgnoreCase));

            if (inGlobal)
            {
                _logger?.LogDebug("Term '{Term}' flagged by global terminology", term);
            }

            return inGlobal;
        }

        return false;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Checks IgnoredRules for pattern matches.
    /// Supports:
    /// - Exact match (case-insensitive)
    /// - Prefix wildcard: "PASSIVE-*"
    /// - Suffix wildcard: "*-WARNINGS"
    /// - Global wildcard: "*"
    /// </remarks>
    public bool IsRuleIgnored(string ruleId, StyleConfiguration effectiveConfig)
    {
        if (string.IsNullOrEmpty(ruleId))
        {
            return false;
        }

        foreach (var pattern in effectiveConfig.IgnoredRules)
        {
            if (MatchesPattern(ruleId, pattern))
            {
                _logger?.LogDebug("Rule '{RuleId}' matches ignore pattern '{Pattern}'",
                    ruleId, pattern);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Detects conflicts between two configuration layers.
    /// </summary>
    /// <param name="conflicts">List to append detected conflicts to.</param>
    /// <param name="higher">Higher-priority configuration.</param>
    /// <param name="lower">Lower-priority configuration.</param>
    /// <param name="higherSource">Source identifier for higher layer.</param>
    /// <param name="lowerSource">Source identifier for lower layer.</param>
    private static void DetectLayerConflicts(
        List<ConfigurationConflict> conflicts,
        StyleConfiguration higher,
        StyleConfiguration lower,
        ConfigurationSource higherSource,
        ConfigurationSource lowerSource)
    {
        // Profile settings
        AddConflictIfDifferent(conflicts, "DefaultProfile",
            higher.DefaultProfile, lower.DefaultProfile, higherSource, lowerSource);

        AddConflictIfDifferent(conflicts, "AllowProfileSwitching",
            higher.AllowProfileSwitching, lower.AllowProfileSwitching, higherSource, lowerSource);

        // Readability constraints
        AddConflictIfDifferent(conflicts, "TargetGradeLevel",
            higher.TargetGradeLevel, lower.TargetGradeLevel, higherSource, lowerSource);

        AddConflictIfDifferent(conflicts, "MaxSentenceLength",
            higher.MaxSentenceLength, lower.MaxSentenceLength, higherSource, lowerSource);

        AddConflictIfDifferent(conflicts, "GradeLevelTolerance",
            higher.GradeLevelTolerance, lower.GradeLevelTolerance, higherSource, lowerSource);

        // Voice analysis settings
        AddConflictIfDifferent(conflicts, "PassiveVoiceThreshold",
            higher.PassiveVoiceThreshold, lower.PassiveVoiceThreshold, higherSource, lowerSource);

        AddConflictIfDifferent(conflicts, "FlagAdverbs",
            higher.FlagAdverbs, lower.FlagAdverbs, higherSource, lowerSource);

        AddConflictIfDifferent(conflicts, "FlagWeaselWords",
            higher.FlagWeaselWords, lower.FlagWeaselWords, higherSource, lowerSource);
    }

    /// <summary>
    /// Adds a conflict to the list if the values are different.
    /// </summary>
    private static void AddConflictIfDifferent<T>(
        List<ConfigurationConflict> conflicts,
        string key,
        T? higherValue,
        T? lowerValue,
        ConfigurationSource higherSource,
        ConfigurationSource lowerSource)
    {
        if (!Equals(higherValue, lowerValue))
        {
            conflicts.Add(new ConfigurationConflict(
                key, higherSource, lowerSource, higherValue, lowerValue));
        }
    }

    /// <summary>
    /// Checks if a rule ID matches a pattern (supports wildcards).
    /// </summary>
    /// <param name="ruleId">The rule ID to check.</param>
    /// <param name="pattern">The pattern to match against.</param>
    /// <returns>True if the rule ID matches the pattern.</returns>
    private static bool MatchesPattern(string ruleId, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        // Global wildcard
        if (pattern == "*")
        {
            return true;
        }

        // Suffix wildcard: "PASSIVE-*" matches "PASSIVE-001"
        if (pattern.EndsWith('*'))
        {
            var prefix = pattern[..^1];
            return ruleId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        // Prefix wildcard: "*-WARNINGS" matches "STYLE-WARNINGS"
        if (pattern.StartsWith('*'))
        {
            var suffix = pattern[1..];
            return ruleId.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        // Exact match (case-insensitive)
        return ruleId.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
