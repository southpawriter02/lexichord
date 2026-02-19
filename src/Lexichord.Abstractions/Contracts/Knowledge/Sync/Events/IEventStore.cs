// =============================================================================
// File: IEventStore.cs
// Project: Lexichord.Abstractions
// Description: Interface for sync event persistence and retrieval.
// =============================================================================
// LOGIC: IEventStore provides the persistence layer for sync events,
//   enabling event history queries and audit trails. Implementations
//   should be thread-safe for concurrent event recording.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: SyncEventRecord, SyncEventQuery (v0.7.6j)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Interface for sync event persistence and retrieval.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Provides the persistence layer for sync events,
/// enabling event history queries, audit trails, and event replay.
/// </para>
/// <para>
/// <b>Implementation:</b> Implementations should be thread-safe for
/// concurrent event recording. The in-memory implementation uses
/// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>.
/// </para>
/// <para>
/// <b>Retention:</b> Event retention is license-tier dependent:
/// </para>
/// <list type="bullet">
///   <item>WriterPro: 7 days</item>
///   <item>Teams: 30 days</item>
///   <item>Enterprise: Unlimited</item>
/// </list>
/// <para>
/// <b>Usage:</b> Used by <see cref="ISyncEventPublisher"/> to store
/// event history when <see cref="SyncEventOptions.StoreInHistory"/> is true.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
public interface IEventStore
{
    /// <summary>
    /// Stores an event record in the event store.
    /// </summary>
    /// <param name="record">The event record to store.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// LOGIC: Persists the event record for later retrieval. Implementations
    /// should handle duplicate EventIds gracefully (idempotent operation).
    /// </remarks>
    Task StoreAsync(SyncEventRecord record, CancellationToken ct = default);

    /// <summary>
    /// Gets an event record by its ID.
    /// </summary>
    /// <param name="eventId">The unique event identifier.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// The event record if found, or null if not found.
    /// </returns>
    /// <remarks>
    /// LOGIC: Retrieves a specific event by its unique identifier.
    /// Returns null if the event has been removed due to retention policy.
    /// </remarks>
    Task<SyncEventRecord?> GetAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Queries event records with filters and pagination.
    /// </summary>
    /// <param name="query">The query criteria including filters, sorting, and pagination.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A list of matching event records, ordered and paginated per query.
    /// </returns>
    /// <remarks>
    /// LOGIC: Applies filters from the query in order:
    /// 1. EventType filter
    /// 2. DocumentId filter
    /// 3. PublishedAfter/PublishedBefore date range
    /// 4. SuccessfulOnly filter
    /// 5. Sorting per SortOrder
    /// 6. Pagination via PageSize and PageOffset
    /// </remarks>
    Task<IReadOnlyList<SyncEventRecord>> QueryAsync(
        SyncEventQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all event records for a specific document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="limit">Maximum number of records to return.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A list of event records for the document, ordered by published timestamp descending.
    /// </returns>
    /// <remarks>
    /// LOGIC: Convenience method for document-centric queries.
    /// Returns most recent events first.
    /// </remarks>
    Task<IReadOnlyList<SyncEventRecord>> GetByDocumentAsync(
        Guid documentId,
        int limit = 100,
        CancellationToken ct = default);
}
