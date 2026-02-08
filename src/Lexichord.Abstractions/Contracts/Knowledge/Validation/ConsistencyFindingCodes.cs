// =============================================================================
// File: ConsistencyFindingCodes.cs
// Project: Lexichord.Abstractions
// Description: Machine-readable finding codes for the Consistency Checker.
// =============================================================================
// LOGIC: Each constant follows the CONSISTENCY_* prefix convention.
//   These codes are used in ValidationFinding.Code for programmatic filtering,
//   localization lookup, and UI categorization of consistency issues.
//
// v0.6.5h: Consistency Checker (CKVS Phase 3a)
// Dependencies: None (standalone constants)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Machine-readable finding codes for consistency validation.
/// </summary>
/// <remarks>
/// <para>
/// These codes categorize the types of consistency issues detected by the
/// <see cref="IConsistencyChecker"/>. Each code maps to a specific
/// <see cref="ConflictType"/> for programmatic filtering.
/// </para>
/// <para>
/// <b>Convention:</b> All codes use the <c>CONSISTENCY_</c> prefix to distinguish
/// them from schema (<c>SCHEMA_</c>) and axiom (<c>AXIOM_</c>) finding codes.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5h as part of the Consistency Checker.
/// </para>
/// </remarks>
public static class ConsistencyFindingCodes
{
    /// <summary>
    /// Generic consistency conflict (fallback code).
    /// </summary>
    public const string ConsistencyConflict = "CONSISTENCY_CONFLICT";

    /// <summary>
    /// Direct value contradiction between claims.
    /// </summary>
    public const string ValueContradiction = "CONSISTENCY_VALUE_CONTRADICTION";

    /// <summary>
    /// Conflicting property values for the same entity.
    /// </summary>
    public const string PropertyConflict = "CONSISTENCY_PROPERTY_CONFLICT";

    /// <summary>
    /// Contradictory relationship between entities.
    /// </summary>
    public const string RelationshipConflict = "CONSISTENCY_RELATIONSHIP_CONFLICT";

    /// <summary>
    /// Temporal inconsistency between claims.
    /// </summary>
    public const string TemporalConflict = "CONSISTENCY_TEMPORAL_CONFLICT";

    /// <summary>
    /// Semantic contradiction detected via claim diff service.
    /// </summary>
    public const string SemanticConflict = "CONSISTENCY_SEMANTIC_CONFLICT";

    /// <summary>
    /// Duplicate claim detected.
    /// </summary>
    public const string DuplicateClaim = "CONSISTENCY_DUPLICATE";
}
