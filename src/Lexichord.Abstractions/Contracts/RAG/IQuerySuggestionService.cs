// =============================================================================
// File: IQuerySuggestionService.cs
// Project: Lexichord.Abstractions
// Description: Interface for query autocomplete suggestions (v0.5.4c).
// =============================================================================
// LOGIC: Defines the contract for providing intelligent autocomplete:
//   - GetSuggestionsAsync: Retrieve ranked suggestions for a prefix
//   - RecordQueryAsync: Track executed queries for history-based suggestions
//   - ExtractSuggestionsAsync: Extract n-grams from documents during indexing
// =============================================================================
// VERSION: v0.5.4c (Query Suggestions)
// DEPENDENCIES:
//   - IChunkRepository (v0.4.1c) for content analysis
//   - ITerminologyRepository (v0.2.2b) for domain terms
//   - Document indexing pipeline (v0.4.3) for n-gram extraction
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Provides query autocomplete suggestions based on indexed content.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IQuerySuggestionService"/> powers the search box autocomplete dropdown,
/// helping users discover content and search faster. Suggestions come from:
/// <list type="bullet">
///   <item><description>Query history (previously executed searches)</description></item>
///   <item><description>Document headings (extracted during indexing)</description></item>
///   <item><description>Content n-grams (common phrases)</description></item>
///   <item><description>Domain terms (from terminology database)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>UI Integration:</b>
/// <list type="bullet">
///   <item><description>Dropdown appears after 2+ characters typed</description></item>
///   <item><description>Debounce: 200ms after last keystroke</description></item>
///   <item><description>Max 10 suggestions visible</description></item>
///   <item><description>Keyboard navigation (↑↓ Enter Esc)</description></item>
///   <item><description>Prefix highlighting in suggestions</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Performance Target:</b> Suggestions should be returned in &lt; 50ms.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe as the service
/// may be called concurrently from UI and background indexing.
/// </para>
/// <para>
/// <b>License Gate:</b> Autocomplete is gated at Writer Pro tier via
/// <c>FeatureFlags.RAG.RelevanceTuner</c>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4c as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In search box ViewModel
/// private async Task OnSearchTextChanged(string text)
/// {
///     if (text.Length &lt; 2) return;
///
///     // Debounced call
///     var suggestions = await _suggestionService.GetSuggestionsAsync(text);
///
///     SuggestionItems = suggestions.Select(s => new SuggestionItemViewModel(s)).ToList();
///     IsSuggestionsOpen = suggestions.Any();
/// }
/// </code>
/// </example>
public interface IQuerySuggestionService
{
    /// <summary>
    /// Gets suggestions for a partial query prefix.
    /// </summary>
    /// <param name="prefix">
    /// The partial query typed so far. Minimum 2 characters recommended.
    /// </param>
    /// <param name="maxResults">
    /// Maximum suggestions to return (default: 10).
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token for async operation.
    /// </param>
    /// <returns>
    /// Ranked list of suggestions, ordered by score descending.
    /// Returns empty list if prefix is null/empty or no matches found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Matching Logic:</b>
    /// <list type="bullet">
    ///   <item><description>Case-insensitive prefix matching</description></item>
    ///   <item><description>Ranked by score (frequency + recency + source weight)</description></item>
    ///   <item><description>History matches boosted for personalization</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Performance:</b> Uses database prefix index for O(log n) lookup.
    /// Results are cached for 30 seconds per prefix.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var suggestions = await service.GetSuggestionsAsync("token", maxResults: 5);
    /// // Returns: ["token refresh", "Token Validation", "token authentication", ...]
    /// </code>
    /// </example>
    Task<IReadOnlyList<QuerySuggestion>> GetSuggestionsAsync(
        string? prefix,
        int maxResults = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an executed query for frequency tracking.
    /// </summary>
    /// <param name="query">
    /// The executed query text.
    /// </param>
    /// <param name="resultCount">
    /// Number of results returned for this query.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token for async operation.
    /// </param>
    /// <remarks>
    /// <para>
    /// Recording queries enables:
    /// <list type="bullet">
    ///   <item><description>History-based suggestions (most frequent queries)</description></item>
    ///   <item><description>Zero-result tracking (content gap analysis)</description></item>
    ///   <item><description>Query popularity metrics</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Deduplication:</b> Repeated queries increment frequency count rather
    /// than creating duplicate entries.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // After search execution
    /// await service.RecordQueryAsync("token refresh", resultCount: 12);
    /// </code>
    /// </example>
    Task RecordQueryAsync(
        string query,
        int resultCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts and stores suggestions from document content during indexing.
    /// </summary>
    /// <param name="documentId">
    /// The document being indexed.
    /// </param>
    /// <param name="content">
    /// The document content (Markdown text).
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token for async operation.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>Extraction Rules:</b>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Source</term>
    ///     <description>N-gram Size / Pattern</description>
    ///   </listheader>
    ///   <item>
    ///     <term>Document headings (# ## ###)</term>
    ///     <description>1-5 words</description>
    ///   </item>
    ///   <item>
    ///     <term>Code blocks (identifiers)</term>
    ///     <description>PascalCase/camelCase tokens</description>
    ///   </item>
    ///   <item>
    ///     <term>Bold/italic text</term>
    ///     <description>1-3 words</description>
    ///   </item>
    ///   <item>
    ///     <term>High-frequency phrases</term>
    ///     <description>2-4 word n-grams</description>
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Deduplication:</b> Normalized (lowercase, trimmed) text is used for
    /// deduplication. Frequency is incremented for repeated extractions.
    /// </para>
    /// <para>
    /// <b>Performance:</b> Extraction is performed asynchronously during
    /// document indexing to avoid blocking the main indexing pipeline.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Called by DocumentIndexingPipeline
    /// await service.ExtractSuggestionsAsync(docId, markdownContent);
    /// </code>
    /// </example>
    Task ExtractSuggestionsAsync(
        Guid documentId,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears suggestions associated with a document (for re-indexing).
    /// </summary>
    /// <param name="documentId">
    /// The document ID to clear suggestions for.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token for async operation.
    /// </param>
    /// <remarks>
    /// Called before re-indexing a document to remove stale suggestions.
    /// </remarks>
    Task ClearDocumentSuggestionsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}
