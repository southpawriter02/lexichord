// =============================================================================
// File: IndexStatusViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel for the Index Status View in Settings.
// Version: v0.4.7a
// =============================================================================

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel for the Index Status View, displaying document status and statistics.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexStatusViewModel"/> manages the state and logic for displaying
/// indexed document status in the Settings dialog. It provides filtering by text
/// and status, as well as aggregate statistics.
/// </para>
/// <para>
/// <b>Dependencies:</b>
/// <list type="bullet">
/// <item><see cref="IIndexStatusService"/> - Retrieves status data</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7a as part of the Index Status View.
/// </para>
/// </remarks>
public partial class IndexStatusViewModel : ObservableObject
{
    private readonly IIndexStatusService _statusService;
    private readonly ILogger<IndexStatusViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="IndexStatusViewModel"/>.
    /// </summary>
    /// <param name="statusService">Service for retrieving index status.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public IndexStatusViewModel(
        IIndexStatusService statusService,
        ILogger<IndexStatusViewModel> logger)
    {
        _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("[IndexStatusViewModel] Initialized");
    }

    #region Observable Properties

    /// <summary>
    /// Gets or sets whether data is currently loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the aggregate index statistics.
    /// </summary>
    [ObservableProperty]
    private IndexStatistics? _statistics;

    /// <summary>
    /// Gets or sets the text filter for document names.
    /// </summary>
    [ObservableProperty]
    private string _filterText = string.Empty;

    /// <summary>
    /// Gets or sets the status filter. Null means all statuses.
    /// </summary>
    [ObservableProperty]
    private IndexingStatus? _statusFilter;

    #endregion

    #region Collections

    /// <summary>
    /// Gets the collection of all loaded documents.
    /// </summary>
    public ObservableCollection<IndexedDocumentViewModel> Documents { get; } = new();

    /// <summary>
    /// Gets the filtered documents based on current filter settings.
    /// </summary>
    public IReadOnlyList<IndexedDocumentViewModel> FilteredDocuments =>
        Documents.Where(MatchesFilter).ToList();

    #endregion

    #region Filter Options

    /// <summary>
    /// Gets the available status filter options.
    /// </summary>
    public static IReadOnlyList<IndexingStatus?> StatusFilterOptions { get; } = new IndexingStatus?[]
    {
        null, // All
        IndexingStatus.Indexed,
        IndexingStatus.Pending,
        IndexingStatus.Indexing,
        IndexingStatus.Stale,
        IndexingStatus.Failed
    };

    /// <summary>
    /// Gets the display text for a status filter option.
    /// </summary>
    public static string GetStatusFilterDisplay(IndexingStatus? status) =>
        status switch
        {
            null => "All Statuses",
            IndexingStatus.Indexed => "Indexed",
            IndexingStatus.Pending => "Pending",
            IndexingStatus.Indexing => "Indexing",
            IndexingStatus.Stale => "Stale",
            IndexingStatus.Failed => "Failed",
            _ => status.ToString()!
        };

    #endregion

    #region Commands

    /// <summary>
    /// Loads all documents and statistics.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (IsLoading)
        {
            _logger.LogDebug("[IndexStatusViewModel] Load already in progress, skipping");
            return;
        }

        _logger.LogInformation("[IndexStatusViewModel] Loading index status data");
        IsLoading = true;

        try
        {
            // Load documents
            var documents = await _statusService.GetAllDocumentsAsync(ct);

            Documents.Clear();
            foreach (var doc in documents)
            {
                Documents.Add(new IndexedDocumentViewModel(doc));
            }

            _logger.LogDebug("[IndexStatusViewModel] Loaded {Count} documents", documents.Count);

            // Load statistics
            Statistics = await _statusService.GetStatisticsAsync(ct);
            _logger.LogDebug(
                "[IndexStatusViewModel] Stats: {DocCount} docs, {ChunkCount} chunks",
                Statistics.DocumentCount,
                Statistics.ChunkCount);

            // Notify filtered list changed
            OnPropertyChanged(nameof(FilteredDocuments));
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("[IndexStatusViewModel] Load cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IndexStatusViewModel] Failed to load index status");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes stale status for all documents and reloads.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct = default)
    {
        if (IsLoading)
        {
            _logger.LogDebug("[IndexStatusViewModel] Refresh skipped, load in progress");
            return;
        }

        _logger.LogInformation("[IndexStatusViewModel] Refreshing stale status");
        IsLoading = true;

        try
        {
            await _statusService.RefreshStaleStatusAsync(ct);
            await LoadAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("[IndexStatusViewModel] Refresh cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IndexStatusViewModel] Failed to refresh status");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Property Changed Handlers

    /// <summary>
    /// Called when <see cref="FilterText"/> changes.
    /// </summary>
    partial void OnFilterTextChanged(string value)
    {
        _logger.LogDebug("[IndexStatusViewModel] Filter text changed to: '{Filter}'", value);
        OnPropertyChanged(nameof(FilteredDocuments));
    }

    /// <summary>
    /// Called when <see cref="StatusFilter"/> changes.
    /// </summary>
    partial void OnStatusFilterChanged(IndexingStatus? value)
    {
        _logger.LogDebug("[IndexStatusViewModel] Status filter changed to: {Status}", value?.ToString() ?? "All");
        OnPropertyChanged(nameof(FilteredDocuments));
    }

    #endregion

    #region Filtering Logic

    /// <summary>
    /// Determines if a document matches the current filter criteria.
    /// </summary>
    private bool MatchesFilter(IndexedDocumentViewModel doc)
    {
        // Status filter
        if (StatusFilter.HasValue && doc.Status != StatusFilter.Value)
            return false;

        // Text filter
        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            return doc.FileName.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
                || doc.FilePath.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    #endregion
}
