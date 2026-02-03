// =============================================================================
// File: HighlightRenderer.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of IHighlightRenderer for styled text generation.
// =============================================================================
// LOGIC: Transforms Snippet with HighlightSpan positions into StyledTextRun list.
//   - Sorts highlight spans by position to ensure correct ordering.
//   - Splits text into runs at highlight boundaries.
//   - Applies style based on HighlightType (QueryMatch=bold, FuzzyMatch=italic).
//   - Handles truncation ellipsis at start/end.
//   - Validates spans are within text bounds.
// =============================================================================
// VERSION: v0.5.6b (Query Term Highlighting)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Rendering;

/// <summary>
/// Renders <see cref="Snippet"/> content with styled highlights.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="HighlightRenderer"/> implements <see cref="IHighlightRenderer"/> to
/// transform snippet text with highlight positions into a sequence of styled runs
/// suitable for UI rendering.
/// </para>
/// <para>
/// <b>Algorithm:</b>
/// <list type="number">
///   <item><description>Sort highlight spans by start position.</description></item>
///   <item><description>If truncated at start, prepend ellipsis run.</description></item>
///   <item><description>Iterate through text, creating runs for unhighlighted and highlighted segments.</description></item>
///   <item><description>Apply style based on highlight type and theme colors.</description></item>
///   <item><description>If truncated at end, append ellipsis run.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6b as part of The Answer Preview feature.
/// </para>
/// </remarks>
public sealed class HighlightRenderer : IHighlightRenderer
{
    private readonly ILogger<HighlightRenderer> _logger;

    /// <summary>
    /// The ellipsis string used for truncated snippets.
    /// </summary>
    private const string Ellipsis = "...";

    /// <summary>
    /// Initializes a new instance of <see cref="HighlightRenderer"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public HighlightRenderer(ILogger<HighlightRenderer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IReadOnlyList<StyledTextRun> Render(Snippet snippet, HighlightTheme theme)
    {
        ArgumentNullException.ThrowIfNull(snippet);
        ArgumentNullException.ThrowIfNull(theme);

        var runs = new List<StyledTextRun>();
        var text = snippet.Text;

        // LOGIC: Handle empty text case.
        if (string.IsNullOrEmpty(text))
        {
            _logger.LogDebug("Rendering empty snippet");
            return runs;
        }

        // LOGIC: Add leading ellipsis for truncated start.
        if (snippet.IsTruncatedStart)
        {
            runs.Add(CreateEllipsisRun(theme));
        }

        // LOGIC: If no highlights, return single run with full text.
        if (!snippet.HasHighlights)
        {
            runs.Add(new StyledTextRun(text, TextStyle.Default));
            
            if (snippet.IsTruncatedEnd)
            {
                runs.Add(CreateEllipsisRun(theme));
            }
            
            return runs;
        }

        // LOGIC: Sort highlights by start position.
        var sortedHighlights = snippet.Highlights
            .OrderBy(h => h.Start)
            .ToList();

        // LOGIC: Build runs by iterating through text and highlights.
        var currentPosition = 0;

        foreach (var highlight in sortedHighlights)
        {
            // LOGIC: Validate highlight bounds.
            if (highlight.Start < 0 || highlight.Start >= text.Length ||
                highlight.Start + highlight.Length > text.Length)
            {
                _logger.LogWarning(
                    "Skipping invalid highlight span: Start={Start}, Length={Length}, TextLength={TextLength}",
                    highlight.Start, highlight.Length, text.Length);
                continue;
            }

            // LOGIC: Add unhighlighted text before this highlight.
            if (highlight.Start > currentPosition)
            {
                var unhighlightedText = text[currentPosition..highlight.Start];
                runs.Add(new StyledTextRun(unhighlightedText, TextStyle.Default));
            }

            // LOGIC: Add highlighted text.
            var highlightText = text.Substring(highlight.Start, highlight.Length);
            var style = GetStyleForType(highlight.Type, theme);
            runs.Add(new StyledTextRun(highlightText, style));

            currentPosition = highlight.Start + highlight.Length;
        }

        // LOGIC: Add any remaining unhighlighted text after last highlight.
        if (currentPosition < text.Length)
        {
            var remainingText = text[currentPosition..];
            runs.Add(new StyledTextRun(remainingText, TextStyle.Default));
        }

        // LOGIC: Add trailing ellipsis for truncated end.
        if (snippet.IsTruncatedEnd)
        {
            runs.Add(CreateEllipsisRun(theme));
        }

        _logger.LogDebug(
            "Rendered snippet with {HighlightCount} highlights into {RunCount} runs",
            snippet.Highlights.Count, runs.Count);

        return runs;
    }

    /// <inheritdoc />
    public bool ValidateHighlights(Snippet snippet)
    {
        ArgumentNullException.ThrowIfNull(snippet);

        if (string.IsNullOrEmpty(snippet.Text))
        {
            // LOGIC: Empty text with no highlights is valid.
            return !snippet.HasHighlights;
        }

        foreach (var highlight in snippet.Highlights)
        {
            // LOGIC: Check start is non-negative.
            if (highlight.Start < 0)
            {
                _logger.LogDebug("Invalid highlight: negative start {Start}", highlight.Start);
                return false;
            }

            // LOGIC: Check length is positive.
            if (highlight.Length <= 0)
            {
                _logger.LogDebug("Invalid highlight: non-positive length {Length}", highlight.Length);
                return false;
            }

            // LOGIC: Check end doesn't exceed text length.
            if (highlight.Start + highlight.Length > snippet.Text.Length)
            {
                _logger.LogDebug(
                    "Invalid highlight: end {End} exceeds text length {TextLength}",
                    highlight.Start + highlight.Length, snippet.Text.Length);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the text style for a given highlight type, applying theme colors.
    /// </summary>
    /// <param name="type">The type of highlight.</param>
    /// <param name="theme">The color theme.</param>
    /// <returns>A <see cref="TextStyle"/> with appropriate styling.</returns>
    private static TextStyle GetStyleForType(HighlightType type, HighlightTheme theme)
    {
        return type switch
        {
            HighlightType.QueryMatch => new TextStyle(
                IsBold: true,
                IsItalic: false,
                ForegroundColor: theme.ExactMatchForeground,
                BackgroundColor: theme.ExactMatchBackground),
            
            HighlightType.FuzzyMatch => new TextStyle(
                IsBold: false,
                IsItalic: true,
                ForegroundColor: theme.FuzzyMatchForeground,
                BackgroundColor: theme.FuzzyMatchBackground),
            
            HighlightType.KeyPhrase => new TextStyle(
                IsBold: false,
                IsItalic: false,
                ForegroundColor: theme.KeyPhraseForeground,
                BackgroundColor: null),
            
            _ => TextStyle.Default
        };
    }

    /// <summary>
    /// Creates an ellipsis run with theme-appropriate styling.
    /// </summary>
    /// <param name="theme">The color theme.</param>
    /// <returns>A styled run containing "...".</returns>
    private static StyledTextRun CreateEllipsisRun(HighlightTheme theme)
    {
        var style = new TextStyle(
            IsBold: false,
            IsItalic: false,
            ForegroundColor: theme.EllipsisColor,
            BackgroundColor: null);
        
        return new StyledTextRun(Ellipsis, style);
    }
}
