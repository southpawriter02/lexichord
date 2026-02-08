// =============================================================================
// File: FixAllAction.cs
// Project: Lexichord.Abstractions
// Description: Action record for applying multiple consolidated fixes at once.
// =============================================================================
// LOGIC: Encapsulates a "Fix All" action that applies multiple ConsolidatedFix
//   instances. Tracks total findings resolved, auto-applicability, and any
//   warnings about potential conflicts or manual-review requirements.
//
// v0.6.5i: Validation Result Aggregator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Aggregation;

/// <summary>
/// Represents an action to apply multiple <see cref="ConsolidatedFix"/> instances
/// at once, resolving batch validation findings in a single operation.
/// </summary>
/// <remarks>
/// <para>
/// Created by <see cref="IFixConsolidator.CreateFixAllAction"/> from a set of
/// consolidated fixes. The action includes metadata about how many findings
/// will be resolved and whether all fixes can be auto-applied.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5i as part of the Validation Result Aggregator.
/// </para>
/// </remarks>
public record FixAllAction
{
    /// <summary>
    /// Human-readable description of what "Fix All" will do.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// All consolidated fixes to apply in this action.
    /// </summary>
    public required IReadOnlyList<ConsolidatedFix> Fixes { get; init; }

    /// <summary>
    /// Total number of findings that will be resolved by applying all fixes.
    /// </summary>
    public int TotalFindingsResolved { get; init; }

    /// <summary>
    /// Whether every fix in this action can be auto-applied without user review.
    /// </summary>
    public bool AllAutoApplicable { get; init; }

    /// <summary>
    /// Warnings about this fix-all action, e.g., manual review required or
    /// overlapping edits detected.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
