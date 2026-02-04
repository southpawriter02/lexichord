// =============================================================================
// File: DependencyPattern.cs
// Project: Lexichord.Abstractions
// Description: Pattern based on dependency parse relations for claim extraction.
// =============================================================================
// LOGIC: Defines patterns that match claims based on grammatical structure
//   (dependency relations) rather than surface text patterns.
//
// v0.5.6g: Claim Extractor (Knowledge Graph Claim Extraction Pipeline)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.ClaimExtraction;

/// <summary>
/// A pattern based on dependency parse tree relations.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Matches claims based on grammatical structure rather
/// than surface text. More robust to paraphrasing and word order changes.
/// </para>
/// <para>
/// <b>Usage:</b> Used by the <see cref="ExtractionPattern"/> when
/// <see cref="ExtractionPattern.Type"/> is <see cref="PatternType.Dependency"/>.
/// </para>
/// <para>
/// <b>Example:</b> A pattern matching "X requires Y" would specify:
/// <list type="bullet">
/// <item><description>SubjectRelations: ["nsubj"]</description></item>
/// <item><description>ObjectRelations: ["dobj"]</description></item>
/// <item><description>VerbLemmas: ["require"]</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6g as part of the Claim Extractor.
/// </para>
/// </remarks>
public record DependencyPattern
{
    /// <summary>
    /// Gets the required dependency relations from verb to subject.
    /// </summary>
    /// <value>
    /// List of acceptable dependency relation types for the subject
    /// (e.g., "nsubj", "nsubj:pass"). If null or empty, defaults to common
    /// subject relations.
    /// </value>
    public IReadOnlyList<string>? SubjectRelations { get; init; }

    /// <summary>
    /// Gets the required dependency relations from verb to object.
    /// </summary>
    /// <value>
    /// List of acceptable dependency relation types for the object
    /// (e.g., "dobj", "obj", "pobj"). If null or empty, defaults to common
    /// object relations.
    /// </value>
    public IReadOnlyList<string>? ObjectRelations { get; init; }

    /// <summary>
    /// Gets the required verb lemmas for matching.
    /// </summary>
    /// <value>
    /// List of verb lemmas that trigger this pattern (e.g., "accept", "require").
    /// If null or empty, matches any verb.
    /// </value>
    public IReadOnlyList<string>? VerbLemmas { get; init; }

    /// <summary>
    /// Gets required prepositions for indirect object patterns.
    /// </summary>
    /// <value>
    /// List of prepositions for patterns like "X depends on Y"
    /// (e.g., "on", "with", "for"). If null, no preposition required.
    /// </value>
    public IReadOnlyList<string>? Prepositions { get; init; }

    /// <summary>
    /// Gets whether the subject and object can be swapped (passive voice).
    /// </summary>
    /// <value>
    /// If <c>true</c>, the pattern matches passive constructions where
    /// the grammatical subject is the semantic object.
    /// </value>
    public bool AllowPassive { get; init; } = true;

    /// <summary>
    /// Checks whether a verb lemma matches this pattern.
    /// </summary>
    /// <param name="lemma">The verb lemma to check.</param>
    /// <returns><c>true</c> if the lemma matches or no lemmas are specified.</returns>
    public bool MatchesVerb(string lemma)
    {
        if (VerbLemmas == null || VerbLemmas.Count == 0)
        {
            return true;
        }

        return VerbLemmas.Contains(lemma, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks whether a relation matches the subject relations.
    /// </summary>
    /// <param name="relation">The relation to check.</param>
    /// <returns><c>true</c> if the relation is a valid subject relation.</returns>
    public bool IsSubjectRelation(string relation)
    {
        var subjectRels = SubjectRelations ?? new[] { "nsubj", "nsubj:pass", "csubj" };
        return subjectRels.Contains(relation, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks whether a relation matches the object relations.
    /// </summary>
    /// <param name="relation">The relation to check.</param>
    /// <returns><c>true</c> if the relation is a valid object relation.</returns>
    public bool IsObjectRelation(string relation)
    {
        var objectRels = ObjectRelations ?? new[] { "dobj", "obj", "pobj", "iobj" };
        return objectRels.Contains(relation, StringComparer.OrdinalIgnoreCase);
    }
}
