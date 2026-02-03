// =============================================================================
// File: ContextExpandedEvent.cs
// Project: Lexichord.Modules.RAG
// Description: MediatR event published when context expansion completes.
// =============================================================================
// LOGIC: Published by ContextExpansionService after successful context
//   expansion. Used for telemetry, analytics, and debugging. Includes timing
//   information and cache status.
// =============================================================================
// VERSION: v0.5.3a (Context Expansion Service)
// =============================================================================

using MediatR;

namespace Lexichord.Modules.RAG.Events;

/// <summary>
/// Published when context expansion completes successfully.
/// </summary>
/// <param name="ChunkId">The ID of the expanded chunk.</param>
/// <param name="DocumentId">The document containing the chunk.</param>
/// <param name="BeforeCount">Number of preceding chunks retrieved.</param>
/// <param name="AfterCount">Number of following chunks retrieved.</param>
/// <param name="HasBreadcrumb">Whether heading breadcrumb was resolved.</param>
/// <param name="ElapsedMilliseconds">Time taken for expansion (0 if from cache).</param>
/// <param name="FromCache">Whether the result came from cache.</param>
/// <remarks>
/// <para>
/// <see cref="ContextExpandedEvent"/> is published by
/// <see cref="Services.ContextExpansionService"/> after each successful
/// context expansion operation. Consumers can use this event for:
/// <list type="bullet">
///   <item><description>Tracking expansion frequency and cache efficiency.</description></item>
///   <item><description>Monitoring query performance and latency.</description></item>
///   <item><description>Building analytics dashboards for RAG usage.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3a as part of The Context Window feature.
/// </para>
/// </remarks>
public record ContextExpandedEvent(
    Guid ChunkId,
    Guid DocumentId,
    int BeforeCount,
    int AfterCount,
    bool HasBreadcrumb,
    long ElapsedMilliseconds,
    bool FromCache) : INotification;
