// =============================================================================
// File: IContradictionResolver.cs
// Project: Lexichord.Abstractions
// Description: Interface and data types for conflict resolution suggestions.
// =============================================================================
// LOGIC: After a conflict is detected by IConflictDetector, the
//   IContradictionResolver suggests how to resolve it. Resolutions range
//   from auto-applicable (e.g. VersionExisting for temporal conflicts) to
//   manual review (e.g. semantic contradictions).
//
// v0.6.5h: Consistency Checker (CKVS Phase 3a)
// Dependencies: Claim (v0.5.6e), ConflictType (v0.6.5h)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation;

/// <summary>
/// Suggests resolutions for detected claim conflicts.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IContradictionResolver"/> takes a detected conflict and
/// produces a <see cref="ConflictResolution"/> describing how to resolve it.
/// Resolution strategies vary by conflict type: value contradictions may
/// prefer the newer claim, while semantic contradictions require manual review.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be stateless and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5h as part of the Consistency Checker.
/// </para>
/// </remarks>
public interface IContradictionResolver
{
    /// <summary>
    /// Suggests a resolution for a detected conflict.
    /// </summary>
    /// <param name="newClaim">The new claim that triggered the conflict.</param>
    /// <param name="existingClaim">The existing claim from the knowledge base.</param>
    /// <param name="conflictType">The type of conflict detected.</param>
    /// <returns>
    /// A <see cref="ConflictResolution"/> describing the suggested resolution strategy.
    /// </returns>
    ConflictResolution SuggestResolution(
        Claim newClaim,
        Claim existingClaim,
        ConflictType conflictType);
}

/// <summary>
/// Suggested resolution for a detected claim conflict.
/// </summary>
/// <remarks>
/// <para>
/// Immutable record describing how a conflict should be resolved. The
/// <see cref="CanAutoApply"/> flag indicates whether the resolution can be
/// applied without human review (only for high-confidence strategies like
/// temporal versioning).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5h as part of the Consistency Checker.
/// </para>
/// </remarks>
public record ConflictResolution
{
    /// <summary>
    /// The resolution strategy to apply.
    /// </summary>
    public ResolutionStrategy Strategy { get; init; }

    /// <summary>
    /// Human-readable description of the resolution.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Confidence in this resolution suggestion (0.0–1.0).
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Whether this resolution can be auto-applied without human review.
    /// </summary>
    public bool CanAutoApply { get; init; }
}

/// <summary>
/// Strategies for resolving claim conflicts.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.6.5h as part of the Consistency Checker.
/// </para>
/// </remarks>
public enum ResolutionStrategy
{
    /// <summary>Keep the new claim, supersede existing.</summary>
    AcceptNew,

    /// <summary>Keep the existing claim, reject new.</summary>
    KeepExisting,

    /// <summary>Mark both as valid in different contexts.</summary>
    Contextualize,

    /// <summary>Merge claims into a unified statement.</summary>
    Merge,

    /// <summary>Require manual resolution by a human reviewer.</summary>
    ManualReview,

    /// <summary>Version the existing claim as historical, accept new as current.</summary>
    VersionExisting
}
