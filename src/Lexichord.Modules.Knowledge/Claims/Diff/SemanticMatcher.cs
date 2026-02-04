// =============================================================================
// File: SemanticMatcher.cs
// Project: Lexichord.Modules.Knowledge
// Description: Semantic matching for claim similarity comparison.
// =============================================================================
// LOGIC: Uses Jaro-Winkler string similarity to find semantically similar
//   claims. Compares subject, predicate, and object components separately
//   and combines into an overall similarity score.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: Claim (v0.5.6e), ClaimEntity (v0.5.6e), ClaimObject (v0.5.6e)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Claims;

namespace Lexichord.Modules.Knowledge.Claims.Diff;

/// <summary>
/// Provides semantic matching for claim similarity comparison.
/// </summary>
/// <remarks>
/// <para>
/// Uses Jaro-Winkler string similarity for fuzzy matching between claims.
/// Components are weighted as follows:
/// </para>
/// <list type="bullet">
///   <item><b>Subject:</b> 35% weight — entity matching.</item>
///   <item><b>Predicate:</b> 30% weight — relationship type.</item>
///   <item><b>Object:</b> 35% weight — value or entity matching.</item>
/// </list>
/// <para>
/// <b>Performance:</b> O(n*m) where n and m are claim counts. For large sets,
/// consider indexing strategies.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
public class SemanticMatcher
{
    // Component weights for similarity calculation
    private const float SubjectWeight = 0.35f;
    private const float PredicateWeight = 0.30f;
    private const float ObjectWeight = 0.35f;

    // Jaro-Winkler scaling factor (0.0 to 0.25)
    private const float JaroWinklerScalingFactor = 0.1f;

    // Maximum prefix length for Jaro-Winkler
    private const int MaxPrefixLength = 4;

    /// <summary>
    /// Finds the best match for a claim in a set of candidates.
    /// </summary>
    /// <param name="target">The claim to find a match for.</param>
    /// <param name="candidates">Potential matching claims.</param>
    /// <param name="threshold">Minimum similarity to consider a match (0.0-1.0).</param>
    /// <returns>
    /// The best matching claim and its similarity score, or (null, 0) if no match.
    /// </returns>
    /// <remarks>
    /// LOGIC: Iterates through candidates computing similarity. Returns the
    /// best match above threshold. Early exits if perfect match found.
    /// </remarks>
    public (Claim? Match, float Similarity) FindMatch(
        Claim target,
        IReadOnlyList<Claim> candidates,
        float threshold)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count == 0)
            return (null, 0f);

        Claim? bestMatch = null;
        float bestSimilarity = 0f;

        foreach (var candidate in candidates)
        {
            var similarity = ComputeSimilarity(target, candidate);

            if (similarity > bestSimilarity && similarity >= threshold)
            {
                bestMatch = candidate;
                bestSimilarity = similarity;

                // Early exit on exact match
                if (similarity >= 0.999f)
                    break;
            }
        }

        return (bestMatch, bestSimilarity);
    }

    /// <summary>
    /// Computes similarity between two claims.
    /// </summary>
    /// <param name="a">First claim.</param>
    /// <param name="b">Second claim.</param>
    /// <returns>Similarity score from 0.0 (no match) to 1.0 (exact match).</returns>
    /// <remarks>
    /// LOGIC: Combines subject, predicate, and object similarity with weights.
    /// Exact ID match returns 1.0 immediately.
    /// </remarks>
    public float ComputeSimilarity(Claim a, Claim b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        // Exact ID match
        if (a.Id == b.Id)
            return 1.0f;

        // Compute component similarities
        var subjectSim = ComputeEntitySimilarity(a.Subject, b.Subject);
        var predicateSim = ComputePredicateSimilarity(a.Predicate, b.Predicate);
        var objectSim = ComputeObjectSimilarity(a.Object, b.Object);

        // Weighted combination
        return (subjectSim * SubjectWeight) +
               (predicateSim * PredicateWeight) +
               (objectSim * ObjectWeight);
    }

    /// <summary>
    /// Computes similarity between two claim entities.
    /// </summary>
    /// <param name="a">First entity.</param>
    /// <param name="b">Second entity.</param>
    /// <returns>Similarity score from 0.0 to 1.0.</returns>
    /// <remarks>
    /// LOGIC: Resolved entities compare by ID. Unresolved entities compare
    /// by normalized form with type bonus for matching types.
    /// </remarks>
    public float ComputeEntitySimilarity(ClaimEntity a, ClaimEntity b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        // If both resolved to same entity ID, exact match
        if (a.IsResolved && b.IsResolved && a.EntityId == b.EntityId)
            return 1.0f;

        // Compare normalized forms
        var formSimilarity = JaroWinkler(
            a.NormalizedForm ?? a.SurfaceForm.ToLowerInvariant(),
            b.NormalizedForm ?? b.SurfaceForm.ToLowerInvariant());

        // Bonus for matching entity types
        var typeBonus = string.Equals(a.EntityType, b.EntityType, StringComparison.OrdinalIgnoreCase)
            ? 0.1f : 0f;

        return Math.Min(1.0f, formSimilarity + typeBonus);
    }

    /// <summary>
    /// Computes similarity between two predicates.
    /// </summary>
    /// <param name="a">First predicate.</param>
    /// <param name="b">Second predicate.</param>
    /// <returns>Similarity score from 0.0 to 1.0.</returns>
    /// <remarks>
    /// LOGIC: Predicates are typically standardized strings, so exact match
    /// is expected. Fuzzy matching handles minor variations.
    /// </remarks>
    public float ComputePredicateSimilarity(string a, string b)
    {
        // Exact match is common for standardized predicates
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            return 1.0f;

        // Fuzzy match for variations
        return JaroWinkler(a.ToUpperInvariant(), b.ToUpperInvariant());
    }

    /// <summary>
    /// Computes similarity between two claim objects.
    /// </summary>
    /// <param name="a">First object.</param>
    /// <param name="b">Second object.</param>
    /// <returns>Similarity score from 0.0 to 1.0.</returns>
    /// <remarks>
    /// LOGIC: Different object types have low similarity. Entities use
    /// entity comparison, literals use string comparison.
    /// </remarks>
    public float ComputeObjectSimilarity(ClaimObject a, ClaimObject b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        // Different types have low base similarity
        if (a.Type != b.Type)
            return 0.2f;

        return a.Type switch
        {
            ClaimObjectType.Entity when a.Entity is not null && b.Entity is not null
                => ComputeEntitySimilarity(a.Entity, b.Entity),

            ClaimObjectType.Literal
                => ComputeLiteralSimilarity(a, b),

            _ => 0.5f  // Unknown types get moderate similarity
        };
    }

    /// <summary>
    /// Computes similarity between literal values.
    /// </summary>
    private float ComputeLiteralSimilarity(ClaimObject a, ClaimObject b)
    {
        var aValue = a.LiteralValue ?? string.Empty;
        var bValue = b.LiteralValue ?? string.Empty;

        // Exact match
        if (string.Equals(aValue, bValue, StringComparison.Ordinal))
            return 1.0f;

        // Try numeric comparison for numeric literal types (based on LiteralType)
        if (a.LiteralType is "int" or "decimal" or "integer" or "number" &&
            b.LiteralType is "int" or "decimal" or "integer" or "number")
        {
            if (decimal.TryParse(aValue, out var aNum) && decimal.TryParse(bValue, out var bNum))
            {
                // Close numeric values have higher similarity
                var max = Math.Max(Math.Abs(aNum), Math.Abs(bNum));
                if (max == 0) return aNum == bNum ? 1.0f : 0.0f;

                var diff = Math.Abs(aNum - bNum) / max;
                return Math.Max(0, 1.0f - (float)diff);
            }
        }

        // Fall back to string comparison
        return JaroWinkler(aValue, bValue);
    }

    /// <summary>
    /// Calculates Jaro-Winkler similarity between two strings.
    /// </summary>
    /// <param name="s1">First string.</param>
    /// <param name="s2">Second string.</param>
    /// <returns>Similarity score from 0.0 to 1.0.</returns>
    /// <remarks>
    /// LOGIC: Jaro-Winkler is well-suited for comparing names and short strings.
    /// It gives higher scores to strings that match from the beginning.
    /// </remarks>
    public static float JaroWinkler(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
            return 1.0f;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0.0f;
        if (s1.Equals(s2, StringComparison.Ordinal))
            return 1.0f;

        var jaroScore = CalculateJaro(s1, s2);

        // Count common prefix (up to max length)
        var prefixLength = 0;
        var maxPrefix = Math.Min(MaxPrefixLength, Math.Min(s1.Length, s2.Length));
        while (prefixLength < maxPrefix && s1[prefixLength] == s2[prefixLength])
            prefixLength++;

        // Apply Jaro-Winkler adjustment
        return jaroScore + (prefixLength * JaroWinklerScalingFactor * (1 - jaroScore));
    }

    /// <summary>
    /// Calculates base Jaro similarity.
    /// </summary>
    private static float CalculateJaro(string s1, string s2)
    {
        var matchDistance = Math.Max(s1.Length, s2.Length) / 2 - 1;
        if (matchDistance < 0) matchDistance = 0;

        var s1Matches = new bool[s1.Length];
        var s2Matches = new bool[s2.Length];

        var matches = 0;
        var transpositions = 0;

        // Find matches
        for (var i = 0; i < s1.Length; i++)
        {
            var start = Math.Max(0, i - matchDistance);
            var end = Math.Min(i + matchDistance + 1, s2.Length);

            for (var j = start; j < end; j++)
            {
                if (s2Matches[j] || s1[i] != s2[j])
                    continue;

                s1Matches[i] = true;
                s2Matches[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0)
            return 0.0f;

        // Count transpositions
        var k = 0;
        for (var i = 0; i < s1.Length; i++)
        {
            if (!s1Matches[i])
                continue;

            while (!s2Matches[k])
                k++;

            if (s1[i] != s2[k])
                transpositions++;

            k++;
        }

        var jaro = ((float)matches / s1.Length +
                    (float)matches / s2.Length +
                    (float)(matches - transpositions / 2) / matches) / 3;

        return jaro;
    }
}
