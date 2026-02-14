// =============================================================================
// File: RelevanceSignalScores.cs
// Project: Lexichord.Abstractions
// Description: Per-signal breakdown of entity relevance scores.
// =============================================================================
// LOGIC: Captures individual signal scores from the five-signal relevance
//   scoring algorithm. Each signal is independently scored in the range
//   [0.0, 1.0] before being combined via weighted sum into a final score.
//
// v0.7.2f: Entity Relevance Scorer (CKVS Phase 4a)
// Dependencies: None (pure abstraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Per-signal breakdown of entity relevance scores.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RelevanceSignalScores"/> record captures the individual
/// scores from each of the five relevance scoring signals. This provides
/// transparency into how the final composite score was computed, enabling
/// diagnostics and per-signal analysis.
/// </para>
/// <para>
/// <b>Signal Range:</b> All signal scores are in the range [0.0, 1.0]:
/// <list type="bullet">
///   <item><see cref="SemanticScore"/>: Cosine similarity between query and entity embeddings.</item>
///   <item><see cref="MentionScore"/>: Document mention frequency (saturates at 5 mentions).</item>
///   <item><see cref="TypeScore"/>: Entity type preference match (1.0 preferred, 0.3 non-preferred, 0.5 neutral).</item>
///   <item><see cref="RecencyScore"/>: Linear decay based on entity modification date.</item>
///   <item><see cref="NameMatchScore"/>: Query term presence in entity name.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.2f as part of the Entity Relevance Scorer.
/// </para>
/// </remarks>
public sealed record RelevanceSignalScores
{
    /// <summary>
    /// Semantic similarity score between query and entity embeddings.
    /// </summary>
    /// <value>Range [0.0, 1.0]. Defaults to 0.0f.</value>
    /// <remarks>
    /// LOGIC: Computed as cosine similarity between the query embedding
    /// vector and the entity text embedding vector. Returns 0.0 when
    /// the embedding service is unavailable or fails.
    /// </remarks>
    public float SemanticScore { get; init; }

    /// <summary>
    /// Document mention frequency score.
    /// </summary>
    /// <value>Range [0.0, 1.0]. Defaults to 0.0f.</value>
    /// <remarks>
    /// LOGIC: Counts occurrences of the entity name in the document
    /// content plus term-level matches. Normalized by dividing by 5
    /// and clamping to 1.0 (5+ mentions = maximum score).
    /// </remarks>
    public float MentionScore { get; init; }

    /// <summary>
    /// Entity type preference match score.
    /// </summary>
    /// <value>Range [0.0, 1.0]. Defaults to 0.0f.</value>
    /// <remarks>
    /// LOGIC: Scores 1.0 if the entity type matches the preferred types
    /// set, 0.3 if it does not match, and 0.5 (neutral) when no
    /// preferred types are configured.
    /// </remarks>
    public float TypeScore { get; init; }

    /// <summary>
    /// Recency score based on entity modification date.
    /// </summary>
    /// <value>Range [0.0, 1.0]. Defaults to 0.0f.</value>
    /// <remarks>
    /// LOGIC: Linear decay from 1.0 (modified today) to 0.0 (modified
    /// <see cref="ScoringConfig.RecencyDecayDays"/> or more days ago).
    /// Clamped to [0.0, 1.0].
    /// </remarks>
    public float RecencyScore { get; init; }

    /// <summary>
    /// Name matching score based on query term presence in entity name.
    /// </summary>
    /// <value>Range [0.0, 1.0]. Defaults to 0.0f.</value>
    /// <remarks>
    /// LOGIC: Counts query terms that appear in the entity name.
    /// Direct substring matches (entity name contains term) score 2 points,
    /// term-level matches score 1 point. Normalized by (queryTerms.Count * 2)
    /// and clamped to 1.0.
    /// </remarks>
    public float NameMatchScore { get; init; }
}
