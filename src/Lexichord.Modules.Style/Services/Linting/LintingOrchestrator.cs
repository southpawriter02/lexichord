using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Orchestrates reactive linting for open documents.
/// </summary>
/// <remarks>
/// LOGIC: Manages document subscriptions and coordinates debounced,
/// concurrent lint operations. Serves as the central hub for the
/// reactive linting pipeline.
///
/// Threading:
/// - Subscribe/Unsubscribe are thread-safe via ConcurrentDictionary
/// - Scans run on thread pool via IStyleEngine.AnalyzeAsync
/// - Results are published to the observable stream
///
/// Version: v0.2.3a
/// </remarks>
public sealed class LintingOrchestrator : ILintingOrchestrator
{
    private readonly ConcurrentDictionary<string, DocumentSubscription> _subscriptions = new();
    private readonly Subject<LintResult> _resultsSubject = new();
    private readonly SemaphoreSlim _scanSemaphore;
    private readonly IStyleEngine _styleEngine;
    private readonly IMediator _mediator;
    private readonly ILogger<LintingOrchestrator> _logger;
    private readonly LintingOptions _options;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LintingOrchestrator"/> class.
    /// </summary>
    public LintingOrchestrator(
        IStyleEngine styleEngine,
        IMediator mediator,
        IOptions<LintingOptions> options,
        ILogger<LintingOrchestrator> logger)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new LintingOptions();

        _scanSemaphore = new SemaphoreSlim(_options.MaxConcurrentScans);

        _logger.LogDebug(
            "LintingOrchestrator initialized with debounce={Debounce}ms, maxConcurrent={MaxConcurrent}",
            _options.DebounceMilliseconds,
            _options.MaxConcurrentScans);
    }

    /// <inheritdoc />
    public IObservable<LintResult> Results => _resultsSubject;

    /// <inheritdoc />
    public int ActiveDocumentCount => _subscriptions.Count;

    /// <inheritdoc />
    public IDisposable Subscribe(
        string documentId,
        string? filePath,
        IObservable<string> contentStream)
    {
        ArgumentNullException.ThrowIfNull(documentId);
        ArgumentNullException.ThrowIfNull(contentStream);

        if (_disposed)
            throw new ObjectDisposedException(nameof(LintingOrchestrator));

        // LOGIC: Remove existing subscription if present
        Unsubscribe(documentId);

        _logger.LogDebug("Subscribing document to linting pipeline: {DocumentId}", documentId);

        var subscription = new DocumentSubscription(
            documentId,
            filePath,
            contentStream,
            _options,
            (docId, content, ct) => _ = ExecuteScanAsync(docId, content, ct));

        _subscriptions[documentId] = subscription;

        // LOGIC: Return disposable that unsubscribes on disposal
        return Disposable.Create(() => Unsubscribe(documentId));
    }

    /// <inheritdoc />
    public void Unsubscribe(string documentId)
    {
        if (_subscriptions.TryRemove(documentId, out var subscription))
        {
            _logger.LogDebug("Unsubscribed document from linting pipeline: {DocumentId}", documentId);
            subscription.Dispose();
        }
    }

    /// <inheritdoc />
    public DocumentLintState? GetDocumentState(string documentId)
    {
        return _subscriptions.TryGetValue(documentId, out var subscription)
            ? subscription.State
            : null;
    }

    /// <inheritdoc />
    public async Task<LintResult> TriggerManualScanAsync(
        string documentId,
        string content,
        CancellationToken cancellationToken = default)
    {
        if (!_subscriptions.TryGetValue(documentId, out var subscription))
            throw new InvalidOperationException(
                $"Document '{documentId}' is not subscribed to the linting pipeline.");

        _logger.LogDebug("Manual scan triggered for document: {DocumentId}", documentId);

        // LOGIC: Cancel any in-flight debounced scan
        subscription.CancelCurrentScan();

        // LOGIC: Execute scan directly (bypasses debounce)
        return await ExecuteScanCoreAsync(documentId, content, subscription, cancellationToken);
    }

    /// <summary>
    /// Executes a scan operation after debounce.
    /// </summary>
    private async Task ExecuteScanAsync(
        string documentId,
        string content,
        CancellationToken cancellationToken)
    {
        if (!_subscriptions.TryGetValue(documentId, out var subscription))
            return;

        await ExecuteScanCoreAsync(documentId, content, subscription, cancellationToken);
    }

    /// <summary>
    /// Core scan execution logic shared by debounced and manual scans.
    /// </summary>
    private async Task<LintResult> ExecuteScanCoreAsync(
        string documentId,
        string content,
        DocumentSubscription subscription,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        LintResult result;

        try
        {
            // LOGIC: Respect concurrency limit
            await _scanSemaphore.WaitAsync(cancellationToken);

            try
            {
                // LOGIC: Publish start event
                await _mediator.Publish(
                    new LintingStartedEvent(documentId, DateTimeOffset.UtcNow),
                    cancellationToken);

                _logger.LogDebug("Starting lint scan for document: {DocumentId}", documentId);

                // LOGIC: Execute the actual analysis
                var violations = await _styleEngine.AnalyzeAsync(content, cancellationToken);

                stopwatch.Stop();

                result = LintResult.Success(documentId, violations, stopwatch.Elapsed);

                // LOGIC: Update subscription state
                subscription.CompleteWith(violations, DateTimeOffset.UtcNow);

                _logger.LogDebug(
                    "Lint completed for {DocumentId}: {ViolationCount} violations in {Duration}ms",
                    documentId,
                    violations.Count,
                    stopwatch.ElapsedMilliseconds);
            }
            finally
            {
                _scanSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            result = LintResult.Cancelled(documentId, stopwatch.Elapsed);
            subscription.MarkCancelled();

            _logger.LogDebug("Lint cancelled for document: {DocumentId}", documentId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result = LintResult.Failed(documentId, ex.Message, stopwatch.Elapsed);
            subscription.MarkCancelled();

            _logger.LogWarning(ex, "Lint failed for document: {DocumentId}", documentId);

            // LOGIC: Publish error event
            await _mediator.Publish(
                new LintingErrorEvent(documentId, ex.Message, ex),
                CancellationToken.None);
        }

        // LOGIC: Publish completion event and result
        await _mediator.Publish(
            new LintingCompletedEvent(result),
            CancellationToken.None);

        _resultsSubject.OnNext(result);

        return result;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogDebug("Disposing LintingOrchestrator with {Count} active subscriptions",
            _subscriptions.Count);

        // LOGIC: Dispose all subscriptions
        foreach (var kvp in _subscriptions)
        {
            kvp.Value.Dispose();
        }
        _subscriptions.Clear();

        // LOGIC: Complete the results stream
        _resultsSubject.OnCompleted();
        _resultsSubject.Dispose();

        _scanSemaphore.Dispose();
    }
}
