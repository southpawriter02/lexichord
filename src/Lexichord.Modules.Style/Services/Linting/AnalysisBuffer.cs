using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Lexichord.Abstractions.Contracts.Linting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Buffers and debounces analysis requests using System.Reactive.
/// </summary>
/// <remarks>
/// LOGIC: Implements per-document debouncing with latest-wins semantics.
/// Uses Rx operators to group requests by document and apply throttling:
///
/// 1. Incoming requests flow into a Subject
/// 2. GroupBy splits into per-document streams
/// 3. Each group applies Throttle for debouncing
/// 4. Merge combines back into single output stream
///
/// Key design decisions:
/// - Latest-wins: Throttle naturally discards intermediate values
/// - Per-document isolation: GroupBy ensures documents don't interfere
/// - Cancellation tracking: ConcurrentDictionary maps documentId to CTS
/// - Thread-safe: All operations are safe for concurrent access
///
/// Version: v0.3.7a
/// </remarks>
public sealed class AnalysisBuffer : IAnalysisBuffer
{
    private readonly Subject<AnalysisRequest> _inputSubject = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pendingCts = new();
    private readonly CompositeDisposable _disposables = new();
    private readonly ILogger<AnalysisBuffer> _logger;
    private readonly AnalysisBufferOptions _options;

    private readonly Subject<AnalysisRequest> _outputSubject = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalysisBuffer"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the buffer.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="logger"/> is null.
    /// </exception>
    public AnalysisBuffer(
        IOptions<AnalysisBufferOptions> options,
        ILogger<AnalysisBuffer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new AnalysisBufferOptions();

        _logger.LogDebug(
            "AnalysisBuffer initializing with IdlePeriod={IdlePeriodMs}ms, MaxDocuments={MaxDocs}, Enabled={Enabled}",
            _options.IdlePeriodMilliseconds,
            _options.MaxBufferedDocuments,
            _options.Enabled);

        // LOGIC: Build the reactive pipeline
        var subscription = BuildPipeline();
        _disposables.Add(subscription);
        _disposables.Add(_inputSubject);
        _disposables.Add(_outputSubject);
    }

    /// <inheritdoc />
    public IObservable<AnalysisRequest> Requests => _outputSubject.AsObservable();

    /// <inheritdoc />
    public int PendingCount => _pendingCts.Count;

    /// <inheritdoc />
    public void Submit(AnalysisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_disposed)
            throw new ObjectDisposedException(nameof(AnalysisBuffer));

        _logger.LogDebug(
            "Received analysis request for DocumentId={DocumentId}, FilePath={FilePath}, ContentLength={ContentLength}",
            request.DocumentId,
            request.FilePath ?? "(null)",
            request.Content.Length);

        // LOGIC: Cancel any previous pending request for this document
        CancelPendingRequest(request.DocumentId);

        // LOGIC: Create new CTS for this request
        var cts = CancellationTokenSource.CreateLinkedTokenSource(request.CancellationToken);
        _pendingCts[request.DocumentId] = cts;

        // LOGIC: Create request with our linked token
        var bufferedRequest = request.WithCancellationToken(cts.Token);

        // LOGIC: If buffering is disabled, emit immediately
        if (!_options.Enabled)
        {
            _logger.LogDebug(
                "Buffer disabled, emitting request immediately for DocumentId={DocumentId}",
                request.DocumentId);
            EmitRequest(bufferedRequest);
            return;
        }

        // LOGIC: Push through the reactive pipeline
        _inputSubject.OnNext(bufferedRequest);
    }

    /// <inheritdoc />
    public void Cancel(string documentId)
    {
        if (string.IsNullOrEmpty(documentId))
            return;

        _logger.LogDebug("Cancelling pending request for DocumentId={DocumentId}", documentId);
        CancelPendingRequest(documentId);
    }

    /// <inheritdoc />
    public void CancelAll()
    {
        _logger.LogDebug("Cancelling all {Count} pending requests", _pendingCts.Count);

        foreach (var docId in _pendingCts.Keys.ToList())
        {
            CancelPendingRequest(docId);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogDebug("Disposing AnalysisBuffer with {PendingCount} pending requests", PendingCount);

        // LOGIC: Cancel all pending requests
        CancelAll();

        // LOGIC: Complete and dispose subjects
        _inputSubject.OnCompleted();
        _outputSubject.OnCompleted();
        _disposables.Dispose();
    }

    /// <summary>
    /// Builds the reactive pipeline for debouncing requests.
    /// </summary>
    /// <returns>Subscription to dispose when shutting down.</returns>
    /// <remarks>
    /// LOGIC: Pipeline structure:
    /// 1. GroupBy(DocumentId) - separates into per-document streams
    /// 2. SelectMany with Throttle - debounces each document stream
    /// 3. Subscribe - emits debounced requests to output
    ///
    /// The Throttle operator implements latest-wins naturally:
    /// it discards all values except the last one in the window.
    /// </remarks>
    private IDisposable BuildPipeline()
    {
        var idlePeriod = TimeSpan.FromMilliseconds(_options.IdlePeriodMilliseconds);

        return _inputSubject
            .GroupBy(r => r.DocumentId)
            .SelectMany(group =>
                group.Throttle(idlePeriod))
            .Subscribe(
                OnDebounceComplete,
                OnPipelineError,
                OnPipelineComplete);
    }

    /// <summary>
    /// Called when a request has passed debounce and should be emitted.
    /// </summary>
    /// <param name="request">The debounced request.</param>
    private void OnDebounceComplete(AnalysisRequest request)
    {
        if (request.CancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Debounced request already cancelled, skipping DocumentId={DocumentId}",
                request.DocumentId);
            CleanupPendingRequest(request.DocumentId);
            return;
        }

        _logger.LogDebug(
            "Debounce complete for DocumentId={DocumentId}, emitting request (waited {IdlePeriod}ms)",
            request.DocumentId,
            _options.IdlePeriodMilliseconds);

        EmitRequest(request);
    }

    /// <summary>
    /// Emits a request to downstream subscribers.
    /// </summary>
    /// <param name="request">The request to emit.</param>
    private void EmitRequest(AnalysisRequest request)
    {
        // LOGIC: Remove from pending tracking (request is now in-flight)
        CleanupPendingRequest(request.DocumentId);

        // LOGIC: Emit to subscribers
        _outputSubject.OnNext(request);
    }

    /// <summary>
    /// Called when the pipeline encounters an error.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    private void OnPipelineError(Exception ex)
    {
        _logger.LogError(ex, "Analysis buffer pipeline error");
        _outputSubject.OnError(ex);
    }

    /// <summary>
    /// Called when the pipeline completes normally.
    /// </summary>
    private void OnPipelineComplete()
    {
        _logger.LogDebug("Analysis buffer pipeline completed");
    }

    /// <summary>
    /// Cancels and removes a pending request for a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    private void CancelPendingRequest(string documentId)
    {
        if (_pendingCts.TryRemove(documentId, out var cts))
        {
            _logger.LogDebug("Cancelling CTS for DocumentId={DocumentId}", documentId);

            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // LOGIC: CTS was already disposed, safe to ignore
            }
            finally
            {
                cts.Dispose();
            }
        }
    }

    /// <summary>
    /// Removes a pending request without cancelling (used when emitting).
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    private void CleanupPendingRequest(string documentId)
    {
        if (_pendingCts.TryRemove(documentId, out var cts))
        {
            // LOGIC: Don't cancel, just dispose - the request is being processed
            cts.Dispose();
        }
    }
}
