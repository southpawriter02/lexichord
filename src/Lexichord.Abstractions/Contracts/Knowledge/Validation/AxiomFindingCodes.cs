// =============================================================================
// File: AxiomFindingCodes.cs
// Project: Lexichord.Abstractions
// Description: Machine-readable finding codes for axiom validation.
// =============================================================================
// LOGIC: Defines constant strings used as the 'Code' field in
//   ValidationFinding instances produced by the AxiomValidatorService.
//   Each code corresponds to a specific class of axiom violation,
//   enabling programmatic filtering, UI grouping, and localization.
//   Follows the AXIOM_* prefix convention for namespace isolation
//   from SchemaFindingCodes (SCHEMA_*) and other validator codes.
//
// v0.6.5g: Axiom Validator (CKVS Phase 3a)
// Dependencies: None (pure constants)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Machine-readable finding codes produced by the Axiom Validator.
/// </summary>
/// <remarks>
/// <para>
/// Each constant corresponds to a specific class of axiom violation detected
/// during entity validation against domain rules. Codes follow the
/// <c>AXIOM_*</c> prefix convention for namespace isolation from other
/// validator finding codes (e.g., <see cref="SchemaFindingCodes"/>).
/// </para>
/// <para>
/// <b>Usage:</b> These codes appear in <see cref="ValidationFinding.Code"/> and
/// can be used for:
/// <list type="bullet">
///   <item>Programmatic filtering in the validation results panel.</item>
///   <item>Localization lookup for translated error messages.</item>
///   <item>Telemetry categorization of axiom validation failures.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5g as part of the Axiom Validator.
/// </para>
/// </remarks>
public static class AxiomFindingCodes
{
    /// <summary>Generic axiom violation (catch-all for unmapped constraint types).</summary>
    public const string AxiomViolation = "AXIOM_VIOLATION";

    /// <summary>A required property is missing or empty.</summary>
    public const string RequiredViolation = "AXIOM_REQUIRED";

    /// <summary>A property value is not in the allowed set (OneOf/NotOneOf constraint).</summary>
    public const string PropertyConstraint = "AXIOM_PROPERTY_CONSTRAINT";

    /// <summary>A collection property's count is outside the allowed bounds.</summary>
    public const string CardinalityViolation = "AXIOM_CARDINALITY";

    /// <summary>A string property does not match its regex pattern.</summary>
    public const string PatternViolation = "AXIOM_PATTERN";

    /// <summary>A numeric property is outside its allowed range.</summary>
    public const string RangeViolation = "AXIOM_RANGE";

    /// <summary>Mutually exclusive properties both have values (NotBoth constraint).</summary>
    public const string MutualExclusionViolation = "AXIOM_MUTUAL_EXCLUSION";

    /// <summary>Co-dependent properties are incomplete (RequiresTogether constraint).</summary>
    public const string DependencyViolation = "AXIOM_DEPENDENCY";

    /// <summary>A property value does not equal the expected value (Equals constraint).</summary>
    public const string EqualityViolation = "AXIOM_EQUALITY";

    /// <summary>A property value equals a forbidden value (NotEquals constraint).</summary>
    public const string InequalityViolation = "AXIOM_INEQUALITY";

    /// <summary>A relationship or reference constraint is invalid.</summary>
    public const string RelationshipInvalid = "AXIOM_RELATIONSHIP_INVALID";

    /// <summary>No axioms were found for the entity type (informational).</summary>
    public const string NoAxiomsFound = "AXIOM_NONE_FOUND";
}
