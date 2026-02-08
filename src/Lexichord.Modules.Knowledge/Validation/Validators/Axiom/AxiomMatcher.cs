// =============================================================================
// File: AxiomMatcher.cs
// Project: Lexichord.Modules.Knowledge
// Description: Matches entities to applicable axioms by type and target kind.
// =============================================================================
// LOGIC: Filters a collection of axioms to find those applicable to a given
//   entity. Matching criteria:
//     1. Axiom must be enabled (IsEnabled == true)
//     2. Axiom.TargetKind must be Entity (we only validate entities here)
//     3. Axiom.TargetType must match entity.Type (case-insensitive)
//   Results are ordered by severity (errors first) for deterministic output.
//
// v0.6.5g: Axiom Validator (CKVS Phase 3a)
// Dependencies: Axiom (v0.4.6e), AxiomTargetKind (v0.4.6e),
//               KnowledgeEntity (v0.4.5e)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;

namespace Lexichord.Modules.Knowledge.Validation.Validators.Axiom;

/// <summary>
/// Matches entities to applicable axioms by type and target kind.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="AxiomMatcher"/> is an internal, stateless helper used by
/// <see cref="AxiomValidatorService"/> to filter axioms. It is deterministic
/// and thread-safe — the same inputs always produce the same output.
/// </para>
/// <para>
/// <b>Ordering:</b> Matched axioms are returned ordered by
/// <see cref="AxiomSeverity"/> (errors first), ensuring that the most
/// critical violations appear earliest in the findings list.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5g as part of the Axiom Validator.
/// </para>
/// </remarks>
internal static class AxiomMatcher
{
    /// <summary>
    /// Finds all axioms applicable to the given entity.
    /// </summary>
    /// <param name="entity">The entity to match axioms against.</param>
    /// <param name="axioms">The full set of axioms to filter.</param>
    /// <returns>
    /// Axioms whose <c>TargetType</c> matches the entity's <c>Type</c>,
    /// <c>TargetKind</c> is <see cref="AxiomTargetKind.Entity"/>, and
    /// <c>IsEnabled</c> is <c>true</c>. Ordered by severity (errors first).
    /// </returns>
    /// <remarks>
    /// LOGIC: Three-step filter:
    /// 1. Skip disabled axioms (IsEnabled == false)
    /// 2. Only include axioms targeting entities (TargetKind.Entity)
    /// 3. Match TargetType to entity.Type (case-insensitive)
    /// Then order by Severity ascending (Error=0 first, Info=2 last).
    /// </remarks>
    public static IReadOnlyList<Lexichord.Abstractions.Contracts.Knowledge.Axiom> FindMatchingAxioms(
        KnowledgeEntity entity,
        IReadOnlyList<Lexichord.Abstractions.Contracts.Knowledge.Axiom> axioms)
    {
        return axioms
            .Where(a => a.IsEnabled)
            .Where(a => a.TargetKind == AxiomTargetKind.Entity)
            .Where(a => string.Equals(a.TargetType, entity.Type, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Severity)
            .ToList();
    }

    /// <summary>
    /// Checks whether a single axiom applies to the given entity.
    /// </summary>
    /// <param name="axiom">The axiom to check.</param>
    /// <param name="entity">The entity to match against.</param>
    /// <returns>
    /// <c>true</c> if the axiom is enabled, targets entities, and its
    /// <c>TargetType</c> matches the entity's <c>Type</c>.
    /// </returns>
    public static bool DoesAxiomApply(Lexichord.Abstractions.Contracts.Knowledge.Axiom axiom, KnowledgeEntity entity)
    {
        if (!axiom.IsEnabled)
            return false;

        if (axiom.TargetKind != AxiomTargetKind.Entity)
            return false;

        return string.Equals(axiom.TargetType, entity.Type, StringComparison.OrdinalIgnoreCase);
    }
}
