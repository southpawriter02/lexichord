// =============================================================================
// File: ClaimValidationStatus.cs
// Project: Lexichord.Abstractions
// Description: Validation status for claims in the knowledge graph.
// =============================================================================
// LOGIC: Defines the possible validation states for a claim after validation
//   against axioms and the knowledge graph. Used to track whether a claim
//   has been validated, is valid, invalid, or in conflict with other claims.
//
// v0.5.6e: Claim Data Model (CKVS Phase 2b)
// Dependencies: None (pure enum)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// Validation status of a claim against axioms and the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// Claims are validated against the Axiom Store (v0.4.6-KG) and the Knowledge
/// Graph to ensure consistency. The validation process assigns one of these
/// status values to indicate the outcome.
/// </para>
/// <para>
/// <b>Validation Flow:</b>
/// <list type="number">
///   <item>Claim is extracted with <see cref="Pending"/> status.</item>
///   <item>Validation engine checks against axioms.</item>
///   <item>Status is updated to <see cref="Valid"/>, <see cref="Invalid"/>,
///     <see cref="Conflict"/>, or <see cref="Inconclusive"/>.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6e as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
public enum ClaimValidationStatus
{
    /// <summary>
    /// Claim has not yet been validated.
    /// </summary>
    /// <remarks>
    /// LOGIC: Default status for newly extracted claims. The validation
    /// engine (v0.6.5-KG) will process pending claims asynchronously.
    /// </remarks>
    Pending,

    /// <summary>
    /// Claim is valid and consistent with axioms and the knowledge graph.
    /// </summary>
    /// <remarks>
    /// LOGIC: The claim passed all axiom checks and does not conflict
    /// with any existing claims in the graph.
    /// </remarks>
    Valid,

    /// <summary>
    /// Claim violates one or more axioms.
    /// </summary>
    /// <remarks>
    /// LOGIC: The claim's assertion violates a domain rule defined in the
    /// Axiom Store. See <see cref="Claim.ValidationMessages"/> for details.
    /// </remarks>
    Invalid,

    /// <summary>
    /// Claim conflicts with one or more existing claims.
    /// </summary>
    /// <remarks>
    /// LOGIC: The claim contradicts another claim in the knowledge graph.
    /// Both claims cannot be true simultaneously. See
    /// <see cref="Claim.RelatedClaims"/> for the conflicting claims.
    /// </remarks>
    Conflict,

    /// <summary>
    /// Claim could not be validated due to missing context.
    /// </summary>
    /// <remarks>
    /// LOGIC: The validation engine could not determine validity because
    /// required entities or axioms are missing from the knowledge graph.
    /// </remarks>
    Inconclusive
}
