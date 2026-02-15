// -----------------------------------------------------------------------
// <copyright file="BatchProgressViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Abstractions.Contracts;

using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// ViewModel for the batch simplification progress dialog.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This ViewModel provides real-time feedback during batch
/// simplification operations by:
/// </para>
/// <list type="bullet">
///   <item><description>Displaying overall progress percentage and bar</description></item>
///   <item><description>Showing current paragraph being processed</description></item>
///   <item><description>Tracking simplified vs. skipped paragraph counts</description></item>
///   <item><description>Estimating remaining time based on average processing</description></item>
///   <item><description>Displaying recent activity log</description></item>
///   <item><description>Supporting cancellation via cancel command</description></item>
/// </list>
/// <para>
/// <b>Lifecycle:</b>
/// <list type="number">
///   <item><description>Create instance via DI (transient)</description></item>
///   <item><description>Call <see cref="StartAsync"/> to begin the batch operation</description></item>
///   <item><description>Subscribe to <see cref="Completed"/> event for completion</description></item>
///   <item><description>Handle completion or cancellation</description></item>
/// </list>
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <example>
/// <code>
/// var viewModel = serviceProvider.GetRequiredService&lt;BatchProgressViewModel&gt;();
///
/// viewModel.Completed += async (s, e) =>
/// {
///     if (e.WasCancelled)
///     {
///         ShowNotification("Batch simplification was cancelled.");
///     }
///     else
///     {
///         ShowCompletionDialog(e.Result);
///     }
/// };
///
/// await viewModel.StartAsync(documentPath, target, options);
/// </code>
/// </example>
/// <seealso cref="BatchCompletionViewModel"/>
/// <seealso cref="IBatchSimplificationService"/>
/// <seealso cref="BatchSimplificationProgress"/>
public sealed partial class BatchProgressViewModel : DisposableViewModel
{
    #region Constants

    /// <summary>
    /// Maximum number of recent activity items to display.
    /// </summary>
    private const int MaxActivityItems = 10;

    #endregion

    #region Fields

    private readonly IBatchSimplificationService _batchService;
    private readonly ILogger<BatchProgressViewModel> _logger;
    private CancellationTokenSource? _cancellationTokenSource;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchProgressViewModel"/> class.
    /// </summary>
    /// <param name="batchService">The batch simplification service.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public BatchProgressViewModel(
        IBatchSimplificationService batchService,
        ILogger<BatchProgressViewModel> logger)
    {
        _batchService = batchService ?? throw new ArgumentNullException(nameof(batchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RecentActivity = new ObservableCollection<ActivityItem>();
    }

    #endregion

    #region Observable Properties

    /// <summary>
    /// Gets the dialog title.
    /// </summary>
    [ObservableProperty]
    private string _title = "Simplifying Document";

    /// <summary>
    /// Gets or sets the current progress percentage (0-100).
    /// </summary>
    [ObservableProperty]
    private double _progressPercent;

    /// <summary>
    /// Gets or sets the current paragraph index (1-based).
    /// </summary>
    [ObservableProperty]
    private int _currentParagraph;

    /// <summary>
    /// Gets or sets the total paragraph count.
    /// </summary>
    [ObservableProperty]
    private int _totalParagraphs;

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "Initializing...";

    /// <summary>
    /// Gets or sets the current paragraph preview text.
    /// </summary>
    [ObservableProperty]
    private string? _currentParagraphPreview;

    /// <summary>
    /// Gets or sets the number of simplified paragraphs.
    /// </summary>
    [ObservableProperty]
    private int _simplifiedCount;

    /// <summary>
    /// Gets or sets the number of skipped paragraphs.
    /// </summary>
    [ObservableProperty]
    private int _skippedCount;

    /// <summary>
    /// Gets or sets the average grade level improvement.
    /// </summary>
    [ObservableProperty]
    private double _averageImprovement;

    /// <summary>
    /// Gets or sets the total tokens used.
    /// </summary>
    [ObservableProperty]
    private int _tokensUsed;

    /// <summary>
    /// Gets or sets the estimated remaining time.
    /// </summary>
    [ObservableProperty]
    private TimeSpan _estimatedTimeRemaining;

    /// <summary>
    /// Gets or sets a value indicating whether the operation is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isRunning;

    /// <summary>
    /// Gets or sets a value indicating whether cancellation was requested.
    /// </summary>
    [ObservableProperty]
    private bool _isCancelling;

    /// <summary>
    /// Gets the recent activity items.
    /// </summary>
    public ObservableCollection<ActivityItem> RecentActivity { get; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the formatted progress string (e.g., "42 of 63").
    /// </summary>
    public string ProgressText => $"{CurrentParagraph} of {TotalParagraphs}";

    /// <summary>
    /// Gets the formatted estimated time remaining string.
    /// </summary>
    public string TimeRemainingText
    {
        get
        {
            if (EstimatedTimeRemaining.TotalSeconds < 1)
            {
                return "Almost done";
            }

            if (EstimatedTimeRemaining.TotalMinutes < 1)
            {
                return $"~{EstimatedTimeRemaining.TotalSeconds:F0} seconds remaining";
            }

            return $"~{EstimatedTimeRemaining.TotalMinutes:F1} minutes remaining";
        }
    }

    /// <summary>
    /// Gets the formatted average improvement string.
    /// </summary>
    public string AverageImprovementText =>
        SimplifiedCount > 0
            ? $"-{AverageImprovement:F1} grade levels"
            : "N/A";

    /// <summary>
    /// Gets the formatted tokens used string.
    /// </summary>
    public string TokensUsedText => $"{TokensUsed:N0}";

    #endregion

    #region Events

    /// <summary>
    /// Raised when the batch operation completes (successfully or cancelled).
    /// </summary>
    public event EventHandler<BatchCompletedEventArgs>? Completed;

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to cancel the batch operation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        if (_cancellationTokenSource is null || IsCancelling)
        {
            return;
        }

        _logger.LogInformation("User requested batch cancellation");
        IsCancelling = true;
        StatusMessage = "Cancelling...";
        _cancellationTokenSource.Cancel();
    }

    /// <summary>
    /// Gets a value indicating whether cancellation is possible.
    /// </summary>
    private bool CanCancel() => IsRunning && !IsCancelling;

    #endregion

    #region Public Methods

    /// <summary>
    /// Starts the batch simplification operation.
    /// </summary>
    /// <param name="documentPath">Path to the document to simplify.</param>
    /// <param name="target">The readability target.</param>
    /// <param name="options">Batch processing options.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task<BatchSimplificationResult> StartAsync(
        string documentPath,
        ReadabilityTarget target,
        BatchSimplificationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(target);

        if (IsRunning)
        {
            throw new InvalidOperationException("A batch operation is already in progress.");
        }

        _logger.LogInformation("Starting batch simplification: {DocumentPath}", documentPath);

        // LOGIC: Reset state
        IsRunning = true;
        IsCancelling = false;
        ProgressPercent = 0;
        CurrentParagraph = 0;
        TotalParagraphs = 0;
        SimplifiedCount = 0;
        SkippedCount = 0;
        TokensUsed = 0;
        AverageImprovement = 0;
        RecentActivity.Clear();

        _cancellationTokenSource = new CancellationTokenSource();
        var progress = new Progress<BatchSimplificationProgress>(OnProgressUpdate);

        try
        {
            var result = await _batchService.SimplifyDocumentAsync(
                documentPath,
                target,
                options,
                progress,
                _cancellationTokenSource.Token);

            _logger.LogInformation(
                "Batch simplification completed: {Simplified}/{Total} paragraphs",
                result.SimplifiedParagraphs, result.TotalParagraphs);

            IsRunning = false;
            Completed?.Invoke(this, new BatchCompletedEventArgs(result, result.WasCancelled));

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Batch simplification was cancelled by user");

            var cancelledResult = BatchSimplificationResult.Cancelled(
                documentPath,
                TotalParagraphs,
                CurrentParagraph,
                SimplifiedCount,
                SkippedCount,
                Array.Empty<ParagraphSimplificationResult>(),
                ReadabilityMetrics.Empty,
                UsageMetrics.Zero,
                TimeSpan.Zero);

            IsRunning = false;
            Completed?.Invoke(this, new BatchCompletedEventArgs(cancelledResult, WasCancelled: true));

            return cancelledResult;
        }
        finally
        {
            IsRunning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Handles progress updates from the batch service.
    /// </summary>
    /// <param name="progress">The progress report.</param>
    private void OnProgressUpdate(BatchSimplificationProgress progress)
    {
        // LOGIC: Update observable properties
        ProgressPercent = progress.PercentComplete;
        CurrentParagraph = progress.CurrentParagraph;
        TotalParagraphs = progress.TotalParagraphs;
        StatusMessage = progress.StatusMessage ?? $"Processing paragraph {CurrentParagraph} of {TotalParagraphs}...";
        CurrentParagraphPreview = progress.CurrentParagraphPreview;
        SimplifiedCount = progress.SimplifiedSoFar;
        SkippedCount = progress.SkippedSoFar;
        EstimatedTimeRemaining = progress.EstimatedTimeRemaining;

        // LOGIC: Notify computed properties
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(TimeRemainingText));

        _logger.LogDebug(
            "Progress update: {Current}/{Total} ({Percent:F1}%)",
            progress.CurrentParagraph, progress.TotalParagraphs, progress.PercentComplete);
    }

    /// <summary>
    /// Adds an activity item to the recent activity list.
    /// </summary>
    /// <param name="item">The activity item to add.</param>
    public void AddActivity(ActivityItem item)
    {
        RecentActivity.Insert(0, item);

        // LOGIC: Keep the list bounded
        while (RecentActivity.Count > MaxActivityItems)
        {
            RecentActivity.RemoveAt(RecentActivity.Count - 1);
        }
    }

    #endregion

    #region Disposal

    /// <inheritdoc/>
    protected override void OnDisposed()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    #endregion
}

/// <summary>
/// Represents an item in the recent activity list.
/// </summary>
/// <remarks>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
public record ActivityItem
{
    /// <summary>
    /// Gets the paragraph index (1-based).
    /// </summary>
    public required int ParagraphIndex { get; init; }

    /// <summary>
    /// Gets a value indicating whether the paragraph was simplified.
    /// </summary>
    public required bool WasSimplified { get; init; }

    /// <summary>
    /// Gets the description text.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the icon character (✓ for simplified, – for skipped).
    /// </summary>
    public string Icon => WasSimplified ? "✓" : "–";

    /// <summary>
    /// Creates an activity item for a simplified paragraph.
    /// </summary>
    /// <param name="paragraphIndex">The paragraph index (1-based).</param>
    /// <param name="originalGrade">The original grade level.</param>
    /// <param name="simplifiedGrade">The simplified grade level.</param>
    /// <returns>A new activity item.</returns>
    public static ActivityItem Simplified(int paragraphIndex, double originalGrade, double simplifiedGrade)
    {
        var improvement = originalGrade - simplifiedGrade;
        return new ActivityItem
        {
            ParagraphIndex = paragraphIndex,
            WasSimplified = true,
            Description = $"Para {paragraphIndex}: Grade {originalGrade:F0} → Grade {simplifiedGrade:F0} ({improvement:+0;-0} levels)"
        };
    }

    /// <summary>
    /// Creates an activity item for a skipped paragraph.
    /// </summary>
    /// <param name="paragraphIndex">The paragraph index (1-based).</param>
    /// <param name="reason">The skip reason.</param>
    /// <returns>A new activity item.</returns>
    public static ActivityItem Skipped(int paragraphIndex, ParagraphSkipReason reason)
    {
        var reasonText = reason switch
        {
            ParagraphSkipReason.AlreadySimple => "already simple",
            ParagraphSkipReason.TooShort => "too short",
            ParagraphSkipReason.IsHeading => "is heading",
            ParagraphSkipReason.IsCodeBlock => "is code block",
            ParagraphSkipReason.IsBlockquote => "is blockquote",
            ParagraphSkipReason.IsListItem => "is list item",
            ParagraphSkipReason.MaxParagraphsReached => "limit reached",
            ParagraphSkipReason.ProcessingFailed => "processing failed",
            _ => "skipped"
        };

        return new ActivityItem
        {
            ParagraphIndex = paragraphIndex,
            WasSimplified = false,
            Description = $"Para {paragraphIndex}: Skipped ({reasonText})"
        };
    }
}

/// <summary>
/// Event arguments for the <see cref="BatchProgressViewModel.Completed"/> event.
/// </summary>
/// <remarks>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <param name="Result">The batch simplification result.</param>
/// <param name="WasCancelled">Whether the operation was cancelled.</param>
public record BatchCompletedEventArgs(
    BatchSimplificationResult Result,
    bool WasCancelled);
