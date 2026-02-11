// -----------------------------------------------------------------------
// <copyright file="IRequestCoalescer.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.Agents.Performance;

/// <summary>
/// Coalesces rapid sequential requests to reduce API calls.
/// </summary>
/// <remarks>
/// <para>
/// The request coalescer batches requests that arrive within a configurable
/// time window (<see cref="CoalescingWindow"/>). This reduces API pressure
/// from rapid sequential queries (e.g., user typing corrections quickly).
/// </para>
/// <para>
/// Each request added during the coalescing window is queued, and all pending
/// requests are processed as a batch once the window expires.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8c as part of Performance Optimization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Submit a request — it may be coalesced with other rapid requests
/// var response = await coalescer.CoalesceAsync(request, cancellationToken);
///
/// // Check pending requests
/// Console.WriteLine($"Pending: {coalescer.PendingRequestCount}");
/// Console.WriteLine($"Window: {coalescer.CoalescingWindow.TotalMilliseconds}ms");
/// </code>
/// </example>
/// <seealso cref="RequestCoalescer"/>
/// <seealso cref="PerformanceOptions"/>
public interface IRequestCoalescer
{
    /// <summary>
    /// Coalesces a request with pending requests if within the coalescing window.
    /// </summary>
    /// <param name="request">The chat request to coalesce.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>
    /// A task that resolves to the <see cref="ChatResponse"/> once the
    /// coalesced batch is processed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via <paramref name="ct"/>.
    /// </exception>
    /// <remarks>
    /// The returned task completes only when the batch containing this request
    /// is processed by the underlying <see cref="IChatCompletionService"/>.
    /// </remarks>
    Task<ChatResponse> CoalesceAsync(ChatRequest request, CancellationToken ct);

    /// <summary>
    /// Gets the count of pending coalesced requests awaiting processing.
    /// </summary>
    /// <value>A non-negative integer representing queued requests in the current batch.</value>
    int PendingRequestCount { get; }

    /// <summary>
    /// Gets the coalescing time window.
    /// </summary>
    /// <value>The <see cref="TimeSpan"/> within which requests are coalesced.</value>
    TimeSpan CoalescingWindow { get; }
}
