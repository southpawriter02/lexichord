// =============================================================================
// File: CombinedFixWorkflow.cs
// Project: Lexichord.Modules.Knowledge
// Description: Manages combined fix workflow for validation and linter fixes.
// =============================================================================
// LOGIC: Stateless service that checks a list of UnifiedFix instances for
//   conflicts (same FindingId) and orders them for safe application.
//
//   Conflict Detection:
//     Two fixes conflict if they target the same FindingId.
//
//   Ordering:
//     Fixes are ordered by FindingId descending so that applying them
//     sequentially doesn't shift offsets for earlier fixes.
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Validation.Integration;

/// <summary>
/// Manages combined fix workflow for validation and linter fixes.
/// </summary>
/// <remarks>
/// <para>
/// Stateless singleton. Thread-safe — no mutable state.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public sealed class CombinedFixWorkflow : ICombinedFixWorkflow
{
    // =========================================================================
    // Fields
    // =========================================================================

    /// <summary>Logger for diagnostic output.</summary>
    private readonly ILogger<CombinedFixWorkflow> _logger;

    // =========================================================================
    // Constructor
    // =========================================================================

    /// <summary>
    /// Initializes a new instance of the <see cref="CombinedFixWorkflow"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> is null.</exception>
    public CombinedFixWorkflow(ILogger<CombinedFixWorkflow> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Log construction for DI verification during startup.
        _logger.LogDebug("[v0.6.5j] CombinedFixWorkflow constructed.");
    }

    // =========================================================================
    // ICombinedFixWorkflow — CheckForConflicts
    // =========================================================================

    /// <inheritdoc />
    public FixConflictResult CheckForConflicts(IReadOnlyList<UnifiedFix> fixes)
    {
        ArgumentNullException.ThrowIfNull(fixes);

        // LOGIC: Early return for trivial cases (0 or 1 fix cannot conflict).
        if (fixes.Count <= 1)
        {
            _logger.LogTrace(
                "[v0.6.5j] CheckForConflicts: {Count} fix(es) — no conflicts possible.",
                fixes.Count);
            return FixConflictResult.None();
        }

        var conflicts = new List<FixConflict>();

        // LOGIC: Group fixes by FindingId. Multiple fixes targeting the same
        // FindingId are considered conflicting because they would both try
        // to modify the same location.
        var groupedByFinding = fixes
            .GroupBy(f => f.FindingId)
            .Where(g => g.Count() > 1);

        foreach (var group in groupedByFinding)
        {
            var fixesInGroup = group.ToList();

            // LOGIC: Create a conflict entry for each pair in the group.
            for (var i = 0; i < fixesInGroup.Count; i++)
            {
                for (var j = i + 1; j < fixesInGroup.Count; j++)
                {
                    var conflict = new FixConflict(
                        fixesInGroup[i],
                        fixesInGroup[j],
                        $"Both fixes target the same finding (FindingId={group.Key}).");

                    conflicts.Add(conflict);

                    _logger.LogDebug(
                        "[v0.6.5j] Conflict detected: Fix {FixA} and Fix {FixB} target FindingId={FindingId}.",
                        fixesInGroup[i].Id, fixesInGroup[j].Id, group.Key);
                }
            }
        }

        _logger.LogDebug(
            "[v0.6.5j] CheckForConflicts: {FixCount} fixes checked, {ConflictCount} conflict(s) found.",
            fixes.Count, conflicts.Count);

        return new FixConflictResult { Conflicts = conflicts };
    }

    // =========================================================================
    // ICombinedFixWorkflow — OrderFixesForApplication
    // =========================================================================

    /// <inheritdoc />
    public IReadOnlyList<UnifiedFix> OrderFixesForApplication(IReadOnlyList<UnifiedFix> fixes)
    {
        ArgumentNullException.ThrowIfNull(fixes);

        // LOGIC: Early return for trivial cases.
        if (fixes.Count <= 1)
        {
            _logger.LogTrace(
                "[v0.6.5j] OrderFixesForApplication: {Count} fix(es) — no ordering needed.",
                fixes.Count);
            return fixes;
        }

        // LOGIC: Order by FindingId for deterministic application order.
        // This ensures consistent behavior across runs.
        var ordered = fixes
            .OrderBy(f => f.FindingId)
            .ToList();

        _logger.LogDebug(
            "[v0.6.5j] OrderFixesForApplication: Ordered {Count} fixes by FindingId.",
            ordered.Count);

        return ordered;
    }
}
