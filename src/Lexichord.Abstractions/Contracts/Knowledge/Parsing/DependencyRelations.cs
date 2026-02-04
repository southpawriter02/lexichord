// =============================================================================
// File: DependencyRelations.cs
// Project: Lexichord.Abstractions
// Description: Constants for common dependency relation types.
// =============================================================================
// LOGIC: Provides string constants for dependency relations used in parsing.
//   Based on Universal Dependencies (UD) and Stanford Dependencies conventions.
//
// v0.5.6f: Sentence Parser (Claim Extraction Pipeline)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Parsing;

/// <summary>
/// Constants for common dependency relation types.
/// </summary>
/// <remarks>
/// <para>
/// These constants represent grammatical relations in dependency parsing,
/// based on Universal Dependencies and Stanford Dependencies conventions.
/// </para>
/// <para>
/// <b>Core Relations:</b>
/// <list type="bullet">
///   <item><see cref="ROOT"/>: The root of the dependency tree.</item>
///   <item><see cref="NSUBJ"/>: Nominal subject of a clause.</item>
///   <item><see cref="DOBJ"/>: Direct object of a verb.</item>
///   <item><see cref="IOBJ"/>: Indirect object of a verb.</item>
/// </list>
/// </para>
/// <para>
/// <b>Modifier Relations:</b>
/// <list type="bullet">
///   <item><see cref="AMOD"/>: Adjectival modifier.</item>
///   <item><see cref="ADVMOD"/>: Adverbial modifier.</item>
///   <item><see cref="PREP"/>: Prepositional modifier.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6f as part of the Sentence Parser.
/// </para>
/// </remarks>
public static class DependencyRelations
{
    /// <summary>
    /// Root of the dependency tree.
    /// </summary>
    /// <remarks>
    /// LOGIC: The main predicate of the sentence, typically a verb.
    /// </remarks>
    public const string ROOT = "ROOT";

    /// <summary>
    /// Nominal subject.
    /// </summary>
    /// <remarks>
    /// LOGIC: The noun phrase that is the subject of a clause.
    /// Example: "endpoint" in "The endpoint accepts parameters."
    /// </remarks>
    public const string NSUBJ = "nsubj";

    /// <summary>
    /// Passive nominal subject.
    /// </summary>
    /// <remarks>
    /// LOGIC: The noun phrase that is the subject of a passive clause.
    /// Example: "parameters" in "Parameters are accepted by the endpoint."
    /// </remarks>
    public const string NSUBJPASS = "nsubjpass";

    /// <summary>
    /// Direct object.
    /// </summary>
    /// <remarks>
    /// LOGIC: The noun phrase that is the direct object of a verb.
    /// Example: "parameters" in "The endpoint accepts parameters."
    /// </remarks>
    public const string DOBJ = "dobj";

    /// <summary>
    /// Indirect object.
    /// </summary>
    /// <remarks>
    /// LOGIC: The noun phrase that is the indirect object of a verb.
    /// Example: "user" in "The API returns data to the user."
    /// </remarks>
    public const string IOBJ = "iobj";

    /// <summary>
    /// Object of a preposition.
    /// </summary>
    public const string POBJ = "pobj";

    /// <summary>
    /// Prepositional modifier.
    /// </summary>
    public const string PREP = "prep";

    /// <summary>
    /// Adjectival modifier.
    /// </summary>
    /// <remarks>
    /// LOGIC: An adjective modifying a noun.
    /// Example: "optional" in "an optional parameter."
    /// </remarks>
    public const string AMOD = "amod";

    /// <summary>
    /// Adverbial modifier.
    /// </summary>
    public const string ADVMOD = "advmod";

    /// <summary>
    /// Compound modifier.
    /// </summary>
    /// <remarks>
    /// LOGIC: A noun modifying another noun.
    /// Example: "rate" in "rate limiting."
    /// </remarks>
    public const string COMPOUND = "compound";

    /// <summary>
    /// Conjunct.
    /// </summary>
    public const string CONJ = "conj";

    /// <summary>
    /// Coordinating conjunction.
    /// </summary>
    public const string CC = "cc";

    /// <summary>
    /// Punctuation.
    /// </summary>
    public const string PUNCT = "punct";

    /// <summary>
    /// Auxiliary verb.
    /// </summary>
    public const string AUX = "aux";

    /// <summary>
    /// Passive auxiliary.
    /// </summary>
    public const string AUXPASS = "auxpass";

    /// <summary>
    /// Attribute.
    /// </summary>
    public const string ATTR = "attr";

    /// <summary>
    /// Relative clause modifier.
    /// </summary>
    public const string RELCL = "relcl";

    /// <summary>
    /// Open clausal complement.
    /// </summary>
    public const string XCOMP = "xcomp";

    /// <summary>
    /// Determiner.
    /// </summary>
    public const string DET = "det";

    /// <summary>
    /// Numerical modifier.
    /// </summary>
    public const string NUMMOD = "nummod";

    /// <summary>
    /// Possession modifier.
    /// </summary>
    public const string POSS = "poss";

    /// <summary>
    /// Case marking.
    /// </summary>
    public const string CASE = "case";

    /// <summary>
    /// Marker (subordinating conjunction).
    /// </summary>
    public const string MARK = "mark";

    /// <summary>
    /// Adverbial clause modifier.
    /// </summary>
    public const string ADVCL = "advcl";

    /// <summary>
    /// Clausal complement.
    /// </summary>
    public const string CCOMP = "ccomp";

    /// <summary>
    /// Appositional modifier.
    /// </summary>
    public const string APPOS = "appos";

    /// <summary>
    /// Negation modifier.
    /// </summary>
    public const string NEG = "neg";
}
