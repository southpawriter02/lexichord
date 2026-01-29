using System.Text.RegularExpressions;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Lexichord.Abstractions.Contracts.Editor;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Editor.Services;

/// <summary>
/// Service for searching and replacing text in the editor.
/// </summary>
/// <remarks>
/// LOGIC: SearchService wraps AvalonEdit's document and provides:
///
/// - Pattern matching with options (case, whole word, regex)
/// - Text marker-based highlighting for all matches
/// - Navigation between matches
/// - Replace operations with undo support
///
/// The service maintains state for the current search session.
/// </remarks>
public sealed class SearchService : ISearchService
{
    private readonly ILogger<SearchService> _logger;
    private TextEditor? _editor;
    private TextDocument? _document;
    private TextMarkerService? _markerService;

    private List<SearchResult> _matches = [];
    private int _currentMatchIndex;
    private string _lastSearchText = string.Empty;
    private SearchOptions _lastOptions = SearchOptions.Default;

    public SearchService(ILogger<SearchService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void AttachToEditor(TextEditor editor)
    {
        _editor = editor;
        _document = editor.Document;

        // LOGIC: Create text marker service for highlighting
        // Always create a new one when attaching to ensure clean state
        _markerService = new TextMarkerService(_document);
        editor.TextArea.TextView.BackgroundRenderers.Add(_markerService);

        _logger.LogDebug("SearchService attached to editor");
    }

    /// <inheritdoc/>
    public bool IsSearchVisible { get; private set; }

    /// <inheritdoc/>
    public int CurrentMatchIndex => _currentMatchIndex;

    /// <inheritdoc/>
    public int TotalMatchCount => _matches.Count;

    /// <inheritdoc/>
    public event EventHandler<SearchResultsChangedEventArgs>? ResultsChanged;

    /// <inheritdoc/>
    public void ShowSearch()
    {
        IsSearchVisible = true;
    }

    /// <inheritdoc/>
    public void HideSearch()
    {
        IsSearchVisible = false;
        ClearHighlights();
    }

    /// <inheritdoc/>
    public SearchResult? FindNext(string searchText, SearchOptions options)
    {
        EnsureMatchesUpdated(searchText, options);

        if (_matches.Count == 0)
            return null;

        // LOGIC: Find next match after current caret position
        var caretOffset = _editor?.CaretOffset ?? 0;

        var nextIndex = _matches.FindIndex(m => m.StartOffset > caretOffset);
        if (nextIndex == -1)
        {
            // Wrap to first match
            nextIndex = 0;
        }

        _currentMatchIndex = nextIndex + 1; // 1-based
        var match = _matches[nextIndex];

        SelectMatch(match);
        RaiseResultsChanged();

        return match;
    }

    /// <inheritdoc/>
    public SearchResult? FindPrevious(string searchText, SearchOptions options)
    {
        EnsureMatchesUpdated(searchText, options);

        if (_matches.Count == 0)
            return null;

        // LOGIC: Find previous match before current caret position
        var caretOffset = _editor?.CaretOffset ?? 0;

        var prevIndex = _matches.FindLastIndex(m => m.StartOffset < caretOffset);
        if (prevIndex == -1)
        {
            // Wrap to last match
            prevIndex = _matches.Count - 1;
        }

        _currentMatchIndex = prevIndex + 1; // 1-based
        var match = _matches[prevIndex];

        SelectMatch(match);
        RaiseResultsChanged();

        return match;
    }

    /// <inheritdoc/>
    public IReadOnlyList<SearchResult> FindAll(string searchText, SearchOptions options)
    {
        _matches = FindMatches(searchText, options).ToList();
        _lastSearchText = searchText;
        _lastOptions = options;
        _currentMatchIndex = 0;

        RaiseResultsChanged();
        return _matches;
    }

    /// <inheritdoc/>
    public bool ReplaceCurrent(string searchText, string replaceText, SearchOptions options)
    {
        if (_editor is null || _document is null)
            return false;

        EnsureMatchesUpdated(searchText, options);

        // LOGIC: Check if current selection matches the search
        var selection = _editor.SelectedText;
        if (!IsMatch(selection, searchText, options))
        {
            _logger.LogDebug("Current selection does not match search text");
            return false;
        }

        // LOGIC: Replace selection
        var offset = _editor.SelectionStart;
        _document.Replace(offset, selection.Length, replaceText);

        _logger.LogDebug("Replaced '{Old}' with '{New}' at offset {Offset}",
            selection, replaceText, offset);

        // LOGIC: Update matches (some may be invalidated)
        RefreshMatches(searchText, options);

        return true;
    }

    /// <inheritdoc/>
    public int ReplaceAll(string searchText, string replaceText, SearchOptions options)
    {
        if (_document is null)
            return 0;

        var matches = FindMatches(searchText, options).ToList();
        if (matches.Count == 0)
            return 0;

        // LOGIC: Replace from end to start to preserve offsets
        _document.BeginUpdate();
        try
        {
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var match = matches[i];
                _document.Replace(match.StartOffset, match.Length, replaceText);
            }
        }
        finally
        {
            _document.EndUpdate();
        }

        _logger.LogInformation("Replaced {Count} occurrences of '{Search}' with '{Replace}'",
            matches.Count, searchText, replaceText);

        // Clear matches after replace all
        _matches.Clear();
        _currentMatchIndex = 0;
        ClearHighlights();
        RaiseResultsChanged();

        return matches.Count;
    }

    /// <inheritdoc/>
    public int HighlightAllMatches(string searchText, SearchOptions options)
    {
        ClearHighlights();

        if (string.IsNullOrEmpty(searchText) || _markerService is null)
            return 0;

        _matches = FindMatches(searchText, options).ToList();
        _lastSearchText = searchText;
        _lastOptions = options;
        _currentMatchIndex = 0;

        // LOGIC: Create text markers for each match
        foreach (var match in _matches)
        {
            _markerService.Create(match.StartOffset, match.Length);
        }

        RaiseResultsChanged();

        _logger.LogDebug("Highlighted {Count} matches for '{Search}'", _matches.Count, searchText);

        return _matches.Count;
    }

    /// <inheritdoc/>
    public void ClearHighlights()
    {
        _markerService?.RemoveAll(_ => true);
        _matches.Clear();
        _currentMatchIndex = 0;
    }

    #region Private Methods

    private void EnsureMatchesUpdated(string searchText, SearchOptions options)
    {
        if (_lastSearchText != searchText || !_lastOptions.Equals(options))
        {
            FindAll(searchText, options);
        }
    }

    private void RefreshMatches(string searchText, SearchOptions options)
    {
        _matches = FindMatches(searchText, options).ToList();
        _lastSearchText = searchText;
        _lastOptions = options;

        // Re-highlight
        ClearHighlights();
        if (_markerService is not null)
        {
            foreach (var match in _matches)
            {
                _markerService.Create(match.StartOffset, match.Length);
            }
        }

        RaiseResultsChanged();
    }

    private IEnumerable<SearchResult> FindMatches(string searchText, SearchOptions options)
    {
        if (_document is null || string.IsNullOrEmpty(searchText))
            yield break;

        var text = _document.Text;
        Regex regex;

        try
        {
            regex = BuildSearchRegex(searchText, options);
        }
        catch (RegexParseException ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern: {Pattern}", searchText);
            yield break;
        }

        var matches = regex.Matches(text);
        foreach (Match match in matches)
        {
            var location = _document.GetLocation(match.Index);
            yield return new SearchResult(
                StartOffset: match.Index,
                Length: match.Length,
                MatchedText: match.Value,
                Line: location.Line,
                Column: location.Column
            );
        }
    }

    private static Regex BuildSearchRegex(string searchText, SearchOptions options)
    {
        var pattern = options.UseRegex
            ? searchText
            : Regex.Escape(searchText);

        if (options.WholeWord)
        {
            pattern = $@"\b{pattern}\b";
        }

        var regexOptions = RegexOptions.Compiled;
        if (!options.MatchCase)
        {
            regexOptions |= RegexOptions.IgnoreCase;
        }

        // LOGIC: Set timeout to prevent ReDoS
        return new Regex(pattern, regexOptions, TimeSpan.FromSeconds(1));
    }

    private bool IsMatch(string text, string searchText, SearchOptions options)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        try
        {
            var regex = BuildSearchRegex(searchText, options);
            return regex.IsMatch(text) && regex.Match(text).Value == text;
        }
        catch (RegexParseException)
        {
            return false;
        }
    }

    private void SelectMatch(SearchResult match)
    {
        if (_editor is null)
            return;

        _editor.Select(match.StartOffset, match.Length);
        _editor.ScrollTo(match.Line, match.Column);
    }

    private void RaiseResultsChanged()
    {
        ResultsChanged?.Invoke(this, new SearchResultsChangedEventArgs
        {
            TotalMatches = _matches.Count,
            CurrentIndex = _currentMatchIndex,
            SearchText = _lastSearchText
        });
    }

    #endregion
}
