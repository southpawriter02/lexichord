// =============================================================================
// File: ReferenceViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel for the Reference Panel (multi-mode search UI).
// =============================================================================
// LOGIC: Orchestrates the Reference Panel's search functionality.
//   1. SearchQuery property (two-way bound to search input).
//   2. SelectedSearchMode property controls which search strategy is used.
//   3. SearchCommand dispatches to the appropriate service based on selected mode:
//      - Semantic: ISemanticSearchService (v0.4.5a)
//      - Keyword: IBM25SearchService (v0.5.1b)
//      - Hybrid: IHybridSearchService (v0.5.1c)
//   4. Results displayed via ObservableCollection of SearchResultItemViewModel.
//   5. License gating via SearchLicenseGuard:
//      - All search modes require WriterPro+ (existing behavior).
//      - Hybrid mode additionally checks CanUseHybrid().
//   6. Search mode preference persisted via ISystemSettingsRepository.
//   7. Search history via ISearchHistoryService (autocomplete dropdown).
//   8. Error handling for FeatureNotLicensedException, OperationCanceledException.
//   9. SearchModeChangedEvent published on mode change for telemetry.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.5a: ISemanticSearchService, SearchResult, SearchHit, SearchOptions
//   - v0.4.5d: SearchLicenseGuard
//   - v0.4.6a: ISearchHistoryService
//   - v0.5.1b: IBM25SearchService
//   - v0.5.1c: IHybridSearchService
//   - v0.5.1d: SearchMode, SearchModeChangedEvent, FeatureCodes.HybridSearch
//   - v0.0.4c: FeatureNotLicensedException, ILicenseContext
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
/// ViewModel for the Reference Panel, providing multi-mode search functionality.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ReferenceViewModel"/> powers the Reference Panel UI, enabling users
/// to perform searches against indexed documents using one of three strategies:
/// Semantic (vector similarity), Keyword (BM25 full-text), or Hybrid (RRF fusion).
/// It manages search execution, result display, mode selection, search history,
/// and license validation.
/// </para>
/// <para>
/// <b>Search Modes (v0.5.1d):</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="SearchMode.Semantic"/>: Vector similarity via pgvector (v0.4.5a). Available to all licensed tiers.</description></item>
///   <item><description><see cref="SearchMode.Keyword"/>: BM25 full-text via ts_rank (v0.5.1b). Available to all licensed tiers.</description></item>
///   <item><description><see cref="SearchMode.Hybrid"/>: RRF fusion of both (v0.5.1c). Requires WriterPro+.</description></item>
/// </list>
/// <para>
/// <b>License Gating:</b> All search requires WriterPro tier. Hybrid mode additionally
/// verifies WriterPro+ tier via <see cref="CanUseHybrid"/>.
/// Core users see a warning and search is disabled. WriterPro+ users who lack the
/// appropriate tier will have Hybrid mode locked.
/// </para>
/// <para>
/// <b>Preference Persistence:</b> The selected search mode is persisted via
/// <see cref="ISystemSettingsRepository"/> under the key "Search.DefaultMode".
/// On initialization, the persisted mode is restored if valid for the current tier.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6a. <b>Enhanced in:</b> v0.5.1d (multi-mode search).
/// </para>
/// </remarks>
public partial class ReferenceViewModel : ObservableObject
{
    // =========================================================================
    // Constants
    // =========================================================================

    /// <summary>
    /// Settings key for persisting the user's preferred search mode.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used with <see cref="ISystemSettingsRepository"/> for cross-session
    /// retention of the selected search mode. The value stored is the string
    /// representation of the <see cref="SearchMode"/> enum.
    /// </remarks>
    internal const string SearchModeSettingsKey = "Search.DefaultMode";

    // =========================================================================
    // Dependencies
    // =========================================================================

    private readonly ISemanticSearchService _semanticSearchService;
    private readonly IBM25SearchService _bm25SearchService;
    private readonly IHybridSearchService _hybridSearchService;
    private readonly ISearchHistoryService _historyService;
    private readonly IReferenceNavigationService _navigationService;
    private readonly SearchLicenseGuard _licenseGuard;
    private readonly ILicenseContext _licenseContext;
    private readonly ISystemSettingsRepository? _settingsRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<ReferenceViewModel> _logger;
    private CancellationTokenSource? _searchCts;

    /// <summary>
    /// Creates a new <see cref="ReferenceViewModel"/> instance.
    /// </summary>
    /// <param name="semanticSearchService">Service for executing semantic (vector) searches (v0.4.5a).</param>
    /// <param name="bm25SearchService">Service for executing BM25 (keyword) searches (v0.5.1b).</param>
    /// <param name="hybridSearchService">Service for executing hybrid (RRF fusion) searches (v0.5.1c).</param>
    /// <param name="historyService">Service for managing search history.</param>
    /// <param name="navigationService">Service for navigating to search result sources (v0.4.6c).</param>
    /// <param name="licenseGuard">Guard for validating license tier for search operations.</param>
    /// <param name="licenseContext">License context for tier-specific behavior (v0.0.4c).</param>
    /// <param name="settingsRepository">Settings repository for preference persistence. May be null if unavailable.</param>
    /// <param name="mediator">MediatR instance for publishing events.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ReferenceViewModel(
        ISemanticSearchService semanticSearchService,
        IBM25SearchService bm25SearchService,
        IHybridSearchService hybridSearchService,
        ISearchHistoryService historyService,
        IReferenceNavigationService navigationService,
        SearchLicenseGuard licenseGuard,
        ILicenseContext licenseContext,
        ISystemSettingsRepository? settingsRepository,
        IMediator mediator,
        ILogger<ReferenceViewModel> logger)
    {
        _semanticSearchService = semanticSearchService ?? throw new ArgumentNullException(nameof(semanticSearchService));
        _bm25SearchService = bm25SearchService ?? throw new ArgumentNullException(nameof(bm25SearchService));
        _hybridSearchService = hybridSearchService ?? throw new ArgumentNullException(nameof(hybridSearchService));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _licenseGuard = licenseGuard ?? throw new ArgumentNullException(nameof(licenseGuard));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _settingsRepository = settingsRepository; // LOGIC: Nullable — settings are optional (in-memory fallback).
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Initialize license state from guard.
        IsLicensed = _licenseGuard.IsSearchAvailable;

        // LOGIC: Initialize hybrid lock state based on license context.
        IsHybridLocked = !CanUseHybrid();

        _logger.LogDebug(
            "ReferenceViewModel initialized: IsLicensed={IsLicensed}, IsHybridLocked={IsHybridLocked}, Tier={Tier}",
            IsLicensed,
            IsHybridLocked,
            _licenseContext.GetCurrentTier());

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
    /// Gets or sets the currently selected search mode.
    /// </summary>
    /// <remarks>
    /// LOGIC: Changing this property triggers:
    ///   1. License check (Hybrid requires WriterPro+).
    ///   2. Preference persistence via ISystemSettingsRepository.
    ///   3. SearchModeChangedEvent publication via MediatR.
    ///   4. If Hybrid is denied, reverts to Semantic.
    /// Introduced in v0.5.1d.
    /// </remarks>
    [ObservableProperty]
    private SearchMode _selectedSearchMode;

    /// <summary>
    /// Gets or sets whether the Hybrid search mode is locked due to license restrictions.
    /// </summary>
    /// <remarks>
    /// LOGIC: When <c>true</c>, the Hybrid option in the mode dropdown shows a lock icon
    /// and selecting it triggers an upgrade prompt instead of switching modes.
    /// Introduced in v0.5.1d.
    /// </remarks>
    [ObservableProperty]
    private bool _isHybridLocked;

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
    /// Gets whether the search feature is licensed (WriterPro+).
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

    /// <summary>
    /// Gets all available search mode values for the UI dropdown.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns all <see cref="SearchMode"/> enum values. The UI uses this
    /// as the ItemsSource for the mode dropdown. Lock icon visibility is controlled
    /// by <see cref="IsHybridLocked"/> rather than filtering the list.
    /// </remarks>
    public SearchMode[] AvailableSearchModes { get; } = Enum.GetValues<SearchMode>();

    // =========================================================================
    // Property Change Handlers
    // =========================================================================

    /// <summary>
    /// Invoked after <see cref="SelectedSearchMode"/> has changed.
    /// </summary>
    /// <param name="value">The new search mode value.</param>
    /// <remarks>
    /// LOGIC: Handles the search mode change lifecycle:
    ///   1. If Hybrid is selected but not licensed, log warning and revert to Semantic.
    ///   2. Persist the new mode preference via ISystemSettingsRepository.
    ///   3. Publish SearchModeChangedEvent for telemetry.
    ///   4. Log the mode change for diagnostics.
    /// </remarks>
    partial void OnSelectedSearchModeChanged(SearchMode value)
    {
        // LOGIC: If Hybrid was selected but user lacks license, revert to Semantic.
        if (value == SearchMode.Hybrid && !CanUseHybrid())
        {
            _logger.LogWarning(
                "Hybrid search denied: license tier {Tier} insufficient. Reverting to Semantic.",
                _licenseContext.GetCurrentTier());

            // LOGIC: Fire-and-forget the upgrade prompt event.
            _ = PublishSearchDeniedAsync();

            // LOGIC: Revert to Semantic on next dispatcher cycle to avoid re-entrant property change.
            SelectedSearchMode = SearchMode.Semantic;
            return;
        }

        _logger.LogDebug(
            "Search mode changed: {NewMode} (tier: {Tier})",
            value,
            _licenseContext.GetCurrentTier());

        // LOGIC: Persist the preference (fire-and-forget).
        _ = PersistSearchModeAsync(value);

        // LOGIC: Publish mode change event for telemetry (fire-and-forget).
        _ = PublishSearchModeChangedAsync(value);
    }

    // =========================================================================
    // Commands
    // =========================================================================

    /// <summary>
    /// Executes a search using the currently selected search mode.
    /// </summary>
    /// <remarks>
    /// LOGIC: Dispatches to the appropriate search service based on <see cref="SelectedSearchMode"/>:
    ///   - <see cref="SearchMode.Semantic"/>: <see cref="ISemanticSearchService"/>
    ///   - <see cref="SearchMode.Keyword"/>: <see cref="IBM25SearchService"/>
    ///   - <see cref="SearchMode.Hybrid"/>: <see cref="IHybridSearchService"/>
    /// All modes use the same <see cref="SearchOptions"/> and return <see cref="SearchResult"/>.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        var query = SearchQuery.Trim();
        var mode = SelectedSearchMode;

        _logger.LogInformation(
            "Executing {Mode} search: {Query}",
            mode,
            query);

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

            // LOGIC: Dispatch to the appropriate search service based on selected mode.
            var result = mode switch
            {
                SearchMode.Semantic => await _semanticSearchService.SearchAsync(query, options, ct),
                SearchMode.Keyword => await _bm25SearchService.SearchAsync(query, options, ct),
                SearchMode.Hybrid => await _hybridSearchService.SearchAsync(query, options, ct),
                _ => throw new InvalidOperationException($"Unknown search mode: {mode}")
            };

            stopwatch.Stop();
            LastSearchDuration = stopwatch.Elapsed;

            _logger.LogInformation(
                "{Mode} search completed: {ResultCount} results in {Duration}ms",
                mode,
                result.Count,
                stopwatch.ElapsedMilliseconds);

            // LOGIC: Add to history on successful search.
            _historyService.AddQuery(query);
            RefreshSearchHistory();

            // LOGIC: Populate results with query for term highlighting (v0.4.6b).
            foreach (var hit in result.Hits)
            {
                Results.Add(new SearchResultItemViewModel(hit, OnNavigateToResult, query));
            }

            NotifyResultsChanged();
        }
        catch (FeatureNotLicensedException ex)
        {
            _logger.LogWarning(ex, "Search blocked: feature not licensed for {Mode} mode", mode);
            ErrorMessage = mode switch
            {
                SearchMode.Hybrid => "Hybrid search requires a WriterPro license. Try Semantic or Keyword mode.",
                _ => "Search requires a WriterPro license."
            };
            if (mode == SearchMode.Hybrid)
            {
                // LOGIC: If Hybrid was denied at the service level, lock it and revert.
                IsHybridLocked = true;
                SelectedSearchMode = SearchMode.Semantic;
            }
            else
            {
                IsLicensed = false;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug("Search cancelled by user");
            // No error message for user-initiated cancellation.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Mode} search failed: {Message}", mode, ex.Message);
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
    // Initialization
    // =========================================================================

    /// <summary>
    /// Initializes the search mode from persisted settings or license-appropriate defaults.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: Called during panel initialization to restore the user's preferred search mode.
    /// The initialization logic follows this decision tree:
    /// </para>
    /// <list type="number">
    ///   <item><description>Update <see cref="IsHybridLocked"/> based on current license.</description></item>
    ///   <item><description>Attempt to load persisted mode from <see cref="ISystemSettingsRepository"/>.</description></item>
    ///   <item><description>If persisted mode is valid for current tier, use it.</description></item>
    ///   <item><description>Otherwise, default to Hybrid for WriterPro+ or Semantic for Core.</description></item>
    /// </list>
    /// <para>
    /// <b>Introduced:</b> v0.5.1d.
    /// </para>
    /// </remarks>
    public async Task InitializeSearchModeAsync()
    {
        _logger.LogDebug("Initializing search mode");

        // LOGIC: Refresh lock state in case license changed since construction.
        IsHybridLocked = !CanUseHybrid();

        // LOGIC: Attempt to load persisted preference.
        if (_settingsRepository is not null)
        {
            try
            {
                var savedMode = await _settingsRepository.GetValueAsync(
                    SearchModeSettingsKey,
                    string.Empty);

                if (!string.IsNullOrEmpty(savedMode) &&
                    Enum.TryParse<SearchMode>(savedMode, out var mode))
                {
                    // LOGIC: Only apply the persisted mode if it's valid for the current tier.
                    if (mode != SearchMode.Hybrid || CanUseHybrid())
                    {
                        _logger.LogDebug("Restored persisted search mode: {Mode}", mode);
                        SelectedSearchMode = mode;
                        return;
                    }

                    _logger.LogDebug(
                        "Persisted mode {Mode} not available for tier {Tier}, using default",
                        mode,
                        _licenseContext.GetCurrentTier());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load persisted search mode, using default");
            }
        }

        // LOGIC: Apply tier-appropriate default.
        SelectedSearchMode = CanUseHybrid() ? SearchMode.Hybrid : SearchMode.Semantic;

        _logger.LogDebug(
            "Search mode initialized to default: {Mode} (tier: {Tier})",
            SelectedSearchMode,
            _licenseContext.GetCurrentTier());
    }

    // =========================================================================
    // Private Methods
    // =========================================================================

    /// <summary>
    /// Checks whether the current license tier supports Hybrid search mode.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the user has WriterPro+ tier;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Hybrid search requires WriterPro license tier, checked via both
    /// SearchLicenseGuard.IsSearchAvailable and direct tier comparison.
    /// </remarks>
    internal bool CanUseHybrid()
    {
        return _licenseGuard.IsSearchAvailable &&
               _licenseContext.GetCurrentTier() >= LicenseTier.WriterPro;
    }

    /// <summary>
    /// Persists the selected search mode to the settings repository.
    /// </summary>
    /// <param name="mode">The search mode to persist.</param>
    private async Task PersistSearchModeAsync(SearchMode mode)
    {
        if (_settingsRepository is null)
        {
            _logger.LogDebug("Settings repository unavailable; search mode not persisted");
            return;
        }

        try
        {
            await _settingsRepository.SetValueAsync(
                SearchModeSettingsKey,
                mode.ToString(),
                "User's preferred search mode (Semantic, Keyword, or Hybrid)");

            _logger.LogDebug("Search mode persisted: {Mode}", mode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist search mode: {Mode}", mode);
        }
    }

    /// <summary>
    /// Publishes a <see cref="SearchModeChangedEvent"/> via MediatR.
    /// </summary>
    /// <param name="newMode">The new search mode.</param>
    private async Task PublishSearchModeChangedAsync(SearchMode newMode)
    {
        try
        {
            await _mediator.Publish(new SearchModeChangedEvent
            {
                PreviousMode = SelectedSearchMode,
                NewMode = newMode,
                LicenseTier = _licenseContext.GetCurrentTier()
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish SearchModeChangedEvent");
        }
    }

    /// <summary>
    /// Publishes a <see cref="SearchDeniedEvent"/> for a Hybrid mode denial.
    /// </summary>
    private async Task PublishSearchDeniedAsync()
    {
        try
        {
            await _mediator.Publish(new SearchDeniedEvent
            {
                CurrentTier = _licenseContext.GetCurrentTier(),
                RequiredTier = LicenseTier.WriterPro,
                FeatureName = "Hybrid Search"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish SearchDeniedEvent for Hybrid mode");
        }
    }

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

    /// <summary>
    /// Handles navigation to a search result's source document.
    /// </summary>
    /// <param name="hit">The search hit to navigate to.</param>
    /// <remarks>
    /// LOGIC: Delegates to <see cref="IReferenceNavigationService.NavigateToHitAsync"/>
    /// which opens the document, scrolls to the match, and highlights the text span.
    /// Uses async void because this is a fire-and-forget UI event callback.
    ///
    /// Version: v0.4.6c
    /// </remarks>
    private async void OnNavigateToResult(SearchHit hit)
    {
        _logger.LogDebug(
            "Navigate requested: {Document} at offset {Offset}",
            hit.Document.FilePath,
            hit.Chunk.StartOffset);

        try
        {
            var success = await _navigationService.NavigateToHitAsync(hit);
            if (!success)
            {
                _logger.LogWarning(
                    "Navigation failed for {Document} at offset {Offset}",
                    hit.Document.FilePath,
                    hit.Chunk.StartOffset);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation error for {Document}", hit.Document.FilePath);
        }
    }
}
