// =============================================================================
// File: UnifiedFindingResult.cs
// Project: Lexichord.Abstractions
// Description: Combined result from validation and linter passes.
// =============================================================================
// LOGIC: Aggregates the unified findings with summary statistics.
//   ByCategory and BySeverity dictionaries provide quick counts for
//   dashboard / panel display.
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

/// <summary>
/// Combined result from both validation and linter passes.
/// </summary>
/// <remarks>
/// <para>
/// Produced by <see cref="ILinterIntegration.GetUnifiedFindingsAsync"/>.
/// Contains the merged, filtered, and sorted list of findings along with
/// summary statistics for dashboard display.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public record UnifiedFindingResult
{
    /// <summary>
    /// All unified findings, sorted by severity (most severe first).
    /// </summary>
    public required IReadOnlyList<UnifiedFinding> Findings { get; init; }

    /// <summary>
    /// Number of findings originating from the validation engine.
    /// </summary>
    public int ValidationCount { get; init; }

    /// <summary>
    /// Number of findings originating from the style linter.
    /// </summary>
    public int LinterCount { get; init; }

    /// <summary>
    /// Overall status computed from the highest-severity finding.
    /// </summary>
    public UnifiedStatus Status { get; init; }

    /// <summary>
    /// Count breakdown by <see cref="FindingCategory"/>.
    /// </summary>
    public IReadOnlyDictionary<FindingCategory, int> ByCategory { get; init; } =
        new Dictionary<FindingCategory, int>();

    /// <summary>
    /// Count breakdown by <see cref="UnifiedSeverity"/>.
    /// </summary>
    public IReadOnlyDictionary<UnifiedSeverity, int> BySeverity { get; init; } =
        new Dictionary<UnifiedSeverity, int>();

    /// <summary>
    /// Creates an empty result with <see cref="UnifiedStatus.Pass"/> status.
    /// </summary>
    /// <returns>An empty <see cref="UnifiedFindingResult"/>.</returns>
    public static UnifiedFindingResult Empty() => new()
    {
        Findings = [],
        ValidationCount = 0,
        LinterCount = 0,
        Status = UnifiedStatus.Pass
    };
}
