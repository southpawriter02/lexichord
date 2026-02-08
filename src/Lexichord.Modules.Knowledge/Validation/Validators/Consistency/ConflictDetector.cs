// =============================================================================
// File: ConflictDetector.cs
// Project: Lexichord.Modules.Knowledge
// Description: Detects specific types of conflicts between claims.
// =============================================================================
// LOGIC: Compares two claims (new vs existing) to detect conflicts.
//   Steps: (1) Check subjects match, (2) Check predicates for contradictory
//   pairs, (3) Compare objects (literal vs literal, entity vs entity,
//   type mismatch). Returns ConflictResult with type and confidence.
//
// v0.6.5h: Consistency Checker (CKVS Phase 3a)
// Dependencies: IConflictDetector (v0.6.5h), Claim (v0.5.6e),
//               ClaimObject (v0.5.6e), ClaimEntity (v0.5.6e)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;

namespace Lexichord.Modules.Knowledge.Validation.Validators.Consistency;

/// <summary>
/// Detects specific types of conflicts between claims.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ConflictDetector"/> implements <see cref="IConflictDetector"/>
/// using structural comparison of claim triples. It checks subject identity,
/// predicate compatibility, and object value matching to identify conflicts.
/// </para>
/// <para>
/// <b>Stateless:</b> This implementation has no mutable state and is safe for
/// concurrent use from multiple threads.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5h as part of the Consistency Checker.
/// </para>
/// </remarks>
public class ConflictDetector : IConflictDetector
{
    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Comparison steps (in order):
    /// <list type="number">
    ///   <item>If subjects have different EntityIds, return no conflict.</item>
    ///   <item>If predicates differ, check for contradictory predicate pairs.</item>
    ///   <item>If same subject and predicate, compare objects for value conflicts.</item>
    /// </list>
    /// </remarks>
    public ConflictResult DetectConflict(Claim newClaim, Claim existingClaim)
    {
        // LOGIC: Different subjects cannot conflict.
        if (newClaim.Subject.EntityId != existingClaim.Subject.EntityId)
        {
            return new ConflictResult { HasConflict = false, ConflictType = ConflictType.None };
        }

        // LOGIC: Different predicates — check for contradictory pairs.
        if (newClaim.Predicate != existingClaim.Predicate)
        {
            return CheckPredicateConflict(newClaim, existingClaim);
        }

        // LOGIC: Same subject and predicate — compare objects.
        return CheckObjectConflict(newClaim, existingClaim);
    }

    /// <summary>
    /// Checks for object-level conflicts between two claims with the same subject and predicate.
    /// </summary>
    /// <param name="newClaim">New claim being validated.</param>
    /// <param name="existingClaim">Existing claim from the knowledge base.</param>
    /// <returns>Conflict result based on object comparison.</returns>
    /// <remarks>
    /// LOGIC: Three comparison paths:
    /// <list type="number">
    ///   <item>Both literals → string/numeric comparison.</item>
    ///   <item>Both entity refs → EntityId comparison + single-value predicate check.</item>
    ///   <item>Type mismatch (one literal, one entity) → ValueContradiction.</item>
    /// </list>
    /// </remarks>
    private ConflictResult CheckObjectConflict(Claim newClaim, Claim existingClaim)
    {
        var newObj = newClaim.Object;
        var existingObj = existingClaim.Object;

        // LOGIC: Both are literals — compare values.
        if (newObj.LiteralValue != null && existingObj.LiteralValue != null)
        {
            if (!ObjectsMatch(newObj.LiteralValue, existingObj.LiteralValue))
            {
                return new ConflictResult
                {
                    HasConflict = true,
                    ConflictType = ConflictType.ValueContradiction,
                    Confidence = 0.9f,
                    Description = $"Conflicting values: '{newObj.LiteralValue}' vs '{existingObj.LiteralValue}' " +
                                  $"for {newClaim.Subject.SurfaceForm} {newClaim.Predicate}"
                };
            }
        }

        // LOGIC: Both are entity references — compare target entities.
        if (newObj.Entity != null && existingObj.Entity != null)
        {
            if (newObj.Entity.EntityId != existingObj.Entity.EntityId)
            {
                // LOGIC: Only conflict if predicate expects a single value.
                if (IsSingleValuePredicate(newClaim.Predicate))
                {
                    return new ConflictResult
                    {
                        HasConflict = true,
                        ConflictType = ConflictType.RelationshipContradiction,
                        Confidence = 0.85f,
                        Description = $"Conflicting relationships: {newClaim.Subject.SurfaceForm} " +
                                      $"{newClaim.Predicate} points to different entities"
                    };
                }
            }
        }

        // LOGIC: Type mismatch — one literal, one entity.
        if ((newObj.LiteralValue != null) != (existingObj.LiteralValue != null))
        {
            return new ConflictResult
            {
                HasConflict = true,
                ConflictType = ConflictType.ValueContradiction,
                Confidence = 0.7f,
                Description = "Object type mismatch: one is literal, other is entity reference"
            };
        }

        // LOGIC: Objects match — no conflict.
        return new ConflictResult { HasConflict = false, ConflictType = ConflictType.None };
    }

    /// <summary>
    /// Checks for contradictory predicate pairs between two claims.
    /// </summary>
    /// <param name="newClaim">New claim being validated.</param>
    /// <param name="existingClaim">Existing claim from the knowledge base.</param>
    /// <returns>Conflict result if predicates are contradictory.</returns>
    /// <remarks>
    /// LOGIC: Maintains a dictionary of known contradictory predicate pairs
    /// (e.g., IS_REQUIRED ↔ IS_OPTIONAL). Checks both directions.
    /// </remarks>
    private static ConflictResult CheckPredicateConflict(Claim newClaim, Claim existingClaim)
    {
        // LOGIC: Known contradictory predicate pairs.
        var contradictions = new Dictionary<string, string[]>
        {
            ["IS_REQUIRED"] = ["IS_OPTIONAL"],
            ["IS_DEPRECATED"] = ["IS_ACTIVE", "IS_CURRENT"],
            ["ACCEPTS"] = ["REJECTS"],
            ["SUPPORTS"] = ["DOES_NOT_SUPPORT"]
        };

        foreach (var pair in contradictions)
        {
            // LOGIC: Check both directions (new→existing and existing→new).
            if ((newClaim.Predicate == pair.Key &&
                 pair.Value.Contains(existingClaim.Predicate)) ||
                (existingClaim.Predicate == pair.Key &&
                 pair.Value.Contains(newClaim.Predicate)))
            {
                return new ConflictResult
                {
                    HasConflict = true,
                    ConflictType = ConflictType.RelationshipContradiction,
                    Confidence = 0.95f,
                    Description = $"Contradictory predicates: '{newClaim.Predicate}' vs '{existingClaim.Predicate}'"
                };
            }
        }

        return new ConflictResult { HasConflict = false, ConflictType = ConflictType.None };
    }

    /// <summary>
    /// Compares two literal values for equality.
    /// </summary>
    /// <param name="a">First literal value.</param>
    /// <param name="b">Second literal value.</param>
    /// <returns>True if the values are considered equal.</returns>
    /// <remarks>
    /// LOGIC: Attempts numeric comparison first (with ε = 0.0001), then
    /// falls back to case-insensitive string comparison.
    /// </remarks>
    private static bool ObjectsMatch(string? a, string? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        // LOGIC: Try numeric comparison first.
        if (double.TryParse(a, out var da) && double.TryParse(b, out var db))
        {
            return Math.Abs(da - db) < 0.0001;
        }

        // LOGIC: Fall back to case-insensitive string comparison.
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if a predicate typically has a single value.
    /// </summary>
    /// <param name="predicate">The predicate to check.</param>
    /// <returns>True if the predicate expects a single value.</returns>
    /// <remarks>
    /// LOGIC: Predicates like HAS_TYPE, RETURNS, IS_REQUIRED naturally have
    /// a single value. Multi-value predicates like ACCEPTS allow multiple
    /// object targets without conflict.
    /// </remarks>
    private static bool IsSingleValuePredicate(string predicate)
    {
        var singleValue = new[]
        {
            "HAS_METHOD", "HAS_TYPE", "HAS_STATUS", "RETURNS",
            "IS_REQUIRED", "IS_DEPRECATED", "HAS_DEFAULT"
        };

        return singleValue.Contains(predicate, StringComparer.OrdinalIgnoreCase);
    }
}
