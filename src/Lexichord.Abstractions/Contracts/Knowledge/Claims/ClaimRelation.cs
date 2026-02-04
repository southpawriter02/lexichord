// =============================================================================
// File: ClaimRelation.cs
// Project: Lexichord.Abstractions
// Description: Relationships between claims in the knowledge graph.
// =============================================================================
// LOGIC: Defines the relationship between two claims, used for tracking
//   claim derivation, support, contradiction, and equivalence. Enables
//   contradiction detection and claim provenance tracking.
//
// v0.5.6e: Claim Data Model (CKVS Phase 2b)
// Dependencies: None (pure records and enum)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// A relationship between two claims.
/// </summary>
/// <remarks>
/// <para>
/// Claims can be related to each other in various ways: one claim may
/// be derived from another, support another, contradict another, or
/// supersede a previous version. This record captures these relationships.
/// </para>
/// <para>
/// <b>Usage:</b> Stored in <see cref="Claim.RelatedClaims"/> to track
/// claim-to-claim relationships for:
/// <list type="bullet">
///   <item>Contradiction detection and resolution.</item>
///   <item>Claim provenance and derivation tracking.</item>
///   <item>Version history management.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6e as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
public record ClaimRelation
{
    /// <summary>
    /// ID of the related claim.
    /// </summary>
    /// <value>The GUID of the claim this relation points to.</value>
    /// <remarks>
    /// LOGIC: References another <see cref="Claim.Id"/> in the claim store.
    /// The relationship direction is from the containing claim to this claim.
    /// </remarks>
    public required Guid RelatedClaimId { get; init; }

    /// <summary>
    /// Type of relationship between the claims.
    /// </summary>
    /// <value>The nature of the relationship (derived, supports, contradicts, etc.).</value>
    public required ClaimRelationType RelationType { get; init; }

    /// <summary>
    /// Confidence in this relationship (0.0-1.0).
    /// </summary>
    /// <value>
    /// A score from 0.0 (low confidence) to 1.0 (certain).
    /// Defaults to 1.0 for manually established relationships.
    /// </value>
    /// <remarks>
    /// LOGIC: Automatically detected relationships may have lower confidence.
    /// Used to filter or prioritize relationship display in the UI.
    /// </remarks>
    public float Confidence { get; init; } = 1.0f;
}

/// <summary>
/// Type of relationship between claims.
/// </summary>
/// <remarks>
/// <para>
/// Defines the semantic relationship between two claims. Used for
/// contradiction detection, claim versioning, and provenance tracking.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6e as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
public enum ClaimRelationType
{
    /// <summary>
    /// This claim is derived from another claim.
    /// </summary>
    /// <remarks>
    /// LOGIC: The claim was inferred or derived from the related claim.
    /// Used for provenance tracking in multi-step reasoning.
    /// </remarks>
    DerivedFrom,

    /// <summary>
    /// This claim supports another claim.
    /// </summary>
    /// <remarks>
    /// LOGIC: The claim provides evidence or reinforcement for the
    /// related claim. Does not imply logical entailment.
    /// </remarks>
    Supports,

    /// <summary>
    /// This claim contradicts another claim.
    /// </summary>
    /// <remarks>
    /// LOGIC: The claim is logically inconsistent with the related claim.
    /// Both claims cannot be true simultaneously. Triggers conflict
    /// detection in the validation engine (v0.6.5-KG).
    /// </remarks>
    Contradicts,

    /// <summary>
    /// This claim supersedes another claim.
    /// </summary>
    /// <remarks>
    /// LOGIC: The claim replaces the related claim, typically due to
    /// a document update. The superseded claim is marked inactive.
    /// </remarks>
    Supersedes,

    /// <summary>
    /// This claim is equivalent to another claim.
    /// </summary>
    /// <remarks>
    /// LOGIC: The claims express the same assertion using different
    /// wording or from different source documents. Used for deduplication.
    /// </remarks>
    EquivalentTo
}
