// =============================================================================
// File: SemanticArgument.cs
// Project: Lexichord.Abstractions
// Description: A semantic argument in a semantic frame.
// =============================================================================
// LOGIC: Represents an argument in a predicate-argument structure identified
//   by Semantic Role Labeling. Each argument has a role (ARG0, ARG1, etc.)
//   and consists of one or more tokens.
//
// v0.5.6f: Sentence Parser (Claim Extraction Pipeline)
// Dependencies: Token, SemanticRole (v0.5.6f)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Parsing;

/// <summary>
/// A semantic argument in a semantic frame.
/// </summary>
/// <remarks>
/// <para>
/// Semantic arguments are the participants in an event or state described
/// by a predicate. Each argument has a semantic role that describes its
/// relationship to the predicate.
/// </para>
/// <para>
/// <b>Example:</b> In "The endpoint accepts parameters":
/// <list type="bullet">
///   <item>Predicate: "accepts"</item>
///   <item>ARG0 (Agent): "The endpoint"</item>
///   <item>ARG1 (Patient): "parameters"</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6f as part of the Sentence Parser.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var argument = new SemanticArgument
/// {
///     Role = SemanticRole.ARG0,
///     Tokens = new[] { theToken, endpointToken },
///     Head = endpointToken
/// };
///
/// Console.WriteLine(argument.Text); // "The endpoint"
/// </code>
/// </example>
public record SemanticArgument
{
    /// <summary>
    /// The semantic role of this argument.
    /// </summary>
    /// <value>A <see cref="SemanticRole"/> value identifying the argument type.</value>
    public required SemanticRole Role { get; init; }

    /// <summary>
    /// The tokens comprising this argument.
    /// </summary>
    /// <value>One or more tokens that make up the argument span.</value>
    public required IReadOnlyList<Token> Tokens { get; init; }

    /// <summary>
    /// The head token of the argument.
    /// </summary>
    /// <value>
    /// The syntactic head of the argument phrase. May be null if not determined.
    /// </value>
    /// <remarks>
    /// LOGIC: The head is typically the main noun in a noun phrase argument.
    /// </remarks>
    public Token? Head { get; init; }

    /// <summary>
    /// Gets the text of this argument.
    /// </summary>
    /// <value>The concatenated text of all tokens, space-separated.</value>
    public string Text => string.Join(" ", Tokens.Select(t => t.Text));

    /// <summary>
    /// Gets the start character offset of this argument.
    /// </summary>
    /// <value>The minimum start offset among all tokens.</value>
    public int StartOffset => Tokens.Count > 0 ? Tokens.Min(t => t.StartChar) : 0;

    /// <summary>
    /// Gets the end character offset of this argument.
    /// </summary>
    /// <value>The maximum end offset among all tokens.</value>
    public int EndOffset => Tokens.Count > 0 ? Tokens.Max(t => t.EndChar) : 0;
}
