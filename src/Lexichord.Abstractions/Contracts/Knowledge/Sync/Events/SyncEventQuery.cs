// =============================================================================
// File: SyncEventQuery.cs
// Project: Lexichord.Abstractions
// Description: Query criteria for filtering sync event history.
// =============================================================================
// LOGIC: SyncEventQuery provides filtering, sorting, and pagination options
//   for querying event history via ISyncEventPublisher.GetEventsAsync.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: EventSortOrder (v0.7.6j)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Query criteria for filtering sync event history.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Provides filtering, sorting, and pagination options for
/// querying event history through <see cref="ISyncEventPublisher.GetEventsAsync"/>.
/// </para>
/// <para>
/// <b>Filtering:</b> Multiple filter criteria are combined with AND logic.
/// Set only the filters you need; null values are ignored.
/// </para>
/// <para>
/// <b>Pagination:</b> Use <see cref="PageSize"/> and <see cref="PageOffset"/>
/// for paginated queries over large result sets.
/// </para>
/// <para>
/// <b>License Requirement:</b> History query access depends on license tier:
/// </para>
/// <list type="bullet">
///   <item>WriterPro: 7 days of history</item>
///   <item>Teams: 30 days of history</item>
///   <item>Enterprise: Unlimited history</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var query = new SyncEventQuery
/// {
///     DocumentId = myDocumentId,
///     EventType = "SyncCompletedEvent",
///     PublishedAfter = DateTimeOffset.UtcNow.AddDays(-7),
///     SortOrder = EventSortOrder.ByPublishedDescending,
///     PageSize = 50
/// };
/// var events = await publisher.GetEventsAsync(query);
/// </code>
/// </example>
public record SyncEventQuery
{
    /// <summary>
    /// Filter by event type name.
    /// </summary>
    /// <value>Event type name to filter by, or null for all types.</value>
    /// <remarks>
    /// LOGIC: Matches against <see cref="SyncEventRecord.EventType"/>.
    /// Example: "SyncCompletedEvent", "SyncFailedEvent".
    /// </remarks>
    public string? EventType { get; init; }

    /// <summary>
    /// Filter by document ID.
    /// </summary>
    /// <value>Document ID to filter by, or null for all documents.</value>
    /// <remarks>
    /// LOGIC: Returns only events for the specified document.
    /// Useful for document-centric event history.
    /// </remarks>
    public Guid? DocumentId { get; init; }

    /// <summary>
    /// Filter by published timestamp (after).
    /// </summary>
    /// <value>Minimum published timestamp, or null for no lower bound.</value>
    /// <remarks>
    /// LOGIC: Returns events published after this timestamp.
    /// Useful for incremental event polling.
    /// </remarks>
    public DateTimeOffset? PublishedAfter { get; init; }

    /// <summary>
    /// Filter by published timestamp (before).
    /// </summary>
    /// <value>Maximum published timestamp, or null for no upper bound.</value>
    /// <remarks>
    /// LOGIC: Returns events published before this timestamp.
    /// Useful for historical analysis.
    /// </remarks>
    public DateTimeOffset? PublishedBefore { get; init; }

    /// <summary>
    /// Filter by successful events only.
    /// </summary>
    /// <value>
    /// True to return only successful events,
    /// false to return only failed events,
    /// null to return all events.
    /// </value>
    /// <remarks>
    /// LOGIC: Matches against <see cref="SyncEventRecord.AllHandlersSucceeded"/>.
    /// Useful for troubleshooting failed events.
    /// </remarks>
    public bool? SuccessfulOnly { get; init; }

    /// <summary>
    /// Sort order for results.
    /// </summary>
    /// <value>How to sort the returned events.</value>
    /// <remarks>
    /// LOGIC: Determines result ordering.
    /// Default: <see cref="EventSortOrder.ByPublishedDescending"/> (newest first).
    /// </remarks>
    public EventSortOrder SortOrder { get; init; } = EventSortOrder.ByPublishedDescending;

    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    /// <value>Page size for pagination.</value>
    /// <remarks>
    /// LOGIC: Limits result set size for performance.
    /// Default: 100. Maximum: 1000.
    /// </remarks>
    public int PageSize { get; init; } = 100;

    /// <summary>
    /// Offset for pagination.
    /// </summary>
    /// <value>Number of results to skip.</value>
    /// <remarks>
    /// LOGIC: Enables pagination through large result sets.
    /// Use with <see cref="PageSize"/> for page-by-page retrieval.
    /// Default: 0 (start from first result).
    /// </remarks>
    public int PageOffset { get; init; } = 0;
}
