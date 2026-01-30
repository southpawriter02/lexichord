using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Lexichord.Abstractions.Contracts.Linting;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Controls debounce timing and state for document linting.
/// </summary>
/// <remarks>
/// LOGIC: Encapsulates the debounce state machine for a single document:
/// - Idle → content change → Waiting (timer starts)
/// - Waiting → timer expires → Scanning (callback invoked)
/// - Scanning → completion → Idle
/// - Any state → new content → cancel current, restart timer
///
/// This explicit state machine replaces inline Throttle for better
/// testability and monitoring.
///
/// Version: v0.2.3b
/// </remarks>
internal sealed class DebounceController : IDisposable
{
    private readonly Subject<string> _contentSubject = new();
    private readonly CompositeDisposable _disposables = new();
    private readonly Action<string, CancellationToken> _onScanRequested;
    private readonly object _stateLock = new();

    private CancellationTokenSource? _currentScanCts;
    private DebounceState _state = DebounceState.Idle;
    private bool _disposed;

    /// <summary>
    /// Initializes a new debounce controller.
    /// </summary>
    /// <param name="debounceMilliseconds">Delay before triggering scan.</param>
    /// <param name="onScanRequested">Callback when scan should execute.</param>
    public DebounceController(
        int debounceMilliseconds,
        Action<string, CancellationToken> onScanRequested)
    {
        ArgumentNullException.ThrowIfNull(onScanRequested);
        _onScanRequested = onScanRequested;

        // LOGIC: Subscribe to content with throttle
        var subscription = _contentSubject
            .Throttle(TimeSpan.FromMilliseconds(debounceMilliseconds))
            .Subscribe(OnThrottleElapsed);

        _disposables.Add(subscription);
    }

    /// <summary>
    /// Gets the current debounce state.
    /// </summary>
    public DebounceState CurrentState
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
    /// Requests a scan for the given content.
    /// </summary>
    /// <param name="content">The document content to scan.</param>
    /// <remarks>
    /// LOGIC: Immediately transitions to Waiting and queues content.
    /// If already waiting/scanning, cancels previous and restarts.
    /// </remarks>
    public void RequestScan(string content)
    {
        if (_disposed) return;

        // LOGIC: Cancel any current operation
        CancelCurrent();

        lock (_stateLock)
        {
            _state = DebounceState.Waiting;
        }

        // LOGIC: Push content through throttle pipeline
        _contentSubject.OnNext(content);
    }

    /// <summary>
    /// Cancels the current pending or in-flight scan.
    /// </summary>
    /// <remarks>
    /// LOGIC: Cancels CTS and transitions to Cancelled state.
    /// The Cancelled state is transient - next RequestScan moves to Waiting.
    /// </remarks>
    public void CancelCurrent()
    {
        lock (_stateLock)
        {
            if (_state is DebounceState.Waiting or DebounceState.Scanning)
            {
                _state = DebounceState.Cancelled;
            }
        }

        _currentScanCts?.Cancel();
        _currentScanCts?.Dispose();
        _currentScanCts = null;
    }

    /// <summary>
    /// Marks the current scan as completed.
    /// </summary>
    /// <remarks>
    /// LOGIC: Called by the orchestrator when scan finishes.
    /// Transitions from Scanning back to Idle.
    /// </remarks>
    public void MarkCompleted()
    {
        lock (_stateLock)
        {
            if (_state == DebounceState.Scanning)
            {
                _state = DebounceState.Idle;
            }
        }
    }

    /// <summary>
    /// Marks the current scan as cancelled.
    /// </summary>
    /// <remarks>
    /// LOGIC: Called when a scan is preemptively cancelled.
    /// </remarks>
    public void MarkCancelled()
    {
        lock (_stateLock)
        {
            _state = DebounceState.Cancelled;
        }
    }

    /// <summary>
    /// Called when throttle delay elapses.
    /// </summary>
    private void OnThrottleElapsed(string content)
    {
        if (_disposed) return;

        lock (_stateLock)
        {
            // LOGIC: Only proceed if still waiting
            if (_state != DebounceState.Waiting)
                return;

            _state = DebounceState.Scanning;
        }

        // LOGIC: Create new CTS for this scan
        _currentScanCts = new CancellationTokenSource();

        // LOGIC: Invoke callback to start scan
        _onScanRequested(content, _currentScanCts.Token);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        CancelCurrent();
        _contentSubject.OnCompleted();
        _contentSubject.Dispose();
        _disposables.Dispose();
    }
}
