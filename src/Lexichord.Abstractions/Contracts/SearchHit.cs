// =============================================================================
// File: SearchHit.cs
// Project: Lexichord.Abstractions
// Description: Record representing an individual semantic search result,
//              pairing a text chunk with its source document and similarity score.
// =============================================================================
// LOGIC: Non-positional record wrapping a TextChunk with its source Document
//   and cosine similarity score.
//   - Chunk and Document are required properties (must be set at construction).
//   - Score is a float in [0.0, 1.0] representing cosine similarity.
//   - ScorePercent and ScoreDecimal provide formatted score strings for UI display.
//   - GetPreview(maxLength) delegates to Chunk.Content for truncated previews,
//     trimming trailing whitespace before appending ellipsis.
//   - Score uses float (not double) to match embedding vector element type.
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents an individual search result with a text chunk, source document, and relevance score.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="SearchHit"/> pairs a <see cref="TextChunk"/> matched by a semantic search query
/// with its source <see cref="RAG.Document"/> metadata and a cosine similarity
/// <see cref="Score"/>. Hits are returned within a <see cref="SearchResult"/> ranked by
/// descending score (most relevant first).
/// </para>
/// <para>
/// <b>Score Interpretation:</b>
/// <list type="bullet">
///   <item>Score &gt;= 0.9 — Very High (near identical content)</item>
///   <item>Score &gt;= 0.8 — High (strongly related)</item>
///   <item>Score &gt;= 0.7 — Medium (related)</item>
///   <item>Score &gt;= 0.5 — Low (loosely related)</item>
///   <item>Score &lt; 0.5 — Very Low (unlikely related)</item>
/// </list>
/// </para>
/// <para>
/// <b>UI Display:</b> Use <see cref="ScorePercent"/> for user-facing labels (e.g., "87%")
/// and <see cref="GetPreview"/> for truncated content previews in result lists.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5a as part of the Search Abstractions layer.
/// </para>
/// </remarks>
public record SearchHit
{
    /// <summary>
    /// The matching text chunk with content and metadata.
    /// </summary>
    /// <remarks>
    /// LOGIC: Contains the chunk's text content, positional offsets for source navigation,
    /// and structural metadata (heading, index, level). The chunk was identified as
    /// semantically similar to the search query by the embedding similarity comparison.
    /// </remarks>
    /// <value>
    /// A <see cref="TextChunk"/> instance. Cannot be null.
    /// </value>
    public required TextChunk Chunk { get; init; }

    /// <summary>
    /// Source document containing this chunk.
    /// </summary>
    /// <remarks>
    /// LOGIC: Provides the document-level context for this search hit, including
    /// the file path, title, and indexing status. Used to display the source
    /// document name in search results and to navigate to the source file.
    /// </remarks>
    /// <value>
    /// A <see cref="RAG.Document"/> instance. Cannot be null.
    /// </value>
    public required Document Document { get; init; }

    /// <summary>
    /// Cosine similarity score between query and chunk (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    /// LOGIC: Computed as <c>1 - cosine_distance(query_embedding, chunk_embedding)</c>
    /// by pgvector's <c>&lt;=&gt;</c> operator. Higher values indicate greater semantic
    /// similarity. The score is a <c>float</c> to match the embedding vector element type.
    /// </remarks>
    /// <value>
    /// A float between 0.0 (completely dissimilar) and 1.0 (identical vectors).
    /// Default: 0.0.
    /// </value>
    public float Score { get; init; }

    /// <summary>
    /// Score formatted as a percentage string (e.g., "87%").
    /// </summary>
    /// <remarks>
    /// LOGIC: Multiplies <see cref="Score"/> by 100 and formats with zero decimal places
    /// using the "F0" format specifier. Uses banker's rounding for midpoint values.
    /// Suitable for user-facing display in search result lists.
    /// </remarks>
    /// <value>
    /// A string in the format "N%" where N is the rounded percentage (e.g., "87%", "100%", "0%").
    /// </value>
    public string ScorePercent => $"{Score * 100:F0}%";

    /// <summary>
    /// Score formatted as a decimal string (e.g., "0.87").
    /// </summary>
    /// <remarks>
    /// LOGIC: Formats <see cref="Score"/> with two decimal places using the "F2"
    /// format specifier. Suitable for developer-facing display or logging.
    /// </remarks>
    /// <value>
    /// A string in the format "N.NN" where N is the score (e.g., "0.87", "1.00", "0.00").
    /// </value>
    public string ScoreDecimal => $"{Score:F2}";

    /// <summary>
    /// Gets a preview of the chunk content, truncated to a maximum length.
    /// </summary>
    /// <param name="maxLength">
    /// Maximum number of characters before truncation. Default: 200.
    /// </param>
    /// <returns>
    /// The full content if it fits within <paramref name="maxLength"/>;
    /// otherwise, the first <paramref name="maxLength"/> characters with trailing
    /// whitespace trimmed and "..." appended.
    /// </returns>
    /// <remarks>
    /// LOGIC: Provides a truncated view of the chunk content for display in search
    /// result lists. Trailing whitespace is trimmed before appending the ellipsis
    /// to avoid awkward spacing. The resulting string length may be less than
    /// <paramref name="maxLength"/> + 3 due to the TrimEnd operation.
    /// </remarks>
    public string GetPreview(int maxLength = 200)
    {
        if (Chunk.Content.Length <= maxLength)
            return Chunk.Content;

        return Chunk.Content[..maxLength].TrimEnd() + "...";
    }
}
