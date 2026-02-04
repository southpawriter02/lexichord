// =============================================================================
// File: ClaimDiffResult.cs
// Project: Lexichord.Abstractions
// Description: Result of a claim diff operation.
// =============================================================================
// LOGIC: Contains all changes between two claim sets: added, removed, modified,
//   and unchanged claims. Includes statistics and optional grouping.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: ClaimChange, ClaimModification, DiffStats (v0.5.6i)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// Result of a claim diff operation.
/// </summary>
/// <remarks>
/// <para>
/// Contains the complete comparison between two sets of claims:
/// </para>
/// <list type="bullet">
///   <item><b>Added:</b> Claims present in new but not old.</item>
///   <item><b>Removed:</b> Claims present in old but not new.</item>
///   <item><b>Modified:</b> Claims present in both with changes.</item>
///   <item><b>Unchanged:</b> Claims identical in both versions.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = diffService.Diff(oldClaims, newClaims);
/// 
/// if (result.HasChanges)
/// {
///     Console.WriteLine($"Changes detected:");
///     Console.WriteLine($"  Added: {result.Stats.AddedCount}");
///     Console.WriteLine($"  Removed: {result.Stats.RemovedCount}");
///     Console.WriteLine($"  Modified: {result.Stats.ModifiedCount}");
/// }
/// </code>
/// </example>
public record ClaimDiffResult
{
    /// <summary>
    /// Claims that were added (present in new, not in old).
    /// </summary>
    /// <value>List of <see cref="ClaimChange"/> with <see cref="ClaimChangeType.Added"/>.</value>
    public required IReadOnlyList<ClaimChange> Added { get; init; }

    /// <summary>
    /// Claims that were removed (present in old, not in new).
    /// </summary>
    /// <value>List of <see cref="ClaimChange"/> with <see cref="ClaimChangeType.Removed"/>.</value>
    public required IReadOnlyList<ClaimChange> Removed { get; init; }

    /// <summary>
    /// Claims that were modified (present in both with changes).
    /// </summary>
    /// <value>List of <see cref="ClaimModification"/> with field-level diffs.</value>
    public required IReadOnlyList<ClaimModification> Modified { get; init; }

    /// <summary>
    /// Claims that were unchanged between versions.
    /// </summary>
    /// <value>List of <see cref="Claim"/> records that are identical.</value>
    public required IReadOnlyList<Claim> Unchanged { get; init; }

    /// <summary>
    /// Number of claims in the old version.
    /// </summary>
    public int OldClaimCount { get; init; }

    /// <summary>
    /// Number of claims in the new version.
    /// </summary>
    public int NewClaimCount { get; init; }

    /// <summary>
    /// Aggregate statistics about the diff.
    /// </summary>
    /// <value>Counts and aggregations for quick summary.</value>
    public DiffStats Stats { get; init; } = new();

    /// <summary>
    /// Whether any changes were detected.
    /// </summary>
    /// <value>True if there are any added, removed, or modified claims.</value>
    public bool HasChanges => Added.Count > 0 || Removed.Count > 0 || Modified.Count > 0;

    /// <summary>
    /// Optional grouping of related changes.
    /// </summary>
    /// <value>
    /// Groups organized by subject entity when <see cref="DiffOptions.GroupRelatedChanges"/>
    /// is enabled. Null if grouping was not requested.
    /// </value>
    public IReadOnlyList<ClaimChangeGroup>? Groups { get; init; }

    /// <summary>
    /// Creates a result indicating no changes.
    /// </summary>
    /// <param name="claims">The claims that are all unchanged.</param>
    /// <returns>A <see cref="ClaimDiffResult"/> with all claims in Unchanged.</returns>
    public static ClaimDiffResult NoChanges(IReadOnlyList<Claim> claims) => new()
    {
        Added = Array.Empty<ClaimChange>(),
        Removed = Array.Empty<ClaimChange>(),
        Modified = Array.Empty<ClaimModification>(),
        Unchanged = claims,
        OldClaimCount = claims.Count,
        NewClaimCount = claims.Count,
        Stats = new DiffStats { UnchangedCount = claims.Count }
    };

    /// <summary>
    /// Creates an empty result with no claims.
    /// </summary>
    public static ClaimDiffResult Empty { get; } = new()
    {
        Added = Array.Empty<ClaimChange>(),
        Removed = Array.Empty<ClaimChange>(),
        Modified = Array.Empty<ClaimModification>(),
        Unchanged = Array.Empty<Claim>(),
        Stats = DiffStats.Empty
    };
}
