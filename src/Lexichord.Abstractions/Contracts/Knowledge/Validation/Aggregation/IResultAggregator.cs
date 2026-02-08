// =============================================================================
// File: IResultAggregator.cs
// Project: Lexichord.Abstractions
// Description: Interface for aggregating validation findings into a unified result.
// =============================================================================
// LOGIC: Defines the contract for combining findings from multiple validators
//   into a single ValidationResult, with support for filtering and grouping.
//
// Spec Adaptations:
//   - Single Aggregate overload (ValidatorFailure type does not exist)
//   - Uses ValidatorId-based filtering (not ValidatorName)
//
// v0.6.5i: Validation Result Aggregator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Aggregation;

/// <summary>
/// Aggregates validation findings from multiple validators into a unified
/// <see cref="ValidationResult"/>.
/// </summary>
/// <remarks>
/// <para>
/// The aggregator is the final stage in the validation pipeline. It receives
/// raw findings from all validators, deduplicates them, applies severity-based
/// sorting and optional limiting, and produces a coherent result.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5i as part of the Validation Result Aggregator.
/// </para>
/// </remarks>
public interface IResultAggregator
{
    /// <summary>
    /// Aggregates findings into a validation result.
    /// </summary>
    /// <param name="findings">Findings from all validators.</param>
    /// <param name="duration">Total validation duration.</param>
    /// <param name="options">Validation options (used for MaxFindings limiting).</param>
    /// <param name="validatorsRun">Number of validators that were executed.</param>
    /// <param name="validatorsSkipped">Number of validators skipped due to mode/license.</param>
    /// <returns>Aggregated, deduplicated, and sorted <see cref="ValidationResult"/>.</returns>
    ValidationResult Aggregate(
        IEnumerable<ValidationFinding> findings,
        TimeSpan duration,
        ValidationOptions options,
        int validatorsRun = 0,
        int validatorsSkipped = 0);

    /// <summary>
    /// Filters findings based on the specified criteria.
    /// </summary>
    /// <param name="findings">Findings to filter.</param>
    /// <param name="filter">Filter criteria to apply.</param>
    /// <returns>Findings matching all specified criteria.</returns>
    IReadOnlyList<ValidationFinding> FilterFindings(
        IReadOnlyList<ValidationFinding> findings,
        FindingFilter filter);

    /// <summary>
    /// Groups findings by the specified criterion.
    /// </summary>
    /// <param name="findings">Findings to group.</param>
    /// <param name="groupBy">Grouping criterion.</param>
    /// <returns>Dictionary mapping group keys to their findings.</returns>
    IReadOnlyDictionary<string, IReadOnlyList<ValidationFinding>> GroupFindings(
        IReadOnlyList<ValidationFinding> findings,
        FindingGroupBy groupBy);
}
