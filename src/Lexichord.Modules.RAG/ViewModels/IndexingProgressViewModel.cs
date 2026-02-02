// =============================================================================
// File: IndexingProgressViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel for the indexing progress toast overlay.
// Version: v0.4.7c
// =============================================================================
// LOGIC: Manages state for progress toast with throttled UI updates.
//   - Show() initializes progress display with operation details.
//   - Subscribes to IndexingProgressUpdatedEvent via MediatR.
//   - Throttles UI updates to max every 100ms to prevent lag.
//   - CancelCommand triggers CancellationTokenSource from caller.
//   - Auto-dismiss after completion (3s success, 2s cancel).
//   - Implements IDisposable for timer + subscription cleanup.
// =============================================================================

using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel for the indexing progress toast overlay.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexingProgressViewModel"/> manages the display state for a toast-style
/// progress overlay shown during indexing operations. It provides real-time updates
/// for document name, progress percentage, and elapsed time.
/// </para>
/// <para>
/// <b>Features:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Throttled UI updates (max every 100ms) to prevent performance issues.</description></item>
///   <item><description>Cancellation support via <see cref="CancelCommand"/>.</description></item>
///   <item><description>Auto-dismiss after completion (3 seconds) or cancellation (2 seconds).</description></item>
///   <item><description>Elapsed time display formatted as "Xs", "Xm Ys", or "Xh Ym".</description></item>
/// </list>
/// <para>
/// <b>Dependencies:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="IMediator"/> - Subscribes to <see cref="IndexingProgressUpdatedEvent"/>.</description></item>
///   <item><description><see cref="ILogger{T}"/> - Diagnostic logging.</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.4.7c as part of Indexing Progress.
/// </para>
/// </remarks>
public partial class IndexingProgressViewModel : ObservableObject, INotificationHandler<IndexingProgressUpdatedEvent>, IDisposable
{
    private readonly ILogger<IndexingProgressViewModel> _logger;
    private readonly Stopwatch _stopwatch = new();
    private readonly object _updateLock = new();

    private CancellationTokenSource? _cancellationTokenSource;
    private System.Timers.Timer? _elapsedTimer;
    private System.Timers.Timer? _autoDismissTimer;
    private DateTimeOffset _lastUiUpdate = DateTimeOffset.MinValue;
    private bool _disposed;

    /// <summary>
    /// Minimum interval between UI updates to prevent performance issues.
    /// </summary>
    private static readonly TimeSpan UpdateThrottleInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Delay before auto-dismiss after successful completion.
    /// </summary>
    private static readonly TimeSpan SuccessDismissDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Delay before auto-dismiss after cancellation.
    /// </summary>
    private static readonly TimeSpan CancelledDismissDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Initializes a new instance of <see cref="IndexingProgressViewModel"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    public IndexingProgressViewModel(ILogger<IndexingProgressViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogDebug("[IndexingProgressViewModel] Initialized");
    }

    #region Observable Properties

    /// <summary>
    /// Gets or sets whether the progress toast is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isVisible;

    /// <summary>
    /// Gets or sets the operation title (e.g., "Re-indexing all documents...").
    /// </summary>
    [ObservableProperty]
    private string _operationTitle = string.Empty;

    /// <summary>
    /// Gets or sets the current document being processed.
    /// </summary>
    [ObservableProperty]
    private string _currentDocument = string.Empty;

    /// <summary>
    /// Gets or sets the number of documents processed so far.
    /// </summary>
    [ObservableProperty]
    private int _processedCount;

    /// <summary>
    /// Gets or sets the total number of documents to process.
    /// </summary>
    [ObservableProperty]
    private int _totalCount;

    /// <summary>
    /// Gets or sets the completion percentage (0-100).
    /// </summary>
    [ObservableProperty]
    private int _percentComplete;

    /// <summary>
    /// Gets or sets whether the operation has completed.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isComplete;

    /// <summary>
    /// Gets or sets whether the operation was cancelled.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _wasCancelled;

    /// <summary>
    /// Gets or sets the formatted elapsed time display.
    /// </summary>
    [ObservableProperty]
    private string _elapsedTimeDisplay = "0s";

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the formatted progress text (e.g., "5 / 10").
    /// </summary>
    public string ProgressText => $"{ProcessedCount} / {TotalCount}";

    /// <summary>
    /// Gets whether the cancel operation is available.
    /// </summary>
    public bool CanCancel => IsVisible && !IsComplete && !WasCancelled;

    #endregion

    #region Public Methods

    /// <summary>
    /// Shows the progress toast and initializes tracking state.
    /// </summary>
    /// <param name="title">The operation title to display.</param>
    /// <param name="totalDocuments">Total number of documents to process.</param>
    /// <param name="cancellationTokenSource">
    /// The cancellation token source to trigger on cancel command.
    /// </param>
    public void Show(string title, int totalDocuments, CancellationTokenSource cancellationTokenSource)
    {
        _logger.LogInformation(
            "[IndexingProgressViewModel] Starting progress display: {Title}, {Total} documents",
            title,
            totalDocuments);

        _cancellationTokenSource = cancellationTokenSource;

        // Reset state
        OperationTitle = title;
        CurrentDocument = string.Empty;
        ProcessedCount = 0;
        TotalCount = totalDocuments;
        PercentComplete = 0;
        IsComplete = false;
        WasCancelled = false;

        // Start elapsed time tracking
        _stopwatch.Restart();
        UpdateElapsedTimeDisplay();
        StartElapsedTimer();

        // Show the toast
        IsVisible = true;
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(CanCancel));
    }

    /// <summary>
    /// Handles the progress updated event from MediatR.
    /// </summary>
    /// <param name="notification">The progress update event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task Handle(IndexingProgressUpdatedEvent notification, CancellationToken cancellationToken)
    {
        if (!IsVisible)
        {
            return Task.CompletedTask;
        }

        var info = notification.Progress;

        // Throttle UI updates
        lock (_updateLock)
        {
            var now = DateTimeOffset.UtcNow;
            var sinceLastUpdate = now - _lastUiUpdate;

            // Always update on completion, throttle otherwise
            if (!info.IsComplete && sinceLastUpdate < UpdateThrottleInterval)
            {
                return Task.CompletedTask;
            }

            _lastUiUpdate = now;
        }

        // Marshal to UI thread
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (info.CurrentDocument != null)
            {
                CurrentDocument = Path.GetFileName(info.CurrentDocument);
            }

            ProcessedCount = info.ProcessedCount;
            PercentComplete = info.PercentComplete;
            OnPropertyChanged(nameof(ProgressText));

            if (info.IsComplete || info.WasCancelled)
            {
                CompleteOperation(info.WasCancelled);
            }
        });

        return Task.CompletedTask;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Cancels the current indexing operation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _logger.LogInformation("[IndexingProgressViewModel] Cancel requested by user");

        _cancellationTokenSource?.Cancel();
        WasCancelled = true;
        OnPropertyChanged(nameof(CanCancel));
    }

    /// <summary>
    /// Dismisses the progress toast.
    /// </summary>
    [RelayCommand]
    private void Dismiss()
    {
        _logger.LogDebug("[IndexingProgressViewModel] Dismissed by user");
        Hide();
    }

    #endregion

    #region Private Methods

    private void CompleteOperation(bool wasCancelled)
    {
        _stopwatch.Stop();
        StopElapsedTimer();

        IsComplete = true;
        WasCancelled = wasCancelled;
        PercentComplete = wasCancelled ? PercentComplete : 100;

        OnPropertyChanged(nameof(CanCancel));

        _logger.LogInformation(
            "[IndexingProgressViewModel] Operation completed: {Processed}/{Total}, Cancelled={Cancelled}, Elapsed={Elapsed}",
            ProcessedCount,
            TotalCount,
            wasCancelled,
            _stopwatch.Elapsed);

        // Start auto-dismiss timer
        var delay = wasCancelled ? CancelledDismissDelay : SuccessDismissDelay;
        StartAutoDismissTimer(delay);
    }

    private void Hide()
    {
        IsVisible = false;
        StopElapsedTimer();
        StopAutoDismissTimer();
        _stopwatch.Reset();
    }

    private void StartElapsedTimer()
    {
        StopElapsedTimer();

        _elapsedTimer = new System.Timers.Timer(1000);
        _elapsedTimer.Elapsed += (_, _) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(UpdateElapsedTimeDisplay);
        };
        _elapsedTimer.Start();
    }

    private void StopElapsedTimer()
    {
        _elapsedTimer?.Stop();
        _elapsedTimer?.Dispose();
        _elapsedTimer = null;
    }

    private void StartAutoDismissTimer(TimeSpan delay)
    {
        StopAutoDismissTimer();

        _autoDismissTimer = new System.Timers.Timer(delay.TotalMilliseconds);
        _autoDismissTimer.AutoReset = false;
        _autoDismissTimer.Elapsed += (_, _) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(Hide);
        };
        _autoDismissTimer.Start();
    }

    private void StopAutoDismissTimer()
    {
        _autoDismissTimer?.Stop();
        _autoDismissTimer?.Dispose();
        _autoDismissTimer = null;
    }

    private void UpdateElapsedTimeDisplay()
    {
        ElapsedTimeDisplay = FormatElapsedTime(_stopwatch.Elapsed);
    }

    /// <summary>
    /// Formats elapsed time for display.
    /// </summary>
    /// <param name="elapsed">The elapsed time to format.</param>
    /// <returns>Formatted string like "30s", "1m 30s", or "1h 5m".</returns>
    internal static string FormatElapsedTime(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
        {
            return $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m";
        }

        if (elapsed.TotalMinutes >= 1)
        {
            return $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds}s";
        }

        return $"{(int)elapsed.TotalSeconds}s";
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes of timers and resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopElapsedTimer();
        StopAutoDismissTimer();
        _stopwatch.Stop();

        GC.SuppressFinalize(this);
    }

    #endregion
}
