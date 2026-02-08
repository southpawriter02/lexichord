// =============================================================================
// File: FixConsolidator.cs
// Project: Lexichord.Modules.Knowledge
// Description: Consolidates fix suggestions from multiple validation findings.
// =============================================================================
// LOGIC: Groups findings that share the same SuggestedFix text into a single
//   ConsolidatedFix. Creates FixAllAction from consolidated fixes with
//   warnings about manual-review requirements.
//
// Spec Adaptations:
//   - SuggestedFix is string? (not ValidationFix record), so consolidation
//     is by exact string match, not by ReplacementText/ReplaceSpan/Description.
//   - TextEdit/Edits are empty (no span data available from string-based fix).
//   - CanAutoApply defaults to true for all consolidated fixes (no per-fix flag).
//   - Confidence defaults to 1.0 (no per-fix confidence data from string).
//
// v0.6.5i: Validation Result Aggregator (CKVS Phase 3a)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Aggregation;

namespace Lexichord.Modules.Knowledge.Validation.Aggregation;

/// <summary>
/// Consolidates fix suggestions from multiple <see cref="ValidationFinding"/>
/// instances by grouping findings with identical <see cref="ValidationFinding.SuggestedFix"/>
/// text.
/// </summary>
/// <remarks>
/// <para>
/// Since <see cref="ValidationFinding.SuggestedFix"/> is a <c>string?</c> (not a
/// structured <c>ValidationFix</c> record), consolidation is done by exact string
/// matching. Findings without a suggested fix are excluded.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5i as part of the Validation Result Aggregator.
/// </para>
/// </remarks>
public class FixConsolidator : IFixConsolidator
{
    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Groups findings by their <see cref="ValidationFinding.SuggestedFix"/> text.
    /// Each group becomes a single <see cref="ConsolidatedFix"/> with all affected
    /// findings. Results are ordered by number of findings resolved (descending).
    /// </para>
    /// </remarks>
    public IReadOnlyList<ConsolidatedFix> ConsolidateFixes(
        IReadOnlyList<ValidationFinding> findings)
    {
        // LOGIC: Filter to only findings that have a suggested fix.
        var withFixes = findings
            .Where(f => f.SuggestedFix != null)
            .ToList();

        if (withFixes.Count == 0)
        {
            return [];
        }

        // LOGIC: Group by exact SuggestedFix text. Each group becomes one ConsolidatedFix.
        var groups = withFixes
            .GroupBy(f => f.SuggestedFix!, StringComparer.Ordinal)
            .Select(group => new ConsolidatedFix
            {
                Description = group.Key,
                AffectedFindings = group.ToList(),
                // LOGIC: No span data available from string-based SuggestedFix.
                Edits = [],
                // LOGIC: Default confidence since no per-fix confidence is available.
                Confidence = 1.0f,
                // LOGIC: Default to auto-applicable since no per-fix flag is available.
                CanAutoApply = true
            })
            .OrderByDescending(f => f.FindingsResolved)
            .ToList();

        return groups;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Creates a <see cref="FixAllAction"/> from the provided consolidated fixes.
    /// Adds a warning if any fixes are not auto-applicable (require manual review).
    /// </para>
    /// </remarks>
    public FixAllAction CreateFixAllAction(IReadOnlyList<ConsolidatedFix> fixes)
    {
        if (fixes.Count == 0)
        {
            return new FixAllAction
            {
                Description = "No fixes available",
                Fixes = [],
                TotalFindingsResolved = 0,
                AllAutoApplicable = true,
                Warnings = []
            };
        }

        // LOGIC: Filter to auto-applicable fixes and compute warnings.
        var autoApplicable = fixes.Where(f => f.CanAutoApply).ToList();
        var warnings = new List<string>();

        if (autoApplicable.Count < fixes.Count)
        {
            var manualCount = fixes.Count - autoApplicable.Count;
            warnings.Add($"{manualCount} fix(es) require manual review and will be skipped");
        }

        var totalResolved = autoApplicable.Sum(f => f.FindingsResolved);

        return new FixAllAction
        {
            Description = $"Apply {autoApplicable.Count} fix(es) to resolve {totalResolved} finding(s)",
            Fixes = autoApplicable,
            TotalFindingsResolved = totalResolved,
            AllAutoApplicable = autoApplicable.Count == fixes.Count,
            Warnings = warnings
        };
    }
}
