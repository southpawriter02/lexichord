// =============================================================================
// File: ParseOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for sentence parsing.
// =============================================================================
// LOGIC: Provides configuration for the sentence parser including language
//   selection, feature toggles (dependencies, SRL, POS, entities), length
//   limits, and caching behavior.
//
// v0.5.6f: Sentence Parser (Claim Extraction Pipeline)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Parsing;

/// <summary>
/// Configuration options for sentence parsing.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of <see cref="ISentenceParser"/>:
/// <list type="bullet">
///   <item><b>Language:</b> Which language model to use.</item>
///   <item><b>Feature Toggles:</b> Which annotations to include.</item>
///   <item><b>Limits:</b> Maximum sentence length.</item>
///   <item><b>Caching:</b> Whether to cache parse results.</item>
/// </list>
/// </para>
/// <para>
/// <b>Performance Tip:</b> Disable unused features to improve parsing speed.
/// For claim extraction, <see cref="IncludeDependencies"/> and
/// <see cref="IncludeSRL"/> are the most important.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6f as part of the Sentence Parser.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new ParseOptions
/// {
///     Language = "en",
///     IncludeDependencies = true,
///     IncludeSRL = true,
///     IncludePOS = true,
///     IncludeEntities = false, // Skip NER for speed
///     MaxSentenceLength = 300,
///     UseCache = true
/// };
/// </code>
/// </example>
public record ParseOptions
{
    /// <summary>
    /// Language code for parsing.
    /// </summary>
    /// <value>
    /// ISO 639-1 language code (e.g., "en", "de", "fr", "es").
    /// Defaults to "en" (English).
    /// </value>
    /// <remarks>
    /// LOGIC: Determines which language model is loaded. Supported languages
    /// depend on the parser implementation.
    /// </remarks>
    public string Language { get; init; } = "en";

    /// <summary>
    /// Whether to perform dependency parsing.
    /// </summary>
    /// <value>
    /// True to include dependency relations and tree. Defaults to true.
    /// </value>
    /// <remarks>
    /// LOGIC: Required for <see cref="ParsedSentence.GetRootVerb"/>,
    /// <see cref="ParsedSentence.GetSubject"/>, etc.
    /// </remarks>
    public bool IncludeDependencies { get; init; } = true;

    /// <summary>
    /// Whether to perform Semantic Role Labeling.
    /// </summary>
    /// <value>
    /// True to include semantic frames. Defaults to true.
    /// </value>
    /// <remarks>
    /// LOGIC: Required for claim extraction to identify predicate-argument
    /// structures.
    /// </remarks>
    public bool IncludeSRL { get; init; } = true;

    /// <summary>
    /// Whether to include part-of-speech tags.
    /// </summary>
    /// <value>
    /// True to include POS and fine-grained tags. Defaults to true.
    /// </value>
    public bool IncludePOS { get; init; } = true;

    /// <summary>
    /// Whether to include named entity recognition.
    /// </summary>
    /// <value>
    /// True to include named entities. Defaults to true.
    /// </value>
    public bool IncludeEntities { get; init; } = true;

    /// <summary>
    /// Maximum sentence length to process.
    /// </summary>
    /// <value>
    /// Maximum character count. Sentences exceeding this are skipped.
    /// Defaults to 500 characters.
    /// </value>
    /// <remarks>
    /// LOGIC: Very long sentences can cause performance issues and are often
    /// difficult to parse accurately. They are skipped and not included in
    /// the results.
    /// </remarks>
    public int MaxSentenceLength { get; init; } = 500;

    /// <summary>
    /// Whether to cache parse results.
    /// </summary>
    /// <value>
    /// True to enable caching. Defaults to true.
    /// </value>
    /// <remarks>
    /// LOGIC: Caching improves performance when the same text is parsed
    /// multiple times. Cache key is based on text hash.
    /// </remarks>
    public bool UseCache { get; init; } = true;

    /// <summary>
    /// Default parse options.
    /// </summary>
    public static ParseOptions Default { get; } = new();

    /// <summary>
    /// Minimal parse options (tokens only, no dependencies or SRL).
    /// </summary>
    public static ParseOptions Minimal { get; } = new()
    {
        IncludeDependencies = false,
        IncludeSRL = false,
        IncludeEntities = false
    };

    /// <summary>
    /// Full parse options with all features enabled.
    /// </summary>
    public static ParseOptions Full { get; } = new()
    {
        IncludeDependencies = true,
        IncludeSRL = true,
        IncludePOS = true,
        IncludeEntities = true
    };
}
