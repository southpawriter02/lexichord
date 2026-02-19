// =============================================================================
// File: EventPriority.cs
// Project: Lexichord.Abstractions
// Description: Defines priority levels for sync event processing.
// =============================================================================
// LOGIC: Event priority determines processing order when events are batched
//   or queued. Higher priority events (Critical, High) may be processed before
//   lower priority events (Normal, Low) in batch scenarios.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Priority level for sync event processing.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Defines how urgently a sync event should be processed.
/// Higher priority events are processed before lower priority events when
/// events are batched or queued.
/// </para>
/// <para>
/// <b>Usage:</b> Set via <see cref="SyncEventOptions.Priority"/> when publishing
/// events through <see cref="ISyncEventPublisher.PublishAsync{TEvent}"/>.
/// </para>
/// <para>
/// <b>Default:</b> <see cref="Normal"/> is the default priority for most
/// sync operations.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
public enum EventPriority
{
    /// <summary>
    /// Low priority event that can be processed last.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for background maintenance events, optional notifications,
    /// and events that do not require immediate processing.
    /// </remarks>
    Low = 0,

    /// <summary>
    /// Normal priority event for standard processing.
    /// </summary>
    /// <remarks>
    /// LOGIC: Default priority for most sync events. Processed in FIFO order
    /// relative to other Normal priority events.
    /// </remarks>
    Normal = 1,

    /// <summary>
    /// High priority event that should be processed soon.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for events that affect user workflow, such as sync completion
    /// notifications that update UI state.
    /// </remarks>
    High = 2,

    /// <summary>
    /// Critical priority event requiring immediate processing.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for sync failures, conflict detection, and events that
    /// require immediate user attention or system response.
    /// </remarks>
    Critical = 3
}
