// =============================================================================
// File: IFixConsolidator.cs
// Project: Lexichord.Abstractions
// Description: Interface for consolidating fix suggestions from multiple findings.
// =============================================================================
// LOGIC: Defines the contract for merging overlapping or identical fix
//   suggestions into ConsolidatedFix instances, and creating a batch
//   FixAllAction from multiple consolidated fixes.
//
// v0.6.5i: Validation Result Aggregator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Aggregation;

/// <summary>
/// Consolidates fix suggestions from multiple <see cref="ValidationFinding"/>
/// instances into a reduced set of <see cref="ConsolidatedFix"/> actions.
/// </summary>
/// <remarks>
/// <para>
/// When multiple findings share the same <see cref="ValidationFinding.SuggestedFix"/>
/// text, the consolidator groups them together so the user sees a single fix
/// action that resolves multiple issues simultaneously.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5i as part of the Validation Result Aggregator.
/// </para>
/// </remarks>
public interface IFixConsolidator
{
    /// <summary>
    /// Consolidates fix suggestions from findings with non-null
    /// <see cref="ValidationFinding.SuggestedFix"/> values.
    /// </summary>
    /// <param name="findings">Findings to consolidate fixes from.</param>
    /// <returns>Consolidated fix set, ordered by confidence descending.</returns>
    IReadOnlyList<ConsolidatedFix> ConsolidateFixes(
        IReadOnlyList<ValidationFinding> findings);

    /// <summary>
    /// Creates a "Fix All" action from a set of consolidated fixes.
    /// </summary>
    /// <param name="fixes">Consolidated fixes to include.</param>
    /// <returns>A <see cref="FixAllAction"/> describing the batch operation.</returns>
    FixAllAction CreateFixAllAction(
        IReadOnlyList<ConsolidatedFix> fixes);
}
