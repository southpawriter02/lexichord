namespace Lexichord.Abstractions.Contracts.Editor;

/// <summary>
/// Options for search operations.
/// </summary>
/// <param name="MatchCase">Whether to match case exactly.</param>
/// <param name="WholeWord">Whether to match whole words only.</param>
/// <param name="UseRegex">Whether to interpret search text as regex.</param>
public record SearchOptions(
    bool MatchCase = false,
    bool WholeWord = false,
    bool UseRegex = false
)
{
    /// <summary>
    /// Default search options (case-insensitive, partial match, no regex).
    /// </summary>
    public static SearchOptions Default { get; } = new();
}

/// <summary>
/// Represents a single search match.
/// </summary>
/// <param name="StartOffset">Start position in document.</param>
/// <param name="Length">Length of matched text.</param>
/// <param name="MatchedText">The matched text content.</param>
/// <param name="Line">Line number (1-based).</param>
/// <param name="Column">Column number (1-based).</param>
public record SearchResult(
    int StartOffset,
    int Length,
    string MatchedText,
    int Line,
    int Column
);

/// <summary>
/// Event arguments for search results changes.
/// </summary>
public class SearchResultsChangedEventArgs : EventArgs
{
    /// <summary>
    /// Total number of matches.
    /// </summary>
    public required int TotalMatches { get; init; }

    /// <summary>
    /// Current match index (1-based), or 0 if no current match.
    /// </summary>
    public required int CurrentIndex { get; init; }

    /// <summary>
    /// The current search text.
    /// </summary>
    public string? SearchText { get; init; }
}
