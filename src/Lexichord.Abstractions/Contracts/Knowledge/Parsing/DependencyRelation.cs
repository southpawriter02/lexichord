// =============================================================================
// File: DependencyRelation.cs
// Project: Lexichord.Abstractions
// Description: A dependency relation between two tokens.
// =============================================================================
// LOGIC: Represents a grammatical dependency between a head (governor) token
//   and a dependent token, with a relation type identifying the grammatical
//   function (e.g., nsubj for nominal subject, dobj for direct object).
//
// v0.5.6f: Sentence Parser (Claim Extraction Pipeline)
// Dependencies: Token (v0.5.6f)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Parsing;

/// <summary>
/// A dependency relation between two tokens in a sentence.
/// </summary>
/// <remarks>
/// <para>
/// Dependency parsing identifies grammatical relationships between words.
/// Each relation connects a head (governor) word to a dependent word with
/// a labeled relation type.
/// </para>
/// <para>
/// <b>Example:</b> In "The endpoint accepts parameters":
/// <list type="bullet">
///   <item>"accepts" (head) → "endpoint" (dependent), relation = "nsubj"</item>
///   <item>"accepts" (head) → "parameters" (dependent), relation = "dobj"</item>
///   <item>"endpoint" (head) → "The" (dependent), relation = "det"</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6f as part of the Sentence Parser.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var relation = new DependencyRelation
/// {
///     Head = verbToken,       // "accepts"
///     Dependent = nounToken,  // "endpoint"
///     Relation = "nsubj"      // nominal subject
/// };
/// </code>
/// </example>
public record DependencyRelation
{
    /// <summary>
    /// The head (governor) token in the dependency.
    /// </summary>
    /// <value>The token that governs the dependent.</value>
    /// <remarks>
    /// LOGIC: In "accepts parameters", "accepts" is the head and
    /// "parameters" is the dependent for the "dobj" relation.
    /// </remarks>
    public required Token Head { get; init; }

    /// <summary>
    /// The dependent token in the dependency.
    /// </summary>
    /// <value>The token that depends on the head.</value>
    public required Token Dependent { get; init; }

    /// <summary>
    /// The dependency relation type.
    /// </summary>
    /// <value>
    /// A label identifying the grammatical function (e.g., "nsubj", "dobj", "prep").
    /// See <see cref="DependencyRelations"/> for common relation types.
    /// </value>
    public required string Relation { get; init; }
}
