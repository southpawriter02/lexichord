// =============================================================================
// File: ContextIssueCodes.cs
// Project: Lexichord.Abstractions
// Description: Well-known issue codes for pre-generation validation.
// =============================================================================
// LOGIC: Centralizes all issue codes used by the pre-generation validator
//   and consistency checker. Each code follows the PREVAL_ prefix convention
//   for easy identification in logs and UI. These codes enable programmatic
//   matching of issues (e.g., for filtering, suppression, or custom handling).
//
// v0.6.6f: Pre-Generation Validator (CKVS Phase 3b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Well-known issue codes for pre-generation validation.
/// </summary>
/// <remarks>
/// <para>
/// All codes use the <c>PREVAL_</c> prefix to distinguish them from other
/// validation systems (e.g., schema validation uses <c>SCHEMA_</c>, axiom
/// validation uses <c>AXIOM_</c>).
/// </para>
/// <para>
/// <b>Usage:</b> Compare against <see cref="ContextIssue.Code"/> to
/// programmatically identify specific issue types.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6f as part of the Pre-Generation Validator.
/// </para>
/// </remarks>
public static class ContextIssueCodes
{
    /// <summary>
    /// Multiple entities in the context have conflicting property values.
    /// </summary>
    public const string ConflictingEntities = "PREVAL_CONFLICTING_ENTITIES";

    /// <summary>
    /// A required entity referenced in the request is not present in the context.
    /// </summary>
    public const string MissingRequiredEntity = "PREVAL_MISSING_ENTITY";

    /// <summary>
    /// An entity in the context violates an applicable axiom rule.
    /// </summary>
    public const string AxiomViolation = "PREVAL_AXIOM_VIOLATION";

    /// <summary>
    /// The knowledge context data may be outdated.
    /// </summary>
    public const string StaleContext = "PREVAL_STALE_CONTEXT";

    /// <summary>
    /// No knowledge context is available for the request.
    /// </summary>
    public const string EmptyContext = "PREVAL_EMPTY_CONTEXT";

    /// <summary>
    /// The user's request conflicts with information in the context.
    /// </summary>
    public const string RequestConflict = "PREVAL_REQUEST_CONFLICT";

    /// <summary>
    /// The user's request is empty, unclear, or cannot be interpreted.
    /// </summary>
    public const string AmbiguousRequest = "PREVAL_AMBIGUOUS_REQUEST";

    /// <summary>
    /// An entity type in the context is not supported by the current configuration.
    /// </summary>
    public const string UnsupportedEntityType = "PREVAL_UNSUPPORTED_TYPE";
}
