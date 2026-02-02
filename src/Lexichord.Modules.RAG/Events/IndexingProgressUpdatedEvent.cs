// =============================================================================
// File: IndexingProgressUpdatedEvent.cs
// Project: Lexichord.Modules.RAG
// Description: MediatR notification event for indexing progress updates.
// Version: v0.4.7c
// =============================================================================
// LOGIC: Event for real-time progress updates during indexing operations.
//   - Published by IndexManagementService during bulk re-index operations.
//   - Consumed by IndexingProgressViewModel to update the progress toast.
//   - Contains IndexingProgressInfo record with current state.
//   - Implements INotification for MediatR pub/sub pattern.
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using MediatR;

namespace Lexichord.Modules.RAG.Events;

/// <summary>
/// Published when indexing progress is updated during an operation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IndexingProgressUpdatedEvent"/> is a MediatR notification published
/// during indexing operations (particularly bulk re-index) to enable real-time UI updates.
/// The <see cref="IndexingProgressViewModel"/> subscribes to this event and updates its
/// state accordingly.
/// </para>
/// <para>
/// <b>Usage:</b> Handlers can subscribe to this event to:
/// </para>
/// <list type="bullet">
///   <item><description>Update progress toast UI with current document and percentage.</description></item>
///   <item><description>Log progress metrics for performance analysis.</description></item>
///   <item><description>Track indexing throughput and timing.</description></item>
///   <item><description>Detect completion or cancellation states.</description></item>
/// </list>
/// <para>
/// <b>Throttling:</b> While this event may be published frequently, the
/// <see cref="IndexingProgressViewModel"/> throttles UI updates to at most every 100ms
/// to prevent performance issues.
/// </para>
/// <para>
/// <b>MediatR Pattern:</b> This is a <see cref="INotification"/> (fire-and-forget),
/// not a request-response. Handlers do not return values.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7c as part of Indexing Progress.
/// </para>
/// </remarks>
/// <param name="Progress">
/// The current progress state including document name, counts, and completion status.
/// </param>
public record IndexingProgressUpdatedEvent(IndexingProgressInfo Progress) : INotification;
