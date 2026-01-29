using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Editor.ViewModels;

/// <summary>
/// ViewModel for the search and replace overlay.
/// </summary>
/// <remarks>
/// LOGIC: SearchOverlayViewModel manages the search UI state and coordinates
/// with ISearchService for actual search operations. It supports:
///
/// - Live search as user types (debounced)
/// - Find next/previous navigation
/// - Replace and Replace All
/// - Search options (case, whole word, regex)
///
/// The ViewModel is scoped to a single ManuscriptViewModel instance.
/// </remarks>
public partial class SearchOverlayViewModel : ObservableObject
{
    private readonly Services.ISearchService _searchService;
    private readonly IMediator _mediator;
    private readonly ILogger<SearchOverlayViewModel> _logger;
    private readonly string _documentId;

    private CancellationTokenSource? _debounceTokenSource;
    private const int DebounceDelayMs = 150;

    public SearchOverlayViewModel(
        Services.ISearchService searchService,
        IMediator mediator,
        ILogger<SearchOverlayViewModel> logger,
        string documentId)
    {
        _searchService = searchService;
        _mediator = mediator;
        _logger = logger;
        _documentId = documentId;

        // LOGIC: Subscribe to search results changes
        _searchService.ResultsChanged += OnSearchResultsChanged;
    }

    #region Search Properties

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FindNextCommand))]
    [NotifyCanExecuteChangedFor(nameof(FindPreviousCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReplaceNextCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReplaceAllCommand))]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _replaceText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchOptions))]
    private bool _matchCase;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchOptions))]
    private bool _wholeWord;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchOptions))]
    private bool _useRegex;

    [ObservableProperty]
    private bool _isReplaceVisible;

    [ObservableProperty]
    private string _matchStatus = string.Empty;

    [ObservableProperty]
    private bool _hasNoResults;

    /// <summary>
    /// Gets the current search options.
    /// </summary>
    public SearchOptions SearchOptions => new(MatchCase, WholeWord, UseRegex);

    #endregion

    #region Commands

    /// <summary>
    /// Command to find the next match.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private void FindNext()
    {
        _logger.LogDebug("Find next: {SearchText}", SearchText);

        var result = _searchService.FindNext(SearchText, SearchOptions);
        HasNoResults = result is null;

        if (result is null)
        {
            _logger.LogDebug("No matches found");
        }
    }

    /// <summary>
    /// Command to find the previous match.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private void FindPrevious()
    {
        _logger.LogDebug("Find previous: {SearchText}", SearchText);

        var result = _searchService.FindPrevious(SearchText, SearchOptions);
        HasNoResults = result is null;
    }

    /// <summary>
    /// Command to replace the current match and find next.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private void ReplaceNext()
    {
        _logger.LogDebug("Replace: {SearchText} -> {ReplaceText}", SearchText, ReplaceText);

        var replaced = _searchService.ReplaceCurrent(SearchText, ReplaceText, SearchOptions);
        if (replaced)
        {
            // Find next match after replacement
            _searchService.FindNext(SearchText, SearchOptions);
        }
    }

    /// <summary>
    /// Command to replace all matches.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task ReplaceAllAsync()
    {
        _logger.LogDebug("Replace all: {SearchText} -> {ReplaceText}", SearchText, ReplaceText);

        var count = _searchService.ReplaceAll(SearchText, ReplaceText, SearchOptions);

        _logger.LogInformation("Replaced {Count} matches", count);

        // LOGIC: Publish event for analytics/logging
        await _mediator.Publish(new SearchExecutedEvent(
            DocumentId: _documentId,
            SearchText: SearchText,
            MatchCount: count,
            Options: SearchOptions
        ));
    }

    /// <summary>
    /// Command to toggle replace panel visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleReplace()
    {
        IsReplaceVisible = !IsReplaceVisible;
    }

    /// <summary>
    /// Command to hide the search overlay.
    /// </summary>
    [RelayCommand]
    private void Hide()
    {
        _logger.LogDebug("Hiding search overlay");
        _searchService.ClearHighlights();
        _searchService.HideSearch();
    }

    private bool CanSearch() => !string.IsNullOrEmpty(SearchText);

    #endregion

    #region Search Text Change Handling

    partial void OnSearchTextChanged(string value)
    {
        // LOGIC: Debounce search to avoid excessive updates while typing
        _debounceTokenSource?.Cancel();
        _debounceTokenSource = new CancellationTokenSource();

        _ = DebounceSearchAsync(value, _debounceTokenSource.Token);
    }

    private async Task DebounceSearchAsync(string searchText, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(DebounceDelayMs, cancellationToken);

            if (string.IsNullOrEmpty(searchText))
            {
                _searchService.ClearHighlights();
                MatchStatus = string.Empty;
                HasNoResults = false;
            }
            else
            {
                var count = _searchService.HighlightAllMatches(searchText, SearchOptions);
                HasNoResults = count == 0;
            }
        }
        catch (OperationCanceledException)
        {
            // Debounce cancelled, ignore
        }
    }

    partial void OnMatchCaseChanged(bool value) => RefreshSearch();
    partial void OnWholeWordChanged(bool value) => RefreshSearch();
    partial void OnUseRegexChanged(bool value) => RefreshSearch();

    private void RefreshSearch()
    {
        if (!string.IsNullOrEmpty(SearchText))
        {
            var count = _searchService.HighlightAllMatches(SearchText, SearchOptions);
            HasNoResults = count == 0;
        }
    }

    #endregion

    #region Event Handlers

    private void OnSearchResultsChanged(object? sender, SearchResultsChangedEventArgs e)
    {
        // LOGIC: Update match status display
        if (e.TotalMatches == 0)
        {
            MatchStatus = "No results";
        }
        else if (e.CurrentIndex == 0)
        {
            MatchStatus = $"{e.TotalMatches} matches";
        }
        else
        {
            MatchStatus = $"{e.CurrentIndex} of {e.TotalMatches}";
        }
    }

    #endregion
}
