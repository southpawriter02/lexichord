namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Resolves conflicts between configuration layers using project-wins semantics.
/// </summary>
/// <remarks>
/// LOGIC: This interface provides conflict resolution for hierarchical configuration.
/// The resolution strategy follows a strict priority order:
///
/// Priority (highest to lowest):
/// 1. Project configuration (requires Writer Pro license)
/// 2. User configuration
/// 3. System defaults
///
/// Key capabilities:
/// - Generic value resolution with null handling
/// - Conflict detection and reporting
/// - Term override logic (additions/exclusions)
/// - Rule ignore patterns with wildcard support
///
/// Thread Safety:
/// - All methods are stateless and thread-safe
/// - No internal mutable state
///
/// Version: v0.3.6b
/// </remarks>
public interface IConflictResolver
{
    /// <summary>
    /// Resolves a configuration value between two layers.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="higher">Value from the higher-priority layer.</param>
    /// <param name="lower">Value from the lower-priority layer.</param>
    /// <returns>
    /// The higher value if non-null; otherwise, the lower value.
    /// </returns>
    /// <remarks>
    /// LOGIC: Simple null-coalescing resolution.
    /// Higher priority values always win when non-null.
    /// </remarks>
    T? Resolve<T>(T? higher, T? lower);

    /// <summary>
    /// Resolves a configuration value with a default fallback.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="higher">Value from the higher-priority layer.</param>
    /// <param name="lower">Value from the lower-priority layer.</param>
    /// <param name="defaultValue">Default value if both layers are null.</param>
    /// <returns>
    /// The higher value if non-null; otherwise, the lower value if non-null;
    /// otherwise, the default value.
    /// </returns>
    /// <remarks>
    /// LOGIC: Extends Resolve with a guaranteed non-null return via default.
    /// Useful for settings that must always have a value.
    /// </remarks>
    T ResolveWithDefault<T>(T? higher, T? lower, T defaultValue);

    /// <summary>
    /// Detects all conflicts between configuration layers.
    /// </summary>
    /// <param name="project">Project-level configuration (may be null).</param>
    /// <param name="user">User-level configuration (may be null).</param>
    /// <param name="system">System-level configuration (always required).</param>
    /// <returns>
    /// A read-only list of detected conflicts between layers.
    /// </returns>
    /// <remarks>
    /// LOGIC: Compares settings across all available layers and reports
    /// cases where different values exist. Conflicts are informational;
    /// the higher-priority value still wins.
    ///
    /// Comparison order:
    /// 1. Project vs User (if both present)
    /// 2. Effective higher (Project or User) vs System
    /// </remarks>
    IReadOnlyList<ConfigurationConflict> DetectConflicts(
        StyleConfiguration? project,
        StyleConfiguration? user,
        StyleConfiguration system);

    /// <summary>
    /// Determines whether a term should be flagged based on configuration.
    /// </summary>
    /// <param name="term">The term to check.</param>
    /// <param name="effectiveConfig">The merged effective configuration.</param>
    /// <returns>
    /// True if the term should be flagged as a style violation; false otherwise.
    /// </returns>
    /// <remarks>
    /// LOGIC: Term override resolution follows this priority:
    ///
    /// 1. Project exclusions → term is ALLOWED (not flagged)
    /// 2. Project additions → term is FORBIDDEN (flagged)
    /// 3. User exclusions → term is ALLOWED (not flagged)
    /// 4. User additions → term is FORBIDDEN (flagged)
    /// 5. Global terminology repository → check if term exists
    ///
    /// Note: The effectiveConfig contains merged lists from all layers.
    /// This method checks the original layer precedence via naming conventions
    /// or separate layer data if available.
    /// </remarks>
    bool ShouldFlagTerm(string term, StyleConfiguration effectiveConfig);

    /// <summary>
    /// Determines whether a rule should be ignored based on configuration.
    /// </summary>
    /// <param name="ruleId">The rule identifier to check.</param>
    /// <param name="effectiveConfig">The merged effective configuration.</param>
    /// <returns>
    /// True if the rule should be ignored; false otherwise.
    /// </returns>
    /// <remarks>
    /// LOGIC: Checks the IgnoredRules list for matches.
    /// Supports wildcard patterns:
    /// - Exact match: "PASSIVE-001"
    /// - Prefix wildcard: "PASSIVE-*" (matches PASSIVE-001, PASSIVE-002, etc.)
    /// - Suffix wildcard: "*-WARNINGS" (matches STYLE-WARNINGS, VOICE-WARNINGS, etc.)
    /// - Global wildcard: "*" (matches all rules)
    ///
    /// Matching is case-insensitive.
    /// </remarks>
    bool IsRuleIgnored(string ruleId, StyleConfiguration effectiveConfig);
}
