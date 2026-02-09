// =============================================================================
// File: FixConflictResult.cs
// Project: Lexichord.Abstractions
// Description: Result of checking fixes for conflicts.
// =============================================================================
// LOGIC: When multiple fixes target the same finding or overlapping regions,
//   applying them sequentially may produce incorrect results. This type
//   reports such conflicts so the UI can warn the user.
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

/// <summary>
/// Describes a conflict between two <see cref="UnifiedFix"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Conflicts arise when two fixes target the same <see cref="UnifiedFinding"/>
/// or when their replacement texts would overlap in the document.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
/// <param name="FixA">First conflicting fix.</param>
/// <param name="FixB">Second conflicting fix.</param>
/// <param name="Reason">Human-readable explanation of the conflict.</param>
public record FixConflict(
    UnifiedFix FixA,
    UnifiedFix FixB,
    string Reason);

/// <summary>
/// Result of <see cref="ICombinedFixWorkflow.CheckForConflicts"/>.
/// </summary>
/// <remarks>
/// <para>
/// Contains the list of detected conflicts. When <see cref="HasConflicts"/>
/// is <c>true</c>, the caller should present the conflicts to the user
/// before proceeding with fix application.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public record FixConflictResult
{
    /// <summary>
    /// List of detected conflicts (empty if no conflicts).
    /// </summary>
    public required IReadOnlyList<FixConflict> Conflicts { get; init; }

    /// <summary>
    /// Whether any conflicts were detected.
    /// </summary>
    public bool HasConflicts => Conflicts.Count > 0;

    /// <summary>
    /// Creates a result with no conflicts.
    /// </summary>
    /// <returns>An empty <see cref="FixConflictResult"/>.</returns>
    public static FixConflictResult None() => new() { Conflicts = [] };
}
