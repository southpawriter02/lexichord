// =============================================================================
// File: IFindingDeduplicator.cs
// Project: Lexichord.Abstractions
// Description: Interface for deduplicating validation findings.
// =============================================================================
// LOGIC: Defines the contract for removing duplicate ValidationFinding
//   instances from a collection. Duplicates are identified by matching
//   Code + ValidatorId combined with either the same PropertyPath or
//   similar Message content.
//
// v0.6.5i: Validation Result Aggregator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Aggregation;

/// <summary>
/// Deduplicates <see cref="ValidationFinding"/> instances based on structural
/// similarity.
/// </summary>
/// <remarks>
/// <para>
/// When multiple validators or multiple passes produce overlapping findings,
/// the deduplicator identifies structurally equivalent findings and retains
/// only the first occurrence. This prevents cluttering the validation result
/// with redundant entries.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5i as part of the Validation Result Aggregator.
/// </para>
/// </remarks>
public interface IFindingDeduplicator
{
    /// <summary>
    /// Removes duplicate findings from a collection, retaining the first
    /// occurrence of each unique finding.
    /// </summary>
    /// <param name="findings">Findings to deduplicate.</param>
    /// <returns>Deduplicated list of findings in original order.</returns>
    IReadOnlyList<ValidationFinding> Deduplicate(
        IEnumerable<ValidationFinding> findings);

    /// <summary>
    /// Determines whether two findings are considered duplicates.
    /// </summary>
    /// <param name="a">First finding.</param>
    /// <param name="b">Second finding.</param>
    /// <returns><c>true</c> if the findings are duplicates; otherwise <c>false</c>.</returns>
    bool AreDuplicates(ValidationFinding a, ValidationFinding b);
}
