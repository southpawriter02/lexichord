// =============================================================================
// File: ChunkPair.cs
// Project: Lexichord.Abstractions
// Description: Record representing a pair of chunks for relationship classification.
// =============================================================================
// VERSION: v0.5.9b (Relationship Classification)
// LOGIC: Bundles two chunks with their similarity score for batch classification.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Represents a pair of chunks to be classified for their semantic relationship.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9b as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// Used by <see cref="IRelationshipClassifier.ClassifyBatchAsync"/> to enable
/// efficient batch processing of multiple chunk pairs.
/// </para>
/// </remarks>
/// <param name="ChunkA">The first chunk in the pair.</param>
/// <param name="ChunkB">The second chunk in the pair.</param>
/// <param name="SimilarityScore">
/// The similarity score between the chunks (0.0 to 1.0).
/// Typically provided by <see cref="ISimilarityDetector"/>.
/// </param>
public record ChunkPair(
    Chunk ChunkA,
    Chunk ChunkB,
    float SimilarityScore);
