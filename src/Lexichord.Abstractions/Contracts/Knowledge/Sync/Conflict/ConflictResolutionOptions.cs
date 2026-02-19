// =============================================================================
// File: ConflictResolutionOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for conflict resolution.
// =============================================================================
// LOGIC: ConflictResolutionOptions provides configuration for the
//   conflict resolution process, including default strategies,
//   per-type strategy mappings, auto-resolution settings, and
//   performance parameters.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: ConflictResolutionStrategy, ConflictType (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;

/// <summary>
/// Configuration options for conflict resolution.
/// </summary>
/// <remarks>
/// <para>
/// Provides comprehensive configuration:
/// </para>
/// <list type="bullet">
///   <item><b>DefaultStrategy:</b> Default strategy for unspecified conflicts.</item>
///   <item><b>StrategyByType:</b> Strategy mappings per conflict type.</item>
///   <item><b>AutoResolve settings:</b> Control automatic resolution by severity.</item>
///   <item><b>MinMergeConfidence:</b> Minimum confidence for auto-merge.</item>
///   <item><b>Performance:</b> Timeout and retry settings.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var options = new ConflictResolutionOptions
/// {
///     DefaultStrategy = ConflictResolutionStrategy.Merge,
///     AutoResolveLow = true,
///     AutoResolveMedium = false,
///     MinMergeConfidence = 0.8f
/// };
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public record ConflictResolutionOptions
{
    /// <summary>
    /// Default strategy for unspecified conflicts.
    /// </summary>
    /// <value>
    /// The <see cref="ConflictResolutionStrategy"/> to use when no specific
    /// strategy is configured for a conflict type.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides fallback behavior.
    /// Default is Merge to attempt intelligent resolution.
    /// </remarks>
    public ConflictResolutionStrategy DefaultStrategy { get; init; } = ConflictResolutionStrategy.Merge;

    /// <summary>
    /// Strategy mappings per conflict type.
    /// </summary>
    /// <value>
    /// A dictionary mapping <see cref="ConflictType"/> to
    /// <see cref="ConflictResolutionStrategy"/>.
    /// </value>
    /// <remarks>
    /// LOGIC: Allows fine-grained control over resolution.
    /// If a conflict type is not in this dictionary, DefaultStrategy is used.
    /// Example: MissingInDocument might use DiscardGraph by default.
    /// </remarks>
    public Dictionary<ConflictType, ConflictResolutionStrategy> StrategyByType { get; init; } = new();

    /// <summary>
    /// Whether to auto-resolve low-severity conflicts.
    /// </summary>
    /// <value>True to automatically resolve Low severity conflicts.</value>
    /// <remarks>
    /// LOGIC: Low severity conflicts are minor discrepancies.
    /// Enabling auto-resolve streamlines the sync process.
    /// Default is true.
    /// </remarks>
    public bool AutoResolveLow { get; init; } = true;

    /// <summary>
    /// Whether to auto-resolve medium-severity conflicts.
    /// </summary>
    /// <value>True to automatically resolve Medium severity conflicts.</value>
    /// <remarks>
    /// LOGIC: Medium severity conflicts should be reviewed but have defaults.
    /// Enabling requires sufficient confidence in the resolution strategy.
    /// Default is false for safety.
    /// </remarks>
    public bool AutoResolveMedium { get; init; } = false;

    /// <summary>
    /// Whether to auto-resolve high-severity conflicts.
    /// </summary>
    /// <value>True to automatically resolve High severity conflicts.</value>
    /// <remarks>
    /// LOGIC: High severity conflicts require manual intervention by default.
    /// Only enable for fully trusted automated workflows.
    /// Default is false.
    /// </remarks>
    public bool AutoResolveHigh { get; init; } = false;

    /// <summary>
    /// Minimum confidence for auto-merge.
    /// </summary>
    /// <value>
    /// A value between 0.0 and 1.0.
    /// Merges with lower confidence require manual review.
    /// </value>
    /// <remarks>
    /// LOGIC: Prevents low-confidence automatic merges.
    /// Default of 0.8 ensures only high-confidence merges proceed.
    /// </remarks>
    public float MinMergeConfidence { get; init; } = 0.8f;

    /// <summary>
    /// Whether to preserve conflict history.
    /// </summary>
    /// <value>True to save resolved conflicts for audit trail.</value>
    /// <remarks>
    /// LOGIC: Enables audit and rollback capabilities.
    /// When true, resolved conflicts are stored for later review.
    /// Default is true.
    /// </remarks>
    public bool PreserveConflictHistory { get; init; } = true;

    /// <summary>
    /// Maximum resolution attempts per conflict.
    /// </summary>
    /// <value>
    /// The maximum number of times to retry resolution.
    /// </value>
    /// <remarks>
    /// LOGIC: Prevents infinite loops in resolution.
    /// After max attempts, the conflict is marked as requiring manual intervention.
    /// Default is 3.
    /// </remarks>
    public int MaxResolutionAttempts { get; init; } = 3;

    /// <summary>
    /// Timeout for resolution operations.
    /// </summary>
    /// <value>Maximum time allowed for resolution.</value>
    /// <remarks>
    /// LOGIC: Prevents resolution from blocking indefinitely.
    /// Operations exceeding this timeout are cancelled.
    /// Default is 30 seconds.
    /// </remarks>
    public TimeSpan ResolutionTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Default options with standard settings.
    /// </summary>
    public static ConflictResolutionOptions Default => new();

    /// <summary>
    /// Determines if a conflict with the given severity can be auto-resolved.
    /// </summary>
    /// <param name="severity">The conflict severity.</param>
    /// <returns>True if auto-resolution is allowed for this severity.</returns>
    public bool CanAutoResolve(ConflictSeverity severity)
    {
        return severity switch
        {
            ConflictSeverity.Low => AutoResolveLow,
            ConflictSeverity.Medium => AutoResolveMedium,
            ConflictSeverity.High => AutoResolveHigh,
            _ => false
        };
    }

    /// <summary>
    /// Gets the resolution strategy for a conflict type.
    /// </summary>
    /// <param name="conflictType">The type of conflict.</param>
    /// <returns>The configured or default strategy.</returns>
    public ConflictResolutionStrategy GetStrategy(ConflictType conflictType)
    {
        return StrategyByType.TryGetValue(conflictType, out var strategy)
            ? strategy
            : DefaultStrategy;
    }
}
