using System.Reactive.Disposables;
using System.Reactive.Linq;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Internal wrapper managing a single document's subscription to the linting pipeline.
/// </summary>
/// <remarks>
/// LOGIC: Encapsulates per-document reactive subscription lifecycle:
/// - Content stream subscription with debouncing
/// - State management
/// - Cancellation of in-flight scans on new content
/// - Cleanup on disposal
///
/// Version: v0.2.3a
/// </remarks>
internal sealed class DocumentSubscription : IDisposable
{
    private readonly string _documentId;
    private readonly string? _filePath;
    private readonly CompositeDisposable _disposables = new();
    private readonly object _stateLock = new();

    private CancellationTokenSource? _currentScanCts;
    private DocumentLintState _state;
    private bool _disposed;

    /// <summary>
    /// Initializes a new document subscription.
    /// </summary>
    /// <param name="documentId">Unique identifier for the document.</param>
    /// <param name="filePath">File path for logging/display.</param>
    /// <param name="contentStream">Observable stream of document content.</param>
    /// <param name="options">Linting configuration options.</param>
    /// <param name="onScanRequested">Callback when debounce completes and scan should start.</param>
    public DocumentSubscription(
        string documentId,
        string? filePath,
        IObservable<string> contentStream,
        LintingOptions options,
        Action<string, string, CancellationToken> onScanRequested)
    {
        _documentId = documentId;
        _filePath = filePath;
        _state = new DocumentLintState
        {
            DocumentId = documentId,
            FilePath = filePath
        };

        // LOGIC: Subscribe to content stream with debouncing
        var subscription = contentStream
            .Throttle(TimeSpan.FromMilliseconds(options.DebounceMilliseconds))
            .Subscribe(content =>
            {
                if (_disposed) return;

                // LOGIC: Cancel any in-flight scan
                CancelCurrentScan();

                // LOGIC: Update state to pending then analyzing
                lock (_stateLock)
                {
                    _state = _state.CreatePending().StartAnalyzing();
                }

                // LOGIC: Create new cancellation token for this scan
                _currentScanCts = new CancellationTokenSource();

                // LOGIC: Request scan (orchestrator will handle actual scanning)
                onScanRequested(documentId, content, _currentScanCts.Token);
            });

        _disposables.Add(subscription);
    }

    /// <summary>
    /// Gets the current state of this document.
    /// </summary>
    public DocumentLintState State
    {
        get
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Updates the state after a successful scan completion.
    /// </summary>
    /// <param name="violations">The violations found.</param>
    /// <param name="timestamp">When the scan completed.</param>
    public void CompleteWith(IReadOnlyList<StyleViolation> violations, DateTimeOffset timestamp)
    {
        lock (_stateLock)
        {
            _state = _state.CompleteWith(violations, timestamp);
        }
    }

    /// <summary>
    /// Updates the state after a cancelled scan.
    /// </summary>
    public void MarkCancelled()
    {
        lock (_stateLock)
        {
            _state = _state.CancelToIdle();
        }
    }

    /// <summary>
    /// Cancels any currently running scan.
    /// </summary>
    public void CancelCurrentScan()
    {
        _currentScanCts?.Cancel();
        _currentScanCts?.Dispose();
        _currentScanCts = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        CancelCurrentScan();
        _disposables.Dispose();
    }
}
