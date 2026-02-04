// =============================================================================
// File: ClaimModification.cs
// Project: Lexichord.Abstractions
// Description: A claim that was modified between versions.
// =============================================================================
// LOGIC: Captures both old and new versions of a modified claim along with
//   detailed field-level changes for review and audit purposes.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: Claim (v0.5.6e), FieldChange (v0.5.6i)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// A claim that was modified between versions.
/// </summary>
/// <remarks>
/// <para>
/// Represents claims that exist in both versions but have different values.
/// Provides both old and new states plus detailed field changes.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><b>OldClaim/NewClaim:</b> Full claim records for comparison.</item>
///   <item><b>ChangeTypes:</b> All types of changes detected.</item>
///   <item><b>FieldChanges:</b> Individual field-level diffs.</item>
///   <item><b>IsSemanticMatch:</b> True if matched by similarity, not ID.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var modification = new ClaimModification
/// {
///     OldClaim = previousClaim,
///     NewClaim = currentClaim,
///     ChangeTypes = new[] { ClaimChangeType.ObjectChanged },
///     FieldChanges = new[]
///     {
///         new FieldChange
///         {
///             FieldName = "Object",
///             OldValue = "10",
///             NewValue = "20",
///             Description = "Object changed from '10' to '20'"
///         }
///     },
///     Description = "Object changed from '10' to '20'",
///     Impact = ChangeImpact.Medium
/// };
/// </code>
/// </example>
public record ClaimModification
{
    /// <summary>
    /// The claim in its old (previous) state.
    /// </summary>
    /// <value>The full <see cref="Claim"/> from the previous version.</value>
    public required Claim OldClaim { get; init; }

    /// <summary>
    /// The claim in its new (current) state.
    /// </summary>
    /// <value>The full <see cref="Claim"/> from the current version.</value>
    public required Claim NewClaim { get; init; }

    /// <summary>
    /// Types of changes detected in this modification.
    /// </summary>
    /// <value>
    /// A list of <see cref="ClaimChangeType"/> values indicating what changed.
    /// Does not include Added or Removed (see <see cref="ClaimChange"/>).
    /// </value>
    public required IReadOnlyList<ClaimChangeType> ChangeTypes { get; init; }

    /// <summary>
    /// Detailed field-level changes.
    /// </summary>
    /// <value>
    /// A list of <see cref="FieldChange"/> records with old/new values.
    /// </value>
    public required IReadOnlyList<FieldChange> FieldChanges { get; init; }

    /// <summary>
    /// Human-readable description of all changes.
    /// </summary>
    /// <value>
    /// A semicolon-separated list of field change descriptions.
    /// </value>
    public required string Description { get; init; }

    /// <summary>
    /// Impact assessment of this modification.
    /// </summary>
    /// <value>
    /// The highest impact level among the detected changes.
    /// </value>
    public ChangeImpact Impact { get; init; }

    /// <summary>
    /// Whether this was a semantic match (not exact ID match).
    /// </summary>
    /// <value>
    /// True if the claims were matched by content similarity rather than ID.
    /// </value>
    /// <remarks>
    /// LOGIC: Semantic matches occur when a claim is re-extracted with a new ID
    /// but similar content. These may require extra review attention.
    /// </remarks>
    public bool IsSemanticMatch { get; init; }

    /// <summary>
    /// Similarity score for semantic matches.
    /// </summary>
    /// <value>
    /// A score from 0.0 to 1.0. Defaults to 1.0 for exact ID matches.
    /// </value>
    public float Similarity { get; init; } = 1.0f;
}
