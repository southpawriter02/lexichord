// =============================================================================
// File: DuplicateCandidate.cs
// Project: Lexichord.Abstractions
// Description: Record representing a potential duplicate chunk with classification.
// =============================================================================
// VERSION: v0.5.9d (Deduplication Service)
// LOGIC: Bundles an existing chunk with its similarity score, relationship
//   classification, and optional canonical record reference for deduplication decisions.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// A potential duplicate candidate with classification.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9d as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This record is returned by <see cref="IDeduplicationService.FindDuplicatesAsync"/>
/// to provide a complete picture of each potential duplicate:
/// </para>
/// <list type="bullet">
///   <item><description>The full <see cref="Chunk"/> data for comparison.</description></item>
///   <item><description>The vector similarity score from <see cref="ISimilarityDetector"/>.</description></item>
///   <item><description>The semantic classification from <see cref="IRelationshipClassifier"/>.</description></item>
///   <item><description>The canonical record ID if the chunk is already part of a canonical group.</description></item>
/// </list>
/// </remarks>
/// <param name="ExistingChunk">
/// The existing chunk that is a potential duplicate.
/// Contains the full chunk data including content and embedding.
/// </param>
/// <param name="SimilarityScore">
/// The cosine similarity score between the new chunk and this candidate.
/// Range: 0.0 to 1.0, where 1.0 is identical.
/// </param>
/// <param name="Classification">
/// The semantic relationship classification between the new chunk and this candidate.
/// Determined by <see cref="IRelationshipClassifier"/> using rule-based or LLM classification.
/// </param>
/// <param name="CanonicalRecordId">
/// The ID of the canonical record this chunk belongs to, if any.
/// Null if the chunk has not been processed for deduplication yet.
/// When not null, merging should target this canonical rather than creating a new one.
/// </param>
public record DuplicateCandidate(
    Chunk ExistingChunk,
    float SimilarityScore,
    RelationshipClassification Classification,
    Guid? CanonicalRecordId)
{
    /// <summary>
    /// Gets whether this candidate is already part of a canonical group.
    /// </summary>
    /// <value><c>true</c> if <see cref="CanonicalRecordId"/> is not null.</value>
    public bool HasCanonicalRecord => CanonicalRecordId.HasValue;

    /// <summary>
    /// Gets whether this candidate is classified as equivalent to the source chunk.
    /// </summary>
    /// <value><c>true</c> if classification type is <see cref="RelationshipType.Equivalent"/>.</value>
    public bool IsEquivalent => Classification.Type == RelationshipType.Equivalent;

    /// <summary>
    /// Gets whether this candidate is classified as contradictory to the source chunk.
    /// </summary>
    /// <value><c>true</c> if classification type is <see cref="RelationshipType.Contradictory"/>.</value>
    public bool IsContradictory => Classification.Type == RelationshipType.Contradictory;

    /// <summary>
    /// Gets whether this candidate's classification has high confidence.
    /// </summary>
    /// <value><c>true</c> if classification confidence is >= 0.7.</value>
    public bool HasHighConfidence => Classification.Confidence >= 0.7f;
}
