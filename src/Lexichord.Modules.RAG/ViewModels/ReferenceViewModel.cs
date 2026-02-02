// =============================================================================
// File: ReferenceViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel for the Reference Panel (semantic search UI).
// =============================================================================
// LOGIC: Orchestrates the Reference Panel's search functionality.
//   1. SearchQuery property (two-way bound to search input).
//   2. SearchCommand executes semantic search via ISemanticSearchService.
//   3. Results displayed via ObservableCollection of SearchResultItemViewModel.
//   4. License gating via SearchLicenseGuard (checks before search).
//   5. Search history via ISearchHistoryService (autocomplete dropdown).
//   6. Error handling for FeatureNotLicensedException, OperationCanceledException.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.5a: ISemanticSearchService, SearchResult, SearchHit, SearchOptions
//   - v0.4.5d: SearchLicenseGuard
//   - v0.4.6a: ISearchHistoryService
//   - v0.0.4c: FeatureNotLicensedException
// =============================================================================

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Search;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel for the Reference Panel, providing semantic search functionality.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ReferenceViewModel"/> powers the Reference Panel UI, enabling users
/// to perform semantic searches against indexed documents. It manages search
/// execution, result display, search history, and license validation.
/// </para>
/// <para>
/// <b>License Gating:</b> Semantic search requires the WriterPro tier or higher.
/// A warning is displayed for unlicensed users, and search is disabled.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6a as part of the Reference Panel View.
/// </para>
/// </remarks>
public partial class ReferenceViewModel : ObservableObject
{
    private readonly ISemanticSearchService _searchService;
    private readonly ISearchHistoryService _historyService;
    private readonly SearchLicenseGuard _licenseGuard;
    private readonly IMediator _mediator;
    private readonly ILogger<ReferenceViewModel> _logger;
    private CancellationTokenSource? _searchCts;

    /// <summary>
    /// Creates a new <see cref="ReferenceViewModel"/> instance.
    /// </summary>
    /// <param name="searchService">Service for executing semantic searches.</param>
    /// <param name="historyService">Service for managing search history.</param>
    /// <param name="licenseGuard">Guard for validating license tier.</param>
    /// <param name="mediator">MediatR instance for publishing events.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ReferenceViewModel(
        ISemanticSearchService searchService,
        ISearchHistoryService historyService,
        SearchLicenseGuard licenseGuard,
        IMediator mediator,
        ILogger<ReferenceViewModel> logger)
    {
        _searchService = searchService;
        _historyService = historyService;
        _licenseGuard = licenseGuard;
        _mediator = mediator;
        _logger = logger;

        // LOGIC: Initialize license state from guard.
        IsLicensed = _licenseGuard.IsSearchAvailable;

        // LOGIC: Load initial search history.
        RefreshSearchHistory();
    }

    // =========================================================================
    // Observable Properties
    // =========================================================================

    /// <summary>
    /// Gets or sets the current search query text.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private string _searchQuery = string.Empty;

    /// <summary>
    /// Gets or sets whether a search is currently in progress.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private bool _isSearching;

    /// <summary>
    /// Gets or sets the error message to display (if any).
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Gets or sets the duration of the last search operation.
    /// </summary>
    [ObservableProperty]
    private TimeSpan _lastSearchDuration;

    /// <summary>
    /// Gets the collection of search results.
    /// </summary>
    public ObservableCollection<SearchResultItemViewModel> Results { get; } = new();

    /// <summary>
    /// Gets the collection of recent search queries (for autocomplete).
    /// </summary>
    public ObservableCollection<string> SearchHistory { get; } = new();

    // =========================================================================
    // Computed Properties
    // =========================================================================

    /// <summary>
    /// Gets whether the semantic search feature is licensed.
    /// </summary>
    [ObservableProperty]
    private bool _isLicensed;

    /// <summary>
    /// Gets whether results are available to display.
    /// </summary>
    public bool HasResults => Results.Count > 0;

    /// <summary>
    /// Gets the count of results for display.
    /// </summary>
    public int ResultCount => Results.Count;

    /// <summary>
    /// Gets whether to show the "no results" message.
    /// </summary>
    public bool ShowNoResults => !IsSearching && !HasResults && !string.IsNullOrWhiteSpace(SearchQuery) && ErrorMessage is null;

    /// <summary>
    /// Gets whether the search command can execute.
    /// </summary>
    public bool CanSearch => !IsSearching && !string.IsNullOrWhiteSpace(SearchQuery) && IsLicensed;

    // =========================================================================
    // Commands
    // =========================================================================

    /// <summary>
    /// Executes a semantic search with the current query.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        var query = SearchQuery.Trim();

        _logger.LogInformation("Executing semantic search: {Query}", query);

        // LOGIC: Cancel any in-progress search.
        await CancelPreviousSearchAsync();

        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        IsSearching = true;
        ErrorMessage = null;
        Results.Clear();
        NotifyResultsChanged();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var options = SearchOptions.Default;
            var result = await _searchService.SearchAsync(query, options, ct);

            stopwatch.Stop();
            LastSearchDuration = stopwatch.Elapsed;

            _logger.LogInformation(
                "Search completed: {ResultCount} results in {Duration}ms",
                result.Count,
                stopwatch.ElapsedMilliseconds);

            // LOGIC: Add to history on successful search.
            _historyService.AddQuery(query);
            RefreshSearchHistory();

            // LOGIC: Populate results.
            foreach (var hit in result.Hits)
            {
                Results.Add(new SearchResultItemViewModel(hit, OnNavigateToResult));
            }

            NotifyResultsChanged();
        }
        catch (FeatureNotLicensedException ex)
        {
            _logger.LogWarning(ex, "Search blocked: feature not licensed");
            ErrorMessage = "Semantic search requires a WriterPro license.";
            IsLicensed = false;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug("Search cancelled by user");
            // No error message for user-initiated cancellation.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed: {Message}", ex.Message);
            ErrorMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
            NotifyResultsChanged();
        }
    }

    /// <summary>
    /// Clears the current search query and results.
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        _logger.LogDebug("Clearing search");
        SearchQuery = string.Empty;
        ErrorMessage = null;
        Results.Clear();
        NotifyResultsChanged();
    }

    /// <summary>
    /// Clears the search history.
    /// </summary>
    [RelayCommand]
    private void ClearHistory()
    {
        _logger.LogDebug("Clearing search history");
        _historyService.ClearHistory();
        RefreshSearchHistory();
    }

    /// <summary>
    /// Selects a query from the history and sets it as the current search.
    /// </summary>
    /// <param name="query">The query to select.</param>
    [RelayCommand]
    private void SelectHistoryItem(string query)
    {
        if (!string.IsNullOrWhiteSpace(query))
        {
            SearchQuery = query;
            _logger.LogDebug("Selected history item: {Query}", query);
        }
    }

    // =========================================================================
    // Private Methods
    // =========================================================================

    private async Task CancelPreviousSearchAsync()
    {
        if (_searchCts is not null)
        {
            await _searchCts.CancelAsync();
            _searchCts.Dispose();
            _searchCts = null;
        }
    }

    private void RefreshSearchHistory()
    {
        SearchHistory.Clear();
        foreach (var query in _historyService.GetRecentQueries())
        {
            SearchHistory.Add(query);
        }
    }

    private void NotifyResultsChanged()
    {
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(ResultCount));
        OnPropertyChanged(nameof(ShowNoResults));
        OnPropertyChanged(nameof(CanSearch));
        SearchCommand.NotifyCanExecuteChanged();
    }

    private void OnNavigateToResult(SearchHit hit)
    {
        // LOGIC: Navigation will be implemented in v0.4.6c via IReferenceNavigationService.
        // For now, just log the intent.
        _logger.LogInformation(
            "Navigate requested: {Document} at offset {Offset}",
            hit.Document.FilePath,
            hit.Chunk.StartOffset);
    }
}
