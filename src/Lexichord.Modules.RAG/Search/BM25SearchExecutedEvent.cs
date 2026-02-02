// =============================================================================
// File: BM25SearchExecutedEvent.cs
// Project: Lexichord.Modules.RAG
// Description: Telemetry event published after BM25 keyword search execution.
// =============================================================================
// LOGIC: MediatR notification for tracking BM25 search usage and performance.
//   Published by BM25SearchService after successful search completion.
//   Subscribers can use this for telemetry, analytics, and debugging.
//
//   Dependencies:
//     - v0.5.1b: BM25SearchService (publisher)
// =============================================================================

using MediatR;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Event published when a BM25 keyword search completes successfully.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BM25SearchExecutedEvent"/> provides telemetry data for BM25 search
/// operations, including query text, result count, and execution duration.
/// This event is published via <see cref="IMediator"/> for consumption by
/// analytics, logging, or monitoring subscribers.
/// </para>
/// <para>
/// <b>Use Cases:</b>
/// <list type="bullet">
///   <item><description>Search analytics and usage tracking.</description></item>
///   <item><description>Performance monitoring and alerting.</description></item>
///   <item><description>Debug logging for search pipeline troubleshooting.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.1b as part of the BM25 Search Implementation.
/// </para>
/// </remarks>
public sealed record BM25SearchExecutedEvent : INotification
{
    /// <summary>
    /// Gets the original query text submitted by the user.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Captures the raw query before preprocessing.</para>
    /// <para>Useful for search analytics and debugging query patterns.</para>
    /// </remarks>
    public required string Query { get; init; }

    /// <summary>
    /// Gets the number of search results returned.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Count of <see cref="Lexichord.Abstractions.Contracts.SearchHit"/> items
    /// in the result set after filtering by MinScore threshold.</para>
    /// <para>Zero indicates no matching chunks were found.</para>
    /// </remarks>
    public required int ResultCount { get; init; }

    /// <summary>
    /// Gets the total duration of the search operation.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Measured from query validation through result assembly.</para>
    /// <para>Includes query preprocessing, SQL execution, and result mapping.</para>
    /// <para>Target threshold: &lt; 100ms for typical queries on 10K chunks.</para>
    /// </remarks>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the search was executed.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Captured at search completion for time-series analysis.</para>
    /// </remarks>
    public required DateTimeOffset Timestamp { get; init; }
}
