// =============================================================================
// File: NamedEntity.cs
// Project: Lexichord.Abstractions
// Description: Named entity recognized in parsed text.
// =============================================================================
// LOGIC: Represents a named entity (person, organization, location, etc.)
//   identified by Named Entity Recognition (NER) during parsing.
//
// v0.5.6f: Sentence Parser (Claim Extraction Pipeline)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Parsing;

/// <summary>
/// A named entity recognized in parsed text.
/// </summary>
/// <remarks>
/// <para>
/// Named Entity Recognition (NER) identifies spans of text that refer to
/// real-world entities such as:
/// <list type="bullet">
///   <item><b>PERSON:</b> Names of people.</item>
///   <item><b>ORG:</b> Organizations, companies.</item>
///   <item><b>GPE:</b> Geopolitical entities (countries, cities).</item>
///   <item><b>DATE:</b> Dates and time expressions.</item>
///   <item><b>PRODUCT:</b> Products and software.</item>
/// </list>
/// </para>
/// <para>
/// <b>Example:</b>
/// <code>
/// var entity = new NamedEntity
/// {
///     Text = "Microsoft",
///     Label = "ORG",
///     StartOffset = 0,
///     EndOffset = 9
/// };
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6f as part of the Sentence Parser.
/// </para>
/// </remarks>
public record NamedEntity
{
    /// <summary>
    /// The entity text as it appears in the document.
    /// </summary>
    /// <value>The span of text identified as an entity.</value>
    public required string Text { get; init; }

    /// <summary>
    /// The entity type label.
    /// </summary>
    /// <value>
    /// NER label such as "PERSON", "ORG", "GPE", "DATE", "PRODUCT".
    /// </value>
    public required string Label { get; init; }

    /// <summary>
    /// Character offset where this entity starts in the source sentence.
    /// </summary>
    /// <value>Zero-based index of the first character.</value>
    public int StartOffset { get; init; }

    /// <summary>
    /// Character offset where this entity ends in the source sentence.
    /// </summary>
    /// <value>Zero-based index past the last character (exclusive).</value>
    public int EndOffset { get; init; }
}
