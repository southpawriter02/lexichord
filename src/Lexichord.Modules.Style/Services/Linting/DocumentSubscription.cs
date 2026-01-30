using System.Reactive.Disposables;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Internal wrapper managing a single document's subscription to the linting pipeline.
/// </summary>
/// <remarks>
/// LOGIC: Encapsulates per-document reactive subscription lifecycle:
/// - Content stream subscription with debouncing via DebounceController
/// - State management delegated to the controller
/// - Cancellation of in-flight scans on new content
/// - Cleanup on disposal
///
/// Version: v0.2.3b
/// </remarks>
internal sealed class DocumentSubscription : IDisposable
{
    private readonly string _documentId;
    private readonly string? _filePath;
    private readonly CompositeDisposable _disposables = new();
    private readonly DebounceController _debounceController;
    private readonly object _stateLock = new();

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

        // LOGIC: Create debounce controller with callback wrapper
        _debounceController = new DebounceController(
            options.DebounceMilliseconds,
            (content, ct) =>
            {
                if (_disposed) return;

                // LOGIC: Update state to analyzing
                lock (_stateLock)
                {
                    _state = _state.CreatePending().StartAnalyzing();
                }

                // LOGIC: Forward to orchestrator
                onScanRequested(documentId, content, ct);
            });

        _disposables.Add(_debounceController);

        // LOGIC: Subscribe to content stream and forward to controller
        var subscription = contentStream.Subscribe(content =>
        {
            if (_disposed) return;
            _debounceController.RequestScan(content);
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
    /// Gets the current debounce state from the controller.
    /// </summary>
    public DebounceState DebounceState => _debounceController.CurrentState;

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
        _debounceController.MarkCompleted();
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
        _debounceController.MarkCancelled();
    }

    /// <summary>
    /// Cancels any currently running scan.
    /// </summary>
    public void CancelCurrentScan()
    {
        _debounceController.CancelCurrent();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _disposables.Dispose();
    }
}

