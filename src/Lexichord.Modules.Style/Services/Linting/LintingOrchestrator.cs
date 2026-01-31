using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Abstractions.Contracts.Threading;
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
/// Threading (v0.2.7a):
/// - Subscribe/Unsubscribe are thread-safe via ConcurrentDictionary
/// - Scans are offloaded to background threads via Task.Run
/// - IThreadMarshaller ensures UI updates happen on the correct thread
/// - Results are published to the observable stream
///
/// Version: v0.2.7a
/// </remarks>
public sealed class LintingOrchestrator : ILintingOrchestrator
{
    private readonly ConcurrentDictionary<string, DocumentSubscription> _subscriptions = new();
    private readonly Subject<LintResult> _resultsSubject = new();
    private readonly SemaphoreSlim _scanSemaphore;
    private readonly IStyleEngine _styleEngine;
    private readonly IFuzzyScanner _fuzzyScanner;
    private readonly IMediator _mediator;
    private readonly IThreadMarshaller _threadMarshaller;
    private readonly IIgnorePatternService? _ignorePatternService;
    private readonly ILogger<LintingOrchestrator> _logger;
    private readonly LintingOptions _options;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LintingOrchestrator"/> class.
    /// </summary>
    public LintingOrchestrator(
        IStyleEngine styleEngine,
        IFuzzyScanner fuzzyScanner,
        IMediator mediator,
        IThreadMarshaller threadMarshaller,
        IOptions<LintingOptions> options,
        ILogger<LintingOrchestrator> logger,
        IIgnorePatternService? ignorePatternService = null)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _fuzzyScanner = fuzzyScanner ?? throw new ArgumentNullException(nameof(fuzzyScanner));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _threadMarshaller = threadMarshaller ?? throw new ArgumentNullException(nameof(threadMarshaller));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new LintingOptions();
        _ignorePatternService = ignorePatternService;

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

        // LOGIC: v0.3.6d - Check if file is ignored before subscribing
        if (_ignorePatternService?.IsIgnored(filePath) == true)
        {
            _logger.LogDebug("Skipping subscription for ignored file: {FilePath}", filePath);
            return System.Reactive.Disposables.Disposable.Empty;
        }

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
    /// <remarks>
    /// LOGIC: Wraps the core scan logic in Task.Run to ensure CPU-bound
    /// regex scanning doesn't block the UI thread. This is the primary
    /// async offloading point for the linting pipeline.
    ///
    /// Version: v0.2.7a
    /// </remarks>
    private async Task ExecuteScanAsync(
        string documentId,
        string content,
        CancellationToken cancellationToken)
    {
        if (!_subscriptions.TryGetValue(documentId, out var subscription))
            return;

        // LOGIC: Offload to background thread via Task.Run
        // This ensures CPU-intensive regex scanning doesn't block UI
        await Task.Run(async () =>
        {
            try
            {
                // LOGIC: Assert we're on a background thread (DEBUG only)
                _threadMarshaller.AssertBackgroundThread(nameof(ExecuteScanAsync));

                await ExecuteScanCoreAsync(documentId, content, subscription, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Background scan cancelled for document: {DocumentId}", documentId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background scan failed for document: {DocumentId}", documentId);
                throw;
            }
        }, cancellationToken);
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

                // LOGIC: Execute regex-based analysis
                var regexViolations = await _styleEngine.AnalyzeAsync(content, cancellationToken);

                // LOGIC: v0.3.1c - Extract regex-flagged words to prevent double-counting
                var regexFlaggedWords = new HashSet<string>(
                    regexViolations.Select(v => v.MatchedText.ToLowerInvariant()),
                    StringComparer.OrdinalIgnoreCase);

                // LOGIC: v0.3.1c - Execute fuzzy scanning after regex scan
                var fuzzyViolations = await _fuzzyScanner.ScanAsync(
                    content,
                    regexFlaggedWords,
                    cancellationToken);

                // LOGIC: v0.3.1c - Aggregate results from both scans
                var allViolations = regexViolations.Concat(fuzzyViolations).ToList();

                stopwatch.Stop();

                result = LintResult.Success(documentId, allViolations, stopwatch.Elapsed);

                // LOGIC: Update subscription state
                subscription.CompleteWith(allViolations, DateTimeOffset.UtcNow);

                _logger.LogDebug(
                    "Lint completed for {DocumentId}: {ViolationCount} violations ({RegexCount} regex, {FuzzyCount} fuzzy) in {Duration}ms",
                    documentId,
                    allViolations.Count,
                    regexViolations.Count,
                    fuzzyViolations.Count,
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
