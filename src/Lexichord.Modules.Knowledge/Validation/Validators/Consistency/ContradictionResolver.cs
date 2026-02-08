// =============================================================================
// File: ContradictionResolver.cs
// Project: Lexichord.Modules.Knowledge
// Description: Suggests resolutions for detected claim conflicts.
// =============================================================================
// LOGIC: Routes by ConflictType to specific resolution strategies:
//   - ValueContradiction: AcceptNew if newer, else ManualReview
//   - PropertyConflict: ManualReview
//   - RelationshipContradiction: VersionExisting if versioning scenario, else ManualReview
//   - TemporalConflict: VersionExisting (auto-applicable)
//   - SemanticContradiction: Contextualize
//   - Default: ManualReview
//
// v0.6.5h: Consistency Checker (CKVS Phase 3a)
// Dependencies: IContradictionResolver (v0.6.5h), Claim (v0.5.6e),
//               ConflictType (v0.6.5h), ConflictResolution (v0.6.5h)
// =============================================================================

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;

namespace Lexichord.Modules.Knowledge.Validation.Validators.Consistency;

/// <summary>
/// Suggests resolutions for detected claim conflicts.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ContradictionResolver"/> implements <see cref="IContradictionResolver"/>
/// using a strategy pattern that routes each <see cref="ConflictType"/> to a
/// specific resolution method. Strategies range from auto-applicable temporal
/// versioning to manual review for ambiguous conflicts.
/// </para>
/// <para>
/// <b>Stateless:</b> This implementation has no mutable state and is safe for
/// concurrent use from multiple threads.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5h as part of the Consistency Checker.
/// </para>
/// </remarks>
public partial class ContradictionResolver : IContradictionResolver
{
    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Routes to specific resolution methods based on conflict type.
    /// Unknown types fall through to ManualReview as the safest default.
    /// </remarks>
    public ConflictResolution SuggestResolution(
        Claim newClaim,
        Claim existingClaim,
        ConflictType conflictType)
    {
        return conflictType switch
        {
            ConflictType.ValueContradiction => ResolveValueContradiction(newClaim, existingClaim),
            ConflictType.PropertyConflict => ResolvePropertyConflict(),
            ConflictType.RelationshipContradiction => ResolveRelationshipConflict(newClaim, existingClaim),
            ConflictType.TemporalConflict => ResolveTemporalConflict(),
            ConflictType.SemanticContradiction => ResolveSemanticConflict(),
            _ => new ConflictResolution
            {
                Strategy = ResolutionStrategy.ManualReview,
                Description = "Manual review required to resolve this conflict",
                Confidence = 0.0f,
                CanAutoApply = false
            }
        };
    }

    /// <summary>
    /// Resolves value contradictions by preferring the newer claim.
    /// </summary>
    /// <remarks>
    /// LOGIC: If the new claim is more recent (by ExtractedAt timestamp),
    /// suggest accepting it. Otherwise, fall back to manual review.
    /// </remarks>
    private static ConflictResolution ResolveValueContradiction(Claim newClaim, Claim existingClaim)
    {
        // LOGIC: Prefer newer claim if from more recent extraction.
        if (newClaim.ExtractedAt > existingClaim.ExtractedAt)
        {
            return new ConflictResolution
            {
                Strategy = ResolutionStrategy.AcceptNew,
                Description = $"Accept new value '{newClaim.Object.LiteralValue}' as more recent",
                Confidence = 0.7f,
                CanAutoApply = false
            };
        }

        return new ConflictResolution
        {
            Strategy = ResolutionStrategy.ManualReview,
            Description = $"Conflicting values: '{newClaim.Object.LiteralValue}' vs " +
                          $"'{existingClaim.Object.LiteralValue}'. Manual review needed.",
            Confidence = 0.5f,
            CanAutoApply = false
        };
    }

    /// <summary>
    /// Resolves property conflicts (always requires manual review).
    /// </summary>
    /// <remarks>
    /// LOGIC: Property conflicts are ambiguous by nature — both claims may
    /// be valid depending on context. Manual review is the safest strategy.
    /// </remarks>
    private static ConflictResolution ResolvePropertyConflict()
    {
        return new ConflictResolution
        {
            Strategy = ResolutionStrategy.ManualReview,
            Description = "Property values conflict. Review both documents to determine correct value.",
            Confidence = 0.3f,
            CanAutoApply = false
        };
    }

    /// <summary>
    /// Resolves relationship contradictions, checking for versioning scenarios.
    /// </summary>
    /// <remarks>
    /// LOGIC: If either claim's subject mentions a version (e.g., "v2", "Version 1"),
    /// suggest versioning the existing claim. Otherwise, require manual review.
    /// </remarks>
    private static ConflictResolution ResolveRelationshipConflict(Claim newClaim, Claim existingClaim)
    {
        // LOGIC: Check if this could be a versioning issue.
        if (IsVersioningScenario(newClaim, existingClaim))
        {
            return new ConflictResolution
            {
                Strategy = ResolutionStrategy.VersionExisting,
                Description = "Mark existing claim as historical and accept new claim as current",
                Confidence = 0.6f,
                CanAutoApply = false
            };
        }

        return new ConflictResolution
        {
            Strategy = ResolutionStrategy.ManualReview,
            Description = "Contradictory relationships detected. Review and update documentation.",
            Confidence = 0.4f,
            CanAutoApply = false
        };
    }

    /// <summary>
    /// Resolves temporal conflicts by versioning the existing claim.
    /// </summary>
    /// <remarks>
    /// LOGIC: Temporal conflicts are the most clear-cut — the newer claim
    /// supersedes the older one. This is the only auto-applicable strategy.
    /// </remarks>
    private static ConflictResolution ResolveTemporalConflict()
    {
        return new ConflictResolution
        {
            Strategy = ResolutionStrategy.VersionExisting,
            Description = "Create versioned history: mark existing as historical, accept new as current",
            Confidence = 0.8f,
            CanAutoApply = true
        };
    }

    /// <summary>
    /// Resolves semantic contradictions by contextualizing both claims.
    /// </summary>
    /// <remarks>
    /// LOGIC: Semantic conflicts often arise from different contexts
    /// (e.g., dev vs production, v1 vs v2). Contextualizing both claims
    /// preserves knowledge while noting the discrepancy.
    /// </remarks>
    private static ConflictResolution ResolveSemanticConflict()
    {
        return new ConflictResolution
        {
            Strategy = ResolutionStrategy.Contextualize,
            Description = "Claims may be valid in different contexts. Consider adding context qualifiers.",
            Confidence = 0.5f,
            CanAutoApply = false
        };
    }

    /// <summary>
    /// Detects if two claims represent a versioning scenario.
    /// </summary>
    /// <param name="newClaim">New claim being validated.</param>
    /// <param name="existingClaim">Existing claim from the knowledge base.</param>
    /// <returns>True if either claim's subject mentions a version.</returns>
    /// <remarks>
    /// LOGIC: Matches patterns like "v1", "V2", "version 3" in subject surface forms.
    /// </remarks>
    private static bool IsVersioningScenario(Claim newClaim, Claim existingClaim)
    {
        var newHasVersion = VersionPattern().IsMatch(newClaim.Subject.SurfaceForm);
        var existingHasVersion = VersionPattern().IsMatch(existingClaim.Subject.SurfaceForm);

        return newHasVersion || existingHasVersion;
    }

    /// <summary>
    /// Compiled regex for detecting version patterns in surface forms.
    /// </summary>
    [GeneratedRegex(@"v\d+|version\s*\d+|V\d+", RegexOptions.IgnoreCase)]
    private static partial Regex VersionPattern();
}
