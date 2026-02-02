// =============================================================================
// File: ISearchHistoryService.cs
// Project: Lexichord.Abstractions
// Description: Interface for managing semantic search query history.
// =============================================================================
// LOGIC: Defines a history service for search queries with optional persistence.
//   - AddQuery: Adds a new query to history (deduplicates, caps at MaxHistorySize).
//   - RemoveQuery: Removes a specific query from history (v0.4.6d).
//   - GetRecentQueries: Returns the most recent queries (newest first).
//   - RecentQueries: Property access to full history (v0.4.6d).
//   - ClearHistory: Clears all stored queries.
//   - SaveAsync/LoadAsync: Persist/restore history to/from user settings (v0.4.6d).
//   - HistoryChanged: Event fired on any history mutation (v0.4.6d).
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.6a: Original interface definition
//   - v0.4.6d: Enhanced with persistence, removal, and events
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Enumeration of search history change types for <see cref="SearchHistoryChangedEventArgs"/>.
/// </summary>
/// <remarks>
/// <b>Introduced in:</b> v0.4.6d as part of Search History enhancement.
/// </remarks>
public enum SearchHistoryChangeType
{
    /// <summary>Query added to history.</summary>
    Added,

    /// <summary>Query removed from history.</summary>
    Removed,

    /// <summary>All history cleared.</summary>
    Cleared,

    /// <summary>History loaded from persistent storage.</summary>
    Loaded
}

/// <summary>
/// Event args for search history changes.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SearchHistoryChangedEventArgs"/> is raised by <see cref="ISearchHistoryService"/>
/// whenever the history is modified. This enables reactive UI updates without polling.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6d as part of Search History enhancement.
/// </para>
/// </remarks>
public class SearchHistoryChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    public SearchHistoryChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets the query that was added or removed.
    /// </summary>
    /// <value>
    /// The affected query, or null for <see cref="SearchHistoryChangeType.Cleared"/>
    /// and <see cref="SearchHistoryChangeType.Loaded"/> operations.
    /// </value>
    public string? Query { get; init; }

    /// <summary>
    /// Gets the new total count of queries in history.
    /// </summary>
    public int NewCount { get; init; }
}

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
///   <item>Persistence to system settings (v0.4.6d).</item>
///   <item>Change notification events (v0.4.6d).</item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe as the service
/// is registered as a singleton and may be accessed from multiple view models.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6a as part of the Reference Panel View.
/// Enhanced in v0.4.6d with persistence and events.
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
    /// Gets the list of recent queries, most recent first.
    /// </summary>
    /// <value>
    /// A read-only list of all queries in history, ordered from newest to oldest.
    /// </value>
    /// <remarks>
    /// <b>Introduced in:</b> v0.4.6d as property-based access to the full history.
    /// </remarks>
    IReadOnlyList<string> RecentQueries { get; }

    /// <summary>
    /// Event raised when history changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Raised after any mutation to the history (add, remove, clear, load).
    /// Enables reactive UI updates without polling.
    /// <para>
    /// <b>Introduced in:</b> v0.4.6d as part of Search History enhancement.
    /// </para>
    /// </remarks>
    event EventHandler<SearchHistoryChangedEventArgs>? HistoryChanged;

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
    /// Removes a specific query from history.
    /// </summary>
    /// <param name="query">
    /// The query to remove. Matching is case-insensitive.
    /// </param>
    /// <returns>
    /// True if the query was found and removed, false if not found.
    /// </returns>
    /// <remarks>
    /// <b>Introduced in:</b> v0.4.6d for user-initiated history item removal.
    /// </remarks>
    bool RemoveQuery(string query);

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

    /// <summary>
    /// Saves history to persistent storage.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    /// <remarks>
    /// LOGIC: Persists the current history to system settings as a JSON array.
    /// If the history has not changed since the last save, the operation is skipped.
    /// <para>
    /// <b>Introduced in:</b> v0.4.6d for persistence support.
    /// </para>
    /// </remarks>
    Task SaveAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads history from persistent storage.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous load operation.</returns>
    /// <remarks>
    /// LOGIC: Restores history from system settings. If no saved history exists
    /// or the JSON is invalid, the history remains empty. Respects <see cref="MaxHistorySize"/>.
    /// <para>
    /// <b>Introduced in:</b> v0.4.6d for persistence support.
    /// </para>
    /// </remarks>
    Task LoadAsync(CancellationToken ct = default);
}
