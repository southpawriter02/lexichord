// -----------------------------------------------------------------------
// <copyright file="IMetadataExtractor.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Specialized agent interface for document metadata extraction (v0.7.6b).
//   Extends IAgent with metadata extraction-specific operations:
//     - ExtractAsync: Extract metadata from a document at a file path
//     - ExtractFromContentAsync: Extract metadata from provided text content directly
//     - SuggestTagsAsync: Suggest tags based on content and existing workspace tags
//     - CalculateReadingTime: Calculate reading time (no LLM required)
//     - ExtractKeyTermsAsync: Extract only key terms (lighter-weight extraction)
//
//   DI registration pattern:
//     services.AddSingleton<IMetadataExtractor, MetadataExtractor>();
//     services.AddSingleton<IAgent>(sp => (IAgent)sp.GetRequiredService<IMetadataExtractor>());
//
//   This dual registration makes the agent discoverable both:
//     - As IMetadataExtractor for command handlers needing typed access
//     - As IAgent for the IAgentRegistry discovery mechanism
//
//   License Requirements:
//     The implementing MetadataExtractor requires WriterPro tier or higher.
//
//   Introduced in: v0.7.6b
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.MetadataExtraction;

/// <summary>
/// Specialized agent interface for document metadata extraction.
/// Extends base <see cref="IAgent"/> with metadata extraction-specific operations.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="IMetadataExtractor"/> defines the core metadata extraction operations
/// implemented by the MetadataExtractor (v0.7.6b). It extends <see cref="IAgent"/> so the
/// implementation can be registered both as a typed metadata extraction service and as a
/// discoverable agent via <see cref="IAgentRegistry"/>.
/// </para>
/// <para>
/// <b>Operations:</b>
/// <list type="bullet">
///   <item><description><see cref="ExtractAsync"/>: Extract metadata from a document at a file path</description></item>
///   <item><description><see cref="ExtractFromContentAsync"/>: Extract metadata from provided text content directly</description></item>
///   <item><description><see cref="SuggestTagsAsync"/>: Suggest tags based on content and existing workspace tags</description></item>
///   <item><description><see cref="CalculateReadingTime"/>: Calculate reading time (no LLM required)</description></item>
///   <item><description><see cref="ExtractKeyTermsAsync"/>: Extract only key terms (lighter-weight extraction)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Requirements:</b>
/// The implementing MetadataExtractor requires <see cref="Abstractions.Contracts.LicenseTier.WriterPro"/>
/// tier or higher. Unlicensed users receive a "Upgrade to WriterPro" message.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6b as part of the Summarizer Agent Metadata Extraction.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Extract metadata from file path
/// var extractor = serviceProvider.GetRequiredService&lt;IMetadataExtractor&gt;();
/// var options = new MetadataExtractionOptions { MaxKeyTerms = 15, MaxConcepts = 8 };
/// var metadata = await extractor.ExtractAsync("/path/to/document.md", options, ct);
///
/// if (metadata.Success)
/// {
///     Console.WriteLine($"Title: {metadata.SuggestedTitle}");
///     Console.WriteLine($"One-liner: {metadata.OneLiner}");
///     Console.WriteLine($"Complexity: {metadata.ComplexityScore}/10");
///     Console.WriteLine($"Reading time: ~{metadata.EstimatedReadingMinutes} min");
///     Console.WriteLine($"Key terms: {string.Join(", ", metadata.KeyTerms.Select(t => t.Term))}");
/// }
///
/// // Extract metadata from content directly
/// var contentMetadata = await extractor.ExtractFromContentAsync(selectedText, options, ct);
///
/// // Quick reading time calculation (no LLM)
/// int minutes = extractor.CalculateReadingTime(content);
/// </code>
/// </example>
/// <seealso cref="DocumentMetadata"/>
/// <seealso cref="KeyTerm"/>
/// <seealso cref="DocumentType"/>
/// <seealso cref="MetadataExtractionOptions"/>
/// <seealso cref="IAgent"/>
public interface IMetadataExtractor : IAgent
{
    /// <summary>
    /// Extracts comprehensive metadata from a document at the specified path.
    /// </summary>
    /// <param name="documentPath">Absolute path to the document to analyze.</param>
    /// <param name="options">Extraction configuration options. Null uses defaults.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the
    /// <see cref="DocumentMetadata"/> with the extracted metadata and metrics.
    /// </returns>
    /// <exception cref="FileNotFoundException">Document not found at <paramref name="documentPath"/>.</exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Reads the document content via <c>IFileService.LoadAsync</c> and
    /// delegates to <see cref="ExtractFromContentAsync"/>. The document path is used for
    /// context assembly and event publishing.
    /// </para>
    /// <para>
    /// <b>Error Handling:</b> Returns a failed <see cref="DocumentMetadata"/> for
    /// runtime errors (timeouts, LLM failures, cancellation) instead of throwing.
    /// </para>
    /// </remarks>
    Task<DocumentMetadata> ExtractAsync(
        string documentPath,
        MetadataExtractionOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Extracts comprehensive metadata from the provided content directly.
    /// Use for selections, excerpts, or content not stored in files.
    /// </summary>
    /// <param name="content">Text content to analyze.</param>
    /// <param name="options">Extraction configuration options. Null uses defaults.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the
    /// <see cref="DocumentMetadata"/> with the extracted metadata and metrics.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="content"/> is null or empty.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This is the primary metadata extraction entry point. The flow is:
    /// </para>
    /// <list type="number">
    ///   <item><description>Validate options via <see cref="MetadataExtractionOptions.Validate"/></description></item>
    ///   <item><description>Publish <c>MetadataExtractionStartedEvent</c></description></item>
    ///   <item><description>Calculate reading time via <see cref="CalculateReadingTime"/></description></item>
    ///   <item><description>Render extraction prompt via <c>IPromptRenderer</c></description></item>
    ///   <item><description>Invoke LLM via <c>IChatCompletionService.CompleteAsync</c></description></item>
    ///   <item><description>Parse JSON response and build <see cref="DocumentMetadata"/></description></item>
    ///   <item><description>Filter key terms by <see cref="MetadataExtractionOptions.MinimumTermImportance"/></description></item>
    ///   <item><description>Match suggested tags with <see cref="MetadataExtractionOptions.ExistingTags"/></description></item>
    ///   <item><description>Publish <c>MetadataExtractionCompletedEvent</c> or <c>MetadataExtractionFailedEvent</c></description></item>
    /// </list>
    /// <para>
    /// <b>Error Handling:</b> Returns a failed <see cref="DocumentMetadata"/> for
    /// runtime errors instead of throwing.
    /// </para>
    /// </remarks>
    Task<DocumentMetadata> ExtractFromContentAsync(
        string content,
        MetadataExtractionOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Suggests tags for a document based on content and existing workspace tags.
    /// Prefers tags that match existing taxonomy for consistency.
    /// </summary>
    /// <param name="content">Text content to analyze for tag suggestions.</param>
    /// <param name="existingWorkspaceTags">Tags already used in the workspace.</param>
    /// <param name="maxSuggestions">Maximum number of tag suggestions. Default: 5.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a list of suggested
    /// tags ordered by relevance. Tags are formatted as lowercase with hyphens.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="content"/> is null or empty.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Tag suggestion uses a three-tier matching strategy:
    /// </para>
    /// <list type="number">
    ///   <item><description>Exact matches with existing workspace tags (highest priority)</description></item>
    ///   <item><description>Fuzzy matches with Levenshtein similarity > 0.8</description></item>
    ///   <item><description>Novel tags if remaining capacity (lowercase-hyphenated format)</description></item>
    /// </list>
    /// <para>
    /// This approach ensures consistent tagging across the workspace while allowing
    /// new tags to be introduced when appropriate.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<string>> SuggestTagsAsync(
        string content,
        IReadOnlyList<string> existingWorkspaceTags,
        int maxSuggestions = 5,
        CancellationToken ct = default);

    /// <summary>
    /// Calculates reading time for content using word count and complexity heuristics.
    /// </summary>
    /// <param name="content">Text content to analyze.</param>
    /// <param name="wordsPerMinute">Base reading speed. Default: 200.</param>
    /// <returns>
    /// Estimated reading time in minutes. Minimum value is 1 for non-empty content.
    /// Returns 0 for null, empty, or whitespace-only content.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method does NOT use the LLM. It is a purely algorithmic
    /// calculation based on the reading time algorithm (spec §5.2):
    /// </para>
    /// <code>
    /// baseMinutes = wordCount / wordsPerMinute
    /// + codeBlocks * 0.5          // +30 sec per code block
    /// + tables * 0.5              // +30 sec per table
    /// + images * 0.2              // +12 sec per image
    /// * 1.10 if avgSentence > 25  // +10% for complex sentences
    /// * 1.20 if techDensity > 10% // +20% for technical content
    /// minimum = 1 minute
    /// </code>
    /// <para>
    /// This allows reading time to be calculated quickly without incurring LLM costs.
    /// </para>
    /// </remarks>
    int CalculateReadingTime(string content, int wordsPerMinute = 200);

    /// <summary>
    /// Extracts only key terms from content (lighter-weight than full extraction).
    /// </summary>
    /// <param name="content">Text content to analyze.</param>
    /// <param name="maxTerms">Maximum number of key terms to extract. Default: 10.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a list of
    /// <see cref="KeyTerm"/> records ordered by importance (descending).
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="content"/> is null or empty.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method provides a lighter-weight alternative to full
    /// <see cref="ExtractFromContentAsync"/> when only key terms are needed.
    /// </para>
    /// <para>
    /// Use cases:
    /// <list type="bullet">
    ///   <item><description>Autocomplete and search suggestions</description></item>
    ///   <item><description>Quick document previews</description></item>
    ///   <item><description>Tag recommendations</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The LLM is invoked with a simplified prompt focused only on key term extraction,
    /// reducing token usage and latency compared to full metadata extraction.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<KeyTerm>> ExtractKeyTermsAsync(
        string content,
        int maxTerms = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the default extraction options.
    /// </summary>
    /// <returns>
    /// A new <see cref="MetadataExtractionOptions"/> instance with default values.
    /// </returns>
    /// <remarks>
    /// <b>LOGIC:</b> Returns the same values as <see cref="MetadataExtractionOptions.Default"/>
    /// but provided as an instance method for interface consumers who may not have
    /// access to the static property.
    /// </remarks>
    MetadataExtractionOptions GetDefaultOptions();
}
