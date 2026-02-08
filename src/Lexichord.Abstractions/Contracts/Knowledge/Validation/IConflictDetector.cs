// =============================================================================
// File: IConflictDetector.cs
// Project: Lexichord.Abstractions
// Description: Interface and data types for claim conflict detection.
// =============================================================================
// LOGIC: Defines the contract for detecting conflicts between two claims.
//   The ConflictResult record captures whether a conflict exists, its type,
//   confidence, and description. ConflictType enumerates all supported
//   conflict categories.
//
// v0.6.5h: Consistency Checker (CKVS Phase 3a)
// Dependencies: Claim (v0.5.6e)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Detects specific types of conflicts between two claims.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IConflictDetector"/> examines a pair of claims (new vs existing)
/// and determines whether they conflict. The detection is synchronous as it
/// operates on in-memory claim data without requiring I/O.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be stateless and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5h as part of the Consistency Checker.
/// </para>
/// </remarks>
public interface IConflictDetector
{
    /// <summary>
    /// Detects if two claims conflict.
    /// </summary>
    /// <param name="newClaim">New claim being validated.</param>
    /// <param name="existingClaim">Existing claim from the knowledge base.</param>
    /// <returns>
    /// A <see cref="ConflictResult"/> indicating whether a conflict was found
    /// and its characteristics.
    /// </returns>
    /// <remarks>
    /// LOGIC: Comparison steps (in order):
    /// <list type="number">
    ///   <item>If subjects differ, return no conflict.</item>
    ///   <item>If predicates differ, check for contradictory predicate pairs.</item>
    ///   <item>If same subject and predicate, compare objects for value/type conflicts.</item>
    /// </list>
    /// </remarks>
    ConflictResult DetectConflict(Claim newClaim, Claim existingClaim);
}

/// <summary>
/// Result of conflict detection between two claims.
/// </summary>
/// <remarks>
/// <para>
/// Immutable record capturing the outcome of a conflict check. The
/// <see cref="SuggestedResolution"/> property is populated by the
/// <see cref="IContradictionResolver"/> after detection.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5h as part of the Consistency Checker.
/// </para>
/// </remarks>
public record ConflictResult
{
    /// <summary>
    /// Whether a conflict was detected.
    /// </summary>
    public bool HasConflict { get; init; }

    /// <summary>
    /// Type of conflict detected.
    /// </summary>
    public ConflictType ConflictType { get; init; }

    /// <summary>
    /// Confidence in conflict detection (0.0–1.0).
    /// </summary>
    /// <value>
    /// Higher values indicate stronger confidence that the conflict is genuine.
    /// </value>
    public float Confidence { get; init; }

    /// <summary>
    /// Human-readable description of the conflict.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Suggested resolution (populated after detection).
    /// </summary>
    public ConflictResolution? SuggestedResolution { get; init; }
}

/// <summary>
/// Types of conflicts that can be detected between claims.
/// </summary>
/// <remarks>
/// <para>
/// Each conflict type maps to a <see cref="ConsistencyFindingCodes"/> constant
/// for programmatic filtering and UI categorization.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5h as part of the Consistency Checker.
/// </para>
/// </remarks>
public enum ConflictType
{
    /// <summary>No conflict detected.</summary>
    None,

    /// <summary>Direct value contradiction (same subject+predicate, different literal values).</summary>
    ValueContradiction,

    /// <summary>Conflicting property values for the same entity.</summary>
    PropertyConflict,

    /// <summary>Contradictory relationship (same subject, conflicting predicates or entity targets).</summary>
    RelationshipContradiction,

    /// <summary>Temporal inconsistency between claims.</summary>
    TemporalConflict,

    /// <summary>Cardinality violation (too many values for a single-value predicate).</summary>
    CardinalityConflict,

    /// <summary>Semantic contradiction detected via claim diff service.</summary>
    SemanticContradiction
}
