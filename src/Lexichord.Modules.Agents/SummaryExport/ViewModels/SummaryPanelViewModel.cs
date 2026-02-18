// -----------------------------------------------------------------------
// <copyright file="SummaryPanelViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: ViewModel for the Summary Panel UI (v0.7.6c).
//   Orchestrates summary display with:
//   - Summary content and metadata display
//   - Mode selection dropdown
//   - Refresh, Copy, Frontmatter, Export actions
//   - Key terms visualization
//
//   Introduced in: v0.7.6c
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Agents.MetadataExtraction;
using Lexichord.Abstractions.Agents.Summarizer;
using Lexichord.Abstractions.Agents.SummaryExport;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.SummaryExport.ViewModels;

/// <summary>
/// ViewModel for the Summary Panel that displays and manages summaries.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This ViewModel orchestrates the Summary Panel UI, providing:
/// <list type="bullet">
/// <item><description>Summary content and mode display</description></item>
/// <item><description>Document metadata visualization</description></item>
/// <item><description>Key terms with importance visualization</description></item>
/// <item><description>Actions: Refresh, Copy, Add to Frontmatter, Export to File</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Lifetime:</b> Transient - each panel instance has its own ViewModel.
/// </para>
/// <para>
/// <b>Thread safety:</b> UI operations are performed on the UI thread via Avalonia's dispatcher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6c as part of the Summarizer Agent Export Formats.
/// </para>
/// </remarks>
/// <seealso cref="ISummaryExporter"/>
/// <seealso cref="ISummarizerAgent"/>
/// <seealso cref="IMetadataExtractor"/>
public sealed partial class SummaryPanelViewModel : ObservableObject
{
    private readonly ISummaryExporter _exporter;
    private readonly ISummarizerAgent _summarizer;
    private readonly IMetadataExtractor _metadataExtractor;
    private readonly ILogger<SummaryPanelViewModel> _logger;

    #region Observable Properties

    /// <summary>
    /// Gets or sets the source document path.
    /// </summary>
    [ObservableProperty]
    private string? _documentPath;

    /// <summary>
    /// Gets or sets the document file name for display.
    /// </summary>
    [ObservableProperty]
    private string? _documentName;

    /// <summary>
    /// Gets or sets the current summarization result.
    /// </summary>
    [ObservableProperty]
    private SummarizationResult? _summary;

    /// <summary>
    /// Gets or sets the current document metadata.
    /// </summary>
    [ObservableProperty]
    private DocumentMetadata? _metadata;

    /// <summary>
    /// Gets or sets the selected summarization mode.
    /// </summary>
    [ObservableProperty]
    private SummarizationMode _selectedMode = SummarizationMode.BulletPoints;

    /// <summary>
    /// Gets or sets whether the panel is loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the generation information string.
    /// </summary>
    [ObservableProperty]
    private string? _generationInfo;

    /// <summary>
    /// Gets or sets the error message if any.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Gets or sets whether the panel has content to display.
    /// </summary>
    [ObservableProperty]
    private bool _hasContent;

    /// <summary>
    /// Gets or sets whether metadata is available.
    /// </summary>
    [ObservableProperty]
    private bool _hasMetadata;

    /// <summary>
    /// Gets or sets the reading time display string.
    /// </summary>
    [ObservableProperty]
    private string? _readingTimeDisplay;

    /// <summary>
    /// Gets or sets the complexity display string.
    /// </summary>
    [ObservableProperty]
    private string? _complexityDisplay;

    /// <summary>
    /// Gets or sets the audience display string.
    /// </summary>
    [ObservableProperty]
    private string? _audienceDisplay;

    /// <summary>
    /// Gets or sets the category display string.
    /// </summary>
    [ObservableProperty]
    private string? _categoryDisplay;

    /// <summary>
    /// Gets or sets the tags display string.
    /// </summary>
    [ObservableProperty]
    private string? _tagsDisplay;

    /// <summary>
    /// Gets or sets the compression display string.
    /// </summary>
    [ObservableProperty]
    private string? _compressionDisplay;

    #endregion

    #region Collections

    /// <summary>
    /// Gets the collection of key terms for display.
    /// </summary>
    public ObservableCollection<KeyTermViewModel> KeyTerms { get; } = new();

    /// <summary>
    /// Gets the available summarization modes.
    /// </summary>
    public IReadOnlyList<SummarizationMode> AvailableModes { get; } = Enum.GetValues<SummarizationMode>();

    #endregion

    #region Commands

    /// <summary>
    /// Command to refresh the summary.
    /// </summary>
    public IAsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// Command to copy summary to clipboard.
    /// </summary>
    public IAsyncRelayCommand CopySummaryCommand { get; }

    /// <summary>
    /// Command to add summary to frontmatter.
    /// </summary>
    public IAsyncRelayCommand AddToFrontmatterCommand { get; }

    /// <summary>
    /// Command to export summary to file.
    /// </summary>
    public IAsyncRelayCommand ExportFileCommand { get; }

    /// <summary>
    /// Command to close the panel.
    /// </summary>
    public IRelayCommand CloseCommand { get; }

    /// <summary>
    /// Command to copy key terms to clipboard.
    /// </summary>
    public IAsyncRelayCommand CopyKeyTermsCommand { get; }

    /// <summary>
    /// Command to clear the cache.
    /// </summary>
    public IAsyncRelayCommand ClearCacheCommand { get; }

    #endregion

    /// <summary>
    /// Event raised when the panel should be closed.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="SummaryPanelViewModel"/> class.
    /// </summary>
    /// <param name="exporter">The summary exporter service.</param>
    /// <param name="summarizer">The summarizer agent.</param>
    /// <param name="metadataExtractor">The metadata extractor.</param>
    /// <param name="logger">The logger for diagnostic messages.</param>
    public SummaryPanelViewModel(
        ISummaryExporter exporter,
        ISummarizerAgent summarizer,
        IMetadataExtractor metadataExtractor,
        ILogger<SummaryPanelViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(summarizer);
        ArgumentNullException.ThrowIfNull(metadataExtractor);
        ArgumentNullException.ThrowIfNull(logger);

        _exporter = exporter;
        _summarizer = summarizer;
        _metadataExtractor = metadataExtractor;
        _logger = logger;

        // Initialize commands
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsLoading && !string.IsNullOrEmpty(DocumentPath));
        CopySummaryCommand = new AsyncRelayCommand(CopyToClipboardAsync, () => Summary != null);
        AddToFrontmatterCommand = new AsyncRelayCommand(AddToFrontmatterAsync, () => Summary != null && !string.IsNullOrEmpty(DocumentPath));
        ExportFileCommand = new AsyncRelayCommand(ExportToFileAsync, () => Summary != null && !string.IsNullOrEmpty(DocumentPath));
        CloseCommand = new RelayCommand(Close);
        CopyKeyTermsCommand = new AsyncRelayCommand(CopyKeyTermsAsync, () => KeyTerms.Count > 0);
        ClearCacheCommand = new AsyncRelayCommand(ClearCacheAsync, () => !string.IsNullOrEmpty(DocumentPath));

        _logger.LogDebug("SummaryPanelViewModel initialized");
    }

    /// <summary>
    /// Loads a summary into the panel for display.
    /// </summary>
    /// <param name="summary">The summary to display.</param>
    /// <param name="metadata">Optional metadata to display.</param>
    /// <param name="documentPath">The source document path.</param>
    public void LoadSummary(SummarizationResult summary, DocumentMetadata? metadata, string documentPath)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(documentPath);

        _logger.LogDebug("Loading summary for {DocumentPath}", documentPath);

        DocumentPath = documentPath;
        DocumentName = Path.GetFileName(documentPath);
        Summary = summary;
        Metadata = metadata;
        SelectedMode = summary.Mode;
        ErrorMessage = null;
        HasContent = true;
        HasMetadata = metadata != null;

        // Build generation info
        GenerationInfo = BuildGenerationInfo(summary);
        CompressionDisplay = $"{summary.OriginalWordCount} → {summary.SummaryWordCount} words ({summary.CompressionRatio:F1}x)";

        // Update metadata displays
        if (metadata != null)
        {
            ReadingTimeDisplay = $"{metadata.EstimatedReadingMinutes} min read";
            ComplexityDisplay = $"{metadata.ComplexityScore}/10";
            AudienceDisplay = metadata.TargetAudience ?? "General";
            CategoryDisplay = metadata.PrimaryCategory ?? "Uncategorized";
            TagsDisplay = metadata.SuggestedTags.Count > 0
                ? string.Join(", ", metadata.SuggestedTags.Take(5))
                : "None";

            // Load key terms
            KeyTerms.Clear();
            foreach (var term in metadata.KeyTerms.Take(8))
            {
                KeyTerms.Add(new KeyTermViewModel(term));
            }
        }

        // Update command states
        RefreshCommand.NotifyCanExecuteChanged();
        CopySummaryCommand.NotifyCanExecuteChanged();
        AddToFrontmatterCommand.NotifyCanExecuteChanged();
        ExportFileCommand.NotifyCanExecuteChanged();
        CopyKeyTermsCommand.NotifyCanExecuteChanged();
        ClearCacheCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Refreshes the summary by regenerating it.
    /// </summary>
    private async Task RefreshAsync()
    {
        if (string.IsNullOrEmpty(DocumentPath))
        {
            return;
        }

        _logger.LogDebug("Refreshing summary for {DocumentPath}", DocumentPath);

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var options = new SummarizationOptions { Mode = SelectedMode };
            var newSummary = await _summarizer.SummarizeAsync(DocumentPath, options);

            DocumentMetadata? newMetadata = null;
            try
            {
                newMetadata = await _metadataExtractor.ExtractAsync(DocumentPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract metadata during refresh");
            }

            if (newSummary.Success)
            {
                LoadSummary(newSummary, newMetadata, DocumentPath);
                await _exporter.CacheSummaryAsync(DocumentPath, newSummary, newMetadata);
            }
            else
            {
                ErrorMessage = newSummary.ErrorMessage ?? "Failed to generate summary.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh summary");
            ErrorMessage = $"Failed to refresh: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Copies the summary to the clipboard.
    /// </summary>
    private async Task CopyToClipboardAsync()
    {
        if (Summary == null || string.IsNullOrEmpty(DocumentPath))
        {
            return;
        }

        _logger.LogDebug("Copying summary to clipboard");

        try
        {
            var options = new SummaryExportOptions
            {
                Destination = ExportDestination.Clipboard,
                ClipboardAsMarkdown = true
            };

            var result = await _exporter.ExportAsync(Summary, DocumentPath, options);

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy to clipboard");
            ErrorMessage = $"Failed to copy: {ex.Message}";
        }
    }

    /// <summary>
    /// Adds the summary to the document's frontmatter.
    /// </summary>
    private async Task AddToFrontmatterAsync()
    {
        if (Summary == null || string.IsNullOrEmpty(DocumentPath))
        {
            return;
        }

        _logger.LogDebug("Adding summary to frontmatter");

        try
        {
            var result = await _exporter.UpdateFrontmatterAsync(DocumentPath, Summary, Metadata);

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update frontmatter");
            ErrorMessage = $"Failed to update frontmatter: {ex.Message}";
        }
    }

    /// <summary>
    /// Exports the summary to a file.
    /// </summary>
    private async Task ExportToFileAsync()
    {
        if (Summary == null || string.IsNullOrEmpty(DocumentPath))
        {
            return;
        }

        _logger.LogDebug("Exporting summary to file");

        try
        {
            var options = new SummaryExportOptions
            {
                Destination = ExportDestination.File,
                IncludeMetadata = Metadata != null,
                IncludeSourceReference = true
            };

            var result = await _exporter.ExportAsync(Summary, DocumentPath, options);

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export to file");
            ErrorMessage = $"Failed to export: {ex.Message}";
        }
    }

    /// <summary>
    /// Copies key terms to the clipboard.
    /// </summary>
    private async Task CopyKeyTermsAsync()
    {
        if (KeyTerms.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Copying key terms to clipboard");

        try
        {
            var terms = string.Join(", ", KeyTerms.Select(t => t.Term));
            var options = new SummaryExportOptions { Destination = ExportDestination.Clipboard };

            // Use a simple summary result for clipboard
            var tempSummary = new SummarizationResult
            {
                Summary = terms,
                Mode = SummarizationMode.BulletPoints,
                Usage = Abstractions.Agents.UsageMetrics.Zero
            };

            await _exporter.ExportAsync(tempSummary, DocumentPath ?? "", options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy key terms");
            ErrorMessage = $"Failed to copy key terms: {ex.Message}";
        }
    }

    /// <summary>
    /// Clears the cached summary.
    /// </summary>
    private async Task ClearCacheAsync()
    {
        if (string.IsNullOrEmpty(DocumentPath))
        {
            return;
        }

        _logger.LogDebug("Clearing cache for {DocumentPath}", DocumentPath);

        try
        {
            await _exporter.ClearCacheAsync(DocumentPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cache");
            ErrorMessage = $"Failed to clear cache: {ex.Message}";
        }
    }

    /// <summary>
    /// Closes the panel.
    /// </summary>
    private void Close()
    {
        _logger.LogDebug("Closing Summary Panel");
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Builds the generation info display string.
    /// </summary>
    private static string BuildGenerationInfo(SummarizationResult summary)
    {
        var age = DateTimeOffset.UtcNow - summary.GeneratedAt;
        var ageString = age.TotalMinutes < 1 ? "just now" :
            age.TotalMinutes < 60 ? $"{(int)age.TotalMinutes} min ago" :
            age.TotalHours < 24 ? $"{(int)age.TotalHours} hr ago" :
            $"{(int)age.TotalDays} days ago";

        return $"Generated {ageString} • {summary.Model ?? "unknown"}";
    }
}
