// -----------------------------------------------------------------------
// <copyright file="BatchCompletionViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Abstractions.Contracts;

using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// ViewModel for the batch simplification completion summary dialog.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This ViewModel displays a summary of the completed batch
/// simplification operation, including:
/// </para>
/// <list type="bullet">
///   <item><description>Before/after readability metrics comparison</description></item>
///   <item><description>Paragraph counts (simplified, skipped, total)</description></item>
///   <item><description>Average grade level improvement</description></item>
///   <item><description>Processing time and token usage</description></item>
///   <item><description>Undo hint for reverting all changes</description></item>
/// </list>
/// <para>
/// <b>Lifecycle:</b>
/// <list type="number">
///   <item><description>Create instance via DI (transient)</description></item>
///   <item><description>Call <see cref="SetResult"/> with the batch result</description></item>
///   <item><description>Display the dialog</description></item>
///   <item><description>Handle user commands (Close, ViewDetails)</description></item>
/// </list>
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <example>
/// <code>
/// var viewModel = serviceProvider.GetRequiredService&lt;BatchCompletionViewModel&gt;();
/// viewModel.SetResult(batchResult);
///
/// viewModel.CloseRequested += (s, e) => CloseDialog();
/// viewModel.ViewDetailsRequested += (s, e) => ShowDetailedResults();
///
/// ShowCompletionDialog(viewModel);
/// </code>
/// </example>
/// <seealso cref="BatchProgressViewModel"/>
/// <seealso cref="BatchSimplificationResult"/>
public sealed partial class BatchCompletionViewModel : DisposableViewModel
{
    #region Fields

    private readonly ILogger<BatchCompletionViewModel> _logger;
    private BatchSimplificationResult? _result;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchCompletionViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public BatchCompletionViewModel(ILogger<BatchCompletionViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region Observable Properties

    /// <summary>
    /// Gets the dialog title.
    /// </summary>
    [ObservableProperty]
    private string _title = "Simplification Complete";

    /// <summary>
    /// Gets the completion message.
    /// </summary>
    [ObservableProperty]
    private string _completionMessage = "Document simplified successfully";

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    [ObservableProperty]
    private bool _isSuccess = true;

    /// <summary>
    /// Gets the original document grade level.
    /// </summary>
    [ObservableProperty]
    private double _originalGradeLevel;

    /// <summary>
    /// Gets the simplified document grade level.
    /// </summary>
    [ObservableProperty]
    private double _simplifiedGradeLevel;

    /// <summary>
    /// Gets the original document Gunning Fog index.
    /// </summary>
    [ObservableProperty]
    private double _originalFogIndex;

    /// <summary>
    /// Gets the simplified document Gunning Fog index.
    /// </summary>
    [ObservableProperty]
    private double _simplifiedFogIndex;

    /// <summary>
    /// Gets the number of simplified paragraphs.
    /// </summary>
    [ObservableProperty]
    private int _simplifiedParagraphs;

    /// <summary>
    /// Gets the number of skipped paragraphs.
    /// </summary>
    [ObservableProperty]
    private int _skippedParagraphs;

    /// <summary>
    /// Gets the total number of paragraphs.
    /// </summary>
    [ObservableProperty]
    private int _totalParagraphs;

    /// <summary>
    /// Gets the grade level improvement.
    /// </summary>
    [ObservableProperty]
    private double _gradeLevelImprovement;

    /// <summary>
    /// Gets the processing time.
    /// </summary>
    [ObservableProperty]
    private TimeSpan _processingTime;

    /// <summary>
    /// Gets the total tokens used.
    /// </summary>
    [ObservableProperty]
    private int _tokensUsed;

    /// <summary>
    /// Gets the estimated cost.
    /// </summary>
    [ObservableProperty]
    private decimal _estimatedCost;

    /// <summary>
    /// Gets a value indicating whether the operation was cancelled.
    /// </summary>
    [ObservableProperty]
    private bool _wasCancelled;

    /// <summary>
    /// Gets a value indicating whether detailed results are available.
    /// </summary>
    [ObservableProperty]
    private bool _hasDetailedResults;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the formatted original grade level string.
    /// </summary>
    public string OriginalGradeLevelText => $"{OriginalGradeLevel:F1}";

    /// <summary>
    /// Gets the formatted simplified grade level string.
    /// </summary>
    public string SimplifiedGradeLevelText => $"{SimplifiedGradeLevel:F1}";

    /// <summary>
    /// Gets the formatted original Fog index string.
    /// </summary>
    public string OriginalFogIndexText => $"{OriginalFogIndex:F1}";

    /// <summary>
    /// Gets the formatted simplified Fog index string.
    /// </summary>
    public string SimplifiedFogIndexText => $"{SimplifiedFogIndex:F1}";

    /// <summary>
    /// Gets the formatted paragraph counts string.
    /// </summary>
    public string ParagraphCountsText =>
        $"{SimplifiedParagraphs} simplified, {SkippedParagraphs} skipped ({TotalParagraphs} total)";

    /// <summary>
    /// Gets the formatted grade level improvement string.
    /// </summary>
    public string GradeLevelImprovementText =>
        GradeLevelImprovement > 0
            ? $"-{GradeLevelImprovement:F1} grade levels"
            : $"+{Math.Abs(GradeLevelImprovement):F1} grade levels";

    /// <summary>
    /// Gets the formatted processing time string.
    /// </summary>
    public string ProcessingTimeText
    {
        get
        {
            if (ProcessingTime.TotalSeconds < 60)
            {
                return $"{ProcessingTime.TotalSeconds:F0} seconds";
            }

            var minutes = (int)ProcessingTime.TotalMinutes;
            var seconds = (int)(ProcessingTime.TotalSeconds % 60);
            return $"{minutes} minute{(minutes == 1 ? "" : "s")} {seconds} seconds";
        }
    }

    /// <summary>
    /// Gets the formatted tokens and cost string.
    /// </summary>
    public string TokensAndCostText =>
        $"{TokensUsed:N0} (estimated cost: ${EstimatedCost:F2})";

    /// <summary>
    /// Gets the undo hint message.
    /// </summary>
    public string UndoHint => "You can undo all changes with Ctrl+Z";

    #endregion

    #region Events

    /// <summary>
    /// Raised when the close command is invoked.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Raised when the view details command is invoked.
    /// </summary>
    public event EventHandler? ViewDetailsRequested;

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to close the dialog.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        _logger.LogDebug("Close command invoked");
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the command to view detailed results.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanViewDetails))]
    private void ViewDetails()
    {
        _logger.LogDebug("View details command invoked");
        ViewDetailsRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets a value indicating whether detailed results can be viewed.
    /// </summary>
    private bool CanViewDetails() => HasDetailedResults;

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the batch simplification result to display.
    /// </summary>
    /// <param name="result">The batch result to display.</param>
    /// <exception cref="ArgumentNullException">Thrown when result is null.</exception>
    public void SetResult(BatchSimplificationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        _result = result;

        // LOGIC: Update status
        WasCancelled = result.WasCancelled;
        IsSuccess = result.Success;

        if (WasCancelled)
        {
            Title = "Simplification Cancelled";
            CompletionMessage = "Document simplification was cancelled";
        }
        else if (!IsSuccess)
        {
            Title = "Simplification Failed";
            CompletionMessage = result.ErrorMessage ?? "An error occurred during simplification";
        }
        else
        {
            Title = "Simplification Complete";
            CompletionMessage = "Document simplified successfully";
        }

        // LOGIC: Update metrics
        OriginalGradeLevel = result.OriginalDocumentMetrics.FleschKincaidGradeLevel;
        SimplifiedGradeLevel = result.SimplifiedDocumentMetrics.FleschKincaidGradeLevel;
        OriginalFogIndex = result.OriginalDocumentMetrics.GunningFogIndex;
        SimplifiedFogIndex = result.SimplifiedDocumentMetrics.GunningFogIndex;

        // LOGIC: Update counts
        SimplifiedParagraphs = result.SimplifiedParagraphs;
        SkippedParagraphs = result.SkippedParagraphs;
        TotalParagraphs = result.TotalParagraphs;

        // LOGIC: Update improvement metrics
        GradeLevelImprovement = result.GradeLevelReduction;
        ProcessingTime = result.TotalProcessingTime;
        TokensUsed = result.TotalTokenUsage.TotalTokens;
        EstimatedCost = result.TotalTokenUsage.EstimatedCost;

        // LOGIC: Detailed results available if there are paragraph results
        HasDetailedResults = result.ParagraphResults.Count > 0;

        // LOGIC: Notify computed properties
        OnPropertyChanged(nameof(OriginalGradeLevelText));
        OnPropertyChanged(nameof(SimplifiedGradeLevelText));
        OnPropertyChanged(nameof(OriginalFogIndexText));
        OnPropertyChanged(nameof(SimplifiedFogIndexText));
        OnPropertyChanged(nameof(ParagraphCountsText));
        OnPropertyChanged(nameof(GradeLevelImprovementText));
        OnPropertyChanged(nameof(ProcessingTimeText));
        OnPropertyChanged(nameof(TokensAndCostText));

        _logger.LogInformation(
            "Displaying completion summary: {Simplified}/{Total} paragraphs, {Improvement:F1} grade level improvement",
            result.SimplifiedParagraphs, result.TotalParagraphs, result.GradeLevelReduction);
    }

    /// <summary>
    /// Gets the underlying batch result.
    /// </summary>
    /// <returns>The batch result, or null if not set.</returns>
    public BatchSimplificationResult? GetResult() => _result;

    #endregion
}
