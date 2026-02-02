// =============================================================================
// File: HighlightedTextBlock.cs
// Project: Lexichord.Modules.RAG
// Description: Custom TextBlock control that highlights specified terms in text.
// =============================================================================
// LOGIC: Extends Avalonia TextBlock to dynamically create Run elements for
//   matched terms with background highlighting. Uses regex for case-insensitive
//   term matching and rebuilds Inlines when Text or HighlightTerms change.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.6b: Initial implementation for search result highlighting
// =============================================================================

using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Controls;
using Avalonia.Media;

namespace Lexichord.Modules.RAG.Controls;

/// <summary>
/// A TextBlock control that highlights specified terms within its text content.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="HighlightedTextBlock"/> extends <see cref="TextBlock"/> to provide
/// automatic highlighting of query terms within displayed text. Terms are matched
/// using case-insensitive regex and rendered with a configurable background color.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// &lt;controls:HighlightedTextBlock
///     Text="{Binding PreviewText}"
///     HighlightTerms="{Binding QueryTerms}"
///     HighlightBrush="{DynamicResource HighlightBrush}" /&gt;
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6b as part of the Search Result Item View.
/// </para>
/// </remarks>
public class HighlightedTextBlock : TextBlock
{
    /// <summary>
    /// Defines the <see cref="HighlightTerms"/> styled property.
    /// </summary>
    /// <remarks>
    /// LOGIC: Property for the list of terms to highlight. When this changes,
    /// the Inlines collection is rebuilt to apply highlighting.
    /// </remarks>
    public static readonly StyledProperty<IReadOnlyList<string>> HighlightTermsProperty =
        AvaloniaProperty.Register<HighlightedTextBlock, IReadOnlyList<string>>(
            nameof(HighlightTerms),
            defaultValue: Array.Empty<string>());

    /// <summary>
    /// Defines the <see cref="HighlightBrush"/> styled property.
    /// </summary>
    /// <remarks>
    /// LOGIC: Property for the brush used to highlight matched terms.
    /// Defaults to a semi-transparent yellow for visibility.
    /// </remarks>
    public static readonly StyledProperty<IBrush> HighlightBrushProperty =
        AvaloniaProperty.Register<HighlightedTextBlock, IBrush>(
            nameof(HighlightBrush),
            defaultValue: new SolidColorBrush(Color.FromArgb(128, 255, 255, 0)));

    /// <summary>
    /// Gets or sets the list of terms to highlight in the text.
    /// </summary>
    /// <value>
    /// A read-only list of strings representing terms to highlight.
    /// Empty list or null results in no highlighting.
    /// </value>
    public IReadOnlyList<string> HighlightTerms
    {
        get => GetValue(HighlightTermsProperty);
        set => SetValue(HighlightTermsProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used for highlighting matched terms.
    /// </summary>
    /// <value>
    /// A brush applied as the background for highlighted text.
    /// Defaults to semi-transparent yellow.
    /// </value>
    public IBrush HighlightBrush
    {
        get => GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    /// <summary>
    /// Static constructor to register property change handlers.
    /// </summary>
    static HighlightedTextBlock()
    {
        // LOGIC: Subscribe to Text and HighlightTerms property changes
        // to rebuild the Inlines collection when either changes.
        TextProperty.Changed.AddClassHandler<HighlightedTextBlock>(
            (x, _) => x.UpdateInlines());
        HighlightTermsProperty.Changed.AddClassHandler<HighlightedTextBlock>(
            (x, _) => x.UpdateInlines());
        HighlightBrushProperty.Changed.AddClassHandler<HighlightedTextBlock>(
            (x, _) => x.UpdateInlines());
    }

    /// <summary>
    /// Rebuilds the Inlines collection to apply term highlighting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: Clears existing Inlines and rebuilds them by:
    /// <list type="number">
    ///   <item><description>If no terms or empty text, add single Run with full text.</description></item>
    ///   <item><description>Build regex pattern from escaped terms joined with OR.</description></item>
    ///   <item><description>Iterate through matches, adding normal and highlighted Runs.</description></item>
    ///   <item><description>Highlighted Runs get the HighlightBrush background.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private void UpdateInlines()
    {
        // LOGIC: Clear existing inlines before rebuilding.
        Inlines?.Clear();

        var text = Text;

        // LOGIC: If no text, nothing to display.
        if (string.IsNullOrEmpty(text))
            return;

        var terms = HighlightTerms;

        // LOGIC: If no terms to highlight, just add the text as-is.
        if (terms == null || terms.Count == 0)
        {
            Inlines?.Add(new Run(text));
            return;
        }

        // LOGIC: Build regex pattern from terms, escaping special characters.
        // Filter out empty/whitespace terms to avoid regex issues.
        var validTerms = terms
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(Regex.Escape)
            .ToArray();

        if (validTerms.Length == 0)
        {
            Inlines?.Add(new Run(text));
            return;
        }

        var pattern = string.Join("|", validTerms);
        Regex regex;

        try
        {
            regex = new Regex(pattern, RegexOptions.IgnoreCase);
        }
        catch (ArgumentException)
        {
            // LOGIC: If regex fails (malformed pattern), just display plain text.
            Inlines?.Add(new Run(text));
            return;
        }

        // LOGIC: Track position in text as we process matches.
        var lastIndex = 0;
        var brush = HighlightBrush;

        foreach (Match match in regex.Matches(text))
        {
            // LOGIC: Add any text before the match as a normal Run.
            if (match.Index > lastIndex)
            {
                Inlines?.Add(new Run(text[lastIndex..match.Index]));
            }

            // LOGIC: Add the matched text as a highlighted Run.
            Inlines?.Add(new Run(match.Value)
            {
                Background = brush,
                FontWeight = FontWeight.SemiBold
            });

            lastIndex = match.Index + match.Length;
        }

        // LOGIC: Add any remaining text after the last match.
        if (lastIndex < text.Length)
        {
            Inlines?.Add(new Run(text[lastIndex..]));
        }
    }
}
