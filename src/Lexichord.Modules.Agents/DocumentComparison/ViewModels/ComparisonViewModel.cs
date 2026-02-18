// -----------------------------------------------------------------------
// <copyright file="ComparisonViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: ViewModel for the Comparison View panel (v0.7.6d).
//   Orchestrates document comparison with:
//   - File path selection and content display
//   - Comparison execution via IDocumentComparer
//   - Change grouping by significance level
//   - Diff view toggle
//   - Export and copy actions
//
//   Introduced in: v0.7.6d
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Agents.DocumentComparison;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.DocumentComparison.ViewModels;

/// <summary>
/// ViewModel for the Comparison View that orchestrates document comparison.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This ViewModel orchestrates the Comparison View UI, providing:
/// <list type="bullet">
/// <item><description>File path selection for original and new documents</description></item>
/// <item><description>Comparison execution via <see cref="IDocumentComparer"/></description></item>
/// <item><description>Change grouping by significance level (Critical, High, Medium, Low)</description></item>
/// <item><description>Toggle for showing raw text diff</description></item>
/// <item><description>Actions: Compare, Refresh, Copy Diff, Export Report</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Lifetime:</b> Transient - each panel instance has its own ViewModel.
/// </para>
/// <para>
/// <b>Thread safety:</b> UI operations are performed on the UI thread via Avalonia's dispatcher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6d as part of the Document Comparison feature.
/// </para>
/// </remarks>
/// <seealso cref="IDocumentComparer"/>
/// <seealso cref="ComparisonResult"/>
/// <seealso cref="ChangeCardViewModel"/>
public sealed partial class ComparisonViewModel : ObservableObject, IDisposable
{
    private readonly IDocumentComparer _comparer;
    private readonly IClipboardService? _clipboardService;
    private readonly ILogger<ComparisonViewModel> _logger;
    private CancellationTokenSource? _cts;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the original document path.
    /// </summary>
    [ObservableProperty]
    private string? _originalDocumentPath;

    /// <summary>
    /// Gets or sets the new document path.
    /// </summary>
    [ObservableProperty]
    private string? _newDocumentPath;

    /// <summary>
    /// Gets or sets the original version label.
    /// </summary>
    [ObservableProperty]
    private string? _originalLabel;

    /// <summary>
    /// Gets or sets the new version label.
    /// </summary>
    [ObservableProperty]
    private string? _newLabel;

    /// <summary>
    /// Gets or sets the current comparison result.
    /// </summary>
    [ObservableProperty]
    private ComparisonResult? _result;

    /// <summary>
    /// Gets or sets whether the panel is loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the error message if any.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Gets or sets whether to show the text diff.
    /// </summary>
    [ObservableProperty]
    private bool _showTextDiff;

    /// <summary>
    /// Gets or sets the selected filter (All, Critical, High, Medium, Low).
    /// </summary>
    [ObservableProperty]
    private string _selectedFilter = "All";

    /// <summary>
    /// Gets or sets whether documents are identical.
    /// </summary>
    [ObservableProperty]
    private bool _areIdentical;

    /// <summary>
    /// Gets or sets whether there is a result to display.
    /// </summary>
    [ObservableProperty]
    private bool _hasResult;

    /// <summary>
    /// Gets or sets the summary text.
    /// </summary>
    [ObservableProperty]
    private string? _summary;

    /// <summary>
    /// Gets or sets the original word count display.
    /// </summary>
    [ObservableProperty]
    private string? _originalWordCountDisplay;

    /// <summary>
    /// Gets or sets the new word count display.
    /// </summary>
    [ObservableProperty]
    private string? _newWordCountDisplay;

    /// <summary>
    /// Gets or sets the word count delta display.
    /// </summary>
    [ObservableProperty]
    private string? _wordCountDeltaDisplay;

    /// <summary>
    /// Gets or sets the change magnitude percentage.
    /// </summary>
    [ObservableProperty]
    private double _changeMagnitude;

    /// <summary>
    /// Gets or sets the change magnitude display.
    /// </summary>
    [ObservableProperty]
    private string? _changeMagnitudeDisplay;

    /// <summary>
    /// Gets or sets the change stats display.
    /// </summary>
    [ObservableProperty]
    private string? _changeStatsDisplay;

    /// <summary>
    /// Gets or sets the text diff content.
    /// </summary>
    [ObservableProperty]
    private string? _textDiff;

    #endregion

    #region Collections

    /// <summary>
    /// Gets the collection of critical changes.
    /// </summary>
    public ObservableCollection<ChangeCardViewModel> CriticalChanges { get; } = new();

    /// <summary>
    /// Gets the collection of high-significance changes.
    /// </summary>
    public ObservableCollection<ChangeCardViewModel> HighChanges { get; } = new();

    /// <summary>
    /// Gets the collection of medium-significance changes.
    /// </summary>
    public ObservableCollection<ChangeCardViewModel> MediumChanges { get; } = new();

    /// <summary>
    /// Gets the collection of low-significance changes.
    /// </summary>
    public ObservableCollection<ChangeCardViewModel> LowChanges { get; } = new();

    /// <summary>
    /// Gets all change cards.
    /// </summary>
    public ObservableCollection<ChangeCardViewModel> AllChanges { get; } = new();

    /// <summary>
    /// Gets the available filter options.
    /// </summary>
    public IReadOnlyList<string> FilterOptions { get; } = new[]
    {
        "All",
        "Critical",
        "High",
        "Medium",
        "Low"
    };

    #endregion

    #region Commands

    /// <summary>
    /// Command to execute the comparison.
    /// </summary>
    public IAsyncRelayCommand CompareCommand { get; }

    /// <summary>
    /// Command to refresh the comparison.
    /// </summary>
    public IAsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// Command to copy the diff to clipboard.
    /// </summary>
    public IAsyncRelayCommand CopyDiffCommand { get; }

    /// <summary>
    /// Command to export the comparison report.
    /// </summary>
    public IAsyncRelayCommand ExportReportCommand { get; }

    /// <summary>
    /// Command to cancel the current operation.
    /// </summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>
    /// Command to close the panel.
    /// </summary>
    public IRelayCommand CloseCommand { get; }

    #endregion

    /// <summary>
    /// Event raised when the panel should be closed.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Initializes a new instance of <see cref="ComparisonViewModel"/>.
    /// </summary>
    /// <param name="comparer">The document comparer service.</param>
    /// <param name="clipboardService">Optional clipboard service for copy operations.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ComparisonViewModel(
        IDocumentComparer comparer,
        IClipboardService? clipboardService,
        ILogger<ComparisonViewModel> logger)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        _clipboardService = clipboardService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize commands
        CompareCommand = new AsyncRelayCommand(CompareAsync, CanCompare);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => HasResult);
        CopyDiffCommand = new AsyncRelayCommand(CopyDiffAsync, () => HasResult && _clipboardService is not null);
        ExportReportCommand = new AsyncRelayCommand(ExportReportAsync, () => HasResult);
        CancelCommand = new RelayCommand(Cancel, () => IsLoading);
        CloseCommand = new RelayCommand(Close);
    }

    /// <summary>
    /// Initializes the view model with paths for comparison.
    /// </summary>
    /// <param name="originalPath">Path to the original document.</param>
    /// <param name="newPath">Path to the new document.</param>
    /// <param name="originalLabel">Optional label for the original version.</param>
    /// <param name="newLabel">Optional label for the new version.</param>
    public void Initialize(
        string? originalPath,
        string? newPath,
        string? originalLabel = null,
        string? newLabel = null)
    {
        OriginalDocumentPath = originalPath;
        NewDocumentPath = newPath;
        OriginalLabel = originalLabel;
        NewLabel = newLabel;

        _logger.LogDebug(
            "ComparisonViewModel initialized: original={OriginalPath}, new={NewPath}",
            originalPath,
            newPath);
    }

    #region Command Implementations

    private bool CanCompare() =>
        !IsLoading &&
        !string.IsNullOrWhiteSpace(OriginalDocumentPath) &&
        !string.IsNullOrWhiteSpace(NewDocumentPath);

    private async Task CompareAsync()
    {
        if (string.IsNullOrWhiteSpace(OriginalDocumentPath) || string.IsNullOrWhiteSpace(NewDocumentPath))
        {
            ErrorMessage = "Please select both original and new documents.";
            return;
        }

        _logger.LogDebug(
            "Starting comparison: {OriginalPath} vs {NewPath}",
            OriginalDocumentPath,
            NewDocumentPath);

        IsLoading = true;
        ErrorMessage = null;
        _cts = new CancellationTokenSource();

        try
        {
            var options = new ComparisonOptions
            {
                OriginalVersionLabel = OriginalLabel,
                NewVersionLabel = NewLabel,
                IncludeTextDiff = true
            };

            var result = await _comparer.CompareAsync(
                OriginalDocumentPath,
                NewDocumentPath,
                options,
                _cts.Token);

            UpdateFromResult(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Comparison cancelled by user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Comparison failed");
            ErrorMessage = $"Comparison failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            _cts?.Dispose();
            _cts = null;

            CompareCommand.NotifyCanExecuteChanged();
            RefreshCommand.NotifyCanExecuteChanged();
            CopyDiffCommand.NotifyCanExecuteChanged();
            ExportReportCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task RefreshAsync()
    {
        await CompareAsync();
    }

    private async Task CopyDiffAsync()
    {
        if (_clipboardService is null || Result is null)
        {
            return;
        }

        try
        {
            var report = await GenerateReportAsync();
            await _clipboardService.SetTextAsync(report);

            _logger.LogInformation("Comparison report copied to clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy to clipboard");
            ErrorMessage = $"Failed to copy: {ex.Message}";
        }
    }

    private async Task ExportReportAsync()
    {
        // LOGIC: Export to file would open a save dialog
        // For now, we just generate the report
        if (Result is null)
        {
            return;
        }

        var report = await GenerateReportAsync();

        _logger.LogInformation(
            "Report generated: {CharacterCount} characters",
            report.Length);

        // TODO: Open save dialog and write to file
    }

    private void Cancel()
    {
        _cts?.Cancel();
    }

    private void Close()
    {
        _cts?.Cancel();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Private Methods

    private void UpdateFromResult(ComparisonResult result)
    {
        Result = result;
        HasResult = true;
        AreIdentical = result.AreIdentical;

        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            Summary = result.Summary;
            return;
        }

        // Update display properties
        Summary = result.Summary;
        OriginalWordCountDisplay = $"{result.OriginalWordCount:N0} words";
        NewWordCountDisplay = $"{result.NewWordCount:N0} words";

        var delta = result.WordCountDelta;
        WordCountDeltaDisplay = delta switch
        {
            > 0 => $"+{delta:N0}",
            < 0 => $"{delta:N0}",
            _ => "0"
        };

        ChangeMagnitude = result.ChangeMagnitude;
        ChangeMagnitudeDisplay = $"{result.ChangeMagnitude:P0}";

        ChangeStatsDisplay = $"Additions: {result.AdditionCount}  •  Modifications: {result.ModificationCount}  •  Deletions: {result.DeletionCount}";

        TextDiff = result.TextDiff;

        // Group changes by significance
        CriticalChanges.Clear();
        HighChanges.Clear();
        MediumChanges.Clear();
        LowChanges.Clear();
        AllChanges.Clear();

        foreach (var change in result.Changes)
        {
            var vm = new ChangeCardViewModel(change);
            AllChanges.Add(vm);

            switch (change.SignificanceLevel)
            {
                case ChangeSignificance.Critical:
                    CriticalChanges.Add(vm);
                    break;
                case ChangeSignificance.High:
                    HighChanges.Add(vm);
                    break;
                case ChangeSignificance.Medium:
                    MediumChanges.Add(vm);
                    break;
                case ChangeSignificance.Low:
                    LowChanges.Add(vm);
                    break;
            }
        }

        _logger.LogDebug(
            "Result updated: {TotalChanges} changes ({Critical} critical, {High} high, {Medium} medium, {Low} low)",
            result.Changes.Count,
            CriticalChanges.Count,
            HighChanges.Count,
            MediumChanges.Count,
            LowChanges.Count);
    }

    private Task<string> GenerateReportAsync()
    {
        if (Result is null)
        {
            return Task.FromResult(string.Empty);
        }

        var report = new System.Text.StringBuilder();

        report.AppendLine("# Document Comparison Report");
        report.AppendLine();

        // Version info
        report.AppendLine("## Versions");
        report.AppendLine($"- **Original:** {Result.OriginalPath}");
        if (Result.OriginalLabel is not null)
        {
            report.AppendLine($"  - Label: {Result.OriginalLabel}");
        }

        report.AppendLine($"  - Word count: {Result.OriginalWordCount:N0}");

        report.AppendLine($"- **New:** {Result.NewPath}");
        if (Result.NewLabel is not null)
        {
            report.AppendLine($"  - Label: {Result.NewLabel}");
        }

        report.AppendLine($"  - Word count: {Result.NewWordCount:N0}");
        report.AppendLine();

        // Summary
        report.AppendLine("## Summary");
        report.AppendLine(Result.Summary);
        report.AppendLine();
        report.AppendLine($"**Change Magnitude:** {Result.ChangeMagnitude:P0}");
        report.AppendLine($"**Word Count Delta:** {Result.WordCountDelta:+#;-#;0}");
        report.AppendLine();

        // Changes
        report.AppendLine($"## Changes ({Result.Changes.Count} total)");
        report.AppendLine();

        // Group and output
        foreach (var level in new[] { ChangeSignificance.Critical, ChangeSignificance.High, ChangeSignificance.Medium, ChangeSignificance.Low })
        {
            var changes = Result.Changes.Where(c => c.SignificanceLevel == level).ToList();
            if (changes.Count == 0)
            {
                continue;
            }

            report.AppendLine($"### {level.GetDisplayLabel()} ({changes.Count})");
            report.AppendLine();

            foreach (var change in changes)
            {
                report.AppendLine($"- **[{change.Category}]** {change.Description}");
                if (change.Section is not null)
                {
                    report.AppendLine($"  - Section: {change.Section}");
                }

                report.AppendLine($"  - Significance: {change.Significance:F2}");

                if (change.Impact is not null)
                {
                    report.AppendLine($"  - Impact: {change.Impact}");
                }

                report.AppendLine();
            }
        }

        // Text diff
        if (!string.IsNullOrEmpty(Result.TextDiff))
        {
            report.AppendLine("## Text Diff");
            report.AppendLine("```diff");
            report.AppendLine(Result.TextDiff);
            report.AppendLine("```");
        }

        return Task.FromResult(report.ToString());
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
