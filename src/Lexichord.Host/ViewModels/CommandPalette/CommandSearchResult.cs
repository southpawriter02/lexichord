using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Commands;

namespace Lexichord.Host.ViewModels.CommandPalette;

/// <summary>
/// Represents a search result in the Command Palette.
/// </summary>
/// <remarks>
/// LOGIC: CommandSearchResult wraps a CommandDefinition with:
/// - Fuzzy match score for ranking
/// - Match positions for highlighting
/// - Display-ready properties
///
/// Results are sorted by score (descending), then by title.
/// Higher scores indicate better matches.
/// </remarks>
public record CommandSearchResult
{
    /// <summary>
    /// Gets the underlying command definition.
    /// </summary>
    public required CommandDefinition Command { get; init; }

    /// <summary>
    /// Gets the fuzzy match score (0-100).
    /// </summary>
    /// <remarks>
    /// LOGIC: Score from FuzzySharp.
    /// 100 = exact match
    /// 0 = no match
    /// Typical threshold for display: >= 40
    /// </remarks>
    public int Score { get; init; }

    /// <summary>
    /// Gets the positions of matched characters in the title.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for highlight rendering.
    /// Each MatchPosition indicates a range of characters that matched.
    /// </remarks>
    public IReadOnlyList<MatchPosition> TitleMatches { get; init; } = [];

    /// <summary>
    /// Gets the positions of matched characters in the category.
    /// </summary>
    public IReadOnlyList<MatchPosition> CategoryMatches { get; init; } = [];

    /// <summary>
    /// Gets the formatted keyboard shortcut string.
    /// </summary>
    /// <remarks>
    /// LOGIC: Pre-formatted for display (e.g., "Ctrl+S").
    /// Null if no shortcut assigned.
    /// </remarks>
    public string? ShortcutDisplay { get; init; }

    /// <summary>
    /// Gets the icon kind for display.
    /// </summary>
    public string IconKind => Command.IconKind ?? "ConsoleLine";

    /// <summary>
    /// Gets the title for display.
    /// </summary>
    public string Title => Command.Title;

    /// <summary>
    /// Gets the category for display.
    /// </summary>
    public string Category => Command.Category;

    /// <summary>
    /// Gets the description for tooltip.
    /// </summary>
    public string? Description => Command.Description;
}
