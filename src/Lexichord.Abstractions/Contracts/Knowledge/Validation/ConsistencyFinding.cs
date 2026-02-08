// =============================================================================
// File: ConsistencyFinding.cs
// Project: Lexichord.Abstractions
// Description: Extended validation finding for consistency issues.
// =============================================================================
// LOGIC: Extends the base ValidationFinding record with consistency-specific
//   properties: the existing claim that conflicts, its document, the conflict
//   type, confidence, and suggested resolution. Inherits the positional
//   constructor parameters from ValidationFinding.
//
// v0.6.5h: Consistency Checker (CKVS Phase 3a)
// Dependencies: ValidationFinding (v0.6.5e), Claim (v0.5.6e),
//               ConflictType (v0.6.5h), ConflictResolution (v0.6.5h)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Extended validation finding for consistency issues.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ConsistencyFinding"/> extends <see cref="ValidationFinding"/>
/// with properties specific to consistency checking: the conflicting existing
/// claim, the conflict type, detection confidence, and a suggested resolution.
/// </para>
/// <para>
/// <b>Immutability:</b> Inherits immutability from <see cref="ValidationFinding"/>.
/// Safe for concurrent aggregation from parallel validator execution.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5h as part of the Consistency Checker.
/// </para>
/// </remarks>
/// <param name="ValidatorId">
/// The unique identifier of the validator that produced this finding.
/// </param>
/// <param name="Severity">The severity level of this finding.</param>
/// <param name="Code">A machine-readable error code from <see cref="ConsistencyFindingCodes"/>.</param>
/// <param name="Message">A human-readable description of the consistency issue.</param>
/// <param name="PropertyPath">Optional property path for precise location.</param>
/// <param name="SuggestedFix">Optional human-readable suggestion for resolving the issue.</param>
public record ConsistencyFinding(
    string ValidatorId,
    ValidationSeverity Severity,
    string Code,
    string Message,
    string? PropertyPath = null,
    string? SuggestedFix = null
) : ValidationFinding(ValidatorId, Severity, Code, Message, PropertyPath, SuggestedFix)
{
    /// <summary>
    /// The existing claim that conflicts with the new claim.
    /// </summary>
    /// <value>The claim from the knowledge base that triggered the conflict.</value>
    public Claim? ExistingClaim { get; init; }

    /// <summary>
    /// Source document of the existing claim.
    /// </summary>
    /// <value>The GUID of the document containing the existing claim.</value>
    public Guid? ExistingClaimDocumentId { get; init; }

    /// <summary>
    /// The type of conflict detected.
    /// </summary>
    public ConflictType ConflictType { get; init; }

    /// <summary>
    /// Confidence in the conflict detection (0.0–1.0).
    /// </summary>
    public float ConflictConfidence { get; init; }

    /// <summary>
    /// Suggested resolution for the conflict.
    /// </summary>
    public ConflictResolution? Resolution { get; init; }
}
