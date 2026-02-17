// -----------------------------------------------------------------------
// <copyright file="DocumentMetadata.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Result record for metadata extraction operations (v0.7.6b).
//   Contains the extracted metadata along with document analysis metrics:
//   key terms, concepts, tags, reading time, complexity score, and usage.
//
//   Success vs. Failure:
//     - Success: All required fields populated, metrics are valid
//     - Failure: Created via Failed() factory — empty collections,
//       zeroed metrics, Usage is UsageMetrics.Zero
//
//   Introduced in: v0.7.6b
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.MetadataExtraction;

/// <summary>
/// Comprehensive metadata extracted from a document.
/// Provides structured information for indexing, categorization, and discovery.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="DocumentMetadata"/> is the primary output of
/// <see cref="IMetadataExtractor.ExtractAsync"/> and <see cref="IMetadataExtractor.ExtractFromContentAsync"/>.
/// It encapsulates extracted metadata, document metrics, and token usage for transparency.
/// </para>
/// <para>
/// <b>Success vs. Failure:</b>
/// Failed results are created via <see cref="Failed"/> and have <see cref="Success"/> set to
/// <c>false</c> with an <see cref="ErrorMessage"/>. Always check <see cref="Success"/> before
/// using the extracted metadata.
/// </para>
/// <para>
/// <b>Thread safety:</b> This is an immutable record and is inherently thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6b as part of the Summarizer Agent Metadata Extraction.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var metadata = await metadataExtractor.ExtractFromContentAsync(content, options, ct);
///
/// if (metadata.Success)
/// {
///     Console.WriteLine($"Title: {metadata.SuggestedTitle ?? "(none)"}");
///     Console.WriteLine($"One-liner: {metadata.OneLiner}");
///     Console.WriteLine($"Key terms: {string.Join(", ", metadata.KeyTerms.Select(t => t.Term))}");
///     Console.WriteLine($"Complexity: {metadata.ComplexityScore}/10");
///     Console.WriteLine($"Reading time: ~{metadata.EstimatedReadingMinutes} min");
/// }
/// else
/// {
///     Console.WriteLine($"Extraction failed: {metadata.ErrorMessage}");
/// }
/// </code>
/// </example>
/// <seealso cref="KeyTerm"/>
/// <seealso cref="DocumentType"/>
/// <seealso cref="MetadataExtractionOptions"/>
/// <seealso cref="IMetadataExtractor"/>
public record DocumentMetadata
{
    /// <summary>
    /// Gets the suggested title if the document lacks one or has a poor title.
    /// </summary>
    /// <value>
    /// A more descriptive or accurate title for the document.
    /// <c>null</c> if the existing title is adequate or document lacks content for title inference.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The LLM analyzes document content to suggest a title when:
    /// - The document has no title
    /// - The existing title is generic (e.g., "Untitled", "Document1")
    /// - The existing title doesn't reflect the content accurately
    /// </remarks>
    public string? SuggestedTitle { get; init; }

    /// <summary>
    /// Gets the one-line description of the document.
    /// </summary>
    /// <value>
    /// A concise description (max 150 characters) suitable for search result
    /// snippets or document previews. Empty string for failed results.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Captures the document's core purpose or main topic in a single
    /// sentence. Should be informative enough to help users decide if the document
    /// is relevant to their needs without reading the full content.
    /// </remarks>
    public required string OneLiner { get; init; }

    /// <summary>
    /// Gets the key terms extracted from the document, ordered by importance.
    /// </summary>
    /// <value>
    /// A list of <see cref="KeyTerm"/> records sorted by <see cref="KeyTerm.Importance"/>
    /// in descending order. Empty list for failed results.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Key terms are words or phrases central to understanding the document.
    /// Terms with importance below <see cref="MetadataExtractionOptions.MinimumTermImportance"/>
    /// are filtered from results. The list is limited by <see cref="MetadataExtractionOptions.MaxKeyTerms"/>.
    /// </remarks>
    public required IReadOnlyList<KeyTerm> KeyTerms { get; init; }

    /// <summary>
    /// Gets the high-level concepts identified in the document.
    /// </summary>
    /// <value>
    /// A list of abstract concepts more general than key terms. Suitable for
    /// topic modeling and document clustering. Empty list for failed results.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Concepts represent broader themes or categories that encompass
    /// multiple key terms. Examples: "distributed systems", "machine learning",
    /// "project management". Limited by <see cref="MetadataExtractionOptions.MaxConcepts"/>.
    /// </remarks>
    public required IReadOnlyList<string> Concepts { get; init; }

    /// <summary>
    /// Gets the suggested tags for categorization.
    /// </summary>
    /// <value>
    /// A list of tags formatted as lowercase with hyphens (e.g., "api-design").
    /// Prefers matching <see cref="MetadataExtractionOptions.ExistingTags"/> when provided.
    /// Empty list for failed results.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Tags are selected using the following priority:
    /// <list type="number">
    /// <item><description>Exact matches with existing workspace tags</description></item>
    /// <item><description>Fuzzy matches (Levenshtein similarity > 0.8)</description></item>
    /// <item><description>Novel tags if remaining capacity</description></item>
    /// </list>
    /// Limited by <see cref="MetadataExtractionOptions.MaxTags"/>.
    /// </remarks>
    public required IReadOnlyList<string> SuggestedTags { get; init; }

    /// <summary>
    /// Gets the inferred primary category for the document.
    /// </summary>
    /// <value>
    /// A high-level categorization such as "Tutorial", "API Reference", "Architecture",
    /// "Report", or "Meeting Notes". <c>null</c> if category cannot be determined.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Primary category helps organize documents in a workspace
    /// and enables category-based filtering. Derived from document structure,
    /// writing style, and content patterns.
    /// </remarks>
    public string? PrimaryCategory { get; init; }

    /// <summary>
    /// Gets the inferred target audience for the document.
    /// </summary>
    /// <value>
    /// The intended readership such as "software developers", "data scientists",
    /// "executives", or "general audience". <c>null</c> if audience cannot be inferred
    /// or if <see cref="MetadataExtractionOptions.InferAudience"/> is false.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Target audience inference considers:
    /// <list type="bullet">
    /// <item><description>Vocabulary complexity and technical density</description></item>
    /// <item><description>Writing style (formal vs. informal)</description></item>
    /// <item><description>Assumed background knowledge</description></item>
    /// <item><description>Document type and purpose</description></item>
    /// </list>
    /// </remarks>
    public string? TargetAudience { get; init; }

    /// <summary>
    /// Gets the estimated reading time in minutes.
    /// </summary>
    /// <value>
    /// Reading time based on word count with complexity adjustments.
    /// Minimum value is 1 minute for non-empty documents. 0 for failed results.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated using the reading time algorithm (spec §5.2):
    /// <code>
    /// baseMinutes = wordCount / wordsPerMinute
    /// + codeBlocks * 0.5          // +30 sec per code block
    /// + tables * 0.5              // +30 sec per table
    /// + images * 0.2              // +12 sec per image
    /// * 1.10 if avgSentence > 25  // +10% for complex sentences
    /// * 1.20 if techDensity > 10% // +20% for technical content
    /// minimum = 1 minute
    /// </code>
    /// </remarks>
    public int EstimatedReadingMinutes { get; init; }

    /// <summary>
    /// Gets the document complexity score.
    /// </summary>
    /// <value>
    /// A score from 1 (elementary) to 10 (expert-level) indicating the document's
    /// difficulty level. 0 for failed results.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated using the complexity scoring algorithm (spec §5.3):
    /// <code>
    /// base = 5
    /// +1 if technicalDensity > 15%
    /// -1 if technicalDensity &lt; 5%
    /// +1 if avgWordsPerSentence > 25
    /// -1 if avgWordsPerSentence &lt; 12
    /// +0.5 if hasNestedHeadings &amp;&amp; hasTables
    /// -0.5 if simpleStructure
    /// clamp(1, 10)
    /// </code>
    /// When <see cref="MetadataExtractionOptions.CalculateComplexity"/> is false,
    /// returns 5 (neutral).
    /// </remarks>
    public int ComplexityScore { get; init; }

    /// <summary>
    /// Gets the detected document type based on structure and content.
    /// </summary>
    /// <value>
    /// A <see cref="MetadataExtraction.DocumentType"/> classification. Returns
    /// <see cref="MetadataExtraction.DocumentType.Unknown"/> if type cannot be determined
    /// or if <see cref="MetadataExtractionOptions.DetectDocumentType"/> is false.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Document type detection analyzes:
    /// <list type="bullet">
    /// <item><description>Document structure (headings, lists, code blocks)</description></item>
    /// <item><description>Writing style (formal, informal, technical)</description></item>
    /// <item><description>Content patterns (instructions, narrative, reference)</description></item>
    /// <item><description>Domain indicators (citations, legal clauses)</description></item>
    /// </list>
    /// </remarks>
    public DocumentType DocumentType { get; init; }

    /// <summary>
    /// Gets the named entities found in the document.
    /// </summary>
    /// <value>
    /// A list of people, organizations, products, and other named entities.
    /// <c>null</c> if <see cref="MetadataExtractionOptions.ExtractNamedEntities"/> is false.
    /// Empty list for failed results when entities were requested.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Named entity extraction identifies proper nouns that represent
    /// specific real-world entities. Useful for document linking and relationship discovery.
    /// </remarks>
    public IReadOnlyList<string>? NamedEntities { get; init; }

    /// <summary>
    /// Gets the primary language of the document.
    /// </summary>
    /// <value>
    /// ISO 639-1 two-letter language code (e.g., "en", "es", "fr").
    /// Defaults to "en" (English) when language cannot be determined.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Language detection is performed by the LLM during extraction.
    /// Used for language-specific processing and content recommendations.
    /// </remarks>
    public string Language { get; init; } = "en";

    /// <summary>
    /// Gets the word count of the document.
    /// </summary>
    /// <value>
    /// Number of words in the source text, counted by splitting on whitespace.
    /// 0 for failed results or empty documents.
    /// </value>
    public int WordCount { get; init; }

    /// <summary>
    /// Gets the character count of the document.
    /// </summary>
    /// <value>
    /// Number of characters in the source text including whitespace.
    /// 0 for failed results or empty documents.
    /// </value>
    public int CharacterCount { get; init; }

    /// <summary>
    /// Gets the timestamp when metadata was extracted.
    /// </summary>
    /// <value>
    /// UTC timestamp of when the extraction completed.
    /// </value>
    public DateTimeOffset ExtractedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the token usage for this extraction operation.
    /// </summary>
    /// <value>
    /// Total prompt and completion tokens consumed, along with estimated cost.
    /// <see cref="UsageMetrics.Zero"/> for failed results.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Token usage is tracked for transparency and cost monitoring.
    /// Displayed in the UI for users to understand resource consumption.
    /// </remarks>
    public required UsageMetrics Usage { get; init; }

    /// <summary>
    /// Gets the model used for extraction.
    /// </summary>
    /// <value>
    /// The LLM model identifier. Example: "gpt-4o".
    /// <c>null</c> for failed results or when model info is unavailable.
    /// </value>
    public string? Model { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    /// <value>
    /// <c>true</c> if extraction completed without errors;
    /// otherwise, <c>false</c>. Check <see cref="ErrorMessage"/> for failure details.
    /// </value>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    /// <value>
    /// A user-facing error message describing what went wrong.
    /// <c>null</c> if <see cref="Success"/> is <c>true</c>.
    /// </value>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">A user-facing error message describing the failure.</param>
    /// <param name="wordCount">The word count of the original document, if known.</param>
    /// <returns>
    /// A <see cref="DocumentMetadata"/> with <see cref="Success"/> set to <c>false</c>
    /// and zeroed metrics.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="errorMessage"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating failed results with consistent structure.
    /// Uses <see cref="UsageMetrics.Zero"/> for token usage and empty collections for lists.
    /// Called from catch blocks in the MetadataExtractor's 3-catch error handling pattern.
    /// </remarks>
    public static DocumentMetadata Failed(string errorMessage, int wordCount = 0)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);

        return new DocumentMetadata
        {
            SuggestedTitle = null,
            OneLiner = string.Empty,
            KeyTerms = Array.Empty<KeyTerm>(),
            Concepts = Array.Empty<string>(),
            SuggestedTags = Array.Empty<string>(),
            PrimaryCategory = null,
            TargetAudience = null,
            EstimatedReadingMinutes = 0,
            ComplexityScore = 0,
            DocumentType = DocumentType.Unknown,
            NamedEntities = null,
            Language = "en",
            WordCount = wordCount,
            CharacterCount = 0,
            ExtractedAt = DateTimeOffset.UtcNow,
            Usage = UsageMetrics.Zero,
            Model = null,
            Success = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Creates a minimal successful result for testing purposes.
    /// </summary>
    /// <param name="oneLiner">The one-line description.</param>
    /// <param name="wordCount">The word count of the document.</param>
    /// <returns>A minimal <see cref="DocumentMetadata"/> with default values.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating test instances with minimal required data.
    /// Not intended for production use; use the full constructor for complete metadata.
    /// </remarks>
    internal static DocumentMetadata CreateMinimal(string oneLiner, int wordCount = 100)
    {
        return new DocumentMetadata
        {
            OneLiner = oneLiner,
            KeyTerms = Array.Empty<KeyTerm>(),
            Concepts = Array.Empty<string>(),
            SuggestedTags = Array.Empty<string>(),
            WordCount = wordCount,
            CharacterCount = wordCount * 5,
            EstimatedReadingMinutes = Math.Max(1, wordCount / 200),
            ComplexityScore = 5,
            DocumentType = DocumentType.Unknown,
            Usage = UsageMetrics.Zero
        };
    }
}
