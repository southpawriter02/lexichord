// =============================================================================
// File: DeduplicationOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for deduplication behavior.
// =============================================================================
// VERSION: v0.5.9d (Deduplication Service)
// LOGIC: Provides configurable thresholds and toggles for the deduplication
//   pipeline. Allows fine-tuning of merge behavior, contradiction detection,
//   and manual review queuing.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Options controlling deduplication behavior.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9d as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// <b>Threshold Guidelines:</b>
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Option</term>
///     <description>Recommendation</description>
///   </listheader>
///   <item>
///     <term><see cref="SimilarityThreshold"/></term>
///     <description>0.85 is balanced; lower (0.80) catches more duplicates but increases false positives.</description>
///   </item>
///   <item>
///     <term><see cref="AutoMergeThreshold"/></term>
///     <description>0.95 is conservative; higher values reduce risk of incorrect merges.</description>
///   </item>
/// </list>
/// <para>
/// <b>Performance Considerations:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Higher <see cref="MaxCandidates"/> increases accuracy but slows processing.</description></item>
///   <item><description>Disabling <see cref="RequireLlmConfirmation"/> speeds up auto-merge but reduces accuracy.</description></item>
/// </list>
/// </remarks>
public record DeduplicationOptions
{
    /// <summary>
    /// Default options with conservative thresholds.
    /// </summary>
    public static readonly DeduplicationOptions Default = new();

    /// <summary>
    /// Similarity threshold for detecting potential duplicates.
    /// </summary>
    /// <value>Default: 0.85. Range: 0.0-1.0.</value>
    /// <remarks>
    /// Chunks with similarity below this threshold are not considered duplicates.
    /// Lower values catch more potential duplicates but increase false positives.
    /// </remarks>
    public float SimilarityThreshold { get; init; } = 0.85f;

    /// <summary>
    /// Threshold above which auto-merge is allowed.
    /// </summary>
    /// <value>Default: 0.95. Range: 0.0-1.0.</value>
    /// <remarks>
    /// Only chunks with similarity at or above this threshold are eligible
    /// for automatic merging. Below this threshold, manual review is triggered
    /// (if <see cref="EnableManualReviewQueue"/> is true).
    /// </remarks>
    public float AutoMergeThreshold { get; init; } = 0.95f;

    /// <summary>
    /// Whether to require LLM confirmation for auto-merge.
    /// </summary>
    /// <value>Default: <c>true</c>.</value>
    /// <remarks>
    /// When enabled, even high-similarity matches are double-checked by the
    /// LLM classifier before auto-merging. This adds latency but improves
    /// accuracy for nuanced content.
    /// </remarks>
    public bool RequireLlmConfirmation { get; init; } = true;

    /// <summary>
    /// Whether to detect and flag contradictions.
    /// </summary>
    /// <value>Default: <c>true</c>.</value>
    /// <remarks>
    /// When enabled, chunks classified as <see cref="RelationshipType.Contradictory"/>
    /// are flagged for the contradiction resolution workflow (v0.5.9e).
    /// When disabled, contradictory chunks are treated as distinct content.
    /// </remarks>
    public bool EnableContradictionDetection { get; init; } = true;

    /// <summary>
    /// Whether to queue ambiguous cases for manual review.
    /// </summary>
    /// <value>Default: <c>true</c>.</value>
    /// <remarks>
    /// When enabled, chunks with low classification confidence (below 0.7)
    /// are queued in the pending_reviews table for admin decision.
    /// When disabled, ambiguous cases are treated as distinct content.
    /// </remarks>
    public bool EnableManualReviewQueue { get; init; } = true;

    /// <summary>
    /// Maximum number of similar chunks to consider.
    /// </summary>
    /// <value>Default: 5.</value>
    /// <remarks>
    /// Limits the number of candidates retrieved from the similarity detector.
    /// Higher values increase accuracy for chunks with many similar neighbors
    /// but increase processing time and memory usage.
    /// </remarks>
    public int MaxCandidates { get; init; } = 5;

    /// <summary>
    /// Project scope for deduplication (null = cross-project).
    /// </summary>
    /// <value>Default: <c>null</c> (cross-project deduplication).</value>
    /// <remarks>
    /// When set, only chunks within the specified project are considered
    /// for deduplication. When null, deduplication operates across all
    /// indexed content in the knowledge base.
    /// </remarks>
    public Guid? ProjectScope { get; init; } = null;
}
