// =============================================================================
// File: UnifiedValidationResult.cs
// Project: Lexichord.Abstractions
// Description: Combined validation result from all validators.
// =============================================================================
// LOGIC: Aggregates issues from Style Linter, Grammar Linter, and CKVS
//   Validation Engine into a single result with grouping and metadata.
//
// v0.7.5f: Issue Aggregator (Unified Validation Feature)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Combined validation result from all validators.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This record encapsulates the aggregated result of running
/// multiple validators on a document:
/// <list type="bullet">
///   <item><description>All issues from all validators</description></item>
///   <item><description>Groupings by category and severity</description></item>
///   <item><description>Summary counts and metrics</description></item>
///   <item><description>Cache and timing metadata</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5f as part of the Unified Validation feature.
/// </para>
/// </remarks>
public record UnifiedValidationResult
{
    /// <summary>
    /// Gets the path to the validated document.
    /// </summary>
    public required string DocumentPath { get; init; }

    /// <summary>
    /// Gets all detected issues from all validators.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Issues are sorted by severity (most severe first), then by
    /// location (document order). Duplicate issues are marked but not removed.
    /// </remarks>
    public required IReadOnlyList<UnifiedIssue> Issues { get; init; }

    /// <summary>
    /// Gets the total validation duration across all validators.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the timestamp when validation completed.
    /// </summary>
    public DateTimeOffset ValidatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets whether this result was retrieved from cache.
    /// </summary>
    public bool IsCached { get; init; }

    /// <summary>
    /// Gets the validation options that produced this result.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Stored for cache validation — a cached result is only valid
    /// if the options match the current request.
    /// </remarks>
    public UnifiedValidationOptions? Options { get; init; }

    /// <summary>
    /// Gets detailed validation trace information (debug level only).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Maps validator name to its detailed result or error message.
    /// Useful for diagnostics and debugging validation issues.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? ValidatorDetails { get; init; }

    /// <summary>
    /// Gets issues grouped by category.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Computed property for UI grouping. Categories with no
    /// issues are omitted from the dictionary.
    /// </remarks>
    public IReadOnlyDictionary<IssueCategory, IReadOnlyList<UnifiedIssue>> ByCategory =>
        Issues
            .GroupBy(i => i.Category)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UnifiedIssue>)g.ToList());

    /// <summary>
    /// Gets issues grouped by severity.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Computed property for UI grouping. Ordered from most severe
    /// (Error) to least severe (Hint).
    /// </remarks>
    public IReadOnlyDictionary<UnifiedSeverity, IReadOnlyList<UnifiedIssue>> BySeverity =>
        Issues
            .GroupBy(i => i.Severity)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UnifiedIssue>)g.ToList());

    /// <summary>
    /// Gets the count of issues by severity.
    /// </summary>
    public IReadOnlyDictionary<UnifiedSeverity, int> CountBySeverity =>
        Issues
            .GroupBy(i => i.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>
    /// Gets the total number of issues.
    /// </summary>
    public int TotalIssueCount => Issues.Count;

    /// <summary>
    /// Gets the number of error-level issues (blocking).
    /// </summary>
    public int ErrorCount => Issues.Count(i => i.Severity == UnifiedSeverity.Error);

    /// <summary>
    /// Gets the number of warning-level issues.
    /// </summary>
    public int WarningCount => Issues.Count(i => i.Severity == UnifiedSeverity.Warning);

    /// <summary>
    /// Gets the number of info-level issues.
    /// </summary>
    public int InfoCount => Issues.Count(i => i.Severity == UnifiedSeverity.Info);

    /// <summary>
    /// Gets the number of hint-level issues.
    /// </summary>
    public int HintCount => Issues.Count(i => i.Severity == UnifiedSeverity.Hint);

    /// <summary>
    /// Gets the number of issues that can be automatically fixed.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Issues where <see cref="UnifiedIssue.CanAutoFix"/> is true.
    /// </remarks>
    public int AutoFixableCount => Issues.Count(i => i.CanAutoFix);

    /// <summary>
    /// Gets whether the document can be published (no errors).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> A document can be published if it has no error-level issues.
    /// Warnings, info, and hints do not block publication.
    /// </remarks>
    public bool CanPublish => !Issues.Any(i => i.Severity == UnifiedSeverity.Error);

    /// <summary>
    /// Gets whether the validation result is empty (no issues).
    /// </summary>
    public bool IsEmpty => Issues.Count == 0;

    /// <summary>
    /// Gets whether the validation found any issues.
    /// </summary>
    public bool HasIssues => Issues.Count > 0;

    /// <summary>
    /// Gets the count of issues by source type.
    /// </summary>
    public IReadOnlyDictionary<string, int> CountBySourceType =>
        Issues
            .Where(i => i.SourceType is not null)
            .GroupBy(i => i.SourceType!)
            .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>
    /// Creates an empty validation result for a document.
    /// </summary>
    /// <param name="documentPath">Path to the document.</param>
    /// <param name="duration">Validation duration.</param>
    /// <returns>An empty <see cref="UnifiedValidationResult"/>.</returns>
    public static UnifiedValidationResult Empty(string documentPath, TimeSpan duration) =>
        new()
        {
            DocumentPath = documentPath,
            Issues = Array.Empty<UnifiedIssue>(),
            Duration = duration,
            ValidatedAt = DateTimeOffset.UtcNow,
            IsCached = false
        };

    /// <summary>
    /// Creates a cached copy of this result.
    /// </summary>
    /// <returns>A copy with <see cref="IsCached"/> set to true.</returns>
    public UnifiedValidationResult AsCached() => this with { IsCached = true };
}
