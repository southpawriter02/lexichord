// =============================================================================
// File: EventSortOrder.cs
// Project: Lexichord.Abstractions
// Description: Defines sort order options for sync event queries.
// =============================================================================
// LOGIC: Event sort order determines how query results are ordered when
//   retrieving event history via ISyncEventPublisher.GetEventsAsync.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Sort order options for sync event query results.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Defines how event records are sorted when querying event
/// history through <see cref="ISyncEventPublisher.GetEventsAsync"/>.
/// </para>
/// <para>
/// <b>Usage:</b> Set via <see cref="SyncEventQuery.SortOrder"/> when querying
/// event history.
/// </para>
/// <para>
/// <b>Default:</b> <see cref="ByPublishedDescending"/> returns the most recent
/// events first.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
public enum EventSortOrder
{
    /// <summary>
    /// Sort by published timestamp ascending (oldest first).
    /// </summary>
    /// <remarks>
    /// LOGIC: Chronological order for reviewing event sequence from start to end.
    /// </remarks>
    ByPublishedAscending = 0,

    /// <summary>
    /// Sort by published timestamp descending (newest first).
    /// </summary>
    /// <remarks>
    /// LOGIC: Default sort order. Shows most recent events first for quick
    /// review of recent activity.
    /// </remarks>
    ByPublishedDescending = 1,

    /// <summary>
    /// Sort by document ID ascending (alphabetical).
    /// </summary>
    /// <remarks>
    /// LOGIC: Groups events by document for reviewing document-specific history.
    /// </remarks>
    ByDocumentAscending = 2,

    /// <summary>
    /// Sort by document ID descending (reverse alphabetical).
    /// </summary>
    /// <remarks>
    /// LOGIC: Groups events by document in reverse order.
    /// </remarks>
    ByDocumentDescending = 3,

    /// <summary>
    /// Sort by event type name ascending (alphabetical).
    /// </summary>
    /// <remarks>
    /// LOGIC: Groups events by type for reviewing specific event categories.
    /// </remarks>
    ByEventTypeAscending = 4
}
