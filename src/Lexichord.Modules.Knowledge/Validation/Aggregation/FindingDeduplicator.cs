// =============================================================================
// File: FindingDeduplicator.cs
// Project: Lexichord.Modules.Knowledge
// Description: Deduplicates validation findings based on structural similarity.
// =============================================================================
// LOGIC: O(n²) pairwise comparison of findings. Two findings are duplicates if:
//   1. They share the same Code AND ValidatorId, AND
//   2. They share the same PropertyPath, OR their Messages are similar
//      (exact match or containment).
//
// Spec Adaptations:
//   - ValidatorName → ValidatorId
//   - Location overlap → PropertyPath equality (no Location/TextSpan on finding)
//   - RelatedEntity/RelatedClaim → omitted (not on finding)
//
// v0.6.5i: Validation Result Aggregator (CKVS Phase 3a)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Aggregation;

namespace Lexichord.Modules.Knowledge.Validation.Aggregation;

/// <summary>
/// Deduplicates <see cref="ValidationFinding"/> instances by structural similarity.
/// </summary>
/// <remarks>
/// <para>
/// Uses an O(n²) pairwise comparison. Two findings are considered duplicates when
/// they share the same <see cref="ValidationFinding.Code"/> and
/// <see cref="ValidationFinding.ValidatorId"/>, and either have the same
/// <see cref="ValidationFinding.PropertyPath"/> or similar
/// <see cref="ValidationFinding.Message"/> content.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5i as part of the Validation Result Aggregator.
/// </para>
/// </remarks>
public class FindingDeduplicator : IFindingDeduplicator
{
    /// <inheritdoc/>
    /// <remarks>
    /// Iterates through findings in order, retaining the first occurrence and
    /// skipping any subsequent findings that are duplicates of an already-seen one.
    /// </remarks>
    public IReadOnlyList<ValidationFinding> Deduplicate(
        IEnumerable<ValidationFinding> findings)
    {
        var result = new List<ValidationFinding>();
        var seen = new List<ValidationFinding>();

        foreach (var finding in findings)
        {
            // LOGIC: Check against all previously seen findings for duplicates.
            var isDuplicate = false;

            foreach (var existing in seen)
            {
                if (AreDuplicates(finding, existing))
                {
                    isDuplicate = true;
                    break;
                }
            }

            // LOGIC: Only add non-duplicate findings to the result.
            if (!isDuplicate)
            {
                result.Add(finding);
                seen.Add(finding);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Duplicate detection criteria (all must match):
    /// <list type="number">
    ///   <item>Same <see cref="ValidationFinding.Code"/>.</item>
    ///   <item>Same <see cref="ValidationFinding.ValidatorId"/>.</item>
    ///   <item>Same <see cref="ValidationFinding.PropertyPath"/> OR similar
    ///         <see cref="ValidationFinding.Message"/> content.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool AreDuplicates(ValidationFinding a, ValidationFinding b)
    {
        // LOGIC: Gate 1 — Code and ValidatorId must match exactly.
        if (a.Code != b.Code || a.ValidatorId != b.ValidatorId)
        {
            return false;
        }

        // LOGIC: Gate 2a — If both have PropertyPath and they match, it's a dup.
        if (a.PropertyPath != null && b.PropertyPath != null)
        {
            if (string.Equals(a.PropertyPath, b.PropertyPath, StringComparison.Ordinal))
            {
                return true;
            }
        }

        // LOGIC: Gate 2b — Fall back to message similarity check.
        return MessagesSimilar(a.Message, b.Message);
    }

    /// <summary>
    /// Checks if two messages are similar enough to be considered duplicates.
    /// </summary>
    /// <param name="a">First message.</param>
    /// <param name="b">Second message.</param>
    /// <returns><c>true</c> if messages are similar; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Uses case-insensitive exact match or containment check. If one message
    /// contains the other, they are considered similar (handles cases where one
    /// validator produces a more detailed version of the same message).
    /// </remarks>
    private static bool MessagesSimilar(string a, string b)
    {
        // LOGIC: Exact match (case-insensitive).
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // LOGIC: Containment check — one message is a substring of the other.
        if (a.Contains(b, StringComparison.OrdinalIgnoreCase) ||
            b.Contains(a, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
