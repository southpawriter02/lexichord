// =============================================================================
// File: ContradictionType.cs
// Project: Lexichord.Abstractions
// Description: Types of contradictions between claims.
// =============================================================================
// LOGIC: Categorizes different types of claim contradictions for targeted
//   resolution strategies and user guidance.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: None (pure enum)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// Types of contradictions between claims.
/// </summary>
/// <remarks>
/// <para>
/// Contradictions occur when claims make incompatible assertions:
/// </para>
/// <list type="bullet">
///   <item><b>DirectContradiction:</b> Same subject and predicate but
///     different objects in different documents.</item>
///   <item><b>DeprecationConflict:</b> One source says deprecated,
///     another says active.</item>
///   <item><b>RequirementConflict:</b> Incompatible REQUIRES claims.</item>
///   <item><b>ValueConflict:</b> Numeric values outside expected range.</item>
///   <item><b>TemporalConflict:</b> Timeline inconsistencies.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
public enum ContradictionType
{
    /// <summary>
    /// Same subject and predicate, but different objects.
    /// </summary>
    /// <remarks>
    /// Example: "limit HAS_DEFAULT 10" vs "limit HAS_DEFAULT 20" in different documents.
    /// </remarks>
    DirectContradiction = 0,

    /// <summary>
    /// Conflicting deprecation status claims.
    /// </summary>
    /// <remarks>
    /// Example: One document claims IS_DEPRECATED, another uses without deprecation.
    /// </remarks>
    DeprecationConflict = 1,

    /// <summary>
    /// Conflicting requirement claims.
    /// </summary>
    /// <remarks>
    /// Example: "A REQUIRES B" and "A REQUIRES NOT B" (implicit from other claims).
    /// </remarks>
    RequirementConflict = 2,

    /// <summary>
    /// Numeric values out of expected range or consistency.
    /// </summary>
    /// <remarks>
    /// Example: "max_size HAS_VALUE 100" but "min_size HAS_VALUE 200".
    /// </remarks>
    ValueConflict = 3,

    /// <summary>
    /// Temporal or ordering inconsistencies.
    /// </summary>
    /// <remarks>
    /// Example: Claims about when features were introduced or deprecated.
    /// </remarks>
    TemporalConflict = 4
}
