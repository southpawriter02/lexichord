// =============================================================================
// File: ISearchHistoryService.cs
// Project: Lexichord.Abstractions
// Description: Interface for managing semantic search query history.
// =============================================================================
// LOGIC: Defines a simple in-memory history service for search queries.
//   - AddQuery: Adds a new query to history (deduplicates, caps at MaxHistorySize).
//   - GetRecentQueries: Returns the most recent queries (newest first).
//   - ClearHistory: Clears all stored queries.
//   - Implementations may optionally persist to user settings.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Service for managing semantic search query history.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ISearchHistoryService"/> provides in-memory storage of recent search
/// queries to enable quick re-execution and autocomplete functionality in the
/// Reference Panel UI. The history is capped at a configurable maximum size
/// (default: 10) and deduplicates queries.
/// </para>
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
///   <item>In-memory storage of recent queries.</item>
///   <item>Deduplication based on case-insensitive comparison.</item>
///   <item>LRU eviction when capacity is reached.</item>
///   <item>Optional persistence to user settings.</item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe as the service
/// is registered as a singleton and may be accessed from multiple view models.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6a as part of the Reference Panel View.
/// </para>
/// </remarks>
public interface ISearchHistoryService
{
    /// <summary>
    /// Gets the maximum number of queries stored in history.
    /// </summary>
    /// <value>
    /// The maximum history size. Default: 10.
    /// </value>
    int MaxHistorySize { get; }

    /// <summary>
    /// Adds a query to the history.
    /// </summary>
    /// <param name="query">
    /// The search query to add. Null, empty, or whitespace-only queries are ignored.
    /// </param>
    /// <remarks>
    /// LOGIC: If the query already exists (case-insensitive), it is moved to the
    /// front of the history. Otherwise, it is added to the front and the oldest
    /// query is evicted if the history exceeds <see cref="MaxHistorySize"/>.
    /// </remarks>
    void AddQuery(string? query);

    /// <summary>
    /// Gets the most recent search queries.
    /// </summary>
    /// <param name="count">
    /// Maximum number of queries to return. Default: 10.
    /// </param>
    /// <returns>
    /// A read-only list of recent queries, ordered from newest to oldest.
    /// Returns an empty list if no queries have been recorded.
    /// </returns>
    IReadOnlyList<string> GetRecentQueries(int count = 10);

    /// <summary>
    /// Clears all stored queries from history.
    /// </summary>
    /// <remarks>
    /// LOGIC: Removes all queries from both in-memory storage and any persisted
    /// storage (if implemented). The next call to <see cref="GetRecentQueries"/>
    /// will return an empty list.
    /// </remarks>
    void ClearHistory();
}
