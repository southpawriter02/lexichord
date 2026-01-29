using AvaloniaEdit;
using Lexichord.Abstractions.Contracts.Editor;

namespace Lexichord.Modules.Editor.Services;

/// <summary>
/// Service for searching and replacing text in documents.
/// </summary>
/// <remarks>
/// LOGIC: ISearchService provides search functionality using AvalonEdit's
/// built-in search infrastructure. It supports:
///
/// - Text search with options (case, whole word, regex)
/// - Match highlighting in document
/// - Find next/previous navigation
/// - Replace and Replace All operations
///
/// The service is scoped to a single document/editor instance.
/// The interface lives in the module layer (not Abstractions) because
/// it depends on AvaloniaEdit's TextEditor type.
/// </remarks>
public interface ISearchService
{
    /// <summary>
    /// Shows the search overlay.
    /// </summary>
    void ShowSearch();

    /// <summary>
    /// Hides the search overlay.
    /// </summary>
    void HideSearch();

    /// <summary>
    /// Gets whether the search overlay is currently visible.
    /// </summary>
    bool IsSearchVisible { get; }

    /// <summary>
    /// Attaches the service to an editor instance.
    /// </summary>
    /// <param name="editor">The TextEditor control.</param>
    void AttachToEditor(TextEditor editor);

    /// <summary>
    /// Finds the next match from the current caret position.
    /// </summary>
    /// <param name="searchText">Text to search for.</param>
    /// <param name="options">Search options.</param>
    /// <returns>The search result, or null if no match found.</returns>
    SearchResult? FindNext(string searchText, SearchOptions options);

    /// <summary>
    /// Finds the previous match from the current caret position.
    /// </summary>
    /// <param name="searchText">Text to search for.</param>
    /// <param name="options">Search options.</param>
    /// <returns>The search result, or null if no match found.</returns>
    SearchResult? FindPrevious(string searchText, SearchOptions options);

    /// <summary>
    /// Finds all matches in the document.
    /// </summary>
    /// <param name="searchText">Text to search for.</param>
    /// <param name="options">Search options.</param>
    /// <returns>All matches found.</returns>
    IReadOnlyList<SearchResult> FindAll(string searchText, SearchOptions options);

    /// <summary>
    /// Replaces the current selection if it matches the search text.
    /// </summary>
    /// <param name="searchText">The search text to match.</param>
    /// <param name="replaceText">The replacement text.</param>
    /// <param name="options">Search options.</param>
    /// <returns>True if replacement was made.</returns>
    bool ReplaceCurrent(string searchText, string replaceText, SearchOptions options);

    /// <summary>
    /// Replaces all matches in the document.
    /// </summary>
    /// <param name="searchText">Text to search for.</param>
    /// <param name="replaceText">The replacement text.</param>
    /// <param name="options">Search options.</param>
    /// <returns>Number of replacements made.</returns>
    int ReplaceAll(string searchText, string replaceText, SearchOptions options);

    /// <summary>
    /// Highlights all matches in the document.
    /// </summary>
    /// <param name="searchText">Text to search for.</param>
    /// <param name="options">Search options.</param>
    /// <returns>Number of matches highlighted.</returns>
    int HighlightAllMatches(string searchText, SearchOptions options);

    /// <summary>
    /// Clears all search highlights from the document.
    /// </summary>
    void ClearHighlights();

    /// <summary>
    /// Gets the current match index (1-based).
    /// </summary>
    int CurrentMatchIndex { get; }

    /// <summary>
    /// Gets the total match count.
    /// </summary>
    int TotalMatchCount { get; }

    /// <summary>
    /// Event raised when search results change.
    /// </summary>
    event EventHandler<SearchResultsChangedEventArgs>? ResultsChanged;
}
