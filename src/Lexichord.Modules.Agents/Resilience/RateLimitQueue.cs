// -----------------------------------------------------------------------
// <copyright file="RateLimitQueue.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Lexichord.Modules.Agents.Resilience;

/// <summary>
/// Queues chat requests during rate limiting with wait time estimation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RateLimitQueue"/> implements a bounded-channel-based FIFO queue
/// that holds requests while the provider's rate limit window is active. A background
/// processing loop drains the queue, respecting the rate limit timing.
/// </para>
/// <para>
/// <b>Architecture:</b>
/// </para>
/// <code>
/// Caller → EnqueueAsync() → Channel → ProcessQueueAsync() → IChatCompletionService
///              ↓                             ↓
///        TaskCompletionSource ←──── SetResult / SetException
/// </code>
/// <para>
/// <b>Capacity:</b> The channel is bounded to 100 entries. When full, callers
/// block in <see cref="EnqueueAsync"/> until space is available.
/// </para>
/// <para>
/// <b>Thread Safety:</b> The channel provides thread-safe read/write operations.
/// The <see cref="_rateLimitUntil"/> field is accessed atomically.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <seealso cref="IRateLimitQueue"/>
/// <seealso cref="ResilientChatService"/>
/// <seealso cref="AgentRateLimitException"/>
public sealed class RateLimitQueue : IRateLimitQueue
{
    private readonly IChatCompletionService _inner;
    private readonly ILogger<RateLimitQueue> _logger;
    private readonly Channel<QueuedRequest> _queue;

    /// <summary>
    /// UTC timestamp after which the rate limit expires.
    /// </summary>
    /// <remarks>
    /// LOGIC: Initialized to <see cref="DateTimeOffset.MinValue"/> to indicate
    /// no rate limit is active. Updated via <see cref="SetRateLimit"/> when a
    /// rate limit response is received.
    /// </remarks>
    private DateTimeOffset _rateLimitUntil = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitQueue"/> class.
    /// </summary>
    /// <param name="inner">
    /// The underlying chat completion service to dispatch requests to once the
    /// rate limit window has expired.
    /// </param>
    /// <param name="logger">Logger for queue operation diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner"/> or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// LOGIC: The constructor starts a background processing loop that drains the channel.
    /// The loop runs for the lifetime of this instance. The bounded channel capacity of
    /// 100 prevents unbounded memory growth under sustained rate limiting.
    /// </remarks>
    public RateLimitQueue(
        IChatCompletionService inner,
        ILogger<RateLimitQueue> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _queue = Channel.CreateBounded<QueuedRequest>(new BoundedChannelOptions(100)
        {
            // LOGIC: Wait mode means callers block when the queue is full,
            // providing natural backpressure without dropping requests.
            FullMode = BoundedChannelFullMode.Wait
        });

        // LOGIC: Start the background processing loop. The fire-and-forget pattern
        // is appropriate here because the loop runs for the lifetime of the service
        // and any exceptions are caught and logged internally.
        _ = ProcessQueueAsync();

        _logger.LogDebug("RateLimitQueue initialized with capacity 100");
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Computed dynamically from <see cref="_rateLimitUntil"/> and the current
    /// UTC time. Returns <see cref="TimeSpan.Zero"/> when no rate limit is active,
    /// ensuring the UI never shows negative wait times.
    /// </remarks>
    public TimeSpan EstimatedWaitTime =>
        _rateLimitUntil > DateTimeOffset.UtcNow
            ? _rateLimitUntil - DateTimeOffset.UtcNow
            : TimeSpan.Zero;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Delegates to the channel reader's count, which provides a thread-safe
    /// approximate count of pending items.
    /// </remarks>
    public int QueueDepth => _queue.Reader.Count;

    /// <inheritdoc />
    public event EventHandler<RateLimitStatusEventArgs>? StatusChanged;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Uses a <see cref="TaskCompletionSource{T}"/> to bridge between the
    /// synchronous channel write and the asynchronous caller. The TCS is completed
    /// by the background processing loop when the request is eventually dispatched.
    /// </remarks>
    public async Task<ChatResponse> EnqueueAsync(ChatRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var tcs = new TaskCompletionSource<ChatResponse>();
        var queued = new QueuedRequest(request, tcs, ct);

        await _queue.Writer.WriteAsync(queued, ct);

        _logger.LogInformation(
            "Request queued. Position: {Pos}, ETA: {Wait}s",
            QueueDepth,
            EstimatedWaitTime.TotalSeconds);

        RaiseStatusChanged();

        // LOGIC: Await the TCS, which will be completed by the processing loop.
        // If the caller's cancellation token fires, we register it to cancel
        // the TCS so the caller isn't left waiting indefinitely.
        using var registration = ct.Register(() => tcs.TrySetCanceled(ct));
        return await tcs.Task;
    }

    /// <summary>
    /// Updates the rate limit expiry timestamp.
    /// </summary>
    /// <param name="duration">The duration of the rate limit window.</param>
    /// <remarks>
    /// LOGIC: Called when a rate limit response is received from the provider.
    /// The processing loop checks <see cref="EstimatedWaitTime"/> before each
    /// dispatch to respect the updated window.
    /// </remarks>
    public void SetRateLimit(TimeSpan duration)
    {
        _rateLimitUntil = DateTimeOffset.UtcNow + duration;

        _logger.LogInformation(
            "Rate limit set. Expires at {ExpiresAt:O} (in {Duration:F1}s)",
            _rateLimitUntil,
            duration.TotalSeconds);

        RaiseStatusChanged();
    }

    /// <summary>
    /// Background loop that processes queued requests sequentially.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: This loop runs continuously, reading from the channel and dispatching
    /// each request to the inner <see cref="IChatCompletionService"/>. It respects
    /// the rate limit window by delaying before each dispatch.
    /// </para>
    /// <para>
    /// <b>Error Handling:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Rate limit errors cause re-queueing with updated timing</description></item>
    ///   <item><description>Other errors are propagated to the caller via the TCS</description></item>
    ///   <item><description>Cancellation is handled gracefully via the TCS</description></item>
    /// </list>
    /// </remarks>
    private async Task ProcessQueueAsync()
    {
        _logger.LogDebug("Rate limit queue processing loop started");

        await foreach (var queued in _queue.Reader.ReadAllAsync())
        {
            // LOGIC: Check if the caller has already cancelled before waiting.
            if (queued.CancellationToken.IsCancellationRequested)
            {
                queued.TaskCompletionSource.TrySetCanceled(queued.CancellationToken);
                RaiseStatusChanged();
                continue;
            }

            // LOGIC: Wait for the rate limit window to expire before dispatching.
            var wait = EstimatedWaitTime;
            if (wait > TimeSpan.Zero)
            {
                _logger.LogDebug("Waiting {Wait}ms for rate limit to expire", wait.TotalMilliseconds);

                try
                {
                    await Task.Delay(wait, queued.CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    queued.TaskCompletionSource.TrySetCanceled(queued.CancellationToken);
                    RaiseStatusChanged();
                    continue;
                }
            }

            try
            {
                _logger.LogDebug("Dispatching queued request to inner service");
                var response = await _inner.CompleteAsync(queued.Request, queued.CancellationToken);
                queued.TaskCompletionSource.SetResult(response);
            }
            catch (Lexichord.Abstractions.Contracts.LLM.RateLimitException rl)
            {
                // LOGIC: The provider rate-limited us again. Update the window and
                // re-queue the request so it gets retried after the new window expires.
                _logger.LogWarning(
                    "Re-rate-limited during queue processing. New wait: {Wait}s",
                    rl.RetryAfter?.TotalSeconds ?? 30);

                SetRateLimit(rl.RetryAfter ?? TimeSpan.FromSeconds(30));
                await _queue.Writer.WriteAsync(queued, queued.CancellationToken);
            }
            catch (OperationCanceledException oce)
            {
                queued.TaskCompletionSource.TrySetCanceled(oce.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queued request");
                queued.TaskCompletionSource.SetException(ex);
            }

            RaiseStatusChanged();
        }

        _logger.LogDebug("Rate limit queue processing loop exited");
    }

    /// <summary>
    /// Raises the <see cref="StatusChanged"/> event with current queue state.
    /// </summary>
    /// <remarks>
    /// LOGIC: Event is raised on each state change (enqueue, dequeue, rate limit update)
    /// to keep UI subscribers in sync. Uses null-conditional to avoid NRE when
    /// no handlers are subscribed.
    /// </remarks>
    private void RaiseStatusChanged()
    {
        StatusChanged?.Invoke(this, new RateLimitStatusEventArgs(
            IsRateLimited: EstimatedWaitTime > TimeSpan.Zero,
            EstimatedWait: EstimatedWaitTime,
            QueueDepth: QueueDepth));
    }

    /// <summary>
    /// Internal record for channel entries that bridges the caller to the processing loop.
    /// </summary>
    /// <param name="Request">The chat request to dispatch.</param>
    /// <param name="TaskCompletionSource">The TCS that the processing loop completes.</param>
    /// <param name="CancellationToken">The caller's cancellation token.</param>
    /// <remarks>
    /// LOGIC: The TCS is the key bridging mechanism: the caller awaits it in
    /// <see cref="EnqueueAsync"/>, and the processing loop sets its result in
    /// <see cref="ProcessQueueAsync"/>.
    /// </remarks>
    private sealed record QueuedRequest(
        ChatRequest Request,
        TaskCompletionSource<ChatResponse> TaskCompletionSource,
        CancellationToken CancellationToken);
}
