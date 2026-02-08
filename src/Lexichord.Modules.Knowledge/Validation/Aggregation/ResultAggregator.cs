// =============================================================================
// File: ResultAggregator.cs
// Project: Lexichord.Modules.Knowledge
// Description: Aggregates validation findings into a unified result.
// =============================================================================
// LOGIC: Orchestrates the aggregation pipeline:
//   1. Deduplicate findings via IFindingDeduplicator
//   2. Sort by severity (Error first) then by PropertyPath
//   3. Limit to MaxFindings if specified
//   4. Build ValidationResult via WithFindings() factory
//
// FilterFindings: Applies AND-combined criteria (MinSeverity, ValidatorIds,
//   Codes, FixableOnly) against each finding.
//
// GroupFindings: Groups by ValidatorId, Severity (string key), or Code.
//
// Spec Adaptations:
//   - No ValidatorFailure overload (type does not exist)
//   - No MinSeverity on ValidationOptions (filtering via FindingFilter only)
//   - ValidatorName → ValidatorId throughout
//   - No Location-based sorting (PropertyPath used as secondary sort)
//
// v0.6.5i: Validation Result Aggregator (CKVS Phase 3a)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Aggregation;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Validation.Aggregation;

/// <summary>
/// Aggregates validation findings into a unified <see cref="ValidationResult"/>.
/// </summary>
/// <remarks>
/// <para>
/// Implements the full aggregation pipeline: deduplication, severity-based sorting,
/// optional limiting, and result construction. Delegates deduplication to
/// <see cref="IFindingDeduplicator"/> and can work with <see cref="IFixConsolidator"/>
/// for downstream fix merging.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5i as part of the Validation Result Aggregator.
/// </para>
/// </remarks>
public class ResultAggregator : IResultAggregator
{
    private readonly IFindingDeduplicator _deduplicator;
    private readonly IFixConsolidator _fixConsolidator;
    private readonly ILogger<ResultAggregator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultAggregator"/> class.
    /// </summary>
    /// <param name="deduplicator">Deduplicator for removing duplicate findings.</param>
    /// <param name="fixConsolidator">Consolidator for merging fix suggestions.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public ResultAggregator(
        IFindingDeduplicator deduplicator,
        IFixConsolidator fixConsolidator,
        ILogger<ResultAggregator> logger)
    {
        _deduplicator = deduplicator;
        _fixConsolidator = fixConsolidator;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Aggregation pipeline:
    /// <list type="number">
    ///   <item>Deduplicate findings via <see cref="IFindingDeduplicator"/>.</item>
    ///   <item>Sort by severity (Error > Warning > Info) then by PropertyPath.</item>
    ///   <item>Limit to <see cref="ValidationOptions.MaxFindings"/> if specified.</item>
    ///   <item>Build result via <see cref="ValidationResult.WithFindings"/>.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public ValidationResult Aggregate(
        IEnumerable<ValidationFinding> findings,
        TimeSpan duration,
        ValidationOptions options,
        int validatorsRun = 0,
        int validatorsSkipped = 0)
    {
        // LOGIC: Materialize findings into a list for counting and processing.
        var findingsList = findings.ToList();
        _logger.LogDebug(
            "Starting aggregation of {Count} raw findings",
            findingsList.Count);

        // LOGIC: Step 1 — Deduplicate via the injected deduplicator.
        var deduplicated = _deduplicator.Deduplicate(findingsList);
        _logger.LogDebug(
            "Deduplication reduced {Original} findings to {Deduplicated}",
            findingsList.Count,
            deduplicated.Count);

        // LOGIC: Step 2 — Sort by severity (descending numeric = Error first),
        // then by PropertyPath for stable ordering within severity bands.
        var sorted = deduplicated
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.PropertyPath ?? string.Empty)
            .ToList();

        // LOGIC: Step 3 — Limit to MaxFindings if the option is set.
        var limited = options.MaxFindings.HasValue && options.MaxFindings.Value > 0
            ? sorted.Take(options.MaxFindings.Value).ToList()
            : sorted;

        _logger.LogInformation(
            "Aggregated {Original} findings into {Final} " +
            "(deduplicated: {Dedup}, limited: {Limited})",
            findingsList.Count,
            limited.Count,
            deduplicated.Count,
            limited.Count);

        // LOGIC: Step 4 — Build the result using the existing WithFindings factory.
        return ValidationResult.WithFindings(
            limited,
            validatorsRun,
            validatorsSkipped,
            duration);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// All non-null filter properties are AND-combined. Severity comparison
    /// uses <c>>=</c> because <see cref="ValidationSeverity"/> values increase
    /// with severity (Info=0, Warning=1, Error=2).
    /// </para>
    /// </remarks>
    public IReadOnlyList<ValidationFinding> FilterFindings(
        IReadOnlyList<ValidationFinding> findings,
        FindingFilter filter)
    {
        _logger.LogDebug(
            "Filtering {Count} findings with filter criteria",
            findings.Count);

        var result = findings.AsEnumerable();

        // LOGIC: MinSeverity filter — include findings at or above the threshold.
        // ValidationSeverity: Info=0, Warning=1, Error=2, so >= is correct.
        if (filter.MinSeverity.HasValue)
        {
            result = result.Where(f => f.Severity >= filter.MinSeverity.Value);
        }

        // LOGIC: ValidatorIds filter — include only findings from specified validators.
        if (filter.ValidatorIds is { Count: > 0 })
        {
            result = result.Where(f => filter.ValidatorIds.Contains(f.ValidatorId));
        }

        // LOGIC: Codes filter — include only findings with specified codes.
        if (filter.Codes is { Count: > 0 })
        {
            result = result.Where(f => filter.Codes.Contains(f.Code));
        }

        // LOGIC: FixableOnly filter — include only findings with a suggested fix.
        if (filter.FixableOnly == true)
        {
            result = result.Where(f => f.SuggestedFix != null);
        }

        var filtered = result.ToList();
        _logger.LogDebug(
            "Filter reduced {Original} findings to {Filtered}",
            findings.Count,
            filtered.Count);

        return filtered;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Groups findings into a dictionary keyed by a string representation of
    /// the grouping criterion. Unsupported <see cref="FindingGroupBy"/> values
    /// throw <see cref="ArgumentOutOfRangeException"/>.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, IReadOnlyList<ValidationFinding>> GroupFindings(
        IReadOnlyList<ValidationFinding> findings,
        FindingGroupBy groupBy)
    {
        _logger.LogDebug(
            "Grouping {Count} findings by {GroupBy}",
            findings.Count,
            groupBy);

        return groupBy switch
        {
            // LOGIC: Group by ValidatorId (the validator that produced each finding).
            FindingGroupBy.Validator => findings
                .GroupBy(f => f.ValidatorId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<ValidationFinding>)g.ToList()),

            // LOGIC: Group by Severity (converted to string key for the dictionary).
            FindingGroupBy.Severity => findings
                .GroupBy(f => f.Severity.ToString())
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<ValidationFinding>)g.ToList()),

            // LOGIC: Group by finding code (machine-readable error code).
            FindingGroupBy.Code => findings
                .GroupBy(f => f.Code)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<ValidationFinding>)g.ToList()),

            _ => throw new ArgumentOutOfRangeException(
                nameof(groupBy),
                groupBy,
                $"Unsupported grouping criterion: {groupBy}")
        };
    }
}
