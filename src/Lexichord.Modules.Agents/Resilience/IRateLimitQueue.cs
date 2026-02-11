// -----------------------------------------------------------------------
// <copyright file="IRateLimitQueue.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.Agents.Resilience;

/// <summary>
/// Queues chat requests during rate limiting with wait time estimation.
/// </summary>
/// <remarks>
/// <para>
/// When the LLM provider signals rate limiting (HTTP 429), the
/// <see cref="ResilientChatService"/> redirects the request to this queue
/// rather than failing immediately. The queue processes requests sequentially,
/// respecting the rate limit window before dispatching each request.
/// </para>
/// <para>
/// The queue provides:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="EstimatedWaitTime"/>: Real-time wait estimate for UI display</description></item>
///   <item><description><see cref="QueueDepth"/>: Current number of pending requests</description></item>
///   <item><description><see cref="StatusChanged"/>: Event for UI updates</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Enqueue a rate-limited request
/// var response = await _rateLimitQueue.EnqueueAsync(request, cancellationToken);
///
/// // Monitor queue status
/// _rateLimitQueue.StatusChanged += (s, args) =>
/// {
///     UpdateUI(args.QueueDepth, args.EstimatedWait);
/// };
/// </code>
/// </example>
/// <seealso cref="RateLimitQueue"/>
/// <seealso cref="RateLimitStatusEventArgs"/>
/// <seealso cref="AgentRateLimitException"/>
public interface IRateLimitQueue
{
    /// <summary>
    /// Enqueues a chat request, waiting for the rate limit window to expire before sending.
    /// </summary>
    /// <param name="request">The chat request to queue.</param>
    /// <param name="ct">
    /// Cancellation token. If cancelled while waiting, the queued request is removed
    /// and a <see cref="OperationCanceledException"/> is thrown.
    /// </param>
    /// <returns>
    /// A task that completes with the <see cref="ChatResponse"/> when the request
    /// has been sent and the provider has responded.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the <paramref name="ct"/> is cancelled while the request is queued.
    /// </exception>
    /// <remarks>
    /// LOGIC: The request is placed in a bounded channel and processed by a background
    /// loop that respects the rate limit timing. If the provider rate-limits again
    /// during processing, the request is re-queued with an updated wait time.
    /// </remarks>
    Task<ChatResponse> EnqueueAsync(ChatRequest request, CancellationToken ct);

    /// <summary>
    /// Gets the estimated wait time until the next request can be sent.
    /// </summary>
    /// <value>
    /// The estimated wait duration. <see cref="TimeSpan.Zero"/> when not rate-limited.
    /// </value>
    /// <remarks>
    /// LOGIC: Computed as the difference between the rate-limit-until timestamp
    /// and the current UTC time. Returns <see cref="TimeSpan.Zero"/> if the
    /// rate limit has expired.
    /// </remarks>
    TimeSpan EstimatedWaitTime { get; }

    /// <summary>
    /// Gets the current number of requests waiting in the queue.
    /// </summary>
    /// <value>
    /// A non-negative integer representing the pending request count.
    /// </value>
    int QueueDepth { get; }

    /// <summary>
    /// Raised when the rate limit status changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event fires when:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>A new request is enqueued</description></item>
    ///   <item><description>A queued request completes processing</description></item>
    ///   <item><description>The rate limit window expires</description></item>
    ///   <item><description>A new rate limit is imposed by the provider</description></item>
    /// </list>
    /// <para>
    /// Event handlers should be lightweight and non-blocking to avoid
    /// impacting queue processing throughput.
    /// </para>
    /// </remarks>
    event EventHandler<RateLimitStatusEventArgs>? StatusChanged;
}
