// =============================================================================
// File: ChunkSearchResult.cs
// Project: Lexichord.Abstractions
// Description: Record representing a similarity search result with score.
// =============================================================================
// LOGIC: Wraps a Chunk with its similarity score for ranked search results.
//   - SimilarityScore ranges from 0.0 (dissimilar) to 1.0 (identical).
//   - The score is typically derived from cosine similarity of embeddings.
//   - Results are returned ordered by descending score.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Represents a chunk returned from a similarity search along with its relevance score.
/// </summary>
/// <remarks>
/// <para>
/// This record pairs a <see cref="Chunk"/> with its <see cref="SimilarityScore"/>,
/// enabling ranked presentation of search results. The similarity score is computed
/// by pgvector using the HNSW index configured on the chunks table.
/// </para>
/// <para>
/// When using cosine distance (the default for text embeddings), the similarity score
/// is calculated as <c>1 - cosine_distance</c>, where higher values indicate greater
/// semantic similarity.
/// </para>
/// </remarks>
/// <param name="Chunk">
/// The chunk that matched the search query.
/// Contains the text content, embedding, and source location information.
/// </param>
/// <param name="SimilarityScore">
/// The relevance score for this result, typically in the range [0.0, 1.0].
/// Higher values indicate greater semantic similarity to the query.
/// For cosine similarity: 1.0 means identical vectors, 0.0 means orthogonal.
/// </param>
public record ChunkSearchResult(Chunk Chunk, double SimilarityScore)
{
    /// <summary>
    /// Gets a value indicating whether this result meets a high confidence threshold.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="SimilarityScore"/> is 0.8 or higher; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// This is a convenience property for filtering high-confidence matches.
    /// The 0.8 threshold is appropriate for most use cases but may need adjustment
    /// based on the embedding model and domain.
    /// </remarks>
    public bool IsHighConfidence => SimilarityScore >= 0.8;

    /// <summary>
    /// Gets a value indicating whether this result meets a minimum relevance threshold.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="SimilarityScore"/> is 0.5 or higher; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// Results below 0.5 similarity are generally not useful and may represent
    /// noise or unrelated content. Consider filtering these in production scenarios.
    /// </remarks>
    public bool MeetsMinimumThreshold => SimilarityScore >= 0.5;
}
