// =============================================================================
// File: Token.cs
// Project: Lexichord.Abstractions
// Description: Token (word) representation with linguistic annotations.
// =============================================================================
// LOGIC: Represents a single token in a sentence with linguistic features
//   extracted by the parser: text, lemma, POS tag, character offsets, and
//   flags for stop words and punctuation.
//
// v0.5.6f: Sentence Parser (Claim Extraction Pipeline)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Parsing;

/// <summary>
/// A token (word) in a parsed sentence with linguistic annotations.
/// </summary>
/// <remarks>
/// <para>
/// Tokens are the basic units of a parsed sentence. Each token includes:
/// <list type="bullet">
///   <item><b>Text:</b> The original word as it appears in the text.</item>
///   <item><b>Lemma:</b> The base/dictionary form (e.g., "accepts" → "accept").</item>
///   <item><b>POS:</b> Coarse-grained part-of-speech tag (e.g., "VERB", "NOUN").</item>
///   <item><b>Tag:</b> Fine-grained POS tag (e.g., "VBZ", "NN").</item>
///   <item><b>Offsets:</b> Character positions in the source text.</item>
/// </list>
/// </para>
/// <para>
/// <b>Example:</b>
/// <code>
/// var token = new Token
/// {
///     Text = "accepts",
///     Lemma = "accept",
///     POS = "VERB",
///     Tag = "VBZ",
///     Index = 2,
///     StartChar = 15,
///     EndChar = 22,
///     IsStopWord = false,
///     IsPunct = false
/// };
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6f as part of the Sentence Parser.
/// </para>
/// </remarks>
public record Token
{
    /// <summary>
    /// The token text as it appears in the document.
    /// </summary>
    /// <value>The original string, preserving case and formatting.</value>
    public required string Text { get; init; }

    /// <summary>
    /// The lemma (base form) of the token.
    /// </summary>
    /// <value>
    /// The dictionary form of the word (e.g., "running" → "run").
    /// May be null if lemmatization was not performed.
    /// </value>
    public string? Lemma { get; init; }

    /// <summary>
    /// Coarse-grained part-of-speech tag.
    /// </summary>
    /// <value>
    /// Universal Dependencies POS tag (e.g., "NOUN", "VERB", "ADJ", "DET").
    /// May be null if POS tagging was disabled.
    /// </value>
    public string? POS { get; init; }

    /// <summary>
    /// Fine-grained part-of-speech tag.
    /// </summary>
    /// <value>
    /// Language-specific tag (e.g., "VBZ" for 3rd person singular present verb).
    /// May be null if POS tagging was disabled.
    /// </value>
    public string? Tag { get; init; }

    /// <summary>
    /// Zero-based index of this token within the sentence.
    /// </summary>
    /// <value>Position in the token sequence (0 = first token).</value>
    public int Index { get; init; }

    /// <summary>
    /// Character offset where this token starts in the source sentence.
    /// </summary>
    /// <value>Zero-based index of the first character.</value>
    public int StartChar { get; init; }

    /// <summary>
    /// Character offset where this token ends in the source sentence.
    /// </summary>
    /// <value>Zero-based index past the last character (exclusive).</value>
    public int EndChar { get; init; }

    /// <summary>
    /// Whether this token is a stop word.
    /// </summary>
    /// <value>
    /// True for common words with little semantic value (e.g., "the", "a", "is").
    /// </value>
    public bool IsStopWord { get; init; }

    /// <summary>
    /// Whether this token is punctuation.
    /// </summary>
    /// <value>True for punctuation marks (e.g., ".", ",", "!").</value>
    public bool IsPunct { get; init; }
}
