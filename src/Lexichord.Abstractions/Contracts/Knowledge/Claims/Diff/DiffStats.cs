// =============================================================================
// File: DiffStats.cs
// Project: Lexichord.Abstractions
// Description: Statistics about a claim diff operation.
// =============================================================================
// LOGIC: Aggregates counts and summaries for quick understanding of the
//   scope and nature of changes between claim versions.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: ClaimChangeType (v0.5.6i)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// Statistics about a claim diff operation.
/// </summary>
/// <remarks>
/// <para>
/// Provides aggregate metrics for understanding the scope of changes:
/// </para>
/// <list type="bullet">
///   <item><b>Counts:</b> Added, removed, modified, unchanged totals.</item>
///   <item><b>Aggregations:</b> Changes by predicate type and change type.</item>
///   <item><b>TotalChanges:</b> Quick check for whether anything changed.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var stats = result.Stats;
/// Console.WriteLine($"Total changes: {stats.TotalChanges}");
/// Console.WriteLine($"  Added: {stats.AddedCount}");
/// Console.WriteLine($"  Removed: {stats.RemovedCount}");
/// Console.WriteLine($"  Modified: {stats.ModifiedCount}");
/// </code>
/// </example>
public record DiffStats
{
    /// <summary>
    /// Number of claims added.
    /// </summary>
    public int AddedCount { get; init; }

    /// <summary>
    /// Number of claims removed.
    /// </summary>
    public int RemovedCount { get; init; }

    /// <summary>
    /// Number of claims modified.
    /// </summary>
    public int ModifiedCount { get; init; }

    /// <summary>
    /// Number of claims unchanged.
    /// </summary>
    public int UnchangedCount { get; init; }

    /// <summary>
    /// Total number of changes (added + removed + modified).
    /// </summary>
    public int TotalChanges => AddedCount + RemovedCount + ModifiedCount;

    /// <summary>
    /// Changes aggregated by predicate type.
    /// </summary>
    /// <value>
    /// Dictionary mapping predicate strings to change counts.
    /// Null if aggregation was not computed.
    /// </value>
    /// <remarks>
    /// Example: { "ACCEPTS": 5, "RETURNS": 3, "REQUIRES": 2 }
    /// </remarks>
    public IReadOnlyDictionary<string, int>? ChangesByPredicate { get; init; }

    /// <summary>
    /// Changes aggregated by change type.
    /// </summary>
    /// <value>
    /// Dictionary mapping <see cref="ClaimChangeType"/> to counts.
    /// Null if aggregation was not computed.
    /// </value>
    public IReadOnlyDictionary<ClaimChangeType, int>? ChangesByType { get; init; }

    /// <summary>
    /// Empty stats with all zero counts.
    /// </summary>
    public static DiffStats Empty { get; } = new();
}
