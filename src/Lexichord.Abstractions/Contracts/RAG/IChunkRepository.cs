// =============================================================================
// File: IChunkRepository.cs
// Project: Lexichord.Abstractions
// Description: Repository interface for chunk storage and vector similarity search.
// =============================================================================
// LOGIC: Defines the contract for chunk operations including vector search.
//   - AddRangeAsync enables batch insertion for efficiency.
//   - SearchSimilarAsync is the core vector similarity search method.
//   - Uses pgvector's HNSW index for fast approximate nearest neighbor search.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Repository interface for managing document chunks and performing vector similarity searches.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the data access contract for <see cref="Chunk"/> entities
/// and importantly provides the vector similarity search capability that powers the
/// RAG system's retrieval functionality.
/// </para>
/// <para>
/// The <see cref="SearchSimilarAsync"/> method leverages pgvector's HNSW (Hierarchical
/// Navigable Small World) index for efficient approximate nearest neighbor search,
/// enabling sub-linear query times even with millions of vectors.
/// </para>
/// <para>
/// <b>Batch Operations:</b> Chunks are typically created in batches during document
/// indexing. The <see cref="AddRangeAsync"/> method is optimized for this use case,
/// using PostgreSQL's COPY protocol for maximum throughput.
/// </para>
/// </remarks>
public interface IChunkRepository
{
    #region Read Operations

    /// <summary>
    /// Retrieves all chunks belonging to a specific document.
    /// </summary>
    /// <param name="documentId">The document's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A collection of chunks ordered by <see cref="Chunk.ChunkIndex"/>.
    /// May be empty if the document has no chunks or doesn't exist.
    /// </returns>
    /// <remarks>
    /// Use this method to reconstruct the full document content or to display
    /// all chunks for debugging/admin purposes. Results are ordered by chunk
    /// index to enable sequential content reconstruction.
    /// </remarks>
    Task<IEnumerable<Chunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves chunks adjacent to a center chunk within the same document.
    /// </summary>
    /// <param name="documentId">The document's unique identifier.</param>
    /// <param name="centerIndex">The zero-based index of the center chunk.</param>
    /// <param name="beforeCount">Number of chunks to retrieve before the center (0-5).</param>
    /// <param name="afterCount">Number of chunks to retrieve after the center (0-5).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A collection of sibling chunks ordered by <see cref="Chunk.ChunkIndex"/>.
    /// Does NOT include the center chunk itself.
    /// May contain fewer chunks than requested if near document boundaries.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Introduced in:</b> v0.5.3a as part of The Context Window feature.
    /// </para>
    /// <para>
    /// This method supports context expansion by retrieving surrounding chunks
    /// for a given search result. The caller partitions results into before/after
    /// based on the center index.
    /// </para>
    /// <para>
    /// <b>Query behavior:</b> Retrieves chunks where:
    /// <c>chunk_index BETWEEN (centerIndex - beforeCount) AND (centerIndex + afterCount)</c>
    /// excluding the center chunk itself.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // For chunk at index 5, get 2 before and 1 after
    /// var siblings = await repo.GetSiblingsAsync(docId, centerIndex: 5, beforeCount: 2, afterCount: 1);
    /// // Returns chunks at indices 3, 4, 6 (if they exist)
    /// </code>
    /// </example>
    Task<IReadOnlyList<Chunk>> GetSiblingsAsync(
        Guid documentId,
        int centerIndex,
        int beforeCount,
        int afterCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for chunks similar to the provided query embedding.
    /// </summary>
    /// <param name="queryEmbedding">
    /// The embedding vector to search for. Must have the same dimensionality
    /// as the stored chunk embeddings (typically 1536 for OpenAI).
    /// </param>
    /// <param name="topK">
    /// Maximum number of results to return. Defaults to 10.
    /// Higher values increase latency proportionally.
    /// </param>
    /// <param name="threshold">
    /// Minimum similarity score for results. Defaults to 0.5.
    /// Results below this threshold are excluded. Range: [0.0, 1.0].
    /// </param>
    /// <param name="projectId">
    /// Optional project ID to scope the search. If provided, only chunks
    /// from documents in this project are searched.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A collection of <see cref="ChunkSearchResult"/> ordered by descending
    /// similarity score. May be empty if no chunks meet the threshold.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="queryEmbedding"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="queryEmbedding"/> has incorrect dimensionality.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the core search method for the RAG system. The query workflow is:
    /// </para>
    /// <list type="number">
    ///   <item><description>
    ///     User enters a query (e.g., "How do I configure SSL?")
    ///   </description></item>
    ///   <item><description>
    ///     Query is converted to an embedding via the embedding service.
    ///   </description></item>
    ///   <item><description>
    ///     This method finds the most similar chunks in the vector database.
    ///   </description></item>
    ///   <item><description>
    ///     Returned chunks provide context for LLM response generation.
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Performance:</b> Uses pgvector's HNSW index with default parameters
    /// (m=16, ef_construction=64). Typical query latency is &lt;50ms for
    /// databases with up to 1M vectors.
    /// </para>
    /// </remarks>
    Task<IEnumerable<ChunkSearchResult>> SearchSimilarAsync(
        float[] queryEmbedding,
        int topK = 10,
        double threshold = 0.5,
        Guid? projectId = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Write Operations

    /// <summary>
    /// Adds multiple chunks to the repository in a single batch operation.
    /// </summary>
    /// <param name="chunks">The chunks to add.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="chunks"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is optimized for bulk insertion during document indexing.
    /// A typical indexing operation creates 5-50 chunks per document, making
    /// batch insertion essential for performance.
    /// </para>
    /// <para>
    /// Implementation should use PostgreSQL's COPY protocol via Npgsql
    /// for maximum throughput (up to 100K rows/second).
    /// </para>
    /// </remarks>
    Task AddRangeAsync(IEnumerable<Chunk> chunks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all chunks belonging to a specific document.
    /// </summary>
    /// <param name="documentId">The document's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The number of chunks that were deleted.
    /// </returns>
    /// <remarks>
    /// <para>
    /// While the database enforces cascade delete via foreign key, this method
    /// is useful for explicitly clearing chunks before re-indexing a document
    /// without deleting the document record itself.
    /// </para>
    /// <para>
    /// <b>Re-indexing workflow:</b>
    /// </para>
    /// <list type="number">
    ///   <item><description>
    ///     Call <see cref="DeleteByDocumentIdAsync"/> to remove old chunks.
    ///   </description></item>
    ///   <item><description>
    ///     Re-chunk the document with updated content.
    ///   </description></item>
    ///   <item><description>
    ///     Generate new embeddings for each chunk.
    ///   </description></item>
    ///   <item><description>
    ///     Call <see cref="AddRangeAsync"/> to store new chunks.
    ///   </description></item>
    /// </list>
    /// </remarks>
    Task<int> DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    #endregion
}
