// =============================================================================
// File: AxiomStatistics.cs
// Project: Lexichord.Abstractions
// Description: Aggregate statistics about stored axioms.
// =============================================================================
// LOGIC: Returned by IAxiomRepository.GetStatisticsAsync to provide:
//   - Total, enabled, and disabled axiom counts.
//   - Breakdown by category.
//   - Breakdown by target type.
//
// v0.4.6f: Axiom Repository (CKVS Phase 1c)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Aggregate statistics about stored axioms.
/// </summary>
/// <remarks>
/// <para>
/// This record provides summary information about the axiom repository
/// for dashboards and monitoring purposes.
/// </para>
/// </remarks>
public record AxiomStatistics
{
    /// <summary>
    /// Gets the total number of axioms in the repository.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets the number of enabled axioms.
    /// </summary>
    public required int EnabledCount { get; init; }

    /// <summary>
    /// Gets the number of disabled axioms.
    /// </summary>
    public required int DisabledCount { get; init; }

    /// <summary>
    /// Gets axiom counts grouped by category.
    /// </summary>
    /// <value>
    /// A dictionary mapping category names to their axiom counts.
    /// Axioms with no category are grouped under an empty string key.
    /// </value>
    public required IReadOnlyDictionary<string, int> ByCategory { get; init; }

    /// <summary>
    /// Gets axiom counts grouped by target type.
    /// </summary>
    /// <value>
    /// A dictionary mapping target type names to their axiom counts.
    /// </value>
    public required IReadOnlyDictionary<string, int> ByTargetType { get; init; }
}
