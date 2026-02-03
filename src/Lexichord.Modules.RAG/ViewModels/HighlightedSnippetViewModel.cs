// =============================================================================
// File: HighlightedSnippetViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel for HighlightedSnippetControl that renders styled inlines.
// =============================================================================
// LOGIC: Converts Snippet to InlineCollection using IHighlightRenderer.
//   - Subscribes to theme changes to update colors.
//   - Converts StyledTextRun list to Avalonia Run elements.
//   - Supports both Light and Dark themes.
// =============================================================================
// VERSION: v0.5.6b (Query Term Highlighting)
// =============================================================================

using Avalonia.Controls.Documents;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Rendering;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel for <see cref="Controls.HighlightedSnippetControl"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="HighlightedSnippetViewModel"/> uses <see cref="IHighlightRenderer"/>
/// to convert a <see cref="Snippet"/> into styled <see cref="Run"/> elements
/// for display in a <c>SelectableTextBlock</c>.
/// </para>
/// <para>
/// <b>Theme Support:</b> The ViewModel accepts a <see cref="HighlightTheme"/>
/// and regenerates the formatted inlines when the theme changes.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6b as part of The Answer Preview feature.
/// </para>
/// </remarks>
public partial class HighlightedSnippetViewModel : ObservableObject
{
    private readonly IHighlightRenderer _renderer;

    [ObservableProperty]
    private Snippet? _snippet;

    [ObservableProperty]
    private HighlightTheme _theme = HighlightTheme.Light;

    [ObservableProperty]
    private InlineCollection? _formattedInlines;

    /// <summary>
    /// Initializes a new instance of <see cref="HighlightedSnippetViewModel"/>.
    /// </summary>
    /// <param name="renderer">The highlight renderer service.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="renderer"/> is <c>null</c>.
    /// </exception>
    public HighlightedSnippetViewModel(IHighlightRenderer renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    /// <summary>
    /// Called when <see cref="Snippet"/> changes.
    /// </summary>
    /// <param name="value">The new snippet value.</param>
    partial void OnSnippetChanged(Snippet? value)
    {
        UpdateFormattedInlines();
    }

    /// <summary>
    /// Called when <see cref="Theme"/> changes.
    /// </summary>
    /// <param name="value">The new theme value.</param>
    partial void OnThemeChanged(HighlightTheme value)
    {
        UpdateFormattedInlines();
    }

    /// <summary>
    /// Regenerates the formatted inlines from the current snippet and theme.
    /// </summary>
    private void UpdateFormattedInlines()
    {
        if (Snippet is null)
        {
            FormattedInlines = null;
            return;
        }

        var runs = _renderer.Render(Snippet, Theme);
        var inlines = new InlineCollection();

        foreach (var styledRun in runs)
        {
            var run = new Run(styledRun.Text);
            ApplyStyle(run, styledRun.Style);
            inlines.Add(run);
        }

        FormattedInlines = inlines;
    }

    /// <summary>
    /// Applies styling to a Run element based on TextStyle properties.
    /// </summary>
    /// <param name="run">The Run element to style.</param>
    /// <param name="style">The style to apply.</param>
    private static void ApplyStyle(Run run, TextStyle style)
    {
        // LOGIC: Apply bold weight.
        if (style.IsBold)
        {
            run.FontWeight = FontWeight.SemiBold;
        }

        // LOGIC: Apply italic style.
        if (style.IsItalic)
        {
            run.FontStyle = FontStyle.Italic;
        }

        // LOGIC: Apply foreground color if specified.
        if (!string.IsNullOrEmpty(style.ForegroundColor))
        {
            if (Color.TryParse(style.ForegroundColor, out var foreground))
            {
                run.Foreground = new SolidColorBrush(foreground);
            }
        }

        // LOGIC: Apply background color if specified.
        if (!string.IsNullOrEmpty(style.BackgroundColor))
        {
            if (Color.TryParse(style.BackgroundColor, out var background))
            {
                run.Background = new SolidColorBrush(background);
            }
        }
    }
}
