// =============================================================================
// File: ScoringConfig.cs
// Project: Lexichord.Abstractions
// Description: Configuration for multi-signal entity relevance scoring.
// =============================================================================
// LOGIC: Defines configurable weights for the five relevance scoring signals
//   (semantic similarity, document mentions, type matching, recency, name
//   matching) plus preferred entity types and recency decay window. Weights
//   should sum to 1.0 for a normalized final score.
//
// v0.7.2f: Entity Relevance Scorer (CKVS Phase 4a)
// Dependencies: None (pure abstraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Configuration for multi-signal entity relevance scoring.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ScoringConfig"/> record defines the weights and parameters
/// for the five-signal relevance scoring algorithm used by
/// <see cref="IEntityRelevanceScorer"/>. Weights control the relative
/// importance of each signal in the final composite score.
/// </para>
/// <para>
/// <b>Default Weights (sum to 1.0):</b>
/// <list type="bullet">
///   <item>Semantic similarity: 0.35 (highest — captures meaning)</item>
///   <item>Document mentions: 0.25 (contextual relevance)</item>
///   <item>Type matching: 0.20 (entity type preference)</item>
///   <item>Recency: 0.10 (freshness)</item>
///   <item>Name matching: 0.10 (term overlap)</item>
/// </list>
/// </para>
/// <para>
/// <b>Immutability:</b> This is a <c>sealed record</c> with <c>init</c>-only
/// properties, supporting <c>with</c>-expression composition for overrides.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.2f as part of the Entity Relevance Scorer.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Default configuration
/// var config = new ScoringConfig();
///
/// // Custom weights emphasizing semantic similarity
/// var custom = new ScoringConfig
/// {
///     SemanticWeight = 0.50f,
///     MentionWeight = 0.20f,
///     TypeWeight = 0.15f,
///     RecencyWeight = 0.05f,
///     NameMatchWeight = 0.10f
/// };
///
/// // With preferred entity types
/// var typed = config with
/// {
///     PreferredTypes = new HashSet&lt;string&gt; { "Endpoint", "Parameter" }
/// };
/// </code>
/// </example>
public sealed record ScoringConfig
{
    /// <summary>
    /// Weight for semantic similarity signal (0.0–1.0).
    /// </summary>
    /// <value>Defaults to 0.35f.</value>
    /// <remarks>
    /// LOGIC: Semantic similarity uses embedding vectors (cosine similarity)
    /// to capture meaning-level relevance between the query and entity text.
    /// This is the highest-weighted signal because it captures conceptual
    /// relevance beyond simple term overlap. When embeddings are unavailable,
    /// this weight is redistributed proportionally to other signals.
    /// </remarks>
    public float SemanticWeight { get; init; } = 0.35f;

    /// <summary>
    /// Weight for document mention signal (0.0–1.0).
    /// </summary>
    /// <value>Defaults to 0.25f.</value>
    /// <remarks>
    /// LOGIC: Document mention scoring counts how many times an entity's
    /// name appears in the current document content. Entities frequently
    /// mentioned in the document are likely relevant to the user's context.
    /// Score saturates at 5 mentions (score = 1.0).
    /// </remarks>
    public float MentionWeight { get; init; } = 0.25f;

    /// <summary>
    /// Weight for type matching signal (0.0–1.0).
    /// </summary>
    /// <value>Defaults to 0.20f.</value>
    /// <remarks>
    /// LOGIC: Type matching boosts entities whose type matches the
    /// <see cref="PreferredTypes"/> set. Preferred types score 1.0,
    /// non-preferred score 0.3, and when no preferences are set all
    /// types score 0.5 (neutral).
    /// </remarks>
    public float TypeWeight { get; init; } = 0.20f;

    /// <summary>
    /// Weight for recency signal (0.0–1.0).
    /// </summary>
    /// <value>Defaults to 0.10f.</value>
    /// <remarks>
    /// LOGIC: Recency scoring applies a linear decay based on how many
    /// days ago the entity was last modified. Entities modified today
    /// score 1.0; entities older than <see cref="RecencyDecayDays"/>
    /// score 0.0. This favors recently updated knowledge.
    /// </remarks>
    public float RecencyWeight { get; init; } = 0.10f;

    /// <summary>
    /// Weight for name matching signal (0.0–1.0).
    /// </summary>
    /// <value>Defaults to 0.10f.</value>
    /// <remarks>
    /// LOGIC: Name matching checks whether query terms appear in the
    /// entity name. Direct substring matches score higher than term-level
    /// matches. This provides a fast, lightweight signal complementing
    /// the more expensive semantic similarity.
    /// </remarks>
    public float NameMatchWeight { get; init; } = 0.10f;

    /// <summary>
    /// Preferred entity types that receive a type-match boost.
    /// </summary>
    /// <value>
    /// A set of entity type names (e.g., "Endpoint", "Parameter"),
    /// or <c>null</c> to treat all types equally (neutral 0.5 score).
    /// </value>
    /// <remarks>
    /// LOGIC: When non-null, entities with types in this set receive
    /// a type score of 1.0, while non-matching types receive 0.3.
    /// This allows agent-specific type preferences (e.g., the Tuning
    /// agent prefers "Endpoint" and "Parameter" types).
    /// </remarks>
    public IReadOnlySet<string>? PreferredTypes { get; init; }

    /// <summary>
    /// Number of days over which recency decays from 1.0 to 0.0.
    /// </summary>
    /// <value>Defaults to 365 days (1 year).</value>
    /// <remarks>
    /// LOGIC: Controls the recency decay window. An entity modified
    /// exactly <see cref="RecencyDecayDays"/> days ago scores 0.0
    /// for recency. Entities older than this also score 0.0 (clamped).
    /// </remarks>
    public int RecencyDecayDays { get; init; } = 365;
}
