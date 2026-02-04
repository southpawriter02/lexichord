// =============================================================================
// File: ClassificationOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for relationship classification.
// =============================================================================
// VERSION: v0.5.9b (Relationship Classification)
// LOGIC: Provides configurable thresholds, caching, and LLM enablement options.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Configuration options for relationship classification behavior.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9b as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// These options control the classification strategy, including thresholds for
/// rule-based vs LLM classification, caching behavior, and output verbosity.
/// </para>
/// </remarks>
public record ClassificationOptions
{
    /// <summary>
    /// Gets the default configuration options.
    /// </summary>
    public static ClassificationOptions Default { get; } = new();

    /// <summary>
    /// Gets or initializes the similarity threshold for rule-based classification.
    /// </summary>
    /// <value>
    /// Similarity scores at or above this threshold use rule-based classification.
    /// Below this threshold (but above minimum), LLM classification is used if enabled.
    /// Default is 0.95.
    /// </value>
    public float RuleBasedThreshold { get; init; } = 0.95f;

    /// <summary>
    /// Gets or initializes whether LLM classification is enabled for ambiguous cases.
    /// </summary>
    /// <value>
    /// When <c>true</c>, pairs with similarity below <see cref="RuleBasedThreshold"/>
    /// will be sent to the LLM for classification. Default is <c>true</c>.
    /// </value>
    public bool EnableLlmClassification { get; init; } = true;

    /// <summary>
    /// Gets or initializes whether classification caching is enabled.
    /// </summary>
    /// <value>
    /// When <c>true</c>, classification results are cached by chunk pair IDs.
    /// Default is <c>true</c>.
    /// </value>
    public bool EnableCaching { get; init; } = true;

    /// <summary>
    /// Gets or initializes the cache duration for classification results.
    /// </summary>
    /// <value>
    /// How long classification results are cached before expiration.
    /// Default is 1 hour.
    /// </value>
    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or initializes whether explanations are included in results.
    /// </summary>
    /// <value>
    /// When <c>true</c>, <see cref="RelationshipClassification.Explanation"/>
    /// will be populated with a human-readable rationale. Default is <c>true</c>.
    /// </value>
    public bool IncludeExplanation { get; init; } = true;

    /// <summary>
    /// Gets or initializes the minimum similarity threshold for classification.
    /// </summary>
    /// <value>
    /// Pairs with similarity below this value are immediately classified as
    /// <see cref="RelationshipType.Distinct"/>. Default is 0.80.
    /// </value>
    public float MinimumSimilarityThreshold { get; init; } = 0.80f;
}
