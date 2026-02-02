// =============================================================================
// File: SearchHistoryService.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of search query history with in-memory storage.
// =============================================================================
// LOGIC: Thread-safe in-memory history service using a LinkedList for O(1) operations.
//   - AddQuery: Deduplicates, moves existing to front, evicts LRU when full.
//   - GetRecentQueries: Returns snapshot of history (newest first).
//   - ClearHistory: Clears all stored queries.
//   - Uses lock for thread safety (singleton lifetime).
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.6a: ISearchHistoryService
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Thread-safe in-memory implementation of search query history.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SearchHistoryService"/> maintains a list of recent search queries
/// in memory, enabling quick re-execution and autocomplete in the Reference Panel.
/// The service uses a <see cref="LinkedList{T}"/> for efficient O(1) insertions
/// at the front and removals from the back.
/// </para>
/// <para>
/// <b>Thread Safety:</b> All operations are protected by a lock to ensure
/// thread safety when accessed from multiple view models concurrently.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6a as part of the Reference Panel View.
/// </para>
/// </remarks>
public sealed class SearchHistoryService : ISearchHistoryService
{
    private readonly LinkedList<string> _history = new();
    private readonly Dictionary<string, LinkedListNode<string>> _index = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private readonly ILogger<SearchHistoryService> _logger;

    /// <inheritdoc />
    public int MaxHistorySize { get; } = 10;

    /// <summary>
    /// Creates a new <see cref="SearchHistoryService"/> instance.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public SearchHistoryService(ILogger<SearchHistoryService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void AddQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return;

        var trimmed = query.Trim();

        lock (_lock)
        {
            // LOGIC: If the query exists, move it to the front.
            if (_index.TryGetValue(trimmed, out var existingNode))
            {
                _history.Remove(existingNode);
                _history.AddFirst(existingNode);
                _logger.LogDebug("Moved existing query to front: {Query}", trimmed);
                return;
            }

            // LOGIC: Add new query to front.
            var newNode = _history.AddFirst(trimmed);
            _index[trimmed] = newNode;

            // LOGIC: Evict oldest if over capacity.
            if (_history.Count > MaxHistorySize)
            {
                var lastNode = _history.Last!;
                _index.Remove(lastNode.Value);
                _history.RemoveLast();
                _logger.LogDebug("Evicted oldest query due to capacity: {Query}", lastNode.Value);
            }

            _logger.LogDebug("Added new query to history: {Query}", trimmed);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetRecentQueries(int count = 10)
    {
        if (count <= 0)
            return Array.Empty<string>();

        lock (_lock)
        {
            return _history.Take(count).ToList().AsReadOnly();
        }
    }

    /// <inheritdoc />
    public void ClearHistory()
    {
        lock (_lock)
        {
            _history.Clear();
            _index.Clear();
            _logger.LogInformation("Search history cleared");
        }
    }
}
