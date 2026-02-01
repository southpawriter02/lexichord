// =============================================================================
// File: SearchResult.cs
// Project: Lexichord.Abstractions
// Description: Record encapsulating the outcome of a semantic search operation,
//              including ranked hits, timing, query embedding, and truncation info.
// =============================================================================
// LOGIC: Non-positional record with required and optional properties.
//   - Hits is required and must be set at construction (ranked by descending score).
//   - Empty() factory creates a zero-result instance for early returns.
//   - Count and HasResults are computed convenience properties.
//   - WasTruncated indicates whether more matches exist beyond TopK.
//   - QueryEmbedding stores the vector used for the search (nullable).
//   - Note: float[] QueryEmbedding uses reference equality in record comparison;
//     two SearchResult instances with identical but separate arrays are NOT equal.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Container for semantic search operation results.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="SearchResult"/> encapsulates the complete output of a semantic search
/// operation, including the ranked list of <see cref="SearchHit"/> matches, timing
/// information, the query embedding vector, and truncation metadata.
/// </para>
/// <para>
/// <b>Hit Ordering:</b> The <see cref="Hits"/> list is ranked by descending cosine
/// similarity score. The first hit is the most relevant match.
/// </para>
/// <para>
/// <b>Empty Results:</b> Use <see cref="Empty"/> to create a zero-result instance
/// for early-return scenarios (e.g., empty query, no indexed documents).
/// </para>
/// <para>
/// <b>Truncation:</b> When <see cref="WasTruncated"/> is <c>true</c>, the actual
/// number of matching chunks exceeded the <see cref="SearchOptions.TopK"/> limit.
/// Additional matches may be retrieved by increasing TopK.
/// </para>
/// <para>
/// <b>Note:</b> This type is distinct from <c>Lexichord.Abstractions.Contracts.Editor.SearchResult</c>,
/// which represents a text-based find/replace match. This <see cref="SearchResult"/> is for
/// vector-based semantic search in the RAG subsystem.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5a as part of the Search Abstractions layer.
/// </para>
/// </remarks>
public record SearchResult
{
    /// <summary>
    /// Matching chunks ranked by similarity score (highest first).
    /// </summary>
    /// <remarks>
    /// LOGIC: The list is sorted by descending <see cref="SearchHit.Score"/>.
    /// Empty list if no matches found above the <see cref="SearchOptions.MinScore"/>
    /// threshold. The list length is bounded by <see cref="SearchOptions.TopK"/>.
    /// </remarks>
    /// <value>
    /// A read-only list of <see cref="SearchHit"/> instances. Never null; may be empty.
    /// </value>
    public required IReadOnlyList<SearchHit> Hits { get; init; }

    /// <summary>
    /// Total search duration including preprocessing, embedding, and query time.
    /// </summary>
    /// <remarks>
    /// LOGIC: Measured from the start of the search operation (including query preprocessing
    /// and embedding generation) to the completion of result assembly. Useful for
    /// performance monitoring and optimization.
    /// </remarks>
    /// <value>
    /// The elapsed time for the complete search operation. Default: <see cref="TimeSpan.Zero"/>.
    /// </value>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// The query embedding vector used for the similarity search.
    /// </summary>
    /// <remarks>
    /// LOGIC: Stores the float array produced by <c>IEmbeddingService.EmbedAsync</c>
    /// for the processed query text. May be <c>null</c> if embedding failed or was
    /// not performed (e.g., cached result, error path). Useful for debugging and
    /// for caching in <c>IQueryPreprocessor</c>.
    /// </remarks>
    /// <value>
    /// A float array of length matching <c>IEmbeddingService.Dimensions</c>, or <c>null</c>.
    /// </value>
    public float[]? QueryEmbedding { get; init; }

    /// <summary>
    /// Original query text (before preprocessing).
    /// </summary>
    /// <remarks>
    /// LOGIC: Preserves the user's original query for display in search history
    /// and telemetry events. This is the raw input before normalization,
    /// abbreviation expansion, or other preprocessing by <c>IQueryPreprocessor</c>.
    /// </remarks>
    /// <value>
    /// The original query string, or <c>null</c> if not provided.
    /// </value>
    public string? Query { get; init; }

    /// <summary>
    /// Whether results were truncated at the <see cref="SearchOptions.TopK"/> limit.
    /// </summary>
    /// <remarks>
    /// LOGIC: When <c>true</c>, the database returned at least <see cref="SearchOptions.TopK"/>
    /// matching chunks, indicating more results may exist beyond the limit.
    /// UI can display a "Show more results" affordance when this is <c>true</c>.
    /// </remarks>
    /// <value>
    /// <c>true</c> if the hit count reached the TopK limit; otherwise, <c>false</c>.
    /// </value>
    public bool WasTruncated { get; init; }

    /// <summary>
    /// Gets the number of hits returned.
    /// </summary>
    /// <value>
    /// The count of <see cref="Hits"/>. Zero if no matches found.
    /// </value>
    public int Count => Hits.Count;

    /// <summary>
    /// Gets whether any results were found.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Hits"/> contains at least one element; otherwise, <c>false</c>.
    /// </value>
    public bool HasResults => Hits.Count > 0;

    /// <summary>
    /// Creates an empty search result with no hits.
    /// </summary>
    /// <param name="query">
    /// Optional original query text to preserve in the result.
    /// </param>
    /// <returns>
    /// A <see cref="SearchResult"/> with an empty <see cref="Hits"/> list,
    /// zero <see cref="Duration"/>, and <see cref="WasTruncated"/> set to <c>false</c>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Factory method for early-return scenarios where no search was performed
    /// (e.g., empty query, no indexed documents, license denial). Avoids null
    /// <see cref="SearchResult"/> instances in the API.
    /// </remarks>
    public static SearchResult Empty(string? query = null) => new()
    {
        Hits = Array.Empty<SearchHit>(),
        Query = query
    };
}
