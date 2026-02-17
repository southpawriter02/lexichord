// -----------------------------------------------------------------------
// <copyright file="MetadataExtractionOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Defines configuration options for metadata extraction operations (v0.7.6b).
//   Controls the behavior of IMetadataExtractor including:
//   - Maximum counts for key terms, concepts, and tags
//   - Reading time calculation parameters
//   - Feature toggles for optional extraction components
//   - Existing tags for tag suggestion matching
//
//   All options have sensible defaults and are validated via Validate() before use.
//
//   Introduced in: v0.7.6b
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.MetadataExtraction;

/// <summary>
/// Configuration options for metadata extraction requests.
/// </summary>
/// <remarks>
/// <para>
/// Configures the behavior of <see cref="IMetadataExtractor"/> operations including
/// the maximum number of items to extract and reading time calculation parameters.
/// All options have default values suitable for typical document analysis.
/// </para>
/// <para>
/// Call <see cref="Validate"/> before using options to ensure all values are within
/// acceptable ranges. Invalid options will throw <see cref="ArgumentException"/>.
/// </para>
/// <para><b>Introduced in:</b> v0.7.6b</para>
/// </remarks>
public record MetadataExtractionOptions
{
    /// <summary>
    /// Maximum number of key terms to extract.
    /// </summary>
    /// <remarks>
    /// LOGIC: Range: 1-50. Default: 10.
    /// Key terms are ordered by importance, so lower limits return only the most
    /// relevant terms. Higher limits provide more comprehensive coverage.
    /// </remarks>
    public int MaxKeyTerms { get; init; } = 10;

    /// <summary>
    /// Maximum number of high-level concepts to identify.
    /// </summary>
    /// <remarks>
    /// LOGIC: Range: 1-20. Default: 5.
    /// Concepts are more abstract than key terms and suitable for topic modeling
    /// and document clustering.
    /// </remarks>
    public int MaxConcepts { get; init; } = 5;

    /// <summary>
    /// Maximum number of tags to suggest.
    /// </summary>
    /// <remarks>
    /// LOGIC: Range: 1-20. Default: 5.
    /// Tags are formatted as lowercase with hyphens (e.g., "machine-learning").
    /// When <see cref="ExistingTags"/> is provided, suggestions prefer matching
    /// existing tags over novel suggestions.
    /// </remarks>
    public int MaxTags { get; init; } = 5;

    /// <summary>
    /// Existing tags in the workspace for consistency.
    /// </summary>
    /// <remarks>
    /// LOGIC: When provided, the tag suggestion algorithm prefers:
    /// 1. Exact matches with existing tags
    /// 2. Fuzzy matches (Levenshtein similarity > 0.8)
    /// 3. Novel tags if remaining capacity
    /// This ensures consistent tagging across the workspace.
    /// </remarks>
    public IReadOnlyList<string>? ExistingTags { get; init; }

    /// <summary>
    /// Whether to extract named entities (people, organizations, products).
    /// </summary>
    /// <remarks>
    /// LOGIC: Default: true.
    /// Extraction of named entities may increase processing time but provides
    /// valuable metadata for document indexing and search.
    /// </remarks>
    public bool ExtractNamedEntities { get; init; } = true;

    /// <summary>
    /// Whether to infer the target audience from content analysis.
    /// </summary>
    /// <remarks>
    /// LOGIC: Default: true.
    /// Target audience inference considers vocabulary complexity, technical
    /// density, and writing style to determine the intended readership.
    /// </remarks>
    public bool InferAudience { get; init; } = true;

    /// <summary>
    /// Whether to calculate document complexity score.
    /// </summary>
    /// <remarks>
    /// LOGIC: Default: true.
    /// Complexity scoring (1-10) is based on vocabulary, sentence structure,
    /// document structure, and content type indicators.
    /// </remarks>
    public bool CalculateComplexity { get; init; } = true;

    /// <summary>
    /// Whether to detect and classify the document type.
    /// </summary>
    /// <remarks>
    /// LOGIC: Default: true.
    /// Document type classification identifies the format/purpose of the document
    /// (e.g., Tutorial, Report, Reference) based on structural patterns.
    /// </remarks>
    public bool DetectDocumentType { get; init; } = true;

    /// <summary>
    /// Whether to include definitions for key terms found in the document.
    /// </summary>
    /// <remarks>
    /// LOGIC: Default: true.
    /// When enabled, the extractor searches for explicit definitions of key
    /// terms within the document content and includes them in the results.
    /// </remarks>
    public bool IncludeDefinitions { get; init; } = true;

    /// <summary>
    /// Reading speed in words per minute for reading time calculation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Range: 100-400. Default: 200 (average adult reading speed).
    /// Lower values (100-150) are appropriate for technical or academic content.
    /// Higher values (250-400) suit casual or familiar content.
    /// </remarks>
    public int WordsPerMinute { get; init; } = 200;

    /// <summary>
    /// Domain context for improved term extraction.
    /// </summary>
    /// <remarks>
    /// LOGIC: Optional. Examples: "software engineering", "machine learning", "finance".
    /// When provided, the extractor uses domain context to better identify
    /// relevant terminology and assign appropriate importance scores.
    /// </remarks>
    public string? DomainContext { get; init; }

    /// <summary>
    /// Minimum importance score for including a key term in results.
    /// </summary>
    /// <remarks>
    /// LOGIC: Range: 0.0-1.0. Default: 0.3.
    /// Terms below this threshold are filtered from results. Higher thresholds
    /// produce fewer but more relevant terms.
    /// </remarks>
    public double MinimumTermImportance { get; init; } = 0.3;

    /// <summary>
    /// Maximum response tokens for LLM calls.
    /// </summary>
    /// <remarks>
    /// LOGIC: Default: 2048.
    /// Controls the maximum length of the LLM response. Increase for documents
    /// with many expected key terms or complex metadata.
    /// </remarks>
    public int MaxResponseTokens { get; init; } = 2048;

    /// <summary>
    /// Validates the options configuration.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when any option value is outside its valid range.
    /// </exception>
    /// <remarks>
    /// LOGIC: Call this method before using options to ensure all values are valid.
    /// Validation checks:
    /// - MaxKeyTerms: 1-50
    /// - MaxConcepts: 1-20
    /// - MaxTags: 1-20
    /// - WordsPerMinute: 100-400
    /// - MinimumTermImportance: 0.0-1.0
    /// </remarks>
    public void Validate()
    {
        if (MaxKeyTerms < 1 || MaxKeyTerms > 50)
        {
            throw new ArgumentException(
                $"MaxKeyTerms must be between 1 and 50, but was {MaxKeyTerms}.",
                nameof(MaxKeyTerms));
        }

        if (MaxConcepts < 1 || MaxConcepts > 20)
        {
            throw new ArgumentException(
                $"MaxConcepts must be between 1 and 20, but was {MaxConcepts}.",
                nameof(MaxConcepts));
        }

        if (MaxTags < 1 || MaxTags > 20)
        {
            throw new ArgumentException(
                $"MaxTags must be between 1 and 20, but was {MaxTags}.",
                nameof(MaxTags));
        }

        if (WordsPerMinute < 100 || WordsPerMinute > 400)
        {
            throw new ArgumentException(
                $"WordsPerMinute must be between 100 and 400, but was {WordsPerMinute}.",
                nameof(WordsPerMinute));
        }

        if (MinimumTermImportance < 0.0 || MinimumTermImportance > 1.0)
        {
            throw new ArgumentException(
                $"MinimumTermImportance must be between 0.0 and 1.0, but was {MinimumTermImportance}.",
                nameof(MinimumTermImportance));
        }
    }

    /// <summary>
    /// Creates options with default values.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns a new instance with all default values. Equivalent to
    /// <c>new MetadataExtractionOptions()</c> but more explicit.
    /// </remarks>
    public static MetadataExtractionOptions Default => new();

    /// <summary>
    /// Creates options optimized for quick extraction with minimal LLM usage.
    /// </summary>
    /// <remarks>
    /// LOGIC: Reduced counts and disabled optional features for faster processing.
    /// Suitable for batch processing or preview scenarios.
    /// </remarks>
    public static MetadataExtractionOptions Quick => new()
    {
        MaxKeyTerms = 5,
        MaxConcepts = 3,
        MaxTags = 3,
        ExtractNamedEntities = false,
        InferAudience = false,
        IncludeDefinitions = false,
        MaxResponseTokens = 1024
    };

    /// <summary>
    /// Creates options for comprehensive extraction with all features enabled.
    /// </summary>
    /// <remarks>
    /// LOGIC: Higher counts and all features enabled for thorough document analysis.
    /// Suitable for detailed document cataloging or knowledge base building.
    /// </remarks>
    public static MetadataExtractionOptions Comprehensive => new()
    {
        MaxKeyTerms = 20,
        MaxConcepts = 10,
        MaxTags = 10,
        ExtractNamedEntities = true,
        InferAudience = true,
        CalculateComplexity = true,
        DetectDocumentType = true,
        IncludeDefinitions = true,
        MinimumTermImportance = 0.2,
        MaxResponseTokens = 4096
    };
}
