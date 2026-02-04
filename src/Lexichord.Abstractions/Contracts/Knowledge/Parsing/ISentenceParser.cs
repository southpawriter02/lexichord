// =============================================================================
// File: ISentenceParser.cs
// Project: Lexichord.Abstractions
// Description: Interface for sentence parsing services.
// =============================================================================
// LOGIC: Defines the contract for parsing text into linguistic structures.
//   Implementations provide sentence segmentation, tokenization, dependency
//   parsing, and semantic role labeling.
//
// v0.5.6f: Sentence Parser (Claim Extraction Pipeline)
// Dependencies: ParseResult, ParsedSentence, ParseOptions (v0.5.6f)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Parsing;

/// <summary>
/// Service for parsing text into linguistic structures.
/// </summary>
/// <remarks>
/// <para>
/// The sentence parser is a core component of the Claim Extraction pipeline.
/// It converts raw text into structured representations suitable for:
/// <list type="bullet">
///   <item>Identifying predicate-argument structures for claim extraction.</item>
///   <item>Extracting grammatical subjects and objects.</item>
///   <item>Recognizing named entities for entity linking.</item>
/// </list>
/// </para>
/// <para>
/// <b>Pipeline:</b>
/// <list type="number">
///   <item>Sentence segmentation — Split text into sentences.</item>
///   <item>Tokenization — Split sentences into words.</item>
///   <item>POS tagging — Assign part-of-speech tags.</item>
///   <item>Dependency parsing — Identify grammatical relations.</item>
///   <item>Semantic Role Labeling — Identify predicate-argument structures.</item>
///   <item>Named Entity Recognition — Identify entity mentions.</item>
/// </list>
/// </para>
/// <para>
/// <b>License:</b> WriterPro tier for basic parsing; Teams tier for full
/// functionality including SRL and multi-language support.
/// Feature code: KG-056f.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6f as part of the Claim Extraction pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ClaimExtractor
/// {
///     private readonly ISentenceParser _parser;
///
///     public async Task&lt;IEnumerable&lt;Claim&gt;&gt; ExtractAsync(string text)
///     {
///         var result = await _parser.ParseAsync(text, new ParseOptions
///         {
///             Language = "en",
///             IncludeSRL = true
///         });
///
///         foreach (var sentence in result.Sentences)
///         {
///             var verb = sentence.GetRootVerb();
///             var subject = sentence.GetSubject();
///             var dobj = sentence.GetDirectObject();
///
///             // Extract claim from subject-verb-object triple...
///         }
///     }
/// }
/// </code>
/// </example>
public interface ISentenceParser
{
    /// <summary>
    /// Parses text into structured sentences with linguistic annotations.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="options">Parsing options. Uses defaults if null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="ParseResult"/> containing parsed sentences and statistics.
    /// </returns>
    /// <remarks>
    /// LOGIC: Segments text into sentences, then parses each sentence
    /// according to the options. Long sentences exceeding
    /// <see cref="ParseOptions.MaxSentenceLength"/> are skipped.
    /// </remarks>
    Task<ParseResult> ParseAsync(
        string text,
        ParseOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Parses a single sentence.
    /// </summary>
    /// <param name="sentence">The sentence text to parse.</param>
    /// <param name="options">Parsing options. Uses defaults if null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="ParsedSentence"/> with linguistic annotations.</returns>
    /// <remarks>
    /// LOGIC: Parses a single sentence without segmentation. Use when the
    /// input is already segmented.
    /// </remarks>
    Task<ParsedSentence> ParseSentenceAsync(
        string sentence,
        ParseOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the languages supported by this parser.
    /// </summary>
    /// <value>
    /// A list of ISO 639-1 language codes (e.g., "en", "de", "fr", "es").
    /// </value>
    IReadOnlyList<string> SupportedLanguages { get; }
}
