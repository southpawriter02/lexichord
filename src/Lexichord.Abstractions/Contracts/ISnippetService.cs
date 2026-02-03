// =============================================================================
// File: ISnippetService.cs
// Project: Lexichord.Abstractions
// Description: Interface for extracting contextual snippets from text chunks.
// =============================================================================
// LOGIC: Defines the contract for snippet extraction.
//   - ExtractSnippet extracts a single snippet centered on query matches.
//   - ExtractMultipleSnippets extracts multiple non-overlapping snippets.
//   - ExtractBatch processes multiple chunks efficiently.
//   - All methods use IQueryAnalyzer for keyword extraction.
// =============================================================================
// VERSION: v0.5.6a (Snippet Extraction)
// DEPENDENCIES:
//   - TextChunk (v0.4.3a)
//   - IQueryAnalyzer (v0.5.4a)
//   - ISentenceBoundaryDetector (v0.5.6c)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Extracts contextual snippets from text chunks based on query terms.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ISnippetService"/> is the primary interface for the Answer Preview
/// feature, generating user-friendly previews of search results that highlight
/// matching query terms.
/// </para>
/// <para>
/// <b>Snippet Centering:</b> Snippets are centered on regions with the highest
/// density of query matches, ensuring users see the most relevant content first.
/// </para>
/// <para>
/// <b>Highlighting:</b> Extracted snippets include <see cref="HighlightSpan"/>
/// metadata that identifies matched regions, enabling UI components to apply
/// visual highlighting.
/// </para>
/// <para>
/// <b>Sentence Boundaries:</b> When <see cref="SnippetOptions.RespectSentenceBoundaries"/>
/// is enabled, snippets expand to natural sentence boundaries for improved readability.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe as the service
/// is registered as a singleton and may be called concurrently.
/// </para>
/// <para>
/// <b>Performance Target:</b> Single snippet extraction should complete in &lt; 10ms (P95).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6a as part of The Answer Preview feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class SearchResultFormatter(ISnippetService snippetService)
/// {
///     public string FormatResult(TextChunk chunk, string query)
///     {
///         var snippet = snippetService.ExtractSnippet(chunk, query, SnippetOptions.Default);
///         return snippet.Text; // Ready for display with truncation markers
///     }
/// }
/// </code>
/// </example>
public interface ISnippetService
{
    /// <summary>
    /// Extracts a single snippet from a chunk centered on query matches.
    /// </summary>
    /// <param name="chunk">The text chunk to extract from.</param>
    /// <param name="query">The search query for match highlighting.</param>
    /// <param name="options">Extraction configuration options.</param>
    /// <returns>
    /// A <see cref="Snippet"/> containing the extracted text and highlights.
    /// Returns <see cref="Snippet.Empty"/> for null or empty chunk content.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The snippet is centered on the region with the highest density of query
    /// term matches. If no matches are found, the snippet falls back to the
    /// first <see cref="SnippetOptions.MaxLength"/> characters of the chunk.
    /// </para>
    /// </remarks>
    Snippet ExtractSnippet(TextChunk chunk, string query, SnippetOptions options);

    /// <summary>
    /// Extracts multiple snippets when a chunk has several match regions.
    /// </summary>
    /// <param name="chunk">The text chunk to extract from.</param>
    /// <param name="query">The search query for match highlighting.</param>
    /// <param name="options">Extraction configuration options.</param>
    /// <param name="maxSnippets">Maximum number of snippets to extract (default: 3).</param>
    /// <returns>
    /// A list of non-overlapping <see cref="Snippet"/> instances, ordered by
    /// match density (highest first). Returns at least one snippet (fallback)
    /// even if no matches are found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Matches are clustered by proximity, and snippets are extracted from
    /// the top clusters. Overlapping regions are automatically excluded.
    /// </para>
    /// </remarks>
    IReadOnlyList<Snippet> ExtractMultipleSnippets(
        TextChunk chunk,
        string query,
        SnippetOptions options,
        int maxSnippets = 3);

    /// <summary>
    /// Extracts snippets for multiple chunks in batch.
    /// </summary>
    /// <param name="chunks">The chunks to process.</param>
    /// <param name="query">The search query for match highlighting.</param>
    /// <param name="options">Extraction configuration options.</param>
    /// <returns>
    /// A dictionary mapping chunk ID to extracted <see cref="Snippet"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Batch processing analyzes the query once and applies it to all chunks,
    /// improving efficiency for search result lists.
    /// </para>
    /// </remarks>
    IDictionary<Guid, Snippet> ExtractBatch(
        IEnumerable<TextChunk> chunks,
        string query,
        SnippetOptions options);
}
