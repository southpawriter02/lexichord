// =============================================================================
// File: ScoringRequest.cs
// Project: Lexichord.Abstractions
// Description: Request record for entity relevance scoring operations.
// =============================================================================
// LOGIC: Encapsulates the input parameters for IEntityRelevanceScorer.
//   Contains the search query, optional document content for mention
//   scoring, and optional preferred entity types for type matching.
//
// v0.7.2f: Entity Relevance Scorer (CKVS Phase 4a)
// Dependencies: None (pure abstraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Request record for entity relevance scoring operations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ScoringRequest"/> record encapsulates the input parameters
/// for <see cref="IEntityRelevanceScorer"/> operations. It provides the query
/// text (required), optional document content for mention-based scoring, and
/// optional preferred entity types for type-match boosting.
/// </para>
/// <para>
/// <b>Design Note:</b> This is a purpose-built record rather than reusing
/// <c>ContextGatheringRequest</c> (strategy-layer, carries agent-specific data)
/// or the LLM-layer <c>ContextRequest</c>. A focused record with just the
/// scoring-relevant fields keeps the scorer decoupled from strategy concerns.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.2f as part of the Entity Relevance Scorer.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var request = new ScoringRequest
/// {
///     Query = "authentication endpoint",
///     DocumentContent = "The authentication endpoint handles OAuth2 flows...",
///     PreferredEntityTypes = new HashSet&lt;string&gt; { "Endpoint", "Parameter" }
/// };
/// </code>
/// </example>
public sealed record ScoringRequest
{
    /// <summary>
    /// The search query text to score entities against.
    /// </summary>
    /// <value>A non-null, non-empty query string.</value>
    /// <remarks>
    /// LOGIC: Used as the basis for semantic embedding (cosine similarity)
    /// and term extraction (name matching). The query is typically derived
    /// from the user's selection or document context.
    /// </remarks>
    public required string Query { get; init; }

    /// <summary>
    /// Optional document content for mention-based scoring.
    /// </summary>
    /// <value>
    /// The full text content of the current document, or <c>null</c> if
    /// document content is not available.
    /// </value>
    /// <remarks>
    /// LOGIC: When provided, the mention scoring signal counts occurrences
    /// of entity names in this content. When <c>null</c>, the mention
    /// signal returns 0.0 for all entities.
    /// </remarks>
    public string? DocumentContent { get; init; }

    /// <summary>
    /// Optional preferred entity types for type-match boosting.
    /// </summary>
    /// <value>
    /// A set of entity type names (e.g., "Endpoint", "Parameter"),
    /// or <c>null</c> to treat all types equally.
    /// </value>
    /// <remarks>
    /// LOGIC: Passed through to <see cref="ScoringConfig.PreferredTypes"/>
    /// override. When non-null, entities with matching types score 1.0
    /// for the type signal, while non-matching types score 0.3.
    /// </remarks>
    public IReadOnlySet<string>? PreferredEntityTypes { get; init; }
}
