// =============================================================================
// File: ClaimExtractionContext.cs
// Project: Lexichord.Abstractions
// Description: Context for claim extraction operations.
// =============================================================================
// LOGIC: Configures claim extraction behavior including confidence thresholds,
//   extraction method toggles, and filtering options.
//
// v0.5.6g: Claim Extractor (Knowledge Graph Claim Extraction Pipeline)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.ClaimExtraction;

/// <summary>
/// Context for claim extraction operations.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Provides configuration and context for claim extraction,
/// including document identification, confidence thresholds, and filtering.
/// </para>
/// <para>
/// <b>Configuration Options:</b>
/// <list type="bullet">
/// <item><description><b>MinConfidence:</b> Filter out low-confidence claims.</description></item>
/// <item><description><b>UsePatterns/UseDependencyExtraction:</b> Toggle extraction methods.</description></item>
/// <item><description><b>PredicateFilter:</b> Limit extraction to specific predicates.</description></item>
/// <item><description><b>CustomPatterns:</b> Add project-specific patterns.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6g as part of the Claim Extractor.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = new ClaimExtractionContext
/// {
///     DocumentId = documentId,
///     ProjectId = projectId,
///     MinConfidence = 0.7f,
///     DeduplicateClaims = true,
///     PredicateFilter = new[] { ClaimPredicate.ACCEPTS, ClaimPredicate.RETURNS }
/// };
/// </code>
/// </example>
public record ClaimExtractionContext
{
    /// <summary>
    /// Gets the document ID being processed.
    /// </summary>
    /// <value>The unique identifier of the source document.</value>
    public Guid DocumentId { get; init; }

    /// <summary>
    /// Gets the project ID for project-specific patterns.
    /// </summary>
    /// <value>The project context for custom pattern loading.</value>
    public Guid ProjectId { get; init; }

    /// <summary>
    /// Gets the minimum confidence threshold.
    /// </summary>
    /// <value>
    /// Claims with confidence below this threshold are filtered out.
    /// Default is 0.5.
    /// </value>
    public float MinConfidence { get; init; } = 0.5f;

    /// <summary>
    /// Gets whether to use pattern-based extraction.
    /// </summary>
    /// <value>Default is <c>true</c>.</value>
    public bool UsePatterns { get; init; } = true;

    /// <summary>
    /// Gets whether to use dependency-based extraction.
    /// </summary>
    /// <value>Default is <c>true</c>.</value>
    public bool UseDependencyExtraction { get; init; } = true;

    /// <summary>
    /// Gets whether to deduplicate claims.
    /// </summary>
    /// <value>
    /// If <c>true</c>, semantically equivalent claims are merged.
    /// Default is <c>true</c>.
    /// </value>
    public bool DeduplicateClaims { get; init; } = true;

    /// <summary>
    /// Gets the predicates to extract (null = all).
    /// </summary>
    /// <value>
    /// If specified, only claims with these predicates are extracted.
    /// </value>
    public IReadOnlyList<string>? PredicateFilter { get; init; }

    /// <summary>
    /// Gets the maximum claims per sentence.
    /// </summary>
    /// <value>
    /// Limits the number of claims extracted from a single sentence.
    /// Default is 5.
    /// </value>
    public int MaxClaimsPerSentence { get; init; } = 5;

    /// <summary>
    /// Gets custom patterns to use.
    /// </summary>
    /// <value>
    /// Additional patterns for project-specific extraction.
    /// Enterprise tier feature.
    /// </value>
    public IReadOnlyList<ExtractionPattern>? CustomPatterns { get; init; }

    /// <summary>
    /// Gets the source language code.
    /// </summary>
    /// <value>ISO 639-1 language code (e.g., "en", "de"). Default is "en".</value>
    public string Language { get; init; } = "en";

    /// <summary>
    /// Creates a default extraction context for a document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <returns>A default context with standard options.</returns>
    public static ClaimExtractionContext Default(Guid documentId) => new()
    {
        DocumentId = documentId
    };
}
