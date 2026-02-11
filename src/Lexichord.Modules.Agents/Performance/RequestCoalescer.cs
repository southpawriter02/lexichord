// -----------------------------------------------------------------------
// <copyright file="RequestCoalescer.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Channels;
using Lexichord.Abstractions.Contracts.LLM;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.Agents.Performance;

/// <summary>
/// Coalesces rapid sequential requests into batches to reduce API pressure.
/// Uses a sliding window approach to group related requests.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses a background processing loop that:
/// </para>
/// <list type="number">
///   <item><description>Accepts requests via <see cref="CoalesceAsync"/>.</description></item>
///   <item><description>Groups them into batches during the coalescing window.</description></item>
///   <item><description>Processes each batch sequentially via the inner <see cref="IChatCompletionService"/>.</description></item>
///   <item><description>Completes each request's <see cref="TaskCompletionSource{T}"/> with the response.</description></item>
/// </list>
/// <para>
/// The coalescing window duration is configured via <see cref="PerformanceOptions.CoalescingWindow"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.8c as part of Performance Optimization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var coalescer = serviceProvider.GetRequiredService&lt;IRequestCoalescer&gt;();
///
/// // Multiple rapid requests will be batched
/// var task1 = coalescer.CoalesceAsync(request1, ct);
/// var task2 = coalescer.CoalesceAsync(request2, ct);
///
/// var responses = await Task.WhenAll(task1, task2);
/// </code>
/// </example>
/// <seealso cref="IRequestCoalescer"/>
/// <seealso cref="PerformanceOptions"/>
public sealed class RequestCoalescer : IRequestCoalescer
{
    // LOGIC: The underlying chat completion service used to process batched requests.
    private readonly IChatCompletionService _inner;

    // LOGIC: Logger for batch processing diagnostics.
    private readonly ILogger<RequestCoalescer> _logger;

    // LOGIC: Resolved performance options including the coalescing window duration.
    private readonly PerformanceOptions _options;

    // LOGIC: Unbounded channel for queuing incoming coalesced requests.
    // Using a channel provides thread-safe producer-consumer semantics.
    private readonly Channel<CoalescedRequest> _channel;

    // LOGIC: Semaphore to synchronize access to the pending batch list.
    private readonly SemaphoreSlim _batchLock = new(1, 1);

    // LOGIC: The current batch of pending requests awaiting processing.
    private List<CoalescedRequest> _pendingBatch = new();

    // LOGIC: Timestamp of the first request in the current batch. Used to
    // determine when the coalescing window has expired.
    private DateTimeOffset _batchStartTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestCoalescer"/> class.
    /// </summary>
    /// <param name="inner">The chat completion service to delegate requests to.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="options">Performance configuration options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner"/>, <paramref name="logger"/>,
    /// or <paramref name="options"/> is null.
    /// </exception>
    public RequestCoalescer(
        IChatCompletionService inner,
        ILogger<RequestCoalescer> logger,
        IOptions<PerformanceOptions> options)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _channel = Channel.CreateUnbounded<CoalescedRequest>();

        _logger.LogDebug(
            "RequestCoalescer initialized with CoalescingWindow={Window}ms",
            _options.CoalescingWindow.TotalMilliseconds);

        // LOGIC: Start the background batch processing loop.
        // This runs for the lifetime of the singleton.
        _ = ProcessBatchesAsync();
    }

    /// <inheritdoc />
    public TimeSpan CoalescingWindow => _options.CoalescingWindow;

    /// <inheritdoc />
    public int PendingRequestCount => _pendingBatch.Count;

    /// <inheritdoc />
    /// <remarks>
    /// The request is added to the current batch and a <see cref="TaskCompletionSource{T}"/>
    /// is created to bridge the gap between the caller and the background processor.
    /// The returned task completes when the batch containing this request is processed.
    /// </remarks>
    public async Task<ChatResponse> CoalesceAsync(ChatRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        // LOGIC: Create a TCS to bridge between the caller and the background processor.
        var tcs = new TaskCompletionSource<ChatResponse>();
        var coalesced = new CoalescedRequest(request, tcs, ct);

        await _batchLock.WaitAsync(ct);
        try
        {
            // LOGIC: If this is the first request in a new batch, record the start time.
            if (_pendingBatch.Count == 0)
            {
                _batchStartTime = DateTimeOffset.UtcNow;
            }

            _pendingBatch.Add(coalesced);

            _logger.LogDebug(
                "Request added to batch. Pending: {Count}",
                _pendingBatch.Count);
        }
        finally
        {
            _batchLock.Release();
        }

        return await tcs.Task;
    }

    /// <summary>
    /// Background loop that processes batches after the coalescing window expires.
    /// </summary>
    /// <remarks>
    /// This loop runs continuously, checking every <see cref="CoalescingWindow"/>
    /// interval whether a batch is ready to be processed. A batch is ready when
    /// it contains requests and the coalescing window has elapsed since the first
    /// request was added.
    /// </remarks>
    private async Task ProcessBatchesAsync()
    {
        while (true)
        {
            // LOGIC: Wait for the coalescing window duration before checking for batches.
            await Task.Delay(_options.CoalescingWindow);

            List<CoalescedRequest>? batch = null;

            await _batchLock.WaitAsync();
            try
            {
                // LOGIC: Check if we have pending requests and the window has elapsed.
                if (_pendingBatch.Count > 0 &&
                    DateTimeOffset.UtcNow - _batchStartTime >= _options.CoalescingWindow)
                {
                    batch = _pendingBatch;
                    _pendingBatch = new List<CoalescedRequest>();
                }
            }
            finally
            {
                _batchLock.Release();
            }

            if (batch != null)
            {
                await ProcessBatchAsync(batch);
            }
        }
    }

    /// <summary>
    /// Processes a batch of coalesced requests sequentially.
    /// </summary>
    /// <param name="batch">The batch of requests to process.</param>
    /// <remarks>
    /// Each request in the batch is sent to the underlying <see cref="IChatCompletionService"/>
    /// individually. The <see cref="TaskCompletionSource{T}"/> for each request is completed
    /// with the response or faulted with the exception.
    /// </remarks>
    private async Task ProcessBatchAsync(List<CoalescedRequest> batch)
    {
        _logger.LogInformation(
            "Processing coalesced batch of {Count} requests",
            batch.Count);

        // LOGIC: Process each request sequentially. Future optimization could
        // batch requests if the API supports it.
        foreach (var request in batch)
        {
            try
            {
                var response = await _inner.CompleteAsync(
                    request.Request,
                    request.CancellationToken);
                request.TaskCompletionSource.SetResult(response);
            }
            catch (Exception ex)
            {
                request.TaskCompletionSource.SetException(ex);
            }
        }
    }

    /// <summary>
    /// Internal record representing a coalesced request with its completion source.
    /// </summary>
    /// <param name="Request">The original chat request.</param>
    /// <param name="TaskCompletionSource">The TCS to signal when processing completes.</param>
    /// <param name="CancellationToken">The cancellation token for the request.</param>
    private sealed record CoalescedRequest(
        ChatRequest Request,
        TaskCompletionSource<ChatResponse> TaskCompletionSource,
        CancellationToken CancellationToken);
}
