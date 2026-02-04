// =============================================================================
// File: ClaimChange.cs
// Project: Lexichord.Abstractions
// Description: A claim that was added or removed.
// =============================================================================
// LOGIC: Represents added or removed claims in a diff result. Includes the
//   claim, change type, impact assessment, and optional semantic match info.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: Claim (v0.5.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// A claim that was added or removed.
/// </summary>
/// <remarks>
/// <para>
/// Represents structural changes where claims are present in one version
/// but not the other. For modified claims, see <see cref="ClaimModification"/>.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><b>Claim:</b> The actual claim that changed.</item>
///   <item><b>ChangeType:</b> Added or Removed.</item>
///   <item><b>Impact:</b> Assessed importance of the change.</item>
///   <item><b>MatchedClaim:</b> For semantic matches, the corresponding claim.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var addedChange = new ClaimChange
/// {
///     Claim = newClaim,
///     ChangeType = ClaimChangeType.Added,
///     Description = "Added: endpoint:/api/users ACCEPTS parameter:limit",
///     Impact = ChangeImpact.Low
/// };
/// </code>
/// </example>
public record ClaimChange
{
    /// <summary>
    /// The claim that was added or removed.
    /// </summary>
    /// <value>The full <see cref="Claim"/> record.</value>
    public required Claim Claim { get; init; }

    /// <summary>
    /// Type of change (Added or Removed).
    /// </summary>
    /// <value>
    /// <see cref="ClaimChangeType.Added"/> or <see cref="ClaimChangeType.Removed"/>.
    /// </value>
    public required ClaimChangeType ChangeType { get; init; }

    /// <summary>
    /// Human-readable description of the change.
    /// </summary>
    /// <value>
    /// A formatted string like "Added: subject PREDICATE object".
    /// </value>
    public required string Description { get; init; }

    /// <summary>
    /// Related evidence text from the source document.
    /// </summary>
    /// <value>
    /// The source sentence if <see cref="DiffOptions.IncludeEvidence"/> is true.
    /// </value>
    public string? Evidence { get; init; }

    /// <summary>
    /// Impact assessment of this change.
    /// </summary>
    /// <value>
    /// Importance level based on predicate type and context.
    /// </value>
    public ChangeImpact Impact { get; init; }

    /// <summary>
    /// Matched claim from the other version (for semantic matches).
    /// </summary>
    /// <value>
    /// When a removed claim was semantically matched to a new claim,
    /// this contains the matched claim. Null for exact matches or no match.
    /// </value>
    /// <remarks>
    /// LOGIC: When semantic matching finds a similar claim with a different ID,
    /// the match is stored here for reference.
    /// </remarks>
    public Claim? MatchedClaim { get; init; }

    /// <summary>
    /// Similarity score if matched semantically.
    /// </summary>
    /// <value>
    /// A score from 0.0 to 1.0 if <see cref="MatchedClaim"/> is set.
    /// </value>
    public float? MatchSimilarity { get; init; }
}
