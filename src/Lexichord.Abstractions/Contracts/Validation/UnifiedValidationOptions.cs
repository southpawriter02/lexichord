// =============================================================================
// File: UnifiedValidationOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for unified validation behavior.
// =============================================================================
// LOGIC: Controls which validators are invoked, how results are filtered,
//   caching behavior, and execution mode (parallel vs sequential).
//
// v0.7.5f: Issue Aggregator (Unified Validation Feature)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Configuration options for unified validation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This record controls the behavior of <see cref="IUnifiedValidationService"/>:
/// <list type="bullet">
///   <item><description>Which validators to invoke (Style, Grammar, CKVS)</description></item>
///   <item><description>How to filter results (severity, category)</description></item>
///   <item><description>Caching behavior (enable, TTL)</description></item>
///   <item><description>Execution mode (parallel vs sequential)</description></item>
///   <item><description>Timeout and limit settings</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Defaults:</b> All validators enabled, all severities included, caching enabled
/// with 5-minute TTL, deduplication enabled, parallel execution, 30-second timeout.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5f as part of the Unified Validation feature.
/// </para>
/// </remarks>
public record UnifiedValidationOptions
{
    /// <summary>
    /// Default options instance with all defaults applied.
    /// </summary>
    public static UnifiedValidationOptions Default { get; } = new();

    /// <summary>
    /// Gets whether to run the Style Linter.
    /// </summary>
    /// <value>Default: <c>true</c>.</value>
    /// <remarks>
    /// <b>LOGIC:</b> When enabled, invokes <c>IStyleDeviationScanner</c> to detect
    /// style guide violations. Available at Core tier and above.
    /// </remarks>
    public bool IncludeStyleLinter { get; init; } = true;

    /// <summary>
    /// Gets whether to run the Grammar Linter.
    /// </summary>
    /// <value>Default: <c>true</c>.</value>
    /// <remarks>
    /// <b>LOGIC:</b> When enabled, invokes grammar checking. Currently a placeholder
    /// for future implementation. Available at WriterPro tier and above.
    /// </remarks>
    public bool IncludeGrammarLinter { get; init; } = true;

    /// <summary>
    /// Gets whether to run the CKVS Validation Engine.
    /// </summary>
    /// <value>Default: <c>true</c>.</value>
    /// <remarks>
    /// <b>LOGIC:</b> When enabled, invokes <c>IValidationEngine</c> for knowledge
    /// and axiom validation. Available at Teams tier and above.
    /// </remarks>
    public bool IncludeValidationEngine { get; init; } = true;

    /// <summary>
    /// Gets the minimum severity level to include in results.
    /// </summary>
    /// <value>Default: <see cref="UnifiedSeverity.Hint"/> (include all).</value>
    /// <remarks>
    /// <b>LOGIC:</b> Issues with severity greater than this value (i.e., less severe)
    /// are filtered out. Since Error=0 is most severe and Hint=3 is least severe,
    /// use <see cref="UnifiedSeverity.Error"/> to show only errors.
    /// </remarks>
    public UnifiedSeverity MinimumSeverity { get; init; } = UnifiedSeverity.Hint;

    /// <summary>
    /// Gets whether to cache validation results.
    /// </summary>
    /// <value>Default: <c>true</c>.</value>
    /// <remarks>
    /// <b>LOGIC:</b> When enabled, results are cached by document path to avoid
    /// redundant validation on unchanged documents.
    /// </remarks>
    public bool EnableCaching { get; init; } = true;

    /// <summary>
    /// Gets the cache time-to-live in milliseconds.
    /// </summary>
    /// <value>Default: 300,000 ms (5 minutes).</value>
    /// <remarks>
    /// <b>LOGIC:</b> Cached results expire after this duration. Set to a lower value
    /// for more responsive updates, higher for better performance.
    /// </remarks>
    public int CacheTtlMs { get; init; } = 300_000;

    /// <summary>
    /// Gets whether to enable deduplication across validators.
    /// </summary>
    /// <value>Default: <c>true</c>.</value>
    /// <remarks>
    /// <b>LOGIC:</b> When enabled, issues detected at the same location by different
    /// validators are deduplicated, keeping the highest-severity instance.
    /// </remarks>
    public bool EnableDeduplication { get; init; } = true;

    /// <summary>
    /// Gets the maximum number of issues to return.
    /// </summary>
    /// <value>Default: 1000. Use 0 for unlimited.</value>
    /// <remarks>
    /// <b>LOGIC:</b> Limits the result size for performance. Issues are sorted by
    /// severity before truncation, so the most severe issues are retained.
    /// </remarks>
    public int MaxIssuesPerDocument { get; init; } = 1000;

    /// <summary>
    /// Gets whether to run validators in parallel.
    /// </summary>
    /// <value>Default: <c>true</c>.</value>
    /// <remarks>
    /// <b>LOGIC:</b> When enabled, validators execute concurrently for faster
    /// overall completion. Disable for deterministic ordering or debugging.
    /// </remarks>
    public bool ParallelValidation { get; init; } = true;

    /// <summary>
    /// Gets the timeout per validator in milliseconds.
    /// </summary>
    /// <value>Default: 30,000 ms (30 seconds).</value>
    /// <remarks>
    /// <b>LOGIC:</b> Each validator is given this timeout independently. If a
    /// validator times out, its results are omitted but others continue.
    /// </remarks>
    public int ValidatorTimeoutMs { get; init; } = 30_000;

    /// <summary>
    /// Gets the issue categories to include, or null to include all.
    /// </summary>
    /// <value>Default: <c>null</c> (include all categories).</value>
    /// <remarks>
    /// <b>LOGIC:</b> When set, only issues matching one of the specified categories
    /// are included in the result.
    /// </remarks>
    public IReadOnlyList<IssueCategory>? FilterByCategory { get; init; }

    /// <summary>
    /// Gets whether to include fix suggestions in the result.
    /// </summary>
    /// <value>Default: <c>true</c>.</value>
    /// <remarks>
    /// <b>LOGIC:</b> When enabled, each <see cref="UnifiedIssue"/> includes
    /// available fix suggestions. Disable for faster validation when fixes
    /// are not needed (e.g., count-only scenarios).
    /// </remarks>
    public bool IncludeFixes { get; init; } = true;

    /// <summary>
    /// Gets the cache TTL as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan CacheTtl => TimeSpan.FromMilliseconds(CacheTtlMs);

    /// <summary>
    /// Gets the validator timeout as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan ValidatorTimeout => TimeSpan.FromMilliseconds(ValidatorTimeoutMs);

    /// <summary>
    /// Determines whether a severity level passes the minimum severity filter.
    /// </summary>
    /// <param name="severity">The severity to check.</param>
    /// <returns><c>true</c> if the severity is at or above the minimum.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Lower numeric value = higher severity. Error=0 is most severe.
    /// </remarks>
    public bool PassesSeverityFilter(UnifiedSeverity severity) =>
        (int)severity <= (int)MinimumSeverity;

    /// <summary>
    /// Determines whether a category passes the category filter.
    /// </summary>
    /// <param name="category">The category to check.</param>
    /// <returns><c>true</c> if no filter is set or the category is in the filter list.</returns>
    public bool PassesCategoryFilter(IssueCategory category) =>
        FilterByCategory is null || FilterByCategory.Contains(category);
}
