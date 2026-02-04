// =============================================================================
// File: ClaimChangeGroup.cs
// Project: Lexichord.Abstractions
// Description: A group of related claim changes.
// =============================================================================
// LOGIC: Groups changes by subject entity for easier review. Helps users
//   understand all changes related to a specific entity at once.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: ClaimChange (v0.5.6i), ClaimModification (v0.5.6i)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// A group of related claim changes.
/// </summary>
/// <remarks>
/// <para>
/// Changes are grouped by subject entity when <see cref="DiffOptions.GroupRelatedChanges"/>
/// is enabled. This makes review easier by showing all changes about the same entity.
/// </para>
/// <para>
/// <b>Example:</b> All changes about "GET /users" endpoint grouped together:
/// <list type="bullet">
///   <item>Added: GET /users ACCEPTS new_param</item>
///   <item>Modified: GET /users RETURNS 200 (was 201)</item>
///   <item>Removed: GET /users ACCEPTS old_param</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
public record ClaimChangeGroup
{
    /// <summary>
    /// Unique identifier for the group.
    /// </summary>
    /// <value>
    /// Typically the subject entity ID or normalized form.
    /// </value>
    public required string GroupId { get; init; }

    /// <summary>
    /// Human-readable label for the group.
    /// </summary>
    /// <value>
    /// The subject entity's surface form (e.g., "GET /users").
    /// </value>
    public required string Label { get; init; }

    /// <summary>
    /// Added or removed claims in this group.
    /// </summary>
    /// <value>
    /// List of <see cref="ClaimChange"/> records involving this entity.
    /// </value>
    public required IReadOnlyList<ClaimChange> Changes { get; init; }

    /// <summary>
    /// Modified claims in this group.
    /// </summary>
    /// <value>
    /// List of <see cref="ClaimModification"/> records involving this entity.
    /// </value>
    public required IReadOnlyList<ClaimModification> Modifications { get; init; }

    /// <summary>
    /// Maximum impact level in this group.
    /// </summary>
    /// <value>
    /// The highest <see cref="ChangeImpact"/> among all changes in the group.
    /// </value>
    public ChangeImpact MaxImpact { get; init; }

    /// <summary>
    /// Total number of changes in this group.
    /// </summary>
    public int TotalChanges => Changes.Count + Modifications.Count;
}
