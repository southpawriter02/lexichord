namespace Lexichord.Abstractions.Contracts.Commands;

/// <summary>
/// Represents a range of matched characters for highlighting.
/// </summary>
/// <param name="Start">Start index in the text (0-based, inclusive).</param>
/// <param name="Length">Number of characters matched.</param>
/// <remarks>
/// LOGIC: Used by the Command Palette to highlight matched portions
/// of command titles and file names when displaying fuzzy search results.
/// </remarks>
public record MatchPosition(int Start, int Length);
