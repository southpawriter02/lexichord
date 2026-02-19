// =============================================================================
// File: SyncEventStore.cs
// Project: Lexichord.Modules.Knowledge
// Description: In-memory implementation of IEventStore for sync event persistence.
// =============================================================================
// LOGIC: Provides thread-safe in-memory storage for sync event records using
//   ConcurrentDictionary. Supports filtering, sorting, and pagination for
//   event history queries. Future versions may add persistent storage.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: IEventStore, SyncEventRecord, SyncEventQuery (v0.7.6j)
// =============================================================================

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.Events;

/// <summary>
/// In-memory implementation of <see cref="IEventStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides thread-safe storage for sync event records using
/// <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </para>
/// <para>
/// <b>Storage Structure:</b>
/// </para>
/// <list type="bullet">
///   <item><b>_events:</b> All event records by EventId.</item>
///   <item><b>_eventsByDocument:</b> Event IDs grouped by DocumentId for efficient lookups.</item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> All operations are thread-safe via ConcurrentDictionary.
/// List modifications use lock statements for atomicity.
/// </para>
/// <para>
/// <b>Persistence:</b> Currently in-memory only. Data is lost on application restart.
/// Future versions may add database persistence.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
public sealed class SyncEventStore : IEventStore
{
    // LOGIC: Primary storage for all event records by EventId.
    private readonly ConcurrentDictionary<Guid, SyncEventRecord> _events = new();

    // LOGIC: Secondary index for document-centric queries.
    private readonly ConcurrentDictionary<Guid, List<Guid>> _eventsByDocument = new();

    private readonly ILogger<SyncEventStore> _logger;

    // LOGIC: Lock object for list modifications within the document index.
    private readonly object _documentIndexLock = new();

    /// <summary>
    /// Initializes a new instance of <see cref="SyncEventStore"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public SyncEventStore(ILogger<SyncEventStore> logger)
    {
        _logger = logger;
        _logger.LogDebug("SyncEventStore initialized");
    }

    #region IEventStore Implementation

    /// <inheritdoc/>
    public Task StoreAsync(SyncEventRecord record, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Storing event record {EventId} of type {EventType} for document {DocumentId}",
            record.EventId, record.EventType, record.DocumentId);

        // LOGIC: TryAdd returns false if key already exists.
        // For idempotent storage, we update if the record already exists.
        if (!_events.TryAdd(record.EventId, record))
        {
            _logger.LogTrace(
                "Event {EventId} already exists, updating",
                record.EventId);
            _events[record.EventId] = record;
        }

        // LOGIC: Update the document index.
        lock (_documentIndexLock)
        {
            var eventIds = _eventsByDocument.GetOrAdd(
                record.DocumentId,
                _ => new List<Guid>());

            if (!eventIds.Contains(record.EventId))
            {
                eventIds.Add(record.EventId);
            }
        }

        _logger.LogTrace(
            "Stored event {EventId}, total events: {Count}",
            record.EventId, _events.Count);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<SyncEventRecord?> GetAsync(Guid eventId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogTrace("Getting event record {EventId}", eventId);

        _events.TryGetValue(eventId, out var record);

        if (record is null)
        {
            _logger.LogTrace("Event {EventId} not found", eventId);
        }

        return Task.FromResult(record);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SyncEventRecord>> QueryAsync(
        SyncEventQuery query,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Querying events with filters: EventType={EventType}, DocumentId={DocumentId}, " +
            "PageSize={PageSize}, PageOffset={PageOffset}",
            query.EventType, query.DocumentId, query.PageSize, query.PageOffset);

        // LOGIC: Start with all events and apply filters incrementally.
        IEnumerable<SyncEventRecord> filtered = _events.Values;

        // Filter by EventType
        if (!string.IsNullOrEmpty(query.EventType))
        {
            filtered = filtered.Where(e =>
                e.EventType.Equals(query.EventType, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by DocumentId
        if (query.DocumentId.HasValue)
        {
            filtered = filtered.Where(e => e.DocumentId == query.DocumentId.Value);
        }

        // Filter by PublishedAfter
        if (query.PublishedAfter.HasValue)
        {
            filtered = filtered.Where(e => e.PublishedAt > query.PublishedAfter.Value);
        }

        // Filter by PublishedBefore
        if (query.PublishedBefore.HasValue)
        {
            filtered = filtered.Where(e => e.PublishedAt < query.PublishedBefore.Value);
        }

        // Filter by SuccessfulOnly
        if (query.SuccessfulOnly.HasValue)
        {
            filtered = filtered.Where(e =>
                e.AllHandlersSucceeded == query.SuccessfulOnly.Value);
        }

        // LOGIC: Apply sorting based on EventSortOrder enum.
        filtered = query.SortOrder switch
        {
            EventSortOrder.ByPublishedAscending => filtered
                .OrderBy(e => e.PublishedAt),
            EventSortOrder.ByPublishedDescending => filtered
                .OrderByDescending(e => e.PublishedAt),
            EventSortOrder.ByDocumentAscending => filtered
                .OrderBy(e => e.DocumentId.ToString()),
            EventSortOrder.ByDocumentDescending => filtered
                .OrderByDescending(e => e.DocumentId.ToString()),
            EventSortOrder.ByEventTypeAscending => filtered
                .OrderBy(e => e.EventType),
            _ => filtered.OrderByDescending(e => e.PublishedAt)
        };

        // Apply pagination
        var results = filtered
            .Skip(query.PageOffset)
            .Take(Math.Min(query.PageSize, 1000)) // Cap at 1000 for safety
            .ToList();

        _logger.LogDebug("Query returned {Count} events", results.Count);

        return Task.FromResult<IReadOnlyList<SyncEventRecord>>(results);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SyncEventRecord>> GetByDocumentAsync(
        Guid documentId,
        int limit = 100,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Getting events for document {DocumentId} with limit {Limit}",
            documentId, limit);

        if (!_eventsByDocument.TryGetValue(documentId, out var eventIds))
        {
            _logger.LogTrace("No events found for document {DocumentId}", documentId);
            return Task.FromResult<IReadOnlyList<SyncEventRecord>>(
                Array.Empty<SyncEventRecord>());
        }

        // LOGIC: Get all event records for the document and sort by timestamp.
        List<SyncEventRecord> results;
        lock (_documentIndexLock)
        {
            results = eventIds
                .Select(id => _events.TryGetValue(id, out var record) ? record : null)
                .Where(r => r is not null)
                .Cast<SyncEventRecord>()
                .OrderByDescending(r => r.PublishedAt)
                .Take(limit)
                .ToList();
        }

        _logger.LogDebug(
            "Retrieved {Count} events for document {DocumentId}",
            results.Count, documentId);

        return Task.FromResult<IReadOnlyList<SyncEventRecord>>(results);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets the total number of stored events.
    /// </summary>
    /// <returns>The count of events in the store.</returns>
    /// <remarks>
    /// LOGIC: Utility method for monitoring and testing.
    /// </remarks>
    public int GetEventCount() => _events.Count;

    /// <summary>
    /// Gets the number of unique documents with events.
    /// </summary>
    /// <returns>The count of documents with at least one event.</returns>
    /// <remarks>
    /// LOGIC: Utility method for monitoring and testing.
    /// </remarks>
    public int GetDocumentCount() => _eventsByDocument.Count;

    /// <summary>
    /// Clears all events from the store.
    /// </summary>
    /// <remarks>
    /// LOGIC: For testing purposes only. Not exposed via IEventStore.
    /// </remarks>
    internal void Clear()
    {
        _events.Clear();
        _eventsByDocument.Clear();
        _logger.LogInformation("SyncEventStore cleared");
    }

    #endregion
}
