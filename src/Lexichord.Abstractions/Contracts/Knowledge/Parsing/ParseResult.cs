// =============================================================================
// File: ParseResult.cs
// Project: Lexichord.Abstractions
// Description: Result container for sentence parsing operations.
// =============================================================================
// LOGIC: Contains the parsed sentences along with timing and statistics from
//   a parsing operation. Returned by ISentenceParser.ParseAsync.
//
// v0.5.6f: Sentence Parser (Claim Extraction Pipeline)
// Dependencies: ParsedSentence (v0.5.6f)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Parsing;

/// <summary>
/// Result container for a sentence parsing operation.
/// </summary>
/// <remarks>
/// <para>
/// Contains all parsed sentences from a text along with timing information
/// and aggregate statistics. Returned by <see cref="ISentenceParser.ParseAsync"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6f as part of the Sentence Parser.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await parser.ParseAsync(text);
///
/// Console.WriteLine($"Parsed {result.Stats.TotalSentences} sentences");
/// Console.WriteLine($"Total tokens: {result.Stats.TotalTokens}");
/// Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds}ms");
///
/// foreach (var sentence in result.Sentences)
/// {
///     var verb = sentence.GetRootVerb();
///     Console.WriteLine($"Root verb: {verb?.Text}");
/// }
/// </code>
/// </example>
public record ParseResult
{
    /// <summary>
    /// The parsed sentences.
    /// </summary>
    /// <value>A list of <see cref="ParsedSentence"/> objects.</value>
    public required IReadOnlyList<ParsedSentence> Sentences { get; init; }

    /// <summary>
    /// Time taken to parse the text.
    /// </summary>
    /// <value>The duration of the parsing operation.</value>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Aggregate statistics for the parse operation.
    /// </summary>
    /// <value>Statistics including counts and averages.</value>
    public ParseStats Stats { get; init; } = new();
}

/// <summary>
/// Aggregate statistics for a parsing operation.
/// </summary>
/// <remarks>
/// <para>
/// Provides summary metrics for a completed parse operation including
/// sentence count, token count, and averages.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6f as part of the Sentence Parser.
/// </para>
/// </remarks>
public record ParseStats
{
    /// <summary>
    /// Total number of sentences parsed.
    /// </summary>
    /// <value>Count of sentences in the result.</value>
    public int TotalSentences { get; init; }

    /// <summary>
    /// Total number of tokens across all sentences.
    /// </summary>
    /// <value>Sum of token counts.</value>
    public int TotalTokens { get; init; }

    /// <summary>
    /// Average number of tokens per sentence.
    /// </summary>
    /// <value>Mean token count. Zero if no sentences.</value>
    public float AverageTokensPerSentence { get; init; }

    /// <summary>
    /// Number of sentences skipped due to length limits.
    /// </summary>
    /// <value>Count of sentences exceeding MaxSentenceLength.</value>
    public int SkippedSentences { get; init; }

    /// <summary>
    /// Number of semantic frames extracted.
    /// </summary>
    /// <value>Total SRL frames across all sentences.</value>
    public int TotalSemanticFrames { get; init; }

    /// <summary>
    /// Number of named entities recognized.
    /// </summary>
    /// <value>Total NER entities across all sentences.</value>
    public int TotalNamedEntities { get; init; }
}
