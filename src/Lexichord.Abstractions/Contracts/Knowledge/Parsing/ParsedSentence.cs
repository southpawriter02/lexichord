// =============================================================================
// File: ParsedSentence.cs
// Project: Lexichord.Abstractions
// Description: A sentence with complete linguistic annotations.
// =============================================================================
// LOGIC: Represents a fully parsed sentence with tokens, dependency tree,
//   semantic frames, and named entities. Provides helper methods for
//   extracting key grammatical elements (root verb, subject, object).
//
// v0.5.6f: Sentence Parser (Claim Extraction Pipeline)
// Dependencies: Token, DependencyNode, DependencyRelation, SemanticFrame,
//               NamedEntity (all v0.5.6f)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Parsing;

/// <summary>
/// A sentence with complete linguistic annotations.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="ParsedSentence"/> contains all linguistic information extracted
/// from a sentence:
/// <list type="bullet">
///   <item><b>Tokens:</b> Individual words with POS tags and lemmas.</item>
///   <item><b>Dependencies:</b> Grammatical relations between words.</item>
///   <item><b>Semantic Frames:</b> Predicate-argument structures.</item>
///   <item><b>Entities:</b> Named entities (people, places, etc.).</item>
/// </list>
/// </para>
/// <para>
/// <b>Helper Methods:</b> Provides convenient access to key elements:
/// <list type="bullet">
///   <item><see cref="GetRootVerb"/>: The main verb of the sentence.</item>
///   <item><see cref="GetSubject"/>: The nominal subject.</item>
///   <item><see cref="GetDirectObject"/>: The direct object.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6f as part of the Sentence Parser.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await parser.ParseAsync("The endpoint accepts parameters.");
/// var sentence = result.Sentences[0];
///
/// var verb = sentence.GetRootVerb();     // "accepts"
/// var subject = sentence.GetSubject();   // "endpoint"
/// var dobj = sentence.GetDirectObject(); // "parameters"
/// </code>
/// </example>
public record ParsedSentence
{
    /// <summary>
    /// Unique identifier for this sentence.
    /// </summary>
    /// <value>A GUID generated on creation.</value>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The original sentence text.
    /// </summary>
    /// <value>The sentence as extracted from the source document.</value>
    public required string Text { get; init; }

    /// <summary>
    /// Tokens in the sentence.
    /// </summary>
    /// <value>Ordered list of tokens with linguistic annotations.</value>
    public required IReadOnlyList<Token> Tokens { get; init; }

    /// <summary>
    /// Root of the dependency tree.
    /// </summary>
    /// <value>
    /// The root node (typically the main verb). Null if dependency parsing
    /// was disabled or failed.
    /// </value>
    public DependencyNode? DependencyRoot { get; init; }

    /// <summary>
    /// All dependency relations in the sentence.
    /// </summary>
    /// <value>
    /// List of head-dependent-relation triples. Null if dependency parsing
    /// was disabled.
    /// </value>
    public IReadOnlyList<DependencyRelation>? Dependencies { get; init; }

    /// <summary>
    /// Semantic frames (predicate-argument structures).
    /// </summary>
    /// <value>
    /// List of frames from Semantic Role Labeling. Null if SRL was disabled.
    /// </value>
    public IReadOnlyList<SemanticFrame>? SemanticFrames { get; init; }

    /// <summary>
    /// Named entities in the sentence.
    /// </summary>
    /// <value>
    /// List of recognized entities. Null if NER was disabled.
    /// </value>
    public IReadOnlyList<NamedEntity>? Entities { get; init; }

    /// <summary>
    /// Start character offset in the source document.
    /// </summary>
    /// <value>Zero-based index where this sentence begins.</value>
    public int StartOffset { get; init; }

    /// <summary>
    /// End character offset in the source document.
    /// </summary>
    /// <value>Zero-based index past the last character (exclusive).</value>
    public int EndOffset { get; init; }

    /// <summary>
    /// Index of this sentence in the document.
    /// </summary>
    /// <value>Zero-based sentence number.</value>
    public int Index { get; init; }

    /// <summary>
    /// Gets the root verb of the sentence.
    /// </summary>
    /// <returns>
    /// The main verb token, or null if not found or no verb exists.
    /// </returns>
    /// <remarks>
    /// LOGIC: First checks if the dependency root is a verb, then searches
    /// for a ROOT relation with a verb dependent.
    /// </remarks>
    public Token? GetRootVerb()
    {
        // Check if the dependency root itself is a verb
        if (DependencyRoot?.Token.POS == "VERB")
        {
            return DependencyRoot.Token;
        }

        // Search dependencies for ROOT relation with a verb
        return Dependencies?.FirstOrDefault(d =>
            d.Relation == DependencyRelations.ROOT &&
            d.Dependent.POS == "VERB")?.Dependent;
    }

    /// <summary>
    /// Gets the subject of the root verb.
    /// </summary>
    /// <returns>
    /// The nominal subject token, or null if not found.
    /// </returns>
    /// <remarks>
    /// LOGIC: Finds the nsubj or nsubjpass relation where the head is the
    /// root verb.
    /// </remarks>
    public Token? GetSubject()
    {
        var root = GetRootVerb();
        if (root == null) return null;

        return Dependencies?.FirstOrDefault(d =>
            d.Head == root &&
            (d.Relation == DependencyRelations.NSUBJ ||
             d.Relation == DependencyRelations.NSUBJPASS))?.Dependent;
    }

    /// <summary>
    /// Gets the direct object of the root verb.
    /// </summary>
    /// <returns>
    /// The direct object token, or null if not found.
    /// </returns>
    /// <remarks>
    /// LOGIC: Finds the dobj relation where the head is the root verb.
    /// </remarks>
    public Token? GetDirectObject()
    {
        var root = GetRootVerb();
        if (root == null) return null;

        return Dependencies?.FirstOrDefault(d =>
            d.Head == root &&
            d.Relation == DependencyRelations.DOBJ)?.Dependent;
    }

    /// <summary>
    /// Gets all verbs in the sentence.
    /// </summary>
    /// <returns>List of tokens with POS tag "VERB".</returns>
    public IReadOnlyList<Token> GetVerbs()
    {
        return Tokens.Where(t => t.POS == "VERB").ToList();
    }

    /// <summary>
    /// Gets all nouns in the sentence.
    /// </summary>
    /// <returns>List of tokens with POS tag "NOUN" or "PROPN".</returns>
    public IReadOnlyList<Token> GetNouns()
    {
        return Tokens.Where(t => t.POS == "NOUN" || t.POS == "PROPN").ToList();
    }

    /// <summary>
    /// Gets content words (non-stop, non-punctuation tokens).
    /// </summary>
    /// <returns>List of meaningful tokens.</returns>
    public IReadOnlyList<Token> GetContentWords()
    {
        return Tokens.Where(t => !t.IsStopWord && !t.IsPunct).ToList();
    }
}
